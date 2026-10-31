using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.App.ViewModels;

public sealed partial class SteamWorkspaceViewModel
{
    private int listingPricingMode,listingPlanRevision;
    private bool managementReturnSupported=true;
    private DateTimeOffset managementDeadline;
    private string managementEligibility="";
    public string ManagementEligibility{get=>managementEligibility;private set=>Set(ref managementEligibility,value);}
    private string listingPriceStatus="판매 중 항목을 선택하고 재등록 가격을 계산하세요. 계산만으로 취소하지 않습니다.";
    public string[] ListingPricingModes{get;}=[PriceDisplay.Immediate];
    public int ListingPricingMode{get=>listingPricingMode;set{
#if MARKETCRATE_LIVE_ONLY
        value=0;
#endif
        if(value is >=0 and <=1&&Set(ref listingPricingMode,value)){listingPlanRevision++;if(managementReprice)ManagementOpen=false;ListingPriceStatus=value==1?"정확한 평균거래가의 실제 체결 원자료가 없어 평균 기준 자동 재등록은 사용할 수 없습니다. 중앙값을 평균으로 대체하지 않습니다.":"가격 기준 변경 · 다시 계산하고 전체 검토하세요.";PrepareListingReprice?.Refresh();}}}
    public string ListingPriceStatus{get=>listingPriceStatus;private set=>Set(ref listingPriceStatus,value);}
    public AsyncCommand PrepareListingReprice{get;private set;}=null!;
    private void InitializeListingReprice()=>PrepareListingReprice=new(PrepareListingRepriceAsync,Error,()=>CanRead&&!Busy&&!AnyReview&&ListingPricingMode==0&&Listings.Any(x=>x.Selected));
    public Task PrepareListingRepriceAsync()=>Run(async token=>
    {
        RequireConnection();var revision=listingPlanRevision;var rows=Listings.Where(x=>x.Selected).ToArray();
        if(ListingPricingMode!=0)throw new ArgumentException("ExactAverageUnavailable");
        if(rows.Length==0||rows.Length>100||rows.Any(x=>x.Listing.State!=SaleState.Listed||x.Listing.Quantity!=1)||rows.Select(x=>x.Listing.Asset).Distinct().Count()!=rows.Length)
        {ListingPriceStatus="게시 확인된 개별 수량1 항목만 지원합니다. 스택/동일 자산 분할·승인 대기·불명 항목은 취소하지 않았습니다.";return;}
        var groups=rows.GroupBy(x=>(x.Listing.Asset.AppId,x.Listing.HashName)).ToArray();
        if(groups.Length>10){ListingPriceStatus="한 번에 최대10상품의 시세만 조회합니다. 선택 범위를 줄이세요.";return;}
        if(!await RefreshRepriceSelectionAsync(rows,token))return;
        var schedules=new Dictionary<int,SteamFeeSchedule>();var books=new Dictionary<(int,string),PriceSnapshot>();
        foreach(var app in rows.Select(x=>x.Listing.Asset.AppId).Distinct())
        {
            OperationLabel="재등록 수수료 확인 중";FeesRead.Update(SteamReadState.Loading);
            try{schedules[app]=await workspace!.FeesAsync(app,token);token.ThrowIfCancellationRequested();FeesRead.Update(SteamReadState.Ready);}
            catch(OperationCanceledException){FeesRead.Update(SteamReadState.Cancelled);throw;}
            catch(Exception error)
            {
                ReadFailed(FeesRead,error);
                // A listing may fill between the preflight GET and fee-context GET.
                // One independent listing recheck only; never retry a restricted/failed fee request.
                if(error is SteamException{Code:SteamFailure.Unsupported}&&CanReadFeature("Listings")&&!await RefreshRepriceSelectionAsync(rows,token))
                {gameFees.Remove(app);if(Game.AppId==app)fees=null;FeesRead.Update(SteamReadState.Stale);Raise(nameof(CurrencyText));return;}
                ListingPriceStatus="판매 수수료 확인 실패 · 계정 연결의 조회 상태와 문제 해결을 확인하세요. 취소·등록 요청은 보내지 않았습니다.";throw;
            }
        }
        foreach(var group in groups)
        {
            OperationLabel=$"재등록 호가 조회 {books.Count+1}/{groups.Length}";
            var item=group.First().Listing;var asset=ListingAsset(item);
            var price=await workspace!.Prices.RefreshAsync(asset,token);token.ThrowIfCancellationRequested();
            if(price.Currency!=item.Currency||price.Currency!=schedules[item.Asset.AppId].Currency)throw new SteamException(SteamFailure.Restricted);
            books.Add(group.Key,price);
        }
        if(revision!=listingPlanRevision)return;
        if(!rows.Select(x=>x.Listing.ListingId).ToHashSet(StringComparer.Ordinal).SetEquals(Listings.Where(x=>x.Selected).Select(x=>x.Listing.ListingId)))
        {ListingPriceStatus="조회 중 선택이 변경되었습니다. 다시 계산하세요. 취소/등록하지 않았습니다.";return;}
        var plan=SalePlanner.Build(rows.Select(x=>new SaleSelection(ListingAsset(x.Listing),books[(x.Listing.Asset.AppId,x.Listing.HashName)],1)),PriceBasis.Immediate);
        if(plan.UnpricedQuantity>0){ListingPriceStatus=$"구매주문 잔량 부족 {plan.UnpricedQuantity}개 · 전체 검토를 만들지 않았습니다. 수량/선택 범위를 줄이거나 최신 호가를 확인하세요.";return;}
        (SteamListingRow Row,long Price,SteamFeeSchedule Fees,FeeQuote? Baseline)[] proposals;
        try
        {
            proposals=plan.Lines.Select(x=>
            {
                var schedule=schedules[x.Selection.Asset.Key.AppId];var baseline=schedule.FromBuyer(x.Allocation.BuyerPays,!LowerPriceOneStep);
                return(Row:rows.Single(r=>r.Listing.Asset==x.Selection.Asset.Key),Price:LowerPriceOneStep?schedule.LowerOrMinimumBuyerPrice(baseline.BuyerPays).BuyerPays:baseline.BuyerPays,Fees:schedule,Baseline:LowerPriceOneStep?(FeeQuote?)baseline:null);
            }).ToArray();
        }
        catch(ArgumentException){ListingPriceStatus=LowerPriceOneStep?"한 단계 낮은 유효 가격을 계산할 수 없는 항목이 있습니다. 최저 가격·수수료를 확인하고 옵션이나 선택을 변경하세요. 취소·등록하지 않았습니다.":"호가와 유효 가격 증분·수수료가 일치하지 않습니다. 최신 자료를 확인하세요. 취소·등록하지 않았습니다.";return;}
        foreach(var proposal in proposals)_=proposal.Fees.FromBuyer(proposal.Price); // No rounding/substitution of unrepresentable prices.
        foreach(var schedule in schedules)gameFees[schedule.Key]=schedule.Value;
        fees=schedules.GetValueOrDefault(Game.AppId)??schedules.Values.First();Raise(nameof(CurrencyText));
        Bulk(()=>{foreach(var proposal in proposals)proposal.Row.SetProposedPrice(proposal.Price,proposal.Fees,proposal.Baseline);});
        BuildManagement(true);ListingPriceStatus=$"{rows.Length}건 가격별 분배 완료 · 현재 구매주문 기준이며 체결 보장 없음 · 검토 전체를 한 번 승인하면 취소/반환 확인 후 직렬 재등록합니다.";
        DiagnosticHub.Emit(DiagnosticEvent.PricePlan,DiagnosticArea.Management,requirement:"B10",count:rows.Length,reason:"Immediate");
    });
    private async Task<bool> RefreshRepriceSelectionAsync(SteamListingRow[] rows,CancellationToken token)
    {
        OperationLabel="선택한 판매등록의 현재 상태 확인 중";ListingsRead.Update(SteamReadState.Loading);
        SteamListing[] current;
        try{current=(await workspace!.Orders.RefreshAsync(token)).ToArray();token.ThrowIfCancellationRequested();ListingsRead.Update(current.Length==0?SteamReadState.Empty:SteamReadState.Ready);}
        catch(OperationCanceledException){ListingsRead.Update(SteamReadState.Cancelled);throw;}
        catch(Exception error){ReadFailed(ListingsRead,error);throw;}
        if(rows.All(x=>current.Any(now=>now.ListingId==x.Listing.ListingId&&now.Asset==x.Listing.Asset&&now.HashName==x.Listing.HashName&&now.Quantity==x.Listing.Quantity&&now.Currency==x.Listing.Currency&&now.SellerReceives==x.Listing.SellerReceives&&now.Fees==x.Listing.Fees&&now.State==SaleState.Listed)))return true;
        SetListings(current);RefreshRequests();
        ListingPriceStatus="선택한 등록이 사라졌거나 가격·수량·상태가 변경되어 판매목록을 갱신했습니다. 즉시 체결 여부는 거래내역에서 확인하세요. 남은 항목을 확인하고 새로 검토하세요. 이전 선택으로 취소·재등록하지 않았습니다.";
        DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Management,DiagnosticOutcome.Blocked,LogLevel.Warning,DiagnosticError.Validation,endpoint:"Listings",reason:"RepriceSelectionChanged",requirement:"B10",count:rows.Length);
        return false;
    }
    private static InventoryAsset ListingAsset(SteamListing listing)=>new(listing.Asset,"Steam",listing.HashName,listing.HashName,listing.Quantity,listing.Quantity>1,true,false);
    private async Task RefreshRepriceResultsAsync(CancellationToken token,string? only=null)
    {
        var plans=workspace!.Store.Reprices().Where(x=>x.Account==workspace.Authentication.SteamId&&x.ReplacementKey is not null&&(only is null||x.Key==only)).OrderByDescending(x=>x.CreatedAt).Take(10).ToArray();
        for(var index=0;index<plans.Length;index++)
        {
            var plan=plans[index];var command=workspace.Store.ReadCommand(plan.ReplacementKey!);if(command is null)continue;
            OperationLabel=$"재등록 서버 상태 대조 {index+1}/{plans.Length}";
            var result=await workspace.Executor.ReconcileAsync(command.Line,command.RequestKey,token);token.ThrowIfCancellationRequested();
            var id="reprice:"+plan.Key;var old=Results.FirstOrDefault(x=>x.LineId==id);if(old is not null)Results.Remove(old);
            Results.Add(new(id,result.State,plan.Listing.HashName+" · 재등록 서버 대조 · "+SteamStatusText.Label(result.State)));
        }
        DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Management,count:plans.Length,requirement:"F09",reason:"RecoverOnly");
    }
}
