using System.Net;
using System.Text.Json;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.Tests;
public static class ListingAssetMapTests
{
    private static string Page(string assets,string listings="[]",int total=0)=>"{\"success\":true,\"total_count\":"+total+",\"assets\":"+assets+",\"listings\":"+listings+",\"listings_to_confirm\":[],\"listings_on_hold\":[]}";
    private const string Row="[{\"listingid\":\"900\",\"currencyid\":2016,\"price\":10000,\"fee\":1500,\"asset\":{\"appid\":730,\"contextid\":\"2\",\"id\":\"101\",\"amount\":\"1\"}}]";
    private static void Check(bool ok){if(!ok)throw new Exception("Listing asset map contract");}
    private static Task Sync(Action action){action();return Task.CompletedTask;}
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{
            ("Explicit empty assets array is a valid empty market map",()=>Sync(()=>{var page=SteamContract.Listings(Page("[]"));Check(page.TotalActive==0&&page.Listings.IsEmpty&&page.Presence.IsEmpty);})),
            ("Nested empty app and context maps support empty arrays",()=>Sync(()=>{foreach(var map in new[]{"{\"730\":[]}","{\"730\":{\"2\":[]}}"})Check(SteamContract.Listings(Page(map)).Presence.IsEmpty);})),
            ("Nonempty arrays null scalar and malformed nested maps remain rejected",Invalid),
            ("Missing metadata cannot erase canonical active membership or invent money",()=>Sync(()=>{var page=SteamContract.Listings(Page("[]",Row,1));Check(page.Presence.Single().State==SaleState.Listed&&page.Presence.Single().Asset.AssetId=="101"&&page.Listings.IsEmpty);})),
            ("Empty asset map cannot make a contradictory active total complete",Contradiction),
            ("User trading validation succeeds with zero listings and array-shaped empty assets",Activation),
            ("Empty map preserves ID-less pending membership without monetary quote",()=>Sync(()=>{var body=Page("[]").Replace("\"listings_to_confirm\":[]","\"listings_to_confirm\":[{\"asset\":{\"appid\":730,\"contextid\":\"2\",\"id\":\"101\"}}]");var page=SteamContract.Listings(body);Check(page.Presence.Single().State==SaleState.AwaitingSteamGuard&&page.Listings.IsEmpty);})),
            ("Asset-map shape diagnostics keep operation identity without payload",Diagnostics)
        };
        int passed=0,failed=0;foreach(var(name,test)in tests)try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}return(passed,failed);
    }
    private static Task Invalid()=>Sync(()=>{foreach(var map in new[]{"null","false","0","\"private-body\"","[{}]","{\"730\":null}","{\"730\":[{}]}","{\"730\":{\"2\":false}}","{\"730\":{\"2\":[{}]}}"}){try{SteamContract.Listings(Page(map));throw new Exception("Invalid map accepted");}catch(SteamException error){Check(error.Code==SteamFailure.ContractChanged);}}});
    private static async Task Contradiction()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Route=p=>p.Contains("mylistings")?new(HttpStatusCode.OK){Content=new StringContent(Page("[]",total:1))}:null;
        try{await f.Workspace!.Orders.RefreshAsync();throw new Exception("Incomplete total accepted");}catch(SteamException error){Check(error.Code==SteamFailure.ContractChanged&&f.Server.Sends==0);}
    }
    private static async Task Activation()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Server.Free=1;f.Server.ManyAssets=2;f.Route=p=>p=="/market/"?new(HttpStatusCode.OK){Content=new StringContent(FeeModeTests.Page(flag:"true"))}:p.Contains("mylistings")?new(HttpStatusCode.OK){Content=new StringContent(Page("[]"))}:null;
        await f.Workspace!.EnableUserTradingAsync(730,"2","CS2",true,true,default);
        Check(f.Workspace.Executor.CanExecute&&f.Workspace.Orders.Presence.IsEmpty&&f.Server.Sends==0&&f.Server.Cancels==0);
        await f.Vm.LoadListingsAsync();Check(f.Vm.Connected&&f.Vm.Listings.Count==0);
    }
    private static Task Diagnostics()=>Sync(()=>{var log=new Capture();using var use=DiagnosticHub.Use(log);using var scope=DiagnosticHub.Scope();var id=DiagnosticHub.Context!.OperationId;SteamContract.Listings(Page("[]"));try{SteamContract.Listings(Page("\"private-body\""));}catch(SteamException){}Check(log.Events.Any(x=>x.Endpoint=="Listings"&&x.Reason=="AssetsMapEmptyArray"&&x.Context.OperationId==id)&&log.Events.Any(x=>x.Reason=="AssetsMapInvalidShape"&&x.Error==DiagnosticError.Schema));var text=JsonSerializer.Serialize(log.Events);Check(!text.Contains("private-body")&&!text.Contains("76561198000000001"));});
    private sealed class Capture:IDiagnosticLog{public Guid SessionId{get;}=Guid.NewGuid();public List<DiagnosticRecord> Events{get;}=[];public void Write(DiagnosticRecord record)=>Events.Add(record);}
}
