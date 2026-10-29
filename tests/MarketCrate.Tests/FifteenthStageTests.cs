using System.Collections.Immutable;
using System.Net;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.Tests;

public static class FifteenthStageTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{
            ("Wallet canonical locale GET follows at most one same-origin hop",Redirect),
            ("Wallet redirect target boundaries reject security region query and external hosts",RedirectBoundary),
            ("Wallet redirect401 preserves explicit global auth stop",Redirect401),
            ("User session activation requires independent identity and fee evidence",Activation),
            ("User-mode adapter activation through Guard listing and reprice preserves journal",UserFlow),
            ("Wrong wallet account cannot activate or send a transaction",WrongAccount),
            ("Missing pricing metadata blocks; verified server sell-mode permits first sale without prior listing",MissingEvidence),
            ("User account binding is lost on consent revocation and logout",Revocation),
            ("Changed order quantity invalidates complete approval before first intent",ChangedBook),
            ("Multiple app scopes execute one immutable batch with one approval",MixedBatch),
            ("Unvalidated app in user mode blocks the entire mixed batch",UnvalidatedApp),
            ("All cancellations validate before any request is sent",CancelPreflight),
            ("Response reusing preexisting listing ID remains Unknown",ExistingId),
            ("KRW shared product demand allocates ten units as three four three",SharedDemand),
            ("Reprice stale book is rejected before cancellation or new intent",RepriceBook),
            ("Default and synthetic activation UI remain separate",DefaultBlocked),
            ("Activation failure reports exact read-only gate without enabling or sending",ActivationGates),
            ("Live default review validates contracts and never submits without final approval",DefaultReview),
            ("Adding another game after activation still validates its contract before whole review",AdditionalGame)
        };
        int passed=0,failed=0;foreach(var(name,test)in tests)try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e);}return(passed,failed);
    }
    private static async Task Redirect()
    {
        using var f=new Harness();await f.Login();f.Route.Redirect="https://steamcommunity.com/market/?l=koreana";
        var wallet=await f.Workspace.ProbeWalletAsync(default);Check(wallet.Currency=="KRW"&&f.Route.WalletCalls==2,"normal redirect failed");
        f.Route.AlwaysRedirect=true;await Fails(()=>f.Workspace.ProbeWalletAsync(default));Check(f.Route.WalletCalls==4,"redirect loop followed");
    }
    private static Task RedirectBoundary()
    {
        var source=new Uri("https://steamcommunity.com/market/");
        foreach(var target in new[]{"http://steamcommunity.com/market/","https://evil.invalid/market/","https://store.steampowered.com/market/","https://steamcommunity.com/login/home/","https://steamcommunity.com/market/?token=synthetic","https://steamcommunity.com/market/?l=koreana&auth=synthetic","https://steamcommunity.com/market/#token","https://steamcommunity.com/market/?l=unknown","https://steamcommunity.com/market/"})Check(!SteamHttp.NormalMarketTarget(source,new(target)),"unsafe redirect approved");
        Check(SteamHttp.NormalMarketTarget(source,new("https://steamcommunity.com/market/?l=english")),"known locale denied");return Task.CompletedTask;
    }
    private static async Task Redirect401(){using var f=new Harness();await f.Login();f.Route.Redirect="https://steamcommunity.com/market/?l=english";f.Route.Final401=true;await Fails(()=>f.Workspace.ProbeWalletAsync(default));Check(f.Workspace.Failure==SteamFailure.Authentication&&f.Route.WalletCalls==2,"401 bypassed");await Fails(()=>f.Workspace.Orders.RefreshAsync());Check(f.Server.Sends==0,"writes issued");}
    private static async Task Activation(){using var f=new Harness();await f.Login();Check(!f.Workspace.Executor.CanExecute,"default armed");await f.Arm();Check(f.Workspace.Executor.CanExecute&&f.Workspace.Policy.UserInitiated&&!f.Workspace.Policy.StackAmountsSupported,"readiness not applied");Check(f.Server.Sends==0&&f.Server.Cancels==0,"activation sent trade");}
    private static async Task ActivationGates()
    {
        foreach(var gate in new[]{SteamTradingCheck.WalletIdentity,SteamTradingCheck.SaleableAssets,SteamTradingCheck.PublisherFees,SteamTradingCheck.InventoryRead})
        {
            var trace=new GateLog();using var capture=DiagnosticHub.Use(trace);
            using var f=new Harness();await f.Login();
            if(gate==SteamTradingCheck.WalletIdentity)f.Route.Account="76561198000000002";
            if(gate==SteamTradingCheck.SaleableAssets)f.Server.Free=0;
            if(gate==SteamTradingCheck.PublisherFees)f.Server.HidePublisherFee=true;
            if(gate==SteamTradingCheck.InventoryRead)f.Server.PrivateInventory=true;
            SteamException? failure=null;try{await f.Arm();}catch(SteamException error){failure=error;}
            Check(failure?.TradingCheck==gate&&!f.Workspace.Access.Trading&&f.Server.Sends==0&&f.Server.Cancels==0,"missing or incorrect gate / trade enabled");
            var text=MarketCrate.App.ViewModels.SteamWorkspaceViewModel.TradingFailureText(failure!);
            Check(text.Contains("확인 단계:")&&!text.Contains("76561198000000002")&&!text.Contains("다른 조회"),"generic or private activation message");
            Check(trace.Events.Any(x=>x.Reason=="TradingCheck"&&x.PreviousState==gate.ToString()&&x.NewState=="Blocked"&&x.Context.OperationId!=Guid.Empty),"gate diagnostic missing cause/operation");
            var json=System.Text.Json.JsonSerializer.Serialize(trace.Events);Check(!json.Contains("76561198000000002")&&!json.Contains("wallet_fee_percent")&&!json.Contains("synthetic-refresh"),"gate diagnostic source leak");
        }
    }
    private sealed class GateLog:IDiagnosticLog{public Guid SessionId{get;}=Guid.NewGuid();public List<DiagnosticRecord> Events{get;}=[];public void Write(DiagnosticRecord record)=>Events.Add(record);}
    private static async Task DefaultReview()
    {
        using var f=new Harness();using var vm=new MarketCrate.App.ViewModels.SteamWorkspaceViewModel(_=>f.Workspace,false){RiskAccepted=true,CredentialsAccepted=true};
        Check(vm.TradeAccepted&&vm.ImagesEnabled&&!vm.CanTrade&&vm.TradingConfirmation=="실제 거래","live/image defaults or unverified capability");
        await vm.BeginAsync();f.Server.Clock.Advance();await vm.CheckAsync();
        var game=new MarketCrate.App.ViewModels.SteamGameScope(730,"2","Counter-Strike 2");vm.Games.Add(game);vm.Game=game;
        await vm.LoadAsync();Check(vm.Assets.Count==1,"inventory failed");vm.Assets[0].Selected=true;await vm.RefreshPricesAsync();
        f.Route.Account="76561198000000002";await vm.OpenTradingReviewAsync();var failed=vm.LastFailedOperationId;
        Check(!vm.CanTrade&&!vm.ReviewOpen&&vm.Status.Contains("지갑과 현재 로그인 계정")&&vm.Status==vm.TradingPreparation&&failed is not null,"activation UI lost precise failure");
        f.Route.Account="76561198000000001";await vm.LoadListingsAsync();Check(vm.DiagnosticOperationId==failed,"later success replaced failed operation");
        await vm.OpenTradingReviewAsync();Check(vm.CanTrade&&vm.ReviewOpen&&vm.ReviewRows.Count==1&&f.Server.Sends==0&&f.Server.Cancels==0,"default preview bypassed gates or final approval");
        vm.CloseReview.Execute(null);vm.TradingConfirmation="실제 취소";await vm.PrepareCancellationAsync();Check(vm.CanCancel&&!vm.CanTrade,"cancellation-only failed");
        await vm.OpenTradingReviewAsync();Check(vm.CanCancel&&!vm.CanTrade&&vm.ReviewOpen&&f.Server.Sends==0,"cancellation-only mode upgraded automatically");
        vm.CloseReview.Execute(null);vm.TradeAccepted=false;await vm.OpenTradingReviewAsync();Check(!vm.CanTrade&&vm.ReviewOpen&&f.Server.Sends==0,"disabled mode re-enabled automatically");
        vm.Logout.Execute(null);Check(!vm.CanTrade&&!vm.ImagesEnabled&&vm.ImageLoader is null&&vm.LastFailedOperationId is null,"logout kept capability or image requests");
    }
    private static async Task UserFlow()
    {
        using var f=new Harness();await f.Login();await f.Arm();var asset=(await f.Workspace.Inventory.GetAsync(default)).Single();var price=await f.Workspace.Prices.RefreshAsync(asset);
        var line=new SaleLineSnapshot("user-1",asset,1,"KRW",f.Server.Schedule.FromBuyer(115),price.CapturedAt){OrderbookDigest=OrderbookFingerprint.Of(price)};
        var batch=new SaleBatchSnapshot(Guid.NewGuid(),f.Workspace.Authentication.SteamId!,DateTimeOffset.UtcNow,[line]);var result=await f.Workspace.Batches.ExecuteAsync(batch);Check(result.Results.Single().State==SaleState.AwaitingSteamGuard,"guard conflated with listing");
        f.Server.Publish=false;result=await f.Workspace.Batches.RecoverAsync();Check(result!.Results.Single().State==SaleState.AwaitingSteamGuard,"pending lost");f.Server.Publish=true;result=await f.Workspace.Batches.RecoverAsync();Check(result!.Results.Single().State==SaleState.Listed,"listing evidence not reflected");
        var listing=(await f.Workspace.Orders.RefreshAsync()).Single(x=>x.ListingId!="900");var repriced=await f.Workspace.Executor.RepriceAsync(listing,116,"user-reprice",reviewedOrderbook:OrderbookFingerprint.Of(price));
        Check(repriced.State==SteamRepriceState.Submitted&&f.Server.Sends==2&&f.Server.Cancels==1&&f.Workspace.Store.AuditTrail("user-reprice").Count>0,"user saga or audit incomplete");
    }
    private static async Task AdditionalGame()
    {
        using var f=new Harness();f.Server.DynamicInventory=true;
        using var vm=new MarketCrate.App.ViewModels.SteamWorkspaceViewModel(_=>f.Workspace,false){RiskAccepted=true,CredentialsAccepted=true};
        await vm.BeginAsync();f.Server.Clock.Advance();await vm.CheckAsync();
        foreach(var app in new[]{730,440})
        {
            var game=new MarketCrate.App.ViewModels.SteamGameScope(app,"2",app==730?"Counter-Strike 2":"Team Fortress 2");vm.Games.Add(game);vm.Game=game;
            await vm.LoadAsync();vm.Assets[0].Selected=true;await vm.RefreshPricesAsync();await vm.OpenTradingReviewAsync();
            Check(vm.ReviewOpen&&vm.CanTrade&&f.Workspace.Policy.VerifiedApps.Contains(app)&&f.Server.Sends==0&&f.Server.Cancels==0,"new game's readiness skipped or preview sent trade");
            vm.CloseReview.Execute(null);
        }
        Check(vm.SelectionSummary.Contains("2")&&f.Workspace.Policy.VerifiedApps.Contains(730),"prior selection/verified game lost");
    }
    private static async Task WrongAccount(){using var f=new Harness();await f.Login();f.Route.Account="76561198000000002";await Fails(f.Arm);Check(!f.Workspace.Access.Trading&&f.Server.Sends==0,"account mismatch armed");}
    private static async Task MissingEvidence()
    {
        using var f=new Harness();await f.Login();f.Route.MissingMinimum=true;await Fails(f.Arm);Check(!f.Workspace.Executor.CanExecute,"missing pricing armed");f.Route.MissingMinimum=false;f.Server.Rows.Clear();await f.Arm();Check(f.Workspace.Access.Trading&&f.Server.Sends==0,"verified first-sale preflight failed or submitted a write");
    }
    private static async Task Revocation(){using var f=new Harness();await f.Login();await f.Arm();f.Workspace.Access.Acknowledge(true,true,true,false);f.Workspace.Access.Acknowledge(true,true,true,true);Check(!f.Workspace.Executor.CanExecute,"generic checkbox bypassed binding");await f.Arm();f.Workspace.Authentication.Dispose();Check(!f.Workspace.Access.Trading&&!f.Workspace.Executor.CanExecute,"logout retained arming");}
    private static async Task ChangedBook()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();var snapshot=f.Vm.ReviewRows.Single().Snapshot(0) with{OrderbookDigest="stale-approved-orderbook"};
        await Fails(()=>f.Workspace!.Batches.ExecuteAsync(new(Guid.NewGuid(),f.Workspace.Authentication.SteamId!,DateTimeOffset.UtcNow,[snapshot])));Check(f.Server.Sends==0&&f.Workspace!.Store.Commands().Count==0,"changed book submitted or intent created");
    }
    private static async Task MixedBatch()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();f.Server.DynamicInventory=true;await f.Prepare(1);f.EnableTrading();var first=f.Vm.ReviewRows.Single().Snapshot(0);var second=first with{LineId="other-game",Asset=first.Asset with{Key=new(570,"6","101"),Game="Dota 2"}};
        var batch=new SaleBatchSnapshot(Guid.NewGuid(),f.Workspace!.Authentication.SteamId!,DateTimeOffset.UtcNow,[first,second]);var result=await f.Workspace.Batches.ExecuteAsync(batch);
        Check(f.Server.Sends==2&&result.Results.All(x=>x.State==SaleState.AwaitingSteamGuard),"mixed scopes not submitted");await f.Workspace.Batches.ExecuteAsync(batch);Check(f.Server.Sends==2,"duplicate approval resent");
    }
    private static async Task UnvalidatedApp(){using var f=new Harness();await f.Login();await f.Arm();var asset=(await f.Workspace.Inventory.GetAsync(default)).Single() with{Key=new(570,"2","101")};var price=await f.Workspace.Prices.RefreshAsync(asset);var line=new SaleLineSnapshot("x",asset,1,"KRW",f.Server.Schedule.FromBuyer(115),price.CapturedAt){OrderbookDigest=OrderbookFingerprint.Of(price)};await Fails(()=>f.Workspace.Batches.ExecuteAsync(new(Guid.NewGuid(),f.Workspace.Authentication.SteamId!,DateTimeOffset.UtcNow,[line])));Check(f.Server.Sends==0,"unverified game sent");}
    private static async Task CancelPreflight(){using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();f.Server.Publish=true;f.Server.Rows.Add(("999",1,100));var rows=await f.Workspace!.Orders.RefreshAsync();var stale=rows[1] with{SellerReceives=999};await Fails(()=>f.Workspace.Executor.CancelBatchAsync([rows[0],stale],"stale-cancel"));Check(f.Server.Cancels==0,"partial cancellation before validation");}
    private static async Task ExistingId()
    {
        using var f=new Harness(new(true,"synthetic only",true));await f.Login();f.Workspace.Access.Acknowledge(true,true,true,true);f.Route.ReuseId=true;
        var asset=(await f.Workspace.Inventory.GetAsync(default)).Single();var price=await f.Workspace.Prices.RefreshAsync(asset);var line=new SaleLineSnapshot("x",asset,1,"KRW",f.Server.Schedule.FromBuyer(115),price.CapturedAt){OrderbookDigest=OrderbookFingerprint.Of(price)};
        var result=await f.Workspace.Batches.ExecuteAsync(new(Guid.NewGuid(),f.Workspace.Authentication.SteamId!,DateTimeOffset.UtcNow,[line]));Check(result.Results.Single().State==SaleState.Unknown&&f.Server.Sends==1,"external ID confirmed");
    }
    private static Task SharedDemand()
    {
        var asset=new InventoryAsset(new(730,"2","1"),"CS2","same","same",6,true,true,true);var other=asset with{Key=new(730,"2","2"),Quantity=4};var price=new PriceSnapshot("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[new(1000,3),new(980,4),new(950,3)]);
        var plan=SalePlanner.Build([new(asset,price,6),new(other,price,4)],true);var amounts=plan.Lines.GroupBy(x=>x.Allocation.BuyerPays).ToDictionary(x=>x.Key,x=>x.Sum(y=>y.Allocation.Quantity));Check(amounts[1000]==3&&amounts[980]==4&&amounts[950]==3&&plan.UnpricedQuantity==0,"shared demand duplicated");return Task.CompletedTask;
    }
    private static async Task RepriceBook(){using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();f.Server.Publish=true;var listing=(await f.Workspace!.Orders.RefreshAsync()).Single();await Fails(()=>f.Workspace.Executor.RepriceAsync(listing,116,"new-stale",reviewedOrderbook:"changed-book"));Check(f.Server.Cancels==0&&f.Workspace.Store.Reprices().Count==0,"stale approval cancelled");}
    private static async Task DefaultBlocked(){using var f=new FifthStageTests.Fixture(false);await f.Login();f.Vm.TradeAccepted=true;f.Vm.TradingConfirmation="실제 거래";Check(!f.Vm.PrepareTrading.CanExecute(null),"synthetic UI used real activation");Check(!f.Workspace!.Executor.CanExecute,"default bypassed");}
    private sealed class Harness:IDisposable
    {
        private readonly string folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage15-"+Guid.NewGuid().ToString("N"));
        public SteamContractServer Server{get;}=new(){Free=1,Publish=true};public Router Route{get;}public SteamExperimentalWorkspace Workspace{get;}
        public Harness(SteamTradingPolicy? policy=null){Server.Rows.Add(("900",1,100));Route=new(Server);Workspace=new(folder,730,"2","Counter-Strike 2",policy,Route,Server.Clock);}
        public async Task Login(){Workspace.Access.Acknowledge(true,true,true,false);await Workspace.Authentication.BeginAsync();Server.Clock.Advance();Check(await Workspace.Authentication.CheckAsync(),"login fixture failed");}
        public Task Arm()=>Workspace.EnableUserTradingAsync(730,"2","Counter-Strike 2",true,true,default);
        public void Dispose(){Workspace.Dispose();Server.Dispose();Directory.Delete(folder,true);}
    }
    private sealed class Router(SteamContractServer server):HttpMessageHandler
    {
        private readonly HttpMessageInvoker inner=new(server.Connection());public string Account="76561198000000001";public string? Redirect;public bool AlwaysRedirect,Final401,MissingMinimum,ReuseId;public int WalletCalls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            if(request.RequestUri!.AbsolutePath=="/market/")
            {
                WalletCalls++;if(Redirect is not null&&(AlwaysRedirect||request.RequestUri.Query.Length==0)){var r=new HttpResponseMessage(HttpStatusCode.Found){Content=new StringContent("")};r.Headers.Location=new(Redirect);return r;}
                if(Final401)return new(HttpStatusCode.Unauthorized){Content=new StringContent("{}")};
                return new(HttpStatusCode.OK){Content=new StringContent("<script>g_bLoggedIn = true; g_steamID = '"+Account+"'; g_rgWalletInfo = {\"wallet_currency\":16,"+(MissingMinimum?"":"\"wallet_market_minimum\":100,")+"\"wallet_currency_increment\":100};</script>")};
            }
            var response=await inner.SendAsync(request,token);if(ReuseId&&request.RequestUri.AbsolutePath=="/market/sellitem/"){response.Content.Dispose();response.Content=new StringContent("{\"success\":true,\"listingid\":\"900\"}");}return response;
        }
        protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
    }
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static async Task Fails(Func<Task> action){try{await action();}catch(SteamException){return;}throw new Exception("Expected typed Steam block");}
}
