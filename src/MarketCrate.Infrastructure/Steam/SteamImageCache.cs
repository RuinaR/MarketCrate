using System.Net;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

public sealed record ItemImageReference(string? Token,bool Invalid=false);
public sealed record ImageCacheHealth(int Entries,long Bytes,int Inflight,bool Blocked);

// Separate client: NEVER shares Steam authentication, cookies, referrer or redirects.
public sealed class SteamImageCache:IDisposable
{
    private sealed class Flight{public readonly CancellationTokenSource Cancel=new();public Task<byte[]> Task=null!;public int Users;public bool Completed;}
    private readonly object gate=new();
    private readonly Dictionary<string,(byte[] Bytes,LinkedListNode<string> Node)> cache=[];
    private readonly LinkedList<string> lru=[];
    private readonly Dictionary<string,Flight> flights=[];
    private readonly SemaphoreSlim serial=new(1,1);
    private readonly HttpClient http;
    private readonly int capacity,maximum;
    private readonly bool synthetic;
    private long bytes;
    private bool blocked,disposed;
    public SteamImageCache(HttpMessageHandler? testHandler=null,int capacity=128,int maximum=16*1024*1024)
    {
        if(capacity<1||maximum<1024)throw new ArgumentException("ImageCacheLimits");
        this.capacity=capacity;this.maximum=maximum;synthetic=testHandler is not null;
        http=new(testHandler??new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false,UseProxy=false});http.Timeout=TimeSpan.FromSeconds(15);
    }
    public static bool ValidToken(string? value)=>value is {Length:>0 and <=2048}&&value.All(c=>char.IsAsciiLetterOrDigit(c)||c is '_' or '-');
    public static Uri ImageUri(string token)=>ValidToken(token)?new("https://community.fastly.steamstatic.com/economy/image/"+token+"/128fx128f"):throw new InvalidDataException("ImageTokenInvalid");
    public ImageCacheHealth Health{get{lock(gate)return new(cache.Count,bytes,flights.Count,blocked);}}
    public async Task<byte[]> GetAsync(string token,CancellationToken cancellation=default)
    {
        _=ImageUri(token);cancellation.ThrowIfCancellationRequested();Flight flight;
        lock(gate)
        {
            ObjectDisposedException.ThrowIf(disposed,this);
            if(blocked)throw new SteamException(SteamFailure.Forbidden);
            if(cache.TryGetValue(token,out var hit)){lru.Remove(hit.Node);lru.AddLast(hit.Node);DiagnosticHub.Emit(DiagnosticEvent.ImageLoad,DiagnosticArea.Data,DiagnosticOutcome.CacheHit,level:LogLevel.Debug,requirement:"B04",endpoint:"Image");return hit.Bytes;}
            if(!flights.TryGetValue(token,out flight!))
            {
                if(flights.Count>=32)throw new InvalidOperationException("ImageQueueFull");
                flight=new();flights.Add(token,flight);var owned=flight;
                flight.Task=Task.Run(()=>FetchAsync(token,owned));
                _=flight.Task.ContinueWith(t=>{_=t.Exception;},CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted|TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
            }
            flight.Users++;
        }
        try{return await flight.Task.WaitAsync(cancellation).ConfigureAwait(false);}
        finally{lock(gate){if(--flight.Users==0){if(flight.Completed)flight.Cancel.Dispose();else flight.Cancel.Cancel();}}}
    }
    private async Task<byte[]> FetchAsync(string token,Flight flight)
    {
        using var operation=DiagnosticHub.Operation(DiagnosticEvent.ImageLoad,DiagnosticArea.Data,"B04");
        using var requestScope=DiagnosticHub.Scope(correlation:true);var start=DateTimeOffset.UtcNow;var entered=false;
        try
        {
            await serial.WaitAsync(flight.Cancel.Token).ConfigureAwait(false);entered=true;
            lock(gate){if(blocked||disposed)throw new SteamException(SteamFailure.Forbidden);}
            if(!synthetic)await Task.Delay(250,flight.Cancel.Token).ConfigureAwait(false);
            using var request=new HttpRequestMessage(HttpMethod.Get,ImageUri(token));request.Headers.Accept.ParseAdd("image/png, image/jpeg");
            DiagnosticHub.Emit(DiagnosticEvent.HttpRequest,DiagnosticArea.HTTP,DiagnosticOutcome.Started,endpoint:"Image",method:"GET",requirement:"B04");
            using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,flight.Cancel.Token).ConfigureAwait(false);
            var status=(int)response.StatusCode;
            DiagnosticHub.Emit(DiagnosticEvent.HttpResponse,DiagnosticArea.HTTP,status==200?DiagnosticOutcome.Succeeded:DiagnosticOutcome.Failed,endpoint:"Image",status:status,started:start,elapsed:(long)(DateTimeOffset.UtcNow-start).TotalMilliseconds,requirement:"B04");
            if(status is 403 or 429){lock(gate)blocked=true;throw new SteamException(status==429?SteamFailure.RateLimited:SteamFailure.Forbidden);}
            if(response.StatusCode!=HttpStatusCode.OK||response.Content.Headers.ContentLength>512*1024||response.Content.Headers.ContentType?.MediaType is not("image/png" or "image/jpeg"))throw new InvalidDataException("ImageContract");
            await using var stream=await response.Content.ReadAsStreamAsync(flight.Cancel.Token).ConfigureAwait(false);using var buffer=new MemoryStream();var chunk=new byte[8192];int read;
            while((read=await stream.ReadAsync(chunk,flight.Cancel.Token).ConfigureAwait(false))>0){if(buffer.Length+read>512*1024)throw new InvalidDataException("ImageTooLarge");buffer.Write(chunk,0,read);}
            var data=buffer.ToArray();ValidateDimensions(data);
            lock(gate)
            {
                if(disposed||flight.Cancel.IsCancellationRequested)throw new OperationCanceledException();
                while(cache.Count>=capacity||bytes+data.Length>maximum){if(lru.First is null)throw new InvalidDataException("ImageCacheLimits");var key=lru.First.Value;bytes-=cache[key].Bytes.Length;cache.Remove(key);lru.RemoveFirst();}
                cache[token]=(data,lru.AddLast(token));bytes+=data.Length;
            }
            DiagnosticHub.Emit(DiagnosticEvent.ImageLoad,DiagnosticArea.Data,requirement:"B04",reason:"ImageReady");return data;
        }
        catch(Exception error)
        {
            DiagnosticHub.Emit(DiagnosticEvent.ImageLoad,DiagnosticArea.Data,error is OperationCanceledException?DiagnosticOutcome.Cancelled:DiagnosticOutcome.Failed,LogLevel.Warning,
                error is SteamException e?e.Code==SteamFailure.RateLimited?DiagnosticError.RateLimited:DiagnosticError.Forbidden:error is TaskCanceledException&&!flight.Cancel.IsCancellationRequested?DiagnosticError.Timeout:error is OperationCanceledException?DiagnosticError.Cancelled:error is InvalidDataException?DiagnosticError.Schema:DiagnosticError.Transport,requirement:"B04",endpoint:"Image",exception:error);throw;
        }
        finally
        {
            if(entered)serial.Release();
            lock(gate){flight.Completed=true;if(flights.TryGetValue(token,out var current)&&ReferenceEquals(current,flight))flights.Remove(token);if(flight.Users==0)flight.Cancel.Dispose();}
            // The final subscriber and worker jointly own CTS lifetime; no live waiter uses a disposed source.
        }
    }
    public static void ValidateDimensions(byte[] data)
    {
        int width=0,height=0;
        if(data.Length>=24&&data.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))
        {width=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(16,4));height=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(20,4));}
        else if(data.Length>4&&data[0]==255&&data[1]==216)
        {
            for(var i=2;i+3<data.Length;){if(data[i++]!=255)break;var marker=data[i++];if(marker==255){i--;continue;}if(marker is 0xd9 or 0xda)break;var length=(data[i]<<8)|data[i+1];if(length<2||i+length>data.Length)break;if(marker is 0xc0 or 0xc1 or 0xc2&&length>=7){height=(data[i+3]<<8)|data[i+4];width=(data[i+5]<<8)|data[i+6];break;}i+=length;}
        }
        if(width is <1 or >512||height is <1 or >512)throw new InvalidDataException("ImageDimensionsUnsupported");
    }
    public void Invalidate(string token){lock(gate){if(cache.Remove(token,out var found)){lru.Remove(found.Node);bytes-=found.Bytes.Length;}}}
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;foreach(var flight in flights.Values)flight.Cancel.Cancel();cache.Clear();lru.Clear();bytes=0;}http.Dispose();}
}
