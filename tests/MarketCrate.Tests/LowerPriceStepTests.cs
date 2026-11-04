using MarketCrate.App.ViewModels;
using MarketCrate.Core;
namespace MarketCrate.Tests;
public static class LowerPriceStepTests
{
    private static void Check(bool ok){if(!ok)throw new Exception("Lower valid price step contract");}
    private static void Reject(Action action){try{action();}catch(ArgumentException){return;}throw new Exception("Minimum must not fall back to an undiscounted quote");}
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{
            ("Fixed approval still blocks fee currency and expired review changes",FixedPriceGuards),
            ("Legacy approval preserves digest checking and does not gain new fixed-price policy",LegacyPriceGuard),
            ("KRW70 to60 valid increment fees and net50 to40",()=>Sync(()=>{var s=new SteamFeeSchedule("KRW",1000,1000,new(50000),new(100000));Check(s.FromBuyer(70)==new FeeQuote(70,50,10,10));Check(s.PreviousValidBuyerPrice(70)==new FeeQuote(60,40,10,10));Reject(()=>s.PreviousValidBuyerPrice(30));})),
            ("USD fee gap23 to21 is one reachable buyer step",()=>Sync(()=>{var s=new SteamFeeSchedule("USD",1,1,new(50000),new(100000));Check(s.PreviousValidBuyerPrice(23).BuyerPays==21);Reject(()=>s.PreviousValidBuyerPrice(22));Reject(()=>s.PreviousValidBuyerPrice(3));})),
            ("Nearest lower valid price property across KRW USD zero and game fees",()=>Sync(Properties)),
            ("Review displays per-unit fees and quantity reduction and unavailable minimum",()=>Sync(Comparison)),
            ("Both price modes show exact buyer reduction and zero reduction at minimum",()=>Sync(ReductionLabels)),
            ("Opt-in default OFF price allocation preserved and one approval sends all changed prices",Batch),
            ("Minimum is retained visibly while below-minimum manual input remains rejected",()=>Sync(Minimum)),
            ("Last shortage row accepts a verified manual price and joins the same batch",ManualShortage),
            ("Verified empty orderbook supports an explicit manual limit listing",ManualEmptyBook),
            ("Explicit manual price survives later bid changes without automatic repricing",ManualBookChanged),
            ("Three minimum-price assets submit once after one unchanged final approval",MinimumBatch),
            ("Changed orderbook preserves all approved buyer prices without duplicate sends",StaleThenFresh),
            ("Manual edits refresh comparison and closed review requires a new plan",Manual),
            ("Changed asset state rejects discounted approval without submission",ChangedBook),
            ("Lower option survives neither logout nor restored connection",Reset),
            ("Listing batch lower proposals are compared before any cancellation",Listing),
            ("KRW30 minimum lower reprice preserves current listing without cancellation",MinimumReprice),
            ("Sell side validates ascending integer levels best price and malformed fields",()=>Sync(SellParser)),
            ("Lowest sell fee minimum clamps and USD fee gaps use one reachable lower price",()=>Sync(SellMoney)),
            ("Inventory lowest sell supports all assets without consuming buy quantity",()=>SellInventory(false)),
            ("Inventory lowest sell lower option sends exact approved prices once",()=>SellInventory(true)),
            ("Lowest sell changes after review retain exact approved price",SellChanged),
            ("Missing sell side is not replaced by buy price or zero",SellMissing),
            ("Listing lowest sell uses ask reference through cancel return and replacement",SellReprice),
            ("Lowest sell at KRW30 retains the current minimum without cancellation",SellMinimum),
            ("Lowest ask changes retain approved reprice through cancellation and replacement",SellRepriceChanged),
            ("Same best ask with changed quantities preserves an explicit price approval",SellQuantityChanged),
            ("Own sell quantities are subtracted by product price and state before choosing the lowest ask",()=>Sync(OwnSellExcluded)),
            ("Inventory lowest reference skips an existing own cheaper listing",InventoryOwnSell)
        };
        int passed=0,failed=0;foreach(var(name,test)in tests)try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}return(passed,failed);
    }
    private static async Task FixedPriceGuards()
    {
        foreach(var change in new[]{"currency","fee","expiry"})
        {
            using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();var line=f.Vm.ReviewRows.Single().Snapshot(0);
            if(change=="currency")f.Server.Currency="USD";else if(change=="fee")f.Server.KrwIncrement=1000;else line=line with{MarketTime=DateTimeOffset.UtcNow.AddMinutes(-6)};
            bool rejected=false;try{await f.Workspace!.Executor.ValidateBatchAsync(new(Guid.NewGuid(),f.Workspace.Authentication.SteamId!,DateTimeOffset.UtcNow,[line]),default);}catch(MarketCrate.Infrastructure.Steam.SteamException){rejected=true;}catch(InvalidOperationException){rejected=true;}catch(ArgumentException){rejected=true;}
            Check(rejected&&f.Server.Sends==0&&f.Workspace!.Store.Commands().Count==0);
        }
    }
    private static async Task LegacyPriceGuard()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();var line=f.Vm.ReviewRows.Single().Snapshot(0) with{FixedApprovedPrice=false};
        var json=System.Text.Json.JsonSerializer.Serialize(line).Replace(",\"FixedApprovedPrice\":false","");var old=System.Text.Json.JsonSerializer.Deserialize<SaleLineSnapshot>(json)!;Check(!old.FixedApprovedPrice);
        f.Server.OrderbookOverride=Book("7000,2");bool rejected=false;try{await f.Workspace!.Executor.ValidateBatchAsync(new(Guid.NewGuid(),f.Workspace.Authentication.SteamId!,DateTimeOffset.UtcNow,[old]),default);}catch(MarketCrate.Infrastructure.Steam.SteamException){rejected=true;}Check(rejected&&f.Server.Sends==0);
    }
    private static Task Sync(Action action){action();return Task.CompletedTask;}
    private static void Properties()
    {
        foreach(var s in new[]{new SteamFeeSchedule("USD",1,1,new(50000),new(100000)),new("KRW",1000,1000,new(50000),new(100000)),new("KRW",100,100,new(50000),new(0)),new("USD",1,1,new(0),new(0))})
        {
            var unit=Money.FromWire(s.IncrementWire,s.Currency).Minor;var minimum=Money.FromWire(s.MinimumWire,s.Currency).Minor;
            var quotes=Enumerable.Range(0,250).Select(i=>s.FromSeller(minimum+i*unit)).ToArray();Reject(()=>s.PreviousValidBuyerPrice(quotes[0].BuyerPays));Check(s.LowerOrMinimumBuyerPrice(quotes[0].BuyerPays)==quotes[0]);
            for(int i=1;i<quotes.Length;i++)Check(s.PreviousValidBuyerPrice(quotes[i].BuyerPays)==quotes[i-1]);
        }
    }
    private static SteamReviewRow Row(long bid,int quantity=3)
    {var s=new SteamFeeSchedule("KRW",1000,1000,new(50000),new(100000));var a=new InventoryAsset(new(753,"6","101"),"Steam","Synthetic","Synthetic",10,true,true,true);return new(a,quantity,bid,new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[new(bid,10)]),s,()=>{},lowerOneStep:true);}
    private static void Comparison(){var row=Row(70);Check(row.Valid&&row.Quote.BuyerPays==60&&row.AdjustmentComparison.Contains("20%")&&row.AdjustmentComparison.Contains("₩30")&&row.AdjustmentComparison.Contains("Steam")&&row.AdjustmentComparison.Contains("게임"));}
    private static void ReductionLabels()
    {
        var s=new SteamFeeSchedule("KRW",1000,1000,new(50000),new(100000));var a=new InventoryAsset(new(3678970,"2","101"),"TBH","Synthetic","Synthetic",3,true,true,true);
        foreach(var lowest in new[]{false,true})
        {
            var row=new SteamReviewRow(a,3,70,new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[new(70,3)]){Asks=[new(70,3)],OwnSellExcluded=true},s,()=>{},lowerOneStep:true,lowestSell:lowest);
            Check(row.AdjustmentComparison.Contains("기준 대비 인하 ₩10 / 개")&&row.AdjustmentComparison.Contains("실수령 감소 ₩30"));row.PriceText="50";Check(row.AdjustmentComparison.Contains("기준 대비 인하 ₩20 / 개"));
            var minimum=new SteamReviewRow(a,1,30,new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[new(30,3)]){Asks=[new(30,3)],OwnSellExcluded=true},s,()=>{},lowerOneStep:true,lowestSell:lowest);
            Check(minimum.AdjustmentComparison.Contains("기준 대비 인하 ₩0 / 개")&&minimum.AdjustmentComparison.Contains("최저 유효 가격 유지"));
        }
    }
    private static void Minimum(){var row=Row(30);Check(row.Valid&&row.Quote==new FeeQuote(30,10,10,10)&&row.AdjustmentComparison.Contains("최저 유효 가격 유지")&&row.AdjustmentComparison.Contains("0%"));row.PriceText="20";Check(!row.Valid);row.PriceText="30";Check(row.Valid);}
    private static async Task MinimumBatch()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=3;f.Server.ManyAssets=3;f.Server.OrderbookOverride="{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[300,3]}}}";await f.Login();await f.Vm.LoadAsync();f.Vm.SelectAll.Execute(null);await f.Vm.RefreshPricesAsync();f.Vm.LowerPriceOneStep=true;f.Vm.BuildReview();f.EnableTrading();
        Check(f.Vm.ReviewRows.Count==3&&f.Vm.ReviewRows.All(x=>x.Valid&&x.Quote.BuyerPays==3)&&f.Vm.ReviewAdjustmentSummary.Contains("최저 유효 가격 유지")&&f.Server.Sends==0);await f.Vm.Submit.ExecuteAsync();await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==3&&f.Server.SellFields.All(x=>x["price"]=="100")&&f.Vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard));
    }
    private static async Task StaleThenFresh()
    {
        var log=new Capture();using var use=DiagnosticHub.Use(log);
        using var f=new FifthStageTests.Fixture();f.Server.Free=3;f.Server.ManyAssets=3;await f.Login();await f.Vm.LoadAsync();f.Vm.SelectAll.Execute(null);await f.Vm.RefreshPricesAsync();f.Vm.LowerPriceOneStep=true;f.Vm.BuildReview();f.EnableTrading();f.Server.OrderbookOverride="{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[11500,2,11300,4,11000,3]}}}";
        var expected=f.Vm.ReviewRows.Select(x=>x.Quote.SellerReceives).ToArray();var bookCalls=f.Server.OrderbookReads;
        await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==3&&f.Vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard));
        Check(f.Server.SellFields.Select(x=>long.Parse(x["price"])).SequenceEqual(expected.Select(x=>new Money(x,"KRW").Wire)));
        Check(log.Records.Any(x=>x.Reason=="FixedApprovedPrice")&&bookCalls==f.Server.OrderbookReads);await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==3);

    }
    private sealed class Capture:IDiagnosticLog{public Guid SessionId{get;}=Guid.NewGuid();public List<DiagnosticRecord> Records{get;}=[];public void Write(DiagnosticRecord record)=>Records.Add(record);}
    private static async Task ManualShortage()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=2;f.Server.ManyAssets=2;f.Server.OrderbookOverride=Book("11500,1");await f.Login();await f.Vm.LoadAsync();f.Vm.SelectAll.Execute(null);await f.Vm.RefreshPricesAsync();f.Vm.LowerPriceOneStep=true;f.Vm.BuildReview();f.EnableTrading();var last=f.Vm.ReviewRows.Last();Check(last.Insufficient&&!last.Valid);last.PriceText="100";Check(last.Valid&&f.Vm.Submit.CanExecute(null)&&f.Vm.ReviewAdjustmentSummary.Contains("수동 가격"));await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==2&&f.Server.SellFields.Last()["price"]==new Money(last.Quote.SellerReceives,"KRW").Wire.ToString()&&f.Vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard));
    }
    private static string Book(string rows)=>"{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":["+rows+"]}}}";
    private static async Task ManualEmptyBook()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;f.Server.OrderbookOverride=Book("");await f.Login();f.Vm.LowerPriceOneStep=true;await f.Prepare(1);f.EnableTrading();var row=f.Vm.ReviewRows.Single();Check(!row.Valid);row.PriceText="30";Check(row.Valid&&f.Vm.Submit.CanExecute(null)&&row.PriceBasis.Contains("수동"));var implicitPrice=row.Snapshot(0) with{ManuallyPricedShortage=false};try{await f.Workspace!.Executor.ValidateBatchAsync(new(Guid.NewGuid(),f.Workspace.Authentication.SteamId!,DateTimeOffset.UtcNow,[implicitPrice]),default);throw new Exception("Implicit empty-book price accepted");}catch(MarketCrate.Infrastructure.Steam.SteamException){Check(f.Server.Sends==0);}await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==1&&f.Vm.Results.Single().State==SaleState.AwaitingSteamGuard);var stored=(await f.Workspace!.Store.LoadAsync(default))!;Check(stored.Snapshot.Lines.Single().UnitQuote.BuyerPays==30&&stored.Snapshot.Lines.Single().ManuallyPricedShortage);
    }
    private static async Task ManualBookChanged()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;f.Server.OrderbookOverride=Book("");await f.Login();f.Vm.LowerPriceOneStep=true;await f.Prepare(1);f.EnableTrading();f.Vm.ReviewRows.Single().PriceText="30";Check(f.Vm.Submit.CanExecute(null));f.Server.OrderbookOverride=Book("300,1");await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==1&&f.Vm.Results.Single().State==SaleState.AwaitingSteamGuard);
    }
    private static async Task Batch()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();Check(!f.Vm.LowerPriceOneStep);f.Vm.LowerPriceOneStep=true;await f.Prepare(10);f.EnableTrading();
        Check(f.Server.Sends==0&&string.Join(',',f.Vm.ReviewRows.Select(x=>x.Quantity))=="3,4,3"&&f.Vm.ReviewRows.All(x=>x.LowerStepRequested&&x.Quote==f.Server.Schedule.PreviousValidBuyerPrice(x.BaselineQuote!.BuyerPays))&&f.Vm.ReviewAdjustmentSummary.Contains("실수령 감소"));
        var expected=f.Vm.ReviewRows.Select(x=>(x.Quantity,Quote:x.Quote)).ToArray();f.Vm.LowerPriceOneStep=false;Check(f.Vm.LowerPriceOneStep);
        await f.Vm.Submit.ExecuteAsync();await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==3&&f.Vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard));
        var record=await f.Workspace!.Store.LoadAsync(default);Check(record!.Snapshot.Lines.Select(x=>(x.Quantity,Quote:x.UnitQuote)).SequenceEqual(expected));
    }
    private static async Task Manual(){using var f=new FifthStageTests.Fixture();await f.Login();f.Vm.LowerPriceOneStep=true;await f.Prepare(1);var row=f.Vm.ReviewRows.Single();var before=row.AdjustmentComparison;row.PriceText=SteamReviewRow.FormatPrice(row.BaselineQuote!.BuyerPays,"KRW");Check(row.AdjustmentComparison!=before&&row.AdjustmentComparison.Contains("0%"));row.PriceText=SteamReviewRow.FormatPrice(f.Server.Schedule.FromSeller(row.BaselineQuote.SellerReceives+1).BuyerPays,"KRW");Check(row.AdjustmentComparison.Contains("실수령 증가")&&f.Vm.ReviewAdjustmentSummary.Contains("실수령 증가"));f.Vm.CloseReview.Execute(null);f.Vm.LowerPriceOneStep=false;f.Vm.Review.Execute(null);Check(f.Vm.ReviewRows.All(x=>!x.LowerStepRequested)&&f.Server.Sends==0);}
    private static async Task ChangedBook(){using var f=new FifthStageTests.Fixture();await f.Login();f.Vm.LowerPriceOneStep=true;await f.Prepare(1);f.EnableTrading();f.Server.Free=0;await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==0&&!f.Vm.ReviewOpen);}
    private static async Task Reset(){using var f=new FifthStageTests.Fixture();await f.Login();f.Vm.LowerPriceOneStep=true;f.Vm.Logout.Execute(null);Check(!f.Vm.LowerPriceOneStep);await f.Login();Check(!f.Vm.LowerPriceOneStep&&f.Server.Sends==0);}
    private static async Task MinimumReprice()
    {
        using var f=new FifthStageTests.Fixture();f.Server.KrwIncrement=1000;f.Server.Free=1;f.Server.OrderbookOverride=Book("3000,1");await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.Submit.ExecuteAsync();f.Server.Publish=true;await f.Vm.LoadListingsAsync();f.Vm.SelectAllListings.Execute(null);f.Vm.LowerPriceOneStep=true;await f.Vm.PrepareListingRepriceAsync();Check(f.Vm.Listings.Single().Listing.SellerReceives==10&&f.Vm.Listings.Single().Listing.Fees==20&&f.Vm.ListingPriceStatus.Contains("이미 제안 가격")&&!f.Vm.ManagementOpen&&f.Server.Cancels==0&&f.Server.Sends==1&&f.Vm.Connected);
    }
    private static string SellBook(string asks="57500,1",string bids="11500,1")=>"{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":["+bids+"],\"rgCompactSellOrders\":["+asks+"]}}}";
    private static void SellParser()
    {
        var book=MarketCrate.Infrastructure.Steam.SteamContract.OrderBook(SellBook("3000,1,4000,2"));Check(book.Asks.Length==2&&book.Asks[0].BuyerPays==30);
        foreach(var asks in new[]{"3000","4000,1,3000,2","3000,0","3000,1,3000,2","3001,1"}){try{MarketCrate.Infrastructure.Steam.SteamContract.OrderBook(SellBook(asks));throw new Exception("Invalid sell depth accepted");}catch(MarketCrate.Infrastructure.Steam.SteamException){}}
        try{MarketCrate.Infrastructure.Steam.SteamContract.OrderBook(SellBook().Replace("\"eCurrency\":16","\"amtMinSellOrder\":100,\"eCurrency\":16"));throw new Exception("Best ask mismatch accepted");}catch(MarketCrate.Infrastructure.Steam.SteamException){}
    }
    private static void SellMoney()
    {
        var krw=new SteamFeeSchedule("KRW",1000,1000,new(50000),new(100000));Check(krw.LowestSellQuote(30,true)==new FeeQuote(30,10,10,10)&&krw.LowestSellQuote(20,false).BuyerPays==30&&krw.LowestSellQuote(70,true).BuyerPays==60);Reject(()=>krw.LowestSellQuote(0,true));
        var usd=new SteamFeeSchedule("USD",1,1,new(50000),new(100000));Check(usd.LowestSellQuote(23,true).BuyerPays==21&&usd.LowestSellQuote(22,true).BuyerPays==21);Reject(()=>usd.LowestSellQuote(22,false));var a=new InventoryAsset(new(730,"2","999"),"Steam","Synthetic","Synthetic",1,false,true,true);var row=new SteamReviewRow(a,1,22,new("USD",DateTimeOffset.UtcNow,"Synthetic",TimeSpan.Zero,[],[]){Asks=[new(22,1)],OwnSellExcluded=true},usd,()=>{},lowerOneStep:true,lowestSell:true);Check(row.Valid&&row.Quote.BuyerPays==21&&row.PriceBasis.Contains("수수료 유효 가격 환산"));
    }
    private static async Task SellInventory(bool lower)
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=2;f.Server.ManyAssets=2;f.Server.OrderbookOverride=SellBook(bids:"");await f.Login();await f.Vm.LoadAsync();f.Vm.SelectAll.Execute(null);f.Vm.PriceTarget=1;f.Vm.LowerPriceOneStep=lower;await f.Vm.RefreshPricesAsync();f.Vm.BuildReview();f.EnableTrading();Check(f.Vm.ReviewRows.Count==2&&f.Vm.ReviewRows.All(x=>x.Valid&&x.Quote.BuyerPays==f.Server.Schedule.LowestSellQuote(575,lower).BuyerPays)&&f.Vm.ReviewLiquidityText.Contains("최저"));await f.Vm.Submit.ExecuteAsync();await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==2&&f.Vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard));var saved=(await f.Workspace!.Store.LoadAsync(default))!;Check(saved.Snapshot.Lines.All(x=>OrderbookFingerprint.IsSell(x.OrderbookDigest)));f.Vm.Logout.Execute(null);await f.Login();Check(f.Vm.PriceTarget==0&&f.Server.Sends==2);
    }
    private static async Task SellChanged()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();f.Server.OrderbookOverride=SellBook();f.Vm.PriceTarget=1;await f.Prepare(1);f.EnableTrading();f.Server.OrderbookOverride=SellBook("60000,1");await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==1&&f.Server.Cancels==0&&f.Server.SellFields.Single()["price"]==new Money(f.Server.Schedule.LowestSellQuote(575,false).SellerReceives,"KRW").Wire.ToString());
    }
    private static async Task SellMissing(){using var f=new FifthStageTests.Fixture();await f.Login();f.Vm.PriceTarget=1;await f.Prepare(1);Check(!f.Vm.ReviewOpen&&f.Server.Sends==0&&f.Vm.Status.Contains("최저 판매주문호가"));}
    private static async Task SellReprice()
    {
        using var f=new FifthStageTests.Fixture();f.Server.IncludeOwnSellDepth=true;f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.Submit.ExecuteAsync();f.Server.Publish=true;await f.Vm.LoadListingsAsync();f.Vm.SelectAllListings.Execute(null);f.Vm.PriceTarget=1;f.Vm.LowerPriceOneStep=true;f.Server.OrderbookOverride=SellBook();await f.Vm.PrepareListingRepriceAsync();Check(f.Vm.ManagementOpen&&f.Vm.ListingPriceStatus.Contains("최저 판매주문호가"));await f.Vm.ConfirmManagement.ExecuteAsync();Check(f.Server.Cancels==1&&f.Server.Sends==2&&f.Server.SellFields.Last()["price"]==new Money(f.Server.Schedule.LowestSellQuote(575,true).SellerReceives,"KRW").Wire.ToString());
    }
    private static async Task SellRepriceChanged()
    {
        using var f=new FifthStageTests.Fixture();f.Server.IncludeOwnSellDepth=true;f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.Submit.ExecuteAsync();f.Server.Publish=true;await f.Vm.LoadListingsAsync();f.Vm.SelectAllListings.Execute(null);f.Vm.PriceTarget=1;f.Server.OrderbookOverride=SellBook();await f.Vm.PrepareListingRepriceAsync();Check(f.Vm.ManagementOpen);f.Server.OrderbookOverride=SellBook("60000,1");await f.Vm.ConfirmManagement.ExecuteAsync();Check(f.Server.Cancels==1&&f.Server.Sends==2);
    }
    private static async Task SellQuantityChanged()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();f.Server.OrderbookOverride=SellBook();f.Vm.PriceTarget=1;await f.Prepare(1);f.EnableTrading();f.Server.OrderbookOverride=SellBook("57500,10");await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==1&&f.Vm.Results.Single().State==SaleState.AwaitingSteamGuard);
    }
    private static void OwnSellExcluded()
    {
        var asset=new InventoryAsset(new(730,"2","100"),"Steam","Synthetic","Synthetic",1,false,true,true);
        var price=new PriceSnapshot("KRW",DateTimeOffset.UtcNow,"Synthetic",TimeSpan.Zero,[],[]){Asks=[new(30,2),new(40,5),new(50,2)]};
        var own=new[]{new MarketCrate.Infrastructure.Steam.SteamListing("1",asset.Key,"Synthetic",2,"KRW",10,20,SaleState.Listed),new("2",asset.Key,"Synthetic",1,"KRW",20,20,SaleState.Listed),new("3",asset.Key,"Synthetic",9,"KRW",10,20,SaleState.AwaitingSteamGuard)};
        var excluded=MarketCrate.Infrastructure.Steam.SteamMarketDataProvider.ExcludeOwnSell(price,asset,own);Check(excluded.OwnSellExcluded&&excluded.Asks[0]==new BidLevel(40,4));
        var after=MarketCrate.Infrastructure.Steam.SteamMarketDataProvider.ExcludeOwnSell(price with{Asks=[new(40,5),new(50,2)]},asset,own.Skip(1).ToArray());Check(OrderbookFingerprint.OfAsk(excluded)==OrderbookFingerprint.OfAsk(after));
        var partial=MarketCrate.Infrastructure.Steam.SteamMarketDataProvider.ExcludeOwnSell(price,asset,[own[0] with{Quantity=1}]);Check(partial.Asks[0]==new BidLevel(30,1));
        var empty=MarketCrate.Infrastructure.Steam.SteamMarketDataProvider.ExcludeOwnSell(price with{Asks=[new(30,2)]},asset,[own[0]]);Check(empty.Asks.IsEmpty);Reject(()=>OrderbookFingerprint.OfAsk(empty));
        try{MarketCrate.Infrastructure.Steam.SteamMarketDataProvider.ExcludeOwnSell(price,asset,[own[0] with{Quantity=3}]);throw new Exception("Own quantity mismatch accepted");}catch(MarketCrate.Infrastructure.Steam.SteamException){}
    }
    private static async Task InventoryOwnSell()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=2;f.Server.ManyAssets=2;f.Server.IncludeOwnSellDepth=true;f.Server.Publish=true;f.Server.Rows.Add(("999",1,100));f.Server.OrderbookOverride=SellBook();await f.Login();await f.Vm.LoadAsync();f.Vm.Assets.Single(x=>x.Asset.Key.AssetId=="102").Selected=true;f.Vm.PriceTarget=1;await f.Vm.RefreshPricesAsync();f.Vm.BuildReview();Check(f.Vm.ReviewRows.Single().Quote.BuyerPays==575&&f.Vm.ReviewRows.Single().PriceBasis.Contains("본인 판매등록 제외")&&f.Server.Sends==0&&f.Server.Cancels==0);
    }
    private static async Task SellMinimum()
    {
        using var f=new FifthStageTests.Fixture();f.Server.KrwIncrement=1000;f.Server.IncludeOwnSellDepth=true;f.Server.Free=1;f.Server.OrderbookOverride=SellBook("","3000,1");await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.Submit.ExecuteAsync();f.Server.Publish=true;await f.Vm.LoadListingsAsync();f.Vm.SelectAllListings.Execute(null);f.Vm.PriceTarget=1;f.Vm.LowerPriceOneStep=true;await f.Vm.PrepareListingRepriceAsync();Check(!f.Vm.ManagementOpen&&f.Vm.ListingPriceStatus.Contains("이미 제안 가격")&&f.Server.Cancels==0&&f.Server.Sends==1);
    }
    private static async Task Listing()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.Submit.ExecuteAsync();f.Server.Publish=true;await f.Vm.LoadListingsAsync();f.Vm.SelectAllListings.Execute(null);f.Vm.LowerPriceOneStep=true;await f.Vm.PrepareListingRepriceAsync();Check(f.Vm.ManagementOpen&&f.Vm.ManagementSummary.Contains("실수령 감소")&&f.Vm.Listings.All(x=>x.HasStepProposal)&&f.Server.Cancels==0);
        var expected=f.Server.Schedule.PreviousValidBuyerPrice(115);await f.Vm.ConfirmManagement.ExecuteAsync();Check(f.Server.Cancels==1&&f.Server.Sends==2&&long.Parse(f.Server.SellFields.Last()["price"])==new Money(expected.SellerReceives,"KRW").Wire&&f.Vm.Results.Last().State==SaleState.AwaitingSteamGuard);
    }
}
