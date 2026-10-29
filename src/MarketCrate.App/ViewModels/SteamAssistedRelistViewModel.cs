using System.Collections.Immutable;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.App.ViewModels;

public sealed partial class SteamWorkspaceViewModel
{
    private SteamRelistPlan? relistReview;
    private string reviewContextText="";
    public string ReviewContextText{get=>reviewContextText;private set=>Set(ref reviewContextText,value);}
    public AsyncCommand CancelForNewReview{get;private set;}=null!;
    public AsyncCommand ReviewCancelledTargets{get;private set;}=null!;
    private void InitializeAssistedRelist()
    {
        CancelForNewReview=new(CancelForNewReviewAsync,Error,()=>!Busy&&CanRead&&CanTrade&&ManagementOpen&&managementReprice&&management.Length>0&&management.All(x=>x.Listing.Quantity==1));
        ReviewCancelledTargets=new(ReviewCancelledTargetsAsync,Error,()=>!Busy&&CanRead&&!AnyReview);
    }
    // Explicit cancellation approval. Never execute a sale from the previous listing approval.
    public Task CancelForNewReviewAsync()=>Run(async token=>
    {
        RequireConnection();if(!CanTrade||!ManagementOpen||!managementReprice||management.Length==0||management.Any(x=>x.Listing.Quantity!=1))throw new SteamException(SteamFailure.Disabled);
        var plan=new SteamRelistPlan(Guid.NewGuid().ToString("N"),workspace!.Authentication.SteamId!,DateTimeOffset.UtcNow,management.Select(x=>x.Listing).ToImmutableArray());
        workspace.Store.SaveRelistPlan(plan); // Before the first request; request keys survive restart.
        var results=await workspace.Executor.CancelBatchAsync(plan.Listings,plan.Key,token);
        Results.Clear();foreach(var result in results)Results.Add(result);
        ManagementOpen=false;WorkPage=2;RefreshRequests();
        ListingPriceStatus=results.Count==plan.Listings.Length&&results.All(x=>x.State==SaleState.Submitted)?"취소 요청 접수 · '취소 후 새 대상 검토'로 보관함을 다시 조회하세요. 반환 확정이나 새 판매 완료가 아닙니다.":"취소 일부 미완료/불명 · 새 등록을 차단했습니다. 작업 이력에서 상태를 대조하세요. 자동 재전송하지 않습니다.";
        Status=ListingPriceStatus;
    });
    // Same product lookup as comparable helpers, but a NEW actual-asset review instead of inferred return.
    public Task ReviewCancelledTargetsAsync()=>Run(async token=>
    {
        RequireConnection();if(AnyReview)throw new SteamException(SteamFailure.Disabled);var account=workspace!.Authentication.SteamId!;
        var plan=workspace.Store.RelistPlans(account).Where(x=>x.SubmissionBatch is null).OrderByDescending(x=>x.CreatedAt).FirstOrDefault();
        if(plan is null){ListingPriceStatus="새 대상 검토를 기다리는 취소 계획이 없습니다.";return;}
        var current=await workspace.Orders.RefreshAsync(token);
        if(plan.Listings.Any(x=>current.Any(now=>now.ListingId==x.ListingId)||workspace.Store.ReadCommand(plan.Key+":"+x.ListingId) is not {Accepted:true,Kind:"cancel"} command||command.Account!=account))
        {ListingPriceStatus="모든 취소의 서버 접수·목록 부재를 확인하지 못했습니다. 불명 요청은 재전송하지 않으며 새 등록도 시작하지 않습니다.";return;}
        var assets=new List<InventoryAsset>();var imageRefs=new Dictionary<AssetKey,ItemImageReference>();
        foreach(var scope in plan.Listings.Select(x=>(x.Asset.AppId,x.Asset.ContextId)).Distinct())
        {
            OperationLabel="취소 후 실제 보관함 재조회 중";
            var provider=workspace.InventoryFor(scope.AppId,scope.ContextId,"Steam");
            var inventory=await provider.GetAsync(token);assets.AddRange(inventory);
            foreach(var key in assetPublisherFees.Keys.Where(x=>x.AppId==scope.AppId&&x.ContextId==scope.ContextId).ToArray())assetPublisherFees.Remove(key);
            foreach(var asset in inventory)if(provider.PublisherFeeBasisPoints(asset.Key) is {} rate)assetPublisherFees[asset.Key]=rate;
            foreach(var image in provider.Images)imageRefs[image.Key]=image.Value;
        }
        var available=workspace.Store.AvailableAll(assets);var selected=new List<(InventoryAsset Asset,PriceSnapshot Price)>();
        foreach(var group in plan.Listings.GroupBy(x=>(x.Asset.AppId,x.Asset.ContextId,x.HashName)))
        {
            var candidates=assets.Where(x=>x.Key.AppId==group.Key.AppId&&x.Key.ContextId==group.Key.ContextId&&x.MarketHashName==group.Key.HashName&&x.Marketable&&!x.Locked&&!x.Stackable&&x.Quantity==1&&available[x.Key]>0).OrderBy(x=>x.Key.AssetId,StringComparer.Ordinal).Take(group.Count()).ToArray();
            if(candidates.Length!=group.Count()){ListingPriceStatus="새 등록에 필요한 판매 가능 개별 자산이 부족합니다. 원래 취소의 반환을 추측하거나 예약을 해제하지 않았습니다.";return;}
            var schedule=await workspace.FeesAsync(group.Key.AppId,token);gameFees[group.Key.AppId]=schedule;
            if(group.Any(x=>x.Currency!=schedule.Currency))throw new SteamException(SteamFailure.Restricted);
            foreach(var asset in candidates)_=FeeFor(asset);
            OperationLabel="새 대상의 즉시거래가능가 갱신 중";var price=await workspace.Prices.RefreshAsync(candidates[0],token);
            if(price.Currency!=schedule.Currency)throw new SteamException(SteamFailure.Restricted);
            selected.AddRange(candidates.Select(x=>(x,price)));
        }
        var allocation=SalePlanner.Build(selected.Select(x=>new SaleSelection(x.Asset,x.Price,1)),PriceBasis.Immediate);
        if(allocation.UnpricedQuantity>0){ListingPriceStatus=$"구매주문 잔량 {allocation.UnpricedQuantity}개 부족 · 새 판매 검토를 만들지 않았습니다.";return;}
        token.ThrowIfCancellationRequested();if(!Connected||workspace.Authentication.SteamId!=account)throw new SteamException(SteamFailure.Authentication);
        // Authentication may remain valid while a listing reappears. Recheck before presenting new targets.
        var fresh=await workspace.Orders.RefreshAsync(token);
        if(plan.Listings.Any(x=>fresh.Any(now=>now.ListingId==x.ListingId)))throw new SteamException(SteamFailure.StaleReview);
        fees=gameFees[selected[0].Asset.Key.AppId];pricingMode=0;Raise(nameof(PricingMode));
        ReviewRows.Clear();foreach(var line in allocation.Lines)ReviewRows.Add(new(line.Selection.Asset,1,line.Allocation.BuyerPays,line.Selection.Price,FeeFor(line.Selection.Asset),Changed,PriceBasis.Immediate){Image=imageRefs.GetValueOrDefault(line.Selection.Asset.Key),ImageOperation=DiagnosticHub.Context?.OperationId});
        relistReview=plan;ReviewContextText="취소 후 보관함에서 찾은 같은 상품을 새 판매 대상으로 제안합니다. 원래 아이템의 반환 인과관계는 미확인입니다. 기존 보유분일 수 있으므로 아래 실제 자산 ID·수량·새 가격을 확인하세요. 전체 판매등록 한 번 승인 시 이 새 대상 전체를 등록합니다.";
        ReviewLiquidityText="현재 구매주문 기준 · 실제 즉시 체결 보장 없음 · 이전 승인 가격은 사용하지 않음";Raise(nameof(ReviewLiquidityText));Raise(nameof(CurrencyText));
        ReviewOpen=true;Status="새 실제 자산 검토 · 아직 판매 요청을 보내지 않았습니다.";
        DiagnosticHub.Emit(DiagnosticEvent.ReviewOpened,DiagnosticArea.Management,count:ReviewRows.Count,requirement:"B10",reason:"NewAssetReview");
    });
}
