using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;

namespace MarketCrate.App.ViewModels;

public sealed class SteamGameScope(int appId,string contextId,string name):Observable
{
    public string Key=>$"{AppId}/{ContextId}";
    public int AppId{get;}=appId;public string ContextId{get;}=contextId;public string Name{get;}=name;
    public ItemImageReference? Image{get;private set;}
    public InventoryScopeState State{get;private set;}
    public int? MarketableItems{get;private set;}
    public string Summary=>$"{Name} · App {AppId}/context {ContextId} · {StateText} · 판매 가능 {(MarketableItems?.ToString()??"미조회")}종";
    public string StateText=>State switch{InventoryScopeState.NotQueried=>"미조회 (보유 미확인)",InventoryScopeState.Loaded=>"조회 완료",InventoryScopeState.Empty=>"빈 보관함",InventoryScopeState.PrivateOrDenied=>"비공개/접근 거부 · 원인 미확인",InventoryScopeState.Unsupported=>"요청한 game/context 경로 미지원",InventoryScopeState.Partial=>"부분 조회 실패",_=>"조회 실패"};
    public void Update(InventoryScopeState state,int? count,ItemImageReference? image=null){var previous=State;State=state;MarketableItems=count;Image=image;DiagnosticHub.Emit(DiagnosticEvent.InventoryScope,DiagnosticArea.Data,state is InventoryScopeState.Partial or InventoryScopeState.Failed?DiagnosticOutcome.Failed:state is InventoryScopeState.Unsupported or InventoryScopeState.PrivateOrDenied?DiagnosticOutcome.Blocked:DiagnosticOutcome.Succeeded,requirement:"C02",from:previous.ToString(),to:state.ToString(),count:count);Raise(nameof(Summary));Raise(nameof(Image));Raise(nameof(StateText));Raise(nameof(State));Raise(nameof(MarketableItems));}
    public override string ToString()=>Summary;
}
public sealed partial class SteamWorkspaceViewModel
{
    private readonly List<InventoryScope> hiddenSuggestions=[];
    private readonly bool catalogTestDefaults=false;
    private static bool OldSuggestion(InventoryScope x)=>x.ContextId=="2"&&(x.AppId==730&&x.Name=="Counter-Strike 2"||x.AppId==570&&x.Name=="Dota 2"||x.AppId==440&&x.Name=="Team Fortress 2");
    private static string CatalogName(int appId,string fallback)=>appId switch{753=>"Steam 커뮤니티",730=>"Counter-Strike 2",570=>"Dota 2",440=>"Team Fortress 2",_=>fallback};
    public string CatalogNotice=>"보관함/판매목록에서 확인되거나 직접 추가한 경로입니다. 게임 구매·보유 여부를 뜻하지 않습니다."+(hiddenSuggestions.Count>0?$" 이전 미확인 추천 {hiddenSuggestions.Count}개는 숨겼으며 저장 자료는 보존했습니다. 필요하면 직접 추가하세요.":"");
    private string appInput="",contextInput="",nameInput="";
    public string AppInput{get=>appInput;set=>Set(ref appInput,value);}
    public string ContextInput{get=>contextInput;set=>Set(ref contextInput,value);}
    public string NameInput{get=>nameInput;set=>Set(ref nameInput,value);}
    public Command AddScope{get;private set;}=null!;public Command RemoveScope{get;private set;}=null!;
    public string ScopeSummary=>Game.Summary+" · 비공개 여부/게임 지원 여부는 명시적 서버 증거가 없으면 단정하지 않습니다.";
    public AsyncCommand ListingMetrics{get;private set;}=null!;
    private void InitializeCatalog()
    {
        ListingMetrics=new(ListingMetricsAsync,Error,()=>CanRead&&!Busy&&!AnyReview&&Listings.Any(x=>x.Selected));
        AddScope=new(()=>{try
        {
            var scope=new InventoryScope(int.Parse(AppInput,System.Globalization.CultureInfo.InvariantCulture),ContextInput,NameInput);scope.Validate();
            if(Games.Count>=100||Games.Any(x=>x.AppId==scope.AppId&&x.ContextId==scope.ContextId))throw new ArgumentException("CatalogDuplicate");
            var next=new SteamGameScope(scope.AppId,scope.ContextId,scope.Name);Games.Add(next);try{SaveCatalog();}catch{Games.Remove(next);throw;}hiddenSuggestions.RemoveAll(x=>x.AppId==scope.AppId&&x.ContextId==scope.ContextId);Raise(nameof(CatalogNotice));Game=next;Status="게임/context 추가 완료 · 직접 갱신할 때만 조회합니다.";
        }catch(Exception error){Error(error);}},()=>CanRead&&!Busy&&!AnyReview);
        RemoveScope=new(()=>{try{var old=Game;var index=Games.IndexOf(old);Games.Remove(old);try{SaveCatalog();}catch{Games.Insert(index,old);throw;}Game=Games[0];Status="게임/context 목록에서 제거 · 거래 저널/예약은 보존합니다.";}catch(Exception error){Error(error);}},()=>CanRead&&!Busy&&!AnyReview&&Games.Count>1);
    }
    private Task ListingMetricsAsync()=>Run(async token=>
    {
        RequireConnection();var revision=priceRevision;var mode=PricingMode;var window=IndicatorWindow;var groups=Listings.Where(x=>x.Selected).GroupBy(x=>(x.Listing.Asset.AppId,x.Listing.HashName)).ToArray();
        if(groups.Length>10)throw new ArgumentException("MetricLimit");
        foreach(var group in groups)
        {
            if(mode==1){foreach(var row in group)row.Metric="최근 정확한 평균 이용 불가 · 실제 체결 원자료 없음";continue;}
            var item=group.First().Listing;var asset=new InventoryAsset(item.Asset,"Steam",item.HashName,item.HashName,item.Quantity,item.Quantity>1,true,false);
            var snapshot=mode==2?await workspace!.Prices.RefreshEstimateAsync(asset,window,token):await workspace!.Prices.RefreshAsync(asset,token);
            token.ThrowIfCancellationRequested();if(revision!=priceRevision)return;
            if(snapshot.Currency!=item.Currency)throw new SteamException(SteamFailure.Restricted);
            RecordCheck("Orderbook",snapshot.Bids.Length,snapshot.Currency);
            foreach(var row in group)
            {
                var current=checked(row.Listing.SellerReceives+row.Listing.Fees);
                row.Metric=mode==2?(snapshot.Estimate is {} ev?$"Steam pricehistory · 최근 정확한 평균 이용 불가 · 기간 중앙값 가중 추정 {new Money(ev.Minor,ev.Currency)} · {ev.Buckets}구간 / 서버 거래량 {ev.Volume} · {ev.WindowStart:MM-dd HH:mm}–{ev.WindowEnd:MM-dd HH:mm} UTC · 수집 {ev.CapturedAt:HH:mm:ss}":"해당 기간 표본 부족 · 평균/추정값 없음"):
                    snapshot.Bids.IsEmpty?$"{PriceDisplay.Immediate} · 구매주문 없음":$"{PriceDisplay.Immediate} 최고 구매주문 {new Money(snapshot.Bids[0].BuyerPays,snapshot.Currency)} · 잔량 {snapshot.Bids[0].Quantity}개 · 현재 등록 단가 대비 {PriceDisplay.Difference(snapshot.Bids[0].BuyerPays,current,snapshot.Currency)} · 수집 {snapshot.CapturedAt:HH:mm:ss} UTC · 수량별 분배는 최종 검토 · {PriceDisplay.ImmediateExplanation}";
            }
            if(snapshot.Estimate is {} estimate)RecordCheck("History",estimate.Buckets,estimate.Currency);
        }
    });
    private void SaveCatalog()=>workspace!.Store.SaveCatalog(workspace.Authentication.SteamId!,Games.Select(x=>new InventoryScope(x.AppId,x.ContextId,x.Name){Pinned=true}).Concat(hiddenSuggestions).DistinctBy(x=>(x.AppId,x.ContextId)).ToArray());
    private void RestoreCatalog()
    {
        var scopes=workspace!.Store.ReadCatalog(workspace.Authentication.SteamId!);
        if(scopes.Count>0){Games.Clear();hiddenSuggestions.Clear();foreach(var scope in scopes){if(!catalogTestDefaults&&!scope.Pinned&&OldSuggestion(scope)){hiddenSuggestions.Add(scope);continue;}Games.Add(new(scope.AppId,scope.ContextId,CatalogName(scope.AppId,scope.Name)));}if(Games.Count==0)Games.Add(new(753,"6","Steam 커뮤니티"));game=Games[0];Raise(nameof(Game));Raise(nameof(CatalogNotice));}
        DiagnosticHub.Emit(DiagnosticEvent.GameCatalog,DiagnosticArea.Data,count:Games.Count,requirement:"C02",reason:"Loaded");
    }
    private void ObserveCatalog(IEnumerable<SteamListing> listings)
    {
        if(!Connected)return;var added=false;
        foreach(var group in listings.Select(x=>x.Asset).DistinctBy(x=>(x.AppId,x.ContextId)))
        {
            if(Games.Any(x=>x.AppId==group.AppId&&x.ContextId==group.ContextId)||Games.Count>=100)continue;
            var old=hiddenSuggestions.FirstOrDefault(x=>x.AppId==group.AppId&&x.ContextId==group.ContextId);
            Games.Add(new(group.AppId,group.ContextId,CatalogName(group.AppId,old?.Name??"게임 "+group.AppId+" (이름 미확인)")));hiddenSuggestions.RemoveAll(x=>x.AppId==group.AppId&&x.ContextId==group.ContextId);added=true;
        }
        if(added){SaveCatalog();Raise(nameof(CatalogNotice));DiagnosticHub.Emit(DiagnosticEvent.GameCatalog,DiagnosticArea.Data,count:Games.Count,requirement:"C02",reason:"Observed");}
    }
    private void ResetCatalog()
    {
        Games.Clear();hiddenSuggestions.Clear();
#if MARKETCRATE_DEMO
        if(catalogTestDefaults){Games.Add(new(730,"2","Counter-Strike 2"));Games.Add(new(570,"2","Dota 2"));Games.Add(new(440,"2","Team Fortress 2"));}
        else
#endif
        Games.Add(new(753,"6","Steam 커뮤니티"));
        game=Games[0];Raise(nameof(Game));Raise(nameof(CatalogNotice));
    }
}