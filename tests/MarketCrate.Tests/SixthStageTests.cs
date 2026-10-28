using System.IO.Compression;
using System.Net;
using System.Text.Json;
using MarketCrate.Core;
using MarketCrate.Infrastructure;
using MarketCrate.Infrastructure.Diagnostics;
using MarketCrate.Infrastructure.Steam;
using MarketCrate.App.ViewModels;

namespace MarketCrate.Tests;

public static class SixthStageTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new List<(string,Func<Task>)>
        {
            ("Diagnostic privacy whitelist exceptions export tampering",Privacy),
            ("Diagnostic default levels operation correlation and commit",Levels),
            ("Diagnostic rotation retention and reviewed operation ZIP",Rotation),
            ("Diagnostic denied/disk-full writer cannot change sale outcome",DiskFailure),
            ("Diagnostic bounded queue drops are visible no UI blocking",QueuePressure),
            ("Synthetic full login split batch Guard publish recovery trace",BatchTrace),
            ("Unknown response restart correlated evidence no resend",UnknownTrace),
            ("Inventory refresh switch filter retains/clamps selections",Selection),
            ("New reprice approval supersedes stale intent restart safe",RepriceRevision),
            ("Unknown cancellation cannot be revised or duplicated",UnknownCancel),
            ("Malformed response parsing and missing schema logs",Parsing),
            ("Storage corruption and migration failure preserve file and diagnose",StorageFault),
            ("Storage locked operation classified without secret SQL",StorageLocked),
            ("Diagnostics VM filter review consent export explicit folder command",DiagnosticUi),
            ("Fee mismatch traces Unknown and stops remaining split",FeeMismatch),
            ("Stale reprice stops BEFORE sending cancellation",StaleCancel),
            ("Partial batch failure preserved with committed transitions",PartialFailure),
            ("Failed journal cannot emit successful result or submit",JournalFailure),
            ("Price outlier warning freshness and diagnostic preference recovery",PriceAndSettings),
            ("Bounded SQLite page limit simulates full storage with root cause",SqliteFull)
        };
        foreach(var status in new[]{400,401,403,429,500,503})
        {
            var code=status;
            tests.Add(($"HTTP {code} classified linked and latched without replay",()=>HttpFailure(code)));
        }
        tests.Add(("HTTP timeout vs transport source exception sanitized",Timeout));
        int passed=0,failed=0;
        foreach(var(name,test)in tests){try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine($"FAIL {name}: {e}");}}
        return(passed,failed);
    }
    private sealed class Fixture:IDisposable
    {
        public string Folder{get;}=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage6-"+Guid.NewGuid().ToString("N"));
        public JsonDiagnosticLog Log{get;}
        private readonly IDisposable scope;
        public Fixture(LogPolicy? policy=null,Func<string,Stream>? open=null){Log=new(Folder,policy,open);scope=DiagnosticHub.Use(Log);}
        public void Dispose(){scope.Dispose();Log.Dispose();if(Directory.Exists(Folder))Directory.Delete(Folder,true);}
    }
    private static async Task Privacy()
    {
        using var f=new Fixture();
        const string secret="SECRET_COOKIE_ACCOUNT_76561198000000001_QR_PAYLOAD";
        try{ThrowSecret(secret);}catch(Exception e){DiagnosticHub.Emit(DiagnosticEvent.AppFault,DiagnosticArea.Lifecycle,DiagnosticOutcome.Failed,LogLevel.Error,exception:e,endpoint:"https://steamcommunity.com/?token="+secret,reason:secret);}
        await f.Log.FlushAsync();var text=string.Join('\n',Directory.GetFiles(f.Folder,"*.jsonl").Select(ReadShared));
        Check(!text.Contains(secret)&&!text.Contains("76561198000000001")&&!text.Contains("https://"));
        Check(text.Contains("ThrowSecret")&&text.Contains("System.InvalidOperationException"));
        var valid=f.Log.Recent.Single();
        // A local log file may be edited. Export validates again, not just at original emission.
        File.WriteAllText(Path.Combine(f.Folder,"session-injected.jsonl"),JsonSerializer.Serialize(valid with{Reason=secret,Endpoint=secret,Exception=new(secret,secret,[secret],[]),EventName=secret},JsonDiagnosticLog.Json));
        var review=await f.Log.ReviewExportAsync(null);var zip=await f.Log.ExportAsync(review.ReviewId,true);
        using var archive=ZipFile.OpenRead(zip);Check(archive.Entries.Count==2);
        foreach(var entry in archive.Entries){using var reader=new StreamReader(entry.Open());Check(!(await reader.ReadToEndAsync()).Contains(secret));}
    }
    private static string ReadShared(string path){using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);using var reader=new StreamReader(stream);return reader.ReadToEnd();}
    private static void ThrowSecret(string value)=>throw new InvalidOperationException(value,new IOException("Cookie="+value));
    private static async Task Levels()
    {
        using var f=new Fixture();DiagnosticHub.Emit(DiagnosticEvent.Settings,DiagnosticArea.Lifecycle,level:LogLevel.Debug);Equal(0,f.Log.Recent.Count);
        f.Log.DiagnosticMode=true;
        using(var operation=DiagnosticHub.Operation(DiagnosticEvent.Login,DiagnosticArea.Authentication,"A01"))
        {using var request=DiagnosticHub.Scope(correlation:true);DiagnosticHub.Emit(DiagnosticEvent.HttpRequest,DiagnosticArea.HTTP,level:LogLevel.Trace,endpoint:"AuthBegin",method:"POST");}
        await f.Log.FlushAsync();var events=f.Log.Recent;Equal(3,events.Count);Equal(1,events.Select(x=>x.Context.OperationId).Distinct().Count());
        Check(events.Any(x=>x.Context.CorrelationId is not null));Check(events.Last().DurationMs>=0);Check(events.All(x=>x.SessionId==f.Log.SessionId&&x.Utc.Offset==TimeSpan.Zero));
        Check(events.All(x=>x.AppVersion!="unavailable"&&x.Commit.Length==40));
    }
    private static async Task Rotation()
    {
        using var f=new Fixture(new(2048,8192,4,1,256));
        for(var i=0;i<22;i++)DiagnosticHub.Emit(DiagnosticEvent.Settings,DiagnosticArea.Lifecycle,count:i);
        await f.Log.FlushAsync();await f.Log.CleanupAsync();var files=Directory.GetFiles(f.Folder,"*.jsonl");Check(files.Length<=4&&files.Length>=2);Check(files.Sum(x=>new FileInfo(x).Length)<=8192);
        Guid id;using(DiagnosticHub.Scope()){id=DiagnosticHub.Context!.OperationId;DiagnosticHub.Emit(DiagnosticEvent.AppFault,DiagnosticArea.Lifecycle,DiagnosticOutcome.Failed,LogLevel.Error);}
        var review=await f.Log.ReviewExportAsync(id);Equal(1,review.Events);await Fails<InvalidOperationException>(()=>f.Log.ExportAsync(review.ReviewId,false));
        var zip=await f.Log.ExportAsync(review.ReviewId,true);using var archive=ZipFile.OpenRead(zip);using var reader=new StreamReader(archive.GetEntry("events.jsonl")!.Open());Check((await reader.ReadToEndAsync()).Contains(id.ToString()));
        await Fails<InvalidOperationException>(()=>f.Log.ExportAsync(review.ReviewId,true));
    }
    private static async Task DiskFailure()
    {
        foreach(var failure in new Exception[]{new UnauthorizedAccessException("synthetic token"),new IOException("synthetic disk full")})
        {
            using var f=new Fixture(open:_=>throw failure);var executor=new TestExecutor();var record=await new SaleBatchCoordinator(executor,new MemoryJournal()).ExecuteAsync(Batch());
            await f.Log.FlushAsync();Equal(2,executor.Calls);Equal(2,record.Results.Length);Check(f.Log.Health.WriteFailures>0);Equal("WriteFailure",f.Log.Health.State);
        }
    }
    private static async Task QueuePressure()
    {
        var started=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var f=new Fixture(new(4096,16384,4,1,2),_=>new SlowStream(started,release));
        DiagnosticHub.Emit(DiagnosticEvent.Settings,DiagnosticArea.Lifecycle);await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        for(var i=0;i<1000;i++)DiagnosticHub.Emit(DiagnosticEvent.Settings,DiagnosticArea.Lifecycle);
        Check(f.Log.Health.Dropped>=998);Check(f.Log.Recent.Count<=512);release.SetResult();await f.Log.FlushAsync();Check(f.Log.Health.Written>0);
    }
    private sealed class SlowStream(TaskCompletionSource started,TaskCompletionSource release):MemoryStream
    {
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,CancellationToken token=default){started.TrySetResult();await release.Task.WaitAsync(token);await base.WriteAsync(buffer,token);}
    }
    private static async Task BatchTrace()
    {
        using var f=new Fixture();using var app=new FifthStageTests.Fixture();await app.Login();await app.Prepare(10);app.EnableTrading();await app.Vm.SubmitAsync();
        Check(app.Vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard));app.Server.Publish=true;await app.Vm.RecoverAsync();Check(app.Vm.Results.All(x=>x.State==SaleState.Listed));
        await f.Log.FlushAsync();var events=f.Log.Recent;Check(events.Any(x=>x.EventId==(int)DiagnosticEvent.AuthState&&x.NewState=="Connected"));
        var results=events.Where(x=>x.EventId==(int)DiagnosticEvent.ItemResult&&x.NewState=="AwaitingSteamGuard").ToArray();Equal(3,results.Length);Equal(1,results.Select(x=>x.Context.OperationId).Distinct().Count());Equal(3,results.Select(x=>x.Context.OperationItemId).Distinct().Count());
        Check(events.Any(x=>x.EventId==(int)DiagnosticEvent.EvidenceChecked&&x.NewState=="Listed"));Check(events.Any(x=>x.EventId==(int)DiagnosticEvent.Transaction&&x.JournalState=="Committed"));
        var json=string.Join('\n',Directory.GetFiles(f.Folder,"*.jsonl").Select(ReadShared));foreach(var secret in new[]{"synthetic-cookie","synthetic-refresh","76561198000000001","synthetic-only","sessionid","Set-Cookie"})Check(!json.Contains(secret));
    }
    private static async Task UnknownTrace()
    {
        using var f=new Fixture();using var app=new FifthStageTests.Fixture();await app.Login();await app.Prepare(3);app.EnableTrading();app.Server.TimeoutSell=true;await app.Vm.SubmitAsync();Equal(SaleState.Unknown,app.Vm.Results.Single().State);app.Vm.Logout.Execute(null);app.Server.TimeoutSell=false;await app.Login();await app.Vm.RecoverAsync();Equal(1,app.Server.Sends);
        Check(f.Log.Recent.Any(x=>x.EventId==(int)DiagnosticEvent.ItemResult&&x.NewState=="Unknown"));Check(f.Log.Recent.Any(x=>x.EventId==(int)DiagnosticEvent.BatchRecovered));
    }
    private static async Task Selection()
    {
        using var f=new Fixture();using var app=new FifthStageTests.Fixture();await app.Login();await app.Vm.LoadAsync();var vm=app.Vm;vm.Assets[0].Quantity=7;vm.Assets[0].Selected=true;await vm.LoadAsync();Equal(7,vm.Assets[0].Quantity);Check(vm.Assets[0].Selected);
        vm.Search="absent";Equal(0,vm.VisibleAssets.Count);vm.SelectAll.Execute(null);Check(vm.Assets[0].Selected);vm.Search="Synthetic";Equal(1,vm.VisibleAssets.Count);
        vm.Game=vm.Games[1];Equal(0,vm.Assets.Count);vm.Game=vm.Games[0];app.Server.Free=3;await vm.LoadAsync();Equal(3,vm.Assets[0].Quantity);Check(vm.Assets[0].Selected);vm.ClearSelection.Execute(null);Check(!vm.Assets[0].Selected);
        vm.Logout.Execute(null);await app.Login();await vm.LoadAsync();Check(!vm.Assets[0].Selected);Check(f.Log.Recent.Any(x=>x.Reason=="SelectionRestored"));
        await app.Prepare(1);app.EnableTrading();await vm.SubmitAsync();app.Server.Publish=true;await vm.RecoverAsync();Equal(1,vm.VisibleListings.Count);vm.SelectAllListings.Execute(null);Check(vm.Listings.Single().Selected);vm.ClearListings.Execute(null);Check(!vm.Listings.Single().Selected);
    }
    private static async Task RepriceRevision()
    {
        using var f=new Fixture();using var app=new FifthStageTests.Fixture();app.Server.Free=1;await app.Login();await app.Prepare(1);app.EnableTrading();await app.Vm.SubmitAsync();app.Server.Publish=true;
        var workspace=app.Workspace!;var listing=(await workspace.Orders.RefreshAsync()).Single();await workspace.Executor.CancelAsync(listing,"old:cancel");
        workspace.Store.SaveReprice(new("old",workspace.Authentication.SteamId!,listing,116,DateTimeOffset.UtcNow.AddMinutes(-6),SteamRepriceState.AwaitingReturn));
        var next=await workspace.Executor.ReviseRepriceAsync("old",117,"new");Equal(SteamRepriceState.Submitted,next.State);Equal(SteamRepriceState.Superseded,workspace.Store.Reprice("old")!.State);Equal(1,app.Server.Cancels);Equal(2,app.Server.Sends);
        await Fails<SteamException>(()=>workspace.Executor.RepriceAsync(listing,116,"old"));app.Vm.Logout.Execute(null);
        using(var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(app.Directory,"steam-state.sqlite")};Pooling=False")){db.Open();using var schema=db.CreateCommand();schema.CommandText="PRAGMA user_version";Equal(3L,(long)schema.ExecuteScalar()!);}
        await app.Login();app.EnableTrading();await app.Workspace!.Executor.RepriceAsync(listing,117,"new");Equal(2,app.Server.Sends);
        Check(f.Log.Recent.Any(x=>x.Reason=="Superseded"&&x.JournalState=="Committed"));
    }
    private static async Task UnknownCancel()
    {
        using var f=new Fixture();using var app=new FifthStageTests.Fixture();app.Server.Free=1;await app.Login();await app.Prepare(1);app.EnableTrading();await app.Vm.SubmitAsync();app.Server.Publish=true;var listing=(await app.Workspace!.Orders.RefreshAsync()).Single();app.Server.TimeoutCancel=true;
        await Fails<SteamException>(()=>app.Workspace.Executor.RepriceAsync(listing,116,"uncertain"));app.Vm.Logout.Execute(null);app.Server.TimeoutCancel=false;await app.Login();app.EnableTrading();
        await Fails<SteamException>(()=>app.Workspace!.Executor.ReviseRepriceAsync("uncertain",117,"forbidden-new"));Equal(1,app.Server.Sends);Equal(1,app.Server.Cancels);Check(app.Workspace!.Store.Reprice("forbidden-new")is null);
        Check(f.Log.Recent.Any(x=>x.EventId==(int)DiagnosticEvent.CancelResult&&x.Reason=="NoCausalEvidence"));
    }
    private static Task Parsing()
    {
        using var f=new Fixture();Throws<SteamException>(()=>SteamContract.OrderBook("not JSON secret"));Throws<SteamException>(()=>SteamContract.Inventory("{\"success\":1}",730,"2","test"));
        Check(f.Log.Recent.Any(x=>x.Error==DiagnosticError.Parsing));Check(f.Log.Recent.Any(x=>x.Error==DiagnosticError.Schema));return Task.CompletedTask;
    }
    private static Task StorageFault()
    {
        using var f=new Fixture();var store=Path.Combine(f.Folder,"store");Directory.CreateDirectory(store);var path=Path.Combine(store,"steam-state.sqlite");File.WriteAllText(path,"not-a-database-secret");Throws<Microsoft.Data.Sqlite.SqliteException>(()=>new SteamStateStore(store));Equal("not-a-database-secret",File.ReadAllText(path));
        Check(f.Log.Recent.Any(x=>x.EventId==(int)DiagnosticEvent.StoreOpen&&x.Outcome==DiagnosticOutcome.Failed));File.Delete(path);
        using(var connection=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False")){connection.Open();using var command=connection.CreateCommand();command.CommandText="PRAGMA user_version=99";command.ExecuteNonQuery();}
        Throws<InvalidDataException>(()=>new SteamStateStore(store));Check(File.Exists(path));return Task.CompletedTask;
    }
    private static Task StorageLocked()
    {
        using var f=new Fixture();var path=Path.Combine(f.Folder,"demo");using var store=new SqliteDemoStore(path);
        using var blocker=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(path,"demo.sqlite")};Pooling=False");blocker.Open();using var transaction=blocker.BeginTransaction();
        Throws<Microsoft.Data.Sqlite.SqliteException>(()=>store.WriteSetting("private-setting","SECRET"));Check(f.Log.Recent.Any(x=>x.Error==DiagnosticError.Locked));return Task.CompletedTask;
    }
    private static async Task DiagnosticUi()
    {
        using var f=new Fixture();DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.UI,DiagnosticOutcome.Failed,LogLevel.Error);var opened=0;var vm=new DiagnosticsViewModel(f.Log,_=>opened++);Check(vm.Events.Count>0);vm.OpenFolder.Execute(null);Equal(1,opened);
        vm.OperationFilter="invalid";Check(!vm.ReviewPackage.CanExecute(null));vm.OperationFilter="";vm.ErrorsOnly=true;Check(vm.Events.All(x=>x.Record.Level>=LogLevel.Error));
        vm.ReviewPackage.Execute(null);await Until(()=>vm.ReviewPackage.CanExecute(null));Check(!vm.Export.CanExecute(null));vm.Reviewed=true;Check(vm.Export.CanExecute(null));vm.Export.Execute(null);await Until(()=>!vm.Export.CanExecute(null)&&Directory.Exists(Path.Combine(f.Folder,"exports")));Check(Directory.GetFiles(Path.Combine(f.Folder,"exports"),"*.zip").Length==1);
    }
    private static async Task FeeMismatch()
    {
        using var f=new Fixture();using var app=new FifthStageTests.Fixture();await app.Login();await app.Prepare(10);app.EnableTrading();app.Server.FeeConflict=true;await app.Vm.SubmitAsync();Equal(1,app.Server.Sends);Equal(SaleState.Unknown,app.Vm.Results.First().State);Check(f.Log.Recent.Any(x=>x.EventId==(int)DiagnosticEvent.ItemResult&&x.NewState=="Unknown"));
    }
    private static async Task StaleCancel()
    {
        using var f=new Fixture();using var app=new FifthStageTests.Fixture();app.Server.Free=1;await app.Login();await app.Prepare(1);app.EnableTrading();await app.Vm.SubmitAsync();app.Server.Publish=true;var listing=(await app.Workspace!.Orders.RefreshAsync()).Single();app.Workspace.Store.SaveReprice(new("stale",app.Workspace.Authentication.SteamId!,listing,116,DateTimeOffset.UtcNow.AddMinutes(-6),SteamRepriceState.AwaitingCancellation));
        await Fails<SteamException>(()=>app.Workspace.Executor.RepriceAsync(listing,116,"stale"));Equal(0,app.Server.Cancels);Equal(1,app.Server.Sends);
    }
    private static async Task PartialFailure()
    {
        using var f=new Fixture();var executor=new TestExecutor{Outcomes=[SaleState.Listed,SaleState.Failed]};var result=await new SaleBatchCoordinator(executor,new MemoryJournal()).ExecuteAsync(Batch());
        Equal(SaleState.Listed,result.Results[0].State);Equal(SaleState.Failed,result.Results[1].State);
        Check(f.Log.Recent.Any(x=>x.EventId==(int)DiagnosticEvent.ItemResult&&x.NewState=="Failed"&&x.JournalState=="Committed"));
        Check(f.Log.Recent.Any(x=>x.EventId==(int)DiagnosticEvent.ItemResult&&x.Amounts is not null&&x.Currency=="KRW"));
    }
    private static async Task JournalFailure()
    {
        using var f=new Fixture();var executor=new TestExecutor();await Fails<IOException>(()=>new SaleBatchCoordinator(executor,new MemoryJournal{Fail=true}).ExecuteAsync(Batch()));Equal(0,executor.Calls);
        Check(f.Log.Recent.Any(x=>x.EventId==(int)DiagnosticEvent.BatchFinished&&x.Error==DiagnosticError.Storage));Check(!f.Log.Recent.Any(x=>x.EventId==(int)DiagnosticEvent.ItemResult&&x.Outcome==DiagnosticOutcome.Succeeded));
    }
    private static Task PriceAndSettings()
    {
        using var f=new Fixture();var line=Batch().Lines[0];var row=new SteamReviewRow(line.Asset,1,115,new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[new(115,1)]),new("KRW",100,100,new(50000),new(100000)),()=>{});
        row.PriceText="1150";Check(row.PriceOutlier&&!row.Valid&&row.PriceWarning.Contains("가격 이상"));
        row.PriceText="116";Check(row.Valid);var old=new SteamReviewRow(line.Asset,1,115,new("KRW",DateTimeOffset.UtcNow.AddMinutes(-6),"synthetic",TimeSpan.Zero,[],[new(115,1)]),new("KRW",100,100,new(50000),new(100000)),()=>{});Check(!old.Valid&&old.PriceWarning.Contains("오래됨"));
        var path=Path.Combine(f.Folder,"demo");using(var store=new SqliteDemoStore(path))using(var vm=new MainViewModel(new(),store)){vm.Diagnostics.DiagnosticMode=true;Equal("true",store.ReadSetting("diagnostic-mode"));}
        f.Log.DiagnosticMode=false;using(var store=new SqliteDemoStore(path))using(var vm=new MainViewModel(new(),store))Check(vm.Diagnostics.DiagnosticMode);
        return Task.CompletedTask;
    }
    private static Task SqliteFull()
    {
        using var f=new Fixture();using var db=new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");db.Open();using var setup=db.CreateCommand();setup.CommandText="PRAGMA page_size=512; CREATE TABLE bounded(value BLOB); PRAGMA max_page_count=2;";setup.ExecuteNonQuery();
        using var insert=db.CreateCommand();insert.CommandText="INSERT INTO bounded VALUES(zeroblob(4096))";Throws<Microsoft.Data.Sqlite.SqliteException>(()=>StorageDiagnostics.NonQuery(insert));
        Check(f.Log.Recent.Any(x=>x.Error==DiagnosticError.Disk&&x.ServerCode==13));using var count=db.CreateCommand();count.CommandText="SELECT COUNT(*) FROM bounded";Equal(0L,(long)count.ExecuteScalar()!);return Task.CompletedTask;
    }
    private sealed class FaultHandler(int status,bool timeout=false):HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {Calls++;if(timeout)throw new TaskCanceledException("TOKEN=SECRET timeout");return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status){Content=new StringContent("SECRET response")});}
    }
    private static async Task HttpFailure(int status)
    {
        using var f=new Fixture();var access=new SteamAccess();access.Acknowledge(true,true,true,false);var handler=new FaultHandler(status);using var http=new SteamHttp(access,handler);
        using(var operation=DiagnosticHub.Operation(DiagnosticEvent.Login,DiagnosticArea.Authentication,"A01")){using var request=SteamHttp.Form(new("https://api.steampowered.com/IAuthenticationService/BeginAuthSessionViaQR/v1/"),new Dictionary<string,string>{{"SECRET","payload"}});await Fails<SteamException>(()=>http.SendAsync(request,default));}
        using(var request=SteamHttp.Form(new("https://api.steampowered.com/IAuthenticationService/BeginAuthSessionViaQR/v1/"),[]))await Fails<SteamException>(()=>http.SendAsync(request,default));Equal(1,handler.Calls);
        var response=f.Log.Recent.Single(x=>x.EventId==(int)DiagnosticEvent.HttpResponse);Equal(status,response.HttpStatus);Check(response.Context.CorrelationId is not null&&response.DurationMs>=0);
        Equal(status switch{401=>DiagnosticError.Authentication,403=>DiagnosticError.Forbidden,429=>DiagnosticError.RateLimited,>=500=>DiagnosticError.Server,_=>DiagnosticError.ClientRequest},response.Error);
    }
    private static async Task Timeout()
    {
        using var f=new Fixture();var access=new SteamAccess();access.Acknowledge(true,true,true,false);using var http=new SteamHttp(access,new FaultHandler(200,true));using var request=SteamHttp.Form(new("https://api.steampowered.com/IAuthenticationService/BeginAuthSessionViaQR/v1/"),[]);await Fails<SteamException>(()=>http.SendAsync(request,default));Check(f.Log.Recent.Any(x=>x.Error==DiagnosticError.Timeout));
    }
    private static SaleBatchSnapshot Batch(){var asset=new InventoryAsset(new(730,"2","101"),"game","test","test",2,true,true,true);return new(Guid.NewGuid(),"synthetic",DateTimeOffset.UtcNow,[new("a",asset,1,"KRW",Pricing.FromBuyer(115,new()),DateTimeOffset.UtcNow),new("b",asset,1,"KRW",Pricing.FromBuyer(115,new()),DateTimeOffset.UtcNow)]);}
    private static async Task Until(Func<bool> ready){using var timeout=new CancellationTokenSource(5000);while(!ready())await Task.Delay(10,timeout.Token);}
    private static void Check(bool value){if(!value)throw new Exception("Assertion failed");}
    private static void Equal<T>(T expected,T actual){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"Expected {expected}, actual {actual}");}
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
    private static async Task Fails<T>(Func<Task> action)where T:Exception{try{await action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
}
