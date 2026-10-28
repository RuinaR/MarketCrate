using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
using Net.Codecrete.QrCodeGenerator;

namespace MarketCrate.App.ViewModels;

public sealed partial class SteamWorkspaceViewModel
{
    private readonly Dictionary<string,(DateTimeOffset At,int Count,string? Currency,Guid? Operation)> readChecks=[];
    private Uri? displayedChallenge;
    private Action<Uri> officialPage = null!;
    private bool syntheticConnection;
    public AsyncCommand LoadListings {get;private set;}=null!;
    public AsyncCommand LoadFees {get;private set;}=null!;
    public Command OpenOfficialMarket {get;private set;}=null!;
    public string ContractSummary => "마지막 파싱 관측 · "+(syntheticConnection?"합성 계약 (실서버 아님)":"실험 연결 (READ_ONLY_READY 판정 아님)")+"\n"+
        (readChecks.Count==0?"조회 증거 없음":string.Join("\n",readChecks.OrderBy(x=>x.Key).Select(x=>$"{x.Key} · {x.Value.Count}건 · {x.Value.Currency??"—"} · {x.Value.At:yyyy-MM-dd HH:mm:ss} UTC · 작업 {x.Value.Operation}")))+
        "\n원문·계정·자산ID·QR·토큰 제외. 조회 실패 시 기존 표는 마지막 관측이며 최신값으로 사용하지 마세요.";
    public string ProductReadiness => "MOCK_READY · GUI_VERIFIED: 사용자 보고의 9차 기본 7경로만\nREAD_ONLY_READY / LIVE_CONTRACT_READY / LIVE_READY / RELEASE_READY: 미검증 또는 차단";
    private void InitializeConnection(bool synthetic,Action<Uri>? open)
    {
        syntheticConnection=synthetic;
        officialPage=open??(uri=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri){UseShellExecute=true}));
        OpenOfficialMarket=new(()=>{DiagnosticHub.Emit(DiagnosticEvent.UiPage,DiagnosticArea.UI,requirement:"F01",reason:"OfficialMarket");officialPage(new("https://steamcommunity.com/market/"));},()=>!Busy&&!AnyReview);
        LoadListings=new(LoadListingsAsync,Error,()=>CanReadFeature("Listings")&&!Busy&&!AnyReview);
        LoadFees=new(LoadFeesAsync,Error,()=>CanReadFeature("Wallet")&&CanReadFeature("Fees")&&!Busy&&!AnyReview);
    }
    private void RecordCheck(string endpoint,int count,string? currency=null)
    {
        if(endpoint=="History")HistoryRead.Update(count==0?SteamReadState.Empty:SteamReadState.Ready);
        readChecks[endpoint]=(DateTimeOffset.UtcNow,count,currency,DiagnosticHub.Context?.OperationId);
        DiagnosticHub.Emit(DiagnosticEvent.ReadContract,DiagnosticArea.Data,requirement:"A04",endpoint:endpoint,count:count,currency:currency,reason:syntheticConnection?"Synthetic":"Observed");
        Raise(nameof(ContractSummary));
    }
    private void UpdateChallenge()
    {
        var challenge=workspace?.Authentication.Challenge;
        if(challenge is null||challenge==displayedChallenge)return;
        var qr=QrCode.EncodeText(challenge.AbsoluteUri,QrCode.Ecc.Medium);var cells=new bool[qr.Size,qr.Size];
        for(var y=0;y<qr.Size;y++)for(var x=0;x<qr.Size;x++)cells[x,y]=qr.GetModule(x,y);
        displayedChallenge=challenge;QrMatrix=cells;
        DiagnosticHub.Emit(DiagnosticEvent.QrReady,DiagnosticArea.Authentication,requirement:"A02",reason:"SourceChanged");
    }
    public Task LoadListingsAsync()=>Run(async token=>
    {
        RequireConnection();var rows=await workspace!.Orders.RefreshAsync(token);token.ThrowIfCancellationRequested();
        SetListings(rows);RefreshRequests();RecordCheck("Listings",rows.Length);
        Status="판매목록 조회 완료 · 보관함/지갑 조회와 별도 · 실제 취소/재가격은 정책 차단 유지";
    });
    public Task LoadFeesAsync()=>Run(async token=>
    {
        RequireConnection();var schedule=await workspace!.FeesAsync(Game.AppId,token);token.ThrowIfCancellationRequested();
        fees=schedule;Raise(nameof(CurrencyText));RecordCheck("Fees",1,schedule.Currency);Changed();
        Status="지갑·게임 수수료 조회 완료 · 등록 전 다시 검증 · 실제 거래 활성화 아님";
    });
}
