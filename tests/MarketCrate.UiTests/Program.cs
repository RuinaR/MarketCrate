using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Threading;
using MarketCrate.App;
using MarketCrate.App.Controls;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure;

internal static class Program
{
    private static int passed, failed;
    private static readonly string Folder = Path.Combine(Directory.GetCurrentDirectory(), "runtime", "ui-tests");
    [STAThread]
    private static int Main(string[] args)
    {
        // This entry point never constructs Window/HwndSource, opens Popup, activates focus, or injects OS input.
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
        Directory.CreateDirectory(Folder);
        try
        {
            app.Resources.MergedDictionaries.Add(new() { Source = new Uri("/MarketCrate.App;component/Themes/Tokens/Colors.xaml", UriKind.Relative) });
            app.Resources.MergedDictionaries.Add(new() { Source = new Uri("/MarketCrate.App;component/Themes/Controls/Controls.xaml", UriKind.Relative) });
            if(args.Contains("--lower-step")){LowerPriceStepUiTests.Run(Test,Render);Console.WriteLine($"LOWER UI RESULT {passed} PASS / {failed} FAIL");return failed==0?0:1;}
            if(args.Contains("--input-paths")){UserInputUiTests.Run(Test,Render);Console.WriteLine($"INPUT UI RESULT {passed} PASS / {failed} FAIL");return failed==0?0:1;}
            using var journal = new SqliteDemoStore(Path.Combine(Folder, Guid.NewGuid().ToString("N")));
            using var vm = new MainViewModel(new(), journal);
            vm.InitializeAsync().GetAwaiter().GetResult();
            var view = new MainView { DataContext = vm };
            using var bindingOutput = new StringWriter();
            var listener = new TextWriterTraceListener(bindingOutput);
            PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
            Test("보관함 1380x820 자체 렌더", () => { Render(view, 1380, 820, 1, "inventory.png"); Check(Find<ListBox>(view).Any(x => x.Items.Count == 5)); });
            Test("실험적 Steam 상태 안내 비표시 렌더", () =>
            {
                var tabs = (TabControl)view.FindName("WorkspaceTabs"); tabs.SelectedIndex = 3;
                Render(view, 1100, 700, 1, "stage4-integration-status.png");
                Check(Find<TextBlock>(view).Any(x => x.Text.Contains("실계정 미검증")));
                var notice = (Border)view.FindName("IntegrationNotice");
                var presenter = Find<ScrollContentPresenter>(tabs).First(x => x.IsAncestorOf(notice));
                Check(notice.TransformToAncestor(presenter).Transform(new Point(notice.ActualWidth, 0)).X <= presenter.ActualWidth);
                Check(Application.Current.Windows.Count == 0 && PresentationSource.FromVisual(view) == null);
                tabs.SelectedIndex = 0;
            });
            Test("창 크기/DPI 출력 밀도 100/125/150/200%", () =>
            {
                foreach (var scale in new[] { 1d, 1.25, 1.5, 2 }) Render(view, 1100, 700, scale, $"inventory-{scale * 100:0}.png");
                Render(view, 1600, 900, 1, "inventory-wide.png");
            });
            Test("최종 검토 레이아웃/단가 바인딩 오류 차단", () =>
            {
                vm.SelectedGame = "Counter-Strike 2"; vm.SelectAll.Execute(null); vm.Items.First(x => x.Selected && x.Maximum == 10).Quantity = 10; vm.Review.Execute(null);
                Render(view, 1100, 700, 1, "review.png"); Check(vm.ReviewLines.Count == 4);
                var list = (ListBox)view.FindName("ReviewList");
                var text = Find<TextBox>(list).First(x => x.GetBindingExpression(TextBox.TextProperty) != null);
                text.Text = "invalid"; text.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
                Check(Validation.GetHasError(text)); Check(vm.HasInputErrors); Check(!vm.Submit.CanExecute(null));
                Render(view, 1100, 700, 1, "review-error.png");
                vm.CloseReview.Execute(null); Check(!vm.HasInputErrors);
                vm.Review.Execute(null); Render(view, 1280, 800, 1, "review-reset.png");
                var submit = Find<Button>(view).First(x => Equals(x.Content, "Mock 배치 전체 실행"));
                var p = submit.TransformToAncestor(view).Transform(new Point()); Check(p.X >= 0 && p.Y + submit.ActualHeight <= 800);
            });
            Test("단일 최종 Mock 실행/진행/완료 렌더", () =>
            {
                vm.Submit.Execute(null); Check(!vm.Busy && !vm.IsReviewOpen); Check(vm.Results.Count == 4);
                var tabs = (TabControl)view.FindName("WorkspaceTabs"); tabs.SelectedIndex = 2;
                Render(view, 1100, 700, 1, "batch-pending.png");
                vm.PublishMockEvidence.Execute(null); Check(vm.Results.Any(x => x.Result.State == MarketCrate.Core.SaleState.Listed));
                Render(view, 1380, 820, 1, "batch-complete.png");
            });
            Test("판매목록 선택/취소 검토/부분 결과 렌더", () =>
            {
                ((TabControl)view.FindName("WorkspaceTabs")).SelectedIndex = 1;
                foreach (var row in vm.Listings) row.Selected = true;
                Check(vm.Listings.Count(x => x.Selected) == 5); vm.CancelPlan.Execute(null);
                Render(view, 1100, 700, 1, "cancel-review.png"); vm.ConfirmCancellation.Execute(null);
                Check(vm.Listings.Any(x => x.Listing.State == MarketCrate.Core.ListingState.Cancelled));
                Check(vm.Listings.Any(x => x.Listing.State == MarketCrate.Core.ListingState.Failed));
                Render(view, 1380, 820, 1, "listings-results.png");
            });
            Test("10,000개 카드 생성 범위/끝 스크롤/필터 Reset/목록 가상화", () =>
            {
                vm.LoadLargeDemo.Execute(null); vm.SelectedGame = "모든 게임"; vm.GridMode = true;
                ((TabControl)view.FindName("WorkspaceTabs")).SelectedIndex = 0;
                var watch = Stopwatch.StartNew(); Render(view, 1380, 820, 1, "large-inventory.png");
                var tiles = Find<VirtualizingTilePanel>(view).Single(); Check(tiles.RealizedCount is > 0 and < 40);
                var initialCount = tiles.RealizedCount;
                tiles.SetVerticalOffset(double.PositiveInfinity); Render(view, 1380, 820, 1, "large-end.png");
                Check(tiles.RealizedCount is > 0 and < 40, $"end containers={tiles.RealizedCount}"); Check(tiles.VerticalOffset > 0, $"end offset={tiles.VerticalOffset}");
                var endCount = tiles.RealizedCount;
                var inventory = (ListBox)view.FindName("InventoryList"); Check(inventory.ItemContainerGenerator.ContainerFromIndex(9999) != null);
                vm.Search = "no-such-item"; Render(view, 1100, 700, 1, "empty.png"); Check(vm.HasNoItems);
                vm.Search = ""; vm.GridMode = false; Render(view, 1380, 820, 1, "large-list.png");
                Check(Find<VirtualizingStackPanel>(inventory).Any()); Check(Find<ListBoxItem>(inventory).Count() < 50);
                Console.WriteLine($"MEASURE 10,000 items: initial tiles={initialCount}, end tiles={endCount}, fixture+renders={watch.ElapsedMilliseconds}ms (not a benchmark)");
            });
            Test("컨트롤 자체 상태/접근성 템플릿 렌더", () => RenderStates());
            Test("수량 Stepper 범위/바인딩/managed accessibility peer", () =>
            {
                var stepper = new NumericStepper { Maximum = 10, Value = 3, Style = (Style)app.FindResource(typeof(NumericStepper)) };
                stepper.Measure(new(140, 40)); stepper.Arrange(new Rect(0, 0, 140, 40)); stepper.ApplyTemplate();
                var peer = UIElementAutomationPeer.CreatePeerForElement(stepper)!;
                Check(peer.GetAutomationControlType() == AutomationControlType.Spinner);
                var range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue)!;
                range.SetValue(7); Check(stepper.Value == 7);
                ((Button)stepper.Template.FindName("PART_Up", stepper)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Check(stepper.Value == 8);
                stepper.Maximum = 4; Check(stepper.Value == 4);
                stepper.Maximum = int.MaxValue; stepper.Value = int.MaxValue;
                ((Button)stepper.Template.FindName("PART_Up", stepper)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Check(stepper.Value == int.MaxValue);
            });
            ThirdStageUiTests.Run((name, test) => Test(name, test), Render);
            FifthStageUiTests.Run((name, test) => Test(name, test), Render);
            SixthStageUiTests.Run((name,test)=>Test(name,test),Render);
            SeventhStageUiTests.Run((name,test)=>Test(name,test),Render);
            EighthStageUiTests.Run((name,test)=>Test(name,test),Render);
            TenthStageUiTests.Run((name,test)=>Test(name,test),Render);
            FourteenthStageUiTests.Run((name,test)=>Test(name,test),Render);
            FifteenthStageUiTests.Run((name,test)=>Test(name,test),Render);
            LiveOnlyUiTests.Run((name,test)=>Test(name,test),Render);
            FeeResponseUiTests.Run((name,test)=>Test(name,test),Render);
            UsabilityUiTests.Run((name,test)=>Test(name,test),Render);ProductAuditUiTests.Run((name,test)=>Test(name,test),Render);
            SessionPersistenceUiTests.Run((name,test)=>Test(name,test),Render);
            ThirteenthStageUiTests.Run((name,test)=>Test(name,test),Render);
            TwelfthStageUiTests.Run((name,test)=>Test(name,test),Render);
            UserInputUiTests.Run(Test,Render);
            LowerPriceStepUiTests.Run(Test,Render);
            InstantSaleRefreshUiTests.Run(Test,Render);
            EleventhStageUiTests.Run((name,test)=>Test(name,test),Render);
            Test("바인딩 오류 없음 (의도적 invalid 가격 변환 제외)", () =>
            {
                listener.Flush(); var output = bindingOutput.ToString();
                var unexpected = output.Split('\n').Where(x => x.Contains("Error:") && !x.Contains("ConvertBack") && !x.Contains("invalid") && !x.Contains("FormatException"));
                Check(!unexpected.Any(), string.Join('\n', unexpected));
            });
            PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
            Test("최상위 창/PresentationSource 미생성", () => Check(app.Windows.Count == 0 && PresentationSource.FromVisual(view) == null));
        }
        catch (Exception e) { failed++; Console.WriteLine($"FAIL setup {e}"); }
        finally { app.Shutdown(); }
        Console.WriteLine($"UI RESULT {passed} PASS / {failed} FAIL · detached STA/RenderTargetBitmap only; physical input/monitor DPI/popups NOT VERIFIED");
        return failed == 0 ? 0 : 1;
    }
    private static void RenderStates()
    {
        var root = new StackPanel { Background = (Brush)Application.Current.FindResource("Canvas"), Margin = new Thickness(20) };
        var buttons = new List<(Button Button, string State)>();
        foreach (var state in new[] { "Normal", "MouseOver", "Pressed", "Focused", "Disabled" })
        { var button = new Button { Content = "Button / " + state, Width = 240, Height = 42, HorizontalAlignment = HorizontalAlignment.Left }; root.Children.Add(button); buttons.Add((button, state)); }
        root.Children.Add(new CheckBox { Content = "선택됨 / Selected", IsChecked = true });
        root.Children.Add(new RadioButton { Content = "Radio selected", IsChecked = true });
        root.Children.Add(new ToggleButton { Content = "Toggle selected", IsChecked = true, Width = 240 });
        root.Children.Add(new TextBox { Style = (Style)Application.Current.FindResource("SearchBox"), Width = 320, HorizontalAlignment = HorizontalAlignment.Left });
        root.Children.Add(new ComboBox { ItemsSource = new[] { "닫힌 드롭다운 (Popup 미실행)" }, SelectedIndex = 0, Width = 320, HorizontalAlignment = HorizontalAlignment.Left });
        var tooltip = new ToolTip { Content = "자체 템플릿 ToolTip / 팝업 표시 없음", Style = (Style)Application.Current.FindResource(typeof(ToolTip)) };
        Render(tooltip, 400, 80, 1, "tooltip-template.png");
        root.Measure(new(600, 700)); root.Arrange(new Rect(0, 0, 600, 700)); root.UpdateLayout();
        foreach (var (button, state) in buttons)
        {
            if (state == "Disabled") button.IsEnabled = false;
            else Check(VisualStateManager.GoToState(button, state, false), $"missing state {state}");
            Check(button.Template != null);
        }
        PumpRenderQueue();
        var normal = (Border)buttons[0].Button.Template.FindName("B", buttons[0].Button);
        var hover = (Border)buttons[1].Button.Template.FindName("B", buttons[1].Button);
        Check(((SolidColorBrush)normal.Background).Color != ((SolidColorBrush)hover.Background).Color, "Hover must visibly differ from Normal");
        Render(root, 600, 700, 1, "control-states.png");
        var view = new MainView();
        Check(KeyboardNavigation.GetTabNavigation((Grid)view.FindName("ReviewOverlay")) == KeyboardNavigationMode.Cycle);
        Check(ToolTipService.GetInitialShowDelay(view) == 650);
    }
    private static void Test(string name, Action test) { try { test(); passed++; Console.WriteLine("PASS " + name); } catch (Exception e) { failed++; Console.WriteLine($"FAIL {name}: {e}"); } }
    private static void Check(bool ok, string message = "assertion failed") { if (!ok) throw new Exception(message); }
    private static IEnumerable<T> Find<T>(DependencyObject parent) where T : DependencyObject
    { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); if (child is T found) yield return found; foreach (var nested in Find<T>(child)) yield return nested; } }
    private static void Render(FrameworkElement view, int width, int height, double scale, string name)
    {
        view.Measure(new(width, height)); view.Arrange(new Rect(0, 0, width, height)); view.UpdateLayout();
        PumpRenderQueue();
        var bitmap = new RenderTargetBitmap((int)(width * scale), (int)(height * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(view); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(Folder, name)); encoder.Save(file);
    }
    private static void PumpRenderQueue()
    {
        // Only this test thread's WPF dispatcher, with no visible window or input source.
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
