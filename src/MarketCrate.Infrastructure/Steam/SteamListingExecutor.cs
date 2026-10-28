using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

public sealed class SteamListingExecutor(SteamHttp http, SteamQrAuthentication auth, SteamStateStore store,
    SteamInventoryProvider inventory, SteamMarketDataProvider prices, SteamOrdersProvider orders) : IAuthorizedListingExecutor
{
    private readonly SemaphoreSlim mutations = new(1, 1);
    private readonly Dictionary<string, SaleLineSnapshot> approved = [];
    public Capability Capability => CanExecute ? Capability.Experimental : Capability.IntegrationBlocked;
    public bool CanExecute => auth.Connected && http.Access.Trading;
    public bool CanReconcile => auth.Connected;
    public async Task ValidateBatchAsync(SaleBatchSnapshot snapshot, CancellationToken token)
    {
        approved.Clear();
        if (!CanExecute || snapshot.AccountReference != auth.SteamId) throw new SteamException(SteamFailure.Disabled);
        BatchValidation.Validate(snapshot, DateTimeOffset.UtcNow);
        // Partial stack sale/asset-ID replacement remains unverified: do not silently treat it as individual assets.
        if (snapshot.Lines.Any(x => x.Quantity != 1 || x.Asset.Stackable) || snapshot.Lines.Select(x => x.Asset.Key).Distinct().Count() != snapshot.Lines.Length)
            throw new SteamException(SteamFailure.Restricted);
        if (store.Commands().Any(x => x.Kind == "sell" && snapshot.Lines.Any(l => l.Asset.Key == x.Line.Asset.Key)))
            throw new SteamException(SteamFailure.Restricted);
        var current = (await inventory.GetAsync(token)).ToDictionary(x => x.Key);
        using var walletRequest = new HttpRequestMessage(HttpMethod.Get, "https://steamcommunity.com/market/");
        var wallet = Wallet(await http.SendAsync(walletRequest, token, allowWalletHtml: true));
        foreach (var line in snapshot.Lines)
        {
            if (!current.TryGetValue(line.Asset.Key, out var asset) || asset != line.Asset || !asset.Marketable || asset.Locked ||
                !inventory.PublisherFees.TryGetValue(asset.Key, out var fee)) throw new SteamException(SteamFailure.Restricted);
            SteamFeeMath.ValidateExact(line, wallet with { GameBasisPoints = fee });
            var price = await prices.RefreshAsync(asset, token);
            if (price.Currency != line.Currency || price.Bids.IsEmpty) throw new SteamException(SteamFailure.Restricted);
            var reference = price.Bids[0].BuyerPays;
            if (line.UnitQuote.BuyerPays < reference / 2 || (decimal)line.UnitQuote.BuyerPays > (decimal)reference * 2)
                throw new SteamException(SteamFailure.Restricted); // Require a new review, never reprice the immutable approval.
        }
        foreach (var line in snapshot.Lines) approved.Add(SaleBatchCoordinator.RequestKey(snapshot, line), line);
    }
    internal static FeePolicy Wallet(string html)
    {
        try
        {
            var index = html.IndexOf("g_rgWalletInfo", StringComparison.Ordinal);
            if (index < 0) throw new SteamException(SteamFailure.Authentication);
            index = html.IndexOf('=', index) + 1; var json = html.IndexOf('{', index);
            if (index == 0 || json < index || json - index > 16) throw new SteamException(SteamFailure.ContractChanged);
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(html[json..])); using var document = JsonDocument.ParseValue(ref reader); var root = document.RootElement;
            if (SteamContract.Integer(root.GetProperty("wallet_fee")) != 1 || SteamContract.Integer(root.GetProperty("wallet_currency")) != 1 || root.GetProperty("wallet_fee_percent").GetDecimal() != .05m ||
                SteamContract.Integer(root.GetProperty("wallet_fee_minimum")) != 1 || SteamContract.Integer(root.GetProperty("wallet_fee_base")) != 0)
                throw new SteamException(SteamFailure.Restricted);
            return new();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { throw new SteamException(SteamFailure.ContractChanged); }
    }
    public async Task<LineResult> SubmitAsync(SaleLineSnapshot line, string requestKey, CancellationToken cancellationToken)
    {
        await mutations.WaitAsync(cancellationToken);
        try
        {
            var old = store.ReadCommand(requestKey);
            if (old is not null)
            {
                if (old.Line != line || old.Kind != "sell" || old.Account != auth.SteamId) throw new SteamException(SteamFailure.Restricted);
                return new(line.LineId, old.State == SaleState.Submitting ? SaleState.Unknown : old.State, "기존 요청: 재전송하지 않음");
            }
            if (!CanExecute || !approved.TryGetValue(requestKey, out var approval) || approval != line || DateTimeOffset.UtcNow - line.MarketTime > TimeSpan.FromMinutes(5))
                throw new SteamException(SteamFailure.Disabled);
            if (store.Commands().Any(x => x.Kind == "sell" && x.Line.Asset.Key == line.Asset.Key))
                throw new SteamException(SteamFailure.Restricted);
            var intent = new SteamCommandRecord(requestKey, auth.SteamId!, line, "sell", SaleState.Submitting);
            store.SaveCommand(intent); // Flush local intent before network; server does not accept our idempotency key.
            try
            {
                using var request = SteamHttp.Form(new("https://steamcommunity.com/market/sellitem/"), new Dictionary<string, string>
                {
                    ["sessionid"] = auth.SessionId!, ["appid"] = line.Asset.Key.AppId.ToString(CultureInfo.InvariantCulture), ["contextid"] = line.Asset.Key.ContextId,
                    ["assetid"] = line.Asset.Key.AssetId, ["amount"] = line.Quantity.ToString(CultureInfo.InvariantCulture),
                    ["price"] = SteamContract.ToWire(line.UnitQuote.SellerReceives, line.Currency).ToString(CultureInfo.InvariantCulture)
                });
                var response = SteamContract.Sell(await http.SendAsync(request, cancellationToken));
                if (response.ListingId is not null && store.Commands().Any(x => x.Kind == "sell" && x.RequestKey != requestKey && x.ListingId == response.ListingId))
                    response = (SaleState.Unknown, null);
                if (response.State == SaleState.Unknown) http.Stop(SteamFailure.ContractChanged);
                store.SaveCommand(intent with { State = response.State, ListingId = response.ListingId });
                return new(line.LineId, response.State, response.State == SaleState.AwaitingSteamGuard ? "공식 Steam 앱/이메일 수동 확인 대기: 게시 완료 아님" : "접수 응답: 서버 게시 확인 필요");
            }
            catch (Exception error) when (error is SteamException or OperationCanceledException or HttpRequestException)
            {
                http.Stop(error is SteamException steam ? steam.Code : SteamFailure.Transport);
                store.SaveCommand(intent with { State = SaleState.Unknown });
                return new(line.LineId, SaleState.Unknown, "Steam 응답 불명/접근 중단: 자동 재전송 금지");
            }
        }
        finally { mutations.Release(); }
    }
    public async Task<LineResult> ReconcileAsync(SaleLineSnapshot line, string requestKey, CancellationToken cancellationToken)
    {
        var command = store.ReadCommand(requestKey);
        if (!CanReconcile || command is null || command.Account != auth.SteamId || command.Line != line || command.Kind != "sell")
            return new(line.LineId, SaleState.Unknown, "동일 계정/요청 증거 없음");
        // sellitem often omits listingid. Asset/name/price similarity alone is not a causal correlation.
        if (command.ListingId is null) return new(line.LineId, command.State == SaleState.AwaitingSteamGuard ? command.State : SaleState.Unknown, "서버 listingid 근거 없음: 자동 재전송/성공 확정 금지");
        var listing = (await orders.RefreshAsync(cancellationToken)).SingleOrDefault(x => x.ListingId == command.ListingId);
        if (listing is null || listing.HashName != line.Asset.MarketHashName || listing.Asset.AppId != line.Asset.Key.AppId || listing.Asset.ContextId != line.Asset.Key.ContextId || listing.Quantity != line.Quantity ||
            listing.Currency != line.Currency || listing.SellerReceives != line.UnitQuote.SellerReceives || listing.Fees != line.UnitQuote.Fees)
            return new(line.LineId, SaleState.Unknown, "목록 부재/불일치: 판매/취소/비표시 구분 불가");
        store.SaveCommand(command with { State = listing.State });
        return new(line.LineId, listing.State, "서버 listingid·가격·수량 대조 결과");
    }
    public async Task<LineResult> CancelAsync(SteamListing reviewed, string requestKey, CancellationToken token = default)
    {
        await mutations.WaitAsync(token);
        try
        {
            if (!CanExecute) throw new SteamException(SteamFailure.Disabled);
            var previous = store.ReadCommand(requestKey);
            if (previous is not null) return new(reviewed.ListingId, SaleState.Unknown, "기존 취소 의도: 재전송 금지");
            if (store.Commands().Any(x => x.Kind == "cancel" && x.ListingId == reviewed.ListingId)) return new(reviewed.ListingId, SaleState.Unknown, "다른 키의 취소 의도 존재");
            var current = (await orders.RefreshAsync(token)).SingleOrDefault(x => x.ListingId == reviewed.ListingId);
            if (current != reviewed) throw new SteamException(SteamFailure.Restricted);
            var asset = new InventoryAsset(reviewed.Asset, "Steam", reviewed.HashName, reviewed.HashName, reviewed.Quantity, reviewed.Quantity > 1, true, false);
            var line = new SaleLineSnapshot(reviewed.ListingId, asset, reviewed.Quantity, reviewed.Currency, new(reviewed.SellerReceives + reviewed.Fees, reviewed.SellerReceives, reviewed.Fees, 0), DateTimeOffset.UtcNow);
            var intent = new SteamCommandRecord(requestKey, auth.SteamId!, line, "cancel", SaleState.Submitting, reviewed.ListingId);
            store.SaveCommand(intent);
            try
            {
                using var request = SteamHttp.Form(new("https://steamcommunity.com/market/removelisting/" + reviewed.ListingId), new Dictionary<string, string> { ["sessionid"] = auth.SessionId! });
                var accepted = SteamContract.CancellationAccepted(await http.SendAsync(request, token));
                if (!accepted) http.Stop(SteamFailure.ContractChanged);
                store.SaveCommand(intent with { State = accepted ? SaleState.Submitted : SaleState.Unknown });
                return new(reviewed.ListingId, accepted ? SaleState.Submitted : SaleState.Unknown, "취소 요청 결과: 취소 완료/자산 반환은 별도 확인 필요");
            }
            catch (Exception error) when (error is SteamException or OperationCanceledException or HttpRequestException)
            { store.SaveCommand(intent with { State = SaleState.Unknown }); return new(reviewed.ListingId, SaleState.Unknown, "취소 불명: 재전송/재등록 금지"); }
        }
        finally { mutations.Release(); }
    }
    public async Task<IReadOnlyList<LineResult>> CancelBatchAsync(IReadOnlyList<SteamListing> reviewed, string batchKey, CancellationToken token = default)
    {
        if (reviewed.Select(x => x.ListingId).Distinct().Count() != reviewed.Count) throw new SteamException(SteamFailure.Restricted);
        var results = new List<LineResult>();
        foreach (var row in reviewed.ToImmutableArray())
        {
            var result = await CancelAsync(row, batchKey + ":" + row.ListingId, token); results.Add(result);
            if (result.State == SaleState.Unknown) break;
        }
        return results;
    }
    public async Task<LineResult> ReconcileCancellationAsync(string requestKey, CancellationToken token = default)
    {
        var intent = store.ReadCommand(requestKey);
        if (!CanReconcile || intent is null || intent.Kind != "cancel" || intent.Account != auth.SteamId || intent.ListingId is null)
            throw new SteamException(SteamFailure.Restricted);
        var current = (await orders.RefreshAsync(token)).SingleOrDefault(x => x.ListingId == intent.ListingId);
        // Absence also means sold/hidden/changed IDs. Never unlock re-listing based on absence.
        var state = current is null ? SaleState.Unknown : SaleState.Submitted;
        store.SaveCommand(intent with { State = state });
        return new(intent.Line.LineId, state, current is null ? "목록 부재: 취소/판매 구분 증거 없음, 재등록 차단" : "목록에 아직 존재: 취소 완료 아님");
    }
}
