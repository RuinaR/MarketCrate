using System.Collections.Immutable;
using System.Net;
using System.Text.Json;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
using Microsoft.Data.Sqlite;

internal static class FourthStageTests
{
    internal static async Task<(int Passed, int Failed)> RunAsync()
    {
        var tests = new (string Name, Func<Task> Run)[]
        {
            ("Steam default disabled sends zero", async () => { using var f = new Fixture(false); await Fails<SteamException>(() => f.Auth.BeginAsync()); Equal(0, f.Calls); Equal(Capability.IntegrationBlocked, f.Executor.Capability); }),
            ("Steam consent requires independent risks and reads", () => Sync(() => { var access = new SteamAccess(); Throws<InvalidOperationException>(() => access.Acknowledge(true, false, true, true)); access.Acknowledge(true,true,false,true); Check(!access.Trading); access.Revoke(); Check(!access.ResearchRiskAccepted); })),
            ("Steam endpoint origin and path pinning", async () => { using var f = new Fixture(); foreach (var url in new[] { "http://steamcommunity.com/market/sellitem/", "https://steamcommunity.com.evil.test/market/sellitem/", "https://user@steamcommunity.com/market/sellitem/", "https://steamcommunity.com:444/market/sellitem/", "https://store.steampowered.com/market/" }) { using var request = new HttpRequestMessage(HttpMethod.Get, url); await Fails<SteamException>(() => f.Http.SendAsync(request, default)); } Equal(0,f.Calls); }),
            ("Steam QR manual interval and session-only credentials", async () => { using var f = new Fixture(); await f.Auth.BeginAsync(); Check(!await f.Auth.CheckAsync()); Equal(1,f.Calls); f.Clock.Advance(); Check(await f.Auth.CheckAsync()); Check(f.Auth.Connected); Check(!f.Auth.ToString().Contains("fixture-refresh")); Check(f.Auth.SessionId is not null); f.Auth.Dispose(); Check(!f.Auth.Connected); Equal(0,f.Http.Cookies.GetCookies(new("https://steamcommunity.com/")).Count); }),
            ("Steam QR transfer host injection rejected", async () => { using var f = new Fixture(); f.BadTransfer=true; await Fails<SteamException>(() => f.Connect()); Equal(3,f.Calls); Check(!f.Auth.Connected); }),
            ("Steam auth expiry 403 429 redirect HTML latch", async () => { foreach (var status in new[] { 401,403,429,302,200 }) { using var f = new Fixture(); f.Status=status; f.Raw=status==200 ? "<html>captcha</html>" : "{}"; using var req = new HttpRequestMessage(HttpMethod.Get,"https://steamcommunity.com/market/orderbook"); await Fails<SteamException>(() => f.Http.SendAsync(req,default)); using var again = new HttpRequestMessage(HttpMethod.Get,"https://steamcommunity.com/market/orderbook"); await Fails<SteamException>(() => f.Http.SendAsync(again,default)); Equal(1,f.Calls); Check(f.Http.Halted is not null); } }),
            ("Steam current orderbook integer currency and quantities", () => Sync(() => { var b=SteamContract.OrderBook(Book(16,[100000,3,98000,4,95000,3])); Equal("KRW",b.Currency); Equal("3,4,3",string.Join(',',Pricing.Allocate(10,b.Bids).Lines.Select(x=>x.Quantity))); Equal(2,Pricing.Allocate(12,b.Bids).UnpricedQuantity); Equal(11500L,SteamContract.ToWire(115,"KRW")); Throws<SteamException>(()=>SteamContract.FromWire(11501,"KRW")); })),
            ("Steam legacy histogram and changed book fail closed", () => Sync(() => { foreach(var json in new[] { "{\"success\":104}",Book(1,[100,1,101,2]),Book(1,[100,1,90]),Book(1,[100,0]),Book(2,[100,1]) }) Throws<SteamException>(()=>SteamContract.OrderBook(json)); })),
            ("Steam inventory flags strict identity missing description", () => Sync(() => { var p=SteamContract.Inventory(Inventory(),730,"2","Fixture game"); var a=p.Assets.Single(); Check(a.Marketable && a.Tradable); Equal(1000,p.PublisherFees![a.Key]); Throws<SteamException>(()=>SteamContract.Inventory(Inventory(),440,"2","Fixture game")); Throws<SteamException>(()=>SteamContract.Inventory(Inventory().Replace("\"assetid\":\"101\"","\"assetid\":1.01"),730,"2","Fixture game")); })),
            ("Steam repeated inventory cursor rejects incomplete", async () => { using var f=new Fixture(); await f.Connect(); f.InventoryBody=Inventory(true); await Fails<SteamException>(()=>f.Inventory.GetAsync(default)); Check(f.Http.Halted is not null); }),
            ("Steam median history distinct from actual trade VWAP", () => Sync(() => { var p=SteamContract.History("{\"success\":true,\"prices\":[[\"Oct 28 2026 01: +0\",1.15,\"3\"]]}"); Equal(1.15m,p.Single().MedianMajor); Equal(3,p.Single().Volume); Throws<SteamException>(()=>SteamContract.History("{\"success\":true,\"prices\":[[\"invalid\",1,\"3\"]]}")); })),
            ("Steam response accepted pending and unknown", () => Sync(() => { Equal(SaleState.Submitted,SteamContract.Sell("{\"success\":true}").State); Equal(SaleState.AwaitingSteamGuard,SteamContract.Sell("{\"success\":true,\"needs_email_confirmation\":true}").State); Equal(SaleState.Unknown,SteamContract.Sell("{\"success\":false,\"message\":\"fixture\"}").State); Throws<SteamException>(()=>SteamContract.Sell("{\"success\":true,\"needs_mobile_confirmation\":\"yes\"}")); Check(SteamContract.CancellationAccepted("[]")); Check(!SteamContract.CancellationAccepted("{}")); })),
            ("Steam exact fee integer gaps reject Demo approximation", () => Sync(() => { for(long i=1;i<10000;i++){var q=SteamFeeMath.FromSeller(i,new());Equal(i+q.SteamFee+q.GameFee,q.BuyerPays);} var line=Line(); SteamFeeMath.ValidateExact(line,new()); Throws<SteamException>(()=>SteamFeeMath.ValidateExact(line with { UnitQuote=Pricing.FromBuyer(22,new()) },new())); Throws<SteamException>(()=>SteamFeeMath.ValidateExact(line with { Currency="KRW" },new())); })),
            ("Steam wallet unknown currency or policy blocks", () => Sync(() => { Equal(500,SteamListingExecutor.Wallet(Wallet).SteamBasisPoints); Throws<SteamException>(()=>SteamListingExecutor.Wallet(Wallet.Replace("\"wallet_currency\":1","\"wallet_currency\":16"))); Throws<SteamException>(()=>SteamListingExecutor.Wallet("<html>login</html>")); Throws<SteamException>(()=>SteamListingExecutor.Wallet(Wallet.Replace("\"wallet_fee\":1","\"wallet_fee\":0"))); })),
            ("Steam one approval real adapter emits seller net and pending", async () => { using var f=new Fixture(); await f.Connect(); var batch=Batch(); var coordinator=new SaleBatchCoordinator(f.Executor,f.Store); var result=await coordinator.ExecuteAsync(batch); Equal(1,f.Sells); Equal(SaleState.AwaitingSteamGuard,result.Results.Single().State); Check(f.SellForm!.Contains("price=100") && !f.SellForm.Contains("price=115")); await coordinator.ExecuteAsync(batch); Equal(1,f.Sells); f.Listed=true; Equal(SaleState.Listed,(await coordinator.RecoverAsync())!.Results.Single().State); }),
            ("Steam timeout unknown restart no automatic submission", async () => { using var f=new Fixture(); await f.Connect(); f.TimeoutSell=true; var batch=Batch(); var coordinator=new SaleBatchCoordinator(f.Executor,f.Store); Equal(SaleState.Unknown,(await coordinator.ExecuteAsync(batch)).Results[0].State); Equal(1,f.Sells); await Fails<InvalidOperationException>(()=>coordinator.ExecuteAsync(batch)); Equal(1,f.Sells); f.Store.Dispose(); using var reopened=new Fixture(directory:f.Directory); await reopened.Connect(); var recovered=await new SaleBatchCoordinator(reopened.Executor,reopened.Store).RecoverAsync(); Equal(SaleState.Unknown,recovered!.Results[0].State); Equal(1,f.Sells); Equal(0,reopened.Sells); }),
            ("Steam missing listingid remains uncertain after acceptance", async () => { using var f=new Fixture(); await f.Connect(); f.SellBody="{\"success\":true}"; var batch=Batch(); var c=new SaleBatchCoordinator(f.Executor,f.Store); await c.ExecuteAsync(batch); f.Listed=true; Equal(SaleState.Unknown,(await c.RecoverAsync())!.Results[0].State); Equal(1,f.Sells); }),
            ("Steam final asset and price mismatch zero writes", async () => { foreach(var mode in new[]{0,1,2}) { using var f=new Fixture(); await f.Connect(); var batch=Batch(); if(mode==0) f.InventoryBody=Inventory().Replace("\"marketable\":1","\"marketable\":0"); if(mode==1) f.BookBody=Book(1,[1000,10]); if(mode==2) batch=batch with { Lines=[Line() with { Quantity=2 }] }; await Fails<Exception>(()=>new SaleBatchCoordinator(f.Executor,f.Store).ExecuteAsync(batch)); Equal(0,f.Sells); } }),
            ("Steam cancel submitted is not cancelled reprice blocked", async () => { using var f=new Fixture(); await f.Connect(); f.Listed=true; var listing=(await f.Orders.RefreshAsync()).Single(); var r=await f.Executor.CancelAsync(listing,"cancel-1"); Equal(SaleState.Submitted,r.State); Equal(1,f.Cancels); await f.Executor.CancelAsync(listing,"cancel-1"); await f.Executor.CancelAsync(listing,"cancel-2"); Equal(1,f.Cancels); Equal(0,f.Sells); }),
            ("Steam cancel timeout preserves intent across reopen", async () => { using var f=new Fixture(); await f.Connect(); f.Listed=true; f.TimeoutCancel=true; var listing=(await f.Orders.RefreshAsync()).Single(); Equal(SaleState.Unknown,(await f.Executor.CancelAsync(listing,"cancel-1")).State); f.Store.Dispose(); using var reopened=new SteamStateStore(f.Directory); Equal(SaleState.Unknown,reopened.ReadCommand("cancel-1")!.State); Equal(1,f.Cancels); }),
            ("Steam incomplete listing buckets reject absence proof", () => Sync(() => { Throws<SteamException>(()=>SteamContract.Listings("{\"success\":true,\"total_count\":0,\"listings\":[]}")); })),
            ("Steam standalone store exclusive future schema preserves file", () => Sync(() => { using var f=new Fixture(); Throws<IOException>(()=>{using var duplicate=new SteamStateStore(f.Directory);}); f.Store.Dispose(); using(var db=new SqliteConnection($"Data Source={Path.Combine(f.Directory,"steam-state.sqlite")};Pooling=False")){db.Open();using var command=db.CreateCommand();command.CommandText="PRAGMA user_version=99";command.ExecuteNonQuery();} var before=File.ReadAllBytes(Path.Combine(f.Directory,"steam-state.sqlite")); Throws<InvalidDataException>(()=>{using var future=new SteamStateStore(f.Directory);}); Check(before.SequenceEqual(File.ReadAllBytes(Path.Combine(f.Directory,"steam-state.sqlite")))); })),
            ("Steam one approval multiple real HTTP commands keep all prices", async () => { using var f=new Fixture(); await f.Connect(); var first=Line();var second=first with { LineId="1",Asset=first.Asset with {Key=new(730,"2","102")}, UnitQuote=Pricing.FromSeller(101,new()) }; var node=System.Text.Json.Nodes.JsonNode.Parse(FourthStageTests.Inventory())!;var asset=node["assets"]![0]!.DeepClone();asset["assetid"]="102";node["assets"]!.AsArray().Add(asset);node["total_inventory_count"]=2;f.InventoryBody=node.ToJsonString(); var batch=Batch() with {Lines=[first,second]};var c=new SaleBatchCoordinator(f.Executor,f.Store);var result=await c.ExecuteAsync(batch);Equal(2,f.Sells);Equal(2,result.Results.Count(x=>x.State==SaleState.AwaitingSteamGuard));Check(f.SellForms[0].Contains("price=100") && f.SellForms[1].Contains("price=101")); await Fails<SteamException>(()=>f.Executor.SubmitAsync(first,"different-unapproved-key",default));Equal(2,f.Sells); }),
            ("Steam transport serializes caller requests", async () => { var access=new SteamAccess();access.Acknowledge(true,true,true,false);int active=0,peak=0;using var http=new SteamHttp(access,new Handler(async _=>{active++;peak=Math.Max(peak,active);await Task.Delay(10);active--;return new(HttpStatusCode.OK){Content=new StringContent("{}")};})); async Task Read(){using var request=new HttpRequestMessage(HttpMethod.Get,"https://steamcommunity.com/market/orderbook");await http.SendAsync(request,default);} await Task.WhenAll(Read(),Read(),Read());Equal(1,peak); }),
            ("Steam cancel reconciliation absence never permits repricing", async () => { using var f=new Fixture();await f.Connect();f.Listed=true;var row=(await f.Orders.RefreshAsync()).Single();await f.Executor.CancelAsync(row,"cancel-1");Equal(SaleState.Submitted,(await f.Executor.ReconcileCancellationAsync("cancel-1")).State);f.Listed=false;Equal(SaleState.Unknown,(await f.Executor.ReconcileCancellationAsync("cancel-1")).State);Equal(0,f.Sells); }),
            ("Steam command store excludes authentication material", async () => { using var f=new Fixture();await f.Connect();await new SaleBatchCoordinator(f.Executor,f.Store).ExecuteAsync(Batch());f.Store.Dispose();var bytes=System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(f.Directory,"steam-state.sqlite")));foreach(var marker in new[]{"fixture-refresh","fixture-auth","fixture-nonce","steamLoginSecure","sessionid"})Check(!bytes.Contains(marker)); }),
            ("Steam journal 128 result deltas reopen within bounded payload", async () =>
            {
                using var f=new Fixture();var line=Line();var batch=Batch() with { Lines=Enumerable.Range(0,128).Select(i=>line with { LineId=i.ToString(), Asset=line.Asset with { Key=new(730,"2",(i+1).ToString()) } }).ToImmutableArray() };
                var record=new BatchRecord(1,batch,batch.Lines.Select(x=>new LineResult(x.LineId,SaleState.Ready,"")).ToImmutableArray());await f.Store.SaveAsync(record,default);
                for(var i=0;i<record.Results.Length;i++) { record=record with { Results=record.Results.SetItem(i,new(i.ToString(),SaleState.Submitting,"intent")) };await f.Store.SaveAsync(record,default);record=record with { Results=record.Results.SetItem(i,new(i.ToString(),SaleState.Unknown,"no response")) };await f.Store.SaveAsync(record,default); }
                f.Store.Dispose();using var reopened=new SteamStateStore(f.Directory);Equal(128,(await reopened.LoadAsync(default))!.Results.Count(x=>x.State==SaleState.Unknown));
                using var db=new SqliteConnection($"Data Source={Path.Combine(f.Directory,"steam-state.sqlite")};Pooling=False");db.Open();using var command=db.CreateCommand();command.CommandText="SELECT SUM(length(value)) FROM state";Check(Convert.ToInt64(command.ExecuteScalar())<1024*1024);
            }),
            ("Steam journal second-row failure rolls back all deltas", async () =>
            {
                using var f=new Fixture();var line=Line();var batch=Batch() with { Lines=[line,line with { LineId="1",Asset=line.Asset with { Key=new(730,"2","102") } }] };
                var record=new BatchRecord(1,batch,[new("0",SaleState.Ready,""),new("1",SaleState.Ready,"")]);await f.Store.SaveAsync(record,default);
                using(var db=new SqliteConnection($"Data Source={Path.Combine(f.Directory,"steam-state.sqlite")};Pooling=False")){db.Open();using var trigger=db.CreateCommand();trigger.CommandText="CREATE TRIGGER fail_second BEFORE INSERT ON state WHEN NEW.kind='result' AND NEW.id='1' BEGIN SELECT RAISE(ABORT,'fixture'); END";trigger.ExecuteNonQuery();}
                await Fails<SqliteException>(()=>f.Store.SaveAsync(record with { Results=[new("0",SaleState.Submitting,""),new("1",SaleState.Submitting,"")] },default));f.Store.Dispose();using var reopened=new SteamStateStore(f.Directory);Check((await reopened.LoadAsync(default))!.Results.All(x=>x.State==SaleState.Ready));
            }),
            ("Steam 10000 synthetic inventory parser bounds", () => Sync(() => { var root=JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(Inventory())!; var asset=JsonDocument.Parse(Inventory()).RootElement.GetProperty("assets")[0]; var rows=Enumerable.Range(1,10000).Select(i=>new{appid=730,contextid="2",assetid=i.ToString(),classid="1",instanceid="0",amount="1"}); var json=JsonSerializer.Serialize(new{success=1,total_inventory_count=10000,assets=rows,descriptions=root["descriptions"]}); Equal(10000,SteamContract.Inventory(json,730,"2","Fixture game").Assets.Length); }))
        };
        int passed=0,failed=0;
        foreach(var test in tests) {try{await test.Run();passed++;Console.WriteLine("PASS "+test.Name);}catch(Exception error){failed++;Console.WriteLine($"FAIL {test.Name}: {error}");}}
        return(passed,failed);
    }
    private static SaleLineSnapshot Line() => new("0",new(new(730,"2","101"),"Fixture game","Fixture item","Fixture item",1,false,true,true),1,"USD",Pricing.FromSeller(100,new()),DateTimeOffset.UtcNow);
    private static SaleBatchSnapshot Batch() => new(Guid.NewGuid(),"76561198000000001",DateTimeOffset.UtcNow,[Line()]);
    private const string Wallet="<script>var g_rgWalletInfo = {\"wallet_fee\":1,\"wallet_currency\":1,\"wallet_fee_percent\":0.05,\"wallet_fee_minimum\":1,\"wallet_fee_base\":0};</script>";
    private static string Inventory(bool more=false) => "{\"success\":1,\"total_inventory_count\":1,\"assets\":[{\"appid\":730,\"contextid\":\"2\",\"assetid\":\"101\",\"classid\":\"1\",\"instanceid\":\"0\",\"amount\":\"1\"}],\"descriptions\":[{\"appid\":730,\"classid\":\"1\",\"instanceid\":\"0\",\"name\":\"Fixture item\",\"market_hash_name\":\"Fixture item\",\"marketable\":1,\"tradable\":1,\"market_fee\":\"0.1\"}]"+(more?",\"more_items\":1,\"last_assetid\":\"101\"":"")+"}";
    private static string Book(int currency,long[] pairs)=>JsonSerializer.Serialize(new{data=new{success=true,data=new{eCurrency=currency,rgCompactBuyOrders=pairs}}});
    private sealed class Clock:TimeProvider {private DateTimeOffset now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>now;public void Advance()=>now=now.AddSeconds(2);}
    private sealed class Handler(Func<HttpRequestMessage,Task<HttpResponseMessage>> send):HttpMessageHandler {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>send(request);}
    private sealed class Fixture:IDisposable
    {
        public string Directory {get;}
        public SteamHttp Http {get;} public SteamQrAuthentication Auth {get;} public SteamStateStore Store {get;} public SteamInventoryProvider Inventory {get;} public SteamOrdersProvider Orders {get;} public SteamListingExecutor Executor {get;} public Clock Clock {get;}=new();
        public int Calls,Sells,Cancels;public int? Status;public string? Raw,SellForm,InventoryBody,BookBody; public bool BadTransfer,TimeoutSell,TimeoutCancel,Listed;
        public List<string> SellForms {get;}=[];
        public string SellBody="{\"success\":true,\"needs_mobile_confirmation\":true,\"listingid\":\"501\"}";
        public Fixture(bool enable=true,string? directory=null)
        {
            Directory=directory??Path.Combine(System.IO.Directory.GetCurrentDirectory(),"runtime","steam-tests-"+Guid.NewGuid().ToString("N"));
            var access=new SteamAccess();if(enable)access.Acknowledge(true,true,true,true);
            Http=new(access,new Handler(Respond));Auth=new(Http,Clock);Store=new(Directory);Inventory=new(Http,Auth,730,"2","Fixture game");Orders=new(Http,Auth);Executor=new(Http,Auth,Store,Inventory,new(Http,Auth),Orders);
        }
        public async Task Connect(){await Auth.BeginAsync();Clock.Advance();Check(await Auth.CheckAsync());}
        private async Task<HttpResponseMessage> Respond(HttpRequestMessage request)
        {
            Calls++;var path=request.RequestUri!.AbsolutePath;string body;
            if(Status is {} status)return new((HttpStatusCode)status){Content=new StringContent(Raw??"{}")};
            if(path.Contains("BeginAuthSession"))body="{\"response\":{\"client_id\":\"123\",\"request_id\":\"AQID\",\"challenge_url\":\"https://s.team/q/1/2\",\"interval\":1}}";
            else if(path.Contains("PollAuthSession"))body="{\"response\":{\"refresh_token\":\"fixture-refresh\"}}";
            else if(path.Contains("finalizelogin"))body=JsonSerializer.Serialize(new{steamID="76561198000000001",transfer_info=new[]{new{url=BadTransfer?"https://evil.test/login/settoken":"https://steamcommunity.com/login/settoken",@params=new{nonce="fixture-nonce",auth="fixture-auth"}}}});
            else if(path=="/login/settoken"){Http.Cookies.Add(new Cookie("steamLoginSecure","fixture-only","/","steamcommunity.com"){Secure=true});body="{\"result\":1}";}
            else if(path.StartsWith("/inventory/"))body=InventoryBody??FourthStageTests.Inventory();
            else if(path=="/market/orderbook")body=BookBody??Book(1,[115,10]);
            else if(path=="/market/")body=Wallet;
            else if(path=="/market/sellitem/"){Sells++;SellForm=await request.Content!.ReadAsStringAsync();SellForms.Add(SellForm);if(TimeoutSell)throw new HttpRequestException("synthetic timeout");body=Sells>1?SellBody.Replace("501",(500+Sells).ToString()):SellBody;}
            else if(path.StartsWith("/market/removelisting/")){Cancels++;if(TimeoutCancel)throw new HttpRequestException("synthetic timeout");body="[]";}
            else if(path=="/market/mylistings/render/")
            {
                var assetRows = new Dictionary<string,object> { ["101"] = new { market_hash_name="Fixture item" } };
                var contexts = new Dictionary<string,object> { ["2"] = assetRows };
                var assets = new Dictionary<string,object> { ["730"] = contexts };
                body=JsonSerializer.Serialize(new{success=true,total_count=Listed?1:0,listings=Listed?new[]{new{listingid="501",price=100,fee=15,currencyid=2001,asset=new{appid=730,contextid="2",id="101",amount="1"}}}:[],listings_to_confirm=Array.Empty<object>(),listings_on_hold=Array.Empty<object>(),assets});
            }
            else throw new Exception("Unexpected synthetic route");
            return new(HttpStatusCode.OK){Content=new StringContent(body)};
        }
        public void Dispose(){Auth.Dispose();Http.Dispose();Store.Dispose();if(System.IO.Directory.Exists(Directory))System.IO.Directory.Delete(Directory,true);}
    }
    private static Task Sync(Action action){action();return Task.CompletedTask;}
    private static void Check(bool condition){if(!condition)throw new Exception("Assertion failed");}
    private static void Equal<T>(T expected,T actual){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"Expected {expected}, actual {actual}");}
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
    private static async Task Fails<T>(Func<Task> action)where T:Exception{try{await action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
}
