using System.Diagnostics;
using System.Text.Json;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;
using MarketCrate.Infrastructure.Steam;

namespace MarketCrate.Tests;

public static class SeventhStageTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new (string,Func<Task>)[]{
            ("Image inventory/description identity and icon fallback",Metadata),
            ("Image host/token path validation and dimension bound",ImageSecurity),
            ("Image deduplication subscriber cancellation no credentials",ImageDedup),
            ("Image last subscriber cancellation and cache recovery",ImageCancel),
            ("Image MIME/size/status failures and 403/429 latch",ImageFaults),
            ("Image LRU bounded sequential load resource sample",ImageMemory),
            ("Catalog custom AppID/context persists per account and logout",Catalog),
            ("Catalog unsupported/private/empty distinguished no AppID probing",CatalogStates),
            ("Catalog invalid input does not expire session or change journal",CatalogInvalid),
            ("Median estimate methodology period volume exact units",Indicators),
            ("History estimate UI review snapshot one approval and guard",EstimateFlow),
            ("Exact average unavailable and strategy resets manual prices",UnavailableAverage),
            ("Median missing/changed samples block final submission",ChangedHistory),
            ("Missing publisher fee and stacked foreign increase fail closed",FeeAndReturn),
            ("New image/catalog/price operations logs safe and correlated",Logs),
            ("Old v3 journal and new catalog coexist across restart",JournalCompatibility),
            ("Median payment/currency contract flag blocks by default",MedianContractGate),
            ("Late history response discarded after price basis switch",LateHistory),
            ("Missing game fee and same-summary altered history cannot submit",StricterContracts)
        };var passed=0;var failed=0;foreach(var(name,test)in tests){try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine($"FAIL {name}: {e}");}}return(passed,failed);
    }
    private static Task Metadata()
    {
        using var server=new SteamContractServer{IconToken="small"};var raw=server.InventoryJson(3);var page=SteamContract.Inventory(raw,730,"2","game");Equal("small",page.Images!.Single().Value.Token);
        var large=raw.Replace("\"icon_url_large\":\"small\"","\"icon_url_large\":\"large\"");Equal("large",SteamContract.Inventory(large,730,"2","game").Images!.Single().Value.Token);
        Throws<SteamException>(()=>SteamContract.Inventory(raw.Replace("\"instanceid\":\"0\"","\"instanceid\":\"bad\""),730,"2","game"));
        var node=System.Text.Json.Nodes.JsonNode.Parse(raw)!;node["assets"]![0]!["classid"]="99";Throws<SteamException>(()=>SteamContract.Inventory(node.ToJsonString(),730,"2","game"));
        var bad=SteamContract.Inventory(raw.Replace("small","https://evil.test/token"),730,"2","game");Check(bad.Images!.Single().Value.Invalid);return Task.CompletedTask;
    }
    private static Task ImageSecurity()
    {
        foreach(var token in new[]{"","../x","x/y","x%2fy","https://steamcommunity.com/","x?token=secret",new string('a',2049)})Throws<InvalidDataException>(()=>SteamImageCache.ImageUri(token));
        using var server=new ImageContractServer();SteamImageCache.ValidateDimensions(server.Bytes);var big=server.Bytes.ToArray();big[16]=0x7f;Throws<InvalidDataException>(()=>SteamImageCache.ValidateDimensions(big));Throws<InvalidDataException>(()=>SteamImageCache.ValidateDimensions([1,2,3]));return Task.CompletedTask;
    }
    private static async Task ImageDedup()
    {
        using var server=new ImageContractServer{Release=new(TaskCreationOptions.RunContinuationsAsynchronously)};using var cache=new SteamImageCache(server);using var cancel=new CancellationTokenSource();
        var a=cache.GetAsync("shared",cancel.Token);var b=cache.GetAsync("shared");await server.Started.Task;cancel.Cancel();await Fails<OperationCanceledException>(()=>a);server.Release.SetResult();await b;await cache.GetAsync("shared");await cache.GetAsync("other");Equal(2,server.Calls);Check(!server.CredentialLeak);Equal(2,cache.Health.Entries);
    }
    private static async Task ImageCancel()
    {
        using var server=new ImageContractServer{Release=new(TaskCreationOptions.RunContinuationsAsynchronously)};using var cache=new SteamImageCache(server);using var cancel=new CancellationTokenSource();var pending=cache.GetAsync("cancel",cancel.Token);await server.Started.Task;cancel.Cancel();await Fails<OperationCanceledException>(()=>pending);await Until(()=>cache.Health.Inflight==0);Equal(0,cache.Health.Entries);server.Release.SetResult();await cache.GetAsync("cancel");Equal(2,server.Calls);
    }
    private static async Task ImageFaults()
    {
        foreach(var status in new[]{302,403,429,500}){using var server=new ImageContractServer{Status=status};using var cache=new SteamImageCache(server);await Fails<Exception>(()=>cache.GetAsync("image"));server.Status=200;if(status is 403 or 429){await Fails<SteamException>(()=>cache.GetAsync("image"));Equal(1,server.Calls);}else {await cache.GetAsync("image");Equal(2,server.Calls);}}
        using var wrong=new ImageContractServer{Mime="text/html"};using var loader=new SteamImageCache(wrong);await Fails<InvalidDataException>(()=>loader.GetAsync("bad"));wrong.Mime="image/png";wrong.Bytes=new byte[512*1024+1];await Fails<InvalidDataException>(()=>loader.GetAsync("large"));Equal(0,loader.Health.Entries);
    }
    private static async Task ImageMemory()
    {
        using var server=new ImageContractServer();using var cache=new SteamImageCache(server,4,1024);using var own=Process.GetCurrentProcess();var cpuStart=own.TotalProcessorTime;var allocated=GC.GetTotalAllocatedBytes(false);var watch=Stopwatch.StartNew();
        for(var i=0;i<100;i++)await cache.GetAsync("image"+i);
        Check(cache.Health.Bytes<=1024&&cache.Health.Entries<=4);await cache.GetAsync("image99");Equal(100,server.Calls);await cache.GetAsync("image0");Equal(101,server.Calls);
        Console.WriteLine($"MEASURE image synthetic 100: {watch.ElapsedMilliseconds}ms wall / {(own.TotalProcessorTime-cpuStart).TotalMilliseconds:0}ms own CPU; allocated {GC.GetTotalAllocatedBytes(false)-allocated} bytes; cache {cache.Health.Bytes} bytes, {cache.Health.Entries} entries (not long-term GUI metrics)");
    }
    private static async Task Catalog()
    {
        using var f=new FifthStageTests.Fixture();f.Server.DynamicInventory=true;await f.Login();f.Vm.AppInput="620";f.Vm.ContextInput="6";f.Vm.NameInput="Custom game";f.Vm.AddScope.Execute(null);Equal(620,f.Vm.Game.AppId);Equal(0,f.Workspace!.Store.ReadCatalog("76561198000000002").Count);await f.Vm.LoadAsync();Equal(6UL,ulong.Parse(f.Vm.Assets.Single().Asset.Key.ContextId));f.Vm.Assets[0].Selected=true;f.Vm.Assets[0].Quantity=4;var added=f.Vm.Game;f.Vm.Game=f.Vm.Games[0];await f.Vm.LoadAsync();f.Vm.Game=added;await f.Vm.LoadAsync();Equal(4,f.Vm.Assets[0].Quantity);Check(f.Vm.Assets[0].Selected);
        f.Vm.Logout.Execute(null);await f.Login();Check(f.Vm.Games.Any(x=>x.AppId==620&&x.ContextId=="6"));var count=f.Vm.Games.Count;f.Vm.RemoveScope.Execute(null);Equal(count-1,f.Vm.Games.Count);
        using var g=new FifthStageTests.Fixture();g.Server.ListingAppId=999;g.Server.ListingContext="4";g.Server.Rows.Add(("222",1,100));g.Server.Publish=true;await g.Login();await g.Vm.LoadAsync();Check(g.Vm.Games.Any(x=>x.AppId==999&&x.ContextId=="4"));Equal(1,g.Server.InventoryReads);g.Vm.Logout.Execute(null);await g.Login();Check(g.Vm.Games.Any(x=>x.AppId==999&&x.ContextId=="4"));Equal(0,g.Server.Sends);
    }
    private static async Task CatalogStates()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();f.Server.UnsupportedInventory=true;await f.Vm.LoadAsync();Equal(InventoryScopeState.Unsupported,f.Vm.Game.State);Check(f.Vm.Connected);f.Server.UnsupportedInventory=false;f.Server.Free=0;await f.Vm.LoadAsync();Equal(InventoryScopeState.Empty,f.Vm.Game.State);f.Server.PrivateInventory=true;await f.Vm.LoadAsync();Equal(InventoryScopeState.PrivateOrDenied,f.Vm.Game.State);Equal(0,f.Server.Sends);
    }
    private static async Task CatalogInvalid()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();var count=f.Vm.Games.Count;foreach(var context in new[]{"0","02","../2","18446744073709551616"}){f.Vm.AppInput="123";f.Vm.ContextInput=context;f.Vm.NameInput="test";f.Vm.AddScope.Execute(null);Equal(count,f.Vm.Games.Count);Check(f.Vm.Connected);}Equal(0,f.Server.Sends);Check(await f.Workspace!.Store.LoadAsync(default)is null);
    }
    private static Task Indicators()
    {
        var now=DateTimeOffset.UtcNow;var rows=new[]{new MedianObservation(now.AddHours(-2),100,1),new MedianObservation(now.AddHours(-1),200,4),new MedianObservation(now.AddDays(-2),999,100)};var value=PriceIndicators.MedianEstimate(rows,"KRW",now,TimeSpan.FromDays(1))!;Equal(200L,value.Minor);Equal(5L,value.Volume);Equal(2,value.Buckets);Check(value.Fresh(now));Check(!value.Fresh(now.AddMinutes(6)));Equal(123L,PriceIndicators.MajorToMinor(1.23m,"USD"));Throws<ArgumentException>(()=>PriceIndicators.MajorToMinor(1.5m,"KRW"));Throws<ArgumentException>(()=>PriceIndicators.MedianEstimate([rows[0],rows[0]],"KRW",now,TimeSpan.FromDays(1)));Check(PriceIndicators.MedianEstimate([],"KRW",now,TimeSpan.FromDays(1))is null);return Task.CompletedTask;
    }
    private static async Task EstimateFlow()
    {
        using var f=new FifthStageTests.Fixture(medianContract:true);f.Server.Free=20;await f.Login();await f.Prepare(20);f.Vm.CloseReview.Execute(null);f.Vm.PricingMode=2;await f.Vm.RefreshPricesAsync();Check(f.Vm.Assets[0].Price!.Estimate is not null);f.Vm.BuildReview();Equal(1,f.Vm.ReviewRows.Count);Check(f.Vm.ReviewLiquidityText.Contains("부족 10개"));Check(f.Vm.ReviewRows[0].PriceBasis.Contains("정확한 평균"));f.EnableTrading();await f.Vm.SubmitAsync();Equal(1,f.Server.Sends);Equal(SaleState.AwaitingSteamGuard,f.Vm.Results.Single().State);Check((await f.Workspace!.Store.LoadAsync(default))!.Snapshot.Lines.Single().Evidence is not null);f.Vm.Logout.Execute(null);f.Server.Publish=true;await f.Login();await f.Vm.RecoverAsync();Equal(SaleState.Listed,f.Vm.Results.Single().State);Equal(1,f.Server.Sends);
    }
    private static async Task UnavailableAverage()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(2);f.Vm.ReviewRows[0].PriceText="116";f.Vm.PricingMode=1;Check(!f.Vm.ReviewOpen&&f.Vm.ReviewRows.Count==0);await f.Vm.RefreshPricesAsync();Check(!f.Vm.Review.CanExecute(null));Throws<SteamException>(()=>f.Vm.BuildReview());Equal(0,f.Server.Sends);
    }
    private static async Task ChangedHistory()
    {
        using var f=new FifthStageTests.Fixture(medianContract:true);await f.Login();await f.Prepare(1);f.Vm.CloseReview.Execute(null);f.Vm.PricingMode=2;await f.Vm.RefreshPricesAsync();f.Vm.BuildReview();f.EnableTrading();f.Server.HistoryOverride="{\"success\":true,\"prices\":[]}";await f.Vm.SubmitAsync();Equal(0,f.Server.Sends);Check(await f.Workspace!.Store.LoadAsync(default)is null);
    }
    private static async Task FeeAndReturn()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(10);f.EnableTrading();f.Server.FeeConflict=true;await f.Vm.SubmitAsync();Equal(1,f.Server.Sends);Check(f.Vm.Results.Skip(1).All(x=>x.State==SaleState.Ready));Equal(7,f.Workspace!.Store.Available(f.Vm.Assets.Single().Asset));await Fails<SteamException>(()=>f.Workspace.Executor.ReviseRepriceAsync("not-proven",116,"no-replacement"));Equal(1,f.Server.Sends);
        using var g=new FifthStageTests.Fixture();g.Server.Free=1;await g.Login();await g.Prepare(1);g.EnableTrading();await g.Vm.SubmitAsync();g.Server.Publish=true;var listing=(await g.Workspace!.Orders.RefreshAsync()).Single();await g.Workspace.Executor.CancelAsync(listing,"single-merged");g.Server.Free=2;var merged=await g.Workspace.Executor.ReconcileCancellationAsync("single-merged");Equal(SaleState.Unknown,merged.State);Check(g.Workspace.Store.Return("single-merged")is null);Equal(1,g.Server.Sends);Equal(1,g.Server.Cancels);
    }
    private static async Task Logs()
    {
        var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","s7-log-"+Guid.NewGuid().ToString("N"));using(var log=new JsonDiagnosticLog(folder))using(DiagnosticHub.Use(log))using(var op=DiagnosticHub.Operation(DiagnosticEvent.UiCommand,DiagnosticArea.UI,"B04"))
        {using var server=new ImageContractServer();using var cache=new SteamImageCache(server);await cache.GetAsync("SECRET_IMAGE_ID");await log.FlushAsync();Check(log.Recent.Where(x=>x.EventId==(int)DiagnosticEvent.ImageLoad).All(x=>x.Context.OperationId==DiagnosticHub.Context!.OperationId));Check(!JsonSerializer.Serialize(log.Recent,JsonDiagnosticLog.Json).Contains("SECRET_IMAGE_ID"));}
        Directory.Delete(folder,true);
    }
    private static async Task JournalCompatibility()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.MissingId=true;await f.Vm.SubmitAsync();var original=await f.Workspace!.Store.LoadAsync(default);f.Workspace.Store.SaveCatalog(f.Workspace.Authentication.SteamId!,[new(620,"6","Custom")]);f.Vm.Logout.Execute(null);await f.Login();Equal(JsonSerializer.Serialize(original!.Snapshot),JsonSerializer.Serialize((await f.Workspace!.Store.LoadAsync(default))!.Snapshot));Check(f.Vm.Games.Single().ContextId=="6");Equal(1,f.Server.Sends);
    }
    private static async Task MedianContractGate()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(1);f.Vm.CloseReview.Execute(null);f.Vm.PricingMode=2;await f.Vm.RefreshPricesAsync();f.Vm.BuildReview();f.EnableTrading();Check(f.Vm.CanTrade&&!f.Vm.CanTradeReview&&!f.Vm.Submit.CanExecute(null));await f.Vm.SubmitAsync();Equal(0,f.Server.Sends);Check(await f.Workspace!.Store.LoadAsync(default)is null);
    }
    private static async Task LateHistory()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(1);f.Vm.CloseReview.Execute(null);f.Vm.PricingMode=2;f.Server.HistoryStarted=new(TaskCreationOptions.RunContinuationsAsynchronously);f.Server.HistoryRelease=new(TaskCreationOptions.RunContinuationsAsynchronously);var loading=f.Vm.RefreshPricesAsync();await f.Server.HistoryStarted.Task;f.Vm.PricingMode=0;f.Server.HistoryRelease.SetResult();await loading;Check(f.Vm.Assets.Single().Price is null&&!f.Vm.Review.CanExecute(null));Equal(0,f.Server.Sends);
        f.Server.InventoryStarted=new(TaskCreationOptions.RunContinuationsAsynchronously);f.Server.InventoryRelease=new(TaskCreationOptions.RunContinuationsAsynchronously);f.Vm.ImagesEnabled=true;var pending=f.Vm.LoadAsync();await f.Server.InventoryStarted.Task;f.Vm.Logout.Execute(null);f.Server.InventoryRelease.SetResult();await pending;Equal(InventoryScopeState.NotQueried,f.Vm.Game.State);Check(!f.Vm.ImagesEnabled&&f.Vm.Assets.Count==0);
    }
    private static async Task StricterContracts()
    {
        using(var f=new FifthStageTests.Fixture(medianContract:true)){await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.HidePublisherFee=true;await f.Vm.SubmitAsync();Equal(0,f.Server.Sends);Check(await f.Workspace!.Store.LoadAsync(default)is null);}
        using(var f=new FifthStageTests.Fixture(medianContract:true))
        {
            await f.Login();await f.Prepare(1);f.Vm.CloseReview.Execute(null);f.Vm.PricingMode=2;f.Server.HistoryOverride="{\"success\":true,\"prices\":[[\"Oct 28 2026 01: +0\",115,\"2\"],[\"Oct 28 2026 02: +0\",115,\"1\"]]}";await f.Vm.RefreshPricesAsync();f.Vm.BuildReview();f.EnableTrading();f.Server.HistoryOverride=f.Server.HistoryOverride.Replace("02:","03:");await f.Vm.SubmitAsync();Equal(0,f.Server.Sends);Check(await f.Workspace!.Store.LoadAsync(default)is null);
        }
    }
    private static async Task Until(Func<bool> condition){using var timeout=new CancellationTokenSource(5000);while(!condition())await Task.Delay(10,timeout.Token);}
    private static void Check(bool condition){if(!condition)throw new Exception("Assertion failed");}
    private static void Equal<T>(T a,T b){if(!EqualityComparer<T>.Default.Equals(a,b))throw new Exception($"Expected {a}, got {b}");}
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
    private static async Task Fails<T>(Func<Task> action)where T:Exception{try{await action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
}
