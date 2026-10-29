using System.Collections.Immutable;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.Tests;
public static class UsabilityTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{("Normal product does not invent owned CS2 Dota or TF2",Default),
            ("Old unverified presets hidden without deleting saved scopes; observed 753 named correctly",Catalog),
            ("Game scope state count and image notifications reach bindings",Notifications),
            ("Ten external listings share book depth 3/4/3; preview sends no cancellation",Depth),
            ("Insufficient buy depth does not silently omit selected listings",Shortage),
            ("Exact average unavailable stays explicit and cannot become fabricated mean",Average),
            ("One approval reprices two journaled listings and preserves Guard state",Batch),
            ("External relisting without return evidence blocked before any cancellation",External),
            ("Changed book invalidates whole review before any cancellation",Changed),
            ("Lost cancel response preserves Unknown and unsent remainder across restart",Unknown),
            ("Changed return ID cannot be matched by same hash or increased inventory",Return),
            ("Busy label appears immediately and clears after cancellation",Busy),
            ("Recovery never modifies a different Steam account batch journal",OtherAccount),
            ("External changed asset IDs relist by fresh whole-asset approval after restart",NewTargets),
            ("Accepted cancellation alone never submits newly found assets",NoAutomaticSale),
            ("Unknown external cancellation blocks new asset review without retransmission",ExternalUnknown),
            ("Insufficient returned or owned quantity cannot create a partial relist review",NewShortage),
            ("Changed new-target book consumes no sales and cannot replay prior approval",NewBookChanged),
            ("Relist journal snapshot cannot change account or source listings",PlanImmutable)};
        int passed=0,failed=0;foreach(var(name,run)in tests)try{await run();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e);}return(passed,failed);
    }
    private static Task Default(){using var vm=new SteamWorkspaceViewModel();Check(vm.Games.Count==1&&vm.Game.AppId==753&&vm.Game.ContextId=="6"&&vm.Game.Name=="Steam 커뮤니티");return Task.CompletedTask;}
    private static async Task Catalog(){using var f=new Fixture(false);await f.Login();f.Workspace!.Store.SaveCatalog(f.Workspace.Authentication.SteamId!,[new(730,"2","Counter-Strike 2"),new(570,"2","Dota 2"),new(440,"2","Team Fortress 2"),new(753,"6","App 753 (판매목록 관측)")]);f.Vm.Logout.Execute(null);await f.Login();Check(f.Vm.Games.Count==1&&f.Vm.Game.Name=="Steam 커뮤니티"&&f.Workspace!.Store.ReadCatalog(f.Workspace.Authentication.SteamId!).Count==4&&f.Vm.CatalogNotice.Contains("3개"));f.Server.External(1);await f.Vm.LoadListingsAsync();Check(f.Vm.Games.Any(x=>x.AppId==730)&&!f.Vm.Games.Any(x=>x.AppId==570));f.Vm.AppInput="570";f.Vm.ContextInput="2";f.Vm.NameInput="Dota 2";f.Vm.AddScope.Execute(null);f.Vm.Logout.Execute(null);await f.Login();Check(f.Vm.Games.Any(x=>x.AppId==570)&&f.Workspace!.Store.ReadCatalog(f.Workspace.Authentication.SteamId!).Single(x=>x.AppId==570).Pinned);}
    private static Task Notifications(){var scope=new SteamGameScope(753,"6","Steam 커뮤니티");var names=new HashSet<string?>();scope.PropertyChanged+=(_,e)=>names.Add(e.PropertyName);scope.Update(InventoryScopeState.Loaded,8);Check(names.Contains(nameof(scope.StateText))&&names.Contains(nameof(scope.MarketableItems))&&scope.StateText=="조회 완료");return Task.CompletedTask;}
    private static async Task Depth(){using var f=new Fixture();await f.Login();f.Server.External(10);await f.Load();await f.Vm.PrepareListingRepriceAsync();Check(f.Vm.ManagementOpen&&f.Vm.Listings.Count(x=>x.NewPriceText=="115")==3&&f.Vm.Listings.Count(x=>x.NewPriceText=="113")==4&&f.Vm.Listings.Count(x=>x.NewPriceText=="110")==3&&f.Server.Cancels==0);}
    private static async Task Shortage(){using var f=new Fixture();await f.Login();f.Server.External(11);await f.Load();await f.Vm.PrepareListingRepriceAsync();Check(!f.Vm.ManagementOpen&&f.Vm.ListingPriceStatus.Contains("부족 1개")&&f.Server.Cancels==0);}
    private static async Task Average(){using var f=new Fixture();await f.Login();f.Server.External(1);await f.Load();f.Vm.ListingPricingMode=1;Check(!f.Vm.PrepareListingReprice.CanExecute(null)&&f.Vm.ListingPriceStatus.Contains("원자료")&&f.Server.Cancels==0);}
    private static async Task Batch(){using var f=new Fixture();await f.Owned();f.Server.TopQuantity=1;await f.Load();await f.Vm.PrepareListingRepriceAsync();Check(f.Vm.ManagementSummary.Contains("115")&&f.Vm.ManagementSummary.Contains("113"));await f.Confirm();Check(f.Server.Cancels==2&&f.Server.Sells==4&&f.Vm.Results.Count==2&&f.Vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard)&&f.Vm.WorkPage==3);f.Server.Publish();await f.Vm.RecoverAsync();Check(f.Vm.Results.Count(x=>x.LineId.StartsWith("reprice:")&&x.State==SaleState.Listed)==2&&f.Server.Sells==4&&f.Server.Cancels==2);}
    private static async Task External(){var log=new Trace();using var trace=DiagnosticHub.Use(log);using var f=new Fixture();await f.Login();f.Server.External(2);await f.Load();await f.Vm.PrepareListingRepriceAsync();f.Arm();await f.Confirm();Check(f.Server.Cancels==0&&f.Server.Sells==0&&f.Vm.ManagementEligibility.Contains("외부")&&!f.Vm.ConfirmManagement.CanExecute(null));Check(log.Events.Any(x=>x.Reason=="NoCausalEvidence"&&x.Endpoint=="RepriceReturn"&&x.Context.OperationId!=Guid.Empty));try{var rows=f.Vm.Listings.Select(x=>(x.Listing,115L,"synthetic book")).ToArray();await f.Workspace!.Executor.ValidateRepriceBatchAsync(rows,default);throw new Exception("external preflight accepted");}catch(SteamException error){Check(error.Code==SteamFailure.Unsupported);}Check(f.Server.Cancels==0);var safe=System.Text.Json.JsonSerializer.Serialize(log.Events);Check(!safe.Contains("76561198000000001")&&!safe.Contains("Synthetic stack"));}
    private static async Task Changed(){using var f=new Fixture();await f.Owned();await f.Load();await f.Vm.PrepareListingRepriceAsync();f.Server.TopQuantity=1;await f.Confirm();Check(f.Server.Cancels==0&&f.Server.Sells==2&&!f.Vm.ManagementOpen);}
    private static async Task Unknown(){using var f=new Fixture();await f.Owned();await f.Load();await f.Vm.PrepareListingRepriceAsync();f.Server.CancelTimeout=true;await f.Confirm();Check(f.Server.Cancels==1&&f.Server.Sells==2&&f.Vm.Results[0].State==SaleState.Unknown&&f.Vm.Results[1].State==SaleState.Ready);f.Vm.Logout.Execute(null);await f.Login();Check(f.Server.Cancels==1&&f.Server.Sells==2&&f.Workspace!.Store.Commands().Any(x=>x.Kind=="cancel"&&x.State==SaleState.Unknown));}
    private static async Task Return(){using var f=new Fixture();await f.Owned();await f.Load();await f.Vm.PrepareListingRepriceAsync();f.Server.ChangedReturn=true;await f.Confirm();Check(f.Server.Cancels==1&&f.Server.Sells==2&&f.Vm.Results[0].State==SaleState.Unknown);}
    private static async Task Busy(){using var f=new Fixture();await f.Login();f.Server.External(1);await f.Load();f.Server.BookStarted=new(TaskCreationOptions.RunContinuationsAsynchronously);f.Server.BookRelease=new(TaskCreationOptions.RunContinuationsAsynchronously);var task=f.Vm.PrepareListingRepriceAsync();await f.Server.BookStarted.Task;Check(f.Vm.Busy&&f.Vm.OperationLabel.Contains("호가")&&f.Vm.CancelOperation.CanExecute(null));f.Vm.CancelOperation.Execute(null);f.Server.BookRelease.SetResult();await task;Check(!f.Vm.Busy&&!f.Vm.ManagementOpen&&f.Server.Cancels==0);}
    private static async Task OtherAccount(){using var f=new Fixture();await f.Login();var asset=(await f.Workspace!.Inventory.GetAsync(default)).First();var line=new SaleLineSnapshot("other-account",asset,1,"KRW",f.Server.Schedule.FromBuyer(115),DateTimeOffset.UtcNow);var batch=new SaleBatchSnapshot(Guid.NewGuid(),"76561198000000002",DateTimeOffset.UtcNow,[line]);await f.Workspace.Store.SaveAsync(new(1,batch,[new(line.LineId,SaleState.Submitting,"synthetic intent")]),default);var before=System.Text.Json.JsonSerializer.Serialize(await f.Workspace.Store.LoadAsync(default));await f.Vm.RecoverAsync();Check(before==System.Text.Json.JsonSerializer.Serialize(await f.Workspace.Store.LoadAsync(default))&&f.Vm.Results.Count==0&&f.Server.Sells==0&&f.Server.Cancels==0);}
    private static async Task PrepareNewTargets(Fixture f)
    {
        await f.Login();f.Server.Available.Clear();f.Server.External(2);f.Server.ChangedReturn=true;await f.Load();await f.Vm.PrepareListingRepriceAsync();f.Arm();await f.Vm.CancelForNewReviewAsync();
        Check(f.Server.Cancels==2&&f.Server.Sells==0&&!f.Vm.ManagementOpen&&f.Workspace!.Store.RelistPlans(f.Workspace.Authentication.SteamId!).Count==1);
    }
    private static async Task NewTargets()
    {
        using var f=new Fixture();await PrepareNewTargets(f);f.Vm.Logout.Execute(null);await f.Login();f.Arm();await f.Vm.ReviewCancelledTargetsAsync();
        Check(f.Vm.ReviewOpen&&f.Vm.ReviewRows.Count==2&&f.Vm.ReviewRows.All(x=>x.Asset.Key.AssetId is "3100" or "3101")&&f.Vm.ReviewContextText.Contains("기존 보유분"));
        await f.Vm.SubmitAsync();Check(f.Server.Sells==2&&f.Server.Cancels==2&&f.Vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard)&&!f.Vm.ReviewOpen);
        await f.Vm.SubmitAsync();await f.Vm.ReviewCancelledTargetsAsync();Check(f.Server.Sells==2&&!f.Vm.ReviewOpen&&f.Workspace!.Store.RelistPlans(f.Workspace.Authentication.SteamId!).Single().SubmissionBatch is not null);
    }
    private static async Task NoAutomaticSale(){var log=new Trace();using var trace=DiagnosticHub.Use(log);using var f=new Fixture();await PrepareNewTargets(f);await f.Vm.ReviewCancelledTargetsAsync();Check(f.Vm.ReviewRows.Count==2&&f.Server.Sells==0&&f.Workspace!.Store.Commands().Where(x=>x.Kind=="cancel").All(x=>x.State==SaleState.Submitted));Check(log.Events.Any(x=>x.Reason=="NewAssetReview"&&x.Context.OperationId==f.Vm.LastOperationId)&&log.Events.Any(x=>x.Reason=="Prepared"&&x.JournalState=="Committed"));var json=System.Text.Json.JsonSerializer.Serialize(log.Events);Check(!json.Contains("76561198000000001")&&!json.Contains("Synthetic stack"));f.Vm.CloseReview.Execute(null);Check(!f.Vm.ReviewOpen&&f.Server.Sells==0);}
    private static async Task ExternalUnknown(){using var f=new Fixture();await f.Login();f.Server.External(2);f.Server.CancelTimeout=true;await f.Load();await f.Vm.PrepareListingRepriceAsync();f.Arm();await f.Vm.CancelForNewReviewAsync();await f.Vm.ReviewCancelledTargetsAsync();Check(f.Server.Cancels==1&&f.Server.Sells==0&&!f.Vm.ReviewOpen&&f.Vm.ListingPriceStatus.Contains("불명")&&f.Workspace!.Store.Commands().Single(x=>x.Kind=="cancel") is {Accepted:false,State:SaleState.Unknown});}
    private static async Task NewShortage(){using var f=new Fixture();await PrepareNewTargets(f);f.Server.Available.Clear();await f.Vm.ReviewCancelledTargetsAsync();Check(!f.Vm.ReviewOpen&&f.Vm.ListingPriceStatus.Contains("부족")&&f.Server.Sells==0);}
    private static async Task NewBookChanged(){using var f=new Fixture();await PrepareNewTargets(f);await f.Vm.ReviewCancelledTargetsAsync();f.Server.TopQuantity=1;await f.Vm.SubmitAsync();Check(f.Server.Sells==0&&!f.Vm.ReviewOpen);await f.Vm.SubmitAsync();Check(f.Server.Sells==0&&f.Workspace!.Store.RelistPlans(f.Workspace.Authentication.SteamId!).Single().SubmissionBatch is not null);}
    private static async Task PlanImmutable(){using var f=new Fixture();await PrepareNewTargets(f);var plan=f.Workspace!.Store.RelistPlans(f.Workspace.Authentication.SteamId!).Single();try{f.Workspace.Store.SaveRelistPlan(plan with{Account="76561198000000002"});throw new Exception("mutated plan accepted");}catch(InvalidDataException){}Check(f.Workspace.Store.RelistPlans(plan.Account).Single().SubmissionBatch is null);}
    private sealed class Fixture:IDisposable
    {
        private readonly string folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","usability-"+Guid.NewGuid().ToString("N"));
        public BulkRepriceContractServer Server{get;}=new();public SteamExperimentalWorkspace? Workspace;public SteamWorkspaceViewModel Vm{get;}
        public Fixture(bool synthetic=true){Vm=new(scope=>Workspace=new(folder,scope.AppId,scope.ContextId,scope.Name,new(true,"synthetic fixture only"),Server.Connection(),Server.Clock),synthetic){RiskAccepted=true,CredentialsAccepted=true};}
        public async Task Login(){await Vm.BeginAsync();Server.Clock.Advance();await Vm.CheckAsync();Check(Vm.Connected);}
        public void Arm(){Vm.TradeAccepted=true;Workspace!.Access.Acknowledge(true,true,true,true);}
        public async Task Load(){await Vm.LoadListingsAsync();Vm.SelectAllListings.Execute(null);}
        public async Task Owned(){await Login();Arm();var assets=await Workspace!.Inventory.GetAsync(default);var price=await Workspace.Prices.RefreshAsync(assets[0]);var batch=new SaleBatchSnapshot(Guid.NewGuid(),Workspace.Authentication.SteamId!,DateTimeOffset.UtcNow,assets.Select((a,i)=>new SaleLineSnapshot(i.ToString(),a,1,"KRW",Server.Schedule.FromBuyer(115),price.CapturedAt){OrderbookDigest=OrderbookFingerprint.Of(price)}).ToImmutableArray());await Workspace.Batches.ExecuteAsync(batch);Server.Publish();}
        public async Task Confirm(){Vm.ConfirmManagement.Execute(null);Vm.ConfirmManagement.Execute(null);using var limit=new CancellationTokenSource(10000);while(Vm.Busy)await Task.Delay(5,limit.Token);}
        public void Dispose(){Vm.Dispose();Server.Dispose();Directory.Delete(folder,true);}
    }
    private sealed class Trace:IDiagnosticLog{public Guid SessionId{get;}=Guid.NewGuid();public List<DiagnosticRecord> Events{get;}=[];public void Write(DiagnosticRecord value)=>Events.Add(value);}
    private static void Check(bool value){if(!value)throw new Exception("usability contract assertion");}
}
