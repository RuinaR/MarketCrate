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
            ("KRW70 to60 valid increment fees and net50 to40",()=>Sync(()=>{var s=new SteamFeeSchedule("KRW",1000,1000,new(50000),new(100000));Check(s.FromBuyer(70)==new FeeQuote(70,50,10,10));Check(s.PreviousValidBuyerPrice(70)==new FeeQuote(60,40,10,10));Reject(()=>s.PreviousValidBuyerPrice(30));})),
            ("USD fee gap23 to21 is one reachable buyer step",()=>Sync(()=>{var s=new SteamFeeSchedule("USD",1,1,new(50000),new(100000));Check(s.PreviousValidBuyerPrice(23).BuyerPays==21);Reject(()=>s.PreviousValidBuyerPrice(22));Reject(()=>s.PreviousValidBuyerPrice(3));})),
            ("Nearest lower valid price property across KRW USD zero and game fees",()=>Sync(Properties)),
            ("Review displays per-unit fees and quantity reduction and unavailable minimum",()=>Sync(Comparison)),
            ("Opt-in default OFF price allocation preserved and one approval sends all changed prices",Batch),
            ("Minimum is retained visibly while below-minimum manual input remains rejected",()=>Sync(Minimum)),
            ("Last shortage row accepts a verified manual price and joins the same batch",ManualShortage),
            ("Verified empty orderbook supports an explicit manual limit listing",ManualEmptyBook),
            ("Orderbook failure or changed bids cannot use the manual empty-book path",ManualBookChanged),
            ("Three minimum-price assets submit once after one unchanged final approval",MinimumBatch),
            ("Changed orderbook sends no request and a fresh review can submit the whole batch",StaleThenFresh),
            ("Manual edits refresh comparison and closed review requires a new plan",Manual),
            ("Changed asset state rejects discounted approval without submission",ChangedBook),
            ("Lower option survives neither logout nor restored connection",Reset),
            ("Listing batch lower proposals are compared before any cancellation",Listing),
            ("KRW30 minimum lower reprice preserves current listing without cancellation",MinimumReprice)
        };
        int passed=0,failed=0;foreach(var(name,test)in tests)try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}return(passed,failed);
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
        await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==0&&f.Workspace!.Store.Commands().Count==0&&!f.Vm.ReviewOpen&&f.Vm.Status.Contains("Steam 승인")&&f.Vm.Status.Contains("호가"));Check(log.Records.Any(x=>x.EventName=="BatchFinished"&&x.Outcome==DiagnosticOutcome.Failed&&x.Error==DiagnosticError.Validation)&&!log.Records.Any(x=>x.Error==DiagnosticError.Storage));await f.Vm.RefreshPrices.ExecuteAsync();f.Vm.Review.Execute(null);Check(f.Vm.ReviewOpen);await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==3&&f.Vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard));
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
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;f.Server.OrderbookOverride=Book("");await f.Login();f.Vm.LowerPriceOneStep=true;await f.Prepare(1);f.EnableTrading();f.Vm.ReviewRows.Single().PriceText="30";Check(f.Vm.Submit.CanExecute(null));f.Server.OrderbookOverride=Book("300,1");await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==0&&f.Workspace!.Store.Commands().Count==0&&!f.Vm.ReviewOpen);
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
    private static async Task Listing()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.Submit.ExecuteAsync();f.Server.Publish=true;await f.Vm.LoadListingsAsync();f.Vm.SelectAllListings.Execute(null);f.Vm.LowerPriceOneStep=true;await f.Vm.PrepareListingRepriceAsync();Check(f.Vm.ManagementOpen&&f.Vm.ManagementSummary.Contains("실수령 감소")&&f.Vm.Listings.All(x=>x.HasStepProposal)&&f.Server.Cancels==0);
        var expected=f.Server.Schedule.PreviousValidBuyerPrice(115);await f.Vm.ConfirmManagement.ExecuteAsync();Check(f.Server.Cancels==1&&f.Server.Sends==2&&long.Parse(f.Server.SellFields.Last()["price"])==new Money(expected.SellerReceives,"KRW").Wire&&f.Vm.Results.Last().State==SaleState.AwaitingSteamGuard);
    }
}
