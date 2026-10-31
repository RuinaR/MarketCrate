using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

public sealed class SteamListingExecutor(SteamHttp http, SteamQrAuthentication auth, SteamStateStore store,
    SteamInventoryProvider inventory, SteamMarketDataProvider prices, SteamOrdersProvider orders, SteamTradingPolicy? policy = null) : IAuthorizedListingExecutor
{
    private readonly SemaphoreSlim mutations = store.Mutations;
    private readonly SemaphoreSlim repricing = new(1, 1);
    private readonly Dictionary<string, SaleLineSnapshot> approved = [];
    private readonly SteamSaleHistory saleHistory=new(http);
    public Capability Capability => CanExecute ? Capability.Experimental : Capability.IntegrationBlocked;
    private readonly SteamTradingPolicy executionPolicy = policy ?? SteamTradingPolicy.Blocked;
    public bool CanCancel => auth.Connected && http.Halted is null && http.Access.Trading && executionPolicy.Enabled && !string.IsNullOrWhiteSpace(executionPolicy.EvidenceReference) && (!executionPolicy.UserInitiated||http.Access.TradingAccount==auth.SteamId);
    public bool CanExecute => CanCancel&&!http.Access.CancellationOnly&&!executionPolicy.CancellationOnly;
    public bool CanReconcile => auth.Connected && http.Halted is null;
    // Called only with a completed, freshly parsed authenticated listing read. No candidate attribution by price alone.
    public IReadOnlyList<LineResult> ObserveListings(IReadOnlyList<SteamListing> listings)
    {
        if(!CanReconcile)return [];
        var byId=listings.ToDictionary(x=>x.ListingId);var results=new List<LineResult>();
        foreach(var command in store.ReservedSells().Where(x=>x.Account==auth.SteamId&&x.ListingId is not null))
        {
            if(!byId.TryGetValue(command.ListingId!,out var actual))continue; // Absence is not proof of failure/return.
            var line=command.Line;
            if(actual.Asset!=line.Asset.Key||actual.HashName!=line.Asset.MarketHashName||actual.Quantity!=line.Quantity||actual.Currency!=line.Currency||actual.SellerReceives!=line.UnitQuote.SellerReceives||actual.Fees!=line.UnitQuote.Fees)continue;
            if(command.State==SaleState.Listed&&actual.State!=SaleState.Listed)continue;
            if(command.State!=actual.State)store.SaveCommand(command with{State=actual.State});
            results.Add(new(line.LineId,actual.State,actual.State==SaleState.AwaitingSteamGuard?SteamContract.ApprovalHint(command.Approval):"현재 Steam 판매목록의 등록 ID·상품·가격·수량 일치"));
            DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Management,DiagnosticOutcome.Reconciled,from:command.State.ToString(),to:actual.State.ToString(),reason:"ServerInventorySync",requirement:"F09");
        }
        return results;
    }
    // Validate the entire management review before the first cancellation. Not an atomic server transaction.
    public async Task ValidateRepriceBatchAsync(IReadOnlyList<(SteamListing Listing,long BuyerPrice,string Book)> review,CancellationToken token)
    {
        if(!CanExecute||review.Count==0||review.Select(x=>x.Listing.ListingId).Distinct().Count()!=review.Count)throw new SteamException(SteamFailure.Disabled);
        var account=auth.SteamId;
        if(review.Any(x=>x.Listing.State!=SaleState.Listed||x.Listing.Quantity!=1||executionPolicy.UserInitiated&&!executionPolicy.VerifiedApps.Contains(x.Listing.Asset.AppId)))throw new SteamException(SteamFailure.Restricted);
        // External listings lack the original inventory identity/return evidence in our journal.
        // Do not cancel them as part of a reprice that cannot safely complete.
        var originals=store.Commands().Where(x=>x.Kind=="sell"&&x.Account==account&&x.ListingId is not null&&!x.Line.Asset.Stackable).Select(x=>x.ListingId).ToHashSet(StringComparer.Ordinal);
        if(review.Any(x=>!originals.Contains(x.Listing.ListingId)))throw new SteamException(SteamFailure.Unsupported){Endpoint="RepriceReturn"};
        var current=(await orders.RefreshAsync(token)).ToDictionary(x=>x.ListingId);
        var schedules=new Dictionary<int,SteamFeeSchedule>();var books=new Dictionary<(int,string),PriceSnapshot>();
        var wallet=executionPolicy.UserInitiated?await SteamFees.FetchTradingWalletAsync(http,account!,token):null;
        foreach(var app in review.Select(x=>x.Listing.Asset.AppId).Distinct())
        {var schedule=await SteamFees.FetchResolvedAsync(http,auth,app,token);if(wallet is not null&&(schedule.Currency!=wallet.Currency||schedule.MinimumWire!=wallet.MinimumWire||schedule.IncrementWire!=wallet.IncrementWire))throw new SteamException(SteamFailure.Restricted);schedules.Add(app,schedule);}
        foreach(var row in review)
        {
            token.ThrowIfCancellationRequested();
            if(!current.TryGetValue(row.Listing.ListingId,out var actual)||actual!=row.Listing)throw new SteamException(SteamFailure.StaleReview);
            var schedule=schedules[row.Listing.Asset.AppId];if(schedule.Currency!=row.Listing.Currency||schedule.FromSeller(row.Listing.SellerReceives).Fees!=row.Listing.Fees)throw new SteamException(SteamFailure.Restricted);
            _=schedule.FromBuyer(row.BuyerPrice);
            var key=(row.Listing.Asset.AppId,row.Listing.HashName);
            if(!books.TryGetValue(key,out var price)){var a=new InventoryAsset(row.Listing.Asset,"Steam",row.Listing.HashName,row.Listing.HashName,1,false,true,false);price=await prices.RefreshAsync(a,token);books.Add(key,price);}
            if(price.Currency!=schedule.Currency||price.Bids.IsEmpty||OrderbookFingerprint.Of(price)!=row.Book||row.BuyerPrice<price.Bids[0].BuyerPays/2||(decimal)row.BuyerPrice>(decimal)price.Bids[0].BuyerPays*2)throw new SteamException(SteamFailure.StaleReview);
        }
        if(!CanExecute||auth.SteamId!=account)throw new SteamException(SteamFailure.Authentication);
        DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Management,requirement:"B10",count:review.Count,reason:"Fresh");
    }
    public async Task ValidateBatchAsync(SaleBatchSnapshot snapshot, CancellationToken token)
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.FinalValidation,DiagnosticArea.Sales,"E09");
        approved.Clear();
        if (!CanExecute || snapshot.AccountReference != auth.SteamId) throw new SteamException(SteamFailure.Disabled);
        if(snapshot.Lines.Any(x=>x.Evidence is not null)&&!executionPolicy.MedianPriceContractSupported)throw new SteamException(SteamFailure.Restricted);
        BatchValidation.Validate(snapshot, DateTimeOffset.UtcNow);
        if (snapshot.Lines.GroupBy(x=>x.Asset.Key).Any(group=>group.First().Asset.Stackable&&!executionPolicy.StackAmountsSupported&&(!executionPolicy.SingleRequestStackApps.Contains(group.Key.AppId)||group.Count()!=1)) ||
            executionPolicy.UserInitiated&&snapshot.Lines.Any(x=>x.OrderbookDigest is null||!executionPolicy.VerifiedApps.Contains(x.Asset.Key.AppId)))
            throw new SteamException(SteamFailure.Restricted);
        var current=new Dictionary<AssetKey,InventoryAsset>();var publisherFees=new Dictionary<AssetKey,int>();
        foreach(var scope in snapshot.Lines.GroupBy(x=>(x.Asset.Key.AppId,x.Asset.Key.ContextId)))
        {
            var provider=inventory.AppId==scope.Key.AppId&&inventory.ContextId==scope.Key.ContextId&&inventory.GameName==scope.First().Asset.Game?inventory:new SteamInventoryProvider(http,auth,scope.Key.AppId,scope.Key.ContextId,scope.First().Asset.Game);
            foreach(var asset in await provider.GetAsync(token))current.Add(asset.Key,asset);
            foreach(var fee in provider.PublisherFees)publisherFees.Add(fee.Key,fee.Value);
        }
        var schedules=new Dictionary<int,SteamFeeSchedule>();
        foreach(var app in snapshot.Lines.Select(x=>x.Asset.Key.AppId).Distinct())
        {
            var schedule=await SteamFees.FetchResolvedAsync(http,auth,app,token,requireWalletIdentity:executionPolicy.UserInitiated);
            if(executionPolicy.UserInitiated&&schedules.Values.FirstOrDefault() is {} previous&&(schedule.Currency!=previous.Currency||schedule.MinimumWire!=previous.MinimumWire||schedule.IncrementWire!=previous.IncrementWire))throw new SteamException(SteamFailure.Restricted);
            schedules.Add(app,schedule);
        }
        if(snapshot.Lines.Any(x=>x.Asset.Stackable&&!executionPolicy.StackAmountsSupported&&schedules[x.Asset.Key.AppId].PublisherFallbackAppId!=x.Asset.Key.AppId))throw new SteamException(SteamFailure.Unsupported){Endpoint="StackContract"};
        var currentListings=await orders.RefreshAsync(token);
        foreach (var group in snapshot.Lines.GroupBy(x => x.Asset.Key))
        {
            if (!current.TryGetValue(group.Key, out var found))throw new SteamException(SteamFailure.Restricted);
            ValidateServerQuantity(found,group.Sum(x=>x.Quantity),currentListings);
        }
        var validatedPrices=new Dictionary<(int,string,long),PriceSnapshot>();
        foreach (var line in snapshot.Lines)
        {
            var schedule=schedules[line.Asset.Key.AppId];
            if (!current.TryGetValue(line.Asset.Key, out var asset) || asset != line.Asset || !asset.Marketable || asset.Locked ||
                schedule.Currency != line.Currency || schedule.FromSeller(line.UnitQuote.SellerReceives) != line.UnitQuote) throw new SteamException(SteamFailure.Restricted);
            if (!schedule.PublisherMatches(asset.Key.AppId,publisherFees.TryGetValue(asset.Key,out var fee)?fee:null)) throw new SteamException(SteamFailure.Restricted);
            var priceKey=(asset.Key.AppId,asset.MarketHashName,line.Evidence is {} source?(source.WindowEnd-source.WindowStart).Ticks:0);
            if(!validatedPrices.TryGetValue(priceKey,out var price))
            {price=line.Evidence is {} evidenceSource?await prices.RefreshEstimateAsync(asset,evidenceSource.WindowEnd-evidenceSource.WindowStart,token):await prices.RefreshAsync(asset,token);validatedPrices.Add(priceKey,price);}
            if(line.OrderbookDigest is {} digest&&OrderbookFingerprint.Of(price)!=digest)
            {DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Sales,DiagnosticOutcome.Blocked,LogLevel.Warning,DiagnosticError.Validation,reason:"OrderbookChanged");throw new SteamException(SteamFailure.StaleReview){Endpoint="OrderbookChanged",RequestNotSent=true};}
            if(line.Evidence is {} evidence)
            {
                if(evidence.Currency!=line.Currency||!evidence.Fresh(DateTimeOffset.UtcNow))throw new SteamException(SteamFailure.StaleReview);
                if(price.Estimate is not {} fresh||fresh.Minor!=evidence.Minor||fresh.Buckets!=evidence.Buckets||fresh.Volume!=evidence.Volume||fresh.SampleDigest!=evidence.SampleDigest){DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Sales,DiagnosticOutcome.Blocked,LogLevel.Warning,DiagnosticError.Validation,reason:"HistoryChanged",requirement:"E09");throw new SteamException(SteamFailure.StaleReview);}
            }
            if (price.Currency != line.Currency || price.Bids.IsEmpty) throw new SteamException(SteamFailure.Restricted);
            var reference = price.Bids[0].BuyerPays;
            if (line.UnitQuote.BuyerPays < reference / 2 || (decimal)line.UnitQuote.BuyerPays > (decimal)reference * 2)
                throw new SteamException(SteamFailure.Restricted); // Require a new review, never reprice the immutable approval.
        }
        BatchValidation.Validate(snapshot,DateTimeOffset.UtcNow); // Network preflight must not age an approval into a durable submission intent.
        foreach (var line in snapshot.Lines) approved.Add(SaleBatchCoordinator.RequestKey(snapshot, line), line);
    }
    // The server owns inventory/listing state. Old records are evidence, not Steam reservations.
    // A NEW user-approved batch may use a currently owned individual asset; it never replays an old key.
    private void ValidateServerQuantity(InventoryAsset asset,int quantity,IReadOnlyList<SteamListing> listings)
    {
        if(!asset.Marketable||asset.Locked||quantity<1||quantity>asset.Quantity)throw new SteamException(SteamFailure.Restricted){Endpoint="ServerInventory"};
        var listed=listings.Any(x=>x.Asset==asset.Key)||orders.Presence.Any(x=>x.Asset==asset.Key);
        if(!asset.Stackable&&listed)throw new SteamException(SteamFailure.Restricted){Endpoint="ServerListing"};
        var uncertain=store.Commands().Where(x=>x.Account==auth.SteamId&&x.Kind=="sell"&&x.Line.Asset.Key==asset.Key&&x.State is SaleState.Unknown or SaleState.Submitting).ToArray();
        if(uncertain.Length==0)return;
        // A short settlement window avoids racing a response-lost request with a stale inventory replica.
        // It is a local precaution, not a guarantee about Steam propagation or request idempotence.
        if(uncertain.Any(x=>x.SentAt is {} sent&&DateTimeOffset.UtcNow-sent<TimeSpan.FromMinutes(2)))
            throw new SteamException(SteamFailure.Restricted){Endpoint="PendingSubmissionSettling"};
        if(asset.Stackable||listed)throw new SteamException(SteamFailure.Restricted){Endpoint="UncertainStack"};
        DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Sales,DiagnosticOutcome.Completed,reason:"FreshOwnedAssetNewApproval",requirement:"F05",count:uncertain.Length,journal:"UnknownPreserved");
    }
#if MARKETCRATE_DEMO
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
#endif
    public async Task<LineResult> SubmitAsync(SaleLineSnapshot line, string requestKey, CancellationToken cancellationToken)
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.ItemResult,DiagnosticArea.Sales,"F01");
        if(auth.SteamId is not {} account)throw new SteamException(SteamFailure.Disabled){RequestNotSent=true};
        using var assetOperation=store.AssetOperations.TryClaim(account,line.Asset.Key.AssetId);
        if(assetOperation is null)
        {
            DiagnosticHub.Emit(DiagnosticEvent.ItemResult,DiagnosticArea.Sales,DiagnosticOutcome.Blocked,reason:"ConcurrentAssetOperation",to:SaleState.Failed.ToString(),requirement:"F05");
            return new(line.LineId,SaleState.Failed,"같은 자산의 다른 작업이 처리 중입니다. 이 요청은 전송하지 않았습니다.");
        }
        var entered=false;var sendStarted=false;
        try
        {
            await mutations.WaitAsync(cancellationToken);entered=true;
            var old = store.ReadCommand(requestKey);
            if (old is not null)
            {
                if (old.Line != line || old.Kind != "sell" || old.Account != auth.SteamId) throw new SteamException(SteamFailure.Restricted);
                return new(line.LineId, old.State == SaleState.Submitting ? SaleState.Unknown : old.State, "기존 요청: 재전송하지 않음");
            }
            if (!CanExecute || !approved.TryGetValue(requestKey, out var approval) || approval != line || DateTimeOffset.UtcNow - line.MarketTime > TimeSpan.FromMinutes(5))
                throw new SteamException(SteamFailure.Disabled);
            var before = await orders.RefreshAsync(cancellationToken);
            var current=line.Asset;
            if(store.Commands().Any(x=>x.Account==account&&x.Kind=="sell"&&x.Line.Asset.Key==line.Asset.Key&&x.State is SaleState.Unknown or SaleState.Submitting))
            {
                var provider=inventory.AppId==line.Asset.Key.AppId&&inventory.ContextId==line.Asset.Key.ContextId?inventory:new SteamInventoryProvider(http,auth,line.Asset.Key.AppId,line.Asset.Key.ContextId,line.Asset.Game);
                current=(await provider.GetAsync(cancellationToken)).SingleOrDefault(x=>x.Key==line.Asset.Key)??throw new SteamException(SteamFailure.StaleReview);
                if(current!=line.Asset)throw new SteamException(SteamFailure.StaleReview);
            }
            ValidateServerQuantity(current,line.Quantity,before);
            var intent = new SteamCommandRecord(requestKey, auth.SteamId!, line, "sell", SaleState.Submitting, SentAt: DateTimeOffset.UtcNow, BeforeListingIds: before.Select(x=>x.ListingId).Concat(orders.Presence.Where(x=>x.ListingId is not null).Select(x=>x.ListingId!)).Distinct(StringComparer.Ordinal).ToArray());
            store.SaveCommand(intent); // Flush local intent before network; server does not accept our idempotency key.
            try
            {
                using var request = SteamHttp.Form(new("https://steamcommunity.com/market/sellitem/"), new Dictionary<string, string>
                {
                    ["sessionid"] = auth.SessionId!, ["appid"] = line.Asset.Key.AppId.ToString(CultureInfo.InvariantCulture), ["contextid"] = line.Asset.Key.ContextId,
                    ["assetid"] = line.Asset.Key.AssetId, ["amount"] = line.Quantity.ToString(CultureInfo.InvariantCulture),
                    ["price"] = SteamContract.ToWire(line.UnitQuote.SellerReceives, line.Currency).ToString(CultureInfo.InvariantCulture)
                });
                request.Headers.Referrer=new("https://steamcommunity.com/profiles/"+auth.SteamId+"/inventory/");
                sendStarted=true; // From this boundary onward a lost outcome must remain Unknown.
                var body = await http.SendAsync(request, cancellationToken);
                var response = SteamContract.Sell(body);
                var conflict = SteamContract.Parse(body, root =>
                    root.TryGetProperty("price", out var serverPrice) && SteamContract.Integer(serverPrice) != new Money(line.UnitQuote.SellerReceives, line.Currency).Wire ||
                    root.TryGetProperty("fee", out var serverFee) && SteamContract.Integer(serverFee) != new Money(line.UnitQuote.Fees, line.Currency).Wire);
                if (conflict) {DiagnosticHub.Emit(DiagnosticEvent.FeeValidated,DiagnosticArea.Sales,DiagnosticOutcome.Unknown,LogLevel.Error,DiagnosticError.Validation,reason:"FieldsMismatch",currency:line.Currency,quote:line.UnitQuote,requirement:"D10");response = response with{State=SaleState.Unknown,ListingId=null,Approval=SteamApprovalChannel.None};}
                if (response.ListingId is not null && ((intent.BeforeListingIds??[]).Contains(response.ListingId)||store.Commands().Any(x => x.Kind == "sell" && x.RequestKey != requestKey && x.ListingId == response.ListingId)))
                    response = response with{State=SaleState.Unknown,ListingId=null,Approval=SteamApprovalChannel.None};
                if (response.State == SaleState.Unknown) http.Stop(SteamFailure.ContractChanged);
                store.SaveCommand(intent with { State = response.State, ListingId = response.ListingId, Accepted = response.State != SaleState.Unknown,Approval=response.Approval });
                return new(line.LineId, response.State, response.State == SaleState.Unknown?"응답 불일치 또는 증거 부족 · 재전송 금지":response.State == SaleState.AwaitingSteamGuard ? SteamContract.ApprovalHint(response.Approval) : "접수 응답: 서버 게시 확인 필요");
            }
            catch (Exception error) when (error is SteamException or OperationCanceledException or HttpRequestException)
            {
                http.Stop(error is SteamException steam ? steam.Code : SteamFailure.Transport);
                store.SaveCommand(intent with { State = SaleState.Unknown });
                return new(line.LineId, SaleState.Unknown, error is SteamException {HttpStatus:{} status}?$"Steam 판매 요청 HTTP {status} · 승인 대기 응답 없음 · 결과 불명, 자동 재전송 금지":"Steam 응답 불명/접근 중단: 자동 재전송 금지");
            }
        }
        catch(SteamException error) when(!sendStarted){error.RequestNotSent=true;throw;}
        catch(OperationCanceledException error) when(!sendStarted){throw new UnsentSteamCancellation(error,cancellationToken);}
        finally { if(entered)mutations.Release(); }
    }
    public async Task<LineResult> ReconcileAsync(SaleLineSnapshot line, string requestKey, CancellationToken cancellationToken)
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Sales,"F09");
        var command = store.ReadCommand(requestKey);
        if (!CanReconcile || command is null || command.Account != auth.SteamId || command.Line != line || command.Kind != "sell")
            return new(line.LineId, SaleState.Unknown, "동일 계정/요청 증거 없음");
        if (store.IsReleased(requestKey)) return new(line.LineId,SaleState.Reconciled,"취소 및 동일 자산 반환 증거로 원래 등록 예약 해제");
        if(command.State==SaleState.Sold)return new(line.LineId,SaleState.Sold,"판매 완료 · 이전 거래내역 대조 기록 · 새 판매 요청 없음");
        if (command.ListingId is null)
        {
            var report = await InspectAsync(requestKey, cancellationToken);
            var observed=report.ObservedAssetState is {} actual?$"현재 Steam 동일 자산 상태: {ObservedLabel(actual)}. 원요청 귀속은 미확인. ":"현재 동일 자산 게시 증거 없음. ";
            var pending=report.ObservedAssetState==SaleState.AwaitingSteamGuard||orders.Presence.Any(x=>x.Asset==line.Asset.Key&&x.State==SaleState.AwaitingSteamGuard);
            var retainedReceipt=command.State==SaleState.AwaitingSteamGuard&&(pending||report.ObservedAssetState==SaleState.Listed);
            if(!pending&&report.ObservedAssetState is null&&await ObserveSoldAsync(command,cancellationToken) is {} sold)return sold;
            var hint=report.ObservedAssetState==SaleState.Listed?"현재 관측 항목에 추가 승인이 필요하다고 판정하지 않습니다. 원래 요청 기록은 보존합니다.":pending?SteamContract.ApprovalHint(command.Approval):"현재 승인 대기도 확인하지 못했습니다. 즉시 체결·취소·기타 처리 여부는 거래내역 확인이 필요합니다.";
            return new(line.LineId,retainedReceipt?SaleState.AwaitingSteamGuard:SaleState.Unknown,$"{observed}{hint} · 서버 등록 ID 없음 · 대조 {report.Verdict}: 자동 게시 확정/재전송 금지");
        }
        var listing = (await orders.RefreshAsync(cancellationToken)).SingleOrDefault(x => x.ListingId == command.ListingId);
        if(listing is null&&command.State==SaleState.AwaitingSteamGuard&&orders.Presence.Any(x=>x.Asset==line.Asset.Key&&x.State==SaleState.AwaitingSteamGuard))
            return new(line.LineId,SaleState.AwaitingSteamGuard,"이전 접수의 승인 대기 유지 · 현재 같은 자산이 승인 대기 목록에 관측됐으나 등록 ID·금액은 미확인입니다. 공식 Steam 앱/이메일에서 확인하고 다시 조회하세요. 게시 완료가 아닙니다.");
        if(listing is null)
        {
            if(await ObserveSoldAsync(command,cancellationToken) is {} sold)return sold;
            DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Sales,DiagnosticOutcome.Unknown,LogLevel.Warning,DiagnosticError.EvidenceMissing,endpoint:"Listings",reason:"ListingNoLongerPresent",requirement:"F09");
            return new(line.LineId,SaleState.Unknown,"현재 판매·승인 대기 목록에 등록 ID가 없습니다. 즉시 체결·취소·기타 처리 여부는 거래내역 확인이 필요합니다. 판매완료를 추측하거나 자동 재전송하지 않습니다.");
        }
        if (listing.HashName != line.Asset.MarketHashName || listing.Asset != line.Asset.Key || listing.Quantity != line.Quantity ||
            listing.Currency != line.Currency || listing.SellerReceives != line.UnitQuote.SellerReceives || listing.Fees != line.UnitQuote.Fees)
            return new(line.LineId, SaleState.Unknown, "목록 부재/불일치: 판매/취소/비표시 구분 불가");
        store.SaveCommand(command with { State = listing.State });
        return new(line.LineId, listing.State, listing.State==SaleState.AwaitingSteamGuard?SteamContract.ApprovalHint(command.Approval)+" · 서버 등록 ID 대조":"서버 listingid·가격·수량 대조 결과");
    }
    private async Task<LineResult?> ObserveSoldAsync(SteamCommandRecord command,CancellationToken token)
    {
        SteamHistorySale? sale;
        try{sale=await saleHistory.FindAsync(command,token);}
        catch(SteamException error){SteamDiagnostics.Failure(error,"MarketHistory");return null;}
        if(sale is null)return null;
        var attributed=command.ListingId is not null;
        DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Sales,attributed?DiagnosticOutcome.Reconciled:DiagnosticOutcome.Unknown,endpoint:"MarketHistory",reason:attributed?"HistorySaleMatched":"HistoryAssetSaleObserved",to:attributed?"Sold":"Unknown",requirement:"F09");
        if(attributed)store.SaveCommand(command with{State=SaleState.Sold});
        return new(command.Line.LineId,attributed?SaleState.Sold:SaleState.Unknown,(attributed?"판매 완료 · 거래내역의 등록 ID·자산·수량·통화·실수령·시각 일치":"현재 동일 자산의 판매 체결 관측 · 원래 응답에 등록 ID가 없어 원요청 귀속은 미확인 · 재전송 금지")+(sale.Held?" · 판매대금은 Steam 보류 상태입니다.":" · 새 판매 요청 없음"));
    }
    public async Task<LineResult> CancelAsync(SteamListing reviewed, string requestKey, CancellationToken token = default)
    {
        using var diagnosticScope=DiagnosticHub.Scope(item:requestKey);
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.CancelRequested,DiagnosticArea.Management,"B08");
        if(auth.SteamId is not {} account)throw new SteamException(SteamFailure.Disabled);
        using var assetOperation=store.AssetOperations.TryClaim(account,reviewed.Asset.AssetId);
        if(assetOperation is null)
        {
            DiagnosticHub.Emit(DiagnosticEvent.CancelResult,DiagnosticArea.Management,DiagnosticOutcome.Blocked,reason:"ConcurrentAssetOperation",to:SaleState.Failed.ToString(),requirement:"B08");
            return new(reviewed.ListingId,SaleState.Failed,"같은 자산의 다른 작업이 처리 중입니다. 이 취소 요청은 전송하지 않았습니다.");
        }
        await mutations.WaitAsync(token);
        try
        {
            if (!CanCancel) throw new SteamException(SteamFailure.Disabled);
            if(reviewed.State!=SaleState.Listed)throw new SteamException(SteamFailure.Unsupported);
            var previous = store.ReadCommand(requestKey);
            if (previous is not null) return new(reviewed.ListingId, SaleState.Unknown, "기존 취소 의도: 재전송 금지");
            if (store.Commands().Any(x => x.Kind == "cancel" && x.ListingId == reviewed.ListingId)) return new(reviewed.ListingId, SaleState.Unknown, "다른 키의 취소 의도 존재");
            var original = store.Commands().SingleOrDefault(x => x.Kind == "sell" && x.ListingId == reviewed.ListingId);
            var current = (await orders.RefreshAsync(token)).SingleOrDefault(x => x.ListingId == reviewed.ListingId);
            if (current != reviewed) throw new SteamException(SteamFailure.Restricted);
            var returnKey = original?.Line.Asset.Key ?? reviewed.Asset;
            // Cancel by listing ID needs no price or inventory permission. Missing before-state never proves a return.
            var available = original is not null&&!http.Access.CancellationOnly?
                (await new SteamInventoryProvider(http,auth,returnKey.AppId,returnKey.ContextId,original.Line.Asset.Game).GetAsync(token)).SingleOrDefault(x => x.Key == returnKey)?.Quantity??0:-1;
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
        if (!CanCancel||reviewed.Count==0||reviewed.Any(x=>x.State!=SaleState.Listed)||reviewed.Select(x => x.ListingId).Distinct().Count() != reviewed.Count) throw new SteamException(SteamFailure.Restricted);
        var frozen=reviewed.ToImmutableArray();var current=(await orders.RefreshAsync(token)).ToDictionary(x=>x.ListingId);
        if(frozen.Any(x=>!current.TryGetValue(x.ListingId,out var now)||now!=x))throw new SteamException(SteamFailure.StaleReview);
        var results = new List<LineResult>();
        foreach (var row in frozen)
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
        var returned = (await new SteamInventoryProvider(http,auth,intent.Line.Asset.Key.AppId,intent.Line.Asset.Key.ContextId,intent.Line.Asset.Game).GetAsync(token)).SingleOrDefault(x => x.Key == intent.Line.Asset.Key && x.MarketHashName == intent.Line.Asset.MarketHashName);
        var serverHeld=orders.Presence.Any(x=>x.ListingId==intent.ListingId||x.Asset==intent.Line.Asset.Key);
        var stillObserved=orders.Presence.Any(x=>x.ListingId==intent.ListingId||x.ListingId is null&&x.Asset==intent.Line.Asset.Key);
        var originalSell=store.Commands().SingleOrDefault(x=>x.Kind=="sell"&&x.Account==auth.SteamId&&x.ListingId==intent.ListingId);
        // Stack increases may be unrelated merges. Without a causal cancellation event, do not release them.
        var confirmed = originalSell is not null && !originalSell.Line.Asset.Stackable && intent.Line.Quantity==1 && current is null && !serverHeld && intent.Accepted && returned is not null && !returned.Stackable && returned.Quantity==1 && intent.AvailableBefore==0;
        var state = confirmed ? SaleState.Reconciled : current is null&&!stillObserved ? SaleState.Unknown : SaleState.Submitted;
        if (confirmed)
        {
            store.SaveReturn(new(requestKey, originalSell!.RequestKey, returned!.Key, intent.Line.Quantity, DateTimeOffset.UtcNow));
        }
        DiagnosticHub.Emit(DiagnosticEvent.CancelResult,DiagnosticArea.Management,confirmed?DiagnosticOutcome.Reconciled:DiagnosticOutcome.Unknown,from:intent.State.ToString(),to:state.ToString(),reason:confirmed?"Committed":"NoCausalEvidence",requirement:"B09");
        store.SaveCommand(intent with { State = state });
        return new(intent.Line.LineId, state, confirmed ? "취소 접수·목록 부재·동일 단일 자산 반환 대조 완료" : serverHeld ? "서버가 해당 자산을 계속 보유하거나 승인 대기로 표시합니다. 상세 가격이 없어도 취소 완료/반환을 확정하지 않습니다." : current is null ? "목록 부재/스택 증가만으로 취소 인과관계 확인 불가: 재등록 차단" : "목록에 아직 존재: 취소 완료 아님");
    }
    public async Task<SteamEvidenceReport> InspectAsync(string requestKey, CancellationToken token = default)
    {
        using var diagnosticScope=DiagnosticHub.Scope(item:requestKey);
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Management,"F06");
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
        var observed=rows.Where(x=>x.Asset==line.Asset.Key&&x.HashName==line.Asset.MarketHashName&&x.Quantity==line.Quantity&&x.Currency==line.Currency&&x.SellerReceives==line.UnitQuote.SellerReceives&&x.Fees==line.UnitQuote.Fees).ToArray();
        var report = new SteamEvidenceReport(requestKey, DateTimeOffset.UtcNow, verdict, candidates){ObservedAssetState=observed.Length==1?observed[0].State:null,ObservedListingId=observed.Length==1?observed[0].ListingId:null}; store.SaveEvidence(report); DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Management,verdict==SteamEvidenceReason.ServerIdAndFields?DiagnosticOutcome.Reconciled:DiagnosticOutcome.Unknown,reason:verdict.ToString(),count:candidates.Length,requirement:"F06");
        if(report.ObservedAssetState is {} state)DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Management,DiagnosticOutcome.Completed,to:state.ToString(),reason:"AssetStateObserved",requirement:"F09");return report;
    }
    public static string ObservedLabel(SaleState state)=>state switch{SaleState.Listed=>"판매 중",SaleState.AwaitingSteamGuard=>"Steam 승인 대기",SaleState.Submitted=>"서버 보류",_=>"상태 미확인"};
    public async Task<SteamRepriceIntent> RepriceAsync(SteamListing listing, long buyer, string key, CancellationToken token = default,string? reviewedOrderbook=null)
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.Reprice,DiagnosticArea.Management,"B10");
        await repricing.WaitAsync(token);
        try { return await RepriceCoreAsync(listing,buyer,key,token,reviewedOrderbook); }
        finally { repricing.Release(); }
    }
    // Explicit NEW approval, never mutate or replay the previous approval. Reuse only proven cancellation.
    public async Task<SteamRepriceIntent> ReviseRepriceAsync(string previousKey,long buyer,string newKey,CancellationToken token=default,string? reviewedOrderbook=null)
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.Reprice,DiagnosticArea.Management,"B10");
        await repricing.WaitAsync(token);
        try
        {
            if(!CanExecute)throw new SteamException(SteamFailure.Disabled);
            var old=store.Reprice(previousKey)??throw new SteamException(SteamFailure.Restricted);
            if(old.Account!=auth.SteamId||old.ReplacementKey is not null||old.State is SteamRepriceState.Superseded or SteamRepriceState.Unknown||store.Reprice(newKey)is not null)throw new SteamException(SteamFailure.Restricted);
            var cancelKey=old.CancellationKey??old.Key+":cancel";
            if((await ReconcileCancellationAsync(cancelKey,token)).State!=SaleState.Reconciled)throw new SteamException(SteamFailure.Restricted);
            var fees=await SteamFees.FetchResolvedAsync(http,auth,old.Listing.Asset.AppId,token);
            if(fees.Currency!=old.Listing.Currency)throw new SteamException(SteamFailure.Restricted);
            _=fees.FromBuyer(buyer);
            if(executionPolicy.UserInitiated&&reviewedOrderbook is null)throw new SteamException(SteamFailure.StaleReview);
            var next=new SteamRepriceIntent(newKey,old.Account,old.Listing,buyer,DateTimeOffset.UtcNow,SteamRepriceState.AwaitingReturn,CancellationKey:cancelKey){OrderbookDigest=reviewedOrderbook};
            store.SupersedeReprice(old,next);
            return await RepriceCoreAsync(next.Listing,buyer,newKey,token);
        }
        finally{repricing.Release();}
    }
    private async Task<SteamRepriceIntent> RepriceCoreAsync(SteamListing listing, long buyer, string key, CancellationToken token,string? reviewedOrderbook=null)
    {
        if (!CanExecute) throw new SteamException(SteamFailure.Disabled);
        var intent = store.Reprice(key);
        var resumed=intent is not null;
        if(executionPolicy.UserInitiated&&store.ReadCommand(intent?.CancellationKey??key+":cancel")is null&&
            !store.Commands().Any(x=>x.Kind=="sell"&&x.Account==auth.SteamId&&x.ListingId==listing.ListingId&&!x.Line.Asset.Stackable&&x.Line.Quantity==1))
            throw new SteamException(SteamFailure.Unsupported){Endpoint="RepriceReturn"};
        if (intent is null)
        {
            if(executionPolicy.UserInitiated&&reviewedOrderbook is null)throw new SteamException(SteamFailure.StaleReview);
            if(reviewedOrderbook is not null)
            {
                var referenceAsset=new InventoryAsset(listing.Asset,"Steam",listing.HashName,listing.HashName,listing.Quantity,listing.Quantity>1,true,false);
                var referencePrice=await prices.RefreshAsync(referenceAsset,token);
                if(OrderbookFingerprint.Of(referencePrice)!=reviewedOrderbook)throw new SteamException(SteamFailure.StaleReview);
            }
            if (store.Reprices().Any(x=>x.Account==auth.SteamId&&x.Listing.ListingId==listing.ListingId)) throw new SteamException(SteamFailure.Restricted);
            var schedule = await SteamFees.FetchResolvedAsync(http,auth, listing.Asset.AppId, token); if (listing.Currency != schedule.Currency) throw new SteamException(SteamFailure.Restricted);
            _ = schedule.FromBuyer(buyer);
            intent = new(key, auth.SteamId!, listing, buyer, DateTimeOffset.UtcNow, SteamRepriceState.AwaitingCancellation){OrderbookDigest=reviewedOrderbook}; store.SaveReprice(intent);
        }
        if (intent.Account != auth.SteamId || intent.Listing != listing || intent.BuyerPrice != buyer) throw new SteamException(SteamFailure.Restricted);
        if (intent.ReplacementKey is not null) return intent; // Never submit replacement twice after restart/timeout.
        if(intent.State==SteamRepriceState.Superseded||intent.SupersededBy is not null)throw new SteamException(SteamFailure.StaleReview);
        if(intent.CreatedAt>DateTimeOffset.UtcNow.AddSeconds(5)||DateTimeOffset.UtcNow-intent.CreatedAt>TimeSpan.FromMinutes(5))throw new SteamException(SteamFailure.StaleReview);
        var cancelKey = intent.CancellationKey??key + ":cancel";
        if(resumed&&store.ReadCommand(cancelKey) is null&&intent.OrderbookDigest is {} beforeCancel)
        {
            var referenceAsset=new InventoryAsset(listing.Asset,"Steam",listing.HashName,listing.HashName,listing.Quantity,listing.Quantity>1,true,false);
            if(OrderbookFingerprint.Of(await prices.RefreshAsync(referenceAsset,token))!=beforeCancel)throw new SteamException(SteamFailure.StaleReview);
        }
        if (store.ReadCommand(cancelKey) is null) await CancelAsync(listing, cancelKey, token);
        var status = await ReconcileCancellationAsync(cancelKey, token);
        if (status.State != SaleState.Reconciled) { intent = intent with { State = SteamRepriceState.AwaitingReturn }; store.SaveReprice(intent); return intent; }
        if (DateTimeOffset.UtcNow - intent.CreatedAt > TimeSpan.FromMinutes(5)) throw new SteamException(SteamFailure.StaleReview);
        var proof = store.Return(cancelKey)!; var asset = (await new SteamInventoryProvider(http,auth,proof.ReturnedAsset.AppId,proof.ReturnedAsset.ContextId,intent.Listing.HashName).GetAsync(token)).Single(x => x.Key == proof.ReturnedAsset);
        var fees = await SteamFees.FetchResolvedAsync(http,auth, asset.Key.AppId, token); var price = await prices.RefreshAsync(asset, token);
        if(executionPolicy.UserInitiated&&intent.OrderbookDigest is null||intent.OrderbookDigest is {} approvedBook&&OrderbookFingerprint.Of(price)!=approvedBook)throw new SteamException(SteamFailure.StaleReview);
        var replacement = new SaleLineSnapshot("replacement", asset, listing.Quantity, fees.Currency, fees.FromBuyer(buyer), price.CapturedAt){OrderbookDigest=OrderbookFingerprint.Of(price)};
        var batch = new SaleBatchSnapshot(Guid.NewGuid(), auth.SteamId!, DateTimeOffset.UtcNow, [replacement]);
        await ValidateBatchAsync(batch, token); var replacementKey = SaleBatchCoordinator.RequestKey(batch, replacement);
        intent = intent with { State = SteamRepriceState.Submitting, ReplacementKey = replacementKey }; store.SaveReprice(intent);
        var result = await SubmitAsync(replacement, replacementKey, token);
        intent = intent with { State = result.State == SaleState.Unknown ? SteamRepriceState.Unknown : SteamRepriceState.Submitted }; store.SaveReprice(intent); return intent;
    }
}
