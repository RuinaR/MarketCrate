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
    public SteamTradingPolicy Policy { get; private set; }
    private readonly bool synthetic;
    private readonly HashSet<int> verifiedApps=[];
    public SteamFailure? Failure => http.Halted;
    public SteamFailure? FeatureFailure(string endpoint)=>http.FeatureFailure(endpoint);
    public SteamExperimentalWorkspace(string isolatedDataDirectory, int appId, string contextId, string game,
        SteamTradingPolicy? policy = null, HttpMessageHandler? contractHandler = null, TimeProvider? clock = null,HttpMessageHandler? imageHandler=null)
    {
#if MARKETCRATE_LIVE_ONLY
        if(contractHandler is not null||imageHandler is not null||clock is not null||policy?.Enabled==true)throw new SteamException(SteamFailure.Disabled);
#endif
        synthetic=contractHandler is not null;
        if(policy?.Enabled==true&&!synthetic)throw new SteamException(SteamFailure.Disabled);
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
    // Explicit UI action, read-only preflight. No secret import, trade POST or persisted activation.
    public async Task EnableUserTradingAsync(int appId,string context,string game,bool unofficialRisk,bool sessionConsent,CancellationToken token)
    {
        Access.RevokeTrading();
        if(!unofficialRisk||!sessionConsent||!Authentication.Connected||Failure is not null)throw new SteamException(SteamFailure.Disabled);
        using var operation=DiagnosticHub.Operation(DiagnosticEvent.FinalValidation,DiagnosticArea.Sales,"F01","TradingReadiness");
        var account=Authentication.SteamId!;
        var wallet=await SteamFees.FetchTradingWalletAsync(http,account,token);
        var schedule=await SteamFees.FetchStructureAsync(http,appId,wallet,token);
        var provider=new SteamInventoryProvider(http,Authentication,appId,context,game);
        var assets=await provider.GetAsync(token);
        if(!assets.Any(x=>x.Marketable&&!x.Locked&&!x.Stackable&&store.Available(x)>0))throw new SteamException(SteamFailure.Restricted);
        if(assets.Where(x=>x.Marketable&&!x.Locked).Any(x=>!provider.PublisherFees.TryGetValue(x.Key,out var fee)||fee*100L!=schedule.Publisher.Millionths))throw new SteamException(SteamFailure.Restricted);
        var listings=await Orders.RefreshAsync(token);
        var samples=listings.Where(x=>x.Asset.AppId==appId&&x.Currency==schedule.Currency&&x.State==SaleState.Listed).ToArray();
        if(samples.Length==0||samples.Any(x=>schedule.FromSeller(x.SellerReceives).Fees!=x.Fees))throw new SteamException(SteamFailure.Restricted);
        token.ThrowIfCancellationRequested();
        if(!Authentication.Connected||Authentication.SteamId!=account||Failure is not null)throw new SteamException(SteamFailure.Authentication);
        verifiedApps.Add(appId);
        Policy=new(true,"User-selected unofficial session; NOT Valve permission",UserInitiated:true){VerifiedApps=[..verifiedApps]};
        Access.Acknowledge(true,true,true,true);Access.BindTradingAccount(account);
        SetGame(appId,context,game);
        DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Sales,DiagnosticOutcome.Completed,reason:"TradingEnabled",count:samples.Length,currency:schedule.Currency);
    }
    public Task<SteamFeeSchedule> FeesAsync(int appId, CancellationToken token = default) => SteamFees.FetchAsync(http, appId, token);
    public Task<SteamInventoryPage> ProbeInventoryAsync(int appId,string contextId,string game,CancellationToken token)=>new SteamInventoryProvider(http,Authentication,appId,contextId,game).ProbeAsync(token);
    public Task<SteamWalletObservation> ProbeWalletAsync(CancellationToken token)=>SteamFees.FetchWalletObservationAsync(http,token);
    public Task<SteamFeeStructure> ProbeFeeStructureAsync(int appId,CancellationToken token)=>SteamFees.FetchStructureObservationAsync(http,appId,token);
    public Task<SteamFeeSchedule> ProbeFeesAsync(int appId,SteamWallet wallet,CancellationToken token)=>SteamFees.FetchStructureAsync(http,appId,wallet,token);
    public void Dispose() { Authentication.Dispose(); Images.Dispose();http.Dispose(); store.Dispose(); }
}
