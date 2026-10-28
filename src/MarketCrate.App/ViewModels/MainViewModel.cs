using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using MarketCrate.Core;
using MarketCrate.Infrastructure;

namespace MarketCrate.App.ViewModels;

public sealed class MainViewModel : Observable, IDisposable
{
    public DiagnosticsViewModel Diagnostics {get;}
    public SteamWorkspaceViewModel Steam { get; }
    private IInventoryProvider inventoryProvider;
    private IMarketDataProvider marketProvider;
    private readonly IDemoJournal journal;
    private readonly IDemoDataStore? data;
    private readonly SaleBatchCoordinator coordinator;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? operation;
    private BatchRecord? record;
    private bool bulkSelection;
    private MockListing[] cancellationSnapshot = [];
    private enum ManagementMode { Cancel, Reprice, RetryListing, RetryBatch }
    private ManagementMode managementMode;
    private Dictionary<string, int> retryApproval = [];
    public ObservableCollection<ManagementRowViewModel> ManagementRows { get; } = [];
    public RangeObservableCollection<ItemViewModel> Items { get; } = [];
    public RangeObservableCollection<ItemViewModel> VisibleItems { get; } = [];
    public ObservableCollection<ReviewLineViewModel> ReviewLines { get; } = [];
    public ObservableCollection<ResultViewModel> Results { get; } = [];
    public ObservableCollection<string> History { get; } = [];
    public ObservableCollection<ListingViewModel> Listings { get; } = [];
    public string[] Games { get; } = ["모든 게임", "Counter-Strike 2", "Dota 2", "Team Fortress 2"];
    public string[] Strategies { get; } = ["구매주문 기반 예상가", "24시간 거래량 가중 평균가"];
    public string[] Sorts { get; } = ["이름순", "평균가 높은순"];
    public Command SelectAll { get; }
    public Command ClearSelection { get; }
    public Command Review { get; }
    public Command CloseReview { get; }
    public Command Cancel { get; }
    public Command CancelPlan { get; }
    public Command SelectAllListings { get; }
    public Command ClearListings { get; }
    public Command RepricePlan { get; }
    public Command RetryListingPlan { get; }
    public Command SelectFailedResults { get; }
    public Command RetryFailedPlan { get; }
    public Command RefreshReviewStatus { get; }
    public Command CloseCancelReview { get; }
    public Command ClearDemo { get; }
    public AsyncCommand Refresh { get; }
    public AsyncCommand Submit { get; }
    public AsyncCommand Recover { get; }
    public AsyncCommand ConfirmCancellation { get; }
    public AsyncCommand PublishMockEvidence { get; }
    public AsyncCommand LoadLargeDemo { get; }
    public Command DeleteLocalDemoData { get; }
    public MainViewModel(DemoDataProvider provider, IDemoJournal journal, bool offlineOnly = false) : this(provider, provider, journal, offlineOnly) { }
    public MainViewModel(IInventoryProvider inventory, IMarketDataProvider market, IDemoJournal journal, bool offlineOnly = false)
    {
        Steam = new(offlineOnly: offlineOnly);
        inventoryProvider = inventory; marketProvider = market; this.journal = journal; data = journal as IDemoDataStore;
        Diagnostics=new(DiagnosticHub.Current as MarketCrate.Infrastructure.Diagnostics.JsonDiagnosticLog,saveMode:value=>Persist("diagnostic-mode",value?"true":"false"));
        if(data?.ReadSetting("diagnostic-mode")=="true")Diagnostics.DiagnosticMode=true;
        if (data?.ReadSetting("demo-count") == "10000" && inventory is DemoDataProvider)
        { var large = new DemoDataProvider(10000); inventoryProvider = large; marketProvider = large; }
        gridMode = data?.ReadSetting("grid") != "false"; reducedMotion = data?.ReadSetting("motion") != "false";
        var savedGame = data?.ReadSetting("game"); if (savedGame != null && Games.Contains(savedGame)) game = savedGame;
        var savedStrategy = data?.ReadSetting("strategy"); if (savedStrategy != null && Strategies.Contains(savedStrategy)) strategy = savedStrategy;
        coordinator = new(new MockListingExecutor(data), journal);
        SelectAll = new(() => SetSelection(VisibleItems, true), () => !Busy && !AnyDialog);
        ClearSelection = new(() => SetSelection(Items, false), () => !Busy && !AnyDialog);
        Review = new(BuildReview, () => !Busy && !AnyDialog && !HasInputErrors && SelectedCount > 0 && record == null);
        CloseReview = new(() => IsReviewOpen = false, () => !Busy);
        Cancel = new(() => operation?.Cancel(), () => Busy);
        SelectAllListings = new(() => { foreach (var row in Listings) row.Selected = row.CanCancel; }, () => !Busy && !AnyDialog);
        ClearListings = new(() => { foreach (var row in Listings) row.Selected = false; }, () => !Busy && !AnyDialog);
        CancelPlan = new(() => OpenManagement(ManagementMode.Cancel), () => CanManage(false));
        RepricePlan = new(() => OpenManagement(ManagementMode.Reprice), () => CanManage(false));
        RetryListingPlan = new(() => OpenManagement(ManagementMode.RetryListing), () => CanManage(true));
        SelectFailedResults = new(() => { foreach (var result in Results) result.Selected = result.CanRetry; }, () => !Busy && !AnyDialog);
        RetryFailedPlan = new(() => OpenManagement(ManagementMode.RetryBatch), () => !Busy && !AnyDialog && Results.Any(x => x.Selected && x.CanRetry));
        RefreshReviewStatus = new(() => { foreach (var row in ReviewLines) row.RefreshFreshness(); Raise(nameof(ReviewLiquidity)); }, () => !Busy);
        CloseCancelReview = new(() => IsCancelReviewOpen = false, () => !Busy);
        ConfirmCancellation = new(CancelListingsAsync, Error, () => !Busy && !HasInputErrors && IsCancelReviewOpen && ManagementRows.Count > 0 && ManagementRows.All(x => x.Valid));
        PublishMockEvidence = new(PublishMockAsync, Error, () => !Busy && !AnyDialog && data != null && (record != null || Listings.Any(x => x.Listing.Action == ListingAction.Reprice && x.Listing.State == ListingState.AwaitingSteamGuard)));
        LoadLargeDemo = new(async () => { var large = new DemoDataProvider(10000); inventoryProvider = large; marketProvider = large; Persist("demo-count", "10000"); await LoadAsync(); }, Error, () => !Busy && !AnyDialog);
        Refresh = new(LoadAsync, Error, () => !Busy && !AnyDialog);
        Submit = new(SubmitAsync, Error, () => !Busy && !HasInputErrors && IsReviewOpen && ReviewLines.Count > 0 && ReviewLines.All(x => x.Valid) && record == null);
        Recover = new(RecoverAsync, Error, () => !Busy && !AnyDialog);
        ClearDemo = new(() => Safe(() => { journal.ClearDemo(); record = null; Results.Clear(); History.Clear(); Status = "활성 Mock 배치 해제. 이전 요청 증거·예약 수량은 보존됩니다."; Update(); }), () => !Busy && !AnyDialog && (record == null || record.Results.All(x => x.State is SaleState.Ready or SaleState.Listed or SaleState.Failed)));
        DeleteLocalDemoData = new(() => Safe(() => { if (journal is SqliteDemoStore sql) sql.DeleteDemoData(); record = null; Items.Clear(); VisibleItems.Clear(); Results.Clear(); Listings.Clear(); History.Clear(); ReviewLines.Clear(); Status = "SQLite 합성 데이터·설정·저널 삭제 완료. 이관 원본 JSON은 보존됩니다."; Raise(nameof(HasNoItems)); Update(); }), () => journal is SqliteDemoStore && !Busy && !IsReviewOpen && !IsCancelReviewOpen);
    }
    private string search = "", game = "모든 게임", sort = "이름순", strategy = "구매주문 기반 예상가";
    private bool marketableOnly = true, gridMode = true, reviewOpen, busy, inputErrors, cancelReviewOpen, reducedMotion = true;
    private string status = "Mock 전용 · Steam 연결 없음 · 실등록 IntegrationBlocked";
    public bool HasInputErrors { get => inputErrors; set { if (Set(ref inputErrors, value)) { Raise(nameof(ReviewTotals)); Update(); } } }
    public string Search { get => search; set { if (Set(ref search, value)) Filter(); } }
    public string SelectedGame { get => game; set { if (Set(ref game, value)) { Persist("game", value); Filter(); } } }
    public string SelectedSort { get => sort; set { if (Set(ref sort, value)) Filter(); } }
    public string SelectedStrategy { get => strategy; set { if (Set(ref strategy, value)) { var reopen = IsReviewOpen; IsReviewOpen = false; ReviewLines.Clear(); Persist("strategy", value); if (reopen) BuildReview(); Status = PriceEditRule; Update(); } } }
    public bool MarketableOnly { get => marketableOnly; set { if (Set(ref marketableOnly, value)) Filter(); } }
    public bool GridMode { get => gridMode; set { if (Set(ref gridMode, value)) Persist("grid", value ? "true" : "false"); } }
    public bool ReducedMotion { get => reducedMotion; set { if (Set(ref reducedMotion, value)) Persist("motion", value ? "true" : "false"); } }
    public bool IsReviewOpen { get => reviewOpen; set { if (Set(ref reviewOpen, value)) Update(); } }
    public bool IsCancelReviewOpen { get => cancelReviewOpen; private set { if (Set(ref cancelReviewOpen, value)) Update(); } }
    public bool AnyDialog => IsReviewOpen || IsCancelReviewOpen;
    public string PriceEditRule => "수동 단가는 현재 검토에서만 유효합니다. 전략 변경·검토 닫기·갱신 후에는 새 추천가로 초기화합니다.";
    public string ReviewLiquidity => $"매수 잔량 부족 {ReviewLines.Where(x => x.IsUnpriced).Sum(x => x.Quantity)}개 · 해당 수량은 즉시 체결 추정 불가. 직접 단가를 입력해도 실제 체결은 보장하지 않습니다.";
    public string ManagementTitle => managementMode switch { ManagementMode.Reprice => "판매 가격 변경 검토 / MOCK", ManagementMode.RetryBatch => "확정 실패 판매등록 재시도 / MOCK", ManagementMode.RetryListing => "확정 실패 관리 작업 재시도 / MOCK", _ => "선택 판매 취소 검토 / MOCK" };
    public string ManagementAction => managementMode switch { ManagementMode.Reprice => "선택 Mock 가격 변경", ManagementMode.RetryBatch => "선택 실패 Mock 등록 재시도", ManagementMode.RetryListing => "선택 실패 Mock 작업 재시도", _ => "선택 Mock 판매 취소" };
    public string ManagementNote => managementMode switch { ManagementMode.Reprice => "원래 등록 취소 확인 후 새 가격으로 Mock 재등록합니다. 원자적 거래가 아니며 부분 실패/승인 대기가 생길 수 있습니다.", ManagementMode.RetryBatch => "확정 실패한 항목만 원래 가격·수량으로 다시 검증합니다. 성공/불명/승인 대기는 재전송하지 않습니다.", _ => "실제 Steam 요청 없음. 확정 실패만 재시도 가능하며 불명 상태는 먼저 대조해야 합니다." };
    public string DiagnosticsText => journal is IDiagnosticStore log ? string.Join("\n", log.ReadDiagnostics().Take(12).Select(x => $"{x.At.ToOffset(TimeSpan.FromHours(9)):HH:mm:ss} KST · {x.Code} · {x.Count}")) : "진단 저장소 없음";
    public bool Busy { get => busy; private set { Set(ref busy, value); Update(); } }
    public string Status { get => status; private set => Set(ref status, value); }
    public bool HasNoItems => VisibleItems.Count == 0;
    public string CancellationSummary => string.Join("\n", cancellationSnapshot.Select(x => $"{x.Name} · {x.Quantity}개 × ₩{x.BuyerPays:N0}"));
    public int SelectedCount => Items.Where(x => x.Selected).Sum(x => x.Quantity);
    public string SelectionSummary => $"{Items.Count(x => x.Selected)}개 자산 · {SelectedCount}개 수량";
    public string Estimate
    {
        get
        {
            if (IsReviewOpen) return ReviewLines.All(x => x.Valid) && !HasInputErrors ? $"₩{ReviewLines.Sum(x => x.Price * x.Quantity):N0}" : "가격 입력 확인 필요";
            try { var plan = CreatePlan(); var total = plan.Lines.Sum(x => checked(x.Allocation.BuyerPays * x.Allocation.Quantity)); return $"₩{total:N0}" + (plan.UnpricedQuantity > 0 ? $" · {plan.UnpricedQuantity}개 추정 불가" : ""); }
            catch (ArgumentException) { return "시세/수량 재검토 필요"; }
        }
    }
    public string ReviewTotals => !HasInputErrors && ReviewLines.All(x => x.Valid) ? $"구매자 지불 ₩{ReviewLines.Sum(x => x.Price * x.Quantity):N0}   /   수수료 ₩{ReviewLines.Sum(x => x.Quote.Fees * x.Quantity):N0}   /   실수령 ₩{ReviewLines.Sum(x => x.Quote.SellerReceives * x.Quantity):N0}" : "가격 오류를 수정하세요 (3 ~ 1,000,000,000원)";
    public string ProgressSummary => record == null ? "아직 실행한 Mock 배치가 없습니다." : $"Mock 배치 {record.Snapshot.BatchId.ToString()[..8]} · 총 {record.Results.Length}건 / 게시 확인 {record.Results.Count(x => x.State == SaleState.Listed)} / 승인 대기 {record.Results.Count(x => x.State == SaleState.AwaitingSteamGuard)} / 실패 {record.Results.Count(x => x.State == SaleState.Failed)} / 불명 {record.Results.Count(x => x.State == SaleState.Unknown)}";
    public async Task InitializeAsync() { try { await LoadAsync(); await RecoverAsync(); } catch (Exception e) { Error(e); } }
    private async Task LoadAsync()
    {
        Busy = true;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); operation = cancellation;
        try
        {
            var assets = await inventoryProvider.GetAsync(cancellation.Token);
            data?.SaveInventory(assets);
            Items.ReplaceAll(assets.Select(asset => new ItemViewModel(asset, marketProvider.Get(asset), Update)));
            LoadListings(); Filter(); Status = $"Mock {assets.Count:N0}개 자산 갱신 · 합성 24시간 표본 · Steam 요청 없음";
        }
        finally { operation = null; Busy = false; }
    }
    private void LoadListings()
    {
        IReadOnlyList<MockListing> values = data == null ? [] : MockCancellation.Recover(data);
        if (values.Count == 0) { values = [new("demo-listed", "Prism Collection • Case", 1, 1200, ListingState.Listed), new("demo-reject", "Cobalt Circuit • Sticker", 2, 1180, ListingState.Listed), new("demo-pending", "Moonlit Archive • Treasure", 1, 650, ListingState.AwaitingSteamGuard)]; data?.SaveListings(values); }
        Listings.Clear(); foreach (var value in values) Listings.Add(new(value, RefreshManagement, MockListingManagement.CanResume(values, value)));
    }
    private void Filter()
    {
        var filtered = Items.Where(x => (game == "모든 게임" || x.Game == game) && (!marketableOnly || x.CanSelect) && x.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        filtered = sort == "평균가 높은순" ? filtered.OrderByDescending(x => Pricing.WeightedAverage(x.Price.Trades)) : filtered.OrderBy(x => x.Name);
        VisibleItems.ReplaceAll(filtered); Raise(nameof(HasNoItems));
    }
    private SalePlan CreatePlan() => SalePlanner.Build(Items.Where(x => x.Selected).Select(x => new SaleSelection(x.Asset, x.Price, x.Quantity)), strategy == Strategies[0]);
    private void BuildReview()
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.ReviewOpened,DiagnosticArea.Pricing,"E01");
        try
        {
            ReviewLines.Clear(); var plan = CreatePlan();
            var selected = Items.Where(x => x.Selected).ToDictionary(x => x.Asset.Key);
            foreach (var line in plan.Lines) ReviewLines.Add(new(selected[line.Selection.Asset.Key], line.Allocation, UpdateReview));
            foreach (var missing in plan.Unpriced) ReviewLines.Add(new(selected[missing.Selection.Asset.Key], new Allocation(0, missing.Quantity), UpdateReview));
            UpdateReview(); IsReviewOpen = ReviewLines.Count > 0;
            if (plan.UnpricedQuantity > 0) Status = $"매수 잔량 {plan.UnpricedQuantity}개 부족 · 미산정 단가를 직접 입력하기 전 제출할 수 없습니다.";
        }
        catch (ArgumentException e) { Status = e.Message; }
    }
    private void UpdateReview() { Raise(nameof(ReviewTotals)); Raise(nameof(ReviewLiquidity)); Raise(nameof(Estimate)); Submit.Refresh(); }
    private async Task SubmitAsync()
    {
        Busy = true;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); operation = cancellation;
        try
        {
            var snapshot = new SaleBatchSnapshot(Guid.NewGuid(), "demo-account", DateTimeOffset.UtcNow, ReviewLines.Select((x, i) => x.Snapshot(i)).ToImmutableArray());
            BatchValidation.Validate(snapshot, DateTimeOffset.UtcNow);
            var approved = snapshot.Lines.GroupBy(x => x.Asset.Key).ToDictionary(x => x.Key, x => x.Sum(l => l.Quantity));
            var selected = Items.Where(x => x.Selected).ToArray();
            if (selected.Length != approved.Count || selected.Any(x => !approved.TryGetValue(x.Asset.Key, out var count) || x.Quantity != count)) throw new ArgumentException("검토 이후 선택 수량이 변경되었습니다. 검토를 다시 여세요.");
            SalePlanner.ValidateCurrent(snapshot, await inventoryProvider.GetAsync(cancellation.Token));
            if (data != null) SalePlanner.ValidateReservations(snapshot, data.ReadBatches(), MockListingManagement.ReleasedRequests(data.ReadListings()));
            foreach (var item in selected)
            {
                var current = marketProvider.Get(item.Asset);
                if (current.Currency != item.Price.Currency || !current.Bids.SequenceEqual(item.Price.Bids) || !current.Trades.SequenceEqual(item.Price.Trades))
                    throw new ArgumentException("가격/매수 잔량이 변경되었습니다. 시세를 갱신하고 다시 검토하세요.");
            }
            record = await coordinator.ExecuteAsync(snapshot, new InlineProgress<BatchRecord>(Display), cancellation.Token);
            Display(record); SyncListings(record); IsReviewOpen = false; Status = "Mock 배치 처리됨 · 승인 대기는 게시 완료가 아닙니다. 실제 Steam 요청 없음.";
        }
        finally { operation = null; Busy = false; }
    }
    private async Task RecoverAsync()
    {
        Busy = true;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); operation = cancellation;
        try { var saved = await coordinator.RecoverAsync(cancellation.Token); if (saved != null) { Display(saved); SyncListings(saved); Status = "저널 복구·Mock 결과 대조 완료 · 자동 재전송 없음"; } }
        finally { operation = null; Busy = false; }
    }
    private async Task PublishMockAsync()
    {
        if (data == null) return;
        foreach (var line in record?.Snapshot.Lines ?? [])
        {
            var key = SaleBatchCoordinator.AttemptKey(record!.Snapshot, line, record.Results.First(x => x.LineId == line.LineId).Attempt);
            if (data.ReadEvidence(key) is { State: SaleState.Submitted or SaleState.AwaitingSteamGuard } previous)
                data.WriteEvidence(key, previous with { State = SaleState.Listed, Message = "명시적 Mock 서버 게시 fixture · Steam/모바일 승인 아님" });
        }
        var listings = data.ReadListings().Select(x => x.Action == ListingAction.Reprice && x.State == ListingState.AwaitingSteamGuard ? x with { State = ListingState.Listed } : x).ToArray();
        data.SaveListings(listings);
        if (record != null) await RecoverAsync(); else LoadListings();
    }
    private bool CanManage(bool retry) => !Busy && !AnyDialog && Listings.Any(x => x.Selected) && Listings.Where(x => x.Selected).All(x => retry ? x.CanRetry : x.CanCancel);
    private void RefreshManagement() { CancelPlan?.Refresh(); RepricePlan?.Refresh(); RetryListingPlan?.Refresh(); RetryFailedPlan?.Refresh(); }
    private void OpenManagement(ManagementMode mode)
    {
        managementMode = mode; ManagementRows.Clear(); retryApproval.Clear();
        if (mode == ManagementMode.RetryBatch && record != null)
        {
            foreach (var row in Results.Where(x => x.Selected && x.CanRetry))
            {
                var index = Enumerable.Range(0, record.Results.Length).First(i => record.Results[i].LineId == row.Result.LineId);
                var line = record.Snapshot.Lines[index]; retryApproval[line.LineId] = row.Result.Attempt;
                ManagementRows.Add(new(new MockListing(line.LineId, line.Asset.Name, line.Quantity, line.UnitQuote.BuyerPays, ListingState.Failed), false, UpdateManagement));
            }
        }
        else
        {
            cancellationSnapshot = Listings.Where(x => x.Selected).Select(x => x.Listing).ToArray();
            foreach (var listing in cancellationSnapshot) ManagementRows.Add(new(listing, mode == ManagementMode.Reprice, UpdateManagement));
        }
        Raise(nameof(ManagementTitle)); Raise(nameof(ManagementAction)); Raise(nameof(ManagementNote)); Raise(nameof(CancellationSummary));
        IsCancelReviewOpen = true;
    }
    private void UpdateManagement() => ConfirmCancellation.Refresh();
    private async Task CancelListingsAsync()
    {
        if (data == null) throw new InvalidOperationException("취소 Mock은 SQLite 저장소가 필요합니다.");
        Busy = true;
        try
        {
            switch (managementMode)
            {
                case ManagementMode.Cancel: MockCancellation.Execute(data, cancellationSnapshot); break;
                case ManagementMode.Reprice: MockListingManagement.Reprice(data, cancellationSnapshot, ManagementRows.Select(x => x.Price).ToArray()); break;
                case ManagementMode.RetryListing: MockListingManagement.RetryFailed(data, cancellationSnapshot); break;
                case ManagementMode.RetryBatch:
                    var current = await inventoryProvider.GetAsync(lifetime.Token);
                    var prices = record!.Snapshot.Lines.Where(x => retryApproval.ContainsKey(x.LineId)).Select(x => marketProvider.Get(x.Asset)).ToArray();
                    if (prices.Any(x => x.Currency != "KRW")) throw new ArgumentException("재시도 통화 불일치");
                    record = await coordinator.RetryFailedAsync(retryApproval, current, prices.Min(x => x.CapturedAt), new InlineProgress<BatchRecord>(Display), lifetime.Token);
                    Display(record); SyncListings(record); break;
            }
            LoadListings(); IsCancelReviewOpen = false; Status = "명시적으로 승인한 Mock 관리 작업 처리됨 · 개별 결과/승인 대기 확인 · 실제 Steam 요청 없음";
            Raise(nameof(DiagnosticsText));
        }
        finally { Busy = false; }
    }
    private void SyncListings(BatchRecord batch) { if (data != null) { MockOrderProjection.Update(data, batch); LoadListings(); } }
    private void Display(BatchRecord batch)
    {
        record = batch;
        if (Results.Count != batch.Results.Length) { Results.Clear(); foreach (var result in batch.Results) { var line = batch.Snapshot.Lines[Results.Count]; Results.Add(new(line.Asset.Name, line.Quantity, line.UnitQuote.BuyerPays, result, RefreshManagement)); } }
        else for (var i = 0; i < Results.Count; i++) Results[i].UpdateResult(batch.Results[i]);
        History.Clear(); History.Add($"Mock 확정 {batch.Snapshot.FinalizedAt.ToOffset(TimeSpan.FromHours(9)):yyyy-MM-dd HH:mm:ss} KST · {batch.Snapshot.Lines.Length}건 · 실수령 예상 ₩{batch.Snapshot.Net:N0}");
        Raise(nameof(ProgressSummary)); Update();
    }
    private void SetSelection(IEnumerable<ItemViewModel> items, bool value)
    { bulkSelection = true; try { foreach (var item in items) item.Selected = value; } finally { bulkSelection = false; Update(); } }
    private void Persist(string key, string value) { try { data?.WriteSetting(key, value); } catch (Exception e) when (e is IOException or Microsoft.Data.Sqlite.SqliteException) { DiagnosticHub.Emit(DiagnosticEvent.Settings,DiagnosticArea.Storage,DiagnosticOutcome.Failed,LogLevel.Warning,DiagnosticError.Storage,exception:e); Status = "설정 저장 실패: 저장소를 확인하세요."; } }
    private void Error(Exception e) { DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.UI,DiagnosticOutcome.Failed,LogLevel.Error,e is OperationCanceledException?DiagnosticError.Cancelled:DiagnosticError.Validation,exception:e); Status = e is OperationCanceledException ? "사용자 취소 · 이미 접수된 요청을 되돌리지 않으며 자동 재전송하지 않습니다." : e is ArgumentException ? e.Message : e is IOException ? "조회/저장 연결 실패 · 자동 재전송 안 함. 수동 갱신/상태 대조가 필요합니다." : "안전 중단: 데이터/저널을 확인하세요. 자동 재전송하지 않았습니다."; try { (journal as IDiagnosticStore)?.RecordDiagnostic(DiagnosticCode.UiFailure); } catch { } Busy = false; Raise(nameof(DiagnosticsText)); }
    private void Safe(Action action) { try { action(); } catch (Exception e) { Error(e); } }
    private void Update()
    {
        if (bulkSelection) return;
        Raise(nameof(SelectedCount)); Raise(nameof(SelectionSummary)); Raise(nameof(Estimate));
        Review?.Refresh(); Submit?.Refresh(); CloseReview?.Refresh(); Cancel?.Refresh(); Recover?.Refresh(); ClearDemo?.Refresh(); Refresh?.Refresh(); SelectAll?.Refresh(); ClearSelection?.Refresh(); CancelPlan?.Refresh(); CloseCancelReview?.Refresh(); ConfirmCancellation?.Refresh(); PublishMockEvidence?.Refresh(); LoadLargeDemo?.Refresh(); DeleteLocalDemoData?.Refresh();
        SelectAllListings?.Refresh(); ClearListings?.Refresh(); RepricePlan?.Refresh(); RetryListingPlan?.Refresh(); SelectFailedResults?.Refresh(); RetryFailedPlan?.Refresh(); RefreshReviewStatus?.Refresh(); Raise(nameof(AnyDialog));
    }
    public void Dispose() { Steam.Dispose(); lifetime.Cancel(); lifetime.Dispose(); }
}
internal sealed class InlineProgress<T>(Action<T> report) : IProgress<T> { public void Report(T value) => report(value); }
public sealed class ResultViewModel(string name, int quantity, long price, LineResult result, Action changed) : Observable
{
    public string Name => name; public int Quantity => quantity; public long Price => price;
    private LineResult resultValue = result;
    public LineResult Result => resultValue;
    public bool CanRetry => Result.State == SaleState.Failed;
    private bool selected;
    public bool Selected { get => selected; set { if (Set(ref selected, value && CanRetry)) changed(); } }
    public string Detail => $"{Quantity}개 × ₩{Price:N0} · {Result.State} · 시도 {Result.Attempt} · {Result.Message}";
    public void UpdateResult(LineResult value) { if (Set(ref resultValue, value, nameof(Result))) { if (!CanRetry) Selected = false; Raise(nameof(Detail)); Raise(nameof(CanRetry)); } }
}
