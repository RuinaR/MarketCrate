using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Reflection;
using MarketCrate.Core;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.Tests;
public static class TbhPriceDisplayTests
{
    private static void Check(bool ok){if(!ok)throw new Exception("TBH price/image/balance contract");}
    private static Task Sync(Action a){a();return Task.CompletedTask;}
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{
            ("TBH kind filters and exact variant labels retain unpriced rows without ranking",()=>Sync(Filters)),
            ("Wallet KRW USD zero invalid and missing balance never fabricate zero",()=>Sync(Wallet)),
            ("Sanitized public Steam orderbook fixture preserves actual nonempty buy and sell depth",()=>Sync(PublicBook)),
            ("Shared game sprite image alias never turns an unverified item into a priced product",()=>Sync(SpriteAlias)),
            ("Orderbook nonzero counters with empty depth cannot become no-orders",()=>Sync(Books)),
            ("Public image metadata requires exact hash app and safe CDN token",()=>Sync(ImageParser)),
            ("Public image lookup deduplicates and cannot replace authenticated cookies",Images),
            ("Anonymous image401 preserves session and independent wallet read",ImageUnauthorized),
            ("Image429 halts further Steam requests without retry",ImageLimited),
            ("One failed product does not skip the other products and explicit refresh repairs it",PartialPrices),
            ("Three schema failures stop a batch without fabricated success",RepeatedSchema),
            ("Wallet balance is a separate timestamped UI amount and clears on logout",Balance),
            ("Image metadata failure is cached until explicit retry without hammering",ImageFailureCache),
            ("Balance cookies and catalogue query text are excluded from structured diagnostics",Privacy)
        };int pass=0,fail=0;foreach(var(name,test)in tests)try{await test();pass++;Console.WriteLine("PASS "+name);}catch(Exception e){fail++;Console.WriteLine("FAIL "+name+": "+e);}return(pass,fail);
    }
    private static void Filters()
    {
        using var vm=new SteamWorkspaceViewModel();vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("1","Gear","Gear (Arcana) C",1,false,"창고",Kind:TbhItemKind.Equipment),new("2","Material","Mat",2,false,"재료창고",Kind:TbhItemKind.Material),new("3","Other",null,1,false,"가방",Kind:TbhItemKind.Other)],0));
        Check(vm.TbhSaveRows.Count==3&&vm.TbhSaveRows.All(x=>x.PriceGroup.Contains("순위 없음"))&&vm.TbhSaveRows.Any(x=>x.Name=="Gear (Arcana) C")&&vm.TbhValuationCoverage.Contains("미확인 3"));vm.TbhKindFilter=1;Check(vm.TbhSaveRows.Single().Item.Kind==TbhItemKind.Equipment);vm.TbhKindFilter=2;Check(vm.TbhSaveRows.Single().Item.Kind==TbhItemKind.Material);vm.TbhKindFilter=0;Check(vm.TbhSaveRows.Count==3);
    }
    private static void PublicBook(){var json=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","TbhPublicOrderbook-20261102.json"));var book=SteamContract.OrderBook(json);Check(book.Currency=="KRW"&&book.Bids.Length==25&&book.Asks.Length==74&&book.Bids[0].BuyerPays==330&&book.Asks[0].BuyerPays==410);}
    private static void SpriteAlias(){var plain=JsonSerializer.SerializeToUtf8Bytes(new{PlayerSaveData=new{value=JsonSerializer.Serialize(new{itemSaveDatas=new[]{new{UniqueId=1,ItemKey=601191,IsBlocked=false}},stashSaveDatas=new[]{new{Index=0,ItemUniqueId=1,Quantity=1,IsBlocked=false}},inventorySaveDatas=Array.Empty<object>()})}});var item=MarketCrate.Infrastructure.Tbh.TbhSaveReader.ParsePlain(plain,DateTimeOffset.UtcNow).Items.Single();Check(item.Kind==TbhItemKind.Equipment&&item.MarketHash is null&&item.ImageMarketHash is not null);var row=new TbhSaveRow(item,"",null,null,null,1,null);Check(row.IconHash is not null&&row.PriceGroup.Contains("순위 없음")&&row.ImageDescription.Contains("대표 아이콘"));}
    private static void Wallet()
    {
        static SteamWalletObservation Read(string balance,int currency=16)=>SteamFees.ObserveWallet("g_bLoggedIn=true;g_rgWalletInfo={\"wallet_currency\":"+currency+",\"wallet_balance\":"+balance+"};");
        Check(Money.FromWire(Read("135000").BalanceWire!.Value,"KRW").Minor==1350);Check(Read("0").BalanceWire==0);Check(Read("135001").BalanceInvalid&&Read("135001").BalanceWire is null);Check(Read("-1").BalanceInvalid);Check(Read("\"1234\"",1).BalanceWire==1234);Check(SteamFees.ObserveWallet("g_rgWalletInfo={\"wallet_currency\":16};").BalanceWire is null);
    }
    private static void Books()
    {
        foreach(var fields in new[]{"\"cBuyOrders\":1,","\"amtMaxBuyOrder\":10000,","\"cSellOrders\":1,"})
        {bool denied=false;try{SteamContract.OrderBook("{\"data\":{\"success\":true,\"data\":{"+fields+"\"eCurrency\":16,\"rgCompactBuyOrders\":[],\"rgCompactSellOrders\":[]}}}");}catch(SteamException){denied=true;}Check(denied);}
        var empty=SteamContract.OrderBook("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"cBuyOrders\":0,\"cSellOrders\":0,\"rgCompactBuyOrders\":[],\"rgCompactSellOrders\":[]}}}");Check(empty.Bids.IsEmpty&&empty.Asks.IsEmpty);
    }
    public static string ImageJson(string hash="Synthetic stack",int app=TbhScope.AppId,string icon="synthetic_image")=>JsonSerializer.Serialize(new{success=true,results=new[]{new{hash_name=hash,asset_description=new{appid=app,market_hash_name=hash,icon_url=icon}}}});
    private static void ImageParser(){Check(SteamMarketImages.Parse(ImageJson(),"Synthetic stack")?.Token=="synthetic_image");Check(SteamMarketImages.Parse(ImageJson("Different"),"Synthetic stack")is null);foreach(var json in new[]{ImageJson(app:730),ImageJson(icon:"https://evil.example/icon"),"{\"success\":true}"}){bool rejected=false;try{SteamMarketImages.Parse(json,"Synthetic stack");}catch(SteamException){rejected=true;}Check(rejected);}}
    public sealed class Router(HttpMessageHandler inner):DelegatingHandler(inner)
    {
        public int ImageCalls,PriceCalls;public HttpStatusCode ImageStatus=HttpStatusCode.OK;public bool BadPrice;public bool AllBad;public long BalanceWire=135000;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var path=request.RequestUri!.AbsolutePath;
            if(path=="/market/search/render/"){ImageCalls++;Check(!request.Headers.Contains("Cookie")&&request.Headers.Authorization is null);var response=new HttpResponseMessage(ImageStatus){Content=new StringContent(ImageJson())};response.Headers.Add("Set-Cookie","sessionid=public-cookie-must-not-be-imported; Path=/; Secure");return Task.FromResult(response);}
            if(path=="/market/")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("<script>g_bLoggedIn=true;g_rgWalletInfo={\"wallet_currency\":16,\"wallet_market_minimum\":100,\"wallet_currency_increment\":100,\"wallet_balance\":"+BalanceWire+"};</script>")});
            if(path=="/market/orderbook"){PriceCalls++;if(BadPrice&&(AllBad||Uri.UnescapeDataString(request.RequestUri.Query).Contains("Bad",StringComparison.Ordinal)))return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"cBuyOrders\":1,\"rgCompactBuyOrders\":[],\"rgCompactSellOrders\":[]}}}")});}
            return base.SendAsync(request,token);
        }
    }
    private sealed class Fixture:IDisposable
    {
        private readonly string folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","price23-"+Guid.NewGuid().ToString("N"));public SteamContractServer Server=new();public Router Route;public SteamWorkspaceViewModel Vm;public SteamExperimentalWorkspace? Workspace;
        public Fixture(){Route=new(Server.Connection());Vm=new(scope=>Workspace=new(folder,scope.AppId,scope.ContextId,scope.Name,contractHandler:Route,clock:Server.Clock),true){RiskAccepted=true,CredentialsAccepted=true};}
        public async Task Login(){await Vm.BeginAsync();Server.Clock.Advance();await Vm.CheckAsync();Check(Vm.Connected);}
        public void Dispose(){Vm.Dispose();Server.Dispose();if(Directory.Exists(folder))Directory.Delete(folder,true);}
    }
    private static async Task Images(){using var f=new Fixture();await f.Login();var property=typeof(SteamQrAuthentication).GetProperty("SessionId",BindingFlags.NonPublic|BindingFlags.Instance)!;var before=property.GetValue(f.Workspace!.Authentication);var rows=await Task.WhenAll(f.Workspace.MarketImages.GetAsync("Synthetic stack"),f.Workspace.MarketImages.GetAsync("Synthetic stack"));Check(rows.All(x=>x?.Token=="synthetic_image")&&f.Route.ImageCalls==1&&Equals(before,property.GetValue(f.Workspace.Authentication)));}
    private static async Task ImageUnauthorized(){using var f=new Fixture();await f.Login();f.Route.ImageStatus=HttpStatusCode.Unauthorized;try{await f.Workspace!.MarketImages.GetAsync("Synthetic stack");}catch(SteamException){}await f.Vm.LoadWallet.ExecuteAsync();Check(f.Vm.Connected&&!f.Vm.RequestsBlocked&&f.Vm.WalletBalanceLabel.Contains("1,350"));}
    private static async Task ImageLimited(){using var f=new Fixture();await f.Login();f.Route.ImageStatus=HttpStatusCode.TooManyRequests;try{await f.Workspace!.MarketImages.GetAsync("Synthetic stack");}catch(SteamException){}try{await f.Workspace!.MarketImages.GetAsync("Other");}catch(SteamException){}Check(f.Route.ImageCalls==1&&f.Vm.RequestsBlocked);}
    private static async Task PartialPrices(){using var f=new Fixture();await f.Login();f.Route.BadPrice=true;f.Vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("1","Bad","Bad",1,false,"창고"),new("2","Good","Good",1,false,"창고")],0));await f.Vm.RefreshTbhPrices.ExecuteAsync();Check(f.Route.PriceCalls==2&&f.Vm.TbhSaveRows.Count(x=>x.PricedTotal is not null)==1&&f.Vm.TbhSaveRows.Any(x=>x.PriceText.Contains("응답 불완전"))&&!f.Vm.RequestsBlocked);f.Route.BadPrice=false;await f.Vm.RefreshTbhPrices.ExecuteAsync();Check(f.Vm.TbhSaveRows.All(x=>x.PricedTotal is not null)&&f.Server.Sends==0&&f.Server.Cancels==0);}
    private static async Task RepeatedSchema(){using var f=new Fixture();await f.Login();f.Route.BadPrice=f.Route.AllBad=true;f.Vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,Enumerable.Range(0,5).Select(x=>new TbhOwnedItem(x.ToString(),"Bad"+x,"Bad"+x,1,false,"창고")).ToArray(),0));await f.Vm.RefreshTbhPrices.ExecuteAsync();Check(f.Route.PriceCalls==3&&f.Vm.TbhSaveRows.All(x=>x.PricedTotal is null));}
    private static async Task ImageFailureCache(){using var f=new Fixture();await f.Login();f.Route.ImageStatus=HttpStatusCode.InternalServerError;for(int i=0;i<3;i++)try{await f.Workspace!.MarketImages.GetAsync("Synthetic stack");}catch(SteamException){}Check(f.Route.ImageCalls==1);f.Route.ImageStatus=HttpStatusCode.OK;f.Workspace!.MarketImages.Invalidate("Synthetic stack");Check((await f.Workspace.MarketImages.GetAsync("Synthetic stack"))?.Token=="synthetic_image"&&f.Route.ImageCalls==2);}
    private sealed class Capture:IDiagnosticLog{public Guid SessionId{get;}=Guid.NewGuid();public List<DiagnosticRecord> Rows{get;}=[];public void Write(DiagnosticRecord row)=>Rows.Add(row);}
    private static async Task Privacy(){var log=new Capture();using var trace=DiagnosticHub.Use(log);using var f=new Fixture();await f.Login();f.Route.BalanceWire=987654300;await f.Vm.LoadWallet.ExecuteAsync();await f.Workspace!.MarketImages.GetAsync("Synthetic stack");var text=JsonSerializer.Serialize(log.Rows);Check(log.Rows.Any(x=>x.Endpoint=="MarketImage")&&!text.Contains("987654300")&&!text.Contains("public-cookie-must-not-be-imported")&&!text.Contains("Synthetic stack")&&!text.Contains("synthetic-refresh-not-a-token"));}
    private static async Task Balance(){using var f=new Fixture();await f.Login();Check(f.Vm.WalletBalanceLabel.Contains("미조회"));await f.Vm.LoadWallet.ExecuteAsync();Check(f.Vm.WalletBalanceLabel.Contains("1,350")&&f.Vm.WalletBalanceHint.Contains("마지막 조회"));f.Vm.Logout.Execute(null);Check(!f.Vm.WalletBalanceLabel.Contains("1,350")&&f.Vm.WalletBalanceLabel.Contains("연결 필요"));}
}
