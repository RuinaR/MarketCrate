using System.Collections.Immutable;
using MarketCrate.Core;
using MarketCrate.Infrastructure;
using MarketCrate.App.ViewModels;
using Microsoft.Data.Sqlite;

internal static class NextStageTests
{
    public static async Task<(int Passed, int Failed)> RunAsync()
    {
        var tests = new (string, Func<Task>)[]
        {
            ("손상 저널 null 행/결과 안전 차단",()=>Sync(()=>
            {
                var ready=Ready(Batch());
                Throws<InvalidDataException>(()=>BatchRecordGuard.Validate(ready with { Results=ready.Results.SetItem(0,null!) }));
                Throws<InvalidDataException>(()=>BatchRecordGuard.Validate(ready with { Snapshot=ready.Snapshot with { Lines=ready.Snapshot.Lines.SetItem(0,null!) } }));
            })),
            ("SQLite 설정/보관함/저널/증거 재열기",async()=>
            {
                await InFolder(async folder=>
                {
                    var batch=Batch();
                    using(var db=new SqliteDemoStore(folder))
                    {
                        db.WriteSetting("grid","false"); db.SaveInventory([Asset()]);
                        var result=await new SaleBatchCoordinator(new MockListingExecutor(db),db).ExecuteAsync(batch);
                        Check(result.Results.All(x=>x.State==SaleState.AwaitingSteamGuard));
                        Throws<IOException>(()=>{ using var second=new SqliteDemoStore(folder); });
                    }
                    using(var db=new SqliteDemoStore(folder))
                    {
                        Check(db.ReadSetting("grid")=="false" && db.ReadInventory().Count==1);
                        var executor=new MockListingExecutor(db); var result=await new SaleBatchCoordinator(executor,db).RecoverAsync();
                        Check(executor.SubmitCount==0 && result!.Results.All(x=>x.State==SaleState.AwaitingSteamGuard));
                    }
                    Check(System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(Path.Combine(folder,"demo.sqlite")),0,15)=="SQLite format 3");
                });
            }),
            ("SQLite 트랜잭션 롤백/활성 저널 보존",async()=>
            {
                await InFolder(async folder=>
                {
                    using var db=new SqliteDemoStore(folder); var original=Ready(Batch()); await db.SaveAsync(original,default);
                    using var other=new SqliteConnection($"Data Source={Path.Combine(folder,"demo.sqlite")};Pooling=False"); other.Open();
                    using var command=other.CreateCommand(); command.CommandText="CREATE TRIGGER block_result BEFORE INSERT ON objects WHEN NEW.kind='result' AND NEW.id LIKE '%:1' BEGIN SELECT RAISE(ABORT,'synthetic failure'); END;"; command.ExecuteNonQuery();
                    await ThrowsAsync<SqliteException>(()=>db.SaveAsync(original with { Results=original.Results.SetItem(0,new("0",SaleState.Submitting,"intent")).SetItem(1,new("1",SaleState.Submitting,"intent")) },default));
                    Check((await db.LoadAsync(default))!.Results[0].State==SaleState.Ready);
                    command.CommandText="SELECT count(*) FROM objects WHERE kind='result'"; Check(Convert.ToInt32(command.ExecuteScalar())==0);
                });
            }),
            ("JSON 이관 1회/원본 보존/삭제 후 부활 금지",async()=>
            {
                await InFolder(async folder=>
                {
                    using(var legacy=new FileBatchJournal(folder)) await legacy.SaveAsync(Ready(Batch()),default);
                    var original=await File.ReadAllTextAsync(Path.Combine(folder,"demo-batch.json"));
                    using(var db=new SqliteDemoStore(folder)) { Check(await db.LoadAsync(default)!=null); db.ClearDemo(); }
                    using(var db=new SqliteDemoStore(folder)) { Check(await db.LoadAsync(default)==null); db.WriteSetting("grid","false"); db.DeleteDemoData(); }
                    using(var db=new SqliteDemoStore(folder)) { Check(await db.LoadAsync(default)==null && db.ReadSetting("grid")==null); }
                    Check(original==await File.ReadAllTextAsync(Path.Combine(folder,"demo-batch.json")));
                });
            }),
            ("미지원 SQLite 스키마 차단/lease 정리",async()=>
            {
                await InFolder(folder=>
                {
                    using(var connection=new SqliteConnection($"Data Source={Path.Combine(folder,"demo.sqlite")};Pooling=False"))
                    { connection.Open(); using var command=connection.CreateCommand(); command.CommandText="PRAGMA user_version=99;"; command.ExecuteNonQuery(); }
                    Throws<InvalidDataException>(()=>{ using var db=new SqliteDemoStore(folder); });
                    using var lease=new FileStream(Path.Combine(folder,"demo-batch.lock"),FileMode.Open,FileAccess.ReadWrite,FileShare.None);
                    return Task.CompletedTask;
                });
            }),
            ("응답 유실 후 재시작: Mock 증거 대조만, 재전송 0",async()=>
            {
                await InFolder(async folder=>
                {
                    var batch=Batch();
                    using(var db=new SqliteDemoStore(folder))
                    {
                        var losing=new LostAcknowledgement(new MockListingExecutor(db));
                        var result=await new SaleBatchCoordinator(losing,db).ExecuteAsync(batch);
                        Check(result.Results[0].State==SaleState.Unknown && result.Results[1].State==SaleState.Ready && losing.Calls==1);
                    }
                    using(var db=new SqliteDemoStore(folder))
                    {
                        var executor=new MockListingExecutor(db); var result=await new SaleBatchCoordinator(executor,db).RecoverAsync();
                        Check(executor.SubmitCount==0 && result!.Results[0].State==SaleState.AwaitingSteamGuard && result.Results[1].State==SaleState.Ready);
                    }
                });
            }),
            ("SQLite 확정 스냅샷/게시 결과 역행 차단",async()=>
            {
                await InFolder(async folder=>
                {
                    using var db=new SqliteDemoStore(folder); var ready=Ready(Batch()); await db.SaveAsync(ready,default);
                    await ThrowsAsync<InvalidDataException>(()=>db.SaveAsync(ready with { Snapshot=ready.Snapshot with { AccountReference="changed" } },default));
                    var submitting=ready with { Results=ready.Results.SetItem(0,new("0",SaleState.Submitting,"")) }; await db.SaveAsync(submitting,default);
                    var listed=submitting with { Results=submitting.Results.SetItem(0,new("0",SaleState.Listed,"")) }; await db.SaveAsync(listed,default);
                    await ThrowsAsync<InvalidDataException>(()=>db.SaveAsync(ready,default));
                    Check((await db.LoadAsync(default))!.Results[0].State==SaleState.Listed);
                });
            }),
            ("공유 호가 잔량/가격별 수량 보존/부족",()=>Sync(()=>
            {
                var selections=new[]{new SaleSelection(Asset(),Price(),6),new SaleSelection(Asset() with { Key=new(730,"2","second") },Price(),5)};
                var plan=SalePlanner.Build(selections,true);
                Check(plan.UnpricedQuantity==1 && plan.Lines.Sum(x=>x.Allocation.Quantity)==10);
                Check(plan.Lines.Where(x=>x.Allocation.BuyerPays==1000).Sum(x=>x.Allocation.Quantity)==3);
                Check(plan.Lines.Sum(x=>x.Allocation.BuyerPays*x.Allocation.Quantity)==9770);
                Throws<ArgumentException>(()=>SalePlanner.Build([selections[0],selections[0]],true));
                Throws<ArgumentException>(()=>SalePlanner.Build([selections[0],selections[1] with { Price=Price() with { Bids=[new(2000,10)] } }],true));
            })),
            ("공유 호가 수량 보존 속성 (50개 합성 케이스)",()=>Sync(()=>
            {
                for(var quantity=1;quantity<=50;quantity++)
                {
                    var selections=Enumerable.Range(0,quantity).Select(i=>new SaleSelection(Asset() with { Key=new(730,"2",$"s-{i}"),Quantity=1,Stackable=false },Price(),1));
                    var plan=SalePlanner.Build(selections,true); Check(plan.Lines.Sum(x=>x.Allocation.Quantity)+plan.UnpricedQuantity==quantity);
                    Check(plan.Lines.Sum(x=>x.Allocation.Quantity)<=10);
                }
            })),
            ("현재 자산 수량 감소/누락 차단",()=>Sync(()=>
            {
                var batch=Batch(); Throws<ArgumentException>(()=>SalePlanner.ValidateCurrent(batch,[Asset() with { Quantity=2 }]));
                Throws<ArgumentException>(()=>SalePlanner.ValidateCurrent(batch,[]));
                SalePlanner.ValidateCurrent(batch,[Asset()]);
            })),
            ("최종 명령 직전 수량/호가 변경은 제출 전 차단",async()=>
            {
                await InFolder(async folder=>
                {
                    using var db=new SqliteDemoStore(folder); var provider=new MutableProvider(); using var vm=new MainViewModel(provider,provider,db); await vm.InitializeAsync();
                    vm.SelectAll.Execute(null); vm.Review.Execute(null); provider.Quantity=0;
                    await Execute(vm,vm.Submit); Check(await db.LoadAsync(default)==null && vm.Status.Contains("수량"));
                    provider.Quantity=10; provider.ChangedPrice=true;
                    await Execute(vm,vm.Submit); Check(await db.LoadAsync(default)==null && vm.Status.Contains("가격"));
                });
            }),
            ("설정 재시작 복원/10,000개 필터 알림 1회",async()=>
            {
                await InFolder(async folder=>
                {
                    using var db=new SqliteDemoStore(folder);
                    using(var vm=new MainViewModel(new(),db)) { await vm.InitializeAsync(); vm.GridMode=false; vm.ReducedMotion=false; vm.SelectedGame="Dota 2"; }
                    using(var vm=new MainViewModel(new(),db))
                    {
                        await vm.InitializeAsync(); Check(!vm.GridMode && !vm.ReducedMotion && vm.SelectedGame=="Dota 2");
                        var notifications=0; vm.VisibleItems.CollectionChanged+=(_,_)=>notifications++;
                        vm.Search="Moon"; Check(notifications==1);
                    }
                });
            }),
            ("Mock 취소 개별/부분 실패/중복 검토 차단/재시작",async()=>
            {
                await InFolder(async folder=>
                {
                    var rows=new[]{new MockListing("a","A",1,1000,ListingState.Listed),new MockListing("demo-reject","B",2,980,ListingState.Listed)};
                    using(var db=new SqliteDemoStore(folder)) { db.SaveListings(rows); var result=MockCancellation.Execute(db,rows); Check(result[0].State==ListingState.Cancelled && result[1].State==ListingState.Failed); Throws<ArgumentException>(()=>MockCancellation.Execute(db,rows)); }
                    using(var db=new SqliteDemoStore(folder)) { Check(db.ReadListings()[0].State==ListingState.Cancelled); db.SaveListings([rows[0] with { State=ListingState.CancelRequested }]); Check(MockCancellation.Recover(db)[0].State==ListingState.Unknown); }
                });
            }),
            ("Mock 증거 없는 Unknown은 성공으로 만들지 않음",async()=>
            {
                await InFolder(async folder=>
                {
                    using var db=new SqliteDemoStore(folder); var record=Ready(Batch()); record=record with { Results=record.Results.SetItem(0,new("0",SaleState.Unknown,"lost")) };
                    await db.SaveAsync(record,default); var executor=new MockListingExecutor(db); var recovered=await new SaleBatchCoordinator(executor,db).RecoverAsync();
                    Check(executor.SubmitCount==0 && recovered!.Results[0].State==SaleState.Unknown);
                    Throws<InvalidOperationException>(()=>db.ClearDemo());
                });
            }),
            ("등록 결과→판매목록→Mock 취소→예약 해제 연결",async()=>
            {
                await InFolder(async folder=>
                {
                    using var db=new SqliteDemoStore(folder); var original=Ready(Batch());
                    var listed=original with { Results=original.Results.Select(x=>x with {State=SaleState.Listed}).ToImmutableArray() };
                    await db.SaveAsync(listed,default); MockOrderProjection.Update(db,listed);
                    Check(db.ReadListings().Count==2); MockCancellation.Execute(db,db.ReadListings());
                    MockOrderProjection.Update(db,listed); Check(db.ReadListings().All(x=>x.State==ListingState.Cancelled));
                    var next=Batch() with { Lines=[new("new",Asset(),10,"KRW",Pricing.FromBuyer(1000,new()),DateTimeOffset.UtcNow)] };
                    var cancelled=db.ReadListings().Where(x=>x.State==ListingState.Cancelled).Select(x=>x.ListingId).ToHashSet();
                    SalePlanner.ValidateReservations(next,db.ReadBatches(),cancelled);
                });
            }),
            ("게시 완료 후 새 배치도 자산 예약 수량 중복 차단",async()=>
            {
                await InFolder(async folder=>
                {
                    using var db=new SqliteDemoStore(folder); var original=Ready(Batch());
                    var listed=original with { Results=original.Results.Select(x=>x with { State=SaleState.Listed }).ToImmutableArray() };
                    await db.SaveAsync(listed,default); db.ClearDemo();
                    var next=Batch() with { Lines=[new("new",Asset(),8,"KRW",Pricing.FromBuyer(1000,new()),DateTimeOffset.UtcNow)] };
                    await ThrowsAsync<ArgumentException>(()=>new SaleBatchCoordinator(new MockListingExecutor(db),db).ExecuteAsync(next));
                    Check(await db.LoadAsync(default)==null && db.ReadBatches().Count==1);
                });
            })
        };
        var pass = 0; var fail = 0;
        foreach (var (name, test) in tests) { try { await test(); pass++; Console.WriteLine("PASS " + name); } catch (Exception e) { fail++; Console.WriteLine($"FAIL {name}: {e}"); } }
        return (pass, fail);
    }
    private static InventoryAsset Asset() => new(new(730, "2", "synthetic"), "Counter-Strike 2", "Synthetic Item", "Shared Hash", 10, true, true, false);
    private static PriceSnapshot Price() => new("KRW", DateTimeOffset.UtcNow, "Mock", TimeSpan.FromHours(24), [new(1000, 2)], [new(1000, 3), new(980, 4), new(950, 3)]);
    private static SaleBatchSnapshot Batch() => new(Guid.NewGuid(), "demo-account", DateTimeOffset.UtcNow, Enumerable.Range(0, 2).Select(i => new SaleLineSnapshot(i.ToString(), Asset(), 2, "KRW", Pricing.FromBuyer(1000, new()), DateTimeOffset.UtcNow)).ToImmutableArray());
    private static BatchRecord Ready(SaleBatchSnapshot batch) => new(1, batch, batch.Lines.Select(x => new LineResult(x.LineId, SaleState.Ready, "")).ToImmutableArray());
    private static Task Sync(Action test) { test(); return Task.CompletedTask; }
    private static void Check(bool value) { if (!value) throw new Exception("assertion failed"); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception($"expected {typeof(T).Name}"); }
    private static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception { try { await action(); } catch (T) { return; } throw new Exception($"expected {typeof(T).Name}"); }
    private static async Task InFolder(Func<string, Task> test)
    {
        var root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "runtime")); var folder = Path.Combine(root, "sqlite-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try { await test(folder); } finally { if (!Path.GetFullPath(folder).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(); Directory.Delete(folder, true); }
    }
    private static async Task Execute(MainViewModel vm, AsyncCommand command)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs args) { if (args.PropertyName == nameof(vm.Busy) && !vm.Busy) done.TrySetResult(); }
        vm.PropertyChanged += Changed;
        try { Check(command.CanExecute(null)); command.Execute(null); Check(await Task.WhenAny(done.Task, Task.Delay(3000)) == done.Task); }
        finally { vm.PropertyChanged -= Changed; }
    }
    private sealed class LostAcknowledgement(IMarketListingExecutor inner) : IMarketListingExecutor
    {
        public Capability Capability => Capability.Mock; public int Calls;
        public async Task<LineResult> SubmitAsync(SaleLineSnapshot line, string key, CancellationToken token) { Calls++; await inner.SubmitAsync(line, key, token); throw new TimeoutException("synthetic ack loss"); }
        public Task<LineResult> ReconcileAsync(SaleLineSnapshot line, string key, CancellationToken token) => inner.ReconcileAsync(line, key, token);
    }
    private sealed class MutableProvider : IInventoryProvider, IMarketDataProvider
    {
        public Capability Capability => Capability.Mock; public int Quantity = 10; public bool ChangedPrice;
        public Task<IReadOnlyList<InventoryAsset>> GetAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<InventoryAsset>>([Asset() with { Quantity = Quantity }]);
        public PriceSnapshot Get(InventoryAsset asset) => ChangedPrice ? Price() with { Bids = [new(2000, 10)] } : Price();
    }
}
