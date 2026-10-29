using System.Net;
using System.Text.Json;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.Tests;

public static class SubmissionTransportTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{
            ("Current own-session cookie follows a server rotation without secret logging",RotatedCookie),
            ("HTTP400 sell remains Unknown with safe status/body diagnosis and no Guard claim",BadRequest),
            ("Actual AJAX sell context headers match the owned inventory",SellHeaders),
            ("Read pacing is bounded independently and write spacing never falls below three seconds",Pacing),
            ("Confirmed canonical route saves a redirect but always rereads changed server mode",CanonicalReuse),
            ("Trading preflight reads account wallet and fee evidence from one Market response",SingleWallet),
            ("Current CSRF cookie reaches actual adapter POST after rotation immediately before send",RotatedSubmission),
            ("Error body success challenge malformed and oversized payloads never imply Guard or replay",ErrorBodies),
            ("Approval response distinguishes mobile email unspecified and no request",ApprovalChannels),
            ("Email approval persists without claiming phone approval and old journals remain compatible",EmailJournal),
            ("Missing server ID cannot turn a pending mobile request into a coincidental listing",PendingWithoutId),
            ("Empty HTTP200 cancel is an acknowledgement while empty HTTP400 stays Unknown",EmptyCancellation)
        };
        int passed=0,failed=0;foreach(var(name,run)in tests)try{await run();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}return(passed,failed);
    }
    private static HttpResponseMessage Json(string body,HttpStatusCode status=HttpStatusCode.OK)=>new(status){Content=new StringContent(body,System.Text.Encoding.UTF8,"application/json")};
    private static async Task RotatedCookie()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Login();
        var before=f.Workspace!.Authentication.SessionId;
        const string rotated="0123456789abcdef01234567";
        f.Route=p=>{if(p!="/market/mylistings/render/")return null;var response=Json(f.Server.ListingsJson());response.Headers.Add("Set-Cookie","sessionid="+rotated+"; Path=/; Secure");return response;};
        await f.Workspace.Orders.RefreshAsync();Check(f.Workspace.Authentication.SessionId==rotated&&before!=rotated&&f.Server.Sends==0);
    }
    private static async Task BadRequest()
    {
        var capture=new Trace();using var trace=DiagnosticHub.Use(capture);
        using var g=new FifthStageTests.Fixture();await g.Login();await g.Prepare(1);g.EnableTrading();
        g.Server.SellStatus=400;g.Server.SellBody="{\"success\":false,\"message\":\"invalid session private-never-log\",\"account\":\"private-never-log\"}";
        await g.Vm.SubmitAsync();Check(g.Vm.Results.Single().State==SaleState.Unknown&&g.Server.Sends==1);
        Check(capture.Events.Any(x=>x.Endpoint=="Sell"&&x.EventName=="ResponseContract"&&x.HttpStatus==400&&x.NewState=="JsonBody"));
        Check(g.Vm.Status.Contains("승인 대기로 확인되지")&&!g.Vm.Status.Contains("다른 조회"));
        Check(g.Vm.Results.Single().Message.Contains("HTTP 400")&&!g.Vm.Results.Single().Message.Contains("수동 확인 대기"));
        Check(!JsonSerializer.Serialize(capture.Events).Contains("private-never-log"));
        await g.Vm.SubmitAsync();Check(g.Server.Sends==1);
    }
    private static async Task SellHeaders(){using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();Check(f.Server.SellReferrer?.AbsolutePath=="/profiles/76561198000000001/inventory/"&&f.Server.SellAjax&&f.Server.Sends==1);}
    private static Task Pacing(){var at=DateTimeOffset.UtcNow;Check(SteamHttp.ReadGap==TimeSpan.FromMilliseconds(1500)&&SteamHttp.WriteGap==TimeSpan.FromSeconds(3));Check(SteamHttp.NextRequestAt(at,at,false,SteamHttp.ReadGap,SteamHttp.WriteGap)==at.AddSeconds(1.5));Check(SteamHttp.NextRequestAt(at,at,true,SteamHttp.ReadGap,SteamHttp.WriteGap)==at.AddSeconds(3));Check(SteamHttp.NextRequestAt(at.AddSeconds(2),at,true,SteamHttp.ReadGap,SteamHttp.WriteGap)==at.AddSeconds(3.5));return Task.CompletedTask;}
    private static string OwnerPage(bool newer)=>FeeModeTests.Page(flag:newer?"true":"false")+"<script>g_bViewingOwnProfile=true;</script>";
    private static string WalletPage=>FeeModeTests.Page().Split("<div")[0];
    private static async Task CanonicalReuse()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Login();var newer=false;var missing=false;
        f.Route=p=>p=="/market/"?Json(WalletPage):p.StartsWith("/profiles/")?new(HttpStatusCode.Found){Headers={Location=new Uri("/id/synthetic-owner/inventory/",UriKind.Relative)},Content=new StringContent("")}:p=="/id/synthetic-owner/inventory/"?missing?Json("{}",HttpStatusCode.NotFound):Json(OwnerPage(newer)):null;
        await f.Workspace!.FeesAsync(730);newer=true;await f.Workspace.FeesAsync(730);
        Check(f.Reads.Count(x=>x.Path.StartsWith("/profiles/"))==1&&f.Reads.Count(x=>x.Path=="/id/synthetic-owner/inventory/")==2&&f.Reads.Count(x=>x.Path.Contains("ajaxfeestructure"))==1&&f.Server.Sends==0);
        missing=true;try{await f.Workspace.FeesAsync(730);throw new Exception("expected404");}catch(SteamException error){Check(error.Code==SteamFailure.Unsupported);}
        var reads=f.Reads.Count;Check(f.Reads.Count(x=>x.Path.StartsWith("/profiles/"))==1);
        missing=false;await f.Workspace.FeesAsync(730);Check(f.Reads.Count>reads&&f.Reads.Count(x=>x.Path.StartsWith("/profiles/"))==2&&f.Server.Sends==0);
    }
    private static async Task SingleWallet()
    {
        using var f=new ThirteenthStageTests.Fixture();await f.Login();f.Server.Free=1;f.Route=p=>p=="/market/"?Json(FeeModeTests.Page(flag:"true")):null;
        await f.Workspace!.EnableUserTradingAsync(730,"2","Counter-Strike 2",true,true,default);
        Check(f.Reads.Count(x=>x.Path=="/market/")==1&&f.Workspace.Executor.CanExecute&&f.Server.Sends==0);
    }
    private static async Task RotatedSubmission(){using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.RotateSessionOnListings=true;await f.Vm.SubmitAsync();Check(f.Server.Sends==1&&f.Vm.Results.Single().State==SaleState.AwaitingSteamGuard);}
    private static async Task ErrorBodies()
    {
        foreach(var body in new[]{"{\"success\":true,\"needs_mobile_confirmation\":true,\"listingid\":\"123\"}","{\"requires_captcha\":true}","<h1>Forbidden</h1>","{\"message\":\"private-malformed",new string('x',17000)+"private-large"})
        {
            var capture=new Trace();using var trace=DiagnosticHub.Use(capture);using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.SellStatus=400;f.Server.SellBody=body;await f.Vm.SubmitAsync();
            Check(f.Server.Sends==1&&f.Vm.Results.Single().State==SaleState.Unknown&&f.Vm.Results.Single().Message.Contains("HTTP 400"));
            Check(!JsonSerializer.Serialize(capture.Events).Contains("private-"));await f.Vm.SubmitAsync();Check(f.Server.Sends==1);
        }
    }
    private static Task ApprovalChannels()
    {
        foreach(var (flags,channel) in new[]{("\"needs_mobile_confirmation\":true",SteamApprovalChannel.Mobile),("\"needs_email_confirmation\":1",SteamApprovalChannel.Email),("\"requires_confirmation\":true",SteamApprovalChannel.Unspecified),("\"needs_mobile_confirmation\":true,\"needs_email_confirmation\":true",SteamApprovalChannel.Either)})
        {var result=SteamContract.Sell("{\"success\":true,"+flags+"}");Check(result.State==SaleState.AwaitingSteamGuard&&result.Approval==channel);}
        Check(SteamContract.Sell("{\"success\":true}").Approval==SteamApprovalChannel.None&&SteamContract.Sell("{\"success\":false,\"needs_mobile_confirmation\":true}").State==SaleState.Unknown);
        Check(SteamContract.ApprovalHint(SteamApprovalChannel.Email).Contains("이메일")&&!SteamContract.ApprovalHint(SteamApprovalChannel.Email).Contains("모바일"));
        return Task.CompletedTask;
    }
    private static async Task EmailJournal()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.EmailConfirmation=true;await f.Vm.SubmitAsync();
        Check(f.Vm.Results.Single().State==SaleState.AwaitingSteamGuard&&f.Vm.Results.Single().Message.Contains("이메일")&&!f.Vm.Results.Single().Message.Contains("모바일")&&f.Server.Sends==1);
        var command=f.Workspace!.Store.Commands().Single(x=>x.Kind=="sell");
        var old=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(command))!.AsObject();old.Remove("Approval");
        Check(JsonSerializer.Deserialize<SteamCommandRecord>(old.ToJsonString())!.Approval==SteamApprovalChannel.None);
        var key=command.RequestKey;f.Vm.Dispose();
        using var reopened=new SteamStateStore(f.Directory);Check(reopened.ReadCommand(key)!.Approval==SteamApprovalChannel.Email);
    }
    private static async Task PendingWithoutId()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.MissingId=true;await f.Vm.SubmitAsync();
        f.Server.Publish=true;await f.Vm.RecoverAsync();Check(f.Server.Sends==1&&f.Vm.Results.Single().State==SaleState.AwaitingSteamGuard&&f.Vm.Results.Single().Message.Contains("서버 등록 ID 없음")&&f.Workspace!.Store.Commands().Single(x=>x.Kind=="sell").Approval==SteamApprovalChannel.Mobile);
    }
    private static async Task EmptyCancellation()
    {
        Check(SteamContract.CancellationAccepted("")&&SteamContract.CancellationAccepted(" ")&&SteamContract.CancellationAccepted("[]")&&!SteamContract.CancellationAccepted("{}"));
        foreach(var status in new[]{200,400})
        {
            using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();f.Server.Publish=true;await f.Vm.RecoverAsync();
            var listing=(await f.Workspace!.Orders.RefreshAsync()).Single();f.Server.EmptyCancelBody=true;f.Server.CancelStatus=status;
            var result=await f.Workspace.Executor.CancelAsync(listing,"empty-cancel");
            Check(f.Server.Cancels==1&&f.Server.Sends==1&&result.State==(status==200?SaleState.Submitted:SaleState.Unknown));
            if(status==200)Check((await f.Workspace.Executor.ReconcileCancellationAsync("empty-cancel")).State==SaleState.Reconciled);
            else Check(f.Workspace.Store.ReadCommand("empty-cancel")!.State==SaleState.Unknown);
        }
    }
    private sealed class Trace:IDiagnosticLog{public Guid SessionId{get;}=Guid.NewGuid();public List<DiagnosticRecord> Events{get;}=[];public void Write(DiagnosticRecord value)=>Events.Add(value);}
    private static void Check(bool value){if(!value)throw new Exception("submission transport contract assertion");}
}
