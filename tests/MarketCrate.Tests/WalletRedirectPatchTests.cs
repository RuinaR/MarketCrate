using System.Net;
using System.Text.Json;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.Tests;

public static class WalletRedirectPatchTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{("Wallet same-origin slashless public route accepted with one hop",Slashless),("Observed302 remains independent of authentication and explains cause",Observed),("Redirect whitelist facts retain cause but exclude secret URL content",Facts),("Slash canonical redirect loop stops after two requests",Loop)};
        int passed=0,failed=0;foreach(var(name,run)in tests)try{await run();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}return(passed,failed);
    }
    private static async Task Slashless()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Login();
        f.Route=p=>p=="/market/"?Redirect("https://steamcommunity.com/market"):p=="/market"?Json("var g_rgWalletInfo={\"wallet_currency\":16,\"wallet_market_minimum\":100,\"wallet_currency_increment\":100};"):null;
        var result=await f.Workspace!.ProbeWalletAsync(default);
        Check(result.Currency=="KRW"&&result.PricingMetadataAvailable&&f.Reads.Count==2&&f.Vm.Connected,"canonical wallet did not complete");
    }
    private static async Task Observed()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Server.Free=0;f.Server.Publish=true;for(int i=0;i<8;i++)f.Server.Rows.Add(((800+i).ToString(),1,100));
        f.Route=p=>p=="/market/"?Redirect("https://steamcommunity.com/market/?unexpected=synthetic"):null;f.All();await f.Vm.SelfCheckAsync();
        var wallet=f.Vm.CheckRows.Single(x=>x.Step=="Wallet").Result!;
        Check(wallet.Outcome==ReadCheckOutcome.Unsupported&&wallet.Detail.Contains("이동")&&!wallet.Detail.Contains("인증 세션 확인 필요"),"redirect called authentication or missing wallet");
        Check(f.Vm.CheckRows.Single(x=>x.Step=="Listings").Result!.Count==8&&f.Vm.CheckRows.Single(x=>x.Step=="Inventory").Result!.Count==0,"observed independent reads lost");
        Check(f.Vm.Connected&&f.Workspace!.Failure is null&&f.Reads.Count==3&&f.Server.Sends==0&&f.Server.Cancels==0,"redirect retried or changed whole login");
    }
    private static async Task Facts()
    {
        var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","wallet-redirect-facts-"+Guid.NewGuid().ToString("N"));
        try
        {
            using var log=new JsonDiagnosticLog(folder);using var use=DiagnosticHub.Use(log);using var f=new ThirteenthStageTests.Fixture();await f.Login();
            f.Route=p=>p=="/market/"?Redirect("https://steamcommunity.com/market/private-canary-path?token=private-canary-token#private-canary-fragment"):null;
            using var operation=DiagnosticHub.Operation(DiagnosticEvent.ReadSelfCheck,DiagnosticArea.Data,"A04","SelfCheck");var id=DiagnosticHub.Context!.OperationId;
            await Unsupported(()=>f.Workspace!.ProbeWalletAsync(default));
            Check(log.Recent.Any(x=>x.Context.OperationId==id&&x.EventId==(int)DiagnosticEvent.ReadContract&&x.Reason=="RedirectMarketOther"&&x.PreviousState=="RedirectCommunity"&&x.NewState=="RedirectFragment"),"redirect facts or operation linkage missing");
            var data=JsonSerializer.Serialize(log.Recent);foreach(var secret in new[]{"private-canary","steamcommunity.com/market","synthetic-cookie","76561198000000001"})Check(!data.Contains(secret),"URL or account leaked");
            var unknown=SteamHttp.DescribeMarketRedirect(new("https://store.steampowered.com/account/?private-canary=value"));Check(unknown==("RedirectStore","RedirectAccount","RedirectOtherQuery"),"safe category wrong");
            Check(!SteamHttp.NormalMarketTarget(new("https://steamcommunity.com/market/"),new("https://steamcommunity.com/market?token=private-canary")),"new query bypass");
            await log.FlushAsync();
        }finally{Directory.Delete(folder,true);}
    }
    private static async Task Loop()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Route=p=>p=="/market/"?Redirect("https://steamcommunity.com/market"):p=="/market"?Redirect("https://steamcommunity.com/market/"):null;
        await Unsupported(()=>f.Workspace!.ProbeWalletAsync(default));Check(f.Reads.Count==2&&f.Vm.Connected,"canonical loop followed");
    }
    private static async Task Unsupported(Func<Task> run){try{await run();}catch(SteamException e)when(e.Code==SteamFailure.Unsupported){return;}throw new Exception("expected unsupported redirect");}
    private static HttpResponseMessage Redirect(string target){var response=new HttpResponseMessage(HttpStatusCode.Found){Content=new StringContent("")};response.Headers.Location=new(target);return response;}
    private static HttpResponseMessage Json(string text)=>new(HttpStatusCode.OK){Content=new StringContent(text)};
    private static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
}
