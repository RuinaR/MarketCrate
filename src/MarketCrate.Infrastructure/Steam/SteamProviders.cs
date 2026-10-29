using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

public sealed class SteamInventoryProvider(SteamHttp http, SteamQrAuthentication auth, int appId, string contextId, string game) : IInventoryProvider
{
    internal int AppId=>appId;
    internal string ContextId=>contextId;
    internal string GameName=>game;
    public Dictionary<AssetKey,ItemImageReference> Images{get;}=[];
    internal Dictionary<AssetKey, int> PublisherFees { get; } = [];
    public int? PublisherFeeBasisPoints(AssetKey key)=>PublisherFees.TryGetValue(key,out var value)?value:null;
    public Capability Capability => auth.Connected && http.Halted is null ? Capability.Experimental : Capability.IntegrationBlocked;
    public async Task<IReadOnlyList<InventoryAsset>> GetAsync(CancellationToken cancellationToken)
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.InventoryLoaded,DiagnosticArea.Data,"C01");
        if (!auth.Connected || appId < 1 || !ulong.TryParse(contextId, out var context) || context == 0) throw new SteamException(SteamFailure.Authentication);
        var images=new Dictionary<AssetKey,ItemImageReference>();
        var all = new List<InventoryAsset>(); var fees = new Dictionary<AssetKey, int>(); var keys = new HashSet<AssetKey>(); var cursors = new HashSet<string>(); string? next = null;
        for (var page = 0; page < 100; page++)
        {
            var result=await PageAsync(next,cancellationToken);
            foreach (var asset in result.Assets)
                if (!keys.Add(asset.Key)) { http.Stop(SteamFailure.ContractChanged); throw new SteamException(SteamFailure.ContractChanged); } else all.Add(asset);
            DiagnosticHub.Emit(DiagnosticEvent.InventoryPage,DiagnosticArea.Data,count:result.Assets.Length,requirement:"C01");
            if(result.Images is {} pageImages)foreach(var image in pageImages)images.Add(image.Key,image.Value);
            next = result.Next;
            if (result.PublisherFees is { } pageFees) foreach (var fee in pageFees) fees.Add(fee.Key, fee.Value);
            if (next is null) { Images.Clear();foreach(var image in images)Images.Add(image.Key,image.Value);PublisherFees.Clear(); foreach (var fee in fees) PublisherFees.Add(fee.Key, fee.Value); return all; }
            if (!cursors.Add(next)) break;
        }
        http.Stop(SteamFailure.ContractChanged); throw new SteamException(SteamFailure.ContractChanged);
    }
    // One page only. Does not replace the full inventory/image/fee caches or claim complete counts.
    public Task<SteamInventoryPage> ProbeAsync(CancellationToken token=default)=>PageAsync(null,token);
    private async Task<SteamInventoryPage> PageAsync(string? next,CancellationToken token)
    {
        if(!auth.Connected)throw new SteamException(SteamFailure.Authentication);
        var path=$"https://steamcommunity.com/inventory/{auth.SteamId}/{appId}/{contextId}?l=english&count=1000";
        if(next is not null)path+="&start_assetid="+next;
        using var correlation=DiagnosticHub.Scope(correlation:true);using var request=new HttpRequestMessage(HttpMethod.Get,path);
        var body=await http.SendAsync(request,token);try{return SteamContract.Inventory(body,appId,contextId,game);}catch(SteamException error){error.Endpoint??="Inventory";throw;}
    }
}

public sealed class SteamMarketDataProvider(SteamHttp http, SteamQrAuthentication auth) : IMarketDataProvider
{
    private readonly Dictionary<(int App, string Hash), PriceSnapshot> cache = [];
    public Capability Capability => http.Access.ResearchRiskAccepted && http.Halted is null ? Capability.Experimental : Capability.IntegrationBlocked;
    // Synchronous domain interface is cache-only; network is always explicit and asynchronous.
    public PriceSnapshot Get(InventoryAsset asset)
    {
        if(!cache.TryGetValue((asset.Key.AppId,asset.MarketHashName),out var value)){DiagnosticHub.Emit(DiagnosticEvent.CacheRead,DiagnosticArea.Data,DiagnosticOutcome.CacheExpired);throw new SteamException(SteamFailure.Restricted);}
        DiagnosticHub.Emit(DiagnosticEvent.CacheRead,DiagnosticArea.Data,DateTimeOffset.UtcNow-value.CapturedAt>TimeSpan.FromMinutes(5)?DiagnosticOutcome.CacheExpired:DiagnosticOutcome.CacheHit,level:LogLevel.Debug);return value;
    }
    public async Task<PriceSnapshot> RefreshAsync(InventoryAsset asset, CancellationToken token = default)
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.PriceLoaded,DiagnosticArea.Data,"D07");
        var query = Uri.EscapeDataString(JsonSerializer.Serialize(new object[] { asset.Key.AppId, asset.MarketHashName }));
        using var correlation=DiagnosticHub.Scope(correlation:true);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://steamcommunity.com/market/orderbook?q=Load&qp=" + query);
        var body = await http.SendAsync(request, token);
        try
        {
            var book = SteamContract.OrderBook(body);
            // History is median buckets, NOT individual trades: keep Trades empty.
            var snapshot = new PriceSnapshot(book.Currency, DateTimeOffset.UtcNow, "Steam unofficial orderbook; historical VWAP unavailable", TimeSpan.Zero, [], book.Bids);
            if (cache.Count >= 256) cache.Clear();
            cache[(asset.Key.AppId, asset.MarketHashName)] = snapshot;
            return snapshot;
        }
        catch (SteamException error) { error.Endpoint??="Orderbook";throw; }
    }
    public async Task<PriceSnapshot> RefreshEstimateAsync(InventoryAsset asset,TimeSpan window,CancellationToken token=default)
    {
        var book=await RefreshAsync(asset,token);var rows=await GetMedianHistoryAsync(asset,token);var captured=DateTimeOffset.UtcNow;
        var evidence=PriceIndicators.MedianEstimate(rows.Select(x=>new MedianObservation(x.At,PriceIndicators.MajorToMinor(x.MedianMajor,book.Currency),x.Volume)),book.Currency,captured,window);
        var result=book with{Estimate=evidence};cache[(asset.Key.AppId,asset.MarketHashName)]=result;
        DiagnosticHub.Emit(DiagnosticEvent.PriceIndicator,DiagnosticArea.Pricing,evidence is null?DiagnosticOutcome.Blocked:DiagnosticOutcome.Succeeded,requirement:"D02",reason:"MedianEstimate",count:evidence?.Buckets??0,currency:book.Currency);return result;
    }
    public async Task<ImmutableArray<SteamHistoryPoint>> GetMedianHistoryAsync(InventoryAsset asset, CancellationToken token = default)
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.HistoryLoaded,DiagnosticArea.Data,"D01");
        if (!auth.Connected) throw new SteamException(SteamFailure.Authentication);
        using var correlation=DiagnosticHub.Scope(correlation:true);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://steamcommunity.com/market/pricehistory/?appid={asset.Key.AppId}&market_hash_name={Uri.EscapeDataString(asset.MarketHashName)}");
        var body = await http.SendAsync(request, token);
        try { return SteamContract.History(body); } catch (SteamException error) { error.Endpoint??="History";throw; }
    }
}

public sealed class SteamOrdersProvider(SteamHttp http, SteamQrAuthentication auth) : IMarketOrderProvider
{
    private ImmutableArray<SteamListing> cache = [];
    public Dictionary<string,ItemImageReference> Images{get;}=[];
    public Capability Capability => auth.Connected && http.Halted is null ? Capability.Experimental : Capability.IntegrationBlocked;
    public IReadOnlyList<LineResult> GetOrders() => cache.Select(x => new LineResult(x.ListingId, x.State, "Steam 목록 관측; 자동 갱신 없음")).ToArray();
    public async Task<ImmutableArray<SteamListing>> RefreshAsync(CancellationToken token = default)
    {
        using var operation=DiagnosticHub.Operation(DiagnosticEvent.ListingsLoaded,DiagnosticArea.Data,"B01");
        if (!auth.Connected) throw new SteamException(SteamFailure.Authentication);
        var images=new Dictionary<string,ItemImageReference>();
        var all = new Dictionary<string, SteamListing>(); int? total = null; var active = 0;
        for (var start = 0; start < 10000; start += 100)
        {
            var page=await PageAsync(start,token);
            if(page.Images is {} pageImages)foreach(var image in pageImages)images[image.Key]=image.Value;
            total ??= page.TotalActive;
            if (total != page.TotalActive) break; // Unstable pagination is not a complete authoritative snapshot.
            foreach (var row in page.Listings)
            {
                if (all.TryGetValue(row.ListingId, out var old) && old != row) { http.Stop(SteamFailure.ContractChanged); throw new SteamException(SteamFailure.ContractChanged); }
                if (all.TryAdd(row.ListingId, row) && row.State == SaleState.Listed) active++;
            }
            if (active == total) { Images.Clear();foreach(var image in images)Images.Add(image.Key,image.Value);cache = all.Values.ToImmutableArray(); return cache; }
            if (page.Listings.Count(x => x.State == SaleState.Listed) == 0) break;
        }
        throw new SteamException(SteamFailure.ContractChanged);
    }
    public Task<SteamListingsPage> ProbeAsync(CancellationToken token=default)=>PageAsync(0,token);
    private async Task<SteamListingsPage> PageAsync(int start,CancellationToken token)
    {
        if(!auth.Connected)throw new SteamException(SteamFailure.Authentication);
        using var correlation=DiagnosticHub.Scope(correlation:true);using var request=new HttpRequestMessage(HttpMethod.Get,$"https://steamcommunity.com/market/mylistings/render/?norender=1&start={start}&count=100");
        var body=await http.SendAsync(request,token);try{return SteamContract.Listings(body);}catch(SteamException error){error.Endpoint??="Listings";throw;}
    }
}

// Explicitly scoped first contract: USD floor/minimum fees. Other currencies remain submission-blocked.
public static class SteamFeeMath
{
    public static FeeQuote FromSeller(long cents, FeePolicy policy)
    {
        if (policy.SteamBasisPoints != 500 || policy.MinimumSteam != 1 || policy.MinimumGame != 1 || policy.GameBasisPoints is < 0 or > 1000)
            throw new SteamException(SteamFailure.Restricted);
        return Pricing.FromSeller(cents, policy);
    }
    public static void ValidateExact(SaleLineSnapshot line, FeePolicy policy)
    {
        if (line.Currency != "USD" || FromSeller(line.UnitQuote.SellerReceives, policy) != line.UnitQuote)
            throw new SteamException(SteamFailure.Restricted); // Never send Demo's synthetic fee gap to Steam.
    }
}
