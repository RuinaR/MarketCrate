using System.Text.Json;

namespace MarketCrate.Infrastructure.Tbh;

// Public reference feed, not an authenticated Steam orderbook or execution price source.
// The site's published constants module uses BRL as its base currency (rate=1).
public sealed record TbhIndexQuote(long? BuyMinor,long? SellMinor,DateTimeOffset? ObservedAt);
public sealed class TbhIndexMarketClient : IDisposable
{
    private readonly HttpClient client;
    private readonly SemaphoreSlim gate=new(1,1);
    private IReadOnlyDictionary<string,TbhIndexQuote>? cache;
    private DateTimeOffset fetched;
    public TbhIndexMarketClient(HttpMessageHandler? handler=null)
    {
        client=new(handler??new HttpClientHandler{UseCookies=false,AllowAutoRedirect=false,MaxConnectionsPerServer=1}){Timeout=TimeSpan.FromSeconds(20)};
    }
    public async Task<IReadOnlyDictionary<string,TbhIndexQuote>> ReadAsync(bool force=false,CancellationToken token=default)
    {
        await gate.WaitAsync(token);
        try
        {
            if(!force&&cache is not null&&DateTimeOffset.UtcNow-fetched<TimeSpan.FromMinutes(5))return cache;
            using var request=new HttpRequestMessage(HttpMethod.Get,"https://api.tbhindex.com/api/items");
            using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
            response.EnsureSuccessStatusCode();
            if(response.Content.Headers.ContentLength>16*1024*1024)throw new InvalidDataException("IndexSize");
            using var stream=await response.Content.ReadAsStreamAsync(token);
            using var buffer=new MemoryStream();var bytes=new byte[8192];int count;
            while((count=await stream.ReadAsync(bytes,token))>0){if(buffer.Length+count>16*1024*1024)throw new InvalidDataException("IndexSize");await buffer.WriteAsync(bytes.AsMemory(0,count),token);}
            var result=Parse(buffer.ToArray());token.ThrowIfCancellationRequested();cache=result;fetched=DateTimeOffset.UtcNow;return result;
        }
        finally{gate.Release();}
    }
    public static IReadOnlyDictionary<string,TbhIndexQuote> Parse(ReadOnlyMemory<byte> bytes)
    {
        using var doc=JsonDocument.Parse(bytes,new JsonDocumentOptions{MaxDepth=12});var root=doc.RootElement;
        if(root.ValueKind==JsonValueKind.Object&&root.TryGetProperty("items",out var items))root=items;
        if(root.ValueKind!=JsonValueKind.Array||root.GetArrayLength()>20000)throw new InvalidDataException("IndexEnvelope");
        if(doc.RootElement.ValueKind==JsonValueKind.Object&&doc.RootElement.TryGetProperty("currency",out var currency)&&(currency.ValueKind!=JsonValueKind.String||currency.GetString()!="BRL"))throw new InvalidDataException("IndexCurrency");
        var result=new Dictionary<string,TbhIndexQuote>(StringComparer.Ordinal);
        foreach(var item in root.EnumerateArray())
        {
            if(item.ValueKind!=JsonValueKind.Object||!item.TryGetProperty("market_hash_name",out var name)||name.ValueKind!=JsonValueKind.String)throw new InvalidDataException("IndexItem");
            var hash=name.GetString()!;if(string.IsNullOrWhiteSpace(hash)||hash.Length>512||hash.Any(char.IsControl))throw new InvalidDataException("IndexName");
            DateTimeOffset? at=null;
            if(item.TryGetProperty("orderbook_updated_at",out var time)&&time.ValueKind==JsonValueKind.String&&System.Text.RegularExpressions.Regex.IsMatch(time.GetString()!,@"(?:Z|[+-][0-9]{2}:[0-9]{2})$")&&time.TryGetDateTimeOffset(out var parsed)&&parsed<=DateTimeOffset.UtcNow.AddMinutes(1))at=parsed;
            // No cross-side fallback, currency conversion, invented depth, or minimum-price clamp.
            if(!result.TryAdd(hash,new(Amount(item,"highest_buy_order"),Amount(item,"lowest_sell_order"),at)))throw new InvalidDataException("IndexDuplicate");
        }
        return result;
    }
    private static long? Amount(JsonElement row,string key)
    {
        if(!row.TryGetProperty(key,out var x)||x.ValueKind==JsonValueKind.Null)return null;
        if(x.ValueKind!=JsonValueKind.Number||!x.TryGetDecimal(out var value)||value<=0||value>long.MaxValue/100m||decimal.Truncate(value*100)!=value*100)return null;
        return decimal.ToInt64(value*100);
    }
    public void Dispose(){client.Dispose();}
}
