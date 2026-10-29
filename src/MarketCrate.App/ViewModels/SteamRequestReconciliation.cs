using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.App.ViewModels;

public sealed record SteamRequestOption(string Key,string Label);

public sealed partial class SteamWorkspaceViewModel
{
    public AsyncCommand RefreshSelectedRequest{get;private set;}=null!;
    public RangeObservableCollection<SteamRequestOption> RequestOptions{get;}=[];
    public Command OpenInventoryReservations{get;private set;}=null!;
    public bool HasInventoryReservations=>Assets.Any(x=>x.Available<x.Owned);
    public string InventoryReservationSummary=>HasInventoryReservations?$"보유 아이템 중 {Assets.Count(x=>x.Available<x.Owned)}종에 이전 판매 요청의 예약이 남아 있습니다. 중복 등록 방지를 위해 예약 수량은 선택할 수 없습니다.":"";
    public string SubmissionAvailability=>!Connected?"Steam 계정을 연결한 뒤 최신 자료를 확인하세요.":!CanTrade?"실제 판매등록은 꺼져 있습니다. 게임·판매 설정에서 서버 계약과 세션 동의를 확인하세요.":PricingMode==2&&workspace?.Policy.MedianPriceContractSupported!=true?"중앙값 추정 가격의 실거래 계약이 미확인입니다. 즉시거래가능가로 새 검토하세요.":ReviewRows.Count==0||ReviewRows.Any(x=>!x.Valid)?"가격·수수료·시세 신선도를 확인하세요. 유효하지 않은 항목은 전송하지 않습니다.":"전체 판매등록 한 번 승인으로 표시된 모든 항목·수량·가격을 제출합니다.";
    private void InitializeRequestReconciliation()
    {
        RefreshSelectedRequest=new(RefreshSelectedRequestAsync,Error,()=>CanRead&&!Busy&&!AnyReview&&SelectedRequest is not null);
        OpenInventoryReservations=new(()=>
        {
            var blocked=Assets.Where(x=>x.Available<x.Owned).Select(x=>x.Asset.Key).ToHashSet();
            var commands=workspace!.Store.ReservedSells().Where(x=>x.Account==workspace.Authentication.SteamId&&blocked.Contains(x.Line.Asset.Key)).OrderByDescending(x=>x.SentAt).ToArray();
            RefreshRequests();SelectedRequest=commands.FirstOrDefault()?.RequestKey;WorkPage=3;
            foreach(var command in commands)if(!Results.Any(x=>x.LineId==command.Line.LineId))Results.Add(new(command.Line.LineId,command.State,"이전 판매 요청으로 예약 중 · "+SteamStatusText.Label(command.State)+" · 자동 재전송/예약 해제 없음"));
            EvidenceText=commands.Length==0?"예약 근거를 연결하지 못했습니다. 진단 로그를 내보내 주세요. 예약은 해제하지 않았습니다.":"이전 요청을 선택했습니다. ‘선택 요청 상태 확인’ 또는 ‘증거 대조’로 서버 상태를 확인하세요. 보관함에 보인다는 사실만으로 불명 요청을 실패로 확정하지 않습니다.";
            Status="보관함 조회와 이전 요청 예약은 별개입니다. 새 등록 요청은 보내지 않았습니다.";
            DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Management,DiagnosticOutcome.Pending,reason:"InventoryReservationOpened",count:commands.Length,requirement:"C10");Changed();
        },()=>!Busy&&!AnyReview&&Connected&&HasInventoryReservations);
    }
    public Task RefreshSelectedRequestAsync()=>Run(async token=>
    {
        RequireConnection();var key=SelectedRequest??throw new SteamException(SteamFailure.Restricted);
        var command=workspace!.Store.ReadCommand(key)??throw new SteamException(SteamFailure.Restricted);
        if(command.Account!=workspace.Authentication.SteamId)throw new SteamException(SteamFailure.Restricted);
        OperationLabel=command.Kind=="cancel"?"선택 취소의 서버 상태 확인 중":"선택 등록의 서버 상태 확인 중";
        var result=command.Kind=="cancel"?await workspace.Executor.ReconcileCancellationAsync(key,token):await workspace.Executor.ReconcileAsync(command.Line,key,token);
        token.ThrowIfCancellationRequested();
        var existing=Results.FirstOrDefault(x=>x.LineId==result.LineId);
        if(existing is not null)Results[Results.IndexOf(existing)]=result;else Results.Add(result);
        RefreshRequests();Raise(nameof(BatchProgressText));EvidenceText=result.Message;
        Status=command.Kind=="cancel"?"취소 요청 상태 확인 완료 · 목록 부재만으로 반환을 확정하지 않습니다. 새 판매 요청은 보내지 않았습니다.":"판매 요청 상태 확인 완료 · 새 등록/재전송 없음";
        DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Management,to:result.State.ToString(),requirement:"B09",reason:"RecoverOnly");
    });
}
