using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MarketCrate.App;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure;
using MarketCrate.Infrastructure.Steam;

internal static class TenthStageUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("즉시거래가능가 공통 리소스·Mock 설정·Steam 진입 바인딩",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage10-ui-"+Guid.NewGuid().ToString("N"));
            using(var store=new SqliteDemoStore(folder))using(var vm=new MainViewModel(new(),store))
            {
                Complete(vm.InitializeAsync());var view=new MainView{DataContext=vm};view.ShowSteamWorkspace();render(view,1100,800,1,"stage10-steam-entry.png");
                Check(((TabControl)view.FindName("WorkspaceTabs")).SelectedIndex==4&&((Border)view.FindName("StatusToast")).Visibility==Visibility.Collapsed);
                Check(Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.Steam.OpenOfficialMarket)));
                ((TabControl)view.FindName("WorkspaceTabs")).SelectedIndex=0;render(view,1100,700,1,"stage10-terms-mock.png");
                var combos=Find<ComboBox>(view).Where(x=>ReferenceEquals(x.ItemsSource,vm.Strategies)).ToArray();Check(combos.Length>=1&&combos.All(combo=>Equals(combo.SelectedItem,PriceDisplay.Immediate)&&Equals(combo.ToolTip,PriceDisplay.ImmediateExplanation)));
                Check(!Find<TextBlock>(view).Any(x=>x.Text.Contains("즉시판매")||x.Text.Contains("구매주문 기반 예상가")));
            }
            Directory.Delete(folder,true);
        });
        test("독립 판매목록·일반 화면 내부 코드 제거·최종 가격 검토 자체 렌더",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage10-connected-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer();
            using(var vm=new SteamWorkspaceViewModel(scope=>new SteamExperimentalWorkspace(folder,scope.AppId,scope.ContextId,scope.Name,contractHandler:server.Connection(),clock:server.Clock),true))
            {
                vm.RiskAccepted=vm.CredentialsAccepted=true;Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadAsync());vm.SelectAll.Execute(null);Complete(vm.RefreshPricesAsync());
                var view=new SteamWorkspaceView{DataContext=vm};vm.WorkPage=4;render(view,1100,700,1,"stage10-contract-summary.png");
                Check(!Find<TextBlock>(view).Any(x=>x.Text.Contains("마지막 파싱 관측")||x.Text.Contains("진단 작업 ID:")));
                vm.BuildReview();render(view,1100,800,1.25,"stage10-review-terms.png");
                Check(Find<TextBlock>(view).Any(x=>x.Text==PriceDisplay.ImmediateExplanation)&&vm.ReviewRows.All(x=>x.PriceBasis.Contains(PriceDisplay.Immediate)));
                vm.CloseReview.Execute(null);vm.WorkPage=2;render(view,1100,700,1,"stage10-listings-independent.png");
                Check(Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.LoadListings))&&Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)is null);
            }
            Directory.Delete(folder,true);
        });
    }
    private static void Complete(Task task){var watch=Stopwatch.StartNew();while(!task.IsCompleted){if(watch.Elapsed>TimeSpan.FromSeconds(10))throw new TimeoutException();var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject{for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T item)yield return item;foreach(var next in Find<T>(child))yield return next;}}
    private static void Check(bool value){if(!value)throw new Exception("Assertion failed");}
}
