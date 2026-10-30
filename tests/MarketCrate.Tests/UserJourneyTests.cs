using System.Net;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.Tests;

public static class UserJourneyTests
{
    private sealed class Trace:IDiagnosticLog{public Guid SessionId{get;}=Guid.NewGuid();public List<DiagnosticRecord> Events{get;}=[];public void Write(DiagnosticRecord value)=>Events.Add(value);}
    private static void Check(bool ok,string message="User journey contract"){if(!ok)throw new Exception(message);}
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{("First launch commands cannot authenticate or trade without consent",FirstLaunch),("UI commands login split approval Guard publish cancellation logout",HappyPath),("Typed quantity invalid matrix blocks old-quantity approval",QuantityInputs),("Review price invalid matrix blocks submission and recovers",PriceInputs),("Review row and aggregate arithmetic overflow blocks submission",Overflow),("Inventory HTTP malformed and empty response matrix with independent reads",ReadFailures),("Cancel loading preserves prior data and selection",CancelRead),("Logout during response discards late data and allows reconnect",LogoutRead),("Double approval pending POST has exactly one request and durable intent",DoubleApproval),("Lost response restart and UI recovery do not resend",UnknownRestart),("Submission error matrix and partial batch preserve Unknown without retry",SubmissionOutcomes),("Catalog invalid matrix does not mutate or send HTTP",CatalogInputs),("Explicit game removal cannot leave invisible sale selections",RemoveGame),("Search sort page and game switches preserve selection and quantity",SelectionPaths)};
        int passed=0,failed=0;foreach(var(name,test)in tests)try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}return(passed,failed);
    }
    private sealed class Proxy(SteamContractServer server):HttpMessageHandler
    {
        private readonly HttpMessageInvoker inner=new(server.Connection());
        public Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage?>>? Intercept;
        public readonly List<string> Paths=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken t){Paths.Add(r.RequestUri!.AbsolutePath);return(Intercept is {} f?await f(r,t):null)??await inner.SendAsync(r,t);}
        protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
    }
    private sealed class Fixture:IDisposable
    {
        private readonly string root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","journey-"+Guid.NewGuid().ToString("N"));
        public SteamContractServer Server{get;}=new();public SteamExperimentalWorkspace? Workspace;public Proxy? Http;public SteamWorkspaceViewModel Vm{get;}
        public Fixture()=>Vm=new(scope=>Workspace=new(root,scope.AppId,scope.ContextId,scope.Name,new(true,"synthetic-only",true),Http=new(Server),Server.Clock),true,openOfficial:_=>throw new Exception("Browser forbidden"));
        public async Task Login(){Vm.RiskAccepted=Vm.CredentialsAccepted=true;await Vm.BeginLogin.ExecuteAsync();Server.Clock.Advance();await Vm.CheckLogin.ExecuteAsync();Check(Vm.Connected);}
        public async Task Prepare(int quantity=1){await Vm.Load.ExecuteAsync();Vm.SelectAll.Execute(null);Vm.Assets[0].QuantityText=quantity.ToString();await Vm.RefreshPrices.ExecuteAsync();Vm.Review.Execute(null);Check(Vm.ReviewOpen);Vm.TradeAccepted=true;Vm.EnableTrading.Execute(null);Check(Vm.Submit.CanExecute(null));}
        public void Dispose(){Vm.Dispose();Server.Dispose();if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    private static async Task FirstLaunch()
    {
        using var f=new Fixture();Check(!f.Vm.Connected&&!f.Vm.Busy&&f.Vm.Assets.Count==0);
        foreach(var cmd in new[]{f.Vm.BeginLogin,f.Vm.CheckLogin,f.Vm.Load,f.Vm.LoadListings,f.Vm.LoadWallet,f.Vm.LoadFees,f.Vm.RefreshPrices,f.Vm.Submit,f.Vm.Recover,f.Vm.ConfirmManagement}){Check(!cmd.CanExecute(null));await cmd.ExecuteAsync();}
        f.Vm.Review.Execute(null);f.Vm.ReviewCancel.Execute(null);Check(f.Server.Calls==0&&f.Server.Sends==0&&f.Server.Cancels==0&&!f.Vm.AnyReview);
    }
    private static async Task HappyPath()
    {
        var trace=new Trace();using var logging=DiagnosticHub.Use(trace);using var f=new Fixture();await f.Login();Check(!f.Vm.CanTrade);await f.Prepare(10);Check(string.Join(',',f.Vm.ReviewRows.Select(x=>x.Quantity))=="3,4,3");
        await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==3&&f.Vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard));
        var operation=f.Vm.LastOperationId;var saleEvents=trace.Events.Where(x=>x.Context.OperationId==operation).ToArray();Check(operation is not null&&saleEvents.Any(x=>x.EventId==(int)DiagnosticEvent.HttpRequest)&&saleEvents.Any(x=>x.EventId==(int)DiagnosticEvent.JournalWrite)&&saleEvents.Count(x=>x.EventId==(int)DiagnosticEvent.ItemResult&&x.JournalState=="Committed")==3,$"UI correlation: HTTP={saleEvents.Count(x=>x.EventId==(int)DiagnosticEvent.HttpRequest)} journal={saleEvents.Count(x=>x.EventId==(int)DiagnosticEvent.JournalWrite)} item={saleEvents.Count(x=>x.EventId==(int)DiagnosticEvent.ItemResult)}");
        var safe=System.Text.Json.JsonSerializer.Serialize(trace.Events);Check(!safe.Contains("synthetic-refresh")&&!safe.Contains("synthetic-cookie")&&!safe.Contains("76561198000000001"));await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==3);
        f.Server.Publish=true;await f.Vm.Recover.ExecuteAsync();Check(f.Vm.Results.All(x=>x.State==SaleState.Listed)&&f.Server.Sends==3);
        f.Vm.SelectAllListings.Execute(null);f.Vm.ReviewCancel.Execute(null);Check(f.Vm.ManagementOpen);await f.Vm.ConfirmManagement.ExecuteAsync();Check(f.Server.Cancels==3&&f.Server.Free==10&&f.Server.Sends==3);
        f.Vm.Logout.Execute(null);Check(!f.Vm.Connected&&!f.Vm.CanTrade&&f.Vm.Assets.Count==0&&f.Vm.Results.Count==0);
    }
    private static async Task QuantityInputs()
    {
        using var f=new Fixture();await f.Login();await f.Prepare();f.Vm.CloseReview.Execute(null);var row=f.Vm.Assets[0];
        foreach(var text in new[]{"","0","-1","11","2147483648","1.5","1e1","1,000"," 1","１","NaN","abc"}){row.QuantityText=text;Check(!row.QuantityInputValid&&f.Vm.HasQuantityInputErrors&&!f.Vm.Review.CanExecute(null));f.Vm.Review.Execute(null);Check(!f.Vm.AnyReview&&f.Server.Sends==0&&f.Vm.SelectionEstimate.Contains("수량"));}
        row.QuantityText="7";Check(row.Quantity==7&&!f.Vm.HasQuantityInputErrors&&f.Vm.Review.CanExecute(null));f.Vm.Review.Execute(null);Check(f.Vm.ReviewRows.Sum(x=>x.Quantity)==7);f.Vm.CloseReview.Execute(null);
        row.Quantity=int.MaxValue;Check(row.Quantity==10&&row.QuantityText=="10");row.Quantity=int.MinValue;Check(row.Quantity==1&&row.QuantityText=="1");
    }
    private static async Task PriceInputs()
    {
        using var f=new Fixture();await f.Login();await f.Prepare();var row=f.Vm.ReviewRows.Single();
        foreach(var text in new[]{"","0","-1","1.5","1e2","1,000","₩115"," 115","NaN","999999999999999999999999999999","1","57","231"}){row.PriceText=text;Check(!row.Valid&&!f.Vm.Submit.CanExecute(null));await f.Vm.Submit.ExecuteAsync();_=row.Amounts;_=f.Vm.Totals;Check(f.Server.Sends==0&&f.Vm.ReviewOpen);}
        row.PriceText="115";Check(row.Valid&&f.Vm.Submit.CanExecute(null));await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==1);
    }
    private static async Task Overflow()
    {
        using var f=new Fixture();await f.Login();await f.Prepare();var asset=f.Vm.Assets[0].Asset with{Quantity=int.MaxValue};var schedule=new SteamFeeSchedule("KRW",100,100,new(0),new(0));var price=new PriceSnapshot("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[]);var buyer=schedule.FromSeller(1_000_000_000_000_000).BuyerPays;
        var row=new SteamReviewRow(asset,int.MaxValue,buyer,price,schedule,()=>{});Check(!row.Valid);_=row.Amounts;
        f.Vm.ReviewRows.Clear();for(var i=0;i<10;i++)f.Vm.ReviewRows.Add(new(asset,1000,buyer,price,schedule,()=>{}));Check(f.Vm.ReviewRows.All(x=>x.Valid)&&!f.Vm.ReviewAmountsValid&&!f.Vm.Submit.CanExecute(null));_=f.Vm.Totals;await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==0);
    }
    private static async Task ReadFailures()
    {
        foreach(var(code,body,empty)in new[]{(200,"{\"success\":1,\"total_inventory_count\":0}",true),(200,"{}",false),(200,"not-json",false),(200,"<html>unknown</html>",false),(400,"{}",false),(401,"{}",false),(403,"{}",false),(404,"{}",false),(429,"{}",false),(500,"{}",false)})
        {using var f=new Fixture();await f.Login();f.Http!.Intercept=(r,t)=>Task.FromResult<HttpResponseMessage?>(r.RequestUri!.AbsolutePath.StartsWith("/inventory/")?new((HttpStatusCode)code){Content=new StringContent(body)}:null);await f.Vm.Load.ExecuteAsync();Check(!f.Vm.Busy&&f.Server.Sends==0);Check(empty?f.Vm.InventoryRead.State==SteamReadState.Empty:f.Vm.InventoryRead.State!=SteamReadState.Empty);Check(f.Http.Paths.Count(x=>x.StartsWith("/inventory/"))==1,"Unexpected retry");if(f.Vm.CanRead){await f.Vm.LoadListings.ExecuteAsync();Check(f.Vm.ListingsRead.State is SteamReadState.Empty or SteamReadState.Ready,"Independent read disabled");}if(code is 403 or 429){var n=f.Http.Paths.Count;await f.Vm.Load.ExecuteAsync();await f.Vm.LoadListings.ExecuteAsync();Check(f.Http.Paths.Count==n,"Restriction bypass");}}
    }
    private static async Task CancelRead()
    {
        using var f=new Fixture();await f.Login();await f.Vm.Load.ExecuteAsync();f.Vm.SelectAll.Execute(null);var row=f.Vm.Assets.Single();row.Quantity=4;f.Server.InventoryStarted=new(TaskCreationOptions.RunContinuationsAsynchronously);f.Server.InventoryRelease=new(TaskCreationOptions.RunContinuationsAsynchronously);
        var task=f.Vm.Load.ExecuteAsync();await f.Server.InventoryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));Check(f.Vm.Busy&&f.Vm.CancelOperation.CanExecute(null));f.Vm.CancelOperation.Execute(null);f.Server.InventoryRelease.SetResult();await task;Check(f.Vm.Connected&&!f.Vm.Busy&&ReferenceEquals(row,f.Vm.Assets.Single())&&row.Selected&&row.Quantity==4&&f.Vm.InventoryRead.State==SteamReadState.Cancelled&&f.Server.Sends==0);
    }
    private static async Task LogoutRead()
    {
        using var f=new Fixture();await f.Login();f.Server.InventoryStarted=new(TaskCreationOptions.RunContinuationsAsynchronously);f.Server.InventoryRelease=new(TaskCreationOptions.RunContinuationsAsynchronously);var task=f.Vm.Load.ExecuteAsync();await f.Server.InventoryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));f.Vm.Logout.Execute(null);f.Server.InventoryRelease.SetResult();await task;Check(!f.Vm.Connected&&!f.Vm.Busy&&!f.Vm.CanTrade&&f.Vm.Assets.Count==0&&f.Vm.Results.Count==0);await f.Login();Check(!f.Vm.CanTrade&&f.Server.Sends==0);
    }
    private static async Task DoubleApproval()
    {
        using var f=new Fixture();await f.Login();await f.Prepare();var started=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);f.Http!.Intercept=async(r,t)=>{if(r.RequestUri!.AbsolutePath=="/market/sellitem/"){started.SetResult();await release.Task.WaitAsync(t);}return null;};
        var first=f.Vm.Submit.ExecuteAsync();await started.Task.WaitAsync(TimeSpan.FromSeconds(5));Check(f.Vm.Busy&&!f.Vm.Submit.CanExecute(null)&&f.Workspace!.Store.Commands().Count==1);await f.Vm.Submit.ExecuteAsync();f.Vm.Review.Execute(null);f.Vm.Game=f.Vm.Games[1];Check(f.Vm.Game.AppId==730);release.SetResult();await first;Check(f.Server.Sends==1&&f.Http.Paths.Count(x=>x=="/market/sellitem/")==1&&!f.Vm.Busy);
    }
    private static async Task UnknownRestart()
    {
        using var f=new Fixture();await f.Login();await f.Prepare();f.Server.TimeoutSell=true;await f.Vm.Submit.ExecuteAsync();Check(f.Server.Sends==1&&f.Vm.Results.Single().State==SaleState.Unknown);f.Vm.Logout.Execute(null);await f.Login();await f.Vm.Recover.ExecuteAsync();Check(f.Server.Sends==1&&f.Workspace!.Store.Commands().Single().State==SaleState.Unknown&&!f.Vm.ReviewOpen);
    }
    private static async Task CatalogInputs()
    {
        using var f=new Fixture();await f.Login();var count=f.Vm.Games.Count;var calls=f.Server.Calls;foreach(var(app,context,name)in new[]{("","2","x"),("-1","2","x"),("2147483648","2","x"),("730","2","duplicate"),("123","../x","x"),("123","0","x"),("123","2","")}){f.Vm.AppInput=app;f.Vm.ContextInput=context;f.Vm.NameInput=name;f.Vm.AddScope.Execute(null);Check(f.Vm.Games.Count==count&&f.Server.Calls==calls&&f.Vm.Connected);}
    }
    private static async Task SubmissionOutcomes()
    {
        foreach(var(code,body)in new[]{(200,"{}"),(200,"not-json"),(200,"<html>unknown</html>"),(200,"{\"success\":false}"),(400,"{}"),(401,"{}"),(403,"{}"),(429,"{}"),(500,"{}")})
        {
            using var f=new Fixture();await f.Login();await f.Prepare();
            f.Http!.Intercept=(r,t)=>Task.FromResult<HttpResponseMessage?>(r.RequestUri!.AbsolutePath=="/market/sellitem/"?new((HttpStatusCode)code){Content=new StringContent(body)}:null);
            await f.Vm.Submit.ExecuteAsync();Check(f.Vm.Results.Single().State==SaleState.Unknown&&!f.Vm.HasApprovalHandoff&&!f.Vm.Busy);
            Check(f.Workspace!.Store.Commands().Single().State==SaleState.Unknown);
            await f.Vm.Submit.ExecuteAsync();Check(f.Http.Paths.Count(x=>x=="/market/sellitem/")==1,"Uncertain submission retried");
        }
        using var partial=new Fixture();await partial.Login();await partial.Prepare(10);int posts=0;
        partial.Http!.Intercept=(r,t)=>Task.FromResult<HttpResponseMessage?>(r.RequestUri!.AbsolutePath=="/market/sellitem/"&&++posts==2?new(HttpStatusCode.InternalServerError){Content=new StringContent("{}")} :null);
        await partial.Vm.Submit.ExecuteAsync();Check(partial.Vm.Results.Select(x=>x.State).SequenceEqual(new[]{SaleState.AwaitingSteamGuard,SaleState.Unknown,SaleState.Ready})&&posts==2&&partial.Server.Sends==1&&partial.Workspace!.Store.Commands().Count==2);
    }
    private static async Task RemoveGame()
    {
        using var f=new Fixture();await f.Login();await f.Vm.Load.ExecuteAsync();f.Vm.SelectAll.Execute(null);Check(f.Vm.SelectionSummary.Contains("선택 1종"));f.Vm.RemoveScope.Execute(null);Check(f.Vm.Games.All(x=>x.AppId!=730)&&f.Vm.SelectionSummary.Contains("선택 0종")&&!f.Vm.Review.CanExecute(null)&&f.Server.Sends==0);
    }
    private static async Task SelectionPaths()
    {
        using var f=new Fixture();await f.Login();await f.Vm.Load.ExecuteAsync();f.Vm.SelectAll.Execute(null);var row=f.Vm.Assets.Single();row.QuantityText="7";var scope=f.Vm.Game;foreach(var search in new[]{"no-match","Synthetic","","한글","%_[]"}){f.Vm.Search=search;f.Vm.Descending=!f.Vm.Descending;f.Vm.WorkPage=2;f.Vm.WorkPage=1;Check(row.Selected&&row.Quantity==7);}f.Vm.Search="";f.Vm.Game=f.Vm.Games[1];f.Vm.Game=scope;Check(ReferenceEquals(row,f.Vm.Assets.Single())&&row.Selected&&row.Quantity==7);f.Vm.ClearSelection.Execute(null);Check(!row.Selected&&row.Quantity==7&&f.Server.Sends==0);
    }
}
