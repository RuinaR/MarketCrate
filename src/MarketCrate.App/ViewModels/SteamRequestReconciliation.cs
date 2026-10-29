using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.App.ViewModels;

public sealed record SteamRequestOption(string Key,string Label);

public sealed partial class SteamWorkspaceViewModel
{
    public AsyncCommand RefreshSelectedRequest{get;private set;}=null!;
    public RangeObservableCollection<SteamRequestOption> RequestOptions{get;}=[];
    public string SubmissionAvailability=>!Connected?"Steam 계정을 연결한 뒤 최신 자료를 확인하세요.":!CanTrade?"실제 판매등록은 꺼져 있습니다. 게임·판매 설정에서 서버 계약과 세션 동의를 확인하세요.":PricingMode==2&&workspace?.Policy.MedianPriceContractSupported!=true?"중앙값 추정 가격의 실거래 계약이 미확인입니다. 즉시거래가능가로 새 검토하세요.":ReviewRows.Count==0||ReviewRows.Any(x=>!x.Valid)?"가격·수수료·시세 신선도를 확인하세요. 유효하지 않은 항목은 전송하지 않습니다.":"전체 판매등록 한 번 승인으로 표시된 모든 항목·수량·가격을 제출합니다.";
    private void InitializeRequestReconciliation()=>RefreshSelectedRequest=new(RefreshSelectedRequestAsync,Error,()=>CanRead&&!Busy&&!AnyReview&&SelectedRequest is not null);
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
