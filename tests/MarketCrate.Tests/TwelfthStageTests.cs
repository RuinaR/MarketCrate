using System.Net;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure.Steam;

namespace MarketCrate.Tests;
public static class TwelfthStageTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{("Inventory parse failure preserves authenticated session and independent listings",()=>ReadFailure("/inventory/",200,"{}")),("Fee parse failure preserves session and inventory",()=>ReadFailure("/market/ajaxfeestructure/",200,"{}")),("Listing parse failure preserves session and independent fees",()=>ReadFailure("/market/mylistings/render/",200,"{}")),("Inventory404 is scoped and retry is explicit",()=>ReadFailure("/inventory/",404,"{}")),("Read transport failure does not resend or erase login",()=>ReadFailure("/inventory/",503,"{}")),("401 expires session while403429 retain session and latch request gate",Restrictions),("Nested fee failure preserves inventory and continues independent listings",PartialLoad),("Read transitions retain operation correlation and sanitized cause",Diagnostics),("Read timeout and transport require manual retry without session loss",ReadNetwork),("Price history failure is independent from login and orderbook state",HistoryFailure)};
        int passed=0,failed=0;foreach(var(name,run)in tests)try{await run();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e);}return(passed,failed);
    }
    private static async Task ReadFailure(string path,int code,string body)
    {
        using var f=new Fixture();await f.Login();f.Route=(p)=>p.StartsWith(path,StringComparison.Ordinal)?new((HttpStatusCode)code){Content=new StringContent(body)}:null;
        if(path.StartsWith("/inventory"))await f.Vm.LoadAsync();else if(path.Contains("fee"))await f.Vm.LoadFeesAsync();else await f.Vm.LoadListingsAsync();
        Check(f.Vm.Connected&&f.Vm.LoginState==SteamLoginState.Connected,"query failure erased authentication");Check(f.Vm.LoadListings.CanExecute(null)&&f.Vm.LoadFees.CanExecute(null),"independent reads blocked");
        f.Route=null;if(path.Contains("fee"))await f.Vm.LoadAsync();else await f.Vm.LoadFeesAsync();Check(f.Vm.Connected,"independent read failed");Check(f.Server.Sends==0&&f.Server.Cancels==0,"unexpected write");
    }
    private static async Task Restrictions()
    {
        foreach(var status in new[]{401,403,429})
        {
            using var f=new Fixture();await f.Login();var calls=0;f.Route=p=>{if(!p.StartsWith("/inventory/"))return null;calls++;return new((HttpStatusCode)status){Content=new StringContent("{}")};};
            await f.Vm.LoadAsync();Check(f.Vm.RequestsBlocked&&!f.Vm.Load.CanExecute(null)&&!f.Vm.LoadListings.CanExecute(null),"server limit not respected");
            if(status==401)Check(!f.Vm.Connected&&f.Vm.LoginState==SteamLoginState.Expired&&f.Vm.BeginLogin.CanExecute(null),"expiry does not require login");
            else Check(f.Vm.Connected&&f.Vm.SessionPresent&&f.Vm.LoginState==SteamLoginState.Connected&&!f.Vm.BeginLogin.CanExecute(null),"request latch confused with authentication");
            await f.Vm.LoadAsync();Check(calls==1&&f.Server.Sends==0,"restricted request resent");
        }
    }
    private static async Task PartialLoad()
    {
        using var f=new Fixture();await f.Login();f.Server.Rows.Add(("900",1,100));f.Server.Publish=true;
        f.Route=p=>p.Contains("ajaxfeestructure")?new(HttpStatusCode.OK){Content=new StringContent("{}")}:null;
        await f.Vm.LoadAsync();Check(f.Vm.Assets.Count==1&&f.Vm.Listings.Count==1,"independent load did not continue");Check(f.Vm.InventoryRead.State==SteamReadState.Ready&&f.Vm.FeesRead.State==SteamReadState.Failed&&f.Vm.ListingsRead.State==SteamReadState.Ready,"wrong per-read states");
        Check(f.Vm.ConnectionSummary.Contains("인증 완료")&&f.Vm.ConnectionSummary.Contains("일부 조회 실패"),"unclear authentication status");
        f.Vm.Assets[0].Selected=true;f.Vm.Assets[0].Quantity=3;var first=f.Vm.Game;f.Vm.Game=f.Vm.Games[1];f.Vm.Game=first;Check(f.Vm.Assets[0].Selected&&f.Vm.Assets[0].Quantity==3,"game switch lost selection");
        f.Route=null;await f.Vm.LoadFeesAsync();Check(f.Vm.FeesRead.State==SteamReadState.Ready&&f.Vm.Connected,"manual recovery failed");
    }
    private static async Task Diagnostics()
    {
        var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage12-log-"+Guid.NewGuid().ToString("N"));
        try
        {
            using var log=new MarketCrate.Infrastructure.Diagnostics.JsonDiagnosticLog(folder);using var scope=MarketCrate.Core.DiagnosticHub.Use(log);using var f=new Fixture();await f.Login();f.Route=p=>p.StartsWith("/inventory/")?new(HttpStatusCode.OK){Content=new StringContent("{private-auth-sentinel")}:null;
            await f.Vm.LoadAsync();var events=log.Recent.Where(x=>x.Context.OperationId==f.Vm.LastOperationId).ToArray();
            Check(events.Any(x=>x.EventId==(int)MarketCrate.Core.DiagnosticEvent.ReadState&&x.Endpoint=="Inventory"&&x.NewState=="Failed"),"read failure not correlated");Check(events.Any(x=>x.Error==MarketCrate.Core.DiagnosticError.Parsing),"parser phase missing");Check(events.Any(x=>x.EventId==(int)MarketCrate.Core.DiagnosticEvent.HttpResponse&&x.Context.CorrelationId is not null),"HTTP correlation missing");
            Check(!events.Any(x=>x.EventId==(int)MarketCrate.Core.DiagnosticEvent.AuthState&&x.NewState=="ContractFailure"),"read recorded as auth failure");await log.FlushAsync();Check(!System.Text.Json.JsonSerializer.Serialize(log.Recent).Contains("private-auth-sentinel"),"raw body leaked");
        }finally{Directory.Delete(folder,true);}
    }
    private static async Task ReadNetwork()
    {
        foreach(var timeout in new[]{false,true})
        {
            using var f=new Fixture();await f.Login();var calls=0;f.Route=p=>{if(!p.StartsWith("/inventory/"))return null;calls++;if(timeout)throw new TaskCanceledException();throw new HttpRequestException();};await f.Vm.LoadAsync();
            Check(f.Vm.Connected&&!f.Vm.RequestsBlocked&&f.Vm.InventoryRead.State==SteamReadState.Failed&&calls==1,"read network failure changed authentication or retried");f.Route=null;await f.Vm.LoadListingsAsync();Check(f.Vm.ListingsRead.State==SteamReadState.Empty,"independent read after timeout failed");
        }
    }
    private static async Task HistoryFailure()
    {
        using var f=new Fixture();await f.Login();await f.Vm.LoadAsync();f.Vm.SelectAll.Execute(null);f.Vm.PricingMode=2;f.Route=p=>p.Contains("pricehistory")?new(HttpStatusCode.OK){Content=new StringContent("{}")}:null;await f.Vm.RefreshPricesAsync();
        Check(f.Vm.Connected&&f.Vm.PricesRead.State==SteamReadState.Failed&&f.Vm.HistoryRead.State==SteamReadState.Failed&&!f.Vm.Review.CanExecute(null),"history error not scoped or stale review allowed");f.Route=p=>p.Contains("pricehistory")?throw new HttpRequestException():null;await f.Vm.RefreshPricesAsync();Check(f.Vm.HistoryRead.State==SteamReadState.Failed&&f.Vm.HistoryRead.Failure==SteamFailure.Transport,"history network cause lost");f.Route=null;await f.Vm.LoadListingsAsync();Check(f.Vm.LoadFees.CanExecute(null),"history failure blocked other data");
    }
    internal sealed class Fixture:IDisposable
    {
        private readonly string folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage12-"+Guid.NewGuid().ToString("N"));public SteamContractServer Server{get;}=new();public SteamWorkspaceViewModel Vm{get;}public Func<string,HttpResponseMessage?>? Route;
        public Fixture(){Vm=new(scope=>new(folder,scope.AppId,scope.ContextId,scope.Name,contractHandler:new Routing(this),clock:Server.Clock),true){RiskAccepted=true,CredentialsAccepted=true};}
        public async Task Login(){await Vm.BeginAsync();Server.Clock.Advance();await Vm.CheckAsync();Check(Vm.Connected,"synthetic login failed");}
        public void Dispose(){Vm.Dispose();Server.Dispose();Directory.Delete(folder,true);}
        private sealed class Routing(Fixture f):HttpMessageHandler
        {private readonly HttpMessageInvoker inner=new(f.Server.Connection());protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>f.Route?.Invoke(request.RequestUri!.AbsolutePath)is {} response?Task.FromResult(response):inner.SendAsync(request,token);protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}}
    }
    internal static void Check(bool value,string message){if(!value)throw new Exception(message);}
}
