using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.App.ViewModels;

public sealed record SteamRequestOption(string Key,string Label);

public sealed partial class SteamWorkspaceViewModel
{
    public AsyncCommand RefreshSelectedRequest{get;private set;}=null!;
    public RangeObservableCollection<SteamRequestOption> RequestOptions{get;}=[];
    public Command OpenPendingInventoryRequests{get;private set;}=null!;
    public bool HasPendingInventoryRequests=>Assets.Any(x=>x.PendingSubmissionQuantity>0);
    public string InventorySyncSummary=>HasPendingInventoryRequests?$"보관함 수량은 Steam 조회 기준입니다. {Assets.Count(x=>x.PendingSubmissionQuantity>0)}종에 이전 요청 기록이 있습니다. 최종 승인 시 현재 보관함·판매목록을 다시 확인하며, 과거 불명 기록을 자동 재전송하지 않습니다.":"";
    private bool StackReviewSupported
    {
        get
        {
            try{return workspace?.Policy.StackAmountsSupported==true||ReviewRows.GroupBy(x=>x.Asset.Key).All(group=>!group.First().Asset.Stackable||workspace?.Policy.SingleRequestStackApps.Contains(group.Key.AppId)==true&&group.Select(x=>x.Quote).Distinct().Count()==1);}
            catch(Exception error)when(error is ArgumentException or OverflowException){return false;}
        }
    }
    public string SubmissionAvailability=>!StackReviewSupported?"스택은 검증된 앱에서 단일 가격·단일 요청으로만 제출할 수 있습니다. 여러 가격으로 분할된 스택은 전송하지 않습니다.":!Connected?"Steam 계정을 연결한 뒤 최신 자료를 확인하세요.":!CanTrade?"실제 판매등록은 꺼져 있습니다. 게임·판매 설정에서 서버 계약과 세션 동의를 확인하세요.":PricingMode==2&&workspace?.Policy.MedianPriceContractSupported!=true?"중앙값 추정 가격의 실거래 계약이 미확인입니다. 즉시거래가능가로 새 검토하세요.":ReviewRows.Count==0||ReviewRows.Any(x=>!x.Valid)?"가격·수수료·시세 신선도를 확인하세요. 유효하지 않은 항목은 전송하지 않습니다.":"전체 판매등록 한 번 승인으로 모든 항목을 제출합니다. 현재 보관함·판매목록을 재확인합니다. 과거 불명 기록이 있으면 새 요청에도 중복 위험이 남으며, 그 기록은 삭제하거나 자동 재전송하지 않습니다.";
    private void InitializeRequestReconciliation()
    {
        RefreshSelectedRequest=new(RefreshSelectedRequestAsync,Error,()=>CanRead&&!Busy&&!AnyReview&&SelectedRequest is not null);
        OpenPendingInventoryRequests=new(()=>
        {
            var blocked=Assets.Where(x=>x.PendingSubmissionQuantity>0).Select(x=>x.Asset.Key).ToHashSet();
            var commands=workspace!.Store.ReservedSells().Where(x=>x.Account==workspace.Authentication.SteamId&&blocked.Contains(x.Line.Asset.Key)).OrderByDescending(x=>x.SentAt).ToArray();
            RefreshRequests();SelectedRequest=commands.FirstOrDefault()?.RequestKey;WorkPage=3;
            foreach(var command in commands)if(!Results.Any(x=>x.LineId==command.Line.LineId))Results.Add(new(command.Line.LineId,command.State,"이전 전송 기록 · "+SteamStatusText.Label(command.State)+" · 자동 재전송 없음"));
            EvidenceText=commands.Length==0?"이전 전송 기록을 연결하지 못했습니다. 진단 로그를 내보내 주세요. 새 전송은 하지 않았습니다.":"이전 요청을 선택했습니다. ‘선택 요청 상태 확인’ 또는 ‘증거 대조’로 서버 상태를 확인하세요. 보관함에 보인다는 사실만으로 불명 요청을 실패로 확정하지 않습니다.";
            Status="Steam 보관함 수량과 로컬 전송 기록은 별개입니다. 새 등록 요청은 보내지 않았습니다.";
            DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Management,DiagnosticOutcome.Pending,reason:"InventoryReservationOpened",count:commands.Length,requirement:"C10");Changed();
        },()=>!Busy&&!AnyReview&&Connected&&HasPendingInventoryRequests);
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
        if(await workspace.Store.LoadAsync(token) is {} active)UpdateObservedStates(active);
        RefreshRequests();Raise(nameof(BatchProgressText));EvidenceText=result.Message;
        Status=command.Kind=="cancel"?"취소 요청 상태 확인 완료 · 목록 부재만으로 반환을 확정하지 않습니다. 새 판매 요청은 보내지 않았습니다.":"판매 요청 상태 확인 완료 · 새 등록/재전송 없음";
        DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Management,to:result.State.ToString(),requirement:"B09",reason:"RecoverOnly");
    });
}
