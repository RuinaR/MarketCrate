using System.Collections.Immutable;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;

internal static class FifthStageTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new (string,Func<Task>)[]
        {
            ("Money KRW/USD integer wire and unsupported rejection",()=>Sync(()=>{Equal(123400L,new Money(1234,"KRW").Wire);Equal(1234L,Money.FromWire(123400,"KRW").Minor);Equal(123L,new Money(123,"USD").Wire);Throws<ArgumentException>(()=>Money.FromWire(12345,"KRW"));Throws<ArgumentException>(()=>new Money(1,"EUR"));Throws<OverflowException>(()=>{_=new Money(long.MaxValue,"KRW").Wire;});})),
            ("KRW server floor increment nearest fees and caps",()=>Sync(()=>{var schedule=Krw();Equal(new FeeQuote(116,101,5,10),schedule.FromSeller(101));Equal(new FeeQuote(30,10,10,10),schedule with { MinimumWire=1000 } is var high?high.FromSeller(10):throw new Exception());Equal(0L,(schedule with { Publisher=new(0) }).FromSeller(100).GameFee);Equal(2L,(schedule with { Steam=new(50000,200) }).FromSeller(100).SteamFee);Throws<ArgumentException>(()=>(schedule with { IncrementWire=1 }).FromSeller(1));})),
            ("USD integer floor fee minimum and unrepresentable gap",()=>Sync(()=>{var usd=new SteamFeeSchedule("USD",1,1,new(50000),new(100000));Equal(new FeeQuote(3,1,1,1),usd.FromSeller(1));Equal(new FeeQuote(23,20,1,2),usd.FromSeller(20));Throws<ArgumentException>(()=>usd.FromBuyer(22));Equal(21L,usd.FromBuyer(22,false).BuyerPays);Equal(115L,usd.FromBuyer(115).BuyerPays);})),
            ("Server fee shape missing currency or fractional unsupported",()=>Sync(()=>{Throws<SteamException>(()=>SteamFees.Wallet("var g_rgWalletInfo={\"wallet_currency\":16};"));Throws<SteamException>(()=>SteamFees.Structure("{\"steam_fee\":{\"fee_rate\":0.05}}",new("KRW",100,100)));Throws<SteamException>(()=>SteamFees.Structure("{\"steam_fee\":{\"fee_rate\":0.0000001},\"publisher_fee\":{\"fee_rate\":0.1}}",new("KRW",100,100)));})),
            ("Same product across contexts shares one order depth",()=>Sync(()=>{var asset=Asset(10);var price=Price();var plan=SalePlanner.Build([new(asset,price,6),new(asset with { Key=new(730,"6","102") },price,6)],true);Equal(2,plan.UnpricedQuantity);Equal(10,plan.Lines.Sum(x=>x.Allocation.Quantity));})),
            ("Full synthetic QR UI to KRW stack split batch pending",async()=>{using var f=new Fixture();await f.Login();await f.Vm.LoadAsync();Equal(1,f.Vm.Assets.Count);f.Vm.Assets[0].Selected=true;f.Vm.Assets[0].Quantity=10;await f.Vm.RefreshPricesAsync();f.Vm.BuildReview();Equal("3,4,3",string.Join(',',f.Vm.ReviewRows.Select(x=>x.Quantity)));f.EnableTrading();await f.Vm.SubmitAsync();Equal(3,f.Server.Sends);Equal(0,f.Server.Free);Check(f.Vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard));Equal(0,f.Vm.Assets[0].Available);Equal(0,f.Workspace!.Store.Available(Asset(10)));} ),
            ("Live policy remains blocked even with user consent",async()=>{using var f=new Fixture(false);await f.Login();await f.Prepare(1);f.EnableTrading();Check(!f.Vm.Submit.CanExecute(null));await f.Vm.SubmitAsync();Equal(0,f.Server.Sends);} ),
            ("Server fee conflict stops remaining split requests",async()=>{using var f=new Fixture();await f.Login();await f.Prepare(10);f.EnableTrading();f.Server.FeeConflict=true;await f.Vm.SubmitAsync();Equal(1,f.Server.Sends);Equal(SaleState.Unknown,f.Vm.Results[0].State);Check(f.Vm.Results.Skip(1).All(x=>x.State==SaleState.Ready));Equal(7,f.Workspace!.Store.Available(Asset(10)));} ),
            ("Stack amount support is a separate policy capability",async()=>{using var f=new Fixture(true,false);await f.Login();await f.Prepare(3);f.EnableTrading();await f.Vm.SubmitAsync();Equal(0,f.Server.Sends);} ),
            ("Fresh wallet currency mismatch blocks submission",async()=>{using var f=new Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.Currency="USD";await f.Vm.SubmitAsync();Equal(0,f.Server.Sends);} ),
            ("Missing ID unique candidate never auto confirmed",async()=>{using var f=new Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.MissingId=true;await f.Vm.SubmitAsync();var key=f.Workspace!.Store.Commands().Single().RequestKey;var report=await f.Workspace.Executor.InspectAsync(key);Equal(SteamEvidenceReason.CandidateOnly,report.Verdict);var recovered=await f.Workspace.Batches.RecoverAsync();Check(recovered!.Results.Single().State!=SaleState.Listed);f.Vm.SelectedRequest=key;await f.Vm.InspectAsync();Check(f.Vm.EvidenceText.Contains("CandidateOnly"));Equal(1,f.Server.Sends);} ),
            ("Multiple coincidental candidates retain uncertainty",async()=>{using var f=new Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.MissingId=true;await f.Vm.SubmitAsync();f.Server.Rows.Add(("999",1,100));var report=await f.Workspace!.Executor.InspectAsync(f.Workspace.Store.Commands().Single().RequestKey);Equal(SteamEvidenceReason.MultipleCandidates,report.Verdict);Equal(1,f.Server.Sends);} ),
            ("Restart reserves uncertain stack without resend",async()=>{using var f=new Fixture();await f.Login();await f.Prepare(3);f.EnableTrading();f.Server.TimeoutSell=true;await f.Vm.SubmitAsync();Equal(1,f.Server.Sends);f.Vm.Logout.Execute(null);f.Server.TimeoutSell=false;await f.Login();Check(f.Workspace!.Store.Available(Asset(10))==7);await f.Vm.RecoverAsync();Equal(1,f.Server.Sends);Equal(SaleState.Unknown,f.Vm.Results.Single().State);} ),
            ("Confirmed cancellation return then one replacement",async()=>{using var f=new Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();f.Server.Publish=true;var listing=(await f.Workspace!.Orders.RefreshAsync()).Single();var result=await f.Workspace.Executor.RepriceAsync(listing,116,"reprice-a");Equal(SteamRepriceState.Submitted,result.State);Equal(1,f.Server.Cancels);Equal(2,f.Server.Sends);await f.Workspace.Executor.RepriceAsync(listing,116,"reprice-a");Equal(2,f.Server.Sends);f.Vm.Logout.Execute(null);await f.Login();f.EnableTrading();await f.Workspace!.Executor.RepriceAsync(listing,116,"reprice-a");Equal(2,f.Server.Sends);} ),
            ("Missing returned asset blocks reprice until explicit resume",async()=>{using var f=new Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();f.Server.Publish=true;var listing=(await f.Workspace!.Orders.RefreshAsync()).Single();f.Server.HideReturnedAsset=true;var result=await f.Workspace.Executor.RepriceAsync(listing,116,"reprice-b");Equal(SteamRepriceState.AwaitingReturn,result.State);Equal(1,f.Server.Sends);f.Server.HideReturnedAsset=false;await f.Workspace.Executor.RepriceAsync(listing,116,"reprice-b");Equal(2,f.Server.Sends);Equal(1,f.Server.Cancels);} ),
            ("Cancel unknown restart never re-cancels or relists",async()=>{using var f=new Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();f.Server.Publish=true;var listing=(await f.Workspace!.Orders.RefreshAsync()).Single();f.Server.TimeoutCancel=true;await Fails<SteamException>(()=>f.Workspace.Executor.RepriceAsync(listing,116,"reprice-c"));f.Vm.Logout.Execute(null);f.Server.TimeoutCancel=false;await f.Login();f.EnableTrading();await f.Workspace!.Executor.RepriceAsync(listing,116,"reprice-c");Equal(1,f.Server.Cancels);Equal(1,f.Server.Sends);} ),
            ("QR expiration denial cancellation and logout states",async()=>{using var f=new Fixture();f.Vm.RiskAccepted=f.Vm.CredentialsAccepted=true;await f.Vm.BeginAsync();Check(f.Vm.QrMatrix is not null);f.Server.Clock.Advance(121);await f.Vm.CheckAsync();Equal(SteamLoginState.Expired,f.Vm.LoginState);Check(f.Vm.QrMatrix is null);await f.Vm.BeginAsync();f.Server.Clock.Advance();f.Server.Denied=true;await f.Vm.CheckAsync();Equal(SteamLoginState.Denied,f.Vm.LoginState);f.Server.Denied=false;await f.Login();f.Vm.Logout.Execute(null);Check(!f.Vm.Connected && f.Vm.Assets.Count==0);} ),
            ("Logout during load discards late response and secrets",async()=>{using var f=new Fixture();await f.Login();f.Server.InventoryStarted=new(TaskCreationOptions.RunContinuationsAsynchronously);f.Server.InventoryRelease=new(TaskCreationOptions.RunContinuationsAsynchronously);var loading=f.Vm.LoadAsync();await f.Server.InventoryStarted.Task;f.Vm.Logout.Execute(null);f.Server.InventoryRelease.SetResult();await loading;Equal(SteamLoginState.Cancelled,f.Vm.LoginState);Equal(0,f.Vm.Assets.Count);Check(f.Vm.QrMatrix is null&&!f.Vm.Connected);} ),
            ("Consent revocation removes trading immediately",async()=>{using var f=new Fixture();await f.Login();f.EnableTrading();Check(f.Vm.CanTrade);f.Vm.TradeAccepted=false;Check(!f.Vm.CanTrade);f.Vm.CredentialsAccepted=false;Check(!f.Vm.Connected);Equal(0,f.Server.Sends);} ),
            ("Review editable totals exact fee and invalid amount",()=>Sync(()=>{var row=new SteamReviewRow(Asset(10),3,115,Price(),Krw(),()=>{});Equal(300L,row.Quote.SellerReceives*row.Quantity);row.PriceText="116";Equal(303L,row.Quote.SellerReceives*row.Quantity);row.PriceText="1.5";Check(!row.Valid);Equal(123L,SteamReviewRow.ParsePrice("1.23","USD"));Throws<ArgumentException>(()=>SteamReviewRow.ParsePrice("1.234","USD"));})),
            ("Stack cancellation cannot infer return from unrelated quantity growth",async()=>
            {
                using var f=new Fixture();await f.Login();await f.Prepare(10);f.EnableTrading();await f.Vm.SubmitAsync();f.Server.Publish=true;
                Throws<InvalidOperationException>(()=>f.Workspace!.Store.ArchiveCompleted());await f.Vm.RecoverAsync();
                var listing=(await f.Workspace!.Orders.RefreshAsync()).Single(x=>x.Quantity==4);await f.Workspace.Executor.CancelAsync(listing,"partial-cancel");Equal(SaleState.Unknown,(await f.Workspace.Executor.ReconcileCancellationAsync("partial-cancel")).State);Equal(0,f.Workspace.Store.Available(Asset(4)));
                f.Vm.ArchiveBatch.Execute(null);Check(await f.Workspace.Store.LoadAsync(default) is null);await f.Vm.LoadAsync();Equal(0,f.Vm.Assets.Single().Available);Check(!f.Vm.Assets.Single().CanSelect);Equal(3,f.Server.Sends);Equal(4,f.Server.Free);
            }),
            ("Steam v1 migration preserves legacy uncertain commands",async()=>
            {
                using var f=new Fixture();await f.Login();await f.Prepare(1);f.EnableTrading();f.Server.MissingId=true;await f.Vm.SubmitAsync();var key=f.Workspace!.Store.Commands().Single().RequestKey;f.Vm.Logout.Execute(null);
                using(var db=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(f.Directory,"steam-state.sqlite")};Pooling=False"))
                {
                    db.Open();using var read=db.CreateCommand();read.CommandText="SELECT value FROM state WHERE kind='command'";var node=System.Text.Json.Nodes.JsonNode.Parse((string)read.ExecuteScalar()!)!;foreach(var name in new[]{"SentAt","BeforeListingIds","Accepted"})node.AsObject().Remove(name);
                    using var write=db.CreateCommand();write.CommandText="UPDATE state SET value=$v WHERE kind='command'; PRAGMA user_version=1";write.Parameters.AddWithValue("$v",node.ToJsonString());write.ExecuteNonQuery();
                }
                await f.Login();Equal(9,f.Workspace!.Store.Available(Asset(10)));var report=await f.Workspace.Executor.InspectAsync(key);Equal(SteamEvidenceReason.NoCandidate,report.Verdict);Equal(1,f.Server.Sends);
            }),
            ("Expired reprice review never submits a replacement",async()=>
            {
                using var f=new Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();f.Server.Publish=true;var listing=(await f.Workspace!.Orders.RefreshAsync()).Single();await f.Workspace.Executor.CancelAsync(listing,"expired:cancel");
                f.Workspace.Store.SaveReprice(new("expired",f.Workspace.Authentication.SteamId!,listing,116,DateTimeOffset.UtcNow.AddMinutes(-6),SteamRepriceState.AwaitingReturn));
                await Fails<SteamException>(()=>f.Workspace.Executor.RepriceAsync(listing,116,"expired"));Equal(1,f.Server.Sends);Equal(1,f.Server.Cancels);
            }),
            ("Login challenge and network failure have separate UI states",async()=>
            {
                using var f=new Fixture();f.Vm.RiskAccepted=f.Vm.CredentialsAccepted=true;f.Server.RequireCaptcha=true;await f.Vm.BeginAsync();f.Server.Clock.Advance();await f.Vm.CheckAsync();Equal(SteamLoginState.AccessBlocked,f.Vm.LoginState);Check(f.Vm.QrMatrix is null);f.Server.RequireCaptcha=false;f.Server.AuthNetworkFailure=true;await f.Vm.BeginAsync();Equal(SteamLoginState.NetworkFailure,f.Vm.LoginState);Equal(0,f.Server.Sends);
            }),
            ("10000 adapter-backed rows bulk selection emits one total change",async()=>
            {
                using var f=new Fixture();f.Server.ManyAssets=10000;await f.Login();await f.Vm.LoadAsync();Equal(10000,f.Vm.Assets.Count);var changes=0;f.Vm.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(f.Vm.Totals))changes++;};f.Vm.SelectAll.Execute(null);Equal(1,changes);Equal(10000,f.Vm.Assets.Count(x=>x.Selected));Equal(0,f.Server.Sends);
            }),
        };
        int passed=0,failed=0;foreach(var(name,test)in tests){try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine($"FAIL {name}: {e}");}}return(passed,failed);
    }
    public sealed class Fixture:IDisposable
    {
        public string Directory{get;}=Path.Combine(System.IO.Directory.GetCurrentDirectory(),"runtime","stage5-"+Guid.NewGuid().ToString("N"));
        public SteamContractServer Server{get;}=new();public SteamExperimentalWorkspace? Workspace{get;private set;}public SteamWorkspaceViewModel Vm{get;}
        public Fixture(bool policy=true,bool stacks=true,bool medianContract=false){Vm=new(scope=>Workspace=new(Directory,scope.AppId,scope.ContextId,scope.Name,new(policy,"synthetic test only",stacks,medianContract),Server.Connection(),Server.Clock),true);}
        public async Task Login(){Vm.RiskAccepted=Vm.CredentialsAccepted=true;await Vm.BeginAsync();Server.Clock.Advance();await Vm.CheckAsync();Check(Vm.Connected);}
        public async Task Prepare(int quantity){await Vm.LoadAsync();Vm.Assets[0].Selected=true;Vm.Assets[0].Quantity=quantity;await Vm.RefreshPricesAsync();Vm.BuildReview();}
        public void EnableTrading(){Vm.TradeAccepted=true;Vm.EnableTrading.Execute(null);}
        public void Dispose(){Vm.Dispose();Server.Dispose();if(System.IO.Directory.Exists(Directory))System.IO.Directory.Delete(Directory,true);}
    }
    private static InventoryAsset Asset(int amount)=>new(new(730,"2","101"),"Counter-Strike 2","Synthetic stack","Synthetic stack",amount,amount>1,true,true);
    private static PriceSnapshot Price()=>new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[new(115,3),new(113,4),new(110,3)]);
    private static SteamFeeSchedule Krw()=>new("KRW",100,100,new(50000),new(100000));
    private static Task Sync(Action a){a();return Task.CompletedTask;}private static void Check(bool c){if(!c)throw new Exception("Assertion failed");}private static void Equal<T>(T a,T b){if(!EqualityComparer<T>.Default.Equals(a,b))throw new Exception($"Expected {a}, actual {b}");}
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}private static async Task Fails<T>(Func<Task> action)where T:Exception{try{await action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
}
