using System.Collections.Immutable;
using System.Collections.ObjectModel;
using MarketCrate.Core;
using MarketCrate.Infrastructure;

namespace MarketCrate.App.ViewModels;

public sealed class MainViewModel : Observable, IDisposable
{
    private readonly DemoDataProvider provider;
    private readonly FileBatchJournal journal;
    private readonly SaleBatchCoordinator coordinator;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? operation;
    private BatchRecord? record;
    public ObservableCollection<ItemViewModel> Items { get; } = [];
    public ObservableCollection<ItemViewModel> VisibleItems { get; } = [];
    public ObservableCollection<ReviewLineViewModel> ReviewLines { get; } = [];
    public ObservableCollection<ResultViewModel> Results { get; } = [];
    public ObservableCollection<string> History { get; } = [];
    public string[] Games { get; } = ["모든 게임", "Counter-Strike 2", "Dota 2", "Team Fortress 2"];
    public string[] Strategies { get; } = ["구매주문 기반 예상가", "24시간 거래량 가중 평균가"];
    public string[] Sorts { get; } = ["이름순", "평균가 높은순"];
    public Command SelectAll { get; }
    public Command ClearSelection { get; }
    public Command Review { get; }
    public Command CloseReview { get; }
    public Command Cancel { get; }
    public Command CancelPlan { get; }
    public AsyncCommand Refresh { get; }
    public AsyncCommand Submit { get; }
    public AsyncCommand Recover { get; }
    public Command ClearDemo { get; }
    public MainViewModel(DemoDataProvider provider, FileBatchJournal journal)
    {
        this.provider = provider; this.journal = journal;
        coordinator = new(new MockListingExecutor(), journal);
        SelectAll = new(() => { foreach (var item in VisibleItems) item.Selected = true; });
        ClearSelection = new(() => { foreach (var item in Items) item.Selected = false; });
        Review = new(BuildReview, () => !Busy && !HasInputErrors && SelectedCount > 0 && record == null);
        CloseReview = new(() => IsReviewOpen = false, () => !Busy);
        Cancel = new(() => operation?.Cancel(), () => Busy);
        CancelPlan = new(() => Status = "Mock 취소/가격 변경 검토: 실제 취소·재등록은 IntegrationBlocked. 어떠한 요청도 전송하지 않았습니다.");
        Refresh = new(LoadAsync, Error, () => !Busy);
        Submit = new(SubmitAsync, Error, () => !Busy && !HasInputErrors && IsReviewOpen && ReviewLines.Count > 0 && ReviewLines.All(x => x.Valid) && record == null);
        Recover = new(RecoverAsync, Error, () => !Busy);
        ClearDemo = new(() => { journal.ClearDemo(); record = null; Results.Clear(); History.Clear(); Status = "합성 Mock 저널 삭제 완료. 새 검토를 시작할 수 있습니다."; Update(); }, () => !Busy);
    }
    private string search = "", game = "모든 게임", sort = "이름순", strategy = "구매주문 기반 예상가";
    private bool marketableOnly = true, gridMode = true, reviewOpen, busy;
    private bool inputErrors;
    public bool HasInputErrors { get => inputErrors; set { if (Set(ref inputErrors, value)) Update(); } }
    private string status = "Mock 전용 · Steam 연결 없음 · 실등록 IntegrationBlocked";
    public string Search { get => search; set { if (Set(ref search, value)) Filter(); } }
    public string SelectedGame { get => game; set { if (Set(ref game, value)) Filter(); } }
    public string SelectedSort { get => sort; set { if (Set(ref sort, value)) Filter(); } }
    public string SelectedStrategy { get => strategy; set { if (Set(ref strategy, value)) Update(); } }
    public bool MarketableOnly { get => marketableOnly; set { if (Set(ref marketableOnly, value)) Filter(); } }
    public bool GridMode { get => gridMode; set => Set(ref gridMode, value); }
    public bool IsReviewOpen { get => reviewOpen; set { if (Set(ref reviewOpen, value)) Submit.Refresh(); } }
    public bool Busy { get => busy; private set { Set(ref busy, value); Update(); } }
    public string Status { get => status; private set => Set(ref status, value); }
    public bool HasNoItems => VisibleItems.Count == 0;
    private bool reducedMotion = true;
    public bool ReducedMotion { get => reducedMotion; set => Set(ref reducedMotion, value); }
    public int SelectedCount => Items.Where(x => x.Selected).Sum(x => x.Quantity);
    public string SelectionSummary => $"{Items.Count(x => x.Selected)}개 자산 · {SelectedCount}개 수량";
    public string Estimate
    {
        get
        {
            long total = 0; var missing = 0;
            foreach (var item in Items.Where(x => x.Selected))
            {
                var plan = Plan(item);
                total += plan.Lines.Sum(x => x.BuyerPays * x.Quantity); missing += plan.UnpricedQuantity;
            }
            return $"₩{total:N0}" + (missing > 0 ? $" · {missing}개 추정 불가" : "");
        }
    }
    public string ReviewTotals => ReviewLines.All(x => x.Valid)
        ? $"구매자 지불 ₩{ReviewLines.Sum(x => x.Price * x.Quantity):N0}   /   수수료 ₩{ReviewLines.Sum(x => x.Quote.Fees * x.Quantity):N0}   /   실수령 ₩{ReviewLines.Sum(x => x.Quote.SellerReceives * x.Quantity):N0}"
        : "가격 오류를 수정하세요 (3 ~ 1,000,000,000원)";
    public string ProgressSummary => record == null ? "아직 실행한 Mock 배치가 없습니다." :
        $"Mock 배치 {record.Snapshot.BatchId.ToString()[..8]} · 총 {record.Results.Length}건 / 게시 확인 {record.Results.Count(x => x.State == SaleState.Listed)} / 승인 대기 {record.Results.Count(x => x.State == SaleState.AwaitingSteamGuard)} / 실패 {record.Results.Count(x => x.State == SaleState.Failed)} / 불명 {record.Results.Count(x => x.State == SaleState.Unknown)}";
    public async Task InitializeAsync() { try { await LoadAsync(); await RecoverAsync(); } catch (Exception e) { Error(e); } }
    private async Task LoadAsync()
    {
        var assets = await provider.GetAsync(lifetime.Token);
        Items.Clear();
        foreach (var asset in assets) Items.Add(new(asset, provider.Get(asset), Update));
        Filter(); Update(); Status = "Mock 데이터 갱신 완료 · 합성 24시간 표본 · Steam 요청 없음";
    }
    private void Filter()
    {
        var items = Items.Where(x => (game == "모든 게임" || x.Game == game) && (!marketableOnly || x.CanSelect) && x.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        items = sort == "평균가 높은순" ? items.OrderByDescending(x => Pricing.WeightedAverage(x.Price.Trades)) : items.OrderBy(x => x.Name);
        VisibleItems.Clear(); foreach (var item in items) VisibleItems.Add(item);
        Raise(nameof(HasNoItems));
    }
    private AllocationResult Plan(ItemViewModel item) => strategy == Strategies[0] ? Pricing.Allocate(item.Quantity, item.Price.Bids) :
        Pricing.WeightedAverage(item.Price.Trades) is long average ? new([new(average, item.Quantity)], 0) : new([], item.Quantity);
    private void BuildReview()
    {
        ReviewLines.Clear();
        // Consume shared order-book depth across assets with the same market identity.
        var remaining = new Dictionary<(int, string, string), List<BidLevel>>();
        foreach (var item in Items.Where(x => x.Selected))
        {
            AllocationResult plan;
            if (strategy == Strategies[0])
            {
                var key = (item.Asset.Key.AppId, item.Asset.Key.ContextId, item.Asset.MarketHashName);
                if (!remaining.TryGetValue(key, out var bids)) remaining[key] = bids = item.Price.Bids.OrderByDescending(x => x.BuyerPays).ToList();
                plan = Pricing.Allocate(item.Quantity, bids);
                var used = item.Quantity - plan.UnpricedQuantity;
                for (var i = 0; i < bids.Count && used > 0; i++) { var count = Math.Min(used, bids[i].Quantity); used -= count; bids[i] = bids[i] with { Quantity = bids[i].Quantity - count }; }
                bids.RemoveAll(x => x.Quantity == 0);
            }
            else plan = Plan(item);
            if (plan.UnpricedQuantity > 0) { ReviewLines.Clear(); Status = "구매주문 잔량 부족: 수량을 줄이거나 평균가 전략으로 다시 검토하세요. 미산정 수량은 제출하지 않습니다."; return; }
            foreach (var allocation in plan.Lines) ReviewLines.Add(new(item, allocation, UpdateReview));
        }
        UpdateReview(); IsReviewOpen = ReviewLines.Count > 0;
    }
    private void UpdateReview() { Raise(nameof(ReviewTotals)); Submit.Refresh(); }
    private async Task SubmitAsync()
    {
        Busy = true;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        operation = cancellation;
        try
        {
            var snapshot = new SaleBatchSnapshot(Guid.NewGuid(), "demo-account", DateTimeOffset.UtcNow, ReviewLines.Select((x, i) => x.Snapshot(i)).ToImmutableArray());
            BatchValidation.Validate(snapshot, DateTimeOffset.UtcNow);
            record = await coordinator.ExecuteAsync(snapshot, new InlineProgress<BatchRecord>(Display), cancellation.Token);
            Display(record); IsReviewOpen = false;
            Status = "Mock 배치 처리됨 · 승인 대기는 게시 완료가 아닙니다. 실제 Steam 연결·거래 없음.";
        }
        finally { operation = null; Busy = false; }
    }
    private async Task RecoverAsync()
    {
        Busy = true;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        operation = cancellation;
        try { var saved = await coordinator.RecoverAsync(cancellation.Token); if (saved != null) { record = saved; Display(saved); Status = "저널 복구·Mock 상태 대조 완료 · 자동 재전송 없음"; } }
        finally { operation = null; Busy = false; }
    }
    private void Display(BatchRecord batch)
    {
        record = batch; Results.Clear();
        foreach (var result in batch.Results)
        { var line = batch.Snapshot.Lines.First(x => x.LineId == result.LineId); Results.Add(new(line.Asset.Name, line.Quantity, line.UnitQuote.BuyerPays, result)); }
        History.Clear(); History.Add($"Mock 확정 {batch.Snapshot.FinalizedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} · {batch.Snapshot.Lines.Length}건 · 실수령 예상 ₩{batch.Snapshot.Net:N0}");
        Raise(nameof(ProgressSummary)); Update();
    }
    private void Error(Exception e) { Status = e is ArgumentException ? e.Message : "안전 중단: 데이터/저널을 확인하세요. 자동 재전송하지 않았습니다."; Busy = false; }
    private void Update()
    { Raise(nameof(SelectedCount)); Raise(nameof(SelectionSummary)); Raise(nameof(Estimate)); Review?.Refresh(); Submit?.Refresh(); CloseReview?.Refresh(); Cancel?.Refresh(); Recover?.Refresh(); ClearDemo?.Refresh(); Refresh?.Refresh(); }
    public void Dispose() { lifetime.Cancel(); lifetime.Dispose(); }
}
internal sealed class InlineProgress<T>(Action<T> report) : IProgress<T> { public void Report(T value) => report(value); }
public record ResultViewModel(string Name, int Quantity, long Price, LineResult Result)
{
    public string Detail => $"{Quantity}개 × ₩{Price:N0} · {Result.State} · {Result.Message}";
}
