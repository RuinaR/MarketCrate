using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
using Net.Codecrete.QrCodeGenerator;

namespace MarketCrate.App.ViewModels;

public enum SteamLoginState { Disconnected, RequestingQr, AwaitingMobile, Connected, Expired, Denied, Cancelled, NetworkFailure, AccessBlocked, ContractFailure, Restoring }
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
    private readonly HashSet<string> observedPostedLines=[];
    private DateTimeOffset? observedPostedAt;
    public int ServerPostedObservationCount=>observedPostedAt is {} at&&DateTimeOffset.UtcNow-at<=TimeSpan.FromMinutes(5)?observedPostedLines.Count:0;
    private readonly Dictionary<AssetKey,int> assetPublisherFees=[];
    private SteamFeeSchedule FeeFor(InventoryAsset asset)
    {
        if(!gameFees.TryGetValue(asset.Key.AppId,out var schedule)||!schedule.PublisherMatches(asset.Key.AppId,assetPublisherFees.TryGetValue(asset.Key,out var publisher)?publisher:null))
            throw new SteamException(SteamFailure.Unsupported);
        return schedule;
    }
    private readonly Dictionary<AssetKey,SteamAssetRow> retainedSelections=[];
    private IEnumerable<SteamAssetRow> AllSteamRows()=>Assets.Concat(inventoryViews.Where(x=>x.Key!=Game.Key).SelectMany(x=>x.Value)).Concat(retainedSelections.Values).DistinctBy(x=>x.Asset.Key);
    private SteamAssetRow[] SelectedSteamRows()=>AllSteamRows().Where(x=>x.Selected).ToArray();
    public bool HasQuantityInputErrors=>SelectedSteamRows().Any(x=>!x.QuantityInputValid);
    private void RetainSelection(bool replaceScope=false)
    {
        foreach(var key in retainedSelections.Where(x=>!x.Value.Selected||replaceScope&&x.Key.AppId==Game.AppId&&x.Key.ContextId==Game.ContextId).Select(x=>x.Key).ToArray())retainedSelections.Remove(key);
        foreach(var row in Assets)if(row.Selected)retainedSelections[row.Asset.Key]=row;
    }
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
    public string[] PricingModes{get;}=[PriceDisplay.Immediate];
    public string[] PriceWindows{get;}=["24시간","7일","30일"];
    public int WindowIndex{get=>windowIndex;set{if(value is >=0 and <=2&&Set(ref windowIndex,value))ResetPriceReview();}}
    private TimeSpan IndicatorWindow=>TimeSpan.FromDays(WindowIndex switch{1=>7,2=>30,_=>1});
        public int PricingMode
    {
        get=>pricingMode;
        set
        {
#if MARKETCRATE_LIVE_ONLY
            value=0; // Retired stored/automation values never reactivate average/history pricing in production.
#endif
            if(value is >=0 and <=2&&Set(ref pricingMode,value))ResetPriceReview();
        }
    }
    private void ResetPriceReview(){priceRevision++;DiagnosticHub.Emit(DiagnosticEvent.PriceIndicator,DiagnosticArea.UI,requirement:"D02",count:WindowIndex,reason:PricingMode==2?"MedianEstimate":PricingMode==1?"ExactAverage":"Immediate");ReviewOpen=false;ReviewRows.Clear();foreach(var row in AllSteamRows())row.ClearPrice();Status="가격 기준/기간 변경 · 수동 가격 초기화 · 선택 시세 갱신 후 다시 검토하세요.";Changed();}
    public Guid? LastOperationId{get;private set;}
    public Guid? LastFailedOperationId{get;private set;}
    public Guid? DiagnosticOperationId=>LastFailedOperationId??LastOperationId;
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
    public bool HasServerListings=>Listings.Count>0||workspace?.Orders.Presence.Length>0;
    public string ListingMetadataSummary=>workspace?.Orders.UnpricedCount is >0?$"장터가 보유한 자산 {workspace.Orders.UnpricedCount}건의 등록 ID·가격·설명을 완전히 확인하지 못했습니다. 빈 판매목록이 아닙니다. 해당 자산의 중복 등록은 차단하며 공식 Steam 장터에서 확인하세요.":"";
    public bool HasApprovalHandoff=>Results.Any(x=>x.State==SaleState.AwaitingSteamGuard&&!(ServerPostedObservationCount>0&&observedPostedLines.Contains(x.LineId)));
    public string ApprovalHandoffText=>ServerPostedObservationCount>0?$"현재 같은 자산 {ServerPostedObservationCount}건의 판매 중 상태를 {observedPostedAt:HH:mm:ss} UTC에 관측했습니다. 원래 요청의 귀속은 미확인이며 요청 기록은 보존합니다. 이 관측 항목에 휴대폰 승인이 계속 필요하다고 표시하거나 다시 등록하지 않습니다. "+(HasApprovalHandoff?"다른 승인 대기 항목은 개별 안내를 확인하세요.":"상태가 바뀌면 다시 조회하세요."):HasApprovalHandoff?"등록 요청 기록에 Steam 확인 대기가 있습니다. 최신 상태는 게시 재조회로 확인하세요. 각 항목의 확인 방식을 확인하세요. 모바일 확인은 공식 Steam 앱 → Steam Guard → 확인에서 직접 승인합니다. 푸시 알림이 없더라도 확인 목록을 열어 보세요. 이메일 확인을 요구한 항목은 이메일을 확인하세요. 승인 후 아래 ‘승인 후 게시 확인’을 누르세요. 알림이 없다는 이유로 다시 등록하지 마세요.":Results.Any(x=>x.State==SaleState.Unknown)?"일부 요청의 접수 결과를 확인하지 못했습니다. Steam Guard 요청이 생성됐다고 판단할 수 없습니다. 공식 장터 상태와 진단을 확인하고 자동·반복 등록하지 마세요.":"등록 요청 접수와 실제 장터 게시는 다릅니다. 승인 후 게시 확인으로 서버 상태를 확인하세요.";
    public Command ClearSelection {get;}
    public Command ClearListings {get;}
    public Command RefreshReviewStatus {get;}
    private void RememberSelection(){RetainSelection();foreach(var row in Assets)selections[row.Asset.Key]=(row.Selected,row.Quantity);foreach(var row in Listings)listingSelections[row.Listing.ListingId]=row.Selected;}
    private void Filter()
    {
        var matches=Assets.Where(x=>(!MarketableOnly||x.Asset.Marketable)&&x.Name.Contains(Search,StringComparison.OrdinalIgnoreCase));
        VisibleAssets.ReplaceAll(Descending?matches.OrderByDescending(x=>x.Name,StringComparer.OrdinalIgnoreCase):matches.OrderBy(x=>x.Name,StringComparer.OrdinalIgnoreCase));
        VisibleListings.ReplaceAll(Listings.Where(x=>x.Listing.Asset.AppId==Game.AppId&&x.Listing.HashName.Contains(Search,StringComparison.OrdinalIgnoreCase)));
        Raise(nameof(EmptyAssets));Raise(nameof(EmptyListings));
        DiagnosticHub.Emit(DiagnosticEvent.UiSelection,DiagnosticArea.UI,level:LogLevel.Debug,reason:"Filter",count:VisibleAssets.Count,requirement:"C12");
    }
    public SteamWorkspaceViewModel(Func<SteamGameScope, SteamExperimentalWorkspace>? factory = null, bool contractTest = false, bool offlineOnly = false, Action<Uri>? openOfficial = null,string? authDirectory=null)
    {
#if MARKETCRATE_LIVE_ONLY
        if(contractTest||offlineOnly||factory is not null)throw new InvalidOperationException("ProductionCompositionOnly");
#endif
        ModeText=offlineOnly?"ISOLATED GUI QA · Mock 전용 · Steam 네트워크 차단":contractTest?"CONTRACT TEST · 합성 서버 · 실제 Steam 요청 없음":"STEAM · 실제 계정 연결 · 서버 응답 기반";
        this.factory = offlineOnly ? (_ => throw new SteamException(SteamFailure.Disabled)) : factory ?? (scope => new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarketCrate", "SteamExperimental"), scope.AppId, scope.ContextId, scope.Name));
        InitializeConnection(contractTest, openOfficial);InitializeTrading();
#if MARKETCRATE_DEMO
        catalogTestDefaults=contractTest||offlineOnly;
#endif
        ResetCatalog();game = Games[0];InitializeCatalog();InitializeListingReprice();InitializeAssistedRelist();InitializeNavigation();InitializeRequestReconciliation();InitializeDevelopmentTools();InitializeFeeComparison();InitializePersistence(offlineOnly?null:authDirectory);
        BeginLogin = new(BeginAsync, Error, () => !offlineOnly && !Busy && !Connected && !AnyReview && RiskAccepted && CredentialsAccepted);
        CheckLogin = new(CheckAsync, Error, () => !Busy && LoginState == SteamLoginState.AwaitingMobile);
        Logout = new(LogoutNow);
        Load = new(LoadAsync, Error, () => !Busy && CanReadFeature("Inventory") && !AnyReview);
        RefreshPrices = new(RefreshPricesAsync, Error, () => !Busy && CanRead && !AnyReview && SelectedSteamRows().Length>0);
        SelectAll = new(() => Bulk(()=>{foreach(var row in VisibleAssets)row.Selected=row.CanSelect;}), () => !Busy && !AnyReview);
        RefreshReviewStatus=new(()=>{foreach(var row in ReviewRows)row.RefreshStatus();Changed();DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Pricing,requirement:"D07",reason:ReviewRows.All(x=>x.Valid)?"Fresh":"Stale");},()=>!Busy&&ReviewOpen);
        ClearSelection=new(()=>Bulk(()=>{foreach(var row in AllSteamRows().Where(x=>x.Asset.Key.AppId==Game.AppId&&x.Asset.Key.ContextId==Game.ContextId))row.Selected=false;foreach(var key in selections.Keys.Where(x=>x.AppId==Game.AppId&&x.ContextId==Game.ContextId).ToArray())selections[key]=(false,selections[key].Quantity);}),()=>!Busy&&!AnyReview);
        ClearListings=new(()=>Bulk(()=>{foreach(var row in Listings)row.Selected=false;}),()=>!Busy&&!AnyReview);
        Review = new(() => { if(NeedsLiveReviewPreparation){prepareReview.Execute(null);return;} try { BuildReview(); } catch(Exception e) { Error(e); } }, () => CanRead && fees is not null && InventoryRead.State==SteamReadState.Ready && PricesRead.State==SteamReadState.Ready && !Busy && !AnyReview && !HasQuantityInputErrors && SelectedSteamRows().Length>0 && SelectedSteamRows().All(x=>x.Price is not null&&(PricingMode!=1||!x.Price.Trades.IsEmpty)&&(PricingMode!=2||x.Price.Estimate is not null)));
        CloseReview = new(() => { ReviewOpen = ManagementOpen = false; ReviewRows.Clear(); Changed(); }, () => !Busy);
        Submit = new(SubmitAsync, Error, () => !Busy && CanRead && ReviewOpen && CanTradeReview && ReviewRows.Count > 0 && ReviewAmountsValid);
        EnableTrading = new(() => { workspace!.Access.Acknowledge(true, true, true, true); Changed(); }, () => syntheticConnection && !Busy && CanRead && TradeAccepted && workspace?.Policy.Enabled == true);
        Recover = new(RecoverAsync, Error, () => !Busy && CanRead && !AnyReview);
        Inspect = new(InspectAsync, Error, () => !Busy && CanRead && SelectedRequest is not null);
        MedianHistory = new(HistoryAsync, Error, () => !Busy && CanRead && SelectedSteamRows().Length>0);
        ReviewCancel = new(() => TryManagement(false), () => Connected && !Busy && !AnyReview && Listings.Any(x => x.Selected));
        ReviewReprice = new(() => TryManagement(true), () => Connected && !Busy && !AnyReview && Listings.Any(x => x.Selected) && (!LowerPriceOneStep||Listings.Where(x=>x.Selected).All(x=>x.HasStepProposal)));
        ConfirmManagement = new(ManageAsync, Error, () => !Busy && CanRead && ManagementOpen && (managementReprice?CanTrade&&managementReturnSupported:CanCancel));
        ReviewStoredReprice=new(()=>{try{var prior=workspace!.Store.Reprice(SelectedReprice??"")??throw new SteamException(SteamFailure.Restricted);if(prior.ReplacementKey is not null||prior.State==SteamRepriceState.Superseded)throw new SteamException(SteamFailure.Restricted);managementReprice=true;managementReturnSupported=true;ManagementEligibility="보존된 취소·반환 근거와 현재 가격을 다시 검증합니다. 불명 결과는 재전송하지 않습니다.";managementDeadline=workspace.Prices.Get(ListingAsset(prior.Listing)).CapturedAt.AddMinutes(5);revisionSource=prior.Key;management=[(prior.Listing,SteamReviewRow.ParsePrice(RevisedPrice,prior.Listing.Currency),Guid.NewGuid().ToString("N"))];managementBooks.Clear();managementBooks[management[0].Key]=CaptureRepriceBook(prior.Listing);ManagementOpen=true;Raise(nameof(ManagementSummary));}catch(Exception error){Error(error);}},()=>!Busy&&Connected&&SelectedReprice is not null&&!AnyReview);
        ResumeReprice = new(ResumeRepriceAsync,Error,()=>!Busy&&Connected&&CanTrade&&SelectedReprice is not null&&!AnyReview);
        SelectAllListings=new(()=>Bulk(()=>{foreach(var row in VisibleListings)row.Selected=true;}),()=>!Busy&&!AnyReview&&Listings.Count>0);
        ArchiveBatch=new(()=>{try{workspace!.Store.ArchiveCompleted(workspace.Authentication.SteamId);Results.Clear();Status="배치 보관 완료 · 미전송 항목의 자동 실행 없음 · 거래 의도·증거·예약 보존";}catch(Exception error){DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.Management,DiagnosticOutcome.Blocked,LogLevel.Warning,DiagnosticError.Validation,exception:error);Status="미확정 배치는 보관할 수 없습니다. 먼저 상태를 대조하세요.";}Changed();},()=>!Busy&&Connected&&!AnyReview&&Results.Count>0);
        CancelOperation=new(()=>operation?.Cancel(),()=>Busy);
    }
    public ObservableCollection<SteamGameScope> Games { get; } = [];
    public SteamGameScope Game { get=>game;set
    {
        if(value is null||Busy||AnyReview||value==game)return;RememberSelection();inventoryViews[game.Key]=Assets.ToArray();
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
    private string operationLabel="";
    public string OperationLabel{get=>operationLabel;private set=>Set(ref operationLabel,value);}
    public bool RiskAccepted { get => riskAccepted; set { Set(ref riskAccepted,value); if(!value&&workspace is not null)LogoutNow(); Changed(); } }
    public bool CredentialsAccepted { get => credentialsAccepted; set { Set(ref credentialsAccepted,value); if(!value&&workspace is not null)LogoutNow(); Changed(); } }
    public bool TradeAccepted { get => tradeAccepted; set { Set(ref tradeAccepted,value); if(!value&&workspace is not null){operation?.Cancel();workspace.Access.Acknowledge(RiskAccepted,CredentialsAccepted,true,false);} Changed(); } }
    public bool CanCancel=>RiskAccepted&&CredentialsAccepted&&TradeAccepted&&workspace?.Executor.CanCancel==true;
    public bool CanTrade => RiskAccepted&&CredentialsAccepted&&TradeAccepted&&workspace?.Executor.CanExecute == true;
    public bool CanTradeReview=>CanTrade&&(PricingMode!=2||workspace?.Policy.MedianPriceContractSupported==true)&&StackReviewSupported;
    public string PricingContractText=>PricingMode==2?"이력 중앙값의 결제액/통화/기간 계약도 별도 확인 필요 · 기본 실제 제출 차단 · 추정은 정확한 거래 평균이 아닙니다.":"호가 기반 추정도 체결 보장 없음 · 실제 등록은 별도 허용/검증 전 차단";
    public bool Connected => workspace?.Authentication.Connected == true;
    public bool ReviewOpen { get => reviewOpen; private set { Set(ref reviewOpen,value); Changed(); } }
    public bool ManagementOpen { get => managementOpen; private set { Set(ref managementOpen,value);Raise(nameof(ManagementActionText)); Changed(); } }
    public string ManagementActionText=>managementReprice?"전체 재등록 승인":"선택 전체 취소 승인";
    public bool AnyReview => ReviewOpen || ManagementOpen;
    public SteamLoginState LoginState { get => state; private set { var old=state;Set(ref state,value);if(value==SteamLoginState.Connected&&old!=value&&!syntheticConnection){imagesEnabled=true;Raise(nameof(ImagesEnabled));}if(old!=value)DiagnosticHub.Emit(DiagnosticEvent.AuthState,DiagnosticArea.Authentication,from:old.ToString(),to:value.ToString(),requirement:"A03"); Raise(nameof(Connected));Raise(nameof(ImageLoader)); Changed(); } }
    public bool[,]? QrMatrix { get => matrix; private set => Set(ref matrix,value); }
    public string Status { get => status; private set => Set(ref status,value); }
    public string EvidenceText { get => evidence; private set => Set(ref evidence,value); }
    public string HistoryText { get => history; private set => Set(ref history,value); }
    public string CurrencyText => fees is null ? workspace?.WalletObservation?.Currency is {} currency?$"마지막 지갑 관측 {currency} · {workspace.WalletObservedAt:HH:mm} UTC · 판매 수수료 미확인":"지갑 통화 미조회" : $"지갑 {fees.Currency} · 최소 {Money.FromWire(fees.MinimumWire,fees.Currency)} · 가격 증분 {Money.FromWire(fees.IncrementWire,fees.Currency)} · 서버 설정";
    public string TradingText => CanTrade ? (syntheticConnection?"합성 계약 실행 · 실제 Steam 거래 없음":"비공식 실제 거래 모드 · 이 세션 한정 · 제재 위험 있음") : CanCancel?"실제 취소 전용 · 새 등록·재가격 OFF · 이 세션 한정 · 비공식 연동 제재 위험 있음":TradeAccepted&&!syntheticConnection?"실거래 모드 기본 선택 · 계정·수수료·자산 검증 대기 · 아직 전송 권한 없음":"실제 거래 OFF · 서버 계약 확인과 명시적 세션 활성화가 필요합니다.";
    public bool ReviewAmountsValid {get{try{if(ReviewRows.Any(x=>!x.Valid))return false;_=ReviewRows.Sum(x=>checked(x.Quote.BuyerPays*x.Quantity));_=ReviewRows.Sum(x=>checked(x.Quote.Fees*x.Quantity));_=ReviewRows.Sum(x=>checked(x.Quote.SellerReceives*x.Quantity));return true;}catch(Exception error)when(error is ArgumentException or OverflowException){return false;}}}
    public string Totals => fees is null || !ReviewAmountsValid ? "가격·수량·시세 또는 합계 범위를 확인하세요." :
        $"구매자 {new Money(ReviewRows.Sum(x=>checked(x.Quote.BuyerPays*x.Quantity)),fees.Currency)} · 수수료 {new Money(ReviewRows.Sum(x=>checked(x.Quote.Fees*x.Quantity)),fees.Currency)} · 실수령 {new Money(ReviewRows.Sum(x=>checked(x.Quote.SellerReceives*x.Quantity)),fees.Currency)}";
    public string ManagementSummary => string.Join("\n",management.Select(x=>
    {
        if(!managementReprice)return $"{x.Listing.HashName} · {x.Listing.Quantity}개 · 판매 취소";
        if(!gameFees.TryGetValue(x.Listing.Asset.AppId,out var schedule))return $"{x.Listing.HashName} · 수수료 미확인 · 게임별 수수료를 다시 조회하세요.";
        var q=schedule.FromBuyer(x.Price);
        return $"{x.Listing.HashName} · {x.Listing.Quantity}개 · 구매자 단가 {new Money(q.BuyerPays,x.Listing.Currency)} · 실수령 {new Money(q.SellerReceives,x.Listing.Currency)} · Steam {new Money(q.SteamFee,x.Listing.Currency)} / 게임 {new Money(q.GameFee,x.Listing.Currency)}"+"\n"+(Listings.FirstOrDefault(r=>r.Listing.ListingId==x.Listing.ListingId)?.ComparisonFor(q)??"");
    }));
    private async Task Run(Func<CancellationToken,Task> action,[System.Runtime.CompilerServices.CallerMemberName]string stage="")
    {
        if(Busy)return;
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.UiCommand,DiagnosticArea.UI,"G08",stage);
        LastOperationId=DiagnosticHub.Context?.OperationId;Raise(nameof(LastOperationId));
        OperationLabel=stage switch{nameof(BeginAsync)=>"QR 인증 요청 중",nameof(CheckAsync)=>"모바일 승인 확인 중",nameof(RestoreRememberedAsync)=>"저장 로그인 복원 중",nameof(LoadAsync)=>"보관함·수수료·판매목록 조회 중",nameof(SubmitAsync)=>"판매등록 전 서버 재검증 중",nameof(OpenTradingReviewAsync)=>"계정·수수료·자산 검증 중",nameof(LoadFeesAsync)=>"지갑·수수료 조회 중",nameof(LoadWalletAsync)=>"지갑 통화 조회 중",nameof(LoadListingsAsync)=>"판매목록 조회 중",nameof(RefreshPricesAsync)=>"선택한 아이템 시세 조회 중",nameof(ManageAsync)=>"검토한 관리 요청 처리 중",nameof(CancelForNewReviewAsync)=>"새 대상 검토를 위한 전체 취소 중",nameof(ReviewCancelledTargetsAsync)=>"취소 후 새 판매 대상 조회 중",nameof(PrepareListingRepriceAsync)=>"일괄 재등록 가격 계산 중",_=>"요청 처리 중"};Busy=true;operation=new();authenticating=stage is nameof(BeginAsync) or nameof(CheckAsync) or nameof(RestoreRememberedAsync);
        var read=ReadFor(stage);read?.Update(SteamReadState.Loading);Raise(nameof(InventoryEmptyTitle));Raise(nameof(InventoryEmptyHint));if(read==PricesRead&&PricingMode==2)HistoryRead.Update(SteamReadState.Stale);
        try{await action(operation.Token);read?.Update(read==InventoryRead&&Assets.Count==0||read==ListingsRead&&!HasServerListings?SteamReadState.Empty:SteamReadState.Ready);}
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
        workspace.Authentication.RememberRequested=RememberLogin;
        LoginState = SteamLoginState.RequestingQr; Status = "공식 Steam 앱으로 QR을 스캔하세요. 외부 QR 서비스는 사용하지 않습니다.";
        await workspace.Authentication.BeginAsync(token); token.ThrowIfCancellationRequested();
        UpdateChallenge(); LoginState=SteamLoginState.AwaitingMobile;
    });
    public Task CheckAsync() => Run(async token =>
    {
        if (workspace is null) throw new SteamException(SteamFailure.Disabled);
        if (await workspace.Authentication.CheckAsync(token)) { token.ThrowIfCancellationRequested(); QrMatrix=null; displayedChallenge=null; LoginState=SteamLoginState.Connected; Status="Steam 인증 성공 · 실제 읽기 연결 · 거래는 별도 차단"; RestoreCatalog();RefreshRequests();RecordCheck("AuthTransfer",1);SaveLoginIfRequested(); }
        else { UpdateChallenge(); Status="모바일 승인 대기 · 갱신된 QR은 화면에 반영 · 서버 간격 이전에는 추가 요청하지 않습니다."; }
    });
    public Task LoadAsync() => Run(async token =>
    {
        RequireConnection(); workspace!.SetGame(Game.AppId,Game.ContextId,Game.Name);
        var assets=await workspace.Inventory.GetAsync(token);token.ThrowIfCancellationRequested();
        foreach(var key in assetPublisherFees.Keys.Where(x=>x.AppId==Game.AppId&&x.ContextId==Game.ContextId).ToArray())assetPublisherFees.Remove(key);
        foreach(var asset in assets)if(workspace.Inventory.PublisherFeeBasisPoints(asset.Key) is {} rate)assetPublisherFees[asset.Key]=rate;
        RememberSelection();
        Bulk(()=>Assets.ReplaceAll(assets.Select(asset=>{var row=new SteamAssetRow(asset,asset.Quantity,Changed){Image=workspace.Inventory.Images.GetValueOrDefault(asset.Key),ImageOperation=DiagnosticHub.Context?.OperationId};if(selections.TryGetValue(asset.Key,out var saved)){row.Quantity=saved.Quantity;row.Selected=saved.Selected;}return row;})));RetainSelection(true);Filter();
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
        foreach(var group in groups){var quote=mode==2?await workspace!.Prices.RefreshEstimateAsync(group.First().Asset,window,token):await workspace!.Prices.RefreshAsync(group.First().Asset,token);token.ThrowIfCancellationRequested();if(revision!=priceRevision)return;if(fees is not null&&quote.Currency!=fees.Currency)throw new SteamException(SteamFailure.Restricted);Bulk(()=>{foreach(var row in group)row.SetPrice(quote);});RecordCheck("Orderbook",quote.Bids.Length,quote.Currency);if(quote.Estimate is {} observed)RecordCheck("History",observed.Buckets,observed.Currency);}
        Status="Steam 호가 갱신 완료 · 매수 잔량은 동일 상품 전체가 공유 · 체결 보장 없음";
    });
    public void BuildReview()
    {
        RequireConnection();
        relistReview=null;ReviewContextText="";
        if(fees is null||HasQuantityInputErrors||PricingMode==1&&Assets.Where(x=>x.Selected).Any(x=>x.Price is null||x.Price.Trades.IsEmpty))throw new SteamException(SteamFailure.Restricted);
        var selected=SelectedSteamRows();foreach(var row in selected)_=FeeFor(row.Asset);if(selected.Select(x=>gameFees.GetValueOrDefault(x.Asset.Key.AppId)?.Currency).Distinct().Count()!=1||selected.Any(x=>!gameFees.ContainsKey(x.Asset.Key.AppId)))throw new SteamException(SteamFailure.Restricted);if(selected.Any(x=>x.Quantity>x.Available || x.Price is null))throw new SteamException(SteamFailure.Restricted);
        var basis=PricingMode switch{1=>MarketCrate.Core.PriceBasis.ExactAverage,2=>MarketCrate.Core.PriceBasis.MedianEstimate,_=>MarketCrate.Core.PriceBasis.Immediate};
        var plan=SalePlanner.Build(selected.Select(x=>new SaleSelection(x.Asset,x.Price!,x.Quantity)),basis);
        var liquidity=basis==MarketCrate.Core.PriceBasis.Immediate?plan:SalePlanner.Build(selected.Select(x=>new SaleSelection(x.Asset,x.Price!,x.Quantity)),true);
        ReviewLiquidityText=$"조회 시점 원래 호가의 매수 잔량 부족 {liquidity.UnpricedQuantity}개 · 제안/수동 가격의 체결 보장 없음";Raise(nameof(ReviewLiquidityText));ReviewRows.Clear();
        foreach(var line in plan.Lines)ReviewRows.Add(new(line.Selection.Asset,line.Allocation.Quantity,line.Allocation.BuyerPays,line.Selection.Price,FeeFor(line.Selection.Asset),Changed,basis,LowerPriceOneStep){Image=selected.First(x=>x.Asset.Key==line.Selection.Asset.Key).Image,ImageOperation=DiagnosticHub.Context?.OperationId});
        foreach(var line in plan.Unpriced)ReviewRows.Add(new(line.Selection.Asset,line.Quantity,0,line.Selection.Price,FeeFor(line.Selection.Asset),Changed,basis,LowerPriceOneStep){Image=selected.First(x=>x.Asset.Key==line.Selection.Asset.Key).Image,ImageOperation=DiagnosticHub.Context?.OperationId});
        DiagnosticHub.Emit(DiagnosticEvent.ReviewOpened,DiagnosticArea.Pricing,requirement:"E01",count:ReviewRows.Count);
        ReviewOpen=true;Status=$"매수 잔량 부족 {liquidity.UnpricedQuantity}개 · 수동 가격은 이 검토 안에서만 유지 · 단일 최종 승인";Changed();
    }
    public Task SubmitAsync() => Run(async token =>
    {
        RequireConnection();if(!CanTradeReview || !ReviewOpen || ReviewRows.Count==0 || !ReviewAmountsValid)throw new SteamException(SteamFailure.Disabled);
        var lines=ReviewRows.Select((x,i)=>x.Snapshot(i)).ToImmutableArray();
        if(workspace!.Policy.StackAmountsSupported!=true)
            lines=lines.GroupBy(x=>x.Asset.Key).SelectMany(group=>
            {
                var first=group.First();
                return first.Asset.Stackable&&workspace.Policy.SingleRequestStackApps.Contains(first.Asset.Key.AppId)&&group.All(x=>x.Asset==first.Asset&&x.Currency==first.Currency&&x.UnitQuote==first.UnitQuote&&x.MarketTime==first.MarketTime&&x.OrderbookDigest==first.OrderbookDigest&&x.Evidence==first.Evidence)
                    ? new[]{first with{Quantity=checked(group.Sum(x=>x.Quantity))}}:group.ToArray();
            }).ToImmutableArray();
        var batch=new SaleBatchSnapshot(Guid.NewGuid(),workspace.Authentication.SteamId!,DateTimeOffset.UtcNow,lines);
        if(relistReview is {} relist)
        {
            if(relist.Account!=workspace.Authentication.SteamId||workspace.Store.RelistPlans(relist.Account).Single(x=>x.Key==relist.Key).SubmissionBatch is not null)throw new SteamException(SteamFailure.Restricted);
            var current=await workspace.Orders.RefreshAsync(token);
            if(relist.Listings.Any(x=>current.Any(now=>now.ListingId==x.ListingId)))throw new SteamException(SteamFailure.StaleReview);
            workspace.Store.StageRelistApproval(relist.Key,batch);relistReview=null;ReviewOpen=false;
        }
        ReviewOpen=false;
        try
        {
            WorkPage=3;
            var result=await workspace.Batches.ExecuteAsync(batch,new BatchUiProgress(this),token);token.ThrowIfCancellationRequested();SetResults(result);ReviewOpen=false;InventoryRead.Update(SteamReadState.Stale);RefreshRequests();Status="배치 요청 결과 · 접수/승인 대기는 게시 완료가 아닙니다.";if(workspace.Failure is {} failure)Error(new SteamException(failure));if(result.Results.Any(x=>x.State==SaleState.Unknown))Status="일부 등록 결과가 불명확합니다. 해당 항목은 Steam Guard 승인 대기로 확인되지 않았습니다. 재전송하지 말고 판매 결과의 오류와 공식 장터 상태를 확인하세요.";
        }
        finally{workspace.Store.ClearRelistApproval(batch.BatchId);}
    });
    public Task RecoverAsync() => Run(async token=>{RequireConnection();var stored=await workspace!.Store.LoadAsync(token);var record=stored?.Snapshot.AccountReference==workspace.Authentication.SteamId?await workspace.Batches.RecoverAsync(token):null;var listings=await workspace.Orders.RefreshAsync(token);token.ThrowIfCancellationRequested();if(record is not null)SetResults(record,true);SetListings(listings);await RefreshRepriceResultsAsync(token);RefreshRequests();Status="서버 상태 대조 완료 · 최근 재등록 최대10건 포함 · 불명 요청을 재전송하지 않았습니다. 이전 항목은 요청을 선택해 직접 대조하세요.";});
    public Task InspectAsync() => Run(async token=>
    {
        RequireConnection();if(SelectedRequest is null)throw new SteamException(SteamFailure.Restricted);
        var report=await workspace!.Executor.InspectAsync(SelectedRequest,token);
        token.ThrowIfCancellationRequested();
        var command=workspace.Store.ReadCommand(SelectedRequest)!;
        EvidenceText=$"요청 시각 {command.SentAt:O} · 대상 {command.Line.Asset.Key.AppId}/{command.Line.Asset.Key.ContextId}/{command.Line.Asset.Key.AssetId} · {command.Line.Quantity}개 · 실수령 단가 {new Money(command.Line.UnitQuote.SellerReceives,command.Line.Currency)}\n"+
            $"요청 전 목록 ID {(command.BeforeListingIds is null?"기록 없음":string.Join(',',command.BeforeListingIds.Take(10)))}\n판정 {report.Verdict} · {report.CheckedAt:O}\n"+string.Join("\n",report.Candidates.Select(x=>$"등록ID {x.ListingId} · {x.Reason} · {x.Quantity}개 · 실수령 {new Money(x.SellerReceives,command.Line.Currency)} · 시각 {x.CreatedAt:O}"))+
            "\n현재 동일 자산의 Steam 상태: "+(report.ObservedAssetState is {} observed?SteamListingExecutor.ObservedLabel(observed)+" (이전 요청 귀속은 미확인)":"확인 증거 없음")+"\n감사 이력: "+string.Join(" → ",workspace.Store.AuditTrail(SelectedRequest).Select(x=>$"{x.At:HH:mm:ss} {x.Action}/{x.State}"))+"\n후보 일치는 인과관계 증명이 아닙니다. 자동 확정·재전송하지 않습니다.";
    });
    private Task HistoryAsync()=>Run(async token=>{RequireConnection();var row=SelectedSteamRows().First();var points=await workspace!.Prices.GetMedianHistoryAsync(row.Asset,token);token.ThrowIfCancellationRequested();HistoryText=$"Steam pricehistory · 수집 {DateTimeOffset.UtcNow:O} · {points.Length}개 시간 구간의 중앙값/거래량 · 정확한 거래 원자료/VWAP/전체 체결 중앙값 아님\n"+string.Join("\n",points.TakeLast(6).Select(x=>$"{x.At:MM-dd HH:mm} UTC · median {x.MedianMajor} · volume {x.Volume}"));});
    private void TryManagement(bool reprice){try{BuildManagement(reprice);}catch(Exception error){DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.Management,DiagnosticOutcome.Failed,LogLevel.Warning,DiagnosticError.Validation,exception:error);Status="관리 검토 오류: 통화/가격/수수료를 확인하세요.";Changed();}}
    private void BuildManagement(bool reprice)
    {
        RequireConnection();
        if(reprice&&LowerPriceOneStep&&Listings.Where(x=>x.Selected).Any(x=>!x.HasStepProposal))throw new SteamException(SteamFailure.Restricted);
        if(reprice&&Listings.Where(x=>x.Selected).Any(x=>!gameFees.ContainsKey(x.Listing.Asset.AppId)))throw new SteamException(SteamFailure.Restricted);revisionSource=null;managementReprice=reprice;
        management=Listings.Where(x=>x.Selected).Select(x=>(x.Listing,reprice?SteamReviewRow.ParsePrice(x.NewPriceText,x.Listing.Currency):0,
            reprice?workspace?.Store.Reprices().FirstOrDefault(p=>p.Listing.ListingId==x.Listing.ListingId)?.Key??Guid.NewGuid().ToString("N"):Guid.NewGuid().ToString("N"))).ToArray();
        managementBooks.Clear();if(reprice)foreach(var row in management){var price=workspace!.Prices.Get(ListingAsset(row.Listing));if(DateTimeOffset.UtcNow-price.CapturedAt>TimeSpan.FromMinutes(5))throw new SteamException(SteamFailure.StaleReview);managementBooks[row.Key]=OrderbookFingerprint.Of(price);var schedule=gameFees[row.Listing.Asset.AppId];if(row.Listing.Currency!=schedule.Currency)throw new SteamException(SteamFailure.Restricted);_=schedule.FromBuyer(row.Price);}
        var recorded=workspace!.Store.Commands().Where(x=>x.Account==workspace.Authentication.SteamId&&x.Kind=="sell"&&x.ListingId is not null&&!x.Line.Asset.Stackable).Select(x=>x.ListingId).ToHashSet(StringComparer.Ordinal);
        managementReturnSupported=management.All(x=>x.Listing.Quantity==1&&recorded.Contains(x.Listing.ListingId));
        ManagementEligibility=reprice&&!managementReturnSupported?"자동 반환 확정 불가 · 외부 등록은 원래 자산의 반환 인과 증거가 없습니다. 개별 자산은 취소 후 새 대상 재검토를 선택할 수 있습니다. 새 판매 대상·가격은 다시 전체 승인해야 하며 스택은 미지원입니다.":"최종 승인 전 전체 가격·서버 상태를 검증합니다. 취소·반환이 불명확하면 후속 재등록을 중단합니다.";
        if(reprice&&!managementReturnSupported)DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Management,DiagnosticOutcome.Blocked,LogLevel.Warning,DiagnosticError.EvidenceMissing,requirement:"B09",endpoint:"RepriceReturn",reason:"NoCausalEvidence",count:management.Length);
        managementDeadline=reprice?management.Min(x=>workspace!.Prices.Get(ListingAsset(x.Listing)).CapturedAt).AddMinutes(5):DateTimeOffset.MaxValue;ManagementOpen=true;Raise(nameof(ManagementSummary));
    }
    private Task ManageAsync()=>Run(async token=>
    {
        RequireConnection();if(managementReprice?!CanTrade:!CanCancel)throw new SteamException(SteamFailure.Disabled);if(managementReprice&&DateTimeOffset.UtcNow>managementDeadline)throw new SteamException(SteamFailure.StaleReview);
        if(managementReprice&&revisionSource is null)await workspace!.Executor.ValidateRepriceBatchAsync(management.Select(x=>(x.Listing,x.Price,managementBooks[x.Key])).ToArray(),token);
        Results.Clear();foreach(var row in management)Results.Add(new(row.Listing.ListingId,SaleState.Ready,"미전송"));
        if(!managementReprice){var cancelled=await workspace!.Executor.CancelBatchAsync(management.Select(x=>x.Listing).ToArray(),Guid.NewGuid().ToString("N"),token);Results.Clear();foreach(var item in cancelled)Results.Add(item);ManagementOpen=false;RefreshRequests();Status="취소 요청 결과 · 반환 확정 전 재등록 금지";return;}
        for(var index=0;index<management.Length;index++)
        {
            if(DateTimeOffset.UtcNow>managementDeadline)throw new SteamException(SteamFailure.StaleReview);var row=management[index];OperationLabel=$"재등록 처리 {index+1}/{management.Length} · 취소/반환 대조";
            try
            {
                var intent=revisionSource is null?await workspace!.Executor.RepriceAsync(row.Listing,row.Price,row.Key,token,managementBooks.GetValueOrDefault(row.Key)):await workspace!.Executor.ReviseRepriceAsync(revisionSource,row.Price,row.Key,token,managementBooks.GetValueOrDefault(row.Key));token.ThrowIfCancellationRequested();
                var state=intent.ReplacementKey is {} replacement?workspace!.Store.ReadCommand(replacement)?.State??SaleState.Unknown:SaleState.Unknown;
                Results[index]=new(row.Listing.ListingId,state,"재등록 · "+SteamStatusText.Label(state)+" · 취소/반환 근거는 작업 이력에서 확인하세요.");
                if(Results[index].State==SaleState.Unknown)break;
            }
            catch
            {
                var state=SaleState.Unknown;
                try{var intent=workspace!.Store.Reprice(row.Key);if(intent?.ReplacementKey is {} replacement)state=workspace.Store.ReadCommand(replacement)?.State??SaleState.Unknown;else if(revisionSource is null&&workspace.Store.ReadCommand(row.Key+":cancel")is null)state=SaleState.Ready;}catch(IOException){}
                Results[index]=new(row.Listing.ListingId,state,state==SaleState.Ready?"전송 전 중단 · 상태/가격을 다시 검토하세요.":"중단 · 저널과 서버 대조 필요 · 자동 재전송하지 않음");ManagementOpen=false;WorkPage=3;RefreshRequests();throw;
            }
        }
        ManagementOpen=false;WorkPage=3;RefreshRequests();Status="관리 결과 · 취소/반환 대조가 불명확하면 재등록하지 않습니다.";if(workspace?.Failure is {} failure)Error(new SteamException(failure));
    });
    private void RequireConnection(){if(!Connected)throw new SteamException(SteamFailure.Authentication);if(workspace!.Failure is {} blocked)throw new SteamException(blocked);}
    private Task ResumeRepriceAsync()=>Run(async token=>
    {
        RequireConnection();var intent=workspace!.Store.Reprice(SelectedReprice??"")??throw new SteamException(SteamFailure.Restricted);
        var result=await workspace.Executor.RepriceAsync(intent.Listing,intent.BuyerPrice,intent.Key,token);token.ThrowIfCancellationRequested();await RefreshRepriceResultsAsync(token,result.Key);Status="보존한 재가격 의도 대조 완료 · 접수와 서버 게시 상태를 구분하세요.";RefreshRequests();
    });
    private void SetListings(IEnumerable<SteamListing> rows){var listingRows=rows.ToArray();foreach(var result in workspace?.Executor.ObserveListings(listingRows)??[]){var index=Results.ToList().FindIndex(x=>x.LineId==result.LineId);if(index<0)Results.Add(result);else Results[index]=result;}ObserveCatalog(listingRows);RememberSelection();Bulk(()=>Listings.ReplaceAll(listingRows.Select(row=>new SteamListingRow(row,Changed){Image=workspace?.Orders.Images.GetValueOrDefault(row.ListingId),ImageOperation=DiagnosticHub.Context?.OperationId,Selected=listingSelections.GetValueOrDefault(row.ListingId)})));Filter();Changed();}
    private void UpdateObservedStates(BatchRecord record)
    {
        observedPostedLines.Clear();observedPostedAt=null;
        if(workspace is null||record.Snapshot.AccountReference!=workspace.Authentication.SteamId)return;
        var byLine=record.Results.ToDictionary(x=>x.LineId);
        foreach(var line in record.Snapshot.Lines)
        {
            var result=byLine[line.LineId];
            if(result.State is not(SaleState.Unknown or SaleState.AwaitingSteamGuard))continue;
            var key=SaleBatchCoordinator.AttemptKey(record.Snapshot,line,result.Attempt);
            var evidence=workspace.Store.Evidence(key);
            if(evidence is not{ObservedAssetState:SaleState.Listed}||evidence.CheckedAt>DateTimeOffset.UtcNow.AddSeconds(5)||DateTimeOffset.UtcNow-evidence.CheckedAt>TimeSpan.FromMinutes(5))continue;
            if(workspace.Store.ReadCommand(key) is not{ListingId:null})continue;
            observedPostedLines.Add(line.LineId);observedPostedAt=evidence.CheckedAt;
        }
    }
    private void SetResults(BatchRecord record,bool refreshObservations=false)
    {
        if(refreshObservations)UpdateObservedStates(record);else{observedPostedLines.Clear();observedPostedAt=null;}
        if(Results.Count!=record.Results.Length||!Results.Select(x=>x.LineId).SequenceEqual(record.Results.Select(x=>x.LineId))){Results.Clear();foreach(var result in record.Results)Results.Add(result);}
        else for(var index=0;index<record.Results.Length;index++)if(Results[index]!=record.Results[index])Results[index]=record.Results[index];
        Raise(nameof(BatchProgressText));Raise(nameof(ApprovalHandoffText));Raise(nameof(HasApprovalHandoff));Raise(nameof(ServerPostedObservationCount));Raise(nameof(ListingMetadataSummary));Raise(nameof(HasServerListings));
    }
    private sealed class BatchUiProgress(SteamWorkspaceViewModel owner):IProgress<BatchRecord>
    {
        // Coordinator resumes on the caller's WPF context; no queued late Progress<T> callbacks.
        public void Report(BatchRecord record)
        {
            if(owner.logoutRequested||!owner.Connected||owner.workspace?.Authentication.SteamId!=record.Snapshot.AccountReference)return;
            owner.SetResults(record);owner.OperationLabel="전체 판매등록 처리 중 · "+owner.BatchProgressText;
        }
    }
    public string BatchProgressText=>$"전체 {Results.Count}건 · 미전송 {Results.Count(x=>x.State==SaleState.Ready)} · 처리 중 {Results.Count(x=>x.State==SaleState.Submitting)} · 접수 {Results.Count(x=>x.State==SaleState.Submitted)} · 요청 기록의 승인 대기 {Results.Count(x=>x.State==SaleState.AwaitingSteamGuard)} · 게시 {Results.Count(x=>x.State==SaleState.Listed)} · 현재 게시 관측 {ServerPostedObservationCount} · 실패 {Results.Count(x=>x.State==SaleState.Failed)} · 불명 {Results.Count(x=>x.State==SaleState.Unknown)}";
    private void RefreshRequests()
    {
        var previous=SelectedRequest;
        RefreshPendingRelists();Requests.Clear();RequestOptions.Clear();RepriceKeys.Clear();
        if(workspace is null)return;
        foreach(var command in workspace.Store.Commands().Where(x=>x.Account==workspace.Authentication.SteamId).OrderByDescending(x=>x.SentAt))
        {
            Requests.Add(command.RequestKey);
            var kind=command.Kind=="cancel"?"취소":"등록";
            RequestOptions.Add(new(command.RequestKey,$"{kind} · {command.Line.Asset.Name} · {SteamStatusText.Label(command.State)} · {command.SentAt:MM-dd HH:mm} UTC"));
        }
        foreach(var plan in workspace.Store.Reprices().Where(x=>x.Account==workspace.Authentication.SteamId))RepriceKeys.Add(plan.Key);
        SelectedRequest=RequestOptions.Any(x=>x.Key==previous)?previous:RequestOptions.FirstOrDefault()?.Key;
        var available=workspace.Store.AvailableAll(Assets.Select(x=>x.Asset).ToArray(),workspace.Authentication.SteamId);
        Bulk(()=>{foreach(var row in Assets)row.UpdateSubmissionHold(Math.Max(0,row.Owned-available[row.Asset.Key]));});
        DiagnosticHub.Emit(DiagnosticEvent.InventoryContract,DiagnosticArea.Data,HasPendingInventoryRequests?DiagnosticOutcome.Blocked:DiagnosticOutcome.Completed,endpoint:"Inventory",reason:"InventoryLocalPendingRows",count:Assets.Count(x=>x.PendingSubmissionQuantity>0),requirement:"C10");
        DiagnosticHub.Emit(DiagnosticEvent.InventoryContract,DiagnosticArea.Data,endpoint:"Inventory",reason:"InventorySelectableRows",count:Assets.Count(x=>x.CanSelect),requirement:"C10");
    }
    private void ClearData(){ResetPriceAdjustment();observedPostedLines.Clear();observedPostedAt=null;LastFailedOperationId=null;Raise(nameof(DiagnosticOperationId));relistReview=null;ReviewContextText="";PendingRelists.Clear();SelectedRelist=null;tradeAccepted=!syntheticConnection;Raise(nameof(TradeAccepted));ResetDevelopmentTools();foreach(var read in ReadStatuses)read.Update(SteamReadState.NotQueried);readChecks.Clear();displayedChallenge=null;Raise(nameof(ContractSummary));ResetCatalog();gameFees.Clear();assetPublisherFees.Clear();TradingConfirmation=syntheticConnection?"":"실제 거래";TradingPreparation="실거래 모드 기본 선택 · 새 연결의 전송 권한은 서버 계약 확인 후 준비됩니다.";inventoryViews.Clear();retainedSelections.Clear();selections.Clear();listingSelections.Clear();VisibleAssets.Clear();VisibleListings.Clear();QrMatrix=null;Assets.Clear();Listings.Clear();ReviewRows.Clear();Results.Clear();Requests.Clear();RequestOptions.Clear();RepriceKeys.Clear();SelectedRequest=SelectedReprice=null;fees=null;ReviewOpen=ManagementOpen=false;managementBooks.Clear();EvidenceText="대조 증거 없음";HistoryText="중간가격 이력 미조회 (정확한 거래 VWAP 아님)";Raise(nameof(CurrencyText));}
    private void LogoutNow()=>EndSession(true);
    private void EndSession(bool forget){if(forget)DeleteRememberedLogin();imagesEnabled=false;Raise(nameof(ImagesEnabled));Raise(nameof(ImageLoader));DiagnosticHub.Emit(DiagnosticEvent.Logout,DiagnosticArea.Authentication,requirement:"A05");var cancelling=Busy||LoginState is SteamLoginState.AwaitingMobile or SteamLoginState.RequestingQr;operation?.Cancel();workspace?.Authentication.Dispose();ClearData();tradeAccepted=false;Raise(nameof(TradeAccepted));LoginState=cancelling?SteamLoginState.Cancelled:SteamLoginState.Disconnected;Status=forget?"로그아웃 · 로컬 저장 로그인 삭제 요청 · 거래 저널 보존":"앱 종료 · 메모리 세션 종료 · 선택한 로그인 유지와 거래 저널 보존";if(Busy)logoutRequested=true;else{workspace?.Dispose();workspace=null;}Changed();}
    private void Error(Exception error)
    {
        LastFailedOperationId=DiagnosticHub.Context?.OperationId??LastOperationId;Raise(nameof(DiagnosticOperationId));
        if(authenticating||error is SteamException{Code:SteamFailure.Authentication or SteamFailure.Expired})PauseSavedLogin();
        DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.UI,DiagnosticOutcome.Failed,LogLevel.Error,error is SteamException se?se.Code switch{SteamFailure.Authentication or SteamFailure.Expired=>DiagnosticError.Authentication,SteamFailure.RateLimited=>DiagnosticError.RateLimited,SteamFailure.Transport=>DiagnosticError.Transport,SteamFailure.ContractChanged=>DiagnosticError.Schema,_=>DiagnosticError.Validation}:DiagnosticError.Validation,endpoint:(error as SteamException)?.Endpoint,reason:(error as SteamException)?.Endpoint,exception:error);
        if(authenticating&&workspace?.Authentication.LastFailureStage==SteamAuthStage.AccountValidation)
        {QrMatrix=null;LoginState=SteamLoginState.ContractFailure;}
        else if(error is SteamException {Code:SteamFailure.Authentication or SteamFailure.Expired})
        {workspace?.Authentication.Dispose();QrMatrix=null;ReviewOpen=ManagementOpen=false;LoginState=SteamLoginState.Expired;}
        else if(authenticating)
        {
            QrMatrix=null;
            LoginState=error is SteamException e?e.Code switch{SteamFailure.Disabled=>LoginState,SteamFailure.Denied=>SteamLoginState.Denied,SteamFailure.Transport=>SteamLoginState.NetworkFailure,SteamFailure.Forbidden or SteamFailure.RateLimited or SteamFailure.Challenge=>SteamLoginState.AccessBlocked,_=>SteamLoginState.ContractFailure}:SteamLoginState.ContractFailure;
        }
        if(!authenticating&&error is SteamException{TradingCheck:not null} tradingError){Status=TradingFailureText(tradingError);Changed();return;}
        if(!authenticating&&error is SteamException{Endpoint:"StackContract"}){Status="스택 수량 전송 계약이 검토 후 변경됐거나 확인되지 않았습니다. 판매 요청은 보내지 않았습니다. 새로 조회하고 검토하세요.";Changed();return;}
        if(!authenticating&&error is SteamException{Endpoint:"PendingSubmissionSettling"}){Status="직전 요청이 아직 서버에 반영 중일 수 있습니다. 전송 후 2분이 지난 뒤 보관함·판매목록을 갱신하고 새로 검토하세요. 이번 요청은 전송하지 않았습니다.";Changed();return;}
        if(!authenticating&&error is SteamException{Endpoint:"ServerInventory"}){Status="선택한 자산의 현재 보유 수량 또는 판매 가능 상태가 바뀌었습니다. 보관함을 갱신하고 새로 검토하세요. 이번 요청은 전송하지 않았습니다.";Changed();return;}
        if(!authenticating&&error is SteamException{Endpoint:"ServerListing"}){Status="선택 자산이 현재 판매 중이거나 승인 대기 목록에 있습니다. 판매목록에서 상태를 확인하세요. 중복 요청은 전송하지 않았습니다.";Changed();return;}
        if(!authenticating&&error is SteamException{Endpoint:"UncertainStack"}){Status="수량형 자산의 이전 요청과 현재 수량을 확정할 증거가 부족합니다. 스택의 중복 수량 전송은 차단했습니다.";Changed();return;}
        if(!authenticating&&error is SteamException{Endpoint:"PendingSubmission"}){Status="서버 보관함은 선택·가격 검토할 수 있지만 이전 전송 결과가 미확정입니다. 이전 요청 확인에서 동기화하세요. 자동 재전송은 하지 않습니다.";Changed();return;}
        if(!authenticating&&error is SteamException{Endpoint:"RepriceReturn"}){Status="일괄 재등록을 시작하지 않았습니다. 외부에서 등록된 항목은 원래 자산의 반환 인과 증거가 없어 자동 재등록을 지원하지 않습니다. 선택 취소는 별도 검토할 수 있습니다.";Changed();return;}
        if(!authenticating&&error is SteamException{Code:SteamFailure.StaleReview}){ReviewOpen=ManagementOpen=false;ReviewRows.Clear();managementBooks.Clear();Status="승인한 가격·수량·호가 자료가 변경되었거나 오래되었습니다. 갱신 후 새 최종 검토가 필요합니다. 자동으로 낮은 가격을 제출하지 않았습니다.";Changed();return;}
        Status=authenticating&&error is SteamException{Code:SteamFailure.Disabled}?"이 환경에서는 인증을 시작할 수 없습니다 (Disabled).":authenticating?"인증 작업 실패 · "+(workspace?.Authentication.LastFailureStage?.ToString()??"AuthBegin"):RequestsBlocked?"서버 제한 또는 안전 차단 · 추가 요청 중단":"이 조회를 완료하지 못했습니다. 다른 조회는 사용할 수 있습니다.";Changed();
    }
    private void Changed()
    {
        if(bulkUpdate)return;
        Raise(nameof(BatchProgressText));Raise(nameof(ApprovalHandoffText));Raise(nameof(HasApprovalHandoff));Raise(nameof(ServerPostedObservationCount));Raise(nameof(ListingMetadataSummary));Raise(nameof(HasServerListings));
        Raise(nameof(HasPendingInventoryRequests));Raise(nameof(InventorySyncSummary));OpenPendingInventoryRequests?.Refresh();
        Raise(nameof(InventoryEmptyTitle));Raise(nameof(InventoryEmptyHint));
        Raise(nameof(HasQuantityInputErrors));Raise(nameof(ReviewAmountsValid));Raise(nameof(CanChoosePriceAdjustment));Raise(nameof(ReviewAdjustmentSummary));
        Raise(nameof(CanChangeRememberLogin));DeleteSavedLogin?.Refresh();
        CompareOfficialFees?.Refresh();Raise(nameof(FeeComparisonAvailability));InvalidateFeeComparison();
        RefreshDevelopmentTools();
        Raise(nameof(Connected));Raise(nameof(SessionPresent));Raise(nameof(CanRead));Raise(nameof(RequestsBlocked));Raise(nameof(AuthenticationText));Raise(nameof(ConnectionSummary));Raise(nameof(RequestAccessText));Raise(nameof(SelectionSummary));Raise(nameof(SelectionEstimate));Raise(nameof(AnyReview));LoadListings?.Refresh();LoadFees?.Refresh();LoadWallet?.Refresh();OpenOfficialMarket?.Refresh();
        PrepareTrading?.Refresh();PrepareCancellation?.Refresh();Raise(nameof(CanCancel));Raise(nameof(Totals));Raise(nameof(TradingText));Raise(nameof(CanTrade));Raise(nameof(CanTradeReview));Raise(nameof(PricingContractText));Raise(nameof(SubmissionAvailability));
        BeginLogin?.Refresh();CheckLogin?.Refresh();Load?.Refresh();RefreshPrices?.Refresh();SelectAll?.Refresh();Review?.Refresh();CloseReview?.Refresh();Submit?.Refresh();EnableTrading?.Refresh();Recover?.Refresh();Inspect?.Refresh();MedianHistory?.Refresh();ReviewCancel?.Refresh();ReviewReprice?.Refresh();ConfirmManagement?.Refresh();
        ResumeReprice?.Refresh();ReviewStoredReprice?.Refresh();RefreshReviewStatus?.Refresh();ListingMetrics?.Refresh();PrepareListingReprice?.Refresh();
        CancelForNewReview?.Refresh();ReviewCancelledTargets?.Refresh();OpenInventory?.Refresh();OpenGameSettings?.Refresh();RefreshSelectedRequest?.Refresh();
        ClearSelection?.Refresh();ClearListings?.Refresh();
        SelectAllListings?.Refresh();ArchiveBatch?.Refresh();
        CancelOperation?.Refresh();AddScope?.Refresh();RemoveScope?.Refresh();Raise(nameof(ScopeSummary));
    }
    private void Bulk(Action action){bulkUpdate=true;try{action();}finally{bulkUpdate=false;Changed();}}
    public void Dispose()=>EndSession(false);
}

public sealed class SteamAssetRow(InventoryAsset asset,int available,Action changed):Observable
{
    public ItemImageReference? Image{get;init;}public Guid? ImageOperation{get;init;}
    public InventoryAsset Asset {get;}=asset;public string Name=>Asset.Name;public int Owned=>Asset.Quantity;public int Available {get;private set;}=available;public int Maximum=>Math.Max(1,Available);
    public bool CanSelect=>Asset.Marketable&&!Asset.Locked&&Available>0;public string Flags=>$"보유 {Owned} · 서버 수량 {Available} · {(Asset.Marketable?"장터 판매 가능":"장터 판매 불가")} · {(Asset.Tradable?"거래 가능":"거래 제한")}";
    public int PendingSubmissionQuantity{get;private set;}
    public string SelectionHint=>!Asset.Marketable?"Steam이 장터 판매 불가로 표시한 아이템입니다.":Asset.Locked?"현재 판매 제한이 있는 아이템입니다.":PendingSubmissionQuantity>0?"서버 보관함 기준으로 선택·가격 검토할 수 있습니다. 이전 요청의 결과가 미확정이면 새 전송 전에 상태 확인이 필요합니다. Steam의 예약 기능이 아닙니다.":"판매 수량을 정한 뒤 시세를 갱신하세요.";
    public void UpdateSubmissionHold(int quantity){PendingSubmissionQuantity=Math.Max(0,quantity);Raise(nameof(PendingSubmissionQuantity));Raise(nameof(SelectionHint));changed();}
    private bool selected;private int quantity=1;private string quantityText="1";
    public bool Selected{get=>selected;set{if(Set(ref selected,value&&CanSelect))changed();}}
    public bool QuantityInputValid=>int.TryParse(quantityText,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var value)&&value>=1&&value<=Maximum&&value==quantity;
    public string QuantityText{get=>quantityText;set{var wasValid=QuantityInputValid;if(!Set(ref quantityText,value))return;if(int.TryParse(value,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var parsed)&&parsed>=1&&parsed<=Maximum)Set(ref quantity,parsed,nameof(Quantity));Raise(nameof(QuantityInputValid));if(wasValid!=QuantityInputValid){using var scope=DiagnosticHub.Operation(DiagnosticEvent.UiSelection,DiagnosticArea.UI,"C07");DiagnosticHub.Emit(DiagnosticEvent.UiSelection,DiagnosticArea.UI,QuantityInputValid?DiagnosticOutcome.Succeeded:DiagnosticOutcome.Blocked,QuantityInputValid?LogLevel.Information:LogLevel.Warning,QuantityInputValid?DiagnosticError.None:DiagnosticError.Validation,requirement:"C07",reason:QuantityInputValid?"Committed":"InputRejected");}changed();}}
    public int Quantity{get=>quantity;set{var updated=Set(ref quantity,Math.Clamp(value,1,Maximum));updated|=Set(ref quantityText,quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),nameof(QuantityText));if(updated){Raise(nameof(QuantityInputValid));changed();}}}
    public PriceSnapshot? Price {get;private set;}public string PriceText=>Price?.Estimate is {} indicator?$"Steam pricehistory 추정 중앙값 {new Money(indicator.Minor,indicator.Currency)} (실제 평균 아님) · {indicator.Buckets}개 기간 / 거래량 {indicator.Volume} · {indicator.WindowStart:MM-dd HH:mm}–{indicator.WindowEnd:MM-dd HH:mm} UTC · 수집 {indicator.CapturedAt:HH:mm:ss}":Price is null?"시세 미조회":$"{PriceDisplay.Immediate} · 최고 구매주문  {(Price.Bids.IsEmpty?"매수 잔량 없음":new Money(Price.Bids[0].BuyerPays,Price.Currency).ToString())} · 최저 판매호가 정보 없음 · {Price.CapturedAt:HH:mm:ss} UTC";
    public void ClearPrice(){Price=null;Raise(nameof(PriceText));}
    public void SetPrice(PriceSnapshot price){Price=price;Raise(nameof(PriceText));changed();}
    public void UpdateAvailable(int value){Available=value;if(value==0)Selected=false;Quantity=Math.Clamp(quantity,1,Maximum);Raise(nameof(Available));Raise(nameof(Flags));Raise(nameof(SelectionHint));Raise(nameof(Maximum));Raise(nameof(CanSelect));Raise(nameof(Quantity));Raise(nameof(QuantityInputValid));changed();}
}
public sealed class SteamReviewRow:Observable
{
    private readonly SteamFeeSchedule fees;private readonly Action changed;private readonly PriceEvidence? priceEvidence;private readonly string bookDigest;private string text;private readonly long originalBid,recommended,reference;private readonly bool adjustmentUnavailable;
    public bool LowerStepRequested{get;}public FeeQuote? BaselineQuote{get;}public string Currency=>fees.Currency;
    public string AdjustmentComparison
    {
        get
        {
            if(!LowerStepRequested)return "";
            if(adjustmentUnavailable||BaselineQuote is null)return "한 단계 낮은 유효 가격을 계산할 수 없습니다. 최저 가격/호가 부족/수수료를 확인하고 선택 또는 옵션을 변경하세요.";
            try{return PriceComparisonText.Format(BaselineQuote,Quote,fees.Currency,Quantity);}catch(Exception error)when(error is ArgumentException or OverflowException){return "가격 입력/금액 범위를 확인해야 하향 전후 금액을 비교할 수 있습니다.";}
        }
    }
    public ItemImageReference? Image{get;init;}public Guid? ImageOperation{get;init;}
    public InventoryAsset Asset{get;}public int Quantity{get;}public DateTimeOffset CapturedAt{get;}public bool Insufficient{get;}public string Identity=>$"{Asset.Game} · {Asset.Name} · {Quantity}개";
    public string PriceText{get=>text;set{if(!Set(ref text,value))return;DiagnosticHub.Emit(DiagnosticEvent.PriceEdited,DiagnosticArea.Pricing,requirement:"E05",reason:Valid?"Committed":"InputRejected");Raise(nameof(Valid));Raise(nameof(Amounts));Raise(nameof(PriceBasis));Raise(nameof(PriceWarning));Raise(nameof(AdjustmentComparison));changed();}}
    public SteamReviewRow(InventoryAsset asset,int quantity,long buyer,PriceSnapshot price,SteamFeeSchedule fees,Action changed,MarketCrate.Core.PriceBasis basis=MarketCrate.Core.PriceBasis.Immediate,bool lowerOneStep=false)
    {bookDigest=OrderbookFingerprint.Of(price);priceEvidence=basis==MarketCrate.Core.PriceBasis.MedianEstimate?price.Estimate:null;Asset=asset;Quantity=quantity;CapturedAt=price.CapturedAt;Insufficient=buyer==0;this.fees=fees;this.changed=changed;originalBid=buyer;reference=price.Bids.IsEmpty?0:price.Bids[0].BuyerPays;LowerStepRequested=lowerOneStep;try{BaselineQuote=fees.FromBuyer(buyer,false);buyer=lowerOneStep?fees.PreviousValidBuyerPrice(BaselineQuote.BuyerPays).BuyerPays:BaselineQuote.BuyerPays;}catch(Exception error)when(error is ArgumentException or OverflowException){buyer=0;adjustmentUnavailable=lowerOneStep;}recommended=buyer;text=buyer==0?"":FormatPrice(buyer,fees.Currency);}
    public string PriceBasis=>adjustmentUnavailable?"한 단계 하향 불가 · 최저 가격·호가·수수료 확인 필요":priceEvidence is {} ev?$"Steam pricehistory · 기간 중앙값의 거래량 가중 중앙값 추정 · 정확한 평균/즉시거래가능가 아님 · {ev.Buckets}구간/서버 거래량 {ev.Volume} · {ev.WindowStart:MM-dd HH:mm}–{ev.WindowEnd:MM-dd HH:mm} UTC":Insufficient?"매수 잔량 부족 · 직접 가격 필요 · 예상 미체결 수량":$"{PriceDisplay.Immediate} · 호가 {new Money(originalBid,fees.Currency)} · 증분/수수료 반영 추천 {new Money(recommended,fees.Currency)} · {(text==FormatPrice(recommended,fees.Currency)?"추천":"수동 수정")}";
    public bool Valid{get{try{var quote=Quote;_=checked(quote.BuyerPays*Quantity);_=checked(quote.Fees*Quantity);_=checked(quote.SellerReceives*Quantity);return !adjustmentUnavailable&&Quantity>=1&&Quantity<=Asset.Quantity&&(priceEvidence is null||priceEvidence.Fresh(DateTimeOffset.UtcNow))&&!PriceOutlier&&DateTimeOffset.UtcNow-CapturedAt<=TimeSpan.FromMinutes(5)&&CapturedAt<=DateTimeOffset.UtcNow.AddSeconds(5);}catch(Exception e)when(e is ArgumentException or OverflowException){return false;}}}
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
    private FeeQuote? comparisonBaseline;
    public bool HasStepProposal=>comparisonBaseline is not null;
    public string ComparisonFor(FeeQuote actual)=>comparisonBaseline is {} baseline?PriceComparisonText.Format(baseline,actual,Listing.Currency,Listing.Quantity):"";
    public void ClearPriceComparison(){comparisonBaseline=null;Raise(nameof(HasStepProposal));Raise(nameof(Proposal));}
    public ItemImageReference? Image{get;init;}public Guid? ImageOperation{get;init;}
    private string metric="즉시거래가능가 미조회";public string Metric{get=>metric;set=>Set(ref metric,value);}
    public SteamListing Listing{get;}=listing;public string PriceSummary=>$"{Listing.Quantity}개 · {SteamStatusText.Label(Listing.State)} · 구매자 {new Money(Listing.SellerReceives+Listing.Fees,Listing.Currency)}";public string Description=>$"{Listing.HashName} · {PriceSummary}";
    private SteamFeeSchedule? proposalFees;
    private string newPriceText=SteamReviewRow.FormatPrice(listing.SellerReceives+listing.Fees,listing.Currency);
    private bool selected;public bool Selected{get=>selected;set{Set(ref selected,value);changed();}}
    public string NewPriceText{get=>newPriceText;set{if(Set(ref newPriceText,value)){Raise(nameof(Proposal));changed();}}}
    public string Proposal{get{if(proposalFees is null)return "재등록 가격 미계산";try{var quote=proposalFees.FromBuyer(SteamReviewRow.ParsePrice(NewPriceText,Listing.Currency));return $"새 단가 {new Money(quote.BuyerPays,Listing.Currency)} · 실수령 {new Money(quote.SellerReceives,Listing.Currency)} · Steam {new Money(quote.SteamFee,Listing.Currency)} / 게임 {new Money(quote.GameFee,Listing.Currency)}";}catch(Exception error)when(error is ArgumentException or OverflowException){return "금액/증분 오류 · 재등록 불가";}}}
    public void SetProposedPrice(long buyer,SteamFeeSchedule schedule,FeeQuote? baseline=null){proposalFees=schedule;comparisonBaseline=baseline;NewPriceText=SteamReviewRow.FormatPrice(buyer,Listing.Currency);Raise(nameof(Proposal));Raise(nameof(HasStepProposal));}
}
