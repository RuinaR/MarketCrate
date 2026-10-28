using System.Diagnostics;
using MarketCrate.Core;

namespace MarketCrate.App;

// WPF trace text can contain user values. Discard it before the logging boundary.
internal sealed class BindingDiagnostics:TraceListener
{
    private int reported;
    public override void Write(string? message) { }
    public override void WriteLine(string? message) { }
    public override void TraceEvent(TraceEventCache? cache,string source,TraceEventType type,int id,string? message)
    {
        if(type>TraceEventType.Error||Interlocked.Increment(ref reported)>20)return;
        DiagnosticHub.Emit(DiagnosticEvent.UiBinding,DiagnosticArea.UI,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticError.Binding,requirement:"G08");
    }
    public override void TraceEvent(TraceEventCache? cache,string source,TraceEventType type,int id,string? format,params object?[]? args)=>TraceEvent(cache,source,type,id,(string?)null);
}
