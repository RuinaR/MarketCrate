using System.Net;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
using MarketCrate.Infrastructure.Diagnostics;
namespace MarketCrate.Tests;
public static class FourteenthStageTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{("Reported eight listings zero inventory and wallet401 is reproduced without later requests",Observed401),("Wallet login redirect is endpoint authorization not loss of whole session",LoginRedirect),("Non-login redirect is not guessed authentication and is never followed",OtherRedirect),("Read API denial preserves session and unknown error stays safely blocked",ReadApiResult),("Wallet observation separates currency optional pricing null and missing fields",WalletShapes),("Empty inventory requires explicit noncontradictory proof",EmptyInventory),("Marketable zero is distinct from zero owned assets and wrong context fails",Scopes),("Sanitized wallet inventory and error-class facts trace observed failure",Facts)};
        int passed=0,failed=0;foreach(var(name,run)in tests)try{await run();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e);}return(passed,failed);
    }
    private static async Task Observed401()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Prepare();for(int i=0;i<8;i++)f.Server.Rows.Add(((900+i).ToString(),1,100));f.Server.Publish=true;f.Server.Free=0;f.Route=p=>p=="/market/"?new(HttpStatusCode.Unauthorized){Content=new StringContent("{}")} :null;f.All();await f.Vm.SelfCheckAsync();
        Check(Row(f,"Listings").Count==8&&Row(f,"Listings").Detail.Contains("총계 8"),"eight listings lost");Check(Row(f,"Inventory").Count==0&&Row(f,"Inventory").Classification==ReadCheckClassification.Empty&&Row(f,"Marketable").Count==0,"zero inventory not distinguished");Check(Row(f,"Wallet").Classification==ReadCheckClassification.Authentication&&!f.Vm.Connected,"401 not expired");Check(f.Vm.CheckRows.Where(x=>x.Step is "Fees" or "Orderbook" or "History" or "Image").All(x=>x.Result?.Outcome==ReadCheckOutcome.Blocked)&&f.Reads.Count==3&&f.Images.Calls==0,"requests after restriction");
    }
    private static async Task LoginRedirect()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Prepare();f.Route=p=>p=="/market/"?Redirect("https://steamcommunity.com/login/home/?goto=private-never-log"):null;f.All();await f.Vm.SelfCheckAsync();
        Check(Row(f,"Wallet").Classification==ReadCheckClassification.Authorization&&f.Vm.Connected&&f.Vm.SessionPresent&&!f.Vm.RequestsBlocked,"endpoint auth confused with session");Check(!f.Vm.LoadFees.CanExecute(null)&&f.Vm.LoadListings.CanExecute(null)&&f.Reads.Count==3,"feature gate incorrect");f.Reads.Clear();await f.Vm.LoadFeesAsync();Check(f.Reads.Count==0,"blocked wallet resent");await f.Vm.LoadListingsAsync();Check(f.Reads.Count==1&&f.Vm.Connected,"independent listings blocked");
    }
    private static async Task ReadApiResult()
    {
        foreach(var code in new[]{15,8})
        {
            using var f=new ThirteenthStageTests.Fixture();await f.Prepare();f.Route=p=>{if(p!="/market/")return null;var response=Json("{}");response.Headers.Add("x-eresult",code.ToString());return response;};f.All();await f.Vm.SelfCheckAsync();Check(f.Vm.Connected&&f.Reads.Count==3,"read API rejection erased session or continued");Check(Row(f,"Wallet").Classification==(code==15?ReadCheckClassification.Authorization:ReadCheckClassification.AccessRestricted),"read API code guessed authentication");
        }
    }
    private static async Task OtherRedirect()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Prepare();f.Route=p=>p=="/market/"?Redirect("https://steamcommunity.com/market/new/?private=never-log"):null;f.All();await f.Vm.SelfCheckAsync();Check(Row(f,"Wallet").Outcome==ReadCheckOutcome.Unsupported&&f.Vm.Connected&&!f.Vm.RequestsBlocked,"official non-login redirect guessed auth");Check(Row(f,"Orderbook").Outcome==ReadCheckOutcome.Succeeded&&!f.Reads.Any(x=>x.Path=="/market/new/"),"followed redirect or independent read blocked");
    }
    private static async Task WalletShapes()
    {
        var currency=SteamFees.ObserveWallet("<script>var g_bLoggedIn = true; var g_rgWalletInfo = {\"wallet_currency\":16};</script>");Check(currency.Currency=="KRW"&&!currency.PricingMetadataAvailable&&currency.LoggedIn==true,"currency only rejected");Throws(()=>SteamFees.Wallet("var g_rgWalletInfo={\"wallet_currency\":16};"));
        Check(SteamFees.ObserveWallet("var g_rgWalletInfo = null;").Currency is null&&SteamFees.ObserveWallet("<html>no wallet</html>").Currency is null,"missing wallet invented");
        using var f=new ThirteenthStageTests.Fixture();await f.Prepare();f.Route=p=>p=="/market/"?Json("var g_bLoggedIn = true; var g_rgWalletInfo={\"wallet_currency\":16};"):null;f.All();await f.Vm.SelfCheckAsync();Check(Row(f,"Wallet").Currency=="KRW"&&Row(f,"Wallet").Outcome==ReadCheckOutcome.Succeeded&&Row(f,"Fees").Outcome==ReadCheckOutcome.Unverified,"pricing defaults invented or fee observation blocked");Check(Row(f,"Orderbook").Outcome==ReadCheckOutcome.Succeeded&&f.Vm.Connected,"currency-only blocked other reads");
        var wallet=new SteamWallet("KRW",100,100);var extra="{\"steam_fee\":{\"fee_rate\":\"0.05\"},\"publisher_fee\":{\"fee_rate\":\"0.1\"},\"new_field\":true}";Check(SteamFees.ObserveStructure(extra).AdditionalFields,"new fee field hidden");Throws(()=>SteamFees.Structure(extra,wallet));
    }
    private static async Task EmptyInventory()
    {
        foreach(var json in new[]{"{\"success\":1,\"total_inventory_count\":0}","{\"success\":1,\"total_inventory_count\":0,\"assets\":[],\"descriptions\":[]}"})Check(SteamContract.Inventory(json,730,"2","CS2").VerifiedEmpty,"normal empty rejected");
        foreach(var json in new[]{"{\"success\":1,\"assets\":[],\"descriptions\":[]}","{\"success\":1,\"total_inventory_count\":3,\"assets\":[],\"descriptions\":[]}","{\"success\":1,\"total_inventory_count\":0,\"more_items\":1,\"last_assetid\":\"1\"}","{\"success\":1,\"total_inventory_count\":0,\"Error\":\"private-never-log\"}","{\"success\":0,\"total_inventory_count\":0}"})Throws(()=>SteamContract.Inventory(json,730,"2","CS2"));
        await Task.CompletedTask;
    }
    private static async Task Scopes()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Prepare();f.Route=p=>p.StartsWith("/inventory/")?Json(f.Server.InventoryJson(1).Replace("\"marketable\":1","\"marketable\":0",StringComparison.Ordinal)):null;f.Vm.CheckInventory=true;f.Vm.CheckApproved=true;await f.Vm.SelfCheckAsync();Check(Row(f,"Inventory").Count==1&&Row(f,"Marketable").Count==0,"marketable filter erased owned assets");Throws(()=>SteamContract.Inventory(f.Server.InventoryJson(1),570,"2","Dota"));Throws(()=>SteamContract.Inventory(f.Server.InventoryJson(1),730,"3","CS2"));
    }
    private static async Task Facts()
    {
        var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage14-diag-"+Guid.NewGuid().ToString("N"));try
        {
            using var log=new JsonDiagnosticLog(folder);using var scope=DiagnosticHub.Use(log);using var f=new ThirteenthStageTests.Fixture();await f.Prepare();f.Server.Free=0;f.Route=p=>p=="/market/"?Json("var g_bLoggedIn = true; var g_rgWalletInfo=null; /* private-canary-never-log */"):null;f.All();await f.Vm.SelfCheckAsync();var id=f.Vm.LastOperationId;
            Check(log.Recent.Any(x=>x.Context.OperationId==id&&x.EventId==(int)DiagnosticEvent.WalletContract&&x.Reason=="WalletNull")&&log.Recent.Any(x=>x.Context.OperationId==id&&x.EventId==(int)DiagnosticEvent.InventoryContract&&x.Reason=="InventoryEmptyVerified"),"contract facts missing");Check(Row(f,"Wallet").Outcome==ReadCheckOutcome.Unverified&&f.Vm.Connected,"null wallet logged out");await log.FlushAsync();var text=System.Text.Json.JsonSerializer.Serialize(log.Recent);foreach(var secret in new[]{"private-canary-never-log","synthetic-cookie","76561198000000001"})Check(!text.Contains(secret),"private data leaked");
        }finally{Directory.Delete(folder,true);}
    }
    private static SteamReadCheckResult Row(ThirteenthStageTests.Fixture f,string step)=>f.Vm.CheckRows.Single(x=>x.Step==step).Result!;
    private static HttpResponseMessage Json(string text)=>new(HttpStatusCode.OK){Content=new StringContent(text)};
    private static HttpResponseMessage Redirect(string uri){var response=new HttpResponseMessage(HttpStatusCode.Found){Content=new StringContent("")};response.Headers.Location=new(uri);return response;}
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Throws(Action run){try{run();}catch(SteamException){return;}throw new Exception("unproven data accepted");}
}
