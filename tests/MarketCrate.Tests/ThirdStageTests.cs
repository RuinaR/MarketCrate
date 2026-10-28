using System.Collections.Immutable;
using System.Diagnostics;
using MarketCrate.Core;
using MarketCrate.Infrastructure;
using MarketCrate.App.ViewModels;
using Microsoft.Data.Sqlite;

internal static class ThirdStageTests
{
    public static async Task<(int Passed, int Failed)> RunAsync()
    {
        var tests = new (string Name, Func<Task> Run)[]
        {
            ("검토 단가·수수료 즉시 합계/전략 변경 초기화",()=>InFolder(async folder=>
            {
                using var db=new SqliteDemoStore(folder); using var vm=new MainViewModel(new(),db); await vm.InitializeAsync();
                vm.SelectedGame="Counter-Strike 2"; vm.SelectAll.Execute(null); vm.Review.Execute(null);
                vm.ReviewLines[0].Price=2345; Check(vm.ReviewTotals.Contains("3,545"));
                Check(vm.ReviewLines[0].IsManual && vm.ReviewLines[0].SteamFee.Contains("Steam"));
                vm.SelectedStrategy=vm.Strategies[1]; Check(vm.IsReviewOpen && vm.ReviewLines.All(x=>!x.IsManual));
                vm.HasInputErrors=true; Check(vm.ReviewTotals.Contains("가격 오류") && !vm.Submit.CanExecute(null));
            })),
            ("부족 수량 별도 행/가격 없음/수동 가격 후 전체 배치",()=>InFolder(async folder=>
            {
                using var db=new SqliteDemoStore(folder); var provider=new ShortProvider(); using var vm=new MainViewModel(provider,provider,db);
                await vm.InitializeAsync(); vm.SelectAll.Execute(null); vm.Items[0].Quantity=12; vm.Review.Execute(null);
                Check(vm.IsReviewOpen && vm.ReviewLines.Where(x=>x.IsUnpriced).Sum(x=>x.Quantity)==2);
                Check(vm.ReviewLiquidity.Contains("2개") && !vm.Submit.CanExecute(null));
                vm.ReviewLines.Single(x=>x.IsUnpriced).PriceText="1400"; Check(vm.Submit.CanExecute(null));
                await Execute(vm,vm.Submit); Check((await db.LoadAsync(default))!.Snapshot.Lines.Sum(x=>x.Quantity)==12);
            })),
            ("수동 등록 재시도/시도 키/성공 행 보존/중복 승인 차단",()=>InFolder(async folder=>
            {
                using var db=new SqliteDemoStore(folder); var batch=Batch(); var executor=new MockListingExecutor(db); var c=new SaleBatchCoordinator(executor,db);
                var first=await c.ExecuteAsync(batch); Check(first.Results[0].State==SaleState.Failed);
                var approved=new Dictionary<string,int>{{"failed",0}};
                var retried=await c.RetryFailedAsync(approved,batch.Lines.Select(x=>x.Asset).ToArray(),DateTimeOffset.UtcNow);
                Check(retried.Results[0].Attempt==1 && retried.Results[0].State==SaleState.AwaitingSteamGuard);
                Check(retried.Results[1]==first.Results[1] && executor.SubmitCount==3);
                await ThrowsAsync<ArgumentException>(()=>c.RetryFailedAsync(approved,batch.Lines.Select(x=>x.Asset).ToArray(),DateTimeOffset.UtcNow));
                var fresh=new MockListingExecutor(db); var recovered=await new SaleBatchCoordinator(fresh,db).RecoverAsync();
                Check(fresh.SubmitCount==0 && recovered!.Results[0].Attempt==1 && recovered.Results[0].State==SaleState.AwaitingSteamGuard);
            })),
            ("불명/승인 대기/오래된 재시도 검토 거절",()=>InFolder(async folder=>
            {
                using var db=new SqliteDemoStore(folder); var batch=Batch(); var c=new SaleBatchCoordinator(new MockListingExecutor(db),db); await c.ExecuteAsync(batch);
                await ThrowsAsync<ArgumentException>(()=>c.RetryFailedAsync(new Dictionary<string,int>{{"ok",0}},batch.Lines.Select(x=>x.Asset).ToArray(),DateTimeOffset.UtcNow));
                await ThrowsAsync<ArgumentException>(()=>c.RetryFailedAsync(new Dictionary<string,int>{{"failed",0}},batch.Lines.Select(x=>x.Asset).ToArray(),DateTimeOffset.UtcNow.AddMinutes(-6)));
            })),
            ("VM 실패 항목 선택→단일 재시도 검토→대조",()=>InFolder(async folder=>
            {
                using var db=new SqliteDemoStore(folder); using var vm=new MainViewModel(new(),db); await vm.InitializeAsync();
                vm.SelectedGame="Counter-Strike 2"; vm.SelectAll.Execute(null); vm.Review.Execute(null); await Execute(vm,vm.Submit);
                vm.SelectFailedResults.Execute(null); vm.RetryFailedPlan.Execute(null); Check(vm.IsCancelReviewOpen && vm.ManagementRows.Count==1);
                await Execute(vm,vm.ConfirmCancellation); Check(vm.Results.Single(x=>x.Result.LineId=="line-1").Result.Attempt==1);
                await Execute(vm,vm.PublishMockEvidence); Check(vm.Results.All(x=>x.Result.State==SaleState.Listed));
            })),
            ("취소 실패 수동 재시도/중복 취소 차단",()=>InFolder(folder=>
            {
                using var db=new SqliteDemoStore(folder); var row=new MockListing("demo-reject","Synthetic",1,1000,ListingState.Listed); db.SaveListings([row]);
                var failed=MockCancellation.Execute(db,[row]).Single(); Check(failed.State==ListingState.Failed);
                var done=MockListingManagement.RetryFailed(db,[failed]).Single(); Check(done.State==ListingState.Cancelled && done.Attempt==2);
                Throws<ArgumentException>(()=>MockListingManagement.RetryFailed(db,[failed])); return Task.CompletedTask;
            })),
            ("가격 변경 취소→재등록/대기 상태/예약 수량 유지",()=>InFolder(folder=>
            {
                using var db=new SqliteDemoStore(folder); var row=new MockListing("original","Synthetic",3,1000,ListingState.Listed); db.SaveListings([row]);
                var changed=MockListingManagement.Reprice(db,[row],[1300]); var old=changed.Single(x=>x.ListingId=="original");
                var replacement=changed.Single(x=>x.ListingId==old.ReplacementId);
                Check(old.State==ListingState.Cancelled && old.BuyerPays==1000 && replacement.BuyerPays==1300 && replacement.Quantity==3 && replacement.State==ListingState.AwaitingSteamGuard);
                Check(!MockListingManagement.ReleasedRequests(changed).Contains("original"));
                db.SaveListings(changed.Select(x=>x.ListingId==replacement.ListingId?x with {State=ListingState.Listed}:x).ToArray());
                MockCancellation.Execute(db,[db.ReadListings().Single(x=>x.ListingId==replacement.ListingId)]);
                Check(MockListingManagement.ReleasedRequests(db.ReadListings()).Contains("original"));
                Throws<ArgumentException>(()=>MockListingManagement.Reprice(db,[row],[1400])); return Task.CompletedTask;
            })),
            ("취소 intent 중단은 Unknown, 자동 재취소 없음",()=>InFolder(folder=>
            {
                using var db=new SqliteDemoStore(folder); var row=new MockListing("original","Synthetic",1,1000,ListingState.Listed); db.SaveListings([row]);
                Throws<IOException>(()=>MockCancellation.Execute(new FaultStore(db,1),[row]));
                var unknown=MockCancellation.Recover(db).Single(); Check(unknown.State==ListingState.Unknown);
                Throws<ArgumentException>(()=>MockListingManagement.RetryFailed(db,[unknown])); return Task.CompletedTask;
            })),
            ("재가격 취소 직후 중단/명시적 미전송 재개/재중복 차단",()=>InFolder(folder=>
            {
                using var db=new SqliteDemoStore(folder); var row=new MockListing("original","Synthetic",1,1000,ListingState.Listed); db.SaveListings([row]);
                Throws<IOException>(()=>MockListingManagement.Reprice(new FaultStore(db,2),[row],[1200]));
                var cancelled=db.ReadListings().Single(); Check(MockListingManagement.CanResume(db.ReadListings(),cancelled));
                MockListingManagement.RetryFailed(db,[cancelled]); Check(db.ReadListings().Count==2);
                Throws<ArgumentException>(()=>MockListingManagement.RetryFailed(db,[cancelled])); return Task.CompletedTask;
            })),
            ("재등록 intent 중단은 Unknown/예약 유지/재전송 거절",()=>InFolder(folder=>
            {
                using var db=new SqliteDemoStore(folder); var row=new MockListing("original","Synthetic",1,1000,ListingState.Listed); db.SaveListings([row]);
                Throws<IOException>(()=>MockListingManagement.Reprice(new FaultStore(db,3),[row],[1200]));
                var recovered=MockCancellation.Recover(db); Check(recovered.Any(x=>x.State==ListingState.Unknown));
                Check(!MockListingManagement.ReleasedRequests(recovered).Contains("original"));
                Throws<ArgumentException>(()=>MockListingManagement.RetryFailed(db,[recovered.Single(x=>x.State==ListingState.Unknown)])); return Task.CompletedTask;
            })),
            ("SQLite v1→v2 설정 보존/업데이트 경로 독립",()=>InFolder(folder=>
            {
                using(var sql=Open(folder)) { using var cmd=sql.CreateCommand(); cmd.CommandText="CREATE TABLE objects(kind TEXT,id TEXT,value TEXT,PRIMARY KEY(kind,id)); INSERT INTO objects VALUES('setting','preserved','yes'); PRAGMA user_version=1;"; cmd.ExecuteNonQuery(); }
                using(var db=new SqliteDemoStore(folder)) Check(db.ReadSetting("preserved")=="yes");
                using(var sql=Open(folder)) { using var cmd=sql.CreateCommand(); cmd.CommandText="PRAGMA user_version"; Check(Convert.ToInt32(cmd.ExecuteScalar())==2); }
                return Task.CompletedTask;
            })),
            ("마이그레이션 실패 rollback/손상 DB 원본 보호",()=>InFolder(folder=>
            {
                using(var sql=Open(folder)) { using var cmd=sql.CreateCommand(); cmd.CommandText="CREATE TABLE objects(kind TEXT,id TEXT,value TEXT,PRIMARY KEY(kind,id)); INSERT INTO objects VALUES('setting','preserved','yes'); CREATE TABLE diagnostics(incompatible TEXT); PRAGMA user_version=1;"; cmd.ExecuteNonQuery(); }
                Throws<SqliteException>(()=> { using var db=new SqliteDemoStore(folder); });
                using(var sql=Open(folder)) { using var cmd=sql.CreateCommand(); cmd.CommandText="PRAGMA user_version"; Check(Convert.ToInt32(cmd.ExecuteScalar())==1); cmd.CommandText="SELECT value FROM objects WHERE id='preserved'"; Check((string)cmd.ExecuteScalar()! == "yes"); }
                var corrupt=Path.Combine(folder,"corrupt"); Directory.CreateDirectory(corrupt); File.WriteAllText(Path.Combine(corrupt,"demo.sqlite"),"SYNTHETIC NOT SQLITE");
                Throws<SqliteException>(()=> { using var db=new SqliteDemoStore(corrupt); });
                Check(File.ReadAllText(Path.Combine(corrupt,"demo.sqlite"))=="SYNTHETIC NOT SQLITE"); return Task.CompletedTask;
            })),
            ("구조화 진단에는 설정값/예외/아이템 문자열 없음",()=>InFolder(folder=>
            {
                using var db=new SqliteDemoStore(folder); db.WriteSetting("sample","SYNTHETIC-DO-NOT-LOG");
                db.RecordDiagnostic(DiagnosticCode.UiFailure); Check(db.ReadDiagnostics().All(x=>Enum.IsDefined(x.Code)));
                using(var sql=Open(folder)) { using var cmd=sql.CreateCommand(); cmd.CommandText="WITH RECURSIVE n(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM n WHERE x<505) INSERT INTO diagnostics SELECT '2026-10-28T00:00:00Z',0,0 FROM n;"; cmd.ExecuteNonQuery(); }
                db.RecordDiagnostic(DiagnosticCode.StoreOpened);
                using(var sql=Open(folder)) { using var cmd=sql.CreateCommand(); cmd.CommandText="SELECT count(*) FROM diagnostics"; Check(Convert.ToInt32(cmd.ExecuteScalar())==500); }
                SafeStartupDiagnostics.StorageFailure(folder); Check(!File.ReadAllText(Path.Combine(folder,"logs","startup.log")).Contains("SYNTHETIC")); return Task.CompletedTask;
            })),
            ("구형 취소 불명은 과거 등록 증거로 덮어쓰지 않음",()=>InFolder(async folder=>
            {
                using var db=new SqliteDemoStore(folder); var batch=Batch(); var listed=new BatchRecord(1,batch,batch.Lines.Select(x=>new LineResult(x.LineId,SaleState.Listed,"synthetic")).ToImmutableArray());
                var line=batch.Lines[0]; var id=SaleBatchCoordinator.RequestKey(batch,line);
                db.SaveListings([new(id,line.Asset.Name,line.Quantity,line.UnitQuote.BuyerPays,ListingState.Unknown)]);
                MockOrderProjection.Update(db,listed); Check(db.ReadListings().Single(x=>x.ListingId==id).State==ListingState.Unknown);
                await Task.CompletedTask;
            })),
            ("128행 배치 변경 행 저장/재열기/제한된 DB payload",()=>InFolder(async folder=>
            {
                var batch=new SaleBatchSnapshot(Guid.NewGuid(),"demo-account",DateTimeOffset.UtcNow,Enumerable.Range(0,128).Select(i=>new SaleLineSnapshot(i.ToString(),Asset("many-"+i),1,"KRW",Pricing.FromBuyer(1000,new()),DateTimeOffset.UtcNow)).ToImmutableArray());
                using(var db=new SqliteDemoStore(folder)) { var record=await new SaleBatchCoordinator(new MockListingExecutor(db),db).ExecuteAsync(batch); Check(record.Results.All(x=>x.State==SaleState.AwaitingSteamGuard)); }
                using(var db=new SqliteDemoStore(folder)) { var restored=await db.LoadAsync(default); Check(restored!.Results.Length==128 && restored.Results.All(x=>x.State==SaleState.AwaitingSteamGuard)); Check(db.ReadBatches().Single().Results.All(x=>x.State==SaleState.AwaitingSteamGuard)); }
                using var sql=Open(folder); using var cmd=sql.CreateCommand(); cmd.CommandText="SELECT sum(length(value)) FROM objects WHERE kind IN ('journal','batch','result')";
                Check(Convert.ToInt64(cmd.ExecuteScalar())<1_000_000);
            })),
            ("소유 headless 테스트 자식 종료 후 intent 복구",()=>CrashTest("--crash-intent")),
            ("소유 headless 테스트 자식 미커밋 종료 후 SQLite 복구",()=>CrashTest("--crash-transaction"))
        };
        var pass = 0; var fail = 0;
        foreach (var test in tests) { try { await test.Run(); pass++; Console.WriteLine("PASS " + test.Name); } catch (Exception e) { fail++; Console.WriteLine($"FAIL {test.Name}: {e}"); } }
        return (pass, fail);
    }
    internal static int CrashHelper(string[] args)
    {
        using var db = new SqliteDemoStore(args[1]);
        if (args[0] == "--crash-intent")
        { var batch = Batch(); db.SaveAsync(new(1, batch, batch.Lines.Select(x => new LineResult(x.LineId, SaleState.Submitting, "synthetic interrupted intent")).ToImmutableArray()), default).GetAwaiter().GetResult(); }
        else
        {
            db.WriteSetting("stable", "before"); using var sql = Open(args[1]); using var tx = sql.BeginTransaction(); using var cmd = sql.CreateCommand(); cmd.Transaction = tx;
            cmd.CommandText = "UPDATE objects SET value='uncommitted' WHERE kind='setting' AND id='stable'"; cmd.ExecuteNonQuery();
            Environment.Exit(72); // This project-owned helper only. No disposal/commit, no GUI or other processes touched.
        }
        Environment.Exit(71); return 71;
    }
    private static Task CrashTest(string mode) => InFolder(async folder =>
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Directory.GetCurrentDirectory() };
        start.ArgumentList.Add(typeof(ThirdStageTests).Assembly.Location); start.ArgumentList.Add(mode); start.ArgumentList.Add(folder);
        using var child = Process.Start(start) ?? throw new Exception("owned helper did not start");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15)); await child.WaitForExitAsync(timeout.Token);
        Check(child.ExitCode == (mode == "--crash-intent" ? 71 : 72));
        using var db = new SqliteDemoStore(folder);
        if (mode == "--crash-intent") { var executor = new MockListingExecutor(db); var recovered = await new SaleBatchCoordinator(executor, db).RecoverAsync(); Check(executor.SubmitCount == 0 && recovered!.Results.All(x => x.State == SaleState.Unknown)); }
        else Check(db.ReadSetting("stable") == "before");
    });
    private static SqliteConnection Open(string folder) { var sql = new SqliteConnection($"Data Source={Path.Combine(folder, "demo.sqlite")};Pooling=False;Default Timeout=2"); sql.Open(); return sql; }
    private static InventoryAsset Asset(string id) => new(new(730, "2", id), "Counter-Strike 2", "Synthetic", "Synthetic " + id, 20, true, true, true);
    private static SaleBatchSnapshot Batch() => new(Guid.NewGuid(), "demo-account", DateTimeOffset.UtcNow, [new("failed", Asset("demo-2"), 1, "KRW", Pricing.FromBuyer(1000, new()), DateTimeOffset.UtcNow), new("ok", Asset("demo-1"), 1, "KRW", Pricing.FromBuyer(1000, new()), DateTimeOffset.UtcNow)]);
    private static void Check(bool ok) { if (!ok) throw new Exception("assertion failed"); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception($"expected {typeof(T).Name}"); }
    private static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception { try { await action(); } catch (T) { return; } throw new Exception($"expected {typeof(T).Name}"); }
    private static async Task InFolder(Func<string, Task> action)
    { var root = Path.GetFullPath("runtime"); var folder = Path.Combine(root, "stage3-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); try { await action(folder); } finally { if (!Path.GetFullPath(folder).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(); Directory.Delete(folder, true); } }
    private static async Task Execute(MainViewModel vm, AsyncCommand command)
    {
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Changed(object? s, System.ComponentModel.PropertyChangedEventArgs e) { if (e.PropertyName == nameof(vm.Busy) && !vm.Busy) complete.TrySetResult(); }
        vm.PropertyChanged += Changed; try { Check(command.CanExecute(null)); command.Execute(null); Check(await Task.WhenAny(complete.Task, Task.Delay(5000)) == complete.Task); } finally { vm.PropertyChanged -= Changed; }
    }
    private sealed class ShortProvider : IInventoryProvider, IMarketDataProvider
    {
        public Capability Capability => Capability.Mock;
        public Task<IReadOnlyList<InventoryAsset>> GetAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<InventoryAsset>>([Asset("shortage")]);
        public PriceSnapshot Get(InventoryAsset asset) => new("KRW", DateTimeOffset.UtcNow, "합성 Mock", TimeSpan.FromHours(24), [new(1100, 10)], [new(1000, 3), new(980, 4), new(950, 3)]);
    }
    private sealed class FaultStore(IDemoDataStore inner, int failAfter) : IDemoDataStore
    {
        private int calls;
        public void SaveListings(IReadOnlyList<MockListing> rows) { inner.SaveListings(rows); if (++calls == failAfter) throw new IOException("synthetic interruption"); }
        public IReadOnlyList<MockListing> ReadListings() => inner.ReadListings(); public string? ReadSetting(string k) => inner.ReadSetting(k); public void WriteSetting(string k, string v) => inner.WriteSetting(k, v);
        public void SaveInventory(IReadOnlyList<InventoryAsset> a) => inner.SaveInventory(a); public IReadOnlyList<InventoryAsset> ReadInventory() => inner.ReadInventory();
        public LineResult? ReadEvidence(string k) => inner.ReadEvidence(k); public void WriteEvidence(string k, LineResult r) => inner.WriteEvidence(k, r); public IReadOnlyList<BatchRecord> ReadBatches() => inner.ReadBatches();
    }
}
