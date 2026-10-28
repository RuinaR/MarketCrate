using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MarketCrate.App;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure.Steam;
internal static class FourteenthStageUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("실제 관측 변형 합성8건·빈 보관함·조회권한 제한 요약과 상세 접기",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage14-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer{Free=0,Publish=true};for(int i=0;i<8;i++)server.Rows.Add(((900+i).ToString(),1,100));
            using(var vm=new SteamWorkspaceViewModel(scope=>new SteamExperimentalWorkspace(folder,scope.AppId,scope.ContextId,scope.Name,contractHandler:new Router(server),clock:server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());vm.CheckWallet=true;vm.CheckListings=vm.CheckInventory=vm.CheckFees=vm.CheckOrders=vm.CheckHistory=vm.CheckImage=true;vm.CheckApproved=true;Complete(vm.SelfCheckAsync());vm.WorkPage=5;
                var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,800,1,"stage14-summary.png");Check(vm.Connected&&vm.FeatureRestricted&&vm.CheckSummary.Contains("성공 4")&&vm.CheckSummary.Contains("차단 5"),"summary conflated failed and blocked");Check(vm.CheckRows.All(x=>!x.ShowDetails),"details not collapsed");var details=Find<StackPanel>(view).Where(x=>x.GetBindingExpression(UIElement.VisibilityProperty)?.ParentBinding.Path?.Path=="ShowDetails").ToArray();Check(details.Length>0&&details.All(x=>x.Visibility==Visibility.Collapsed),"repeated IDs still expanded");
                var wallet=vm.CheckRows.Single(x=>x.Step=="Wallet");wallet.ShowDetails=true;var list=Find<ListBox>(view).Single(x=>ReferenceEquals(x.ItemsSource,vm.CheckRows));view.UpdateLayout();list.ScrollIntoView(wallet);view.UpdateLayout();Check(wallet.Classification=="Authorization"&&Find<TextBox>(view).Any(x=>x.Text==wallet.Operation),"expanded cause or ID missing");var row=(FrameworkElement)list.ItemContainerGenerator.ContainerFromItem(wallet);render(row,1000,260,1.25,"stage14-details.png");Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)is null,"native window created");
            }
            Directory.Delete(folder,true);
        });
    }
    private sealed class Router(SteamContractServer server):HttpMessageHandler
    {
        private readonly HttpMessageInvoker inner=new(server.Connection());
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){if(request.RequestUri!.AbsolutePath=="/market/"){var response=new HttpResponseMessage(HttpStatusCode.Found){Content=new StringContent("")};response.Headers.Location=new("https://steamcommunity.com/login/home/");return Task.FromResult(response);}return inner.SendAsync(request,token);}
        protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
    }
    private static void Complete(Task task){var watch=Stopwatch.StartNew();while(!task.IsCompleted){if(watch.Elapsed>TimeSpan.FromSeconds(20))throw new TimeoutException();var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T found)yield return found;foreach(var nested in Find<T>(child))yield return nested;}}
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
}
