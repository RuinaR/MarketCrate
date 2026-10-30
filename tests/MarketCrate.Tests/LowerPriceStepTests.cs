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
            ("Minimum cannot be bypassed with manual input",()=>Sync(Minimum)),
            ("Manual edits refresh comparison and closed review requires a new plan",Manual),
            ("Changed asset state rejects discounted approval without submission",ChangedBook),
            ("Lower option survives neither logout nor restored connection",Reset),
            ("Listing batch lower proposals are compared before any cancellation",Listing)
        };
        int passed=0,failed=0;foreach(var(name,test)in tests)try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}return(passed,failed);
    }
    private static Task Sync(Action action){action();return Task.CompletedTask;}
    private static void Properties()
    {
        foreach(var s in new[]{new SteamFeeSchedule("USD",1,1,new(50000),new(100000)),new("KRW",1000,1000,new(50000),new(100000)),new("KRW",100,100,new(50000),new(0)),new("USD",1,1,new(0),new(0))})
        {
            var unit=Money.FromWire(s.IncrementWire,s.Currency).Minor;var minimum=Money.FromWire(s.MinimumWire,s.Currency).Minor;
            var quotes=Enumerable.Range(0,250).Select(i=>s.FromSeller(minimum+i*unit)).ToArray();Reject(()=>s.PreviousValidBuyerPrice(quotes[0].BuyerPays));
            for(int i=1;i<quotes.Length;i++)Check(s.PreviousValidBuyerPrice(quotes[i].BuyerPays)==quotes[i-1]);
        }
    }
    private static SteamReviewRow Row(long bid,int quantity=3)
    {var s=new SteamFeeSchedule("KRW",1000,1000,new(50000),new(100000));var a=new InventoryAsset(new(753,"6","101"),"Steam","Synthetic","Synthetic",10,true,true,true);return new(a,quantity,bid,new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[new(bid,10)]),s,()=>{},lowerOneStep:true);}
    private static void Comparison(){var row=Row(70);Check(row.Valid&&row.Quote.BuyerPays==60&&row.AdjustmentComparison.Contains("20%")&&row.AdjustmentComparison.Contains("₩30")&&row.AdjustmentComparison.Contains("Steam")&&row.AdjustmentComparison.Contains("게임"));}
    private static void Minimum(){var row=Row(30);Check(!row.Valid&&row.AdjustmentComparison.Contains("계산할 수 없습니다"));row.PriceText="30";Check(!row.Valid);}
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
    private static async Task Listing()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.Submit.ExecuteAsync();f.Server.Publish=true;await f.Vm.LoadListingsAsync();f.Vm.SelectAllListings.Execute(null);f.Vm.LowerPriceOneStep=true;await f.Vm.PrepareListingRepriceAsync();Check(f.Vm.ManagementOpen&&f.Vm.ManagementSummary.Contains("실수령 감소")&&f.Vm.Listings.All(x=>x.HasStepProposal)&&f.Server.Cancels==0);
        var expected=f.Server.Schedule.PreviousValidBuyerPrice(115);await f.Vm.ConfirmManagement.ExecuteAsync();Check(f.Server.Cancels==1&&f.Server.Sends==2&&long.Parse(f.Server.SellFields.Last()["price"])==new Money(expected.SellerReceives,"KRW").Wire&&f.Vm.Results.Last().State==SaleState.AwaitingSteamGuard);
    }
}
