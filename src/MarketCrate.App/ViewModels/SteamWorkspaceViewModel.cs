using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
using Net.Codecrete.QrCodeGenerator;

namespace MarketCrate.App.ViewModels;

public enum SteamLoginState { Disconnected, RequestingQr, AwaitingMobile, Connected, Expired, Denied, Cancelled, NetworkFailure, AccessBlocked, ContractFailure }
public sealed record SteamGameScope(int AppId, string ContextId, string Name) { public override string ToString() => Name; }

// Uses the same real adapters in production and injected synthetic HTTP in contract tests.
public sealed class SteamWorkspaceViewModel : Observable, IDisposable
{
    private readonly Func<SteamGameScope, SteamExperimentalWorkspace> factory;
    public string ModeText { get; }
    private SteamExperimentalWorkspace? workspace;
    private CancellationTokenSource? operation;
    private bool busy, riskAccepted, credentialsAccepted, tradeAccepted, reviewOpen, managementOpen, logoutRequested;
    private SteamLoginState state;
    private string status = "실험 연동 · 연결 안 됨 · 합성 데이터 없음", evidence = "요청을 선택하고 수동 대조를 실행하세요.", history = "중간가격 이력 미조회 (정확한 거래 VWAP 아님)";
    private bool[,]? matrix;
    private SteamFeeSchedule? fees;
    private SteamGameScope game;
    private (SteamListing Listing, long Price, string Key)[] management = [];
    private bool managementReprice;
    private bool bulkUpdate;
    public SteamWorkspaceViewModel(Func<SteamGameScope, SteamExperimentalWorkspace>? factory = null, bool contractTest = false)
    {
        ModeText=contractTest?"CONTRACT TEST · 합성 서버 · 실제 Steam 요청 없음":"STEAM EXPERIMENTAL · 실제 계정 읽기 · Mock과 분리";
        this.factory = factory ?? (scope => new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarketCrate", "SteamExperimental"), scope.AppId, scope.ContextId, scope.Name));
        game = Games[0];
        BeginLogin = new(BeginAsync, Error, () => !Busy && RiskAccepted && CredentialsAccepted);
        CheckLogin = new(CheckAsync, Error, () => !Busy && LoginState == SteamLoginState.AwaitingMobile);
        Logout = new(LogoutNow);
        Load = new(LoadAsync, Error, () => !Busy && Connected && !AnyReview);
        RefreshPrices = new(RefreshPricesAsync, Error, () => !Busy && Connected && !AnyReview && Assets.Any(x => x.Selected));
        SelectAll = new(() => Bulk(()=>{foreach(var row in Assets)row.Selected=row.CanSelect;}), () => !Busy && !AnyReview);
        Review = new(() => { try { BuildReview(); } catch(Exception e) { Error(e); } }, () => !Busy && !AnyReview && Assets.Any(x => x.Selected) && Assets.Where(x=>x.Selected).All(x=>x.Price is not null));
        CloseReview = new(() => { ReviewOpen = ManagementOpen = false; ReviewRows.Clear(); Changed(); }, () => !Busy);
        Submit = new(SubmitAsync, Error, () => !Busy && Connected && ReviewOpen && CanTrade && ReviewRows.Count > 0 && ReviewRows.All(x => x.Valid));
        EnableTrading = new(() => { workspace!.Access.Acknowledge(true, true, true, true); Changed(); }, () => !Busy && Connected && TradeAccepted && workspace?.Policy.Enabled == true);
        Recover = new(RecoverAsync, Error, () => !Busy && Connected && !AnyReview);
        Inspect = new(InspectAsync, Error, () => !Busy && Connected && SelectedRequest is not null);
        MedianHistory = new(HistoryAsync, Error, () => !Busy && Connected && Assets.Any(x => x.Selected));
        ReviewCancel = new(() => TryManagement(false), () => !Busy && !AnyReview && Listings.Any(x => x.Selected));
        ReviewReprice = new(() => TryManagement(true), () => !Busy && !AnyReview && Listings.Any(x => x.Selected));
        ConfirmManagement = new(ManageAsync, Error, () => !Busy && Connected && CanTrade && ManagementOpen);
        ResumeReprice = new(ResumeRepriceAsync,Error,()=>!Busy&&Connected&&CanTrade&&SelectedReprice is not null&&!AnyReview);
        SelectAllListings=new(()=>Bulk(()=>{foreach(var row in Listings)row.Selected=true;}),()=>!Busy&&!AnyReview&&Listings.Count>0);
        ArchiveBatch=new(()=>{try{workspace!.Store.ArchiveCompleted();Results.Clear();Status="확정 배치 보관 완료 · 거래 의도·증거·예약은 보존";}catch(Exception){Status="미확정 배치는 보관할 수 없습니다. 먼저 상태를 대조하세요.";}Changed();},()=>!Busy&&Connected&&!AnyReview&&Results.Count>0);
        CancelOperation=new(()=>operation?.Cancel(),()=>Busy);
    }
    public SteamGameScope[] Games { get; } = [new(730,"2","Counter-Strike 2"),new(570,"2","Dota 2"),new(440,"2","Team Fortress 2")];
    public SteamGameScope Game { get => game; set { if (!Busy && !AnyReview && Set(ref game, value)) { Assets.Clear(); Listings.Clear(); fees = null; Status = "게임 변경: 실제 데이터 갱신을 누르세요."; Changed(); } } }
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
    public bool Connected => LoginState == SteamLoginState.Connected && workspace?.Authentication.Connected == true;
    public bool ReviewOpen { get => reviewOpen; private set { Set(ref reviewOpen,value); Changed(); } }
    public bool ManagementOpen { get => managementOpen; private set { Set(ref managementOpen,value); Changed(); } }
    public bool AnyReview => ReviewOpen || ManagementOpen;
    public SteamLoginState LoginState { get => state; private set { Set(ref state,value); Raise(nameof(Connected)); Changed(); } }
    public bool[,]? QrMatrix { get => matrix; private set => Set(ref matrix,value); }
    public string Status { get => status; private set => Set(ref status,value); }
    public string EvidenceText { get => evidence; private set => Set(ref evidence,value); }
    public string HistoryText { get => history; private set => Set(ref history,value); }
    public string CurrencyText => fees is null ? "지갑 통화 미조회" : $"지갑 {fees.Currency} · 최소 {Money.FromWire(fees.MinimumWire,fees.Currency)} · 가격 증분 {Money.FromWire(fees.IncrementWire,fees.Currency)} · 서버 설정";
    public string TradingText => CanTrade ? "승인된 실험 환경 · 실제 배치 제출 가능" : "IntegrationBlocked · Valve 허용 근거 미확보, 실제 등록·취소·재가격 비활성";
    public string Totals => fees is null || ReviewRows.Any(x => !x.Valid) ? "가격/시세 입력을 확인하세요." :
        $"구매자 {new Money(ReviewRows.Sum(x=>checked(x.Quote.BuyerPays*x.Quantity)),fees.Currency)} · 수수료 {new Money(ReviewRows.Sum(x=>checked(x.Quote.Fees*x.Quantity)),fees.Currency)} · 실수령 {new Money(ReviewRows.Sum(x=>checked(x.Quote.SellerReceives*x.Quantity)),fees.Currency)}";
    public string ManagementSummary => string.Join("\n",management.Select(x=>$"{x.Listing.HashName} · {x.Listing.Quantity}개 · {(managementReprice ? "취소 확인 후 새 구매자 가격 " + new Money(x.Price,x.Listing.Currency) : "판매 취소")}"));
    private async Task Run(Func<CancellationToken,Task> action)
    {
        if (Busy) return; Busy = true; operation = new();
        try { await action(operation.Token); }
        catch (OperationCanceledException) { if (!logoutRequested) { LoginState = SteamLoginState.Cancelled; Status = "미전송 중단 요청 · 이미 접수된 요청은 저널 대조 필요"; } }
        catch(Exception error) { if(!logoutRequested) { if(operation.IsCancellationRequested){LoginState=SteamLoginState.Cancelled;Status="미전송 중단 요청 · 결과 불명은 재전송하지 않습니다.";}else Error(error); } }
        finally
        {
            operation.Dispose(); operation = null;
            if (logoutRequested) { workspace?.Dispose(); workspace=null; logoutRequested=false; }
            Busy = false;
        }
    }
    public Task BeginAsync() => Run(async token =>
    {
        if (!RiskAccepted || !CredentialsAccepted) throw new SteamException(SteamFailure.Disabled);
        ClearData(); workspace?.Dispose(); workspace = factory(Game); workspace.Access.Acknowledge(true,true,true,false);
        LoginState = SteamLoginState.RequestingQr; Status = "공식 Steam 앱으로 QR을 스캔하세요. 외부 QR 서비스는 사용하지 않습니다.";
        var challenge = await workspace.Authentication.BeginAsync(token); token.ThrowIfCancellationRequested();
        var qr = QrCode.EncodeText(challenge.AbsoluteUri,QrCode.Ecc.Medium); var cells = new bool[qr.Size,qr.Size];
        for(var y=0;y<qr.Size;y++)for(var x=0;x<qr.Size;x++)cells[x,y]=qr.GetModule(x,y);
        QrMatrix=cells; LoginState=SteamLoginState.AwaitingMobile;
    });
    public Task CheckAsync() => Run(async token =>
    {
        if (workspace is null) throw new SteamException(SteamFailure.Disabled);
        if (await workspace.Authentication.CheckAsync(token)) { token.ThrowIfCancellationRequested(); QrMatrix=null; LoginState=SteamLoginState.Connected; Status="Steam 인증 성공 · 실제 읽기 연결 · 거래는 별도 차단"; RefreshRequests(); }
        else Status="모바일 승인 대기 · 서버 간격 이전에는 추가 요청하지 않습니다.";
    });
    public Task LoadAsync() => Run(async token =>
    {
        RequireConnection(); workspace!.SetGame(Game.AppId,Game.ContextId,Game.Name);
        var assets=await workspace.Inventory.GetAsync(token);var schedule=await workspace.FeesAsync(Game.AppId,token);var listings=await workspace.Orders.RefreshAsync(token);token.ThrowIfCancellationRequested();
        fees=schedule;var available=workspace.Store.AvailableAll(assets);Assets.ReplaceAll(assets.Select(asset=>new SteamAssetRow(asset,available[asset.Key],Changed)));
        SetListings(listings);Raise(nameof(CurrencyText));Status=$"Steam 실제 응답 · {Game.Name} · {assets.Count}개 자산 · {DateTimeOffset.Now:HH:mm:ss} 갱신 · 시세는 선택 후 별도 갱신";RefreshRequests();Changed();
    });
    public Task RefreshPricesAsync() => Run(async token =>
    {
        RequireConnection();var selected=Assets.Where(x=>x.Selected).ToArray();var groups=selected.GroupBy(x=>x.Asset.MarketHashName).ToArray();
        if(groups.Length>50)throw new SteamException(SteamFailure.Restricted);
        foreach(var group in groups){var quote=await workspace!.Prices.RefreshAsync(group.First().Asset,token);token.ThrowIfCancellationRequested();if(quote.Currency!=fees?.Currency)throw new SteamException(SteamFailure.Restricted);foreach(var row in group)row.SetPrice(quote);}
        Status="Steam 호가 갱신 완료 · 매수 잔량은 동일 상품 전체가 공유 · 체결 보장 없음";
    });
    public void BuildReview()
    {
        if(fees is null)throw new SteamException(SteamFailure.Restricted);
        var selected=Assets.Where(x=>x.Selected).ToArray();if(selected.Any(x=>x.Quantity>x.Available || x.Price is null))throw new SteamException(SteamFailure.Restricted);
        var plan=SalePlanner.Build(selected.Select(x=>new SaleSelection(x.Asset,x.Price!,x.Quantity)),true);ReviewRows.Clear();
        foreach(var line in plan.Lines)ReviewRows.Add(new(line.Selection.Asset,line.Allocation.Quantity,line.Allocation.BuyerPays,line.Selection.Price,fees,Changed));
        foreach(var line in plan.Unpriced)ReviewRows.Add(new(line.Selection.Asset,line.Quantity,0,line.Selection.Price,fees,Changed));
        ReviewOpen=true;Status=$"매수 잔량 부족 {plan.UnpricedQuantity}개 · 수동 가격은 이 검토 안에서만 유지 · 단일 최종 승인";Changed();
    }
    public Task SubmitAsync() => Run(async token =>
    {
        RequireConnection();if(!CanTrade || !ReviewOpen || ReviewRows.Count==0 || ReviewRows.Any(x=>!x.Valid))throw new SteamException(SteamFailure.Disabled);
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
    private Task HistoryAsync()=>Run(async token=>{RequireConnection();var row=Assets.First(x=>x.Selected);var points=await workspace!.Prices.GetMedianHistoryAsync(row.Asset,token);token.ThrowIfCancellationRequested();HistoryText=$"Steam 중간가격 이력 {points.Length}개 시간 구간 · 정확한 거래 VWAP 아님\n"+string.Join("\n",points.TakeLast(6).Select(x=>$"{x.At:MM-dd HH:mm} UTC · median {x.MedianMajor} · volume {x.Volume}"));});
    private void TryManagement(bool reprice){try{BuildManagement(reprice);}catch(Exception){Status="관리 검토 오류: 통화/가격/수수료를 확인하세요.";Changed();}}
    private void BuildManagement(bool reprice)
    {
        if(fees is null)throw new SteamException(SteamFailure.Restricted);managementReprice=reprice;
        management=Listings.Where(x=>x.Selected).Select(x=>(x.Listing,reprice?SteamReviewRow.ParsePrice(x.NewPriceText,x.Listing.Currency):0,
            reprice?workspace?.Store.Reprices().FirstOrDefault(p=>p.Listing.ListingId==x.Listing.ListingId)?.Key??Guid.NewGuid().ToString("N"):Guid.NewGuid().ToString("N"))).ToArray();
        if(reprice)foreach(var row in management){if(row.Listing.Currency!=fees.Currency)throw new SteamException(SteamFailure.Restricted);_=fees.FromBuyer(row.Price);}
        ManagementOpen=true;Raise(nameof(ManagementSummary));
    }
    private Task ManageAsync()=>Run(async token=>
    {
        RequireConnection();if(!CanTrade)throw new SteamException(SteamFailure.Disabled);Results.Clear();
        foreach(var row in management)
        {
            if(managementReprice){var intent=await workspace!.Executor.RepriceAsync(row.Listing,row.Price,row.Key,token);token.ThrowIfCancellationRequested();Results.Add(new(row.Listing.ListingId,intent.State==SteamRepriceState.Submitted?SaleState.Submitted:SaleState.Unknown,"재가격 "+intent.State));}
            else {var result=await workspace!.Executor.CancelAsync(row.Listing,row.Key,token);token.ThrowIfCancellationRequested();Results.Add(result);}
            if(Results.Last().State==SaleState.Unknown)break;
        }
        ManagementOpen=false;RefreshRequests();Status="관리 결과 · 취소/반환 대조가 불명확하면 재등록하지 않습니다.";if(workspace?.Failure is {} failure)Error(new SteamException(failure));
    });
    private void RequireConnection(){if(!Connected)throw new SteamException(SteamFailure.Authentication);}
    private Task ResumeRepriceAsync()=>Run(async token=>
    {
        RequireConnection();var intent=workspace!.Store.Reprice(SelectedReprice??"")??throw new SteamException(SteamFailure.Restricted);
        var result=await workspace.Executor.RepriceAsync(intent.Listing,intent.BuyerPrice,intent.Key,token);token.ThrowIfCancellationRequested();Status="보존한 재가격 의도 대조/재개: "+result.State;RefreshRequests();
    });
    private void SetListings(IEnumerable<SteamListing> rows){Listings.ReplaceAll(rows.Select(row=>new SteamListingRow(row,Changed)));Changed();}
    private void SetResults(BatchRecord record){Results.Clear();foreach(var result in record.Results)Results.Add(result);}
    private void RefreshRequests(){Requests.Clear();RepriceKeys.Clear();if(workspace is not null){foreach(var command in workspace.Store.Commands())Requests.Add(command.RequestKey);foreach(var plan in workspace.Store.Reprices())RepriceKeys.Add(plan.Key);var available=workspace.Store.AvailableAll(Assets.Select(x=>x.Asset).ToArray());Bulk(()=>{foreach(var row in Assets)row.UpdateAvailable(available[row.Asset.Key]);});}}
    private void ClearData(){QrMatrix=null;Assets.Clear();Listings.Clear();ReviewRows.Clear();Results.Clear();Requests.Clear();RepriceKeys.Clear();SelectedRequest=SelectedReprice=null;fees=null;ReviewOpen=ManagementOpen=false;EvidenceText="대조 증거 없음";HistoryText="중간가격 이력 미조회 (정확한 거래 VWAP 아님)";Raise(nameof(CurrencyText));}
    private void LogoutNow(){var cancelling=Busy||LoginState is SteamLoginState.AwaitingMobile or SteamLoginState.RequestingQr;operation?.Cancel();workspace?.Authentication.Dispose();ClearData();tradeAccepted=false;Raise(nameof(TradeAccepted));LoginState=cancelling?SteamLoginState.Cancelled:SteamLoginState.Disconnected;Status="로그아웃 · 인증정보 저장 없음 · 거래 저널 보존";if(Busy)logoutRequested=true;else{workspace?.Dispose();workspace=null;}Changed();}
    private void Error(Exception error)
    {
        QrMatrix=null;
        LoginState=error is SteamException e ? e.Code switch{SteamFailure.Expired or SteamFailure.Authentication=>SteamLoginState.Expired,SteamFailure.Denied=>SteamLoginState.Denied,SteamFailure.Transport=>SteamLoginState.NetworkFailure,SteamFailure.Forbidden or SteamFailure.RateLimited or SteamFailure.Challenge=>SteamLoginState.AccessBlocked,SteamFailure.Disabled or SteamFailure.StaleReview or SteamFailure.Restricted=>LoginState,_=>SteamLoginState.ContractFailure}:SteamLoginState.ContractFailure;
        Status=error is SteamException steam ? "Steam 작업 중단 · "+steam.Code : "입력/계약 검증 실패 · 세부 응답/인증정보는 기록하지 않습니다.";Changed();
    }
    private void Changed()
    {
        if(bulkUpdate)return;
        Raise(nameof(Totals));Raise(nameof(TradingText));Raise(nameof(CanTrade));
        BeginLogin?.Refresh();CheckLogin?.Refresh();Load?.Refresh();RefreshPrices?.Refresh();SelectAll?.Refresh();Review?.Refresh();CloseReview?.Refresh();Submit?.Refresh();EnableTrading?.Refresh();Recover?.Refresh();Inspect?.Refresh();MedianHistory?.Refresh();ReviewCancel?.Refresh();ReviewReprice?.Refresh();ConfirmManagement?.Refresh();
        ResumeReprice?.Refresh();
        SelectAllListings?.Refresh();ArchiveBatch?.Refresh();
        CancelOperation?.Refresh();
    }
    private void Bulk(Action action){bulkUpdate=true;try{action();}finally{bulkUpdate=false;Changed();}}
    public void Dispose()=>LogoutNow();
}

public sealed class SteamAssetRow(InventoryAsset asset,int available,Action changed):Observable
{
    public InventoryAsset Asset {get;}=asset;public string Name=>Asset.Name;public int Owned=>Asset.Quantity;public int Available {get;private set;}=available;public int Maximum=>Math.Max(1,Available);
    public bool CanSelect=>Asset.Marketable&&!Asset.Locked&&Available>0;public string Flags=>$"보관함 {Owned} · 로컬 예약 반영 가용 {Available} · marketable {Asset.Marketable} · tradable {Asset.Tradable}";
    private bool selected;private int quantity=1;public bool Selected{get=>selected;set{if(Set(ref selected,value&&CanSelect))changed();}}public int Quantity{get=>quantity;set{if(Set(ref quantity,Math.Clamp(value,1,Maximum)))changed();}}
    public PriceSnapshot? Price {get;private set;}public string PriceText=>Price is null?"시세 미조회":$"{Price.Source} · {(Price.Bids.IsEmpty?"매수 잔량 없음":new Money(Price.Bids[0].BuyerPays,Price.Currency).ToString())} · {Price.CapturedAt:HH:mm:ss} UTC";
    public void SetPrice(PriceSnapshot price){Price=price;Raise(nameof(PriceText));changed();}
    public void UpdateAvailable(int value){Available=value;if(value==0)Selected=false;quantity=Math.Clamp(quantity,1,Maximum);Raise(nameof(Available));Raise(nameof(Flags));Raise(nameof(Maximum));Raise(nameof(CanSelect));Raise(nameof(Quantity));changed();}
}
public sealed class SteamReviewRow:Observable
{
    private readonly SteamFeeSchedule fees;private readonly Action changed;private string text;private readonly long originalBid,recommended;
    public InventoryAsset Asset{get;}public int Quantity{get;}public DateTimeOffset CapturedAt{get;}public bool Insufficient{get;}public string Identity=>$"{Asset.Name} · {Asset.Key.AssetId} · {Quantity}개";
    public string PriceText{get=>text;set{Set(ref text,value);Raise(nameof(Valid));Raise(nameof(Amounts));Raise(nameof(PriceBasis));changed();}}
    public SteamReviewRow(InventoryAsset asset,int quantity,long buyer,PriceSnapshot price,SteamFeeSchedule fees,Action changed)
    {Asset=asset;Quantity=quantity;CapturedAt=price.CapturedAt;Insufficient=buyer==0;this.fees=fees;this.changed=changed;originalBid=buyer;try{buyer=fees.FromBuyer(buyer,false).BuyerPays;}catch(ArgumentException){buyer=0;}recommended=buyer;text=buyer==0?"":FormatPrice(buyer,fees.Currency);}
    public string PriceBasis=>Insufficient?"매수 잔량 부족 · 직접 가격 필요 · 예상 미체결 수량":$"호가 {new Money(originalBid,fees.Currency)} · 증분/수수료 반영 추천 {new Money(recommended,fees.Currency)} · {(text==FormatPrice(recommended,fees.Currency)?"추천":"수동 수정")}";
    public bool Valid{get{try{_=Quote;return DateTimeOffset.UtcNow-CapturedAt<=TimeSpan.FromMinutes(5)&&CapturedAt<=DateTimeOffset.UtcNow.AddSeconds(5);}catch(Exception e)when(e is ArgumentException or OverflowException){return false;}}}
    public FeeQuote Quote=>fees.FromBuyer(ParsePrice(text,fees.Currency));
    public string Amounts=>Valid?$"수량 {Quantity} · Steam {new Money(checked(Quote.SteamFee*Quantity),fees.Currency)} · 게임 {new Money(checked(Quote.GameFee*Quantity),fees.Currency)} · 실수령 {new Money(checked(Quote.SellerReceives*Quantity),fees.Currency)} · {CapturedAt:HH:mm:ss} UTC{(Insufficient?" · 매수 잔량 부족/예상 미체결":"")}":"가격/증분/수수료/시세 오류 · 입력 필요";
    public SaleLineSnapshot Snapshot(int index)=>new("steam-"+index,Asset,Quantity,fees.Currency,Quote,CapturedAt);
    public static string FormatPrice(long minor,string currency)=>currency=="KRW"?minor.ToString(System.Globalization.CultureInfo.InvariantCulture):$"{minor/100}.{minor%100:00}";
    public static long ParsePrice(string text,string currency)
    {if(currency=="KRW"&&long.TryParse(text,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var won))return won;if(currency=="USD"&&decimal.TryParse(text,System.Globalization.NumberStyles.AllowDecimalPoint,System.Globalization.CultureInfo.InvariantCulture,out var usd)&&usd>=0&&usd*100==decimal.Truncate(usd*100))return checked((long)(usd*100));throw new ArgumentException("InvalidPrice");}
}
public sealed class SteamListingRow(SteamListing listing,Action changed):Observable
{
    public SteamListing Listing{get;}=listing;public string Description=>$"{Listing.HashName} · {Listing.Quantity}개 · {Listing.State} · 구매자 {new Money(Listing.SellerReceives+Listing.Fees,Listing.Currency)} · ID {Listing.ListingId}";
    private bool selected;public bool Selected{get=>selected;set{Set(ref selected,value);changed();}}public string NewPriceText{get;set;}=SteamReviewRow.FormatPrice(listing.SellerReceives+listing.Fees,listing.Currency);
}
