using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Diagnostics;

public sealed record LogPolicy(int FileBytes=2*1024*1024,int TotalBytes=20*1024*1024,int Files=20,int Days=14,int Queue=256);
public sealed record LogHealth(long Accepted,long Written,long Dropped,long WriteFailures,bool DiagnosticMode,string State,DiagnosticError LastFailure=DiagnosticError.None,int? NativeError=null,Guid? FailedOperationId=null);
public sealed record DiagnosticPackageReview(Guid ReviewId,Guid? OperationId,int Events,int Errors,int Skipped,string Summary);

// Local, bounded, single writer. Diagnostic failure is not a transaction failure.
public sealed class JsonDiagnosticLog:IDiagnosticLog,IDisposable
{
    private readonly Channel<(DiagnosticRecord? Record,TaskCompletionSource? Barrier)> queue;
    private readonly ConcurrentQueue<DiagnosticRecord> recent=new();
    private readonly Task writer;
    private readonly LogPolicy policy;
    private readonly Func<string,Stream>? testOpen;
    private long accepted,written,dropped,failures;
    private int enabled,disposed,lastFailure,nativeError;
    private DiagnosticContext? failedContext;
    private DiagnosticRecord[]? reviewed;
    private DiagnosticPackageReview? review;
    public Guid SessionId{get;}=Guid.NewGuid();
    public string DirectoryPath{get;}
    public static readonly JsonSerializerOptions Json=new(){Converters={new JsonStringEnumConverter()}};
    public JsonDiagnosticLog(string directory,LogPolicy? policy=null,Func<string,Stream>? testOpen=null)
    {
        DirectoryPath=Path.GetFullPath(directory);this.policy=policy??new();this.testOpen=testOpen;
        if(this.policy.FileBytes<1024||this.policy.TotalBytes<this.policy.FileBytes||this.policy.Files<2||this.policy.Days<1||this.policy.Queue<2)throw new ArgumentException("InvalidLogPolicy");
        queue=Channel.CreateBounded<(DiagnosticRecord?,TaskCompletionSource?)>(new BoundedChannelOptions(this.policy.Queue){SingleReader=true,FullMode=BoundedChannelFullMode.Wait});
        writer=Task.Run(WriterAsync);
    }
    private void RecordFailure(Exception error,DiagnosticContext? context=null)
    {
        var code=error.HResult&0xffff;
        Volatile.Write(ref lastFailure,(int)(error is UnauthorizedAccessException?DiagnosticError.Forbidden:error is IOException&&code==112?DiagnosticError.Disk:error is IOException&&code is 32 or 33?DiagnosticError.Locked:DiagnosticError.Logging));
        Volatile.Write(ref nativeError,code);
        Volatile.Write(ref failedContext,context??DiagnosticHub.Context);
        Interlocked.Increment(ref failures);
    }
    public bool DiagnosticMode{get=>Volatile.Read(ref enabled)==1;set=>Volatile.Write(ref enabled,value?1:0);}
    public LogHealth Health=>new(Interlocked.Read(ref accepted),Interlocked.Read(ref written),Interlocked.Read(ref dropped),Interlocked.Read(ref failures),DiagnosticMode,Volatile.Read(ref disposed)!=0?"Closed":Interlocked.Read(ref failures)>0?"WriteFailure":"Active",(DiagnosticError)Volatile.Read(ref lastFailure),Volatile.Read(ref nativeError)==0?null:Volatile.Read(ref nativeError),Volatile.Read(ref failedContext)?.OperationId);
    public IReadOnlyList<DiagnosticRecord> Recent=>recent.ToArray();
    public void Write(DiagnosticRecord record)
    {
        try
        {
            if(Volatile.Read(ref disposed)!=0||record.Level<(DiagnosticMode?LogLevel.Trace:LogLevel.Information))return;
            var safe=DiagnosticRecordSafety.Clean(record);if(safe is null){Interlocked.Increment(ref dropped);return;}
            recent.Enqueue(safe);while(recent.Count>512)recent.TryDequeue(out _);
            if(queue.Writer.TryWrite((safe,null)))Interlocked.Increment(ref accepted);else Interlocked.Increment(ref dropped);
        }
        catch(Exception error){RecordFailure(error);}
    }
    public async Task FlushAsync(CancellationToken token=default)
    {
        if(Volatile.Read(ref disposed)!=0)return;
        var barrier=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await queue.Writer.WriteAsync((null,barrier),token).ConfigureAwait(false);await barrier.Task.WaitAsync(token).ConfigureAwait(false);
    }
    private async Task WriterAsync()
    {
        Stream? stream=null;DateOnly day=default;var sequence=0;long length=0;var broken=false;
        try
        {
            await foreach(var entry in queue.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                if(entry.Barrier is {} barrier)
                {try{if(stream is not null)await stream.FlushAsync().ConfigureAwait(false);}catch(Exception error){RecordFailure(error);}finally{barrier.TrySetResult();}continue;}
                if(broken)continue;
                try
                {
                    var bytes=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry.Record,Json)+"\n");
                    if(bytes.Length>32768){Interlocked.Increment(ref dropped);continue;}
                    var today=DateOnly.FromDateTime(DateTime.UtcNow);
                    if(stream is null||today!=day||length+bytes.Length>policy.FileBytes)
                    {
                        if(stream is not null)await stream.DisposeAsync().ConfigureAwait(false);
                        Directory.CreateDirectory(DirectoryPath);if((File.GetAttributes(DirectoryPath)&FileAttributes.ReparsePoint)!=0)throw new IOException("LogDirectoryLink");
                        CleanupCore();day=today;
                        var path=Path.Combine(DirectoryPath,$"session-{day:yyyyMMdd}-{SessionId:N}-{sequence++:D4}.jsonl");
                        stream=testOpen?.Invoke(path)??new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read,4096,FileOptions.Asynchronous);length=0;
                    }
                    await stream.WriteAsync(bytes).ConfigureAwait(false);await stream.FlushAsync().ConfigureAwait(false);length+=bytes.Length;Interlocked.Increment(ref written);
                }
                catch(Exception error){broken=true;RecordFailure(error,entry.Record?.Context);}
            }
        }
        catch(Exception error){RecordFailure(error);}
        finally{try{if(stream is not null)await stream.DisposeAsync().ConfigureAwait(false);}catch(Exception error){RecordFailure(error);}}
    }
    private FileInfo[] OwnedFiles()=>Directory.Exists(DirectoryPath)&&(File.GetAttributes(DirectoryPath)&FileAttributes.ReparsePoint)==0?new DirectoryInfo(DirectoryPath).GetFiles("session-*.jsonl",SearchOption.TopDirectoryOnly).Where(x=>(x.Attributes&FileAttributes.ReparsePoint)==0).OrderBy(x=>x.LastWriteTimeUtc).ToArray():[];
    private void CleanupCore()
    {
        var files=OwnedFiles();long total=files.Sum(x=>x.Length);var remaining=files.Length;
        foreach(var file in files)
        {
            if(file.LastWriteTimeUtc<DateTime.UtcNow.AddDays(-policy.Days)||remaining>=policy.Files||total>policy.TotalBytes-policy.FileBytes)
            {try{var size=file.Length;file.Delete();total-=size;remaining--;}catch(IOException error){RecordFailure(error);}catch(UnauthorizedAccessException error){RecordFailure(error);}}
        }
    }
    public async Task CleanupAsync(CancellationToken token=default)
    {await FlushAsync(token);await Task.Run(CleanupCore,token);}
    public async Task<DiagnosticPackageReview> ReviewExportAsync(Guid? operationId,CancellationToken token=default)
    {
        await FlushAsync(token);var result=await Task.Run(()=>ReadSafe(operationId),token);
        reviewed=result.Records;review=new(Guid.NewGuid(),operationId,result.Records.Length,result.Records.Count(x=>x.Level>=LogLevel.Error),result.Skipped,
            "manifest.json + events.jsonl only; sanitized symbols, internal IDs; no DB/settings/token/raw response; local file only");return review;
    }
    private (DiagnosticRecord[] Records,int Skipped) ReadSafe(Guid? operation)
    {
        var records=new List<DiagnosticRecord>();var skipped=0;long readBytes=0;
        foreach(var file in OwnedFiles())
        {
            if(file.Length>policy.FileBytes+32768){skipped++;continue;}
            foreach(var line in ReadLines(file.FullName))
            {
                readBytes+=line.Length;if(readBytes>policy.TotalBytes)return(records.ToArray(),skipped+1);
                if(line.Length>32768){skipped++;continue;}
                try{var value=JsonSerializer.Deserialize<DiagnosticRecord>(line,Json);var safe=value is null?null:DiagnosticRecordSafety.Clean(value);if(safe is null){skipped++;continue;}if(operation is null||safe.Context.OperationId==operation)records.Add(safe);}
                catch(JsonException){skipped++;}
            }
        }
        return(records.ToArray(),skipped);
    }
    private static IEnumerable<string> ReadLines(string path)
    {
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        using var reader=new StreamReader(stream);
        while(reader.ReadLine()is {} line)yield return line;
    }
    public async Task<string> ExportAsync(Guid reviewId,bool userReviewed,CancellationToken token=default)
    {
        if(!userReviewed||review?.ReviewId!=reviewId||reviewed is null)throw new InvalidOperationException("ReviewExportFirst");
        var snapshot=reviewed;var approved=review;reviewed=null;review=null;
        return await Task.Run(()=>
        {
            var directory=Path.Combine(DirectoryPath,"exports");Directory.CreateDirectory(directory);
            if((File.GetAttributes(DirectoryPath)&FileAttributes.ReparsePoint)!=0||(File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0)throw new IOException("LogDirectoryLink");
            var target=Path.Combine(directory,$"marketcrate-diagnostics-{reviewId:N}.zip");
            var temporary=target+".tmp";
            try
            {
                using(var file=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                using(var archive=new ZipArchive(file,ZipArchiveMode.Create))
                {
                    using(var manifest=new StreamWriter(archive.CreateEntry("manifest.json").Open(),new UTF8Encoding(false)))manifest.Write(JsonSerializer.Serialize(new{SchemaVersion=1,Review=approved,Health,AppVersion=snapshot.FirstOrDefault()?.AppVersion,BuildVersion=snapshot.FirstOrDefault()?.BuildVersion,Commit=snapshot.FirstOrDefault()?.Commit,Environment=new{OS="Windows",Runtime=System.Environment.Version.ToString(),Architecture=System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()},Notice="Local diagnostic snapshot, not transaction authority. No automatic upload. Interrupted/missing events may occur."},Json));
                    using(var events=new StreamWriter(archive.CreateEntry("events.jsonl").Open(),new UTF8Encoding(false)))foreach(var value in snapshot){token.ThrowIfCancellationRequested();var safe=DiagnosticRecordSafety.Clean(value);if(safe is not null)events.WriteLine(JsonSerializer.Serialize(safe,Json));}
                }
                token.ThrowIfCancellationRequested();File.Move(temporary,target);
            }
            catch{try{File.Delete(temporary);}catch(IOException error){RecordFailure(error);}catch(UnauthorizedAccessException error){RecordFailure(error);}throw;}
            return target;
        },token).ConfigureAwait(false);
    }
    public void Dispose()
    {if(Interlocked.Exchange(ref disposed,1)!=0)return;queue.Writer.TryComplete();try{writer.Wait(TimeSpan.FromSeconds(1));}catch{/* Best effort; never retry a sale because a log failed. */}}
}
