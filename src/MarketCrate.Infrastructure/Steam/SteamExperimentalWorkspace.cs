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
    public SteamInventoryProvider InventoryFor(int appId,string contextId,string game)=>new(http,Authentication,appId,contextId,game);
    public SteamTradingPolicy Policy { get; private set; }
    private readonly bool synthetic;
    private readonly HashSet<int> verifiedApps=[];
    private readonly HashSet<int> singleRequestStackApps=[];
    public SteamFailure? Failure => http.Halted;
    public SteamFailure? FeatureFailure(string endpoint)=>http.FeatureFailure(endpoint);
    public SteamWalletObservation? WalletObservation{get;private set;}
    public DateTimeOffset? WalletObservedAt{get;private set;}
    private void ObserveWallet(SteamWalletObservation value){WalletObservation=value;WalletObservedAt=DateTimeOffset.UtcNow;}
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
    public async Task EnableUserCancellationAsync(bool unofficialRisk,bool sessionConsent,CancellationToken token)
    {
        Access.RevokeTrading();verifiedApps.Clear();
        if(!unofficialRisk||!sessionConsent||!Authentication.Connected||Failure is not null)throw new SteamException(SteamFailure.Disabled);
        var account=Authentication.SteamId!;var listings=await Orders.RefreshAsync(token);
        token.ThrowIfCancellationRequested();
        if(!Authentication.Connected||Authentication.SteamId!=account||Failure is not null)throw new SteamException(SteamFailure.Authentication);
        if(!listings.Any(x=>x.State==SaleState.Listed))throw new SteamException(SteamFailure.Restricted);
        Policy=new(true,"User-selected unofficial cancellation-only session; NOT Valve permission",UserInitiated:true){CancellationOnly=true};
        Access.Acknowledge(true,true,true,true);Access.RestrictToCancellation();Access.BindTradingAccount(account);
        SetGame(Inventory.AppId,Inventory.ContextId,Inventory.GameName);
        DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Management,requirement:"B08",reason:"CancellationEnabled",count:listings.Length);
    }
    public async Task EnableUserTradingAsync(int appId,string context,string game,bool unofficialRisk,bool sessionConsent,CancellationToken token)
    {
        Access.RevokeTrading();
        using var operation=DiagnosticHub.Operation(DiagnosticEvent.FinalValidation,DiagnosticArea.Sales,"F01","TradingReadiness");
        var check=SteamTradingCheck.Consent;
        void Step(SteamTradingCheck value){check=value;DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Sales,DiagnosticOutcome.Started,reason:"TradingCheck",to:check.ToString());}
        try
        {
            if(!unofficialRisk||!sessionConsent||!Authentication.Connected||Failure is not null)throw new SteamException(SteamFailure.Disabled);
            var account=Authentication.SteamId!;
            Step(SteamTradingCheck.WalletIdentity);
            SteamWalletObservation? wallet=null;
            var schedule=await SteamFees.FetchResolvedAsync(http,Authentication,appId,token,observed:read=>{wallet=read;Step(SteamTradingCheck.FeeContract);},requireWalletIdentity:true);
            Step(SteamTradingCheck.WalletConsistency);
            if(wallet is null||schedule.Currency!=wallet.Currency||schedule.MinimumWire!=wallet.MinimumWire||schedule.IncrementWire!=wallet.IncrementWire)throw new SteamException(SteamFailure.Restricted);
            Step(SteamTradingCheck.InventoryRead);
            var provider=new SteamInventoryProvider(http,Authentication,appId,context,game);
            var assets=await provider.GetAsync(token);
            Step(SteamTradingCheck.SaleableAssets);
            var singleStackContract=schedule.PublisherFallbackAppId==appId;
            if(!assets.Any(x=>x.Marketable&&!x.Locked&&(!x.Stackable||singleStackContract)&&store.Available(x)>0))throw new SteamException(SteamFailure.Restricted);
            Step(SteamTradingCheck.PublisherFees);
            if(assets.Where(x=>x.Marketable&&!x.Locked).Any(x=>!schedule.PublisherMatches(x.Key.AppId,provider.PublisherFees.TryGetValue(x.Key,out var fee)?fee:null)))throw new SteamException(SteamFailure.Restricted);
            if(schedule.PublisherFallbackAppId==appId&&assets.Any(x=>!provider.PublisherFees.ContainsKey(x.Key)))DiagnosticHub.Emit(DiagnosticEvent.FeeValidated,DiagnosticArea.Pricing,reason:"PublisherFromAppContract");
            Step(SteamTradingCheck.ListingsRead);
            var listings=await Orders.RefreshAsync(token);
            var samples=listings.Where(x=>x.Asset.AppId==appId&&x.Currency==schedule.Currency&&x.State==SaleState.Listed).ToArray();
            // Resolved server sell-dialog mode + wallet/app/item contracts replace the circular
            // prerequisite of having sold this game before. Existing listings remain a veto on conflict.
            Step(SteamTradingCheck.ListingFees);
            if(samples.Any(x=>schedule.FromSeller(x.SellerReceives).Fees!=x.Fees))throw new SteamException(SteamFailure.Restricted);
            token.ThrowIfCancellationRequested();
            Step(SteamTradingCheck.SessionCheck);
            if(!Authentication.Connected||Authentication.SteamId!=account||Failure is not null)throw new SteamException(SteamFailure.Authentication);
            verifiedApps.Add(appId);
            if(singleStackContract)singleRequestStackApps.Add(appId);else singleRequestStackApps.Remove(appId);
            Policy=new(true,"User-selected unofficial session; NOT Valve permission",UserInitiated:true){VerifiedApps=[..verifiedApps],SingleRequestStackApps=[..singleRequestStackApps]};
            Access.Acknowledge(true,true,true,true);Access.BindTradingAccount(account);
            SetGame(appId,context,game);
            DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Sales,DiagnosticOutcome.Completed,reason:"TradingEnabled",count:samples.Length,currency:schedule.Currency);
        }
        catch(Exception error)
        {
            Access.RevokeTrading();
            if(error is SteamException steam)steam.TradingCheck=check;
            DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Sales,error is OperationCanceledException?DiagnosticOutcome.Cancelled:DiagnosticOutcome.Blocked,LogLevel.Warning,error is OperationCanceledException?DiagnosticError.None:DiagnosticError.Validation,reason:"TradingCheck",from:check.ToString(),to:"Blocked");
            throw;
        }
    }
    public Task<SteamFeeSchedule> FeesAsync(int appId, CancellationToken token = default) => SteamFees.FetchResolvedAsync(http, Authentication, appId, token,ObserveWallet);
    public async Task<SteamWalletObservation> WalletAsync(CancellationToken token=default){if(!Authentication.Connected)throw new SteamException(SteamFailure.Authentication);var result=await SteamFees.FetchWalletObservationAsync(http,token);ObserveWallet(result);return result;}
#if MARKETCRATE_DEMO
    public Task<SteamInventoryPage> ProbeInventoryAsync(int appId,string contextId,string game,CancellationToken token)=>new SteamInventoryProvider(http,Authentication,appId,contextId,game).ProbeAsync(token);
    public Task<SteamWalletObservation> ProbeWalletAsync(CancellationToken token)=>SteamFees.FetchWalletObservationAsync(http,token);
    public Task<SteamFeeStructure> ProbeFeeStructureAsync(int appId,CancellationToken token)=>SteamFees.FetchStructureObservationAsync(http,appId,token);
    public Task<SteamFeeSchedule> ProbeFeesAsync(int appId,SteamWallet wallet,CancellationToken token)=>SteamFees.FetchStructureAsync(http,appId,wallet,token);
#endif
    public void Dispose() { Authentication.Dispose(); Images.Dispose();http.Dispose(); store.Dispose(); }
}
