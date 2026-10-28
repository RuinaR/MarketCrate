using System.IO.Compression;
using System.Text.Json;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;

namespace MarketCrate.Tests;

public static class NinthStageTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        try { await OrderedExport(); Console.WriteLine("PASS Diagnostic operation ZIP chronological order independent of file modification times");return(1,0); }
        catch(Exception error){Console.WriteLine("FAIL Diagnostic operation ZIP chronological order: "+error);return(0,1);}
    }
    private static async Task OrderedExport()
    {
        var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage9-diagnostic-"+Guid.NewGuid().ToString("N"));
        try
        {
            using var log=new JsonDiagnosticLog(folder);using var scope=DiagnosticHub.Use(log);
            using(var operation=DiagnosticHub.Scope())DiagnosticHub.Emit(DiagnosticEvent.AppStart,DiagnosticArea.Lifecycle,reason:"IsolatedQa");
            await log.FlushAsync();var original=log.Recent.Single();var id=Guid.NewGuid();
            var first=original with{Utc=original.Utc.AddMinutes(-2),Context=original.Context with{OperationId=id},EventId=(int)DiagnosticEvent.UiCommand,EventName=nameof(DiagnosticEvent.UiCommand),Outcome=DiagnosticOutcome.Started};
            var last=first with{Utc=original.Utc.AddMinutes(-1),EventId=(int)DiagnosticEvent.UiFailure,EventName=nameof(DiagnosticEvent.UiFailure),Outcome=DiagnosticOutcome.Failed,Level=LogLevel.Error,Error=DiagnosticError.Validation};
            var earlyFile=Path.Combine(folder,"session-stage9-early.jsonl");var lateFile=Path.Combine(folder,"session-stage9-late.jsonl");
            await File.WriteAllTextAsync(earlyFile,JsonSerializer.Serialize(first,JsonDiagnosticLog.Json)+"\n");
            await File.WriteAllTextAsync(lateFile,JsonSerializer.Serialize(last,JsonDiagnosticLog.Json)+"\n");
            // Reproduce copies/restores touching old files last: filesystem order is not event time.
            File.SetLastWriteTimeUtc(lateFile,DateTime.UtcNow.AddSeconds(-20));File.SetLastWriteTimeUtc(earlyFile,DateTime.UtcNow.AddSeconds(-10));
            var review=await log.ReviewExportAsync(id);Check(review.Events==2&&review.Errors==1);
            var zip=await log.ExportAsync(review.ReviewId,true);Check(Path.GetFullPath(zip).StartsWith(Path.GetFullPath(folder)+Path.DirectorySeparatorChar));
            using var archive=ZipFile.OpenRead(zip);Check(archive.Entries.Count==2);
            using var reader=new StreamReader(archive.GetEntry("events.jsonl")!.Open());
            var entries=(await reader.ReadToEndAsync()).Split('\n',StringSplitOptions.RemoveEmptyEntries).Select(x=>JsonSerializer.Deserialize<DiagnosticRecord>(x,JsonDiagnosticLog.Json)!).ToArray();
            Check(entries[0].Utc==first.Utc&&entries[1].Utc==last.Utc);
            Check(entries.All(x=>x.Context.OperationId==id)&&entries[1].Error==DiagnosticError.Validation);
            Check(!archive.Entries.Any(x=>x.Name.EndsWith(".db")||x.Name.Contains("scenario")));
        }
        finally { Directory.Delete(folder,true); }
    }
    private static void Check(bool value){if(!value)throw new Exception("Chronological export assertion failed");}
}
