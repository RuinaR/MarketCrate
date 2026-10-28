using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

// Session composition. The UI invokes its factory only on an explicit login action.
// Construction opens the isolated journal but does not log in, show a Window or send a request.
public sealed class SteamExperimentalWorkspace : IDisposable
{
    private readonly SteamStateStore store;
    private readonly SteamHttp http;
    public SteamImageCache Images{get;}
    public SteamAccess Access { get; } = new();
    public SteamQrAuthentication Authentication { get; }
    public SteamInventoryProvider Inventory { get; private set; }
    public SteamMarketDataProvider Prices { get; }
    public SteamOrdersProvider Orders { get; }
    public SteamListingExecutor Executor { get; private set; }
    public SaleBatchCoordinator Batches { get; private set; }
    public SteamStateStore Store => store;
    public SteamTradingPolicy Policy { get; }
    public SteamFailure? Failure => http.Halted;
    public SteamExperimentalWorkspace(string isolatedDataDirectory, int appId, string contextId, string game,
        SteamTradingPolicy? policy = null, HttpMessageHandler? contractHandler = null, TimeProvider? clock = null,HttpMessageHandler? imageHandler=null)
    {
        store = new(isolatedDataDirectory);Images=new(imageHandler);
        Policy = policy ?? SteamTradingPolicy.Blocked;
        http = new(Access, contractHandler); Authentication = new(http, clock);
        Inventory = new(http, Authentication, appId, contextId, game);
        Prices = new(http, Authentication); Orders = new(http, Authentication);
        Executor = new(http, Authentication, store, Inventory, Prices, Orders, Policy);
        Batches = new(Executor, store);
    }
    public void SetGame(int appId, string contextId, string game)
    {
        Inventory = new(http, Authentication, appId, contextId, game);
        Executor = new(http, Authentication, store, Inventory, Prices, Orders, Policy); Batches = new(Executor, store);
    }
    public Task<SteamFeeSchedule> FeesAsync(int appId, CancellationToken token = default) => SteamFees.FetchAsync(http, appId, token);
    public void Dispose() { Authentication.Dispose(); Images.Dispose();http.Dispose(); store.Dispose(); }
}
