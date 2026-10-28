using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;

namespace MarketCrate.App.ViewModels;

public sealed class DiagnosticsViewModel : Observable
{
    private readonly JsonDiagnosticLog? log;
    private readonly Action<bool>? saveMode;
    private DiagnosticPackageReview? review;
    private string operation="",status="진단 초기화 전",summary="내보내기 전에 포함 범위를 검토하세요.";
    private bool reviewed,errorsOnly;
    public bool ErrorsOnly{get=>errorsOnly;set{if(Set(ref errorsOnly,value))Update();}}
    public DiagnosticsViewModel(JsonDiagnosticLog? log=null,Action<string>? openFolder=null,Action<bool>? saveMode=null)
    {
        this.log=log;this.saveMode=saveMode;
        Refresh=new(Update);
        OpenFolder=new(()=>{if(log is null)return;Directory.CreateDirectory(log.DirectoryPath);(openFolder??Open)(log.DirectoryPath);},()=>log is not null);
        Cleanup=new(async()=>{await log!.CleanupAsync();DiagnosticHub.Emit(DiagnosticEvent.Retention,DiagnosticArea.Diagnostics,reason:"Cleanup");Update();},Failure,()=>log is not null);
        ReviewPackage=new(async()=>{var filter=OperationFilter;Guid? id=filter.Length==0?null:Guid.Parse(filter);var snapshot=await log!.ReviewExportAsync(id);if(filter!=OperationFilter)return;review=snapshot;Reviewed=false;Summary=$"검토 ID {review.ReviewId} · 작업 {review.OperationId?.ToString()??"전체"}\n이벤트 {review.Events} · 오류 {review.Errors} · 제외 {review.Skipped}\nmanifest.json + events.jsonl · 내부 작업 ID와 정제된 코드 위치 포함\nDB·인증정보·설정 원문 없음 · 자동 외부 전송 없음";Export?.Refresh();},Failure,()=>log is not null&&ValidFilter);
        Export=new(async()=>{Status="로컬 ZIP 저장: "+await log!.ExportAsync(review!.ReviewId,Reviewed);review=null;Reviewed=false;Export?.Refresh();},Failure,()=>log is not null&&review is not null&&Reviewed);
        Update();
    }
    private static void Open(string path)=>Process.Start(new ProcessStartInfo(path){UseShellExecute=true}); // Only explicit user command; never called in QA.
    public Command Refresh{get;} public Command OpenFolder{get;}
    public AsyncCommand Cleanup{get;} public AsyncCommand ReviewPackage{get;} public AsyncCommand Export{get;}
    public ObservableCollection<DiagnosticRow> Events{get;}=[];
    public string OperationFilter{get=>operation;set{if(Set(ref operation,value)){review=null;Reviewed=false;ReviewPackage.Refresh();Export?.Refresh();Raise(nameof(ValidFilter));}}}
    public bool ValidFilter=>OperationFilter.Length==0||Guid.TryParse(OperationFilter,out _);
    public bool DiagnosticMode{get=>log?.DiagnosticMode==true;set{if(log is not null){log.DiagnosticMode=value;try{saveMode?.Invoke(value);}catch(Exception e){Failure(e);}DiagnosticHub.Emit(DiagnosticEvent.DiagnosticMode,DiagnosticArea.Diagnostics,reason:"UserRequested");Raise();Update();}}}
    public bool Reviewed{get=>reviewed;set{Set(ref reviewed,value);Export?.Refresh();}}
    public string Status{get=>status;private set=>Set(ref status,value);}
    public string Summary{get=>summary;private set=>Set(ref summary,value);}
    public string Policy=>"Information 이상 기본 · 상세 모드 Trace/Debug · 파일 2 MiB / 전체 20 MiB / 최대 20개 / 14일 · 큐 256개 · 최근 이벤트 512개\n진단 로그는 복구 저널이 아닙니다. 누락/디스크 실패 시 거래를 재전송하지 않습니다. ZIP은 사용자가 직접 검토·공유합니다.";
    private void Update()
    {
        Events.Clear();var rows=log?.Recent??[];
        foreach(var item in rows.Where(x=>(!ErrorsOnly||x.Level>=LogLevel.Error)&&(!Guid.TryParse(OperationFilter,out var id)||x.Context.OperationId==id)).TakeLast(100).Reverse())Events.Add(new(item));
        var health=log?.Health;Status=health is null?"로거 연결 없음 (격리 UI/테스트 환경)":$"{health.State} · 저장 {health.Written} · 큐 누락 {health.Dropped} · 쓰기 실패 {health.WriteFailures} · Session {log!.SessionId}";
    }
    private void Failure(Exception e){DiagnosticHub.Emit(DiagnosticEvent.Export,DiagnosticArea.Diagnostics,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticError.Logging,exception:e);Status="진단 작업 실패 · 파일 접근/디스크/검토 ID를 확인하세요. 거래 저널은 변경하지 않았습니다.";}
}
public sealed record DiagnosticRow(DiagnosticRecord Record)
{
    public string Time=>$"{Record.Utc:yyyy-MM-dd HH:mm:ss} UTC / {Record.Utc.ToOffset(TimeSpan.FromHours(9)):HH:mm:ss} KST";
    public string Detail=>$"{Record.Level} · {Record.EventId} {Record.EventName} · {Record.Outcome} · {Record.Error} · {Record.PreviousState} → {Record.NewState}";
    public string OperationId=>Record.Context.OperationId.ToString();
    public string Links=>$"요청 {Record.Context.CorrelationId} · 배치 {Record.Context.BatchId} · 내부 항목 {Record.Context.OperationItemId}";
}
