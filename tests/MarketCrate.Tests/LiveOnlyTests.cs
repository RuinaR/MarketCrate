using System.Net;
using MarketCrate.Core;
using MarketCrate.Infrastructure;
using MarketCrate.Infrastructure.Steam;
using MarketCrate.Infrastructure.Diagnostics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
namespace MarketCrate.Tests;

public static class LiveOnlyTests
{
    private const string Eligibility="https://steamcommunity.com/market/eligibilitycheck/?goto=%2Fmarket%2F";
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{("Steam eligibility GET evaluates server check then fixed Market return",RoundTrip),("Eligibility goto rejects external nested extra and double encoded destinations",Boundaries),("Eligibility401403429 stop with no return request or bypass",Limits),("Eligibility200 is incomplete permission not wallet or login success",Incomplete),("Repeated eligibility cycle stops and never generates wallet data",Loop),("Unknown Market redirect is never treated as eligibility",Unknown),("Eligibility trace preserves operation and excludes server cookies and raw bodies",Trace),("Production preferences survive restart and reject future schema without reset",Preferences)};
        int passed=0,failed=0;foreach(var(name,test)in tests)try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}return(passed,failed);
    }
    private static async Task RoundTrip()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Login();var root=0;f.Route=p=>
        {
            if(p=="/market/"&&root++==0)return Redirect(Eligibility);
            if(p=="/market/eligibilitycheck/"){var response=Redirect("https://steamcommunity.com/market/");response.Headers.Add("Set-Cookie","eligibility-test=server-signed-synthetic; Path=/; Secure; HttpOnly");return response;}
            if(p=="/market/"){Check(f.Workspace!.Authentication.Connected,"session lost during eligibility");return Json("var g_bLoggedIn=true; var g_rgWalletInfo={\"wallet_currency\":16};");}return null;
        };
        var wallet=await f.Workspace!.ProbeWalletAsync(default);Check(wallet.Currency=="KRW"&&!wallet.PricingMetadataAvailable&&f.Reads.Count==3&&f.Server.Sends==0,"eligibility contract or no default violated");
        Check(SteamHttp.DescribeMarketRedirect(new(Eligibility)).Path=="RedirectEligibilityPath","eligibility source not diagnosed");
    }
    private static Task Boundaries()
    {
        Check(SteamHttp.EligibilityTarget(new(Eligibility))==new Uri("https://steamcommunity.com/market/"),"fixed goto failed");
        foreach(var uri in new[]{"https://evil.invalid/market/eligibilitycheck/?goto=%2Fmarket%2F","http://steamcommunity.com/market/eligibilitycheck/?goto=/market/","https://steamcommunity.com/market/eligibilitycheck/?goto=https://evil.invalid","https://steamcommunity.com/market/eligibilitycheck/?goto=%252Fmarket%252F","https://steamcommunity.com/market/eligibilitycheck/?goto=/market/&token=private","https://steamcommunity.com/market/eligibilitycheck/?goto=/market/?token=private","https://steamcommunity.com/market/eligibilitycheck/?goto=/login/","https://steamcommunity.com/market/eligibilitycheck/?goto=/market/#private"})Check(SteamHttp.EligibilityTarget(new(uri)) is null,"untrusted goto accepted");return Task.CompletedTask;
    }
    private static async Task Limits()
    {
        foreach(var status in new[]{401,403,429})
        {
            using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Route=p=>p=="/market/"?Redirect(Eligibility):p=="/market/eligibilitycheck/"?new((HttpStatusCode)status){Content=new StringContent("restricted-private-canary")}:null;
            await Throws(()=>f.Workspace!.ProbeWalletAsync(default));Check(f.Reads.Count==2&&f.Workspace!.Failure==(status==401?SteamFailure.Authentication:status==403?SteamFailure.Forbidden:SteamFailure.RateLimited),"restriction bypassed");
        }
    }
    private static async Task Incomplete()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Route=p=>p=="/market/"?Redirect(Eligibility):p=="/market/eligibilitycheck/"?Json("eligibility not completed"):null;
        await Throws(()=>f.Workspace!.ProbeWalletAsync(default));Check(f.Vm.Connected&&f.Workspace!.FeatureFailure("Wallet")==SteamFailure.Authorization,"incomplete eligibility lost login");await Throws(()=>f.Workspace!.ProbeWalletAsync(default));Check(f.Reads.Count==2,"incomplete eligibility resent");await f.Workspace!.Orders.RefreshAsync();Check(f.Reads.Count==3,"independent read blocked");
    }
    private static async Task Loop(){using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Route=p=>p=="/market/"?Redirect(Eligibility):p=="/market/eligibilitycheck/"?Redirect("https://steamcommunity.com/market/"):null;await Throws(()=>f.Workspace!.ProbeWalletAsync(default));Check(f.Reads.Count==3&&f.Vm.Connected,"eligibility cycle retried");}
    private static async Task Unknown(){using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Route=p=>p=="/market/"?Redirect("https://steamcommunity.com/market/unknown/?goto=%2Fmarket%2F"):null;await Throws(()=>f.Workspace!.ProbeWalletAsync(default));Check(f.Reads.Count==1,"unknown path guessed eligibility");}
    private static async Task Trace()
    {
        var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","eligibility-trace-"+Guid.NewGuid().ToString("N"));try
        {
            using var log=new JsonDiagnosticLog(folder);using var use=DiagnosticHub.Use(log);using var f=new ThirteenthStageTests.Fixture();await f.Login();var roots=0;
            f.Route=p=>p=="/market/"&&roots++==0?Redirect(Eligibility):p=="/market/eligibilitycheck/"?WithCookie():p=="/market/"?Json("g_rgWalletInfo={\"wallet_currency\":16}; /* private-eligibility-body */"):null;
            using var operation=DiagnosticHub.Operation(DiagnosticEvent.ReadSelfCheck,DiagnosticArea.Data,"A04","SelfCheck");var id=DiagnosticHub.Context!.OperationId;await f.Workspace!.ProbeWalletAsync(default);
            foreach(var reason in new[]{"MarketEligibilityRedirect","MarketEligibilityReturn","WalletObject"})Check(log.Recent.Any(x=>x.Context.OperationId==id&&x.Reason==reason),"eligibility causal trace missing");
            var text=JsonSerializer.Serialize(log.Recent);foreach(var secret in new[]{"private-eligibility-body","private-eligibility-cookie","synthetic-cookie","76561198000000001","?goto="})Check(!text.Contains(secret),"eligibility secret leaked");await log.FlushAsync();
        }finally{Directory.Delete(folder,true);}
        static HttpResponseMessage WithCookie(){var response=Redirect("https://steamcommunity.com/market/");response.Headers.Add("Set-Cookie","eligibility-test=private-eligibility-cookie; Path=/; Secure; HttpOnly");return response;}
    }
    private static Task Preferences()
    {
        var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","preferences-"+Guid.NewGuid().ToString("N"));try
        {
            using(var store=new UserPreferencesStore(folder)){store.Save("diagnostic-mode",true);store.Save("reduced-motion",false);}
            using(var store=new UserPreferencesStore(folder))Check(store.Read("diagnostic-mode")&&!store.Read("reduced-motion",true),"preferences not persisted");
            using(var db=new SqliteConnection("Data Source="+Path.Combine(folder,"preferences.sqlite3")+";Pooling=False")){db.Open();using var command=db.CreateCommand();command.CommandText="PRAGMA user_version=99";command.ExecuteNonQuery();}
            try{using var invalid=new UserPreferencesStore(folder);throw new Exception("future schema reset");}catch(InvalidDataException){ }
            using(var db=new SqliteConnection("Data Source="+Path.Combine(folder,"preferences.sqlite3")+";Pooling=False")){db.Open();using var command=db.CreateCommand();command.CommandText="PRAGMA user_version";Check(Convert.ToInt32(command.ExecuteScalar())==99,"future DB altered");}
        }finally{Directory.Delete(folder,true);}return Task.CompletedTask;
    }
    private static async Task Throws(Func<Task> action){try{await action();}catch(SteamException){return;}throw new Exception("Expected safe typed block");}
    private static HttpResponseMessage Json(string text)=>new(HttpStatusCode.OK){Content=new StringContent(text)};
    private static HttpResponseMessage Redirect(string target){var response=new HttpResponseMessage(HttpStatusCode.Found){Content=new StringContent("")};response.Headers.Location=new(target);return response;}
    private static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
}
