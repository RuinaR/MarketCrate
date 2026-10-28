using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

// Developer composition only. App startup continues to construct DemoDataProvider.
// Constructing this does not log in, open a browser, display a Window or send a request.
public sealed class SteamExperimentalWorkspace : IDisposable
{
    private readonly SteamStateStore store;
    private readonly SteamHttp http;
    public SteamAccess Access { get; } = new();
    public SteamQrAuthentication Authentication { get; }
    public SteamInventoryProvider Inventory { get; }
    public SteamMarketDataProvider Prices { get; }
    public SteamOrdersProvider Orders { get; }
    public SteamListingExecutor Executor { get; }
    public SaleBatchCoordinator Batches { get; }
    public SteamExperimentalWorkspace(string isolatedDataDirectory, int appId, string contextId, string game)
    {
        store = new(isolatedDataDirectory);
        http = new(Access); Authentication = new(http);
        Inventory = new(http, Authentication, appId, contextId, game);
        Prices = new(http, Authentication); Orders = new(http, Authentication);
        Executor = new(http, Authentication, store, Inventory, Prices, Orders);
        Batches = new(Executor, store);
    }
    public void Dispose() { Authentication.Dispose(); http.Dispose(); store.Dispose(); }
}
