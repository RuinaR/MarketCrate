using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MarketCrate.App;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure.Steam;

internal static class FifteenthStageUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("실거래 기본OFF·별도세션 확인·개발용 활성화 분리 렌더",()=>
        {
            using var vm=new SteamWorkspaceViewModel();vm.WorkPage=4;
            var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,800,1,"stage15-trading-off.png");
            Check(!vm.ContractToolsVisible&&!vm.PrepareTrading.CanExecute(null)&&!vm.EnableTrading.CanExecute(null),"unsafe default");
            Check(Find<TextBox>(view).Any(x=>x.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path?.Path=="TradingConfirmation"),"confirmation UI disconnected");
            Check(Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.PrepareTrading)),"preflight UI disconnected");
            var panel=Find<TextBlock>(view).Single(x=>x.GetBindingExpression(TextBlock.TextProperty)?.ParentBinding.Path?.Path=="TradingPreparation");
            render((FrameworkElement)VisualTreeHelper.GetParent(panel),1000,700,1.25,"stage15-trading-controls.png");
            Check(Application.Current.Windows.Count==0,"native window shown");
        });
        test("다른 게임 선택 보존→가격 갱신→전체검토→단일 승인 두게임 처리",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage15-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer{Free=2,DynamicInventory=true};
            using(var vm=new SteamWorkspaceViewModel(scope=>new SteamExperimentalWorkspace(folder,scope.AppId,scope.ContextId,scope.Name,new(true,"synthetic only",true),server.Connection(),server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadAsync());vm.Assets[0].Selected=true;vm.Assets[0].Quantity=1;
                vm.Game=vm.Games[1];Complete(vm.LoadAsync());vm.Assets[0].Selected=true;vm.Assets[0].Quantity=1;Complete(vm.RefreshPricesAsync());
                Check(vm.Review.CanExecute(null),"review command disconnected");vm.Review.Execute(null);Check(vm.ReviewRows.Count==2&&vm.SelectionSummary.Contains("2"),"other game omitted");
                var view=new SteamWorkspaceView{DataContext=vm};render(view,1380,900,1,"stage15-multi-game-review.png");
                Check(vm.ReviewRows.Select(x=>x.Asset.Key.AppId).Distinct().Count()==2&&vm.ReviewRows.All(x=>x.Identity.Contains(x.Asset.Game)),"game labels ambiguous");
                vm.TradeAccepted=true;vm.EnableTrading.Execute(null);vm.Submit.Execute(null);CompleteUntil(()=>!vm.Busy);Check(server.Sends==2&&vm.Results.Count==2,"single approval did not process both");
                Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)is null,"window/input source created");
            }
            Directory.Delete(folder,true);
        });
    }
    private static void Complete(Task task){CompleteUntil(()=>task.IsCompleted);task.GetAwaiter().GetResult();}
    private static void CompleteUntil(Func<bool> done){var watch=Stopwatch.StartNew();while(!done()){if(watch.Elapsed>TimeSpan.FromSeconds(25))throw new TimeoutException();var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}}
    private static IEnumerable<T> Find<T>(DependencyObject root)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T result)yield return result;foreach(var next in Find<T>(child))yield return next;}}
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
}
