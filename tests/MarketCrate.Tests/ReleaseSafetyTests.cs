using System.Collections.Immutable;
using System.Text.Json;
using MarketCrate.Core;
using MarketCrate.Infrastructure;
using MarketCrate.Infrastructure.Steam;
using Microsoft.Data.Sqlite;
namespace MarketCrate.Tests;

public static class ReleaseSafetyTests
{
    private const string AccountA="76561198000000001",AccountB="76561198000000002";
    private static InventoryAsset Asset()=>new(new(730,"2","101"),"Synthetic","Synthetic","Synthetic",1,false,true,true);
    private static SaleBatchSnapshot Batch()=>new(Guid.NewGuid(),AccountA,DateTimeOffset.UtcNow,
        [new("line",Asset(),1,"KRW",new(115,100,5,10),DateTimeOffset.UtcNow)]);
    private static string Temp()=>Path.Combine(Directory.GetCurrentDirectory(),"runtime","release-safety-"+Guid.NewGuid().ToString("N"));
    private static void Check(bool value){if(!value)throw new Exception("Release safety contract");}
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{
            ("Same batch ID cannot acknowledge changed approved prices or another account",SnapshotIdentity),
            ("Completed own cancellation may open a NEW actual-asset review without local phantom holds",OwnedCancelNewReview),
            ("A different account intent cannot create local pending quantity on the current account",AccountPendingIsolation)
        };
        int passed=0,failed=0;
        foreach(var(name,test)in tests)try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}
        return(passed,failed);
    }
    private sealed class Executor:IAuthorizedListingExecutor
    {
        public int Calls;public Capability Capability=>Capability.Experimental;public bool CanExecute=>true;public bool CanReconcile=>true;
        public Task ValidateBatchAsync(SaleBatchSnapshot snapshot,CancellationToken token)=>Task.CompletedTask;
        public Task<LineResult> SubmitAsync(SaleLineSnapshot line,string key,CancellationToken token){Calls++;return Task.FromResult(new LineResult(line.LineId,SaleState.AwaitingSteamGuard,"Synthetic"));}
        public Task<LineResult> ReconcileAsync(SaleLineSnapshot line,string key,CancellationToken token)=>Task.FromResult(new LineResult(line.LineId,SaleState.Unknown,"Synthetic"));
    }
    private static async Task SnapshotIdentity()
    {
        var directory=Temp();var original=Batch();var executor=new Executor();
        using(var store=new SteamStateStore(directory))
        {
            var coordinator=new SaleBatchCoordinator(executor,store);await coordinator.ExecuteAsync(original);
            foreach(var changed in new[]{original with{AccountReference=AccountB},original with{Lines=[original.Lines[0] with{UnitQuote=new(116,100,6,10)}]}})
            {var rejected=false;try{await coordinator.ExecuteAsync(changed);}catch(InvalidDataException){rejected=true;}Check(rejected&&executor.Calls==1);}
            Check((await store.LoadAsync(default))!.Snapshot==original);
        }
        using(var reopened=new SteamStateStore(directory)){await new SaleBatchCoordinator(executor,reopened).ExecuteAsync(original);Check(executor.Calls==1);}
    }
    private static async Task OwnedCancelNewReview()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();await f.Prepare(1);f.EnableTrading();await f.Vm.SubmitAsync();
        f.Server.Publish=true;await f.Vm.LoadListingsAsync();var original=f.Workspace!.Store.Commands().Single();f.Vm.SelectAllListings.Execute(null);
        await f.Vm.PrepareListingRepriceAsync();await f.Vm.CancelForNewReviewAsync();await f.Vm.ReviewCancelledTargetsAsync();
        Check(f.Vm.ReviewOpen&&f.Vm.ReviewRows.Count==1&&f.Vm.ReviewRows[0].Asset.Key.AssetId=="101"&&f.Server.Sends==1&&f.Server.Cancels==1&&f.Workspace.Store.ReadCommand(original.RequestKey)==original);
        await f.Vm.SubmitAsync();Check(f.Server.Sends==2&&f.Server.Cancels==1);
    }
    private static async Task AccountPendingIsolation()
    {
        using var f=new FifthStageTests.Fixture();f.Server.Free=1;await f.Login();
        var line=Batch().Lines[0];f.Workspace!.Store.SaveCommand(new("other-account",AccountB,line,"sell",SaleState.Unknown,SentAt:DateTimeOffset.UtcNow.AddMinutes(-10)));
        await f.Vm.LoadAsync();Check(f.Vm.Assets.Single().PendingSubmissionQuantity==0&&!f.Vm.HasPendingInventoryRequests&&f.Vm.RequestOptions.Count==0);
        f.Workspace.Store.SaveCommand(new("own-account",AccountA,line,"sell",SaleState.Unknown,SentAt:DateTimeOffset.UtcNow.AddMinutes(-10)));
        await f.Vm.LoadAsync();Check(f.Vm.Assets.Single().PendingSubmissionQuantity==1&&f.Workspace.Store.Commands().Count==2&&f.Server.Sends==0);
    }
    // Separate test processes only. The identifier cannot designate a user/application data path.
    public static async Task<int> StorageCompatibility(string action,string identifier)
    {
        var id=Guid.ParseExact(identifier,"N");var root=Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),"runtime","release-storage-compat",id.ToString("N")));
        for(var check=new DirectoryInfo(root);check is not null;check=check.Parent)if(check.Exists&&(check.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("TestPathBoundary");
        var oldId=Guid.Parse("11111111-1111-1111-1111-111111111111");var newId=Guid.Parse("22222222-2222-2222-2222-222222222222");
        if(action=="old-write")
        {
            if(Directory.Exists(root))throw new IOException("New owned test path required");
            Directory.CreateDirectory(root);using var store=new SteamStateStore(Path.Combine(root,"Steam"));using var preferences=new UserPreferencesStore(Path.Combine(root,"Preferences"));
            var batch=Batch() with{BatchId=oldId};await store.SaveAsync(new(1,batch,[new("line",SaleState.Ready,"Synthetic")]),default);
            store.SaveCommand(new("legacy-unknown",AccountA,batch.Lines[0],"sell",SaleState.Unknown,SentAt:DateTimeOffset.UtcNow.AddMinutes(-10)));preferences.Save("reduced-motion",true);
        }
        else
        {
            using var store=new SteamStateStore(Path.Combine(root,"Steam"));using var preferences=new UserPreferencesStore(Path.Combine(root,"Preferences"));
            Check(store.ReadCommand("legacy-unknown") is{State:SaleState.Unknown,ListingId:null});
            if(action=="new-read-write")
            {
                Check(preferences.Read("reduced-motion")&&(await store.FindBatchAsync(oldId,default))is not null);
                var batch=Batch() with{BatchId=newId};await store.SaveAsync(new(1,batch,[new("line",SaleState.Ready,"Synthetic new provider")]),default);preferences.Save("reduced-motion",false);
            }
            else if(action=="old-read")
            {Check(!preferences.Read("reduced-motion")&&(await store.LoadAsync(default))!.Snapshot.BatchId==newId&&(await store.FindBatchAsync(oldId,default))is not null);}
            else throw new ArgumentException("Unknown compatibility action");
        }
        using var memory=new SqliteConnection("Data Source=:memory:");memory.Open();using var command=memory.CreateCommand();command.CommandText="SELECT sqlite_version()";
        Console.WriteLine(JsonSerializer.Serialize(new{Outcome="PASS",Action=action,Provider=typeof(SqliteConnection).Assembly.GetName().Version?.ToString(),SQLite=command.ExecuteScalar()?.ToString(),Runtime=Environment.Version.ToString(),SyntheticOnly=true}));
        return 0;
    }
}
