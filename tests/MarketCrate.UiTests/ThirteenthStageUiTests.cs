using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MarketCrate.App;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure.Steam;
internal static class ThirteenthStageUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("자가검증 범위 기본OFF·진입 명령·승인·비활성 안내 바인딩",()=>
        {
            using var vm=new SteamWorkspaceViewModel(offlineOnly:true);var view=new SteamWorkspaceView{DataContext=vm};vm.ShowSelfCheck.Execute(null);render(view,1100,800,1,"stage13-selfcheck-offline.png");
            Check(((TabControl)view.FindName("SteamPages")).SelectedIndex==5,"entry disconnected");Check(!vm.CheckApproved&&!vm.CheckListings&&!vm.CheckInventory&&!vm.CheckWallet&&!vm.CheckOrders&&!vm.CheckImage,"implicit read consent");Check(Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.SelfCheck)&&!x.IsEnabled),"unapproved command enabled");Check(Find<CheckBox>(view).Any(x=>x.GetBindingExpression(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty)?.ParentBinding.Path?.Path=="CheckApproved"),"consent missing");
        });
        test("자가검증 실제 어댑터 합성 성공·차단 결과와 작업 ID 자체 렌더",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage13-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer{IconToken="synthetic_image"};using var images=new ImageContractServer();
            using(var vm=new SteamWorkspaceViewModel(scope=>new SteamExperimentalWorkspace(folder,scope.AppId,scope.ContextId,scope.Name,contractHandler:server.Connection(),clock:server.Clock,imageHandler:images),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadAsync());vm.Assets[0].Selected=true;vm.CheckWallet=true;vm.CheckListings=vm.CheckInventory=vm.CheckFees=vm.CheckOrders=vm.CheckHistory=vm.CheckImage=true;vm.CheckApproved=true;Complete(vm.SelfCheckAsync());vm.ShowSelfCheck.Execute(null);
                var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,800,1,"stage13-selfcheck-success-small.png");render(view,1380,900,1.25,"stage13-selfcheck-success.png");Check(vm.CheckRows.All(x=>x.Result?.Outcome==ReadCheckOutcome.Succeeded),"synthetic check failed");Check(Find<TextBlock>(view).Any(x=>x.Text.Contains("합성 HTTP 검증")),"synthetic source absent");Check(Find<TextBox>(view).Any(x=>x.Text==vm.LastOperationId?.ToString()),"operation ID absent");
                server.InventoryStarted=new(TaskCreationOptions.RunContinuationsAsynchronously);server.InventoryRelease=new(TaskCreationOptions.RunContinuationsAsynchronously);vm.CheckApproved=true;var running=vm.SelfCheckAsync();Complete(server.InventoryStarted.Task);render(view,1100,800,1,"stage13-selfcheck-running.png");Check(!((Grid)view.FindName("SelfCheckScopes")).IsEnabled&&!((CheckBox)view.FindName("SelfCheckConsent")).IsEnabled,"running scope could be changed");Check(Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.CancelOperation)&&x.IsEnabled),"cancel unavailable");vm.CancelOperation.Execute(null);server.InventoryRelease.SetResult();Complete(running);server.InventoryRelease=null;
                server.PrivateInventory=true;vm.CheckApproved=true;Complete(vm.SelfCheckAsync());render(view,1100,800,1,"stage13-selfcheck-blocked.png");Check(vm.Connected&&vm.RequestsBlocked&&!vm.SelfCheck.CanExecute(null),"restricted requests enabled");Check(vm.CheckRows.Any(x=>x.Result?.Outcome==ReadCheckOutcome.Blocked),"blocked result missing");Check(server.Sends==0&&server.Cancels==0&&Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)is null,"write/native window occurred");
            }
            Directory.Delete(folder,true);
        });
        test("페이지 탐색 명령과 Selector 양방향 바인딩 상태 보존",()=>
        {
            using var vm=new SteamWorkspaceViewModel(offlineOnly:true);var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,800,1,"stage13-navigation-start.png");var pages=(TabControl)view.FindName("SteamPages");vm.ShowSelfCheck.Execute(null);render(view,1100,800,1,"stage13-navigation-check.png");Check(pages.SelectedIndex==5,"command navigation failed");pages.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedIndexProperty,1);render(view,1100,800,1,"stage13-navigation-inventory.png");Check(vm.WorkPage==1,"UI selection not reflected in VM");vm.WorkPage=4;render(view,1100,800,1,"stage13-navigation-advanced.png");Check(pages.SelectedIndex==4,"VM selection not reflected in UI");
        });
    }
    private static void Complete(Task task){var watch=Stopwatch.StartNew();while(!task.IsCompleted){if(watch.Elapsed>TimeSpan.FromSeconds(20))throw new TimeoutException();var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T found)yield return found;foreach(var nested in Find<T>(child))yield return nested;}}
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
}
