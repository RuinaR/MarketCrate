using System.Net;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.Tests;
public static class InstantSaleRefreshTests
{
    private static void Check(bool ok,string reason="Instant sale refresh contract"){if(!ok)throw new Exception(reason);}
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{("Already gone selected listing refreshes before any fee lookup",GoneBeforeFees),("Changed listing price invalidates cached reprice before fee lookup",ChangedBeforeFees),("Current listing still requires app fee contract without fallback",MissingFees),("ID-less disappeared receipt is not an endless current Guard claim",MissingReceipt),("Known-ID absence remains unconfirmed and another asset can form a new batch",NextBatch),("Denied fresh listing read cannot prepare cancellation or request fees",Denied)};
        tests=[..tests,("Listing filling during fee lookup invalidates stale reprice without fee retry",GoneDuringFees)];
        tests=[..tests,("Known listing filled before first refresh is sold and survives restart",SoldBeforeRefresh),("Already posted listing transitions to sold without reverting historical evidence",SoldAfterListed),("ID-less exact asset sale is observation without fabricated request attribution",ObservedSold),("History purchase cancellation rollback currency amount and duplicate evidence cannot imply sold",InvalidHistory)];
        tests=[..tests,("History403 preserves old receipt and prevents repeat traffic or resubmission",HistoryDenied)];
        tests=[..tests,("Off-grid historical KRW row cannot abort other instant-filled batch lines",OffGridBatch),("Off-grid target is not rounded into an approved amount",OffGridTarget),("History mismatch reason distinguishes amount from missing listing",AmountDiagnosis),("Wire parser preserves USD cents and KRW integer precision",RawUnits)];
        tests=[..tests,("Public purchase contract nullable hold and revoked fields do not abort history",NullableFlags)];
        int passed=0,failed=0;foreach(var(name,test)in tests)try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}return(passed,failed);
    }
    private static HttpResponseMessage Html(string body)=>new(HttpStatusCode.OK){Content=new StringContent(body)};
    private static async Task Active(ThirteenthStageTests.Fixture f){await f.Login();f.Server.Publish=true;f.Server.Rows.Add(("900",1,100));await f.Vm.LoadListingsAsync();f.Vm.SelectAllListings.Execute(null);f.Reads.Clear();}
    private static void MissingContext(ThirteenthStageTests.Fixture f)
    {
        var page=FeeModeTests.Page().Replace("g_rgAppContextData={\"730\":{\"market_pubfee_rate\":0.10}};","g_rgAppContextData={};");
        f.Route=p=>p=="/market/"||p.StartsWith("/profiles/")?Html(page):null;
    }
    private static bool FeeRequest((string Method,string Path) r)=>r.Path=="/market/"||r.Path.Contains("ajaxfeestructure")||r.Path.StartsWith("/profiles/");
    private static async Task GoneBeforeFees(){using var f=new ThirteenthStageTests.Fixture();await Active(f);f.Server.Rows.Clear();f.Server.Free=0;MissingContext(f);await f.Vm.PrepareListingRepriceAsync();Check(f.Reads.Any(x=>x.Path.Contains("mylistings"))&&!f.Reads.Any(FeeRequest)&&f.Vm.Listings.Count==0&&!f.Vm.ManagementOpen&&f.Vm.Connected&&f.Server.Cancels==0&&f.Server.Sends==0);}
    private static async Task ChangedBeforeFees(){using var f=new ThirteenthStageTests.Fixture();await Active(f);f.Server.Rows[0]=("900",1,200);MissingContext(f);await f.Vm.PrepareListingRepriceAsync();Check(!f.Reads.Any(FeeRequest)&&f.Vm.Listings.Single().Listing.SellerReceives==200&&!f.Vm.ManagementOpen&&f.Server.Cancels==0);}
    private static async Task MissingFees(){using var f=new ThirteenthStageTests.Fixture();await Active(f);MissingContext(f);await f.Vm.PrepareListingRepriceAsync();Check(f.Reads.Any(x=>x.Path.Contains("mylistings"))&&f.Vm.FeesRead.State==MarketCrate.App.ViewModels.SteamReadState.Unsupported&&f.Vm.Connected&&f.Vm.Listings.Count==1&&!f.Vm.ManagementOpen&&f.Server.Cancels==0);}
    private static async Task GoneDuringFees()
    {
        using var f=new ThirteenthStageTests.Fixture();await Active(f);MissingContext(f);var route=f.Route;
        f.Route=p=>{if(p=="/market/"){f.Server.Rows.Clear();f.Server.Free=0;}return route!(p);};
        await f.Vm.PrepareListingRepriceAsync();
        Check(f.Reads.Count(x=>x.Path.Contains("mylistings"))==2&&f.Reads.Count(x=>x.Path=="/market/")==1&&f.Vm.Listings.Count==0&&f.Vm.FeesRead.State==MarketCrate.App.ViewModels.SteamReadState.Stale&&!f.Vm.ManagementOpen&&f.Vm.Connected&&f.Server.Cancels==0&&f.Server.Sends==0);
    }
    private static async Task MissingReceipt()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.MissingId=true;await f.Vm.Submit.ExecuteAsync();var command=f.Workspace!.Store.Commands().Single();f.Server.Rows.Clear();await f.Vm.Recover.ExecuteAsync();
        Check(f.Vm.Results.Single().State==SaleState.Unknown&&!f.Vm.HasApprovalHandoff&&f.Vm.Results.Single().Message.Contains("현재")&&f.Workspace.Store.ReadCommand(command.RequestKey)==command&&f.Server.Sends==1);
        await f.Vm.Recover.ExecuteAsync();Check(f.Server.Sends==1&&f.Vm.Results.Single().State==SaleState.Unknown);
    }
    private static async Task NextBatch()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.Submit.ExecuteAsync();var first=(await f.Workspace!.Store.LoadAsync(default))!;f.Server.Rows.Clear();await f.Vm.Recover.ExecuteAsync();Check(f.Vm.Results.Single().State==SaleState.Unknown&&f.Server.Sends==1);
        f.Server.ManyAssets=2;f.Server.Free=1;await f.Vm.Load.ExecuteAsync();f.Vm.ClearSelection.Execute(null);f.Vm.Assets.Single(x=>x.Asset.Key.AssetId=="102").Selected=true;await f.Vm.RefreshPrices.ExecuteAsync();f.Vm.Review.Execute(null);Check(f.Vm.ReviewOpen);await f.Vm.Submit.ExecuteAsync();
        Check(f.Server.Sends==2&&f.Server.SellFields.Last()["assetid"]=="102"&&(await f.Workspace.Store.FindBatchAsync(first.Snapshot.BatchId,default))!.Results.Single().State==SaleState.Unknown);
    }
    private static async Task Denied(){using var f=new ThirteenthStageTests.Fixture();await Active(f);f.Route=p=>p.Contains("mylistings")?new(HttpStatusCode.Forbidden){Content=new StringContent("{}")} :null;await f.Vm.PrepareListingRepriceAsync();Check(!f.Reads.Any(FeeRequest)&&!f.Vm.ManagementOpen&&f.Vm.Connected&&f.Vm.RequestsBlocked&&f.Server.Cancels==0&&f.Server.Sends==0);}
    private static string History(SteamCommandRecord command,string? id=null)
    {
        id??=command.ListingId??"950";var at=command.SentAt!.Value.ToUnixTimeSeconds();var line=command.Line;var asset=new{appid=line.Asset.Key.AppId,contextid=line.Asset.Key.ContextId,id=line.Asset.Key.AssetId,amount="0"};
        return System.Text.Json.JsonSerializer.Serialize(new{success=true,start=0,pagesize=100,total_count=1,events=new[]{new{listingid=id,purchaseid="880",event_type=3,time_event=at}},listings=new Dictionary<string,object>{{id,new{listingid=id,currencyid=2016,original_price=SteamContract.ToWire(line.UnitQuote.SellerReceives,line.Currency),asset}}},purchases=new Dictionary<string,object>{{id+"_880",new{listingid=id,purchaseid="880",time_sold=at,received_currencyid="2016",received_amount=SteamContract.ToWire(line.UnitQuote.SellerReceives,line.Currency),asset=new{asset.appid,asset.contextid,id="changed-buyer-id",amount="1"},failed=0,needs_rollback=0,funds_returned=0}}}});
    }
    private static async Task SoldBeforeRefresh()
    {
        var log=new CaptureLog();using var diagnostic=DiagnosticHub.Use(log);
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();var command=f.Workspace!.Store.Commands().Single();f.Server.Rows.Clear();f.Server.MarketHistoryOverride=History(command);await f.Vm.RecoverAsync();
        Check(f.Vm.Results.Single().State==SaleState.Sold&&f.Vm.BatchProgressText.Contains("판매 완료 1")&&!f.Vm.HasApprovalHandoff&&f.Workspace.Store.Commands().Single().State==SaleState.Sold&&f.Workspace.Store.ReservedSells().Count==0);
        var reads=f.Server.MarketHistoryReads;await f.Vm.RecoverAsync();Check(f.Server.Sends==1&&f.Server.MarketHistoryReads==reads);
        f.Vm.Logout.Execute(null);await f.Login();await f.Vm.RecoverAsync();Check(f.Vm.Results.Single().State==SaleState.Sold&&f.Server.Sends==1&&!f.Vm.CanTrade);
        Check(log.Records.Any(x=>x.Endpoint=="MarketHistory"&&x.Reason=="HistorySaleMatched"&&x.Context.OperationId!=Guid.Empty));
        var diagnostics=System.Text.Json.JsonSerializer.Serialize(log.Records);foreach(var secret in new[]{"76561198000000001","synthetic-cookie","synthetic-refresh","received_amount","purchaseid"})Check(!diagnostics.Contains(secret));
    }
    private static async Task SoldAfterListed()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();f.Server.Publish=true;await f.Vm.RecoverAsync();Check(f.Vm.Results.Single().State==SaleState.Listed);var command=f.Workspace!.Store.Commands().Single();f.Server.Rows.Clear();await f.Vm.RecoverAsync();Check(f.Vm.Results.Single().State==SaleState.Listed&&f.Vm.Results.Single().Message.Contains("과거 게시"));f.Server.MarketHistoryOverride=History(command);await f.Vm.RecoverAsync();Check(f.Vm.Results.Single().State==SaleState.Sold&&f.Server.Sends==1);
    }
    private static async Task ObservedSold()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;f.Server.MissingId=true;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();var command=f.Workspace!.Store.Commands().Single();f.Server.Rows.Clear();f.Server.MarketHistoryOverride=History(command);await f.Vm.RecoverAsync();Check(f.Vm.Results.Single().State==SaleState.Unknown&&f.Vm.Results.Single().Message.Contains("판매 체결 관측")&&!f.Vm.HasApprovalHandoff&&f.Workspace.Store.Commands().Single()==command&&f.Server.Sends==1);
    }
    private static Task InvalidHistory()
    {
        var asset=new InventoryAsset(new(730,"2","101"),"Steam","item","item",1,false,true,true);var command=new SteamCommandRecord("synthetic","76561198000000001",new("line",asset,1,"KRW",new(115,100,5,10),DateTimeOffset.UtcNow),"sell",SaleState.Submitted,"900",DateTimeOffset.UtcNow);
        var valid=History(command);Check(SteamSaleHistory.Parse(valid).Length==1);
        foreach(var changed in new[]{valid.Replace("\"event_type\":3","\"event_type\":4"),valid.Replace("\"failed\":0","\"failed\":1"),valid.Replace("\"needs_rollback\":0","\"needs_rollback\":1"),valid.Replace("\"funds_returned\":0","\"funds_returned\":1"),valid.Replace("\"received_amount\":10000","\"received_amount\":9900"),valid.Replace("\"received_currencyid\":\"2016\"","\"received_currencyid\":\"2001\""),valid.Replace("\"amount\":\"1\"","\"amount\":\"2\"")})Check(SteamSaleHistory.Parse(changed).Length==0);
        var node=System.Text.Json.Nodes.JsonNode.Parse(valid)!;var events=node["events"]!.AsArray();events.Add(events[0]!.DeepClone());try{SteamSaleHistory.Parse(node.ToJsonString());throw new Exception("Duplicate accepted");}catch(SteamException){}return Task.CompletedTask;
    }
    private static async Task HistoryDenied()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();var command=f.Workspace!.Store.Commands().Single();f.Server.Rows.Clear();f.Server.MarketHistoryStatus=403;await f.Vm.RecoverAsync();await f.Vm.RecoverAsync();
        Check(f.Vm.RequestsBlocked&&f.Server.MarketHistoryReads==1&&f.Server.Sends==1&&f.Workspace.Store.ReadCommand(command.RequestKey)==command&&f.Vm.Results.All(x=>x.State!=SaleState.Sold));
    }
    private sealed class CaptureLog:IDiagnosticLog
    {public Guid SessionId{get;}=Guid.NewGuid();public List<DiagnosticRecord> Records{get;}=[];public void Write(DiagnosticRecord record)=>Records.Add(record);}
    private static string MergeHistory(IEnumerable<SteamCommandRecord> commands,bool offGridFirst)
    {
        var list=commands.ToArray();var entries=new List<System.Text.Json.Nodes.JsonNode>();
        if(offGridFirst)
        {
            var unrelated=list[0] with{ListingId="999",Line=list[0].Line with{Asset=list[0].Line.Asset with{Key=new(730,"2","999")}}};
            var node=System.Text.Json.Nodes.JsonNode.Parse(History(unrelated))!;node["listings"]!["999"]!["original_price"]=3141;node["purchases"]!["999_880"]!["received_amount"]=3141;entries.Add(node);
        }
        entries.AddRange(list.Select(x=>System.Text.Json.Nodes.JsonNode.Parse(History(x))!));
        var root=System.Text.Json.Nodes.JsonNode.Parse("{\"success\":true,\"events\":[],\"listings\":{},\"purchases\":{}}")!;
        foreach(var entry in entries){foreach(var ev in entry["events"]!.AsArray())root["events"]!.AsArray().Add(ev!.DeepClone());foreach(var prop in entry["listings"]!.AsObject())root["listings"]![prop.Key]=prop.Value!.DeepClone();foreach(var prop in entry["purchases"]!.AsObject())root["purchases"]![prop.Key]=prop.Value!.DeepClone();}return root.ToJsonString();
    }
    private static async Task OffGridBatch()
    {
        var log=new CaptureLog();using var use=DiagnosticHub.Use(log);using var f=new FifthStageTests.Fixture();f.Server.Free=3;f.Server.ManyAssets=3;await f.Login();await f.Vm.LoadAsync();f.Vm.SelectAll.Execute(null);await f.Vm.RefreshPricesAsync();f.Vm.LowerPriceOneStep=true;f.Vm.BuildReview();Check(f.Vm.ReviewRows.Count==3&&f.Vm.ReviewRows.All(x=>x.Quote.BuyerPays<x.BaselineQuote!.BuyerPays));f.EnableTrading();await f.Vm.SubmitAsync();Check(f.Server.Sends==3);var commands=f.Workspace!.Store.Commands();f.Server.Rows.Clear();f.Server.MarketHistoryOverride=MergeHistory(commands,true);await f.Vm.RecoverAsync();
        Check(f.Vm.Results.Count==3&&f.Vm.Results.All(x=>x.State==SaleState.Sold)&&f.Server.MarketHistoryReads==1&&f.Server.Sends==3);
        Check(log.Records.Count(x=>x.Reason=="HistorySaleMatched")==3);
        Check(!System.Text.Json.JsonSerializer.Serialize(log.Records).Contains("received_amount"));
    }
    private static async Task OffGridTarget()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();var command=f.Workspace!.Store.Commands().Single();var root=System.Text.Json.Nodes.JsonNode.Parse(History(command))!;var id=command.ListingId!;var wire=SteamContract.ToWire(command.Line.UnitQuote.SellerReceives,"KRW")-1;root["listings"]![id]!["original_price"]=wire;root["purchases"]![id+"_880"]!["received_amount"]=wire;
        Check(SteamSaleHistory.Parse(root.ToJsonString()).Length==1);f.Server.Rows.Clear();f.Server.MarketHistoryOverride=root.ToJsonString();await f.Vm.RecoverAsync();Check(f.Vm.Results.Single().State==SaleState.Unknown&&f.Server.Sends==1&&f.Workspace.Store.ReadCommand(command.RequestKey)==command);
    }
    private static async Task AmountDiagnosis()
    {
        var log=new CaptureLog();using var use=DiagnosticHub.Use(log);using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();var command=f.Workspace!.Store.Commands().Single();var node=System.Text.Json.Nodes.JsonNode.Parse(History(command))!;node["listings"]![command.ListingId!]!["original_price"]=9900;node["purchases"]![command.ListingId+"_880"]!["received_amount"]=9900;f.Server.Rows.Clear();f.Server.MarketHistoryOverride=node.ToJsonString();await f.Vm.RecoverAsync();Check(f.Vm.Results.Single().State==SaleState.Unknown&&log.Records.Any(x=>x.Reason=="HistoryAmountMismatch"&&x.Endpoint=="MarketHistory"&&x.Context.OperationId!=Guid.Empty));
    }
    private static Task RawUnits()
    {
        var asset=new InventoryAsset(new(730,"2","101"),"Steam","item","item",1,false,true,true);var command=new SteamCommandRecord("synthetic","76561198000000001",new("line",asset,1,"KRW",new(115,100,5,10),DateTimeOffset.UtcNow),"sell",SaleState.Submitted,"900",DateTimeOffset.UtcNow);
        var krw=History(command).Replace("10000","9007199254740901");Check(SteamSaleHistory.Parse(krw).Length==1);var usd=krw.Replace("2016","2001");Check(SteamSaleHistory.Parse(usd).Single().Currency=="USD");return Task.CompletedTask;
    }
    private static Task NullableFlags()
    {
        var asset=new InventoryAsset(new(730,"2","101"),"Steam","item","item",1,false,true,true);var command=new SteamCommandRecord("synthetic","76561198000000001",new("line",asset,1,"KRW",new(115,100,5,10),DateTimeOffset.UtcNow),"sell",SaleState.Submitted,"900",DateTimeOffset.UtcNow);
        var node=System.Text.Json.Nodes.JsonNode.Parse(History(command))!;var sale=node["purchases"]!["900_880"]!;sale["funds_held"]=null;sale["funds_revoked"]=null;Check(SteamSaleHistory.Parse(node.ToJsonString()).Length==1);sale["funds_revoked"]=1;Check(SteamSaleHistory.Parse(node.ToJsonString()).Length==0);return Task.CompletedTask;
    }
}
