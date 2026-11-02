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
            ("Balance cookies and catalogue query text are excluded from structured diagnostics",Privacy),
            ("Ambiguous local items never consume shared bids or enter either price total",()=>Sync(AmbiguousPrices)),
            ("Oversized row arithmetic excludes only the invalid row and keeps usable items",()=>Sync(RowOverflow)),
            ("Aggregate arithmetic overflow is safe in all bound summary getters",()=>Sync(TotalOverflow)),
            ("Bounded refresh reaches unseen items before stale previously queried items",RefreshProgress),
            ("All type lock equipped mode and search partitions preserve shared quantity and totals",()=>Sync(FilterMatrix)),
            ("Cancel double click and logout during late price failure cannot restore stale results",CancelPricePaths),
            ("Missing sell depth is unavailable rather than an observed empty market",MissingSellDepth),
            ("Shared bid exhaustion is distinct from an actually empty buy orderbook",()=>Sync(SharedDepthReason)),
            ("Orderbook field currency depth and consistency failures retain a safe read stage",()=>Sync(BookStages)),
            ("Failed value read stage reaches UI and correlated sanitized diagnostics",ValueReadTrace),
            ("Observed empty sell book remains different from an omitted sell field",()=>Sync(ObservedEmptySell)),
            ("Out-of-range currency IDs cannot wrap into KRW",()=>Sync(CurrencyWrap)),
            ("Known nonmarketable items are normal exclusions and never consume prices or requests",NonMarketable),
            ("Warm valuation cache avoids repeated Steam requests",WarmPrices),
            ("Updating one quote preserves unrelated rows without collection reset",()=>Sync(StableRows)),
            ("Price batch defers image metadata without bypassing the shared HTTP queue",PriorityBatch),
            ("Deferred image cancellation is bounded and server429 still stops further reads",PriorityCancellation)
        };int pass=0,fail=0;foreach(var(name,test)in tests)try{await test();pass++;Console.WriteLine("PASS "+name);}catch(Exception e){fail++;Console.WriteLine("FAIL "+name+": "+e);}return(pass,fail);
    }

    private static Dictionary<string,PriceSnapshot> Quotes(SteamWorkspaceViewModel vm)=>(Dictionary<string,PriceSnapshot>)typeof(SteamWorkspaceViewModel).GetField("tbhQuotes",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(vm)!;
    private static PriceSnapshot Book(long amount,int count=10)=>new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[new(amount,count)]){Asks=[new(amount,count)]};
    private static void AmbiguousPrices()
    {
        using var vm=new SteamWorkspaceViewModel();Quotes(vm)["Shared"]=Book(100,1);
        vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("a","Ambiguous","Shared",1,false,"unknown",Ambiguous:true),new("b","Valid","Shared",1,false,"stash")],0));
        foreach(var mode in new[]{0,1}){vm.TbhPriceMode=mode;Check(vm.TbhSaveRows.Single(x=>x.Item.Ambiguous).PricedTotal is null&&vm.TbhSaveRows.Single(x=>!x.Item.Ambiguous).PricedTotal==100&&vm.TbhValuationCoverage.Contains("1개 항목 금액만"));}
    }
    private static void RowOverflow()
    {
        using var vm=new SteamWorkspaceViewModel();Quotes(vm)["Huge"]=Book(long.MaxValue/2,10);Quotes(vm)["Good"]=Book(100,10);
        vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("a","Huge","Huge",10,false,"stash"),new("b","Good","Good",1,false,"stash")],0));
        foreach(var mode in new[]{0,1}){vm.TbhPriceMode=mode;Check(vm.TbhSaveRows.Count==2&&vm.TbhSaveRows.Single(x=>x.Item.ItemKey=="a").PricedTotal is null&&vm.TbhSaveRows.Single(x=>x.Item.ItemKey=="b").PricedTotal==100&&vm.TbhSaveRows.Any(x=>x.PriceText.Contains("범위")));_=vm.TbhSaveSummary;_=vm.TbhValuationText;}
    }
    private static void TotalOverflow()
    {
        using var vm=new SteamWorkspaceViewModel();Quotes(vm)["Huge"]=Book(long.MaxValue/2+1,10);
        vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("a","A","Huge",1,false,"stash"),new("b","B","Huge",1,false,"stash")],0));
        Check(vm.TbhValuationText.Contains("범위")&&vm.TbhSaveSummary.Contains("범위")&&vm.TbhSaveRows.All(x=>x.PricedTotal is not null));
    }
    private static async Task RefreshProgress()
    {
        using var f=new Fixture();await f.Login();var quotes=Quotes(f.Vm);for(int i=0;i<200;i++)quotes["Old"+i]=Book(100) with{CapturedAt=DateTimeOffset.UtcNow.AddMinutes(-6)};
        f.Vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,Enumerable.Range(0,201).Select(i=>new TbhOwnedItem(i.ToString(),i<200?"Old"+i:"Unseen",i<200?"Old"+i:"Unseen",1,false,"stash")).ToArray(),0));
        await f.Vm.RefreshTbhPrices.ExecuteAsync();Check(quotes.ContainsKey("Unseen")&&f.Route.PriceCalls==200&&f.Server.Sends==0);
    }

    private static void FilterMatrix()
    {
        using var vm=new SteamWorkspaceViewModel();Quotes(vm)["Shared"]=Book(100,5) with{Asks=[new(150,9)]};
        TbhOwnedItem[] items=[new("a","Gear","Shared",3,false,"stash",Kind:TbhItemKind.Equipment),new("b","Locked","Shared",4,true,"stash",Kind:TbhItemKind.Equipment),new("c","Material","Shared",2,false,"materials",Equipped:true,Kind:TbhItemKind.Material),new("d","Uncertain","Shared",100,false,"unknown",Ambiguous:true,Kind:TbhItemKind.Other),new("e","Unmapped",null,4,false,"stash")];
        vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,items,0));
        foreach(var locked in new[]{false,true})foreach(var equipped in new[]{false,true})foreach(var kind in Enumerable.Range(0,4))foreach(var mode in new[]{0,1})foreach(var search in new[]{"","Gear","Shared","no match","재료","%_[]","\t","Unmapped"})
        {
            vm.TbhIncludeLocked=locked;vm.TbhIncludeEquipped=equipped;vm.TbhKindFilter=kind;vm.TbhPriceMode=mode;vm.TbhSearch=search;
            var selected=items.Where(x=>(locked||!x.Locked)&&(equipped||!x.Equipped)&&(kind==0||kind==1&&x.Kind==TbhItemKind.Equipment||kind==2&&x.Kind==TbhItemKind.Material||kind==3&&x.Kind is TbhItemKind.Unknown or TbhItemKind.Other)&&(search.Length==0||x.Name.Contains(search)||x.MarketHash?.Contains(search)==true)).ToArray();
            var count=selected.Where(x=>!x.Ambiguous&&x.MarketHash is not null).Sum(x=>x.Quantity);long expected=mode==0?Math.Min(count,5)*100:count*150;
            Check(vm.TbhSaveRows.Count==selected.Length&&vm.TbhSaveRows.Sum(x=>x.PricedTotal??0)==expected&&vm.TbhSaveRows.Where(x=>x.Item.Ambiguous||x.Item.MarketHash is null).All(x=>x.PricedTotal is null));
            _=vm.TbhSaveSummary;_=vm.TbhValuationText;_=vm.TbhValuationCoverage;
        }
        var prior=vm.TbhKindFilter;vm.TbhKindFilter=-1;Check(vm.TbhKindFilter==prior);prior=vm.TbhPriceMode;vm.TbhPriceMode=99;Check(vm.TbhPriceMode==prior);
    }
    private static async Task CancelPricePaths()
    {
        foreach(var logout in new[]{false,true})
        {
            using var f=new Fixture();await f.Login();f.Vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("a","Good","Good",1,false,"stash")],0));
            var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            f.Route.PriceGate=async _=>{entered.TrySetResult();await release.Task;throw new HttpRequestException("synthetic late failure");};
            var pending=f.Vm.RefreshTbhPrices.ExecuteAsync();await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));Check(f.Vm.Busy&&!f.Vm.RefreshTbhPrices.CanExecute(null));await f.Vm.RefreshTbhPrices.ExecuteAsync();Check(f.Route.PriceCalls==1);
            if(logout)f.Vm.Logout.Execute(null);else f.Vm.CancelOperation.Execute(null);release.SetResult();await pending.WaitAsync(TimeSpan.FromSeconds(5));
            Check(!f.Vm.Busy&&f.Vm.Connected!=logout&&f.Route.PriceCalls==1&&Quotes(f.Vm).Count==0&&f.Vm.TbhSaveRows.All(x=>!x.PriceText.Contains("조회 실패"))&&f.Server.Sends==0);
        }
    }

    private static async Task MissingSellDepth()
    {
        using var f=new Fixture();await f.Login();f.Server.OrderbookOverride="{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[10000,1],\"cSellOrders\":3,\"amtMinSellOrder\":15000}}}";
        f.Vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("1","Good","Good",1,false,"stash")],0));await f.Vm.RefreshTbhPrices.ExecuteAsync();Check(f.Vm.TbhSaveRows.Single().PricedTotal==100);f.Vm.TbhPriceMode=1;
        Check(f.Vm.TbhSaveRows.Single().PricedTotal is null&&f.Vm.TbhSaveRows.Single().PriceText.Contains("필드")&&!f.Vm.TbhSaveRows.Single().PriceText.Contains("판매주문 없음"));
    }
    private static void SharedDepthReason()
    {
        using var vm=new SteamWorkspaceViewModel();Quotes(vm)["Shared"]=Book(100,1);vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("1","A","Shared",1,false,"stash"),new("2","B","Shared",1,false,"stash")],0));
        var exhausted=vm.TbhSaveRows.Single(x=>x.PricedTotal is null);Check(exhausted.PriceText.Contains("다른 항목")&&!exhausted.PriceText.Contains("현재 매수주문 잔량 없음"));
    }

    private static void CurrencyWrap()
    {bool rejected=false;try{SteamContract.OrderBook("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":4294967312,\"rgCompactBuyOrders\":[]}}}");}catch(SteamException error){rejected=error.ReadStage=="BookCurrency";}Check(rejected);}
    private static void BookStages()
    {
        foreach(var (body,expected) in new[]{("{}","BookEnvelope"),("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[10001,1]}}}","BookBuyAmount"),("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[10000,0]}}}","BookBuyQuantity"),("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[],\"rgCompactSellOrders\":[10001,1]}}}","BookSellAmount"),("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[],\"rgCompactSellOrders\":[10000,-1]}}}","BookSellQuantity"),("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":999}}}","BookCurrency"),("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[10000]}}}","BookBuyDepth"),("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[],\"cBuyOrders\":1}}}","BookBuyConsistency"),("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[],\"rgCompactSellOrders\":[10000]}}}","BookSellDepth"),("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[],\"rgCompactSellOrders\":[],\"amtMinSellOrder\":10000}}}","BookSellConsistency")})
        {SteamException? failure=null;try{SteamContract.OrderBook(body);}catch(SteamException e){failure=e;}Check(failure?.ReadStage==expected&&DiagnosticHub.AllowedToken(expected)==expected);}
    }
    private static async Task ValueReadTrace()
    {
        var log=new Capture();using var trace=DiagnosticHub.Use(log);using var f=new Fixture();await f.Login();f.Route.BadPrice=true;
        f.Vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("private-id-marker","Bad","Bad",1,false,"private-location-marker")],0));await f.Vm.RefreshTbhPrices.ExecuteAsync();
        var row=f.Vm.TbhSaveRows.Single();Check(row.ReadState==TbhValueReadState.RequestFailed&&row.ReadDetails.Contains("매수 잔량 정합성")&&f.Vm.TbhReadSummary.Contains("응답 검증 실패"));
        Check(log.Rows.Any(x=>x.Context.OperationId==f.Vm.LastOperationId&&x.Context.OperationItemId is not null&&x.Reason=="BookBuyConsistency")&&log.Rows.Any(x=>x.EventId==(int)DiagnosticEvent.TbhReadContract&&x.Reason=="RequestFailed"&&x.Count==1));
        var json=JsonSerializer.Serialize(log.Rows);Check(!json.Contains("private-id-marker")&&!json.Contains("private-location-marker")&&!json.Contains("synthetic-refresh-not-a-token"));
        f.Route.BadPrice=false;f.Server.OrderbookOverride="{\"data\":{\"success\":true,\"data\":{\"eCurrency\":4294967312,\"rgCompactBuyOrders\":[]}}}";await f.Vm.RefreshTbhPrices.ExecuteAsync();
        Check(log.Rows.Any(x=>x.Context.OperationId==f.Vm.LastOperationId&&x.EventId==(int)DiagnosticEvent.TbhSavePrices&&x.Error==DiagnosticError.Currency&&x.Reason=="BookCurrency"));
        f.Server.OrderbookOverride="{\"data\":{\"success\":true,\"data\":{\"eCurrency\":999,\"rgCompactBuyOrders\":[]}}}";await f.Vm.RefreshTbhPrices.ExecuteAsync();
        Check(log.Rows.Any(x=>x.Context.OperationId==f.Vm.LastOperationId&&x.EventId==(int)DiagnosticEvent.UiFailure&&x.Error==DiagnosticError.Currency&&x.Reason=="BookCurrency"));
    }
    private static void ObservedEmptySell()
    {
        var book=SteamContract.OrderBook("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[],\"rgCompactSellOrders\":[],\"cSellOrders\":0}}}");Check(book.SellDepthObserved&&book.Asks.IsEmpty);
        using var vm=new SteamWorkspaceViewModel();Quotes(vm)["Empty"]=new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],book.Bids){Asks=book.Asks,SellDepthObserved=book.SellDepthObserved};vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("a","Empty","Empty",1,false,"stash")],0));vm.TbhPriceMode=1;Check(vm.TbhSaveRows.Single().ReadState==TbhValueReadState.NoSellOrders);
    }

    private static async Task NonMarketable()
    {
        var log=new Capture();using var trace=DiagnosticHub.Use(log);using var f=new Fixture();await f.Login();
        Quotes(f.Vm)["Good"]=Book(100,1);
        f.Vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("1","Known relic","Good",1,false,"stash",CanExchangeMarketable:false,TradeEvidence:TbhTradeEvidence.UserConfirmedCategory),new("2","Definition false","Good",1,false,"stash",CanExchangeMarketable:false,TradeEvidence:TbhTradeEvidence.ItemDefinition),new("3","Ordinary","Good",1,false,"stash"),new("4","Event name alone",null,1,false,"stash")],0));
        Check(f.Vm.TbhSaveRows.Count(x=>x.PricedTotal is not null)==1&&f.Vm.TbhSaveRows.Count(x=>x.ReadState==TbhValueReadState.NonMarketableItem)==2&&f.Vm.TbhSaveRows.Single(x=>x.Item.ItemKey=="4").ReadState==TbhValueReadState.MarketMappingMissing);
        await f.Vm.ForceTbhPrices.ExecuteAsync();Check(f.Route.PriceCalls==1&&f.Vm.TbhValuationCoverage.Contains("거래불가 2개 정상 제외")&&f.Vm.TbhValuationCoverage.Contains("미확인 1"));
        f.Vm.TbhPriceMode=1;Check(f.Vm.TbhSaveRows.Where(x=>x.Item.CanExchangeMarketable==false).All(x=>x.PricedTotal is null&&x.PriceGroup.Contains("거래불가")&&x.ReadDetails.Contains("거래불가로 조회하지 않음"))&&log.Rows.Any(x=>x.Reason=="NonMarketableItem"&&x.Outcome==DiagnosticOutcome.Completed));
        f.Vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("1","Known relic","Good",1,false,"stash",CanExchangeMarketable:false)],0));var before=f.Route.PriceCalls;await f.Vm.RefreshTbhPrices.ExecuteAsync();Check(f.Route.PriceCalls==before&&f.Server.Sends==0);
    }

    private static async Task WarmPrices()
    {
        using var f=new Fixture();await f.Login();f.Vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,Enumerable.Range(0,100).Select(i=>new TbhOwnedItem(i.ToString(),"P"+i,"P"+i,1,false,"stash")).ToArray(),0));
        var timer=System.Diagnostics.Stopwatch.StartNew();await f.Vm.RefreshTbhPrices.ExecuteAsync();var coldMs=timer.ElapsedMilliseconds;var calls=f.Route.PriceCalls;timer.Restart();await f.Vm.RefreshTbhPrices.ExecuteAsync();var warmMs=timer.ElapsedMilliseconds;
        Check(calls==100&&f.Route.PriceCalls==calls);await f.Vm.ForceTbhPrices.ExecuteAsync();Check(f.Route.PriceCalls==calls+100);Console.WriteLine($"PERF SAMPLE synthetic100 products: cold={calls} requests/{coldMs}ms; warm=0 requests/{warmMs}ms; forced=100 requests; real network latency NOT measured");
    }
    private static void StableRows()
    {using var vm=new SteamWorkspaceViewModel();var snapshot=new TbhSaveSnapshot(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("a","A","A",1,false,"stash"),new("b","B","B",1,false,"stash")],0);vm.ApplyTbhSnapshot(snapshot);var row=vm.TbhSaveRows.Single(x=>x.Item.ItemKey=="b");int resets=0;vm.TbhSaveRows.CollectionChanged+=(_,e)=>{if(e.Action==System.Collections.Specialized.NotifyCollectionChangedAction.Reset)resets++;};Quotes(vm)["A"]=Book(100);vm.ApplyTbhSnapshot(snapshot);Check(resets==0&&ReferenceEquals(row,vm.TbhSaveRows.Single(x=>x.Item.ItemKey=="b")));vm.ApplyTbhSnapshot(snapshot with{Items=snapshot.Items.Select(x=>x with{}).ToArray()});Check(resets==0&&ReferenceEquals(row,vm.TbhSaveRows.Single(x=>x.Item.ItemKey=="b")));vm.ApplyTbhSnapshot(snapshot with{Items=[new("z","New","New",1,false,"stash")]});Check(resets==1);}

    private static async Task PriorityBatch()
    {
        using var f=new Fixture();await f.Login();using var first=f.Workspace!.PrioritizePriceReads();var second=f.Workspace.PrioritizePriceReads();
        var icon=f.Workspace.MarketImages.GetAsync("Synthetic stack");await Task.Yield();Check(f.Route.ImageCalls==0&&!icon.IsCompleted);await f.Workspace.Prices.RefreshNameAsync(TbhScope.AppId,"Good");Check(f.Route.PriceCalls==1&&f.Route.ImageCalls==0);second.Dispose();second.Dispose();Check(!icon.IsCompleted);first.Dispose();Check((await icon.WaitAsync(TimeSpan.FromSeconds(5)))?.Token=="synthetic_image"&&f.Route.ImageCalls==1);
    }
    private static async Task PriorityCancellation()
    {
        using var f=new Fixture();await f.Login();var lease=f.Workspace!.PrioritizePriceReads();using var cancel=new CancellationTokenSource();var icon=f.Workspace.MarketImages.GetAsync("Synthetic stack",cancel.Token);cancel.Cancel();bool cancelled=false;try{await icon;}catch(OperationCanceledException){cancelled=true;}Check(cancelled&&f.Route.ImageCalls==0);lease.Dispose();f.Route.ImageStatus=HttpStatusCode.TooManyRequests;try{await f.Workspace.MarketImages.GetAsync("Synthetic stack");}catch(SteamException e){Check(e.Code==SteamFailure.RateLimited);}var before=f.Route.PriceCalls;try{await f.Workspace.Prices.RefreshNameAsync(TbhScope.AppId,"Good");}catch(SteamException){}Check(f.Route.ImageCalls==1&&f.Route.PriceCalls==before&&f.Vm.RequestsBlocked);
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
        public Func<CancellationToken,Task<HttpResponseMessage>>? PriceGate;
        public int ImageCalls,PriceCalls;public HttpStatusCode ImageStatus=HttpStatusCode.OK;public bool BadPrice;public bool AllBad;public long BalanceWire=135000;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var path=request.RequestUri!.AbsolutePath;
            if(path=="/market/search/render/"){ImageCalls++;Check(!request.Headers.Contains("Cookie")&&request.Headers.Authorization is null);var response=new HttpResponseMessage(ImageStatus){Content=new StringContent(ImageJson())};response.Headers.Add("Set-Cookie","sessionid=public-cookie-must-not-be-imported; Path=/; Secure");return Task.FromResult(response);}
            if(path=="/market/")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("<script>g_bLoggedIn=true;g_rgWalletInfo={\"wallet_currency\":16,\"wallet_market_minimum\":100,\"wallet_currency_increment\":100,\"wallet_balance\":"+BalanceWire+"};</script>")});
            if(path=="/market/orderbook"){PriceCalls++;if(PriceGate is {} gate)return gate(token);if(BadPrice&&(AllBad||Uri.UnescapeDataString(request.RequestUri.Query).Contains("Bad",StringComparison.Ordinal)))return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"cBuyOrders\":1,\"rgCompactBuyOrders\":[],\"rgCompactSellOrders\":[]}}}")});}
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
