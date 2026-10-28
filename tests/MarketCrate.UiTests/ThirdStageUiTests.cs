using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MarketCrate.App;
using MarketCrate.App.Controls;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure;

internal static class ThirdStageUiTests
{
    public static void Run(Action<string, Action> test, Action<FrameworkElement, int, int, double, string> render)
    {
        test("부족 수량/수동 가격/전략 전환 자체 렌더", () =>
        {
            using var fixture = new Fixture(new ShortProvider()); var vm = fixture.Vm; var view = fixture.View;
            vm.SelectAll.Execute(null); vm.Items[0].Quantity = 12; vm.Review.Execute(null);
            render(view, 1100, 700, 1, "stage3-shortage.png"); Check(vm.ReviewLines.Any(x => x.IsUnpriced) && !vm.Submit.CanExecute(null));
            var row = vm.ReviewLines.Single(x => x.IsUnpriced); Check(row.PriceText == ""); row.PriceText = "1500";
            var total = vm.ReviewLines.Sum(x => x.Price * x.Quantity).ToString("N0"); Check(vm.Estimate.Contains(total) && vm.ReviewTotals.Contains(total));
            render(view, 1280, 800, 1.5, "stage3-manual-price.png");
            vm.SelectedStrategy = vm.Strategies[1]; Check(vm.ReviewLines.All(x => !x.IsManual));
            render(view, 1100, 700, 1, "stage3-strategy-reset.png");
        });
        test("개별/전체 선택·가격 변경 검토·입력 오류·재등록 대기 렌더", () =>
        {
            using var fixture = new Fixture(new DemoDataProvider()); var vm = fixture.Vm; var view = fixture.View;
            ((TabControl)view.FindName("WorkspaceTabs")).SelectedIndex = 1;
            vm.SelectAllListings.Execute(null); Check(vm.Listings.Count(x => x.Selected) == 2); vm.ClearListings.Execute(null); Check(vm.Listings.All(x => !x.Selected));
            vm.Listings.First(x => x.CanCancel && x.Listing.ListingId != "demo-reject").Selected = true; vm.RepricePlan.Execute(null);
            render(view, 1100, 700, 1, "stage3-reprice-review.png");
            var box = Find<TextBox>((Grid)view.FindName("ManagementOverlay")).First();
            box.Text = "invalid"; box.GetBindingExpression(TextBox.TextProperty)!.UpdateSource(); Check(vm.HasInputErrors && !vm.ConfirmCancellation.CanExecute(null));
            box.Text = "1575"; box.GetBindingExpression(TextBox.TextProperty)!.UpdateSource(); Check(!vm.HasInputErrors);
            render(view, 1280, 800, 1, "stage3-reprice-edited.png"); vm.ConfirmCancellation.Execute(null);
            Check(!vm.Busy && !vm.IsCancelReviewOpen && vm.Listings.Any(x => x.Listing.BuyerPays == 1575 && x.Listing.State == ListingState.AwaitingSteamGuard));
            render(view, 1380, 820, 1, "stage3-reprice-pending.png");
            vm.PublishMockEvidence.Execute(null); Check(vm.Listings.Any(x => x.Listing.BuyerPays == 1575 && x.Listing.State == ListingState.Listed));
        });
        test("확정 실패 배치 재시도 검토/시도 번호/결과 렌더", () =>
        {
            using var fixture = new Fixture(new DemoDataProvider()); var vm = fixture.Vm; var view = fixture.View;
            vm.SelectedGame = "Counter-Strike 2"; vm.SelectAll.Execute(null); vm.Review.Execute(null); vm.Submit.Execute(null); Check(!vm.Busy);
            ((TabControl)view.FindName("WorkspaceTabs")).SelectedIndex = 2;
            vm.SelectFailedResults.Execute(null); vm.RetryFailedPlan.Execute(null); Check(vm.ManagementRows.Count == 1);
            render(view, 1100, 700, 1, "stage3-retry-review.png"); vm.ConfirmCancellation.Execute(null);
            Check(vm.Results.Any(x => x.Result.Attempt == 1) && !vm.Results.Any(x => x.CanRetry));
            render(view, 1380, 820, 1, "stage3-retry-result.png"); vm.PublishMockEvidence.Execute(null); Check(vm.Results.All(x => x.Result.State == SaleState.Listed));
        });
        test("관리 실패 선택 후 명시적 취소 재시도 렌더", () =>
        {
            using var fixture = new Fixture(new DemoDataProvider()); var vm = fixture.Vm; var view = fixture.View;
            ((TabControl)view.FindName("WorkspaceTabs")).SelectedIndex = 1;
            vm.Listings.Single(x => x.Listing.ListingId == "demo-reject").Selected = true; vm.CancelPlan.Execute(null); vm.ConfirmCancellation.Execute(null);
            vm.Listings.Single(x => x.CanRetry).Selected = true; vm.RetryListingPlan.Execute(null);
            render(view, 1100, 700, 1, "stage3-cancel-retry.png"); vm.ConfirmCancellation.Execute(null);
            Check(vm.Listings.Single(x => x.Listing.ListingId == "demo-reject").Listing.State == ListingState.Cancelled);
            Check(!vm.ConfirmCancellation.CanExecute(null));
        });
        test("로딩·Mock 조회 실패/기존 데이터 보존/재전송 없음 렌더", () =>
        {
            var provider = new DeferredProvider(); using var fixture = new Fixture(provider); var vm = fixture.Vm; var view = fixture.View;
            provider.Deferred = true; vm.Refresh.Execute(null); Check(vm.Busy);
            render(view, 1100, 700, 1, "stage3-loading.png");
            var panel=(Border)view.FindName("BusyPanel"); var cancel=Find<Button>(panel).Single();
            var bottom=cancel.TransformToAncestor(panel).Transform(new Point(0,cancel.ActualHeight)); Check(bottom.Y<=panel.ActualHeight);
            provider.Completion.SetException(new IOException("SYNTHETIC-NEVER-LOG"));
            render(view, 1100, 700, 1, "stage3-network-error.png");
            Check(!vm.Busy && vm.Items.Count == 7 && vm.Status.Contains("연결 실패"));
            Check(fixture.Store.ReadDiagnostics().Any(x => x.Code == DiagnosticCode.UiFailure));
        });
        test("대량 10,000개 카드 반복 스크롤/재활용 선택 보존", () =>
        {
            using var fixture = new Fixture(new DemoDataProvider(10000)); var vm = fixture.Vm; var view = fixture.View;
            render(view, 1380, 820, 1, "stage3-load-start.png"); var panel = Find<VirtualizingTilePanel>(view).Single(); vm.Items[0].Selected = true;
            foreach (var fraction in new[] { 0.2, 0.7, 1, 0.4, 0 })
            {
                panel.SetVerticalOffset(panel.ExtentHeight * fraction);
                view.Measure(new(1380, 820)); view.Arrange(new Rect(0, 0, 1380, 820)); view.UpdateLayout();
                Check(panel.RealizedCount is > 0 and < 40);
            }
            render(view, 1380, 820, 1, "stage3-load-return.png");
            var list = (ListBox)view.FindName("InventoryList"); var first = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
            Check(Find<CheckBox>(first).Single().IsChecked == true && vm.SelectedCount == 1);
        });
    }
    private sealed class Fixture : IDisposable
    {
        public SqliteDemoStore Store { get; }
        public MainViewModel Vm { get; }
        public MainView View { get; }
        public Fixture(IInventoryProvider provider)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "runtime", "ui-tests", "stage3-" + Guid.NewGuid().ToString("N"));
            Store = new(path); Vm = new(provider, (IMarketDataProvider)provider, Store); Vm.InitializeAsync().GetAwaiter().GetResult(); View = new() { DataContext = Vm };
        }
        public void Dispose() { Vm.Dispose(); Store.Dispose(); }
    }
    private class ShortProvider : IInventoryProvider, IMarketDataProvider
    {
        public Capability Capability => Capability.Mock;
        public virtual Task<IReadOnlyList<InventoryAsset>> GetAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<InventoryAsset>>([new(new(730, "2", "shortage"), "Counter-Strike 2", "Synthetic Depth Shortage", "Synthetic", 20, true, true, true)]);
        public virtual PriceSnapshot Get(InventoryAsset asset) => new("KRW", DateTimeOffset.UtcNow, "합성 Mock", TimeSpan.FromHours(24), [new(1100, 10)], [new(1000, 3), new(980, 4), new(950, 3)]);
    }
    private sealed class DeferredProvider : ShortProvider
    {
        public bool Deferred;
        public TaskCompletionSource<IReadOnlyList<InventoryAsset>> Completion { get; } = new();
        private readonly DemoDataProvider demo = new();
        public override Task<IReadOnlyList<InventoryAsset>> GetAsync(CancellationToken token) => Deferred ? Completion.Task : demo.GetAsync(token);
        public override PriceSnapshot Get(InventoryAsset asset) => demo.Get(asset);
    }
    private static void Check(bool ok) { if (!ok) throw new Exception("assertion failed"); }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = VisualTreeHelper.GetChild(root, i); if (child is T match) yield return match; foreach (var nested in Find<T>(child)) yield return nested; } }
}
