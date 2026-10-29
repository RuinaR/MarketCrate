using MarketCrate.Tests;
using System.Collections.Immutable;
using MarketCrate.Core;
using MarketCrate.Infrastructure;
using MarketCrate.App.ViewModels;

if(args.Contains("--recovery-failure")){await RecoveryFailure();return 0;}
if(args.Contains("--journal-recovery")){await JournalRecovery();return 0;}
if(args.Contains("--fees")){var focused=await FeeResponseTests.RunAsync();return focused.Failed==0?0:1;}
if(args.Contains("--live-only")){var focused=await LiveOnlyTests.RunAsync();return focused.Failed==0?0:1;}
if(args.Contains("--wallet")){var focused=await WalletRedirectPatchTests.RunAsync();return focused.Failed==0?0:1;}
if(args.Contains("--stage15")){var focused=await FifteenthStageTests.RunAsync();return focused.Failed==0?0:1;}
if(args.Contains("--stage14")){var focused=await FourteenthStageTests.RunAsync();return focused.Failed==0?0:1;}
if(args.Contains("--stage13")){var focused=await ThirteenthStageTests.RunAsync();return focused.Failed==0?0:1;}
if(args.Contains("--stage7")){var focused=await SeventhStageTests.RunAsync();Console.WriteLine($"STAGE7 RESULT {focused.Passed} PASS / {focused.Failed} FAIL");return focused.Failed==0?0:1;}
if(args.Contains("--stage6")){var focused=await SixthStageTests.RunAsync();Console.WriteLine($"STAGE6 RESULT {focused.Passed} PASS / {focused.Failed} FAIL");return focused.Failed==0?0:1;}

if (args.Length > 0 && args[0].StartsWith("--crash-", StringComparison.Ordinal)) return ThirdStageTests.CrashHelper(args);

var tests = new (string Name, Func<Task> Run)[] {
 ("호가 3/4/3 배분",()=>Sync(()=> { var p=Pricing.Allocate(10,[new(1000,3),new(980,4),new(950,3)]); Equal("3,4,3",string.Join(',',p.Lines.Select(x=>x.Quantity))); Equal(0,p.UnpricedQuantity); })),
 ("호가 부족/빈 주문",()=>Sync(()=> { Equal(4,Pricing.Allocate(7,[new(1000,3)]).UnpricedQuantity); Equal(7,Pricing.Allocate(7,[]).UnpricedQuantity); })),
 ("호가 정렬/잘못된 잔량",()=>Sync(()=> { Equal(1000L,Pricing.Allocate(1,[new(500,3),new(1000,2)]).Lines[0].BuyerPays); Throws<ArgumentException>(()=>Pricing.Allocate(2,[new(100,0)])); })),
 ("가중 평균/빈 표본/반올림",()=>Sync(()=> { Equal(175L,Pricing.WeightedAverage([new(100,1),new(200,3)])!.Value); Equal(2L,Pricing.WeightedAverage([new(1,1),new(2,1)])!.Value); Check(Pricing.WeightedAverage([])==null); })),
 ("원화 수수료/최소값/역산",()=>Sync(()=> { var q=Pricing.FromSeller(1000,new()); Equal(1150L,q.BuyerPays); Equal(50L,q.SteamFee); Equal(100L,q.GameFee); Equal(1000L,Pricing.FromBuyer(1150,new()).SellerReceives); Throws<ArgumentOutOfRangeException>(()=>Pricing.FromBuyer(2,new())); })),
 ("모든 정수 가격 수수료 보존",()=>Sync(()=> { for(long i=3;i<20000;i++) { var q=Pricing.FromBuyer(i,new()); Equal(i,checked(q.SellerReceives+q.SteamFee+q.GameFee)); Check(q.SellerReceives>0); } })),
 ("0% 게임 수수료/오버플로",()=>Sync(()=> { Equal(0L,Pricing.FromSeller(1,new(GameBasisPoints:0)).GameFee); Throws<OverflowException>(()=>Pricing.FromSeller(long.MaxValue,new())); })),
 ("배치 수량 상한/중복 행",()=>Sync(()=> { var b=Batch(); BatchValidation.Validate(b,DateTimeOffset.UtcNow); Throws<ArgumentException>(()=>BatchValidation.Validate(b with { Lines=[b.Lines[0] with { Quantity=11 }] },DateTimeOffset.UtcNow)); Throws<ArgumentException>(()=>BatchValidation.Validate(b with { Lines=[b.Lines[0],b.Lines[0]] },DateTimeOffset.UtcNow)); })),
 ("중복 자산 분할 총량/상충 자산",()=>Sync(()=> { var b=Batch(); var l=b.Lines[0]; Throws<ArgumentException>(()=>BatchValidation.Validate(b with { Lines=[l with { Quantity=6 },l with { LineId="other",Quantity=5 }] },DateTimeOffset.UtcNow)); Throws<ArgumentException>(()=>BatchValidation.Validate(b with { Lines=[l,l with { LineId="other",Asset=l.Asset with { Quantity=99 } }] },DateTimeOffset.UtcNow)); })),
 ("비판매/보류/비 스택",()=>Sync(()=> { foreach(var a in new[] { Asset() with { Marketable=false },Asset() with { Locked=true },Asset() with { Stackable=false } }) Throws<ArgumentException>(()=>BatchValidation.Validate(Batch() with { Lines=[Line("x",a,2)] },DateTimeOffset.UtcNow)); })),
 ("오래된 시세/미래 시세/통화",()=>Sync(()=> { var b=Batch(); foreach(var l in new[] { b.Lines[0] with { MarketTime=DateTimeOffset.UtcNow.AddMinutes(-6) },b.Lines[0] with { MarketTime=DateTimeOffset.UtcNow.AddHours(1) },b.Lines[0] with { Currency="EUR" } }) Throws<ArgumentException>(()=>BatchValidation.Validate(b with { Lines=[l] },DateTimeOffset.UtcNow)); })),
 ("한 번 승인 전체/분할 보존",async()=> { var j=new MemoryJournal(); var e=new TestExecutor(); var c=new SaleBatchCoordinator(e,j); var b=Batch(); var r=await c.ExecuteAsync(b); Equal(3,e.Calls); Equal("3,4,3",string.Join(',',r.Snapshot.Lines.Select(x=>x.Quantity))); Equal("1000,980,950",string.Join(',',r.Snapshot.Lines.Select(x=>x.UnitQuote.BuyerPays))); }),
 ("중복 클릭/새 배치 차단",async()=> { var j=new MemoryJournal(); var e=new TestExecutor(); var c=new SaleBatchCoordinator(e,j); var b=Batch(); await Task.WhenAll(c.ExecuteAsync(b),c.ExecuteAsync(b)); await ThrowsAsync<InvalidOperationException>(()=>c.ExecuteAsync(Batch())); Equal(3,e.Calls); }),
 ("부분 성공/실패/승인 대기",async()=> { var e=new TestExecutor { Outcomes=[SaleState.Listed,SaleState.Failed,SaleState.AwaitingSteamGuard] }; var r=await new SaleBatchCoordinator(e,new MemoryJournal()).ExecuteAsync(Batch()); Equal(SaleState.Failed,r.Results[1].State); Equal(SaleState.AwaitingSteamGuard,r.Results[2].State); Equal(3,e.Calls); }),
 ("타임아웃 중단/자동 재전송 금지",async()=> { var j=new MemoryJournal(); var e=new TestExecutor { Timeout=true }; var c=new SaleBatchCoordinator(e,j); var b=Batch(); var r=await c.ExecuteAsync(b); Equal(SaleState.Unknown,r.Results[0].State); Equal(SaleState.Ready,r.Results[1].State); await c.ExecuteAsync(b); Equal(1,e.Calls); await new SaleBatchCoordinator(e,j).RecoverAsync(); Equal(1,e.Calls); Equal(1,e.Lookups); }),
 ("재시작 Submitting은 대조만",async()=> { var b=Batch(); var j=new MemoryJournal { Record=new(1,b,[new("0",SaleState.Submitting,"intent"),new("1",SaleState.Ready,""),new("2",SaleState.Ready,"")]) }; var e=new TestExecutor(); await new SaleBatchCoordinator(e,j).RecoverAsync(); Equal(0,e.Calls); Equal(1,e.Lookups); }),
 ("선행 저널 저장 실패시 전송 0",async()=> { var e=new TestExecutor(); await ThrowsAsync<IOException>(()=>new SaleBatchCoordinator(e,new MemoryJournal { Fail=true }).ExecuteAsync(Batch())); Equal(0,e.Calls); }),
 ("Blocked 실제 쓰기 거절",async()=> { await ThrowsAsync<InvalidOperationException>(()=>new SaleBatchCoordinator(new BlockedListingExecutor(),new MemoryJournal()).ExecuteAsync(Batch())); }),
 ("취소된 배치 전송 0",async()=> { using var token=new CancellationTokenSource(); token.Cancel(); var e=new TestExecutor(); await ThrowsAsync<OperationCanceledException>(()=>new SaleBatchCoordinator(e,new MemoryJournal()).ExecuteAsync(Batch(),cancellationToken:token.Token)); Equal(0,e.Calls); }),
 ("불변 스냅샷 복사",()=>Sync(()=> { var original=Batch(); var edit=original.Lines[0] with { UnitQuote=Pricing.FromBuyer(2000,new()) }; var changed=original with { Lines=original.Lines.SetItem(0,edit) }; Equal(1000L,original.Lines[0].UnitQuote.BuyerPays); Equal(2000L,changed.Lines[0].UnitQuote.BuyerPays); })),
 ("파일 저널 재시작/독점/삭제",async()=> { var folder=TestFolder(); try { using(var j=new FileBatchJournal(folder)) { await new SaleBatchCoordinator(new TestExecutor(),j).ExecuteAsync(Batch()); Throws<IOException>(()=> { using var second=new FileBatchJournal(folder); }); } using(var j=new FileBatchJournal(folder)) { Equal(3,(await j.LoadAsync(default))!.Results.Length); var e=new TestExecutor(); await new SaleBatchCoordinator(e,j).RecoverAsync(); Equal(0,e.Calls); j.ClearDemo(); Check(await j.LoadAsync(default)==null); } } finally { Directory.Delete(folder,true); } }),
 ("손상/미지원 스키마 차단",async()=> { var folder=TestFolder(); try { using var j=new FileBatchJournal(folder); await File.WriteAllTextAsync(Path.Combine(folder,"demo-batch.json"),"{}"); await ThrowsAsync<Exception>(()=>j.LoadAsync(default)); } finally { Directory.Delete(folder,true); } }),
 ("MVVM 게임/검색/marketable/수량/검토",async()=> { var folder=TestFolder(); try { using var j=new FileBatchJournal(folder); using var vm=new MainViewModel(new(),j); await vm.InitializeAsync(); Equal(5,vm.VisibleItems.Count); vm.SelectedGame="Counter-Strike 2"; Equal(2,vm.VisibleItems.Count); vm.SelectAll.Execute(null); vm.VisibleItems[0].Quantity=999; Check(vm.VisibleItems[0].Quantity<=vm.VisibleItems[0].Maximum); vm.Review.Execute(null); Check(vm.IsReviewOpen); Check(vm.ReviewLines.Count>=2); vm.ReviewLines[0].Price=2345; Equal(2345L,vm.ReviewLines[0].Snapshot(0).UnitQuote.BuyerPays); vm.Search="does-not-exist"; Check(vm.HasNoItems); vm.CloseReview.Execute(null); vm.ClearSelection.Execute(null); Equal(0,vm.SelectedCount); } finally { Directory.Delete(folder,true); } }),
 ("MVVM 재시작 저널은 신규 실행 차단",JournalRecovery),
 ("복구 읽기 실패는 검토/저널 해제를 차단하고 명시적 복구 후만 허용",RecoveryFailure),
 ("MVVM 최종 명령 1회/더블 클릭/입력 오류",async()=> { var folder=TestFolder(); try { using var j=new FileBatchJournal(folder); using var vm=new MainViewModel(new(),j); await vm.InitializeAsync(); vm.SelectedGame="Counter-Strike 2"; vm.SelectAll.Execute(null); vm.Review.Execute(null); var count=vm.ReviewLines.Count; vm.HasInputErrors=true; Check(!vm.Submit.CanExecute(null)); vm.HasInputErrors=false; vm.ReviewLines[0].Price=2345; var complete=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); vm.PropertyChanged+=(_,e)=> { if(e.PropertyName==nameof(vm.Busy) && !vm.Busy) complete.TrySetResult(); }; vm.Submit.Execute(null); vm.Submit.Execute(null); Check(await Task.WhenAny(complete.Task,Task.Delay(5000))==complete.Task); Equal(count,vm.Results.Count); Equal(2345L,(await j.LoadAsync(default))!.Snapshot.Lines[0].UnitQuote.BuyerPays); Check(!vm.IsReviewOpen); Check(!vm.Submit.CanExecute(null)); } finally { Directory.Delete(folder,true); } })
};
var failed = 0;
foreach (var test in tests) { try { await test.Run(); Console.WriteLine($"PASS {test.Name}"); } catch (Exception e) { failed++; Console.WriteLine($"FAIL {test.Name}: {e}"); } }
var next = await NextStageTests.RunAsync();
failed += next.Failed;
var third = await ThirdStageTests.RunAsync(); failed += third.Failed;
var fourth = await FourthStageTests.RunAsync(); failed += fourth.Failed;
var fifth = await FifthStageTests.RunAsync(); failed += fifth.Failed;
var sixth=await SixthStageTests.RunAsync();failed+=sixth.Failed;
var seventh=await SeventhStageTests.RunAsync();failed+=seventh.Failed;
var eighth=await EighthStageTests.RunAsync();failed+=eighth.Failed;
var ninth=await NinthStageTests.RunAsync();failed+=ninth.Failed;
var tenth=await TenthStageTests.RunAsync();failed+=tenth.Failed;
var eleventh=await EleventhStageTests.RunAsync();failed+=eleventh.Failed;
var twelfth=await TwelfthStageTests.RunAsync();failed+=twelfth.Failed;
var thirteenth=await ThirteenthStageTests.RunAsync();failed+=thirteenth.Failed;
var fourteenth=await FourteenthStageTests.RunAsync();failed+=fourteenth.Failed;
var fifteenth=await FifteenthStageTests.RunAsync();failed+=fifteenth.Failed;
var walletPatch=await WalletRedirectPatchTests.RunAsync();failed+=walletPatch.Failed;
var liveOnly=await LiveOnlyTests.RunAsync();failed+=liveOnly.Failed;
var feeResponse=await FeeResponseTests.RunAsync();failed+=feeResponse.Failed;
var total = feeResponse.Passed+feeResponse.Failed+liveOnly.Passed+liveOnly.Failed+walletPatch.Passed+walletPatch.Failed+fifteenth.Passed+fifteenth.Failed+fourteenth.Passed+fourteenth.Failed+ thirteenth.Passed+thirteenth.Failed+ twelfth.Passed+twelfth.Failed+ eleventh.Passed+eleventh.Failed+tenth.Passed+tenth.Failed+ninth.Passed+ninth.Failed+eighth.Passed+eighth.Failed+seventh.Passed+seventh.Failed+sixth.Passed+sixth.Failed+tests.Length + next.Passed + next.Failed + third.Passed + third.Failed + fourth.Passed + fourth.Failed + fifth.Passed + fifth.Failed;
Console.WriteLine($"RESULT {total - failed}/{total} PASS; {failed} FAIL · synthetic data only, no GUI/network (Steam HTTP replaced by test handler)");
return failed == 0 ? 0 : 1;

static async Task RecoveryFailure()
{
    var journal=new RecoveryFaultJournal();using var vm=new MainViewModel(new(),journal);await vm.InitializeAsync();vm.SelectAll.Execute(null);
    Check(!vm.Review.CanExecute(null));Check(!vm.ClearDemo.CanExecute(null));Equal(0,journal.Writes);
    journal.Fail=false;await vm.InitializeAsync();vm.SelectAll.Execute(null);Check(vm.Review.CanExecute(null));Equal(0,journal.Writes);
}

static async Task JournalRecovery()
{
    var folder=TestFolder();var trace=new RecoveryTrace();using var capture=DiagnosticHub.Use(trace);
    try
    {
        using var journal=new FileBatchJournal(folder);await new SaleBatchCoordinator(new TestExecutor(),journal).ExecuteAsync(Batch());
        var before=await journal.LoadAsync(default);if(before?.Results.Length!=3)throw new Exception("Persisted journal missing before VM recovery");
        using var vm=new MainViewModel(new(),journal);await vm.InitializeAsync();vm.SelectAll.Execute(null);
        if(vm.Review.CanExecute(null)||vm.Results.Count!=3)throw new Exception("VM journal recovery did not block new review: "+System.Text.Json.JsonSerializer.Serialize(trace.Failures));
    }
    finally{Directory.Delete(folder,true);}
}

static InventoryAsset Asset() => new(new(730, "2", "synthetic"), "Synthetic Game", "Synthetic Item", "Synthetic Hash", 10, true, true, false);
static SaleLineSnapshot Line(string id, InventoryAsset asset, int count, long price = 1000) => new(id, asset, count, "KRW", Pricing.FromBuyer(price, new()), DateTimeOffset.UtcNow);
static SaleBatchSnapshot Batch() => new(Guid.NewGuid(), "demo-account", DateTimeOffset.UtcNow, [Line("0", Asset(), 3), Line("1", Asset(), 4, 980), Line("2", Asset(), 3, 950)]);
static Task Sync(Action action) { action(); return Task.CompletedTask; }
static void Check(bool condition) { if (!condition) throw new Exception("assertion failed"); }
static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"expected {expected}; actual {actual}"); }
static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception($"expected {typeof(T).Name}"); }
static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception { try { await action(); } catch (T) { return; } throw new Exception($"expected {typeof(T).Name}"); }
static string TestFolder() => Path.Combine(Directory.GetCurrentDirectory(), "runtime", "tests-" + Guid.NewGuid().ToString("N"));
sealed class MemoryJournal : IBatchJournal
{
    public BatchRecord? Record; public bool Fail;
    public Task<BatchRecord?> LoadAsync(CancellationToken token) => Task.FromResult(Record);
    public Task SaveAsync(BatchRecord record, CancellationToken token) { if (Fail) throw new IOException("synthetic storage failure"); Record = record; return Task.CompletedTask; }
}
sealed class TestExecutor : IMarketListingExecutor
{
    public Capability Capability => Capability.Mock;
    public int Calls; public int Lookups; public bool Timeout;
    public SaleState[] Outcomes = [SaleState.AwaitingSteamGuard, SaleState.Submitted, SaleState.Listed];
    public Task<LineResult> SubmitAsync(SaleLineSnapshot line, string key, CancellationToken token)
    { Calls++; if (Timeout) throw new TimeoutException(); return Task.FromResult(new LineResult(line.LineId, Outcomes[(Calls - 1) % Outcomes.Length], "synthetic")); }
    public Task<LineResult> ReconcileAsync(SaleLineSnapshot line, string key, CancellationToken token)
    { Lookups++; return Task.FromResult(new LineResult(line.LineId, SaleState.Reconciled, "synthetic server evidence")); }
}

sealed class RecoveryTrace : IDiagnosticLog
{
    public Guid SessionId {get;}=Guid.NewGuid();
    public List<DiagnosticRecord> Failures {get;}=[];
    public void Write(DiagnosticRecord record){if(record.Error!=DiagnosticError.None)Failures.Add(record);}
}
sealed class RecoveryFaultJournal : IDemoJournal
{
    public bool Fail=true;public int Writes;
    public Task<BatchRecord?> LoadAsync(CancellationToken token)=>Fail?Task.FromException<BatchRecord?>(new IOException("Synthetic recovery read failure")):Task.FromResult<BatchRecord?>(null);
    public Task SaveAsync(BatchRecord record,CancellationToken token){Writes++;return Task.CompletedTask;}
    public void ClearDemo(){throw new Exception("Unverified journal must not be cleared");}
}