using MarketCrate.Core;
namespace MarketCrate.Infrastructure.Steam;

public sealed partial class SteamHttp
{
    private readonly object priorityGate=new();
    private int priceReaders;
    private bool priorityClosed;
    private TaskCompletionSource? priceReadersDone;
    internal IDisposable PrioritizePriceReads()
    {
        lock(priorityGate){ObjectDisposedException.ThrowIf(priorityClosed,this);if(priceReaders++==0)priceReadersDone=new(TaskCreationOptions.RunContinuationsAsynchronously);}
        return new PriorityLease(()=>{lock(priorityGate){if(--priceReaders==0){priceReadersDone?.TrySetResult();priceReadersDone=null;}}});
    }
    private sealed class PriorityLease(Action release):IDisposable
    {private Action? action=release;public void Dispose()=>Interlocked.Exchange(ref action,null)?.Invoke();}
    private async Task EnterReadQueueAsync(bool lowPriority,CancellationToken token)
    {
        while(true)
        {
            Task wait;
            lock(priorityGate){ObjectDisposedException.ThrowIf(priorityClosed,this);wait=lowPriority&&priceReaders>0?priceReadersDone!.Task:Task.CompletedTask;}
            if(!wait.IsCompleted)DiagnosticHub.Emit(DiagnosticEvent.CacheRead,DiagnosticArea.Data,DiagnosticOutcome.Pending,LogLevel.Debug,endpoint:"MarketImage",reason:"ImageDeferred");
            await wait.WaitAsync(token);await queue.WaitAsync(token);
            lock(priorityGate){if(!priorityClosed&&(!lowPriority||priceReaders==0))return;}
            queue.Release();
        }
    }
    private void CloseReadPriority(){lock(priorityGate){priorityClosed=true;priceReadersDone?.TrySetResult();}}
}
