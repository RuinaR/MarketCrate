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
            ("Changed new-target book sends no sale and requires a fresh approval",NewBookChanged),
            ("Relist journal snapshot cannot change account or source listings",PlanImmutable),
            ("Real adapter progress is visible before batch completes without late callbacks",Progress),
            ("Selected cancellation command reaches actual server reconciliation without resend",CancelUiPath),
            ("Older pending relist plan remains selectable independently of newest plan",OlderPlan),
            ("Relist batch association and initial journal commit atomically on write failure",AtomicRelist),
            ("Cancellation-only activation needs no wallet fees or inventory and cannot sell",CancellationOnly),
            ("Cancellation-only capability resets on logout and cannot upgrade cached executor",CancellationReset),
            ("Logout during submission rejects late progress and preserves durable Unknown",ProgressLogout),
            ("Evicting eight-game view cache never omits selected assets from batch targets",SelectionEviction),
            ("Archive command cannot move a different account active journal",ArchiveOwnership),
            ("Ten thousand selected item prices publish bounded summary updates per product",PriceNotifications),
            ("User-requested TBH scope added once and explicit removal preserved after restart",Tbh),
            ("Diagnostics preserves failed operation after a later successful read and resets on logout",FailedOperation)};
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
    private static async Task NewBookChanged(){using var f=new Fixture();await PrepareNewTargets(f);await f.Vm.ReviewCancelledTargetsAsync();f.Server.TopQuantity=1;await f.Vm.SubmitAsync();Check(f.Server.Sells==0&&!f.Vm.ReviewOpen);await f.Vm.SubmitAsync();Check(f.Server.Sells==0&&f.Workspace!.Store.RelistPlans(f.Workspace.Authentication.SteamId!).Single().SubmissionBatch is null);await f.Vm.ReviewCancelledTargetsAsync();Check(f.Vm.ReviewOpen&&f.Server.Sells==0);await f.Vm.SubmitAsync();Check(f.Server.Sells==2);}
    private static async Task Progress(){using var f=new Fixture();await PrepareNewTargets(f);await f.Vm.ReviewCancelledTargetsAsync();f.Server.PauseSaleNumber=2;f.Server.SellStarted=new(TaskCreationOptions.RunContinuationsAsynchronously);f.Server.SaleRelease=new(TaskCreationOptions.RunContinuationsAsynchronously);var submitting=f.Vm.SubmitAsync();await f.Server.SellStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));Check(f.Vm.Busy&&!f.Vm.ReviewOpen&&f.Vm.WorkPage==3&&f.Vm.Results.Count==2&&f.Vm.Results[0].State==SaleState.AwaitingSteamGuard&&f.Vm.Results[1].State==SaleState.Submitting&&f.Vm.BatchProgressText.Contains("승인 대기 1"));f.Server.SaleRelease.SetResult();await submitting;Check(f.Vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard)&&f.Server.Sells==2);}
    private static async Task CancelUiPath(){using var f=new Fixture();await f.Owned();await f.Load();f.Vm.ReviewCancel.Execute(null);await f.Confirm();f.Vm.SelectedRequest=f.Workspace!.Store.Commands().First(x=>x.Kind=="cancel").RequestKey;Check(f.Vm.RefreshSelectedRequest.CanExecute(null));await f.Vm.RefreshSelectedRequestAsync();Check(f.Server.Sells==2&&f.Server.Cancels==2&&f.Vm.Results.Any(x=>x.State==SaleState.Reconciled)&&f.Vm.Status.Contains("새 판매 요청은 보내지"));}
    private static async Task OlderPlan(){using var f=new Fixture();await PrepareNewTargets(f);var first=f.Workspace!.Store.RelistPlans(f.Workspace.Authentication.SteamId!).Single();f.Workspace.Store.SaveRelistPlan(first with{Key=Guid.NewGuid().ToString("N"),CreatedAt=first.CreatedAt.AddSeconds(1)});await f.Vm.LoadListingsAsync();Check(f.Vm.PendingRelists.Count==2&&f.Vm.SelectedRelist?.Key==first.Key);f.Vm.SelectedRelist=f.Vm.PendingRelists.Single(x=>x.Key==first.Key);await f.Vm.ReviewCancelledTargetsAsync();Check(f.Vm.ReviewOpen&&f.Server.Cancels==2&&f.Server.Sells==0);}
    private static async Task AtomicRelist()
    {
        using var f=new Fixture();await PrepareNewTargets(f);await f.Vm.ReviewCancelledTargetsAsync();var plan=f.Workspace!.Store.RelistPlans(f.Workspace.Authentication.SteamId!).Single();
        var snapshot=new SaleBatchSnapshot(Guid.NewGuid(),plan.Account,DateTimeOffset.UtcNow,f.Vm.ReviewRows.Select((x,i)=>x.Snapshot(i)).ToImmutableArray());var record=new BatchRecord(1,snapshot,snapshot.Lines.Select(x=>new LineResult(x.LineId,SaleState.Ready,"synthetic pending")).ToImmutableArray());
        f.Workspace.Store.StageRelistApproval(plan.Key,snapshot);using var db=new Microsoft.Data.Sqlite.SqliteConnection("Data Source="+Path.Combine(f.Root,"steam-state.sqlite")+";Pooling=False");db.Open();using var command=db.CreateCommand();command.CommandText="CREATE TRIGGER synthetic_relist_failure BEFORE INSERT ON state WHEN NEW.kind='relist' BEGIN SELECT RAISE(ABORT,'synthetic write failure'); END";command.ExecuteNonQuery();
        try{await f.Workspace.Store.SaveAsync(record,default);throw new Exception("failed write accepted");}catch(Microsoft.Data.Sqlite.SqliteException){}
        Check(await f.Workspace.Store.LoadAsync(default)is null&&f.Workspace.Store.RelistPlans(plan.Account).Single().SubmissionBatch is null&&f.Server.Sells==0);
        command.CommandText="DROP TRIGGER synthetic_relist_failure";command.ExecuteNonQuery();await f.Workspace.Store.SaveAsync(record,default);Check((await f.Workspace.Store.LoadAsync(default))?.Snapshot.BatchId==snapshot.BatchId&&f.Workspace.Store.RelistPlans(plan.Account).Single().SubmissionBatch==snapshot.BatchId&&f.Server.Sells==0);
    }
    private static async Task CancellationOnly()
    {
        using var f=new Fixture(false);await f.Login();f.Server.External(1);await f.Vm.LoadListingsAsync();f.Vm.Game=f.Vm.Games.Single(x=>x.AppId==730);f.Vm.SelectAllListings.Execute(null);
        f.Arm();var old=f.Workspace!.Executor;Check(old.CanExecute);f.Server.RequestPaths.Clear();f.Vm.TradingConfirmation="실제 취소";await f.Vm.PrepareCancellationAsync();
        Check(f.Vm.CanCancel&&!f.Vm.CanTrade&&!old.CanExecute&&f.Workspace.Policy.CancellationOnly&&f.Workspace.Access.CancellationOnly);f.Vm.ReviewCancel.Execute(null);Check(f.Vm.ConfirmManagement.CanExecute(null));await f.Confirm();
        Check(f.Server.Cancels==1&&f.Server.Sells==0&&!f.Server.RequestPaths.Any(x=>x=="/market/"||x.Contains("feestructure")||x.StartsWith("/inventory/"))&&f.Workspace.Store.Commands().Single(x=>x.Kind=="cancel").AvailableBefore==-1);
    }
    private static async Task CancellationReset()
    {
        using var f=new Fixture(false);await f.Login();f.Server.External(1);await f.Vm.LoadListingsAsync();f.Vm.TradeAccepted=true;f.Vm.TradingConfirmation="실제 취소";await f.Vm.PrepareCancellationAsync();Check(f.Vm.CanCancel);f.Vm.Logout.Execute(null);await f.Login();Check(!f.Vm.CanCancel&&!f.Vm.CanTrade&&!f.Workspace!.Access.Trading&&f.Server.Cancels==0&&f.Server.Sells==0);
    }
    private static async Task ProgressLogout()
    {
        using var f=new Fixture();await PrepareNewTargets(f);await f.Vm.ReviewCancelledTargetsAsync();f.Server.SellStarted=new(TaskCreationOptions.RunContinuationsAsynchronously);f.Server.SaleRelease=new(TaskCreationOptions.RunContinuationsAsynchronously);var task=f.Vm.SubmitAsync();await f.Server.SellStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));var operation=f.Vm.LastOperationId;await f.Vm.LoadListingsAsync();Check(operation==f.Vm.LastOperationId);f.Vm.Logout.Execute(null);await task;Check(!f.Vm.Connected&&f.Vm.Results.Count==0);await f.Login();Check(f.Server.Sells==1&&f.Workspace!.Store.Commands().Any(x=>x.Kind=="sell"&&x.State==SaleState.Unknown)&&!f.Vm.CanTrade);
    }
    private static Task SelectionEviction()
    {
        using var vm=new SteamWorkspaceViewModel();var original=vm.Game;var row=new SteamAssetRow(new(new(original.AppId,original.ContextId,"10001"),original.Name,"Synthetic selected","Synthetic selected",3,true,true,false),3,()=>{}){Selected=true,Quantity=2};vm.Assets.Add(row);
        for(var i=0;i<10;i++){var scope=new SteamGameScope(1000+i,"2","Synthetic scope "+i);vm.Games.Add(scope);vm.Game=scope;vm.Assets.Add(new(new(new(scope.AppId,"2","20001"),scope.Name,"Unselected","Unselected",1,false,true,false),1,()=>{}));}
        Check(vm.SelectionSummary.Contains("1종")&&vm.SelectionSummary.Contains("2개"));vm.Game=original;Check(vm.Assets.Count==0&&vm.SelectionSummary.Contains("2개"));vm.ClearSelection.Execute(null);Check(vm.SelectionSummary.Contains("0종"));return Task.CompletedTask;
    }
    private static async Task ArchiveOwnership()
    {
        using var f=new Fixture();await f.Login();var asset=(await f.Workspace!.Inventory.GetAsync(default)).First();var line=new SaleLineSnapshot("other",asset,1,"KRW",f.Server.Schedule.FromBuyer(115),DateTimeOffset.UtcNow);var snapshot=new SaleBatchSnapshot(Guid.NewGuid(),"76561198000000002",DateTimeOffset.UtcNow,[line]);await f.Workspace.Store.SaveAsync(new(1,snapshot,[new(line.LineId,SaleState.Ready,"unsent")]),default);
        try{f.Workspace.Store.ArchiveCompleted(f.Workspace.Authentication.SteamId);throw new Exception("other account journal archived");}catch(SteamException error){Check(error.Code==SteamFailure.Restricted);}
        Check((await f.Workspace.Store.LoadAsync(default))?.Snapshot.BatchId==snapshot.BatchId&&f.Server.Sells==0&&f.Server.Cancels==0);
    }
    private static async Task PriceNotifications()
    {
        using var f=new Fixture();await f.Login();f.Server.Available.Clear();foreach(var index in Enumerable.Range(1,10000))f.Server.Available.Add((100000+index).ToString());await f.Vm.LoadAsync();f.Vm.SelectAll.Execute(null);var notifications=0;f.Vm.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(f.Vm.SelectionSummary))notifications++;};await f.Vm.RefreshPricesAsync();Check(f.Vm.Assets.Count==10000&&f.Vm.Assets.All(x=>x.Price is not null)&&notifications is >0 and <10&&f.Server.Sells==0&&f.Server.Cancels==0);
    }
    private static async Task Tbh(){using var f=new Fixture(false);await f.Login();var scope=f.Vm.Games.Single(x=>x.AppId==3678970);Check(scope.ContextId=="2"&&scope.Name=="TBH: Task Bar Hero"&&scope.State==InventoryScopeState.NotQueried&&!f.Server.RequestPaths.Any(x=>x.Contains("3678970")));f.Vm.Game=scope;f.Vm.RemoveScope.Execute(null);f.Vm.Logout.Execute(null);await f.Login();Check(!f.Vm.Games.Any(x=>x.AppId==3678970)&&f.Server.Sells==0&&f.Server.Cancels==0);}
    private static async Task PlanImmutable(){using var f=new Fixture();await PrepareNewTargets(f);var plan=f.Workspace!.Store.RelistPlans(f.Workspace.Authentication.SteamId!).Single();try{f.Workspace.Store.SaveRelistPlan(plan with{Account="76561198000000002"});throw new Exception("mutated plan accepted");}catch(InvalidDataException){}Check(f.Workspace.Store.RelistPlans(plan.Account).Single().SubmissionBatch is null);}
    private sealed class Fixture:IDisposable
    {
        private readonly string folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","usability-"+Guid.NewGuid().ToString("N"));
        public string Root=>folder;
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
    private static async Task FailedOperation(){var log=new Trace();using var trace=DiagnosticHub.Use(log);using var f=new Fixture();await f.Login();await f.Vm.SubmitAsync();var failed=f.Vm.LastFailedOperationId;Check(failed is not null&&log.Events.Any(x=>x.EventName=="UiFailure"&&x.Context.OperationId==failed));await f.Vm.LoadListingsAsync();Check(f.Vm.LastOperationId!=failed&&f.Vm.DiagnosticOperationId==failed&&f.Vm.Connected);f.Vm.Logout.Execute(null);Check(f.Vm.LastFailedOperationId is null);}
    private static void Check(bool value){if(!value)throw new Exception("usability contract assertion");}
}
