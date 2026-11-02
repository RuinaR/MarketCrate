using System.Collections.Concurrent;
using System.Text.Json;
using MarketCrate.Core;
namespace MarketCrate.Infrastructure.Steam;

// Exact public catalogue identity only. No search prices are used as wallet quotes.
// Requests share SteamHttp's queue/limits but use its separate cookie-less client.
public sealed class SteamMarketImages(SteamHttp http)
{
    private sealed record Entry(ItemImageReference? Image,DateTimeOffset At);
    private readonly ConcurrentDictionary<string,Entry> cache=new(StringComparer.Ordinal);
    private readonly SemaphoreSlim[] gates=Enumerable.Range(0,32).Select(_=>new SemaphoreSlim(1,1)).ToArray();
    private readonly ConcurrentDictionary<string,(SteamFailure Code,DateTimeOffset At)> failures=new(StringComparer.Ordinal);
    public void Invalidate(string hash){cache.TryRemove(hash,out _);failures.TryRemove(hash,out _);}
    public async Task<ItemImageReference?> GetAsync(string hash,CancellationToken token=default)
    {
        if(string.IsNullOrWhiteSpace(hash)||hash.Length>512||hash.Any(char.IsControl))throw new ArgumentException("MarketImageIdentity");
        token.ThrowIfCancellationRequested();if(cache.TryGetValue(hash,out var hit)&&DateTimeOffset.UtcNow-hit.At<TimeSpan.FromMinutes(15))return hit.Image;
        if(failures.TryGetValue(hash,out var failed)&&DateTimeOffset.UtcNow-failed.At<TimeSpan.FromMinutes(5))throw new SteamException(failed.Code){Endpoint="MarketImage"};
        var gate=gates[(int)((uint)hash.GetHashCode(StringComparison.Ordinal)%32)];await gate.WaitAsync(token);
        try
        {
            if(failures.TryGetValue(hash,out failed)&&DateTimeOffset.UtcNow-failed.At<TimeSpan.FromMinutes(5))throw new SteamException(failed.Code){Endpoint="MarketImage"};
            if(cache.TryGetValue(hash,out hit)&&DateTimeOffset.UtcNow-hit.At<TimeSpan.FromMinutes(15))return hit.Image;
            using var operation=DiagnosticHub.Operation(DiagnosticEvent.ImageLoad,DiagnosticArea.Data,"B04");
            using var request=new HttpRequestMessage(HttpMethod.Get,"https://steamcommunity.com/market/search/render/?appid="+TbhScope.AppId+"&norender=1&start=0&count=10&search_descriptions=0&query="+Uri.EscapeDataString(hash));
            var body=await http.SendAsync(request,token,anonymousImage:true);var image=Parse(body,hash);
            if(cache.Count>=256){var oldest=cache.MinBy(x=>x.Value.At);cache.TryRemove(oldest.Key,out _);}cache[hash]=new(image,DateTimeOffset.UtcNow);
            DiagnosticHub.Emit(DiagnosticEvent.ImageMetadata,DiagnosticArea.Data,image is null?DiagnosticOutcome.Completed:DiagnosticOutcome.Succeeded,requirement:"B04",endpoint:"MarketImage",reason:image is null?"ImageMissing":"ImageReady");return image;
        }
        catch(SteamException error){if(failures.Count>=256)failures.TryRemove(failures.First().Key,out _);failures[hash]=(error.Code,DateTimeOffset.UtcNow);throw;}
        finally{gate.Release();}
    }
    public static ItemImageReference? Parse(string json,string expectedHash)=>SteamContract.Parse(json,root=>
    {
        if(!root.TryGetProperty("success",out var success)||success.ValueKind!=JsonValueKind.True&&!(success.ValueKind==JsonValueKind.Number&&success.GetInt32()==1))throw new SteamException(SteamFailure.ContractChanged);
        var results=root.GetProperty("results");if(results.ValueKind!=JsonValueKind.Array||results.GetArrayLength()>100)throw new SteamException(SteamFailure.ContractChanged);
        var matches=new List<ItemImageReference>();
        foreach(var row in results.EnumerateArray())
        {
            if(row.GetProperty("hash_name").GetString()!=expectedHash)continue;
            var asset=row.GetProperty("asset_description");
            if(SteamContract.Integer(asset.GetProperty("appid"))!=TbhScope.AppId||asset.GetProperty("market_hash_name").GetString()!=expectedHash)throw new SteamException(SteamFailure.ContractChanged);
            var icon=asset.TryGetProperty("icon_url",out var small)?small.GetString():null;
            if(string.IsNullOrEmpty(icon)&&asset.TryGetProperty("icon_url_large",out var large))icon=large.GetString();
            if(!string.IsNullOrEmpty(icon)){if(!SteamImageCache.ValidToken(icon))throw new SteamException(SteamFailure.ContractChanged);matches.Add(new(icon));}
        }
        if(matches.Select(x=>x.Token).Distinct(StringComparer.Ordinal).Count()>1)throw new SteamException(SteamFailure.ContractChanged);
        return matches.FirstOrDefault();
    });
}
