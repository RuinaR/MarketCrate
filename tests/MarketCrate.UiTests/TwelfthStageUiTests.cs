using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure.Steam;
using MarketCrate.App.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MarketCrate.App;

internal static class TwelfthStageUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("Tab Header 외부·하위 컨트롤 Hover가 상위 Header를 변경하지 않음",()=>
        {
            var child=new StackPanel();child.Children.Add(new Button{Content="하위 버튼"});child.Children.Add(new CheckBox{Content="하위 체크"});child.Children.Add(new ComboBox());child.Children.Add(new TextBox());
            var tab=EleventhStageUiTests.Styled(new TabItem{Header="상위 Header",Content=child,IsSelected=true});render(tab,300,60,1,"stage12-tab-normal.png");var b=(Border)tab.Template.FindName("B",tab);var initial=EleventhStageUiTests.Snapshot(b);
            EleventhStageUiTests.State(tab,"IsMouseOver",true,typeof(UIElement));Check(EleventhStageUiTests.Snapshot(b)==initial,"content-inclusive TabItem hover changed Header");
            foreach(Control c in child.Children){EleventhStageUiTests.State(c,"IsMouseOver",true,typeof(UIElement));Check(EleventhStageUiTests.Snapshot(b)==initial,"child hover changed header");EleventhStageUiTests.State(c,"IsMouseOver",false,typeof(UIElement));}
            EleventhStageUiTests.State(b,"IsMouseOver",true,typeof(UIElement));Check(EleventhStageUiTests.Snapshot(b)!=initial,"header hover missing");render(tab,300,60,1,"stage12-tab-hover.png");EleventhStageUiTests.State(b,"IsMouseOver",false,typeof(UIElement));Check(EleventhStageUiTests.Snapshot(b)==initial&&tab.IsSelected,"Header exit lost selected state");
        });
        test("새 Steam 정보 구조·실제 바인딩·크기별 자체 렌더",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage12-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer();server.ManyAssets=100;server.Rows.Add(("900",1,100));server.Publish=true;
            using(var vm=new SteamWorkspaceViewModel(scope=>new SteamExperimentalWorkspace(folder,scope.AppId,scope.ContextId,scope.Name,contractHandler:server.Connection(),clock:server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadAsync());vm.SelectAll.Execute(null);Complete(vm.RefreshPricesAsync());
                var view=new SteamWorkspaceView{DataContext=vm};var pages=(TabControl)view.FindName("SteamPages");render(view,1100,700,1,"stage12-account.png");Check(Find<TextBlock>(view).Any(x=>x.Text=="Steam 인증 완료"),"authenticated header absent");
                pages.SelectedIndex=1;
                foreach(var size in new[]{(1100,700,1d),(1380,860,1.25),(1600,900,1.5),(1100,800,2d)})
                {
                    render(view,size.Item1,size.Item2,size.Item3,$"stage12-inventory-{size.Item1}-{size.Item3}.png");var list=(ListBox)view.FindName("InventoryList");Check(list.ActualWidth>=300&&list.ActualHeight>=200,"inventory collapsed");Check(VirtualizingPanel.GetIsVirtualizing(list)&&VirtualizingPanel.GetVirtualizationMode(list)==VirtualizationMode.Recycling,"virtualization lost");Check(!Find<TextBox>(view).Any(x=>x.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path?.Path=="AppInput"),"advanced fields leaked into inventory");
                }
                var inventory=(ListBox)view.FindName("InventoryList");var scrolling=Find<ScrollViewer>(inventory).First();scrolling.ScrollToVerticalOffset(30);render(view,1100,700,1,"stage12-scroll.png");var offset=scrolling.VerticalOffset;Check(offset>0,"test list did not scroll");pages.SelectedIndex=2;render(view,1100,700,1,"stage12-scroll-away.png");pages.SelectedIndex=1;render(view,1100,700,1,"stage12-scroll-back.png");Check(Find<ScrollViewer>(inventory).First().VerticalOffset==offset,"page switch lost scroll");
                var before=vm.Assets[0];before.Quantity=1;before.Selected=true;var first=vm.Game;vm.Game=vm.Games[1];vm.Game=first;Check(ReferenceEquals(before,vm.Assets[0])&&before.Selected,"game view selection recreated");render(view,1100,700,1,"stage12-game-return.png");Check(Find<ScrollViewer>(inventory).First().VerticalOffset==offset,"game switch lost scroll");
                pages.SelectedIndex=2;render(view,1100,700,1,"stage12-listings.png");Check(Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.LoadListings)),"listing command disconnected");
                pages.SelectedIndex=4;render(view,1100,700,1,"stage12-advanced.png");Check(Find<TextBox>(view).Any(x=>x.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path?.Path=="AppInput"),"advanced fields missing");
                Complete(vm.LoadAsync());Complete(vm.RefreshPricesAsync());vm.BuildReview();render(view,1100,800,1,"stage12-review.png");Check(vm.ReviewOpen&&!((FrameworkElement)view.FindName("SteamBody")).IsEnabled,"modal body remains active");vm.CloseReview.Execute(null);
                Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)is null,"native window created");
            }
            Directory.Delete(folder,true);
        });
        test("헤더 키보드 점선·선택 밑줄·포인터 분리",()=>
        {
            var tab=EleventhStageUiTests.Styled(new TabItem{Header="진단 / 로그",IsSelected=true});render(tab,300,60,1,"stage12-selected.png");var border=(Border)tab.Template.FindName("B",tab);var baseline=EleventhStageUiTests.Snapshot(border);var ring=(FrameworkElement)tab.Template.FindName("FocusRing",tab);
            EleventhStageUiTests.State(tab,"IsKeyboardFocused",true,typeof(UIElement));Check(ring.Visibility==Visibility.Visible&&EleventhStageUiTests.Snapshot(border)==baseline,"focus imitated hover");FocusCue.SetKeyboardMode(tab,false);Check(ring.Visibility==Visibility.Collapsed,"pointer focus visible");FocusCue.SetKeyboardMode(tab,true);EleventhStageUiTests.State(tab,"IsKeyboardFocused",false,typeof(UIElement));Check(ring.Visibility==Visibility.Collapsed&&tab.IsSelected,"keyboard unfocus cleared selection");
        });
    }
    private static void Complete(Task task){var watch=Stopwatch.StartNew();while(!task.IsCompleted){if(watch.Elapsed>TimeSpan.FromSeconds(20))throw new TimeoutException();var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T found)yield return found;foreach(var nested in Find<T>(child))yield return nested;}}
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
}
