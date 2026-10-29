using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
using Net.Codecrete.QrCodeGenerator;

namespace MarketCrate.App.ViewModels;

public enum SteamLoginState { Disconnected, RequestingQr, AwaitingMobile, Connected, Expired, Denied, Cancelled, NetworkFailure, AccessBlocked, ContractFailure }
// Uses the same real adapters in production and injected synthetic HTTP in contract tests.
public sealed partial class SteamWorkspaceViewModel : Observable, IDisposable
{
    private readonly Func<SteamGameScope, SteamExperimentalWorkspace> factory;
    public string ModeText { get; }
    private SteamExperimentalWorkspace? workspace;
    private CancellationTokenSource? operation;
    private bool busy, riskAccepted, credentialsAccepted, tradeAccepted, reviewOpen, managementOpen, logoutRequested;
    private SteamLoginState state;
    private string status = "Steam 연결 안 됨 · 계정 연결을 시작하세요.", evidence = "요청을 선택하고 수동 대조를 실행하세요.", history = "중간가격 이력 미조회 (정확한 거래 VWAP 아님)";
    private bool[,]? matrix;
    private SteamFeeSchedule? fees;
    private readonly Dictionary<int,SteamFeeSchedule> gameFees=[];
    private SteamAssetRow[] SelectedSteamRows()=>Assets.Concat(inventoryViews.Where(x=>x.Key!=Game.Key).SelectMany(x=>x.Value)).Where(x=>x.Selected).DistinctBy(x=>x.Asset.Key).ToArray();
    private SteamGameScope game;
    private (SteamListing Listing, long Price, string Key)[] management = [];
    public string ReviewLiquidityText{get;private set;}="";
    private bool managementReprice;
    private readonly Dictionary<string,string> managementBooks=[];
    private string? revisionSource;
    private string revisedPrice="";
    public string RevisedPrice {get=>revisedPrice;set=>Set(ref revisedPrice,value);}
    public Command ReviewStoredReprice {get;}
    private int pricingMode,windowIndex,priceRevision;
    public string[] PricingModes{get;}=[PriceDisplay.Immediate,PriceDisplay.UnavailableAverage,PriceDisplay.MedianEstimate];
    public string[] PriceWindows{get;}=["24시간","7일","30일"];
    public int WindowIndex{get=>windowIndex;set{if(value is >=0 and <=2&&Set(ref windowIndex,value))ResetPriceReview();}}
    private TimeSpan IndicatorWindow=>TimeSpan.FromDays(WindowIndex switch{1=>7,2=>30,_=>1});
    public int PricingMode{get=>pricingMode;set{if(value is >=0 and <=2&&Set(ref pricingMode,value))ResetPriceReview();}}
    private void ResetPriceReview(){priceRevision++;DiagnosticHub.Emit(DiagnosticEvent.PriceIndicator,DiagnosticArea.UI,requirement:"D02",count:WindowIndex,reason:PricingMode==2?"MedianEstimate":PricingMode==1?"ExactAverage":"Immediate");ReviewOpen=false;ReviewRows.Clear();foreach(var row in Assets.Concat(inventoryViews.Values.SelectMany(x=>x)).DistinctBy(x=>x.Asset.Key))row.ClearPrice();Status="가격 기준/기간 변경 · 수동 가격 초기화 · 선택 시세 갱신 후 다시 검토하세요.";Changed();}
    public Guid? LastOperationId{get;private set;}
    private bool bulkUpdate,imagesEnabled;
    public bool ImagesEnabled{get=>imagesEnabled;set{Set(ref imagesEnabled,value);Raise(nameof(ImageLoader));}}
    public SteamImageCache? ImageLoader=>ImagesEnabled&&Connected?workspace?.Images:null;
    private readonly Dictionary<AssetKey,(bool Selected,int Quantity)> selections=[];
    private string search="";private bool marketableOnly=true,descending;
    private readonly Dictionary<string,bool> listingSelections=[];
    private readonly Dictionary<string,SteamAssetRow[]> inventoryViews=[];
    public RangeObservableCollection<SteamAssetRow> VisibleAssets {get;}=[];
    public RangeObservableCollection<SteamListingRow> VisibleListings {get;}=[];
    public string Search {get=>search;set{if(Set(ref search,value))Filter();}}
    public bool MarketableOnly {get=>marketableOnly;set{if(Set(ref marketableOnly,value))Filter();}}
    public bool Descending {get=>descending;set{if(Set(ref descending,value))Filter();}}
    public bool EmptyAssets=>VisibleAssets.Count==0;
    public bool EmptyListings=>VisibleListings.Count==0;
    public Command ClearSelection {get;}
    public Command ClearListings {get;}
    public Command RefreshReviewStatus {get;}
    private void RememberSelection(){foreach(var row in Assets)selections[row.Asset.Key]=(row.Selected,row.Quantity);foreach(var row in Listings)listingSelections[row.Listing.ListingId]=row.Selected;}
    private void Filter()
    {
        var matches=Assets.Where(x=>(!MarketableOnly||x.Asset.Marketable)&&x.Name.Contains(Search,StringComparison.OrdinalIgnoreCase));
        VisibleAssets.ReplaceAll(Descending?matches.OrderByDescending(x=>x.Name,StringComparer.OrdinalIgnoreCase):matches.OrderBy(x=>x.Name,StringComparer.OrdinalIgnoreCase));
        VisibleListings.ReplaceAll(Listings.Where(x=>x.Listing.Asset.AppId==Game.AppId&&x.Listing.HashName.Contains(Search,StringComparison.OrdinalIgnoreCase)));
        Raise(nameof(EmptyAssets));Raise(nameof(EmptyListings));
        DiagnosticHub.Emit(DiagnosticEvent.UiSelection,DiagnosticArea.UI,level:LogLevel.Debug,reason:"Filter",count:VisibleAssets.Count,requirement:"C12");
    }
    public SteamWorkspaceViewModel(Func<SteamGameScope, SteamExperimentalWorkspace>? factory = null, bool contractTest = false, bool offlineOnly = false, Action<Uri>? openOfficial = null)
    {
#if MARKETCRATE_LIVE_ONLY
        if(contractTest||offlineOnly||factory is not null)throw new InvalidOperationException("ProductionCompositionOnly");
#endif
        ModeText=offlineOnly?"ISOLATED GUI QA · Mock 전용 · Steam 네트워크 차단":contractTest?"CONTRACT TEST · 합성 서버 · 실제 Steam 요청 없음":"STEAM · 실제 계정 연결 · 서버 응답 기반";
        this.factory = offlineOnly ? (_ => throw new SteamException(SteamFailure.Disabled)) : factory ?? (scope => new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarketCrate", "SteamExperimental"), scope.AppId, scope.ContextId, scope.Name));
        InitializeConnection(contractTest, openOfficial);InitializeTrading();
        game = Games[0];InitializeCatalog();InitializeSelfCheck();InitializeFeeComparison();
        BeginLogin = new(BeginAsync, Error, () => !offlineOnly && !Busy && !Connected && !AnyReview && RiskAccepted && CredentialsAccepted);
        CheckLogin = new(CheckAsync, Error, () => !Busy && LoginState == SteamLoginState.AwaitingMobile);
        Logout = new(LogoutNow);
        Load = new(LoadAsync, Error, () => !Busy && CanReadFeature("Inventory") && !AnyReview);
        RefreshPrices = new(RefreshPricesAsync, Error, () => !Busy && CanRead && !AnyReview && SelectedSteamRows().Length>0);
        SelectAll = new(() => Bulk(()=>{foreach(var row in VisibleAssets)row.Selected=row.CanSelect;}), () => !Busy && !AnyReview);
        RefreshReviewStatus=new(()=>{foreach(var row in ReviewRows)row.RefreshStatus();Changed();DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Pricing,requirement:"D07",reason:ReviewRows.All(x=>x.Valid)?"Fresh":"Stale");},()=>!Busy&&ReviewOpen);
        ClearSelection=new(()=>Bulk(()=>{foreach(var row in Assets)row.Selected=false;}),()=>!Busy&&!AnyReview);
        ClearListings=new(()=>Bulk(()=>{foreach(var row in Listings)row.Selected=false;}),()=>!Busy&&!AnyReview);
        Review = new(() => { try { BuildReview(); } catch(Exception e) { Error(e); } }, () => CanRead && fees is not null && InventoryRead.State==SteamReadState.Ready && PricesRead.State==SteamReadState.Ready && !Busy && !AnyReview && SelectedSteamRows().Length>0 && SelectedSteamRows().All(x=>x.Price is not null&&(PricingMode!=1||!x.Price.Trades.IsEmpty)&&(PricingMode!=2||x.Price.Estimate is not null)));
        CloseReview = new(() => { ReviewOpen = ManagementOpen = false; ReviewRows.Clear(); Changed(); }, () => !Busy);
        Submit = new(SubmitAsync, Error, () => !Busy && CanRead && ReviewOpen && CanTradeReview && ReviewRows.Count > 0 && ReviewRows.All(x => x.Valid));
        EnableTrading = new(() => { workspace!.Access.Acknowledge(true, true, true, true); Changed(); }, () => syntheticConnection && !Busy && CanRead && TradeAccepted && workspace?.Policy.Enabled == true);
        Recover = new(RecoverAsync, Error, () => !Busy && CanRead && !AnyReview);
        Inspect = new(InspectAsync, Error, () => !Busy && CanRead && SelectedRequest is not null);
        MedianHistory = new(HistoryAsync, Error, () => !Busy && CanRead && SelectedSteamRows().Length>0);
        ReviewCancel = new(() => TryManagement(false), () => Connected && !Busy && !AnyReview && Listings.Any(x => x.Selected));
        ReviewReprice = new(() => TryManagement(true), () => Connected && !Busy && !AnyReview && Listings.Any(x => x.Selected));
        ConfirmManagement = new(ManageAsync, Error, () => !Busy && CanRead && CanTrade && ManagementOpen);
        ReviewStoredReprice=new(()=>{try{var prior=workspace!.Store.Reprice(SelectedReprice??"")??throw new SteamException(SteamFailure.Restricted);if(prior.ReplacementKey is not null||prior.State==SteamRepriceState.Superseded)throw new SteamException(SteamFailure.Restricted);managementReprice=true;revisionSource=prior.Key;management=[(prior.Listing,SteamReviewRow.ParsePrice(RevisedPrice,prior.Listing.Currency),Guid.NewGuid().ToString("N"))];managementBooks.Clear();managementBooks[management[0].Key]=CaptureRepriceBook(prior.Listing);ManagementOpen=true;Raise(nameof(ManagementSummary));}catch(Exception error){Error(error);}},()=>!Busy&&Connected&&SelectedReprice is not null&&!AnyReview);
        ResumeReprice = new(ResumeRepriceAsync,Error,()=>!Busy&&Connected&&CanTrade&&SelectedReprice is not null&&!AnyReview);
        SelectAllListings=new(()=>Bulk(()=>{foreach(var row in VisibleListings)row.Selected=true;}),()=>!Busy&&!AnyReview&&Listings.Count>0);
        ArchiveBatch=new(()=>{try{workspace!.Store.ArchiveCompleted();Results.Clear();Status="확정 배치 보관 완료 · 거래 의도·증거·예약은 보존";}catch(Exception error){DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.Management,DiagnosticOutcome.Blocked,LogLevel.Warning,DiagnosticError.Validation,exception:error);Status="미확정 배치는 보관할 수 없습니다. 먼저 상태를 대조하세요.";}Changed();},()=>!Busy&&Connected&&!AnyReview&&Results.Count>0);
        CancelOperation=new(()=>operation?.Cancel(),()=>Busy);
    }
    public ObservableCollection<SteamGameScope> Games { get; } = [new(730,"2","Counter-Strike 2"),new(570,"2","Dota 2"),new(440,"2","Team Fortress 2")];
    public SteamGameScope Game { get=>game;set
    {
        if(Busy||AnyReview||value==game)return;RememberSelection();inventoryViews[game.Key]=Assets.ToArray();
        while(inventoryViews.Count>8||inventoryViews.Values.Sum(x=>(long)x.Length)>20000)inventoryViews.Remove(inventoryViews.Keys.First());
        Set(ref game,value);Assets.ReplaceAll(inventoryViews.GetValueOrDefault(game.Key)??[]);Filter();fees=null;
        InventoryRead.Update(SteamReadState.Stale);FeesRead.Update(SteamReadState.Stale);PricesRead.Update(SteamReadState.Stale);
        Status="게임 변경 · 마지막 조회 자료와 선택을 보존합니다. 최신 데이터는 직접 갱신하세요.";Raise(nameof(CurrencyText));Changed();
    }}
    public RangeObservableCollection<SteamAssetRow> Assets { get; } = [];
    public ObservableCollection<SteamReviewRow> ReviewRows { get; } = [];
    public RangeObservableCollection<SteamListingRow> Listings { get; } = [];
    public ObservableCollection<LineResult> Results { get; } = [];
    public ObservableCollection<string> Requests { get; } = [];
    public ObservableCollection<string> RepriceKeys { get; } = [];
    private string? selectedReprice;
    public string? SelectedReprice {get=>selectedReprice;set{Set(ref selectedReprice,value);Changed();}}
    private string? selectedRequest;
    public string? SelectedRequest { get=>selectedRequest; set {Set(ref selectedRequest,value);Changed();} }
    public AsyncCommand BeginLogin { get; } public AsyncCommand CheckLogin { get; } public Command Logout { get; }
    public AsyncCommand Load { get; } public AsyncCommand RefreshPrices { get; } public Command SelectAll { get; }
    public Command Review { get; } public Command CloseReview { get; } public AsyncCommand Submit { get; } public Command EnableTrading { get; }
    public AsyncCommand Recover { get; } public AsyncCommand Inspect { get; } public AsyncCommand MedianHistory { get; }
    public Command ReviewCancel { get; } public Command ReviewReprice { get; } public AsyncCommand ConfirmManagement { get; }
    public AsyncCommand ResumeReprice { get; }
    public Command SelectAllListings {get;} public Command ArchiveBatch {get;}
    public Command CancelOperation {get;}
    public bool Busy { get => busy; private set { Set(ref busy,value); Changed(); } }
    public bool RiskAccepted { get => riskAccepted; set { Set(ref riskAccepted,value); if(!value&&workspace is not null)LogoutNow(); Changed(); } }
    public bool CredentialsAccepted { get => credentialsAccepted; set { Set(ref credentialsAccepted,value); if(!value&&workspace is not null)LogoutNow(); Changed(); } }
    public bool TradeAccepted { get => tradeAccepted; set { Set(ref tradeAccepted,value); if(!value&&workspace is not null){operation?.Cancel();workspace.Access.Acknowledge(RiskAccepted,CredentialsAccepted,true,false);} Changed(); } }
    public bool CanTrade => RiskAccepted&&CredentialsAccepted&&TradeAccepted&&workspace?.Executor.CanExecute == true;
    public bool CanTradeReview=>CanTrade&&(PricingMode!=2||workspace?.Policy.MedianPriceContractSupported==true);
    public string PricingContractText=>PricingMode==2?"이력 중앙값의 결제액/통화/기간 계약도 별도 확인 필요 · 기본 실제 제출 차단 · 추정은 정확한 거래 평균이 아닙니다.":"호가 기반 추정도 체결 보장 없음 · 실제 등록은 별도 허용/검증 전 차단";
    public bool Connected => workspace?.Authentication.Connected == true;
    public bool ReviewOpen { get => reviewOpen; private set { Set(ref reviewOpen,value); Changed(); } }
    public bool ManagementOpen { get => managementOpen; private set { Set(ref managementOpen,value); Changed(); } }
    public bool AnyReview => ReviewOpen || ManagementOpen;
    public SteamLoginState LoginState { get => state; private set { var old=state;Set(ref state,value);if(old!=value)DiagnosticHub.Emit(DiagnosticEvent.AuthState,DiagnosticArea.Authentication,from:old.ToString(),to:value.ToString(),requirement:"A03"); Raise(nameof(Connected));Raise(nameof(ImageLoader)); Changed(); } }
    public bool[,]? QrMatrix { get => matrix; private set => Set(ref matrix,value); }
    public string Status { get => status; private set => Set(ref status,value); }
    public string EvidenceText { get => evidence; private set => Set(ref evidence,value); }
    public string HistoryText { get => history; private set => Set(ref history,value); }
    public string CurrencyText => fees is null ? "지갑 통화 미조회" : $"지갑 {fees.Currency} · 최소 {Money.FromWire(fees.MinimumWire,fees.Currency)} · 가격 증분 {Money.FromWire(fees.IncrementWire,fees.Currency)} · 서버 설정";
    public string TradingText => CanTrade ? (syntheticConnection?"합성 계약 실행 · 실제 Steam 거래 없음":"비공식 실제 거래 모드 · 이 세션 한정 · 제재 위험 있음") : "실제 거래 OFF · 선택 게임의 계약 사전검증과 명시적 세션 활성화가 필요합니다.";
    public string Totals => fees is null || ReviewRows.Any(x => !x.Valid) ? "가격/시세 입력을 확인하세요." :
        $"구매자 {new Money(ReviewRows.Sum(x=>checked(x.Quote.BuyerPays*x.Quantity)),fees.Currency)} · 수수료 {new Money(ReviewRows.Sum(x=>checked(x.Quote.Fees*x.Quantity)),fees.Currency)} · 실수령 {new Money(ReviewRows.Sum(x=>checked(x.Quote.SellerReceives*x.Quantity)),fees.Currency)}";
    public string ManagementSummary => string.Join("\n",management.Select(x=>$"{x.Listing.HashName} · {x.Listing.Quantity}개 · {(managementReprice ? "취소 확인 후 새 구매자 가격 " + new Money(x.Price,x.Listing.Currency) : "판매 취소")}"));
    private async Task Run(Func<CancellationToken,Task> action,[System.Runtime.CompilerServices.CallerMemberName]string stage="")
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.UiCommand,DiagnosticArea.UI,"G08",stage);
        LastOperationId=DiagnosticHub.Context?.OperationId;Raise(nameof(LastOperationId));
        if(Busy)return;Busy=true;operation=new();authenticating=stage is nameof(BeginAsync) or nameof(CheckAsync);
        var read=ReadFor(stage);read?.Update(SteamReadState.Loading);if(read==PricesRead&&PricingMode==2)HistoryRead.Update(SteamReadState.Stale);
        try{await action(operation.Token);read?.Update(read==InventoryRead&&Assets.Count==0||read==ListingsRead&&Listings.Count==0?SteamReadState.Empty:SteamReadState.Ready);}
        catch(OperationCanceledException)
        {
            read?.Update(SteamReadState.Cancelled);
            if(!logoutRequested){if(authenticating&&!Connected)LoginState=SteamLoginState.Cancelled;Status="작업 중단 · 기존 자료 보존 · 미확정 거래는 재전송하지 않습니다.";}
        }
        catch(Exception error)
        {
            if(!logoutRequested)
            {
                if(read is not null)ReadFailed(read,error);if(error is SteamException{Endpoint:"History"}&&read!=HistoryRead)ReadFailed(HistoryRead,error);
                if(stage==nameof(LoadAsync)){Game.Update(error is SteamException sx&&sx.Code==SteamFailure.Unsupported?InventoryScopeState.Unsupported:error is SteamException ax&&ax.Code==SteamFailure.Forbidden?InventoryScopeState.PrivateOrDenied:Assets.Count>0?InventoryScopeState.Partial:InventoryScopeState.Failed,Assets.Count==0?null:Assets.Count(x=>x.Asset.Marketable));Raise(nameof(ScopeSummary));}
                Error(error);
            }
        }
        finally{operation.Dispose();operation=null;authenticating=false;if(logoutRequested){workspace?.Dispose();workspace=null;logoutRequested=false;}Busy=false;}
    }
    public Task BeginAsync() => Run(async token =>
    {
        if (Connected || !RiskAccepted || !CredentialsAccepted) throw new SteamException(SteamFailure.Disabled);
        ClearData(); workspace?.Dispose(); workspace = factory(Game); workspace.Access.Acknowledge(true,true,true,false);
        LoginState = SteamLoginState.RequestingQr; Status = "공식 Steam 앱으로 QR을 스캔하세요. 외부 QR 서비스는 사용하지 않습니다.";
        await workspace.Authentication.BeginAsync(token); token.ThrowIfCancellationRequested();
        UpdateChallenge(); LoginState=SteamLoginState.AwaitingMobile;
    });
    public Task CheckAsync() => Run(async token =>
    {
        if (workspace is null) throw new SteamException(SteamFailure.Disabled);
        if (await workspace.Authentication.CheckAsync(token)) { token.ThrowIfCancellationRequested(); QrMatrix=null; displayedChallenge=null; LoginState=SteamLoginState.Connected; Status="Steam 인증 성공 · 실제 읽기 연결 · 거래는 별도 차단"; RestoreCatalog();RefreshRequests();RecordCheck("AuthTransfer",1); }
        else { UpdateChallenge(); Status="모바일 승인 대기 · 갱신된 QR은 화면에 반영 · 서버 간격 이전에는 추가 요청하지 않습니다."; }
    });
    public Task LoadAsync() => Run(async token =>
    {
        RequireConnection(); workspace!.SetGame(Game.AppId,Game.ContextId,Game.Name);
        var assets=await workspace.Inventory.GetAsync(token);token.ThrowIfCancellationRequested();
        RememberSelection();var available=workspace.Store.AvailableAll(assets);
        Bulk(()=>Assets.ReplaceAll(assets.Select(asset=>{var row=new SteamAssetRow(asset,available[asset.Key],Changed){Image=workspace.Inventory.Images.GetValueOrDefault(asset.Key),ImageOperation=DiagnosticHub.Context?.OperationId};if(selections.TryGetValue(asset.Key,out var saved)){row.Quantity=saved.Quantity;row.Selected=saved.Selected;}return row;})));Filter();
        DiagnosticHub.Emit(DiagnosticEvent.InventoryLoaded,DiagnosticArea.Data,count:assets.Count,requirement:"C13",reason:"SelectionRestored");
        Game.Update(assets.Count==0?InventoryScopeState.Empty:InventoryScopeState.Loaded,assets.Count(x=>x.Marketable),workspace.Inventory.Images.Values.FirstOrDefault(x=>x.Token is not null));
        RecordCheck("Inventory",assets.Count);
        await IndependentRead(FeesRead,async()=>{var schedule=await workspace!.FeesAsync(Game.AppId,token);token.ThrowIfCancellationRequested();fees=schedule;gameFees[Game.AppId]=schedule;RecordCheck("Fees",1,schedule.Currency);Raise(nameof(CurrencyText));});
        await IndependentRead(ListingsRead,async()=>{var listings=await workspace!.Orders.RefreshAsync(token);token.ThrowIfCancellationRequested();SetListings(listings);RecordCheck("Listings",listings.Length);});
        Status=$"{Game.Name} · 보관함 {assets.Count}종 갱신 · 조회별 상태는 계정 연결에서 확인하세요.";RefreshRequests();Changed();
    });
    public Task RefreshPricesAsync() => Run(async token =>
    {
        RequireConnection();var revision=priceRevision;var mode=PricingMode;var window=IndicatorWindow;var selected=SelectedSteamRows();var groups=selected.GroupBy(x=>(x.Asset.Key.AppId,x.Asset.MarketHashName)).ToArray();
        if(groups.Length>50)throw new SteamException(SteamFailure.Restricted);
        foreach(var group in groups){var quote=mode==2?await workspace!.Prices.RefreshEstimateAsync(group.First().Asset,window,token):await workspace!.Prices.RefreshAsync(group.First().Asset,token);token.ThrowIfCancellationRequested();if(revision!=priceRevision)return;if(fees is not null&&quote.Currency!=fees.Currency)throw new SteamException(SteamFailure.Restricted);foreach(var row in group)row.SetPrice(quote);RecordCheck("Orderbook",quote.Bids.Length,quote.Currency);if(quote.Estimate is {} observed)RecordCheck("History",observed.Buckets,observed.Currency);}
        Status="Steam 호가 갱신 완료 · 매수 잔량은 동일 상품 전체가 공유 · 체결 보장 없음";
    });
    public void BuildReview()
    {
        RequireConnection();
        if(fees is null||PricingMode==1&&Assets.Where(x=>x.Selected).Any(x=>x.Price is null||x.Price.Trades.IsEmpty))throw new SteamException(SteamFailure.Restricted);
        var selected=SelectedSteamRows();if(selected.Select(x=>gameFees.GetValueOrDefault(x.Asset.Key.AppId)?.Currency).Distinct().Count()!=1||selected.Any(x=>!gameFees.ContainsKey(x.Asset.Key.AppId)))throw new SteamException(SteamFailure.Restricted);if(selected.Any(x=>x.Quantity>x.Available || x.Price is null))throw new SteamException(SteamFailure.Restricted);
        var basis=PricingMode switch{1=>MarketCrate.Core.PriceBasis.ExactAverage,2=>MarketCrate.Core.PriceBasis.MedianEstimate,_=>MarketCrate.Core.PriceBasis.Immediate};
        var plan=SalePlanner.Build(selected.Select(x=>new SaleSelection(x.Asset,x.Price!,x.Quantity)),basis);
        var liquidity=basis==MarketCrate.Core.PriceBasis.Immediate?plan:SalePlanner.Build(selected.Select(x=>new SaleSelection(x.Asset,x.Price!,x.Quantity)),true);
        ReviewLiquidityText=$"조회 시점 원래 호가의 매수 잔량 부족 {liquidity.UnpricedQuantity}개 · 제안/수동 가격의 체결 보장 없음";Raise(nameof(ReviewLiquidityText));ReviewRows.Clear();
        foreach(var line in plan.Lines)ReviewRows.Add(new(line.Selection.Asset,line.Allocation.Quantity,line.Allocation.BuyerPays,line.Selection.Price,gameFees.GetValueOrDefault(line.Selection.Asset.Key.AppId)??throw new SteamException(SteamFailure.Restricted),Changed,basis){Image=selected.First(x=>x.Asset.Key==line.Selection.Asset.Key).Image,ImageOperation=DiagnosticHub.Context?.OperationId});
        foreach(var line in plan.Unpriced)ReviewRows.Add(new(line.Selection.Asset,line.Quantity,0,line.Selection.Price,gameFees.GetValueOrDefault(line.Selection.Asset.Key.AppId)??throw new SteamException(SteamFailure.Restricted),Changed,basis){Image=selected.First(x=>x.Asset.Key==line.Selection.Asset.Key).Image,ImageOperation=DiagnosticHub.Context?.OperationId});
        DiagnosticHub.Emit(DiagnosticEvent.ReviewOpened,DiagnosticArea.Pricing,requirement:"E01",count:ReviewRows.Count);
        ReviewOpen=true;Status=$"매수 잔량 부족 {liquidity.UnpricedQuantity}개 · 수동 가격은 이 검토 안에서만 유지 · 단일 최종 승인";Changed();
    }
    public Task SubmitAsync() => Run(async token =>
    {
        RequireConnection();if(!CanTradeReview || !ReviewOpen || ReviewRows.Count==0 || ReviewRows.Any(x=>!x.Valid))throw new SteamException(SteamFailure.Disabled);
        var batch=new SaleBatchSnapshot(Guid.NewGuid(),workspace!.Authentication.SteamId!,DateTimeOffset.UtcNow,ReviewRows.Select((x,i)=>x.Snapshot(i)).ToImmutableArray());
        var result=await workspace.Batches.ExecuteAsync(batch,cancellationToken:token);token.ThrowIfCancellationRequested();SetResults(result);ReviewOpen=false;RefreshRequests();Status="배치 요청 결과 · Submitted/승인 대기는 게시 완료가 아닙니다.";if(workspace.Failure is {} failure)Error(new SteamException(failure));
    });
    public Task RecoverAsync() => Run(async token=>{RequireConnection();var record=await workspace!.Batches.RecoverAsync(token);var listings=await workspace.Orders.RefreshAsync(token);token.ThrowIfCancellationRequested();if(record is not null)SetResults(record);SetListings(listings);RefreshRequests();Status="서버 상태 대조 완료 · 불명 요청을 재전송하지 않았습니다.";});
    public Task InspectAsync() => Run(async token=>
    {
        RequireConnection();if(SelectedRequest is null)throw new SteamException(SteamFailure.Restricted);
        var report=await workspace!.Executor.InspectAsync(SelectedRequest,token);
        token.ThrowIfCancellationRequested();
        var command=workspace.Store.ReadCommand(SelectedRequest)!;
        EvidenceText=$"요청 시각 {command.SentAt:O} · 대상 {command.Line.Asset.Key.AppId}/{command.Line.Asset.Key.ContextId}/{command.Line.Asset.Key.AssetId} · {command.Line.Quantity}개 · 실수령 단가 {new Money(command.Line.UnitQuote.SellerReceives,command.Line.Currency)}\n"+
            $"요청 전 목록 ID {(command.BeforeListingIds is null?"기록 없음":string.Join(',',command.BeforeListingIds.Take(10)))}\n판정 {report.Verdict} · {report.CheckedAt:O}\n"+string.Join("\n",report.Candidates.Select(x=>$"등록ID {x.ListingId} · {x.Reason} · {x.Quantity}개 · 실수령 {new Money(x.SellerReceives,command.Line.Currency)} · 시각 {x.CreatedAt:O}"))+
            "\n감사 이력: "+string.Join(" → ",workspace.Store.AuditTrail(SelectedRequest).Select(x=>$"{x.At:HH:mm:ss} {x.Action}/{x.State}"))+"\n후보 일치는 인과관계 증명이 아닙니다. 자동 확정·재전송하지 않습니다.";
    });
    private Task HistoryAsync()=>Run(async token=>{RequireConnection();var row=Assets.First(x=>x.Selected);var points=await workspace!.Prices.GetMedianHistoryAsync(row.Asset,token);token.ThrowIfCancellationRequested();HistoryText=$"Steam pricehistory · 수집 {DateTimeOffset.UtcNow:O} · {points.Length}개 시간 구간의 중앙값/거래량 · 정확한 거래 원자료/VWAP/전체 체결 중앙값 아님\n"+string.Join("\n",points.TakeLast(6).Select(x=>$"{x.At:MM-dd HH:mm} UTC · median {x.MedianMajor} · volume {x.Volume}"));});
    private void TryManagement(bool reprice){try{BuildManagement(reprice);}catch(Exception error){DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.Management,DiagnosticOutcome.Failed,LogLevel.Warning,DiagnosticError.Validation,exception:error);Status="관리 검토 오류: 통화/가격/수수료를 확인하세요.";Changed();}}
    private void BuildManagement(bool reprice)
    {
        RequireConnection();
        if(reprice&&fees is null)throw new SteamException(SteamFailure.Restricted);revisionSource=null;managementReprice=reprice;
        management=Listings.Where(x=>x.Selected).Select(x=>(x.Listing,reprice?SteamReviewRow.ParsePrice(x.NewPriceText,x.Listing.Currency):0,
            reprice?workspace?.Store.Reprices().FirstOrDefault(p=>p.Listing.ListingId==x.Listing.ListingId)?.Key??Guid.NewGuid().ToString("N"):Guid.NewGuid().ToString("N"))).ToArray();
        managementBooks.Clear();if(reprice)foreach(var row in management){var asset=new InventoryAsset(row.Listing.Asset,"Steam",row.Listing.HashName,row.Listing.HashName,row.Listing.Quantity,row.Listing.Quantity>1,true,false);var price=workspace!.Prices.Get(asset);if(DateTimeOffset.UtcNow-price.CapturedAt>TimeSpan.FromMinutes(5))throw new SteamException(SteamFailure.StaleReview);managementBooks[row.Key]=OrderbookFingerprint.Of(price);if(row.Listing.Currency!=fees!.Currency)throw new SteamException(SteamFailure.Restricted);_=fees.FromBuyer(row.Price);}
        ManagementOpen=true;Raise(nameof(ManagementSummary));
    }
    private Task ManageAsync()=>Run(async token=>
    {
        RequireConnection();if(!CanTrade)throw new SteamException(SteamFailure.Disabled);Results.Clear();
        if(!managementReprice){var cancelled=await workspace!.Executor.CancelBatchAsync(management.Select(x=>x.Listing).ToArray(),Guid.NewGuid().ToString("N"),token);foreach(var item in cancelled)Results.Add(item);ManagementOpen=false;RefreshRequests();Status="취소 요청 결과 · 반환 확정 전 재등록 금지";return;}
        foreach(var row in management)
        {
            if(managementReprice){var intent=revisionSource is null?await workspace!.Executor.RepriceAsync(row.Listing,row.Price,row.Key,token,managementBooks.GetValueOrDefault(row.Key)):await workspace!.Executor.ReviseRepriceAsync(revisionSource,row.Price,row.Key,token,managementBooks.GetValueOrDefault(row.Key));token.ThrowIfCancellationRequested();Results.Add(new(row.Listing.ListingId,intent.State==SteamRepriceState.Submitted?SaleState.Submitted:SaleState.Unknown,"재가격 "+intent.State));}
            else {var result=await workspace!.Executor.CancelAsync(row.Listing,row.Key,token);token.ThrowIfCancellationRequested();Results.Add(result);}
            if(Results.Last().State==SaleState.Unknown)break;
        }
        ManagementOpen=false;RefreshRequests();Status="관리 결과 · 취소/반환 대조가 불명확하면 재등록하지 않습니다.";if(workspace?.Failure is {} failure)Error(new SteamException(failure));
    });
    private void RequireConnection(){if(!Connected)throw new SteamException(SteamFailure.Authentication);if(workspace!.Failure is {} blocked)throw new SteamException(blocked);}
    private Task ResumeRepriceAsync()=>Run(async token=>
    {
        RequireConnection();var intent=workspace!.Store.Reprice(SelectedReprice??"")??throw new SteamException(SteamFailure.Restricted);
        var result=await workspace.Executor.RepriceAsync(intent.Listing,intent.BuyerPrice,intent.Key,token);token.ThrowIfCancellationRequested();Status="보존한 재가격 의도 대조/재개: "+result.State;RefreshRequests();
    });
    private void SetListings(IEnumerable<SteamListing> rows){var listingRows=rows.ToArray();ObserveCatalog(listingRows);RememberSelection();Bulk(()=>Listings.ReplaceAll(listingRows.Select(row=>new SteamListingRow(row,Changed){Image=workspace?.Orders.Images.GetValueOrDefault(row.ListingId),ImageOperation=DiagnosticHub.Context?.OperationId,Selected=listingSelections.GetValueOrDefault(row.ListingId)})));Filter();Changed();}
    private void SetResults(BatchRecord record){Results.Clear();foreach(var result in record.Results)Results.Add(result);}
    private void RefreshRequests(){Requests.Clear();RepriceKeys.Clear();if(workspace is not null){foreach(var command in workspace.Store.Commands())Requests.Add(command.RequestKey);foreach(var plan in workspace.Store.Reprices())RepriceKeys.Add(plan.Key);var available=workspace.Store.AvailableAll(Assets.Select(x=>x.Asset).ToArray());Bulk(()=>{foreach(var row in Assets)row.UpdateAvailable(available[row.Asset.Key]);});}}
    private void ClearData(){tradeAccepted=false;Raise(nameof(TradeAccepted));ClearSelfCheck();foreach(var read in ReadStatuses)read.Update(SteamReadState.NotQueried);readChecks.Clear();displayedChallenge=null;Raise(nameof(ContractSummary));ResetCatalog();gameFees.Clear();TradingConfirmation="";TradingPreparation="기본 OFF · 새 연결에서 다시 계약을 확인하세요.";inventoryViews.Clear();selections.Clear();listingSelections.Clear();VisibleAssets.Clear();VisibleListings.Clear();QrMatrix=null;Assets.Clear();Listings.Clear();ReviewRows.Clear();Results.Clear();Requests.Clear();RepriceKeys.Clear();SelectedRequest=SelectedReprice=null;fees=null;ReviewOpen=ManagementOpen=false;managementBooks.Clear();EvidenceText="대조 증거 없음";HistoryText="중간가격 이력 미조회 (정확한 거래 VWAP 아님)";Raise(nameof(CurrencyText));}
    private void LogoutNow(){imagesEnabled=false;Raise(nameof(ImagesEnabled));Raise(nameof(ImageLoader));DiagnosticHub.Emit(DiagnosticEvent.Logout,DiagnosticArea.Authentication,requirement:"A05");var cancelling=Busy||LoginState is SteamLoginState.AwaitingMobile or SteamLoginState.RequestingQr;operation?.Cancel();workspace?.Authentication.Dispose();ClearData();tradeAccepted=false;Raise(nameof(TradeAccepted));LoginState=cancelling?SteamLoginState.Cancelled:SteamLoginState.Disconnected;Status="로그아웃 · 인증정보 저장 없음 · 거래 저널 보존";if(Busy)logoutRequested=true;else{workspace?.Dispose();workspace=null;}Changed();}
    private void Error(Exception error)
    {
        DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.UI,DiagnosticOutcome.Failed,LogLevel.Error,error is SteamException se?se.Code switch{SteamFailure.Authentication or SteamFailure.Expired=>DiagnosticError.Authentication,SteamFailure.RateLimited=>DiagnosticError.RateLimited,SteamFailure.Transport=>DiagnosticError.Transport,SteamFailure.ContractChanged=>DiagnosticError.Schema,_=>DiagnosticError.Validation}:DiagnosticError.Validation,exception:error);
        if(error is SteamException {Code:SteamFailure.Authentication or SteamFailure.Expired})
        {workspace?.Authentication.Dispose();QrMatrix=null;ReviewOpen=ManagementOpen=false;LoginState=SteamLoginState.Expired;}
        else if(authenticating)
        {
            QrMatrix=null;
            LoginState=error is SteamException e?e.Code switch{SteamFailure.Disabled=>LoginState,SteamFailure.Denied=>SteamLoginState.Denied,SteamFailure.Transport=>SteamLoginState.NetworkFailure,SteamFailure.Forbidden or SteamFailure.RateLimited or SteamFailure.Challenge=>SteamLoginState.AccessBlocked,_=>SteamLoginState.ContractFailure}:SteamLoginState.ContractFailure;
        }
        if(!authenticating&&error is SteamException{Code:SteamFailure.StaleReview}){ReviewOpen=ManagementOpen=false;ReviewRows.Clear();managementBooks.Clear();Status="승인한 가격·수량·호가 자료가 변경되었거나 오래되었습니다. 갱신 후 새 최종 검토가 필요합니다. 자동으로 낮은 가격을 제출하지 않았습니다.";Changed();return;}
        Status=authenticating&&error is SteamException{Code:SteamFailure.Disabled}?"이 환경에서는 인증을 시작할 수 없습니다 (Disabled).":authenticating?"인증 작업 실패 · "+(workspace?.Authentication.LastFailureStage?.ToString()??"AuthBegin"):RequestsBlocked?"서버 제한 또는 안전 차단 · 추가 요청 중단":"이 조회를 완료하지 못했습니다. 다른 조회는 사용할 수 있습니다.";Changed();
    }
    private void Changed()
    {
        if(bulkUpdate)return;
        CompareOfficialFees?.Refresh();Raise(nameof(FeeComparisonAvailability));InvalidateFeeComparison();
        CheckScopeChanged();ShowSelfCheck?.Refresh();SelfCheck?.Refresh();
        Raise(nameof(Connected));Raise(nameof(SessionPresent));Raise(nameof(CanRead));Raise(nameof(RequestsBlocked));Raise(nameof(AuthenticationText));Raise(nameof(ConnectionSummary));Raise(nameof(RequestAccessText));Raise(nameof(SelectionSummary));Raise(nameof(SelectionEstimate));Raise(nameof(AnyReview));LoadListings?.Refresh();LoadFees?.Refresh();OpenOfficialMarket?.Refresh();
        PrepareTrading?.Refresh();Raise(nameof(Totals));Raise(nameof(TradingText));Raise(nameof(CanTrade));Raise(nameof(CanTradeReview));Raise(nameof(PricingContractText));
        BeginLogin?.Refresh();CheckLogin?.Refresh();Load?.Refresh();RefreshPrices?.Refresh();SelectAll?.Refresh();Review?.Refresh();CloseReview?.Refresh();Submit?.Refresh();EnableTrading?.Refresh();Recover?.Refresh();Inspect?.Refresh();MedianHistory?.Refresh();ReviewCancel?.Refresh();ReviewReprice?.Refresh();ConfirmManagement?.Refresh();
        ResumeReprice?.Refresh();ReviewStoredReprice?.Refresh();RefreshReviewStatus?.Refresh();ListingMetrics?.Refresh();
        ClearSelection?.Refresh();ClearListings?.Refresh();
        SelectAllListings?.Refresh();ArchiveBatch?.Refresh();
        CancelOperation?.Refresh();AddScope?.Refresh();RemoveScope?.Refresh();Raise(nameof(ScopeSummary));
    }
    private void Bulk(Action action){bulkUpdate=true;try{action();}finally{bulkUpdate=false;Changed();}}
    public void Dispose()=>LogoutNow();
}

public sealed class SteamAssetRow(InventoryAsset asset,int available,Action changed):Observable
{
    public ItemImageReference? Image{get;init;}public Guid? ImageOperation{get;init;}
    public InventoryAsset Asset {get;}=asset;public string Name=>Asset.Name;public int Owned=>Asset.Quantity;public int Available {get;private set;}=available;public int Maximum=>Math.Max(1,Available);
    public bool CanSelect=>Asset.Marketable&&!Asset.Locked&&Available>0;public string Flags=>$"보유 {Owned} · 예약 {Math.Max(0,Owned-Available)} · 가용 {Available} · {(Asset.Marketable?"장터 판매 가능":"장터 판매 불가")} · {(Asset.Tradable?"거래 가능":"거래 제한")}";
    private bool selected;private int quantity=1;public bool Selected{get=>selected;set{if(Set(ref selected,value&&CanSelect))changed();}}public int Quantity{get=>quantity;set{if(Set(ref quantity,Math.Clamp(value,1,Maximum)))changed();}}
    public PriceSnapshot? Price {get;private set;}public string PriceText=>Price?.Estimate is {} indicator?$"Steam pricehistory 추정 중앙값 {new Money(indicator.Minor,indicator.Currency)} (실제 평균 아님) · {indicator.Buckets}개 기간 / 거래량 {indicator.Volume} · {indicator.WindowStart:MM-dd HH:mm}–{indicator.WindowEnd:MM-dd HH:mm} UTC · 수집 {indicator.CapturedAt:HH:mm:ss}":Price is null?"시세 미조회":$"{PriceDisplay.Immediate} · 최고 구매주문  {(Price.Bids.IsEmpty?"매수 잔량 없음":new Money(Price.Bids[0].BuyerPays,Price.Currency).ToString())} · 최저 판매호가 정보 없음 · {Price.CapturedAt:HH:mm:ss} UTC";
    public void ClearPrice(){Price=null;Raise(nameof(PriceText));}
    public void SetPrice(PriceSnapshot price){Price=price;Raise(nameof(PriceText));changed();}
    public void UpdateAvailable(int value){Available=value;if(value==0)Selected=false;quantity=Math.Clamp(quantity,1,Maximum);Raise(nameof(Available));Raise(nameof(Flags));Raise(nameof(Maximum));Raise(nameof(CanSelect));Raise(nameof(Quantity));changed();}
}
public sealed class SteamReviewRow:Observable
{
    private readonly SteamFeeSchedule fees;private readonly Action changed;private readonly PriceEvidence? priceEvidence;private readonly string bookDigest;private string text;private readonly long originalBid,recommended,reference;
    public ItemImageReference? Image{get;init;}public Guid? ImageOperation{get;init;}
    public InventoryAsset Asset{get;}public int Quantity{get;}public DateTimeOffset CapturedAt{get;}public bool Insufficient{get;}public string Identity=>$"{Asset.Game} · {Asset.Name} · {Quantity}개";
    public string PriceText{get=>text;set{if(!Set(ref text,value))return;DiagnosticHub.Emit(DiagnosticEvent.PriceEdited,DiagnosticArea.Pricing,requirement:"E05",reason:Valid?"Committed":"InputRejected");Raise(nameof(Valid));Raise(nameof(Amounts));Raise(nameof(PriceBasis));Raise(nameof(PriceWarning));changed();}}
    public SteamReviewRow(InventoryAsset asset,int quantity,long buyer,PriceSnapshot price,SteamFeeSchedule fees,Action changed,MarketCrate.Core.PriceBasis basis=MarketCrate.Core.PriceBasis.Immediate)
    {bookDigest=OrderbookFingerprint.Of(price);priceEvidence=basis==MarketCrate.Core.PriceBasis.MedianEstimate?price.Estimate:null;Asset=asset;Quantity=quantity;CapturedAt=price.CapturedAt;Insufficient=buyer==0;this.fees=fees;this.changed=changed;originalBid=buyer;reference=price.Bids.IsEmpty?0:price.Bids[0].BuyerPays;try{buyer=fees.FromBuyer(buyer,false).BuyerPays;}catch(ArgumentException){buyer=0;}recommended=buyer;text=buyer==0?"":FormatPrice(buyer,fees.Currency);}
    public string PriceBasis=>priceEvidence is {} ev?$"Steam pricehistory · 기간 중앙값의 거래량 가중 중앙값 추정 · 정확한 평균/즉시거래가능가 아님 · {ev.Buckets}구간/서버 거래량 {ev.Volume} · {ev.WindowStart:MM-dd HH:mm}–{ev.WindowEnd:MM-dd HH:mm} UTC":Insufficient?"매수 잔량 부족 · 직접 가격 필요 · 예상 미체결 수량":$"{PriceDisplay.Immediate} · 호가 {new Money(originalBid,fees.Currency)} · 증분/수수료 반영 추천 {new Money(recommended,fees.Currency)} · {(text==FormatPrice(recommended,fees.Currency)?"추천":"수동 수정")}";
    public bool Valid{get{try{_=Quote;return (priceEvidence is null||priceEvidence.Fresh(DateTimeOffset.UtcNow))&&!PriceOutlier&&DateTimeOffset.UtcNow-CapturedAt<=TimeSpan.FromMinutes(5)&&CapturedAt<=DateTimeOffset.UtcNow.AddSeconds(5);}catch(Exception e)when(e is ArgumentException or OverflowException){return false;}}}
    public bool PriceOutlier{get{try{return reference>0&&(Quote.BuyerPays<reference/2m||Quote.BuyerPays>reference*2m);}catch(Exception e)when(e is ArgumentException or OverflowException){return false;}}}
    public string PriceWarning=>PriceOutlier?"가격 이상: 조회한 최상위 호가의 50~200% 범위를 벗어났습니다. 시세를 갱신하고 검토하세요.":DateTimeOffset.UtcNow-CapturedAt>TimeSpan.FromMinutes(5)?"시세 오래됨 · 검토를 갱신하세요.":"조회 후 5분 이내만 승인 가능 · 최종 요청 전 서버 데이터 재검증";
    public void RefreshStatus(){Raise(nameof(Valid));Raise(nameof(Amounts));Raise(nameof(PriceWarning));}
    public FeeQuote Quote=>fees.FromBuyer(ParsePrice(text,fees.Currency));
    public string Amounts=>Valid?$"수량 {Quantity} · Steam {new Money(checked(Quote.SteamFee*Quantity),fees.Currency)} · 게임 {new Money(checked(Quote.GameFee*Quantity),fees.Currency)} · 실수령 {new Money(checked(Quote.SellerReceives*Quantity),fees.Currency)} · {CapturedAt:HH:mm:ss} UTC{(Insufficient?" · 매수 잔량 부족/예상 미체결":"")}":"가격/증분/수수료/시세 오류 · 입력 필요";
    public SaleLineSnapshot Snapshot(int index)=>new("steam-"+index,Asset,Quantity,fees.Currency,Quote,CapturedAt){Evidence=priceEvidence,OrderbookDigest=bookDigest};
    public static string FormatPrice(long minor,string currency)=>currency=="KRW"?minor.ToString(System.Globalization.CultureInfo.InvariantCulture):$"{minor/100}.{minor%100:00}";
    public static long ParsePrice(string text,string currency)
    {if(currency=="KRW"&&long.TryParse(text,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var won))return won;if(currency=="USD"&&decimal.TryParse(text,System.Globalization.NumberStyles.AllowDecimalPoint,System.Globalization.CultureInfo.InvariantCulture,out var usd)&&usd>=0&&usd*100==decimal.Truncate(usd*100))return checked((long)(usd*100));throw new ArgumentException("InvalidPrice");}
}
public sealed class SteamListingRow(SteamListing listing,Action changed):Observable
{
    public ItemImageReference? Image{get;init;}public Guid? ImageOperation{get;init;}
    private string metric="최근 정확한 평균 이용 불가 · 대체 지표 미조회";public string Metric{get=>metric;set=>Set(ref metric,value);}
    public SteamListing Listing{get;}=listing;public string PriceSummary=>$"{Listing.Quantity}개 · {SteamStatusText.Label(Listing.State)} · 구매자 {new Money(Listing.SellerReceives+Listing.Fees,Listing.Currency)}";public string Description=>$"{Listing.HashName} · {PriceSummary}";
    private bool selected;public bool Selected{get=>selected;set{Set(ref selected,value);changed();}}public string NewPriceText{get;set;}=SteamReviewRow.FormatPrice(listing.SellerReceives+listing.Fees,listing.Currency);
}
