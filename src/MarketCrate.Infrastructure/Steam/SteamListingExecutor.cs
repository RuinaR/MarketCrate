using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

public sealed class SteamListingExecutor(SteamHttp http, SteamQrAuthentication auth, SteamStateStore store,
    SteamInventoryProvider inventory, SteamMarketDataProvider prices, SteamOrdersProvider orders, SteamTradingPolicy? policy = null) : IAuthorizedListingExecutor
{
    private readonly SemaphoreSlim mutations = new(1, 1);
    private readonly SemaphoreSlim repricing = new(1, 1);
    private readonly Dictionary<string, SaleLineSnapshot> approved = [];
    public Capability Capability => CanExecute ? Capability.Experimental : Capability.IntegrationBlocked;
    private readonly SteamTradingPolicy executionPolicy = policy ?? SteamTradingPolicy.Blocked;
    public bool CanExecute => auth.Connected && http.Access.Trading && executionPolicy.Enabled && !string.IsNullOrWhiteSpace(executionPolicy.EvidenceReference);
    public bool CanReconcile => auth.Connected;
    public async Task ValidateBatchAsync(SaleBatchSnapshot snapshot, CancellationToken token)
    {
        approved.Clear();
        if (!CanExecute || snapshot.AccountReference != auth.SteamId) throw new SteamException(SteamFailure.Disabled);
        BatchValidation.Validate(snapshot, DateTimeOffset.UtcNow);
        if (snapshot.Lines.Select(x => x.Asset.Key.AppId).Distinct().Count() != 1 ||
            snapshot.Lines.Any(x => x.Asset.Stackable && !executionPolicy.StackAmountsSupported))
            throw new SteamException(SteamFailure.Restricted);
        var current = (await inventory.GetAsync(token)).ToDictionary(x => x.Key);
        var schedule = await SteamFees.FetchAsync(http, snapshot.Lines[0].Asset.Key.AppId, token);
        foreach (var group in snapshot.Lines.GroupBy(x => x.Asset.Key))
            if (!current.TryGetValue(group.Key, out var found) || group.Sum(x => x.Quantity) > store.Available(found)) throw new SteamException(SteamFailure.Restricted);
        foreach (var line in snapshot.Lines)
        {
            if (!current.TryGetValue(line.Asset.Key, out var asset) || asset != line.Asset || !asset.Marketable || asset.Locked ||
                schedule.Currency != line.Currency || schedule.FromSeller(line.UnitQuote.SellerReceives) != line.UnitQuote) throw new SteamException(SteamFailure.Restricted);
            if (inventory.PublisherFees.TryGetValue(asset.Key, out var fee) && fee * 100L != schedule.Publisher.Millionths) throw new SteamException(SteamFailure.Restricted);
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
            if (line.Quantity > store.Available(line.Asset))
                throw new SteamException(SteamFailure.Restricted);
            var before = await orders.RefreshAsync(cancellationToken);
            var intent = new SteamCommandRecord(requestKey, auth.SteamId!, line, "sell", SaleState.Submitting, SentAt: DateTimeOffset.UtcNow, BeforeListingIds: before.Select(x => x.ListingId).ToArray());
            store.SaveCommand(intent); // Flush local intent before network; server does not accept our idempotency key.
            try
            {
                using var request = SteamHttp.Form(new("https://steamcommunity.com/market/sellitem/"), new Dictionary<string, string>
                {
                    ["sessionid"] = auth.SessionId!, ["appid"] = line.Asset.Key.AppId.ToString(CultureInfo.InvariantCulture), ["contextid"] = line.Asset.Key.ContextId,
                    ["assetid"] = line.Asset.Key.AssetId, ["amount"] = line.Quantity.ToString(CultureInfo.InvariantCulture),
                    ["price"] = SteamContract.ToWire(line.UnitQuote.SellerReceives, line.Currency).ToString(CultureInfo.InvariantCulture)
                });
                var body = await http.SendAsync(request, cancellationToken);
                var response = SteamContract.Sell(body);
                var conflict = SteamContract.Parse(body, root =>
                    root.TryGetProperty("price", out var serverPrice) && SteamContract.Integer(serverPrice) != new Money(line.UnitQuote.SellerReceives, line.Currency).Wire ||
                    root.TryGetProperty("fee", out var serverFee) && SteamContract.Integer(serverFee) != new Money(line.UnitQuote.Fees, line.Currency).Wire);
                if (conflict) response = (SaleState.Unknown, null);
                if (response.ListingId is not null && store.Commands().Any(x => x.Kind == "sell" && x.RequestKey != requestKey && x.ListingId == response.ListingId))
                    response = (SaleState.Unknown, null);
                if (response.State == SaleState.Unknown) http.Stop(SteamFailure.ContractChanged);
                store.SaveCommand(intent with { State = response.State, ListingId = response.ListingId, Accepted = response.State != SaleState.Unknown });
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
        if (store.IsReleased(requestKey)) return new(line.LineId,SaleState.Reconciled,"취소 및 동일 자산 반환 증거로 원래 등록 예약 해제");
        if (command.ListingId is null)
        {
            var report = await InspectAsync(requestKey, cancellationToken);
            return new(line.LineId, command.State == SaleState.AwaitingSteamGuard ? command.State : SaleState.Unknown, $"대조 {report.Verdict}: 인과관계 근거 없음, 자동 확정/재전송 금지");
        }
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
            var original = store.Commands().SingleOrDefault(x => x.Kind == "sell" && x.ListingId == reviewed.ListingId);
            var returnKey = original?.Line.Asset.Key ?? reviewed.Asset;
            var available = (await inventory.GetAsync(token)).SingleOrDefault(x => x.Key == returnKey)?.Quantity ?? 0;
            var asset = new InventoryAsset(returnKey, "Steam", reviewed.HashName, reviewed.HashName, reviewed.Quantity, original?.Line.Asset.Stackable ?? reviewed.Quantity > 1, true, false);
            var line = new SaleLineSnapshot(reviewed.ListingId, asset, reviewed.Quantity, reviewed.Currency, new(reviewed.SellerReceives + reviewed.Fees, reviewed.SellerReceives, reviewed.Fees, 0), DateTimeOffset.UtcNow);
            var intent = new SteamCommandRecord(requestKey, auth.SteamId!, line, "cancel", SaleState.Submitting, reviewed.ListingId, DateTimeOffset.UtcNow, AvailableBefore: available);
            store.SaveCommand(intent);
            try
            {
                using var request = SteamHttp.Form(new("https://steamcommunity.com/market/removelisting/" + reviewed.ListingId), new Dictionary<string, string> { ["sessionid"] = auth.SessionId! });
                var accepted = SteamContract.CancellationAccepted(await http.SendAsync(request, token));
                if (!accepted) http.Stop(SteamFailure.ContractChanged);
                store.SaveCommand(intent with { State = accepted ? SaleState.Submitted : SaleState.Unknown, Accepted = accepted });
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
        if (store.Return(requestKey) is not null) return new(intent.Line.LineId, SaleState.Reconciled, "저널의 확정 자산 반환 증거 보존");
        var current = (await orders.RefreshAsync(token)).SingleOrDefault(x => x.ListingId == intent.ListingId);
        var returned = (await inventory.GetAsync(token)).SingleOrDefault(x => x.Key == intent.Line.Asset.Key && x.MarketHashName == intent.Line.Asset.MarketHashName);
        var originalSell=store.Commands().SingleOrDefault(x=>x.Kind=="sell"&&x.ListingId==intent.ListingId);
        // Stack increases may be unrelated merges. Without a causal cancellation event, do not release them.
        var confirmed = originalSell is not null && !originalSell.Line.Asset.Stackable && intent.Line.Quantity==1 && current is null && intent.Accepted && returned is not null && returned.Quantity >= checked(intent.AvailableBefore + intent.Line.Quantity);
        var state = confirmed ? SaleState.Reconciled : current is null ? SaleState.Unknown : SaleState.Submitted;
        if (confirmed)
        {
            store.SaveReturn(new(requestKey, originalSell!.RequestKey, returned!.Key, intent.Line.Quantity, DateTimeOffset.UtcNow));
        }
        store.SaveCommand(intent with { State = state });
        return new(intent.Line.LineId, state, confirmed ? "취소 접수·목록 부재·동일 단일 자산 반환 대조 완료" : current is null ? "목록 부재/스택 증가만으로 취소 인과관계 확인 불가: 재등록 차단" : "목록에 아직 존재: 취소 완료 아님");
    }
    public async Task<SteamEvidenceReport> InspectAsync(string requestKey, CancellationToken token = default)
    {
        var command = store.ReadCommand(requestKey);
        if (!CanReconcile || command is null || command.Account != auth.SteamId) throw new SteamException(SteamFailure.Restricted);
        var rows = await orders.RefreshAsync(token); var line = command.Line;
        var candidates = rows.Where(x => x.HashName == line.Asset.MarketHashName && x.Asset.AppId == line.Asset.Key.AppId).Select(x =>
        {
            var reason = (command.BeforeListingIds ?? []).Contains(x.ListingId) ? SteamEvidenceReason.ExistingBeforeRequest :
                x.Asset != line.Asset.Key || x.Quantity != line.Quantity || x.Currency != line.Currency || x.SellerReceives != line.UnitQuote.SellerReceives || x.Fees != line.UnitQuote.Fees ? SteamEvidenceReason.FieldsMismatch :
                command.SentAt is null || x.CreatedAt is null || x.CreatedAt < command.SentAt.Value.AddSeconds(-5) || x.CreatedAt > command.SentAt.Value.AddMinutes(2) ? SteamEvidenceReason.TimeMissingOrOutside :
                command.ListingId == x.ListingId ? SteamEvidenceReason.ServerIdAndFields : SteamEvidenceReason.CandidateOnly;
            return new SteamEvidenceCandidate(x.ListingId, reason, x.Quantity, x.SellerReceives, x.CreatedAt);
        }).ToArray();
        var matches = candidates.Count(x => x.Reason == SteamEvidenceReason.CandidateOnly);
        var verdict = candidates.Any(x => x.Reason == SteamEvidenceReason.ServerIdAndFields) ? SteamEvidenceReason.ServerIdAndFields : matches > 1 ? SteamEvidenceReason.MultipleCandidates : matches == 1 ? SteamEvidenceReason.CandidateOnly : SteamEvidenceReason.NoCandidate;
        var report = new SteamEvidenceReport(requestKey, DateTimeOffset.UtcNow, verdict, candidates); store.SaveEvidence(report); return report;
    }
    public async Task<SteamRepriceIntent> RepriceAsync(SteamListing listing, long buyer, string key, CancellationToken token = default)
    {
        await repricing.WaitAsync(token);
        try { return await RepriceCoreAsync(listing,buyer,key,token); }
        finally { repricing.Release(); }
    }
    private async Task<SteamRepriceIntent> RepriceCoreAsync(SteamListing listing, long buyer, string key, CancellationToken token)
    {
        if (!CanExecute) throw new SteamException(SteamFailure.Disabled);
        var intent = store.Reprice(key);
        if (intent is null)
        {
            if (store.Reprices().Any(x=>x.Account==auth.SteamId&&x.Listing.ListingId==listing.ListingId)) throw new SteamException(SteamFailure.Restricted);
            var schedule = await SteamFees.FetchAsync(http, listing.Asset.AppId, token); if (listing.Currency != schedule.Currency) throw new SteamException(SteamFailure.Restricted);
            _ = schedule.FromBuyer(buyer);
            intent = new(key, auth.SteamId!, listing, buyer, DateTimeOffset.UtcNow, SteamRepriceState.AwaitingCancellation); store.SaveReprice(intent);
        }
        if (intent.Account != auth.SteamId || intent.Listing != listing || intent.BuyerPrice != buyer) throw new SteamException(SteamFailure.Restricted);
        if (intent.ReplacementKey is not null) return intent; // Never submit replacement twice after restart/timeout.
        var cancelKey = key + ":cancel";
        if (store.ReadCommand(cancelKey) is null) await CancelAsync(listing, cancelKey, token);
        var status = await ReconcileCancellationAsync(cancelKey, token);
        if (status.State != SaleState.Reconciled) { intent = intent with { State = SteamRepriceState.AwaitingReturn }; store.SaveReprice(intent); return intent; }
        if (DateTimeOffset.UtcNow - intent.CreatedAt > TimeSpan.FromMinutes(5)) throw new SteamException(SteamFailure.StaleReview);
        var proof = store.Return(cancelKey)!; var asset = (await inventory.GetAsync(token)).Single(x => x.Key == proof.ReturnedAsset);
        var fees = await SteamFees.FetchAsync(http, asset.Key.AppId, token); var price = await prices.RefreshAsync(asset, token);
        var replacement = new SaleLineSnapshot("replacement", asset, listing.Quantity, fees.Currency, fees.FromBuyer(buyer), price.CapturedAt);
        var batch = new SaleBatchSnapshot(Guid.NewGuid(), auth.SteamId!, DateTimeOffset.UtcNow, [replacement]);
        await ValidateBatchAsync(batch, token); var replacementKey = SaleBatchCoordinator.RequestKey(batch, replacement);
        intent = intent with { State = SteamRepriceState.Submitting, ReplacementKey = replacementKey }; store.SaveReprice(intent);
        var result = await SubmitAsync(replacement, replacementKey, token);
        intent = intent with { State = result.State == SaleState.Unknown ? SteamRepriceState.Unknown : SteamRepriceState.Submitted }; store.SaveReprice(intent); return intent;
    }
}
