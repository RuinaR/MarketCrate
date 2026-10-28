using System.Net;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.Tests;
public static class ThirteenthStageTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{("Self-check never starts without approval and scope changes revoke approval",Consent),("Self-check listings-only is one GET and leaves full data caches unchanged",Scope),("Self-check all reads are sequential bounded and never trade",All),("Self-check inventory probe does not auto-page or replace selections",Page),("Self-check403429401 stops all remaining reads without retry",Restrictions),("Self-check parse and503 failures remain independent",Independent),("Wallet marker absence is schema failure not inferred logout",WalletFailure),("Missing item and empty data remain unverified without synthetic fallback",Missing),("Self-check cancellation and logout discard stale results",Cancel),("Self-check causal diagnostics exclude secrets and raw bodies",Diagnostics),("QR session requires nonempty secure Community cookie",Cookie),("Self-check currency and publisher fee conflicts fail with specific causes",Mismatch)};
        int passed=0,failed=0;foreach(var(name,run)in tests)try{await run();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e);}return(passed,failed);
    }
    private static async Task Consent()
    {
        using var f=new Fixture();await f.Login();await f.Vm.SelfCheckAsync();Check(f.Reads.Count==0&&!f.Vm.SelfCheck.CanExecute(null),"unapproved check sent requests");f.Vm.CheckListings=true;f.Vm.CheckApproved=true;Check(f.Vm.SelfCheck.CanExecute(null),"approved read unavailable");f.Vm.CheckInventory=true;Check(!f.Vm.CheckApproved,"scope change retained approval");f.Vm.CheckApproved=true;f.Vm.Game=f.Vm.Games[1];Check(!f.Vm.CheckApproved,"game change retained approval");
    }
    private static async Task Scope()
    {
        using var f=new Fixture();await f.Login();f.Server.Rows.Add(("900",1,100));f.Server.Publish=true;f.Vm.CheckListings=true;f.Vm.CheckApproved=true;await f.Vm.SelfCheckAsync();
        Check(f.Reads.Count==1&&f.Reads.All(x=>x.Method=="GET"&&x.Path=="/market/mylistings/render/"),"scope expanded");Check(f.Vm.CheckRows.Single(x=>x.Step=="Listings").Result?.Outcome==ReadCheckOutcome.Succeeded&&f.Vm.Listings.Count==0,"probe replaced full listing cache");Check(!f.Vm.CheckApproved&&!f.Vm.CanTrade,"approval not consumed or trade enabled");Check(f.Vm.CheckRows.Where(x=>x.Step is not("SessionCheck" or "Listings")).All(x=>x.Result?.Outcome==ReadCheckOutcome.Unverified),"excluded scope claimed verified");
    }
    private static async Task All()
    {
        using var f=new Fixture();await f.Prepare();f.All();await f.Vm.SelfCheckAsync();Check(f.Vm.CheckRows.All(x=>x.Result?.Outcome==ReadCheckOutcome.Succeeded),"all synthetic contracts did not pass");Check(f.Reads.Count==6&&f.Images.Calls==1&&!f.Images.CredentialLeak,"wrong request count or image credentials");Check(f.Reads.All(x=>x.Method=="GET")&&f.Server.Sends==0&&f.Server.Cancels==0,"write performed");Check(f.Vm.CheckContext.Contains("Synthetic")&&f.Vm.ProductReadiness.Contains("READ_ONLY_READY /"),"synthetic check promoted live readiness");
    }
    private static async Task Page()
    {
        using var f=new Fixture();await f.Prepare();var row=f.Vm.Assets[0];f.Route=p=>p.StartsWith("/inventory/")?Json(f.Server.InventoryJson(1).Replace("\"success\":1","\"success\":1,\"more_items\":1,\"last_assetid\":\"200\"",StringComparison.Ordinal)):null;f.Vm.CheckInventory=true;f.Vm.CheckApproved=true;await f.Vm.SelfCheckAsync();Check(f.Reads.Count==1&&f.Vm.CheckRows.Single(x=>x.Step=="Inventory").Detail.Contains("추가 페이지 있음"),"probe followed pagination");Check(ReferenceEquals(row,f.Vm.Assets[0])&&row.Selected,"probe replaced selected data");
    }
    private static async Task Restrictions()
    {
        foreach(var status in new[]{401,403,429})
        {
            using var f=new Fixture();await f.Prepare();f.Route=p=>p.Contains("mylistings")?new((HttpStatusCode)status){Content=new StringContent("{}")}:null;f.All();await f.Vm.SelfCheckAsync();Check(f.Reads.Count==1&&f.Images.Calls==0,"restricted check continued");Check(f.Vm.CheckRows.Where(x=>x.Step!="SessionCheck").All(x=>x.Result?.Outcome==ReadCheckOutcome.Blocked),"remaining steps not blocked");Check(status==401?!f.Vm.Connected&&f.Vm.LoginState==SteamLoginState.Expired:f.Vm.Connected&&f.Vm.RequestsBlocked,"request/session states conflated");
        }
    }
    private static async Task Independent()
    {
        foreach(var status in new[]{200,503})
        {
            using var f=new Fixture();await f.Prepare();f.Route=p=>p.Contains("mylistings")?new((HttpStatusCode)status){Content=new StringContent("{private-body-never-log")}:null;f.All();await f.Vm.SelfCheckAsync();Check(f.Vm.Connected&&!f.Vm.RequestsBlocked&&f.Reads.Count==6,"independent read failed or retried");Check(f.Vm.CheckRows.Single(x=>x.Step=="Listings").Result?.Outcome==ReadCheckOutcome.Failed&&f.Vm.CheckRows.Single(x=>x.Step=="Inventory").Result?.Outcome==ReadCheckOutcome.Succeeded,"failure changed other result");
        }
    }
    private static async Task WalletFailure()
    {
        using var f=new Fixture();await f.Prepare();f.Route=p=>p=="/market/"?Json("<html>changed-wallet-schema</html>"):null;f.All();await f.Vm.SelfCheckAsync();Check(f.Vm.Connected&&f.Vm.CheckRows.Single(x=>x.Step=="Wallet").Result?.Error==DiagnosticError.Schema,"missing HTML variable logged out");Check(f.Vm.CheckRows.Single(x=>x.Step=="Fees").Result?.Outcome==ReadCheckOutcome.Unverified&&!f.Reads.Any(x=>x.Path.Contains("ajaxfeestructure")),"dependent fee request invented wallet");
    }
    private static async Task Missing()
    {
        using var f=new Fixture();await f.Login();f.Vm.CheckInventory=true;f.Vm.CheckOrders=true;f.Vm.CheckHistory=true;f.Vm.CheckImage=true;f.Server.Free=0;f.Vm.CheckApproved=true;await f.Vm.SelfCheckAsync();Check(f.Reads.Count==1&&f.Images.Calls==0,"missing item caused requests");Check(f.Vm.CheckRows.Where(x=>x.Step is "Orderbook" or "History" or "Image").All(x=>x.Result?.Outcome==ReadCheckOutcome.Unverified),"missing source fabricated result");Check(f.Vm.CheckRows.Single(x=>x.Step=="Inventory").Result?.Count==0,"empty inventory fabricated count");
    }
    private static async Task Cancel()
    {
        foreach(var logout in new[]{false,true})
        {
            using var f=new Fixture();await f.Login();f.Server.InventoryStarted=new(TaskCreationOptions.RunContinuationsAsynchronously);f.Server.InventoryRelease=new(TaskCreationOptions.RunContinuationsAsynchronously);f.Vm.CheckInventory=true;f.Vm.CheckWallet=true;f.Vm.CheckApproved=true;var run=f.Vm.SelfCheckAsync();await f.Server.InventoryStarted.Task;if(logout)f.Vm.Logout.Execute(null);else f.Vm.CancelOperation.Execute(null);f.Server.InventoryRelease.SetResult();await run;Check(f.Reads.Count==1,"cancel continued requests");if(logout)Check(f.Vm.CheckRows.All(x=>x.Result is null)&&!f.Vm.Connected,"late results survived logout");else Check(f.Vm.Connected&&f.Vm.CheckRows.Single(x=>x.Step=="Wallet").Result?.Outcome==ReadCheckOutcome.Cancelled,"cancellation erased login");
        }
    }
    private static async Task Diagnostics()
    {
        var path=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage13-diag-"+Guid.NewGuid().ToString("N"));try
        {
            using var log=new JsonDiagnosticLog(path);using var scope=DiagnosticHub.Use(log);using var f=new Fixture();await f.Prepare();f.Route=p=>p.Contains("mylistings")?Json("{private-body-never-log"):null;f.All();await f.Vm.SelfCheckAsync();var id=f.Vm.LastOperationId;Check(f.Vm.CheckRows.All(x=>x.Result?.OperationId==id),"row operation missing");Check(log.Recent.Any(x=>x.EventId==(int)DiagnosticEvent.ReadSelfCheck&&x.Endpoint=="Listings"&&x.Error==DiagnosticError.Schema&&x.Context.OperationId==id),"failure not causal");Check(log.Recent.Any(x=>x.EventId==(int)DiagnosticEvent.HttpResponse&&x.Context.OperationId==id&&x.Context.CorrelationId is not null),"request correlation missing");await log.FlushAsync();var text=System.Text.Json.JsonSerializer.Serialize(log.Recent);foreach(var secret in new[]{"private-body-never-log","synthetic-cookie","synthetic-refresh","Synthetic stack","76561198000000001"})Check(!text.Contains(secret),"diagnostic leaked private data");
        }finally{Directory.Delete(path,true);}
    }
    private static async Task Cookie()
    {
        foreach(var cookie in new string?[]{null,"steamLoginSecure=; Path=/; Secure","steamLoginSecure=synthetic; Path=/"})
        {
            using var f=new Fixture();f.Route=p=>{if(p!="/login/settoken")return null;var response=Json("{\"result\":1}");if(cookie is not null)response.Headers.Add("Set-Cookie",cookie);return response;};await f.Vm.BeginAsync();f.Server.Clock.Advance();await f.Vm.CheckAsync();Check(!f.Vm.Connected&&!f.Workspace!.Authentication.HasSession,"invalid Community cookie accepted");
        }
    }
    private static async Task Mismatch()
    {
        foreach(var currency in new[]{true,false})
        {
            using var f=new Fixture();await f.Prepare();f.Route=p=>currency&&p=="/market/orderbook"?Json("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":1,\"rgCompactBuyOrders\":[115,3]}}}"):!currency&&p.Contains("ajaxfeestructure")?Json("{\"steam_fee\":{\"fee_rate\":\"0.05\",\"fee_cap\":\"0\"},\"publisher_fee\":{\"fee_rate\":\"0.2\",\"fee_cap\":\"0\"}}"):null;
            f.All();await f.Vm.SelfCheckAsync();var result=f.Vm.CheckRows.Single(x=>x.Step==(currency?"Orderbook":"Fees")).Result;Check(result?.Outcome==ReadCheckOutcome.Failed&&result.Error==(currency?DiagnosticError.Currency:DiagnosticError.Validation),"mismatch claimed success");Check(f.Vm.Connected&&f.Server.Sends==0&&f.Server.Cancels==0,"mismatch triggered auth or trade");
        }
    }
    internal sealed class Fixture:IDisposable
    {
        private readonly string folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage13-"+Guid.NewGuid().ToString("N"));public SteamContractServer Server{get;}=new(){IconToken="synthetic_image"};public ImageContractServer Images{get;}=new();public SteamExperimentalWorkspace? Workspace;
        public SteamWorkspaceViewModel Vm{get;}public Func<string,HttpResponseMessage?>? Route;public List<(string Method,string Path)> Reads{get;}=[];
        public Fixture(){Vm=new(scope=>Workspace=new(folder,scope.AppId,scope.ContextId,scope.Name,contractHandler:new Routing(this),clock:Server.Clock,imageHandler:Images),true){RiskAccepted=true,CredentialsAccepted=true};}
        public async Task Login(){await Vm.BeginAsync();Server.Clock.Advance();await Vm.CheckAsync();Check(Vm.Connected,"login failed");Reads.Clear();}
        public async Task Prepare(){await Login();await Vm.LoadAsync();Vm.Assets[0].Selected=true;Reads.Clear();}
        public void All(){Vm.CheckWallet=true;Vm.CheckListings=Vm.CheckInventory=Vm.CheckFees=Vm.CheckOrders=Vm.CheckHistory=Vm.CheckImage=true;Vm.CheckApproved=true;}
        public void Dispose(){Vm.Dispose();Server.Dispose();Directory.Delete(folder,true);}
        private sealed class Routing(Fixture f):HttpMessageHandler
        {private readonly HttpMessageInvoker inner=new(f.Server.Connection());protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){f.Reads.Add((request.Method.Method,request.RequestUri!.AbsolutePath));return f.Route?.Invoke(request.RequestUri.AbsolutePath)is {} response?Task.FromResult(response):inner.SendAsync(request,token);}protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}}
    }
    private static HttpResponseMessage Json(string body)=>new(HttpStatusCode.OK){Content=new StringContent(body)};
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
}
