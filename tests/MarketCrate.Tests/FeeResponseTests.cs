using System.Net;
using System.Text.Json;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;
using MarketCrate.Infrastructure.Steam;

namespace MarketCrate.Tests;

public static class FeeResponseTests
{
    private const string FeeJson="{\"steam_fee\":{\"fee_rate\":\"0.05\",\"fee_cap\":\"0\"},\"publisher_fee\":{\"fee_rate\":\"0.1\",\"fee_cap\":\"0\"}}";
    private const string MarketHtml="<!doctype html><html><script>var g_bLoggedIn=true;var captchaLibrary='shared';</script><div id='market_home'>private-body-canary</div></html>";
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{("Fee HTTP200 Market HTML is schema failure, preserves login and independent reads",Market),
            ("Unknown fee HTML pauses requests without claiming CAPTCHA or session expiry",Unknown),
            ("Rendered login form scopes authorization without destroying QR session",Login),
            ("Rendered challenge and access-denied HTML halt even for wallet",Security),
            ("Response media and body mismatch remain separate; valid JSON still fully parsed",Media),
            ("Fee HTML inspection ignores shared scripts comments and incomplete markup",Markers),
            ("HTTP401403429 remain halted without retries or trade writes",Limits),
            ("Fee diagnostics retain operation correlation and exclude HTML header cookie canaries",Trace),
            ("Fee request URL method JSON Accept and invalid AppID are explicit",Request),
            ("KRW USD official-price comparison cannot enable trading or change sale plans",Comparison),
            ("Incomplete or added server fee fields never become a valid money contract",Contracts),
            ("Observed eight listings zero inventory wallet success fee HTML keeps facts independent",Observed),
            ("Legacy wallet fee fields are observed without becoming a replacement schedule",Legacy),
            ("Explicit JSON CAPTCHA requirement overrides otherwise valid fee fields",JsonChallenge)};
        int passed=0,failed=0;foreach(var(name,test)in tests)try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}return(passed,failed);
    }
    private static async Task Market()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Prepare();f.Route=p=>p.Contains("ajaxfeestructure")?Response(MarketHtml,"text/html"):null;f.All();await f.Vm.SelfCheckAsync();
        var row=f.Vm.CheckRows.Single(x=>x.Step=="Fees").Result!;
        Check(row.Error==DiagnosticError.Schema&&row.Outcome==ReadCheckOutcome.Failed&&row.Detail.Contains("장터 화면 HTML"),"HTML misreported as challenge");Check(f.Vm.ConnectionSummary.Contains("응답 계약")&&!f.Vm.CheckNextAction.Contains("권한이 제한"),"contract shown as authorization");
        Check(f.Workspace!.Failure is null&&f.Vm.Connected&&f.Workspace.FeatureFailure("Fees")==SteamFailure.ContractChanged,"login or independent request gate contaminated");
        Check(f.Reads.Count(x=>x.Path.Contains("ajaxfeestructure"))==1&&f.Server.Sends==0&&f.Server.Cancels==0,"fee retransmission or write");
        await f.Workspace.Orders.RefreshAsync();await Failure(()=>f.Workspace.ProbeFeeStructureAsync(730,default));Check(f.Reads.Count(x=>x.Path.Contains("ajaxfeestructure"))==1,"blocked fee reissued");
    }
    private static async Task Unknown()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Prepare();f.Route=p=>p.Contains("ajaxfeestructure")?Response("<html>unrecognized-response</html>","application/json"):null;f.All();await f.Vm.SelfCheckAsync();
        var result=f.Vm.CheckRows.Single(x=>x.Step=="Fees").Result!;Check(result.Error==DiagnosticError.Schema&&result.Outcome==ReadCheckOutcome.Blocked&&result.Detail.Contains("분류 불가"),"unknown guessed auth/CAPTCHA");
        Check(f.Vm.Connected&&f.Workspace!.Failure==SteamFailure.ContractChanged&&f.Vm.CheckRows.Single(x=>x.Step=="Orderbook").Result!.Outcome==ReadCheckOutcome.Blocked,"unknown safety stop lost");
        Check(!f.Reads.Any(x=>x.Path=="/market/orderbook")&&f.Server.Sends==0,"request after unknown");
    }
    private static async Task Login()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Route=p=>p.Contains("ajaxfeestructure")?Response("<html><form action='https://steamcommunity.com/login/dologin/'><input type='password'/></form></html>","text/html"):null;
        var error=await Failure(()=>f.Workspace!.ProbeFeeStructureAsync(730,default));Check(error.Code==SteamFailure.Authorization&&error.BodyKind==SteamBodyKind.HtmlLogin&&f.Vm.Connected&&f.Workspace!.Failure is null,"form equals global logout");await f.Workspace!.Orders.RefreshAsync();
    }
    private static async Task Security()
    {
        foreach(var html in new[]{"<html><form id='challenge-form'></form></html>","<html><h1>Access Denied</h1></html>"})
        {
            using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Route=p=>p=="/market/"?Response(html,"text/html"):null;
            var error=await Failure(()=>f.Workspace!.ProbeWalletAsync(default));Check(error.Code is SteamFailure.Challenge or SteamFailure.Forbidden&&f.Workspace!.Failure==error.Code,"wallet bypassed security");
            var count=f.Reads.Count;await Failure(()=>f.Workspace!.Orders.RefreshAsync());Check(f.Reads.Count==count,"security resent");
        }
    }
    private static async Task Media()
    {
        foreach(var media in new[]{"application/json","text/html","text/plain"})
        {using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Route=p=>p.Contains("ajaxfeestructure")?Response(FeeJson,media):null;var fee=await f.Workspace!.ProbeFeeStructureAsync(730,default);Check(fee.Steam.Millionths==50000&&!fee.AdditionalFields&&f.Workspace.Failure is null,"valid JSON confused with HTML");}
        using var broken=new ThirteenthStageTests.Fixture();await broken.Login();broken.Route=p=>p.Contains("ajaxfeestructure")?Response("{broken-json}","application/json"):null;var error=await Failure(()=>broken.Workspace!.ProbeFeeStructureAsync(730,default));Check(error.Diagnostic==DiagnosticError.Parsing,"JSON parse error misclassified");
    }
    private static Task Markers()
    {
        Check(SteamResponseContract.Inspect(MarketHtml)==SteamBodyKind.HtmlMarket,"market marker");
        Check(SteamResponseContract.Inspect("<html><script>var captcha=\"<form id='challenge-form'>\";</script><!-- <div class='g-recaptcha'> --></html>")==SteamBodyKind.HtmlUnknown,"shared script treated as widget");
        Check(SteamResponseContract.Inspect("<html>captcha</html>")==SteamBodyKind.HtmlUnknown,"word implies challenge");
        Check(SteamResponseContract.Inspect("<html><script>var html=\"<form id='challenge-form'>\"")==SteamBodyKind.HtmlUnknown,"unterminated script treated as widget");
        Check(SteamResponseContract.Inspect(" \uFEFF {\"a\":1}")==SteamBodyKind.JsonBody,"BOM classification");
        Check(SteamResponseContract.Inspect("")==SteamBodyKind.EmptyBody&&SteamResponseContract.Inspect("not json")==SteamBodyKind.OtherBody,"empty/other classification");return Task.CompletedTask;
    }
    private static async Task Limits()
    {
        foreach(var status in new[]{401,403,429}){using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Route=p=>p.Contains("ajaxfeestructure")?new((HttpStatusCode)status){Content=new StringContent(MarketHtml)}:null;var error=await Failure(()=>f.Workspace!.ProbeFeeStructureAsync(730,default));var calls=f.Reads.Count;await Failure(()=>f.Workspace!.Orders.RefreshAsync());Check(f.Reads.Count==calls&&f.Workspace!.Failure==error.Code&&f.Server.Sends==0,"HTTP limit retried");}
    }
    private static async Task Trace()
    {
        var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","fee-trace-"+Guid.NewGuid().ToString("N"));try
        {
            using var log=new JsonDiagnosticLog(folder);using var use=DiagnosticHub.Use(log);using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Route=p=>p.Contains("ajaxfeestructure")?Response(MarketHtml,"application/json"):null;
            using var op=DiagnosticHub.Operation(DiagnosticEvent.ReadSelfCheck,DiagnosticArea.Data,"D10","SelfCheck");var id=DiagnosticHub.Context!.OperationId;
            await Failure(()=>f.Workspace!.ProbeFeeStructureAsync(730,default));
            var observed=log.Recent.Single(x=>x.EventId==3005&&x.Endpoint=="Fees");Check(observed.Context.OperationId==id&&observed.Context.CorrelationId is not null&&observed.PreviousState=="MediaJson"&&observed.NewState=="HtmlMarket"&&observed.Reason=="MediaTypeMismatch"&&observed.HttpStatus==200,"shape trace missing");
            Check(log.Recent.Any(x=>x.Context.OperationId==id&&x.Error==DiagnosticError.Schema&&x.Reason=="HtmlMarket"),"cause disconnected");
            var output=JsonSerializer.Serialize(log.Recent);foreach(var secret in new[]{"private-body-canary","private-cookie-canary","76561198000000001","g_bLoggedIn","<html>"})Check(!output.Contains(secret),"secret leaked");await log.FlushAsync();
        }finally{Directory.Delete(folder,true);}
    }
    private static Task Request(){using var request=SteamFees.StructureRequest(730);Check(request.Method==HttpMethod.Get&&request.RequestUri!.AbsoluteUri=="https://steamcommunity.com/market/ajaxfeestructure/?appid=730"&&request.Headers.Accept.Any(x=>x.MediaType=="application/json")&&!request.Headers.Contains("Cookie"),"request changed or cookie imported");try{SteamFees.StructureRequest(0);throw new Exception("invalid AppID accepted");}catch(ArgumentOutOfRangeException){}return Task.CompletedTask;}
    private static async Task Comparison()
    {
        foreach(var currency in new[]{"KRW","USD"}){using var f=new ThirteenthStageTests.Fixture();f.Server.Currency=currency;await f.Prepare();f.Vm.OfficialSeller=currency=="KRW"?"100":"1.00";f.Vm.OfficialBuyer=currency=="KRW"?"115":"1.15";Check(f.Vm.CompareOfficialFees.CanExecute(null),"comparison disconnected");var reads=f.Reads.Count;f.Vm.CompareOfficialFees.Execute(null);Check(f.Vm.FeeComparison.Contains("계산 일치")&&!f.Workspace!.Access.Trading&&f.Reads.Count==reads&&f.Server.Sends==0,"comparison enabled trade or sent HTTP");f.Vm.OfficialBuyer="999";f.Vm.CompareOfficialFees.Execute(null);Check(f.Vm.FeeComparison.Contains("불일치"),"mismatch success");f.Vm.OfficialSeller="invalid";f.Vm.CompareOfficialFees.Execute(null);Check(f.Vm.FeeComparison.Contains("금액 형식"),"invalid accepted");f.Vm.Logout.Execute(null);Check(!f.Vm.CompareOfficialFees.CanExecute(null)&&!f.Vm.FeeComparison.Contains("계산 일치"),"stale account comparison");}
    }
    private static Task Contracts()
    {
        foreach(var json in new[]{"{}",FeeJson.Replace("0.05","0.0000001"),FeeJson.Replace("\"publisher_fee\"","\"missing_fee\"")}){try{SteamFees.Structure(json,new("KRW",100,100));throw new Exception("incomplete fee accepted");}catch(SteamException){}}
        var extra=FeeJson[..^1]+",\"new_tax\":1}";try{SteamFees.Structure(extra,new("KRW",100,100));throw new Exception("new field silently ignored");}catch(SteamException){}
        try{new SteamFeeSchedule("KRW",1,1,new(50000),new(100000)).Validate();throw new Exception("unknown KRW wire unit accepted");}catch(ArgumentException){}return Task.CompletedTask;
    }
    private static async Task Observed()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Server.Free=0;f.Server.Publish=true;for(int i=0;i<8;i++)f.Server.Rows.Add(((900+i).ToString(),1,100));
        f.Route=p=>p.Contains("ajaxfeestructure")?Response(MarketHtml,"text/html"):null;f.All();await f.Vm.SelfCheckAsync();
        Check(f.Vm.CheckRows.Single(x=>x.Step=="Listings").Result!.Count==8&&f.Vm.CheckRows.Single(x=>x.Step=="Inventory").Result!.Count==0,"8/0 evidence lost");
        Check(f.Vm.CheckRows.Single(x=>x.Step=="Wallet").Result!.Outcome==ReadCheckOutcome.Succeeded&&f.Vm.CheckRows.Single(x=>x.Step=="Fees").Result!.Error==DiagnosticError.Schema,"wallet confused with fee failure");
        Check(f.Vm.CheckRows.Single(x=>x.Step=="Orderbook").Result!.Outcome==ReadCheckOutcome.Unverified&&f.Vm.Connected&&f.Server.Sends==0,"no selected item invented or session lost");
    }
    private static Task Legacy()
    {
        const string root="var g_bLoggedIn=true;var g_rgWalletInfo={\"wallet_currency\":16,\"wallet_fee\":1,\"wallet_fee_percent\":\"0.05\",\"wallet_fee_minimum\":\"1\",\"wallet_fee_base\":\"0\",\"wallet_publisher_fee_percent_default\":\"0.1\"};";
        var wallet=SteamFees.ObserveWallet(root);Check(wallet.LegacyFees is{Complete:true,SteamMillionths:50000,MinimumWire:1,BaseWire:0,DefaultPublisherMillionths:100000}&&!wallet.PricingMetadataAvailable,"legacy inferred KRW unit or silently missing");
        Check(SteamFees.ObserveWallet(root.Replace("0.05","invalid")).LegacyFees is{Invalid:true}&&SteamFees.ObserveWallet(root.Replace("0.05","invalid")).Currency=="KRW","optional fee invalidated wallet currency");
        Check(SteamFees.ObserveWallet("g_rgWalletInfo={\"wallet_currency\":16};").LegacyFees is{Complete:false,Fields:0},"legacy defaults generated");return Task.CompletedTask;
    }
    private static async Task JsonChallenge()
    {
        foreach(var body in new[]{FeeJson[..^1]+",\"captcha_needed\":true}","{\"response\":{\"captcha_needed\":true}}","{\"requires_captcha\":true}"})
        {using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Route=p=>p.Contains("ajaxfeestructure")?Response(body,"application/json"):null;var error=await Failure(()=>f.Workspace!.ProbeFeeStructureAsync(730,default));Check(error.Code==SteamFailure.Challenge&&f.Workspace!.Failure==SteamFailure.Challenge,"explicit requirement ignored");var calls=f.Reads.Count;await Failure(()=>f.Workspace!.Orders.RefreshAsync());Check(f.Reads.Count==calls&&f.Server.Sends==0,"request after explicit challenge");}
    }
    private static HttpResponseMessage Response(string body,string media){var response=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body,System.Text.Encoding.UTF8,media)};response.Headers.Add("Set-Cookie","synthetic=private-cookie-canary; Path=/; Secure; HttpOnly");return response;}
    private static async Task<SteamException> Failure(Func<Task> action){try{await action();}catch(SteamException error){return error;}throw new Exception("expected SteamException");}
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
}
