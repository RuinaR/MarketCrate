using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using MarketCrate.Core;
using MarketCrate.Infrastructure;

namespace MarketCrate.App.ViewModels;

public sealed class MainViewModel : Observable, IDisposable
{
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
    public Command CloseCancelReview { get; }
    public Command ClearDemo { get; }
    public AsyncCommand Refresh { get; }
    public AsyncCommand Submit { get; }
    public AsyncCommand Recover { get; }
    public AsyncCommand ConfirmCancellation { get; }
    public AsyncCommand PublishMockEvidence { get; }
    public AsyncCommand LoadLargeDemo { get; }
    public Command DeleteLocalDemoData { get; }
    public MainViewModel(DemoDataProvider provider, IDemoJournal journal) : this(provider, provider, journal) { }
    public MainViewModel(IInventoryProvider inventory, IMarketDataProvider market, IDemoJournal journal)
    {
        inventoryProvider = inventory; marketProvider = market; this.journal = journal; data = journal as IDemoDataStore;
        if (data?.ReadSetting("demo-count") == "10000" && inventory is DemoDataProvider)
        { var large = new DemoDataProvider(10000); inventoryProvider = large; marketProvider = large; }
        gridMode = data?.ReadSetting("grid") != "false"; reducedMotion = data?.ReadSetting("motion") != "false";
        var savedGame = data?.ReadSetting("game"); if (savedGame != null && Games.Contains(savedGame)) game = savedGame;
        var savedStrategy = data?.ReadSetting("strategy"); if (savedStrategy != null && Strategies.Contains(savedStrategy)) strategy = savedStrategy;
        coordinator = new(new MockListingExecutor(data), journal);
        SelectAll = new(() => SetSelection(VisibleItems, true), () => !Busy && !IsReviewOpen);
        ClearSelection = new(() => SetSelection(Items, false), () => !Busy && !IsReviewOpen);
        Review = new(BuildReview, () => !Busy && !HasInputErrors && SelectedCount > 0 && record == null);
        CloseReview = new(() => IsReviewOpen = false, () => !Busy);
        Cancel = new(() => operation?.Cancel(), () => Busy);
        CancelPlan = new(() => { cancellationSnapshot = Listings.Where(x => x.Selected).Select(x => x.Listing).ToArray(); IsCancelReviewOpen = true; Raise(nameof(CancellationSummary)); }, () => !Busy && Listings.Any(x => x.Selected));
        CloseCancelReview = new(() => IsCancelReviewOpen = false, () => !Busy);
        ConfirmCancellation = new(CancelListingsAsync, Error, () => !Busy && IsCancelReviewOpen && cancellationSnapshot.Length > 0);
        PublishMockEvidence = new(PublishMockAsync, Error, () => !Busy && data != null && record != null);
        LoadLargeDemo = new(async () => { var large = new DemoDataProvider(10000); inventoryProvider = large; marketProvider = large; Persist("demo-count", "10000"); await LoadAsync(); }, Error, () => !Busy && !IsReviewOpen);
        Refresh = new(LoadAsync, Error, () => !Busy && !IsReviewOpen);
        Submit = new(SubmitAsync, Error, () => !Busy && !HasInputErrors && IsReviewOpen && ReviewLines.Count > 0 && ReviewLines.All(x => x.Valid) && record == null);
        Recover = new(RecoverAsync, Error, () => !Busy && !IsReviewOpen);
        ClearDemo = new(() => Safe(() => { journal.ClearDemo(); record = null; Results.Clear(); History.Clear(); Status = "활성 Mock 배치 해제. 이전 요청 증거·예약 수량은 보존됩니다."; Update(); }), () => !Busy && !IsReviewOpen && (record == null || record.Results.All(x => x.State is SaleState.Ready or SaleState.Listed or SaleState.Failed)));
        DeleteLocalDemoData = new(() => Safe(() => { if (journal is SqliteDemoStore sql) sql.DeleteDemoData(); record = null; Items.Clear(); VisibleItems.Clear(); Results.Clear(); Listings.Clear(); History.Clear(); ReviewLines.Clear(); Status = "SQLite 합성 데이터·설정·저널 삭제 완료. 이관 원본 JSON은 보존됩니다."; Raise(nameof(HasNoItems)); Update(); }), () => journal is SqliteDemoStore && !Busy && !IsReviewOpen && !IsCancelReviewOpen);
    }
    private string search = "", game = "모든 게임", sort = "이름순", strategy = "구매주문 기반 예상가";
    private bool marketableOnly = true, gridMode = true, reviewOpen, busy, inputErrors, cancelReviewOpen, reducedMotion = true;
    private string status = "Mock 전용 · Steam 연결 없음 · 실등록 IntegrationBlocked";
    public bool HasInputErrors { get => inputErrors; set { if (Set(ref inputErrors, value)) Update(); } }
    public string Search { get => search; set { if (Set(ref search, value)) Filter(); } }
    public string SelectedGame { get => game; set { if (Set(ref game, value)) { Persist("game", value); Filter(); } } }
    public string SelectedSort { get => sort; set { if (Set(ref sort, value)) Filter(); } }
    public string SelectedStrategy { get => strategy; set { if (Set(ref strategy, value)) { Persist("strategy", value); Update(); } } }
    public bool MarketableOnly { get => marketableOnly; set { if (Set(ref marketableOnly, value)) Filter(); } }
    public bool GridMode { get => gridMode; set { if (Set(ref gridMode, value)) Persist("grid", value ? "true" : "false"); } }
    public bool ReducedMotion { get => reducedMotion; set { if (Set(ref reducedMotion, value)) Persist("motion", value ? "true" : "false"); } }
    public bool IsReviewOpen { get => reviewOpen; set { if (Set(ref reviewOpen, value)) Update(); } }
    public bool IsCancelReviewOpen { get => cancelReviewOpen; private set { Set(ref cancelReviewOpen, value); ConfirmCancellation?.Refresh(); } }
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
            try { var plan = CreatePlan(); var total = plan.Lines.Sum(x => checked(x.Allocation.BuyerPays * x.Allocation.Quantity)); return $"₩{total:N0}" + (plan.UnpricedQuantity > 0 ? $" · {plan.UnpricedQuantity}개 추정 불가" : ""); }
            catch (ArgumentException) { return "시세/수량 재검토 필요"; }
        }
    }
    public string ReviewTotals => ReviewLines.All(x => x.Valid) ? $"구매자 지불 ₩{ReviewLines.Sum(x => x.Price * x.Quantity):N0}   /   수수료 ₩{ReviewLines.Sum(x => x.Quote.Fees * x.Quantity):N0}   /   실수령 ₩{ReviewLines.Sum(x => x.Quote.SellerReceives * x.Quantity):N0}" : "가격 오류를 수정하세요 (3 ~ 1,000,000,000원)";
    public string ProgressSummary => record == null ? "아직 실행한 Mock 배치가 없습니다." : $"Mock 배치 {record.Snapshot.BatchId.ToString()[..8]} · 총 {record.Results.Length}건 / 게시 확인 {record.Results.Count(x => x.State == SaleState.Listed)} / 승인 대기 {record.Results.Count(x => x.State == SaleState.AwaitingSteamGuard)} / 실패 {record.Results.Count(x => x.State == SaleState.Failed)} / 불명 {record.Results.Count(x => x.State == SaleState.Unknown)}";
    public async Task InitializeAsync() { try { await LoadAsync(); await RecoverAsync(); } catch (Exception e) { Error(e); } }
    private async Task LoadAsync()
    {
        Busy = true;
        try
        {
            var assets = await inventoryProvider.GetAsync(lifetime.Token);
            data?.SaveInventory(assets);
            Items.ReplaceAll(assets.Select(asset => new ItemViewModel(asset, marketProvider.Get(asset), Update)));
            LoadListings(); Filter(); Status = $"Mock {assets.Count:N0}개 자산 갱신 · 합성 24시간 표본 · Steam 요청 없음";
        }
        finally { Busy = false; }
    }
    private void LoadListings()
    {
        IReadOnlyList<MockListing> values = data == null ? [] : MockCancellation.Recover(data);
        if (values.Count == 0) { values = [new("demo-listed", "Prism Collection • Case", 1, 1200, ListingState.Listed), new("demo-reject", "Cobalt Circuit • Sticker", 2, 1180, ListingState.Listed), new("demo-pending", "Moonlit Archive • Treasure", 1, 650, ListingState.AwaitingSteamGuard)]; data?.SaveListings(values); }
        Listings.Clear(); foreach (var value in values) Listings.Add(new(value, () => CancelPlan?.Refresh()));
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
        try
        {
            ReviewLines.Clear(); var plan = CreatePlan();
            if (plan.UnpricedQuantity > 0) { Status = $"구매주문 잔량 {plan.UnpricedQuantity}개 부족: 수량을 줄이거나 평균가 전략으로 다시 검토하세요."; return; }
            var selected = Items.Where(x => x.Selected).ToDictionary(x => x.Asset.Key);
            foreach (var line in plan.Lines) ReviewLines.Add(new(selected[line.Selection.Asset.Key], line.Allocation, UpdateReview));
            UpdateReview(); IsReviewOpen = ReviewLines.Count > 0;
        }
        catch (ArgumentException e) { Status = e.Message; }
    }
    private void UpdateReview() { Raise(nameof(ReviewTotals)); Submit.Refresh(); }
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
            if (data != null) SalePlanner.ValidateReservations(snapshot, data.ReadBatches(),data.ReadListings().Where(x=>x.State==ListingState.Cancelled).Select(x=>x.ListingId).ToHashSet());
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
        if (data == null || record == null) return;
        foreach (var line in record.Snapshot.Lines)
        {
            var key = SaleBatchCoordinator.RequestKey(record.Snapshot, line);
            if (data.ReadEvidence(key) is { State: SaleState.Submitted or SaleState.AwaitingSteamGuard } previous)
                data.WriteEvidence(key, previous with { State = SaleState.Listed, Message = "명시적 Mock 서버 게시 fixture · Steam/모바일 승인 아님" });
        }
        await RecoverAsync();
    }
    private Task CancelListingsAsync()
    {
        if (data == null) throw new InvalidOperationException("취소 Mock은 SQLite 저장소가 필요합니다.");
        Busy = true;
        try { MockCancellation.Execute(data, cancellationSnapshot); LoadListings(); IsCancelReviewOpen = false; Status = "선택 Mock 취소 처리됨 · 취소/실패를 개별 표시 · 실제 Steam 취소 없음"; }
        finally { Busy = false; }
        return Task.CompletedTask;
    }
    private void SyncListings(BatchRecord batch) { if(data!=null) { MockOrderProjection.Update(data,batch); LoadListings(); } }
    private void Display(BatchRecord batch)
    {
        record = batch;
        if (Results.Count != batch.Results.Length) { Results.Clear(); foreach (var result in batch.Results) { var line = batch.Snapshot.Lines[Results.Count]; Results.Add(new(line.Asset.Name, line.Quantity, line.UnitQuote.BuyerPays, result)); } }
        else for (var i = 0; i < Results.Count; i++) { if (Results[i].Result != batch.Results[i]) Results[i] = Results[i] with { Result = batch.Results[i] }; }
        History.Clear(); History.Add($"Mock 확정 {batch.Snapshot.FinalizedAt.ToOffset(TimeSpan.FromHours(9)):yyyy-MM-dd HH:mm:ss} KST · {batch.Snapshot.Lines.Length}건 · 실수령 예상 ₩{batch.Snapshot.Net:N0}");
        Raise(nameof(ProgressSummary)); Update();
    }
    private void SetSelection(IEnumerable<ItemViewModel> items, bool value)
    { bulkSelection = true; try { foreach (var item in items) item.Selected = value; } finally { bulkSelection = false; Update(); } }
    private void Persist(string key, string value) { try { data?.WriteSetting(key, value); } catch (Exception e) when (e is IOException or Microsoft.Data.Sqlite.SqliteException) { Status = "설정 저장 실패: 저장소를 확인하세요."; } }
    private void Error(Exception e) { Status = e is ArgumentException ? e.Message : "안전 중단: 데이터/저널을 확인하세요. 자동 재전송하지 않았습니다."; Busy = false; }
    private void Safe(Action action) { try { action(); } catch (Exception e) { Error(e); } }
    private void Update()
    {
        if (bulkSelection) return;
        Raise(nameof(SelectedCount)); Raise(nameof(SelectionSummary)); Raise(nameof(Estimate));
        Review?.Refresh(); Submit?.Refresh(); CloseReview?.Refresh(); Cancel?.Refresh(); Recover?.Refresh(); ClearDemo?.Refresh(); Refresh?.Refresh(); SelectAll?.Refresh(); ClearSelection?.Refresh(); CancelPlan?.Refresh(); CloseCancelReview?.Refresh(); ConfirmCancellation?.Refresh(); PublishMockEvidence?.Refresh(); LoadLargeDemo?.Refresh(); DeleteLocalDemoData?.Refresh();
    }
    public void Dispose() { lifetime.Cancel(); lifetime.Dispose(); }
}
internal sealed class InlineProgress<T>(Action<T> report) : IProgress<T> { public void Report(T value) => report(value); }
public record ResultViewModel(string Name, int Quantity, long Price, LineResult Result)
{
    public string Detail => $"{Quantity}개 × ₩{Price:N0} · {Result.State} · {Result.Message}";
}
