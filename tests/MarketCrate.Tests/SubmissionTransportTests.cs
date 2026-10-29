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
            ("Empty HTTP200 cancel is an acknowledgement while empty HTTP400 stays Unknown",EmptyCancellation),
            ("Server inventory stays selectable after an ambiguous POST while new transmission is guarded",ReservedInventory),
            ("Stack server quantity is not rewritten by historical local intents",PartialReservation),
            ("Normal listing refresh synchronizes a known accepted request without a new POST",KnownListingSync),
            ("Changed listing fields and ID-less candidates never clear an ambiguous transmission",ConflictingListingSync),
            ("Classic owned-app stack amount uses one exact full or partial POST",SingleStack),
            ("Unverified new-mode stack and multi-price stack cannot send",StackBoundaries),
            ("ID-less request reports exact server asset membership without claiming attribution",IdlessObservation),
            ("Stack contract changed after review blocks before intent and transmission",StackModeChanged)
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
            if(status==200){Check((await f.Workspace.Executor.ReconcileCancellationAsync("empty-cancel")).State==SaleState.Reconciled);Check(f.Workspace.Store.ReservedSells().Count==0);}
            else Check(f.Workspace.Store.ReadCommand("empty-cancel")!.State==SaleState.Unknown);
        }
    }
    private static async Task ReservedInventory()
    {
        var trace=new Trace();using var diagnostics=DiagnosticHub.Use(trace);
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();
        f.Server.SellStatus=400;f.Server.SellBody="{\"success\":false}";await f.Vm.SubmitAsync();
        var original=f.Workspace!.Store.Commands().Single(x=>x.Kind=="sell");Check(original.State==SaleState.Unknown);
        f.Vm.Logout.Execute(null);await f.Login();await f.Vm.LoadAsync();
        var row=f.Vm.Assets.Single();Check(row.Owned==1&&row.Available==1&&row.CanSelect&&row.PendingSubmissionQuantity==1&&row.SelectionHint.Contains("이전")&&f.Vm.HasPendingInventoryRequests&&f.Vm.InventorySyncSummary.Contains("이전 요청"));
        f.Vm.SelectAll.Execute(null);Check(row.Selected&&f.Vm.OpenPendingInventoryRequests.CanExecute(null));
        await f.Vm.RefreshPricesAsync();f.Vm.BuildReview();Check(f.Vm.ReviewOpen&&!f.Vm.Submit.CanExecute(null));await f.Vm.SubmitAsync();Check(f.Server.Sends==1&&f.Vm.Status.Contains("이전 전송"));f.Vm.CloseReview.Execute(null);
        var sends=f.Server.Sends;f.Vm.OpenPendingInventoryRequests.Execute(null);
        Check(f.Vm.WorkPage==3&&f.Vm.SelectedRequest==original.RequestKey&&f.Vm.Results.Any(x=>x.State==SaleState.Unknown)&&f.Server.Sends==sends&&f.Workspace.Store.Available(row.Asset)==0);
        Check(trace.Events.Any(x=>x.Reason=="InventoryLocalPendingRows"&&x.Count==1)&&trace.Events.Any(x=>x.Reason=="InventorySelectableRows"&&x.Count==1)&&trace.Events.Any(x=>x.Reason=="InventoryReservationOpened"&&x.Count==1));
        Check(!JsonSerializer.Serialize(trace.Events).Contains(original.RequestKey));
        await f.Vm.RefreshSelectedRequestAsync();Check(f.Server.Sends==1&&f.Workspace.Store.Available(row.Asset)==0&&f.Workspace.Store.ReadCommand(original.RequestKey)!.State==SaleState.Unknown);
    }
    private static async Task PartialReservation()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=2;await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.SellStatus=400;f.Server.SellBody="{\"success\":false}";await f.Vm.SubmitAsync();
        f.Vm.Logout.Execute(null);await f.Login();await f.Vm.LoadAsync();var row=f.Vm.Assets.Single();
        Check(row.Owned==2&&row.Available==2&&row.Maximum==2&&row.PendingSubmissionQuantity==1&&row.CanSelect&&f.Vm.HasPendingInventoryRequests);row.Quantity=2;row.Selected=true;Check(row.Quantity==2&&row.Selected&&f.Server.Sends==1);
    }
    private static async Task KnownListingSync()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();var key=f.Workspace!.Store.Commands().Single().RequestKey;
        f.Server.Publish=true;await f.Vm.LoadListingsAsync();Check(f.Vm.Status.Contains("세션 활성"));Check(f.Workspace.Store.ReadCommand(key)!.State==SaleState.Listed&&f.Vm.Results.Any(x=>x.State==SaleState.Listed)&&f.Server.Sends==1);
        await f.Vm.LoadAsync();Check(f.Vm.Assets.Count==0&&f.Server.Sends==1);
    }
    private static async Task ConflictingListingSync()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();var command=f.Workspace!.Store.Commands().Single();
        f.Server.Publish=true;var listing=(await f.Workspace.Orders.RefreshAsync()).Single();
        Check(f.Workspace.Executor.ObserveListings([listing with{SellerReceives=listing.SellerReceives+1}]).Count==0&&f.Workspace.Store.ReadCommand(command.RequestKey)!.State==SaleState.AwaitingSteamGuard);
        Check(f.Workspace.Executor.ObserveListings([]).Count==0&&f.Workspace.Store.ReadCommand(command.RequestKey)!.State==SaleState.AwaitingSteamGuard&&f.Server.Sends==1);
    }
    private static string ClassicStackPage=>WalletPage+"<script>g_bViewingOwnProfile=true;g_bReactSellDialog=false;</script>";
    private static async Task SingleStack()
    {
        foreach(var quantity in new[]{1,2,10})
        {
            using var f=new ThirteenthStageTests.Fixture();var owned=Math.Max(2,quantity);f.Server.Free=owned;await f.Login();f.Route=p=>p=="/market/"?Json(WalletPage):p.StartsWith("/profiles/")?Json(ClassicStackPage):null;
            await f.Vm.LoadAsync();f.Vm.Assets.Single().Selected=true;f.Vm.Assets.Single().Quantity=quantity;await f.Vm.RefreshPricesAsync();f.Vm.BuildReview();
            await f.Workspace!.EnableUserTradingAsync(730,"2","Counter-Strike 2",true,true,default);f.Vm.TradeAccepted=true;
            if(quantity==10){Check(f.Vm.ReviewRows.Count==3&&!f.Vm.CanTradeReview);foreach(var row in f.Vm.ReviewRows)row.PriceText="115";}
            Check(f.Workspace.Policy.SingleRequestStackApps.Contains(730)&&!f.Workspace.Policy.StackAmountsSupported&&f.Vm.CanTradeReview);
            await f.Vm.SubmitAsync();Check(f.Server.Sends==1&&f.Server.Rows.Single().Quantity==quantity&&f.Server.Free==owned-quantity&&f.Vm.Results.Single().State==SaleState.AwaitingSteamGuard);
            var batch=await f.Workspace.Store.LoadAsync(default);Check(batch!.Snapshot.Lines.Length==1&&batch.Snapshot.Lines[0].Quantity==quantity&&batch.Snapshot.Gross==115L*quantity);
        }
    }
    private static async Task StackBoundaries()
    {
        foreach(var classic in new[]{false,true})
        {
            using var f=new ThirteenthStageTests.Fixture();f.Server.Free=classic?10:2;await f.Login();f.Route=p=>p=="/market/"?Json(WalletPage):p.StartsWith("/profiles/")?Json(classic?ClassicStackPage:OwnerPage(true)):null;
            await f.Vm.LoadAsync();f.Vm.Assets.Single().Selected=true;f.Vm.Assets.Single().Quantity=f.Server.Free;await f.Vm.RefreshPricesAsync();f.Vm.BuildReview();
            try{await f.Workspace!.EnableUserTradingAsync(730,"2","Counter-Strike 2",true,true,default);}catch(SteamException)when(!classic){}
            f.Vm.TradeAccepted=true;Check(!f.Vm.CanTradeReview&&f.Server.Sends==0);
            if(classic)
            {
                Check(f.Vm.SubmissionAvailability.Contains("단일 가격")&&f.Vm.ReviewRows.Count==3);
                var batch=new SaleBatchSnapshot(Guid.NewGuid(),f.Workspace!.Authentication.SteamId!,DateTimeOffset.UtcNow,[..f.Vm.ReviewRows.Select((row,index)=>row.Snapshot(index))]);
                var blocked=false;try{await f.Workspace.Executor.ValidateBatchAsync(batch,default);}catch(SteamException){blocked=true;}
                Check(blocked&&f.Workspace.Store.Commands().Count==0&&f.Server.Sends==0);
            }
        }
    }
    private static async Task IdlessObservation()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.MissingId=true;await f.Vm.SubmitAsync();var command=f.Workspace!.Store.Commands().Single();
        f.Server.Publish=true;var report=await f.Workspace.Executor.InspectAsync(command.RequestKey);
        Check(report.ObservedAssetState==SaleState.Listed&&report.ObservedListingId is not null&&report.Verdict==SteamEvidenceReason.CandidateOnly&&f.Workspace.Store.ReadCommand(command.RequestKey)!.ListingId is null);
        var result=await f.Workspace.Executor.ReconcileAsync(command.Line,command.RequestKey,default);Check(result.State==SaleState.AwaitingSteamGuard&&result.Message.Contains("현재 Steam 동일 자산 상태: 판매 중")&&f.Server.Sends==1);
        f.Server.Rows.Add(("9002",command.Line.Quantity,command.Line.UnitQuote.SellerReceives));report=await f.Workspace.Executor.InspectAsync(command.RequestKey);
        Check(report.ObservedAssetState is null&&report.ObservedListingId is null&&report.Verdict==SteamEvidenceReason.MultipleCandidates);f.Server.Rows.RemoveAt(f.Server.Rows.Count-1);
        f.Server.ListingAppId=440;report=await f.Workspace.Executor.InspectAsync(command.RequestKey);Check(report.ObservedAssetState is null&&report.ObservedListingId is null);
    }
    private static async Task StackModeChanged()
    {
        using var f=new ThirteenthStageTests.Fixture();f.Server.Free=2;await f.Login();var newer=false;
        f.Route=p=>p=="/market/"?Json(WalletPage):p.StartsWith("/profiles/")?Json(newer?OwnerPage(true):ClassicStackPage):null;
        await f.Vm.LoadAsync();f.Vm.Assets.Single().Selected=true;f.Vm.Assets.Single().Quantity=2;await f.Vm.RefreshPricesAsync();f.Vm.BuildReview();await f.Workspace!.EnableUserTradingAsync(730,"2","Counter-Strike 2",true,true,default);f.Vm.TradeAccepted=true;
        Check(f.Vm.CanTradeReview);newer=true;await f.Vm.SubmitAsync();Check(f.Server.Sends==0&&f.Workspace.Store.Commands().Count==0&&f.Vm.Status.Contains("스택"));
    }
    private sealed class Trace:IDiagnosticLog{public Guid SessionId{get;}=Guid.NewGuid();public List<DiagnosticRecord> Events{get;}=[];public void Write(DiagnosticRecord value)=>Events.Add(value);}
    private static void Check(bool value){if(!value)throw new Exception("submission transport contract assertion");}
}
