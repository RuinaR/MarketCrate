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
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;

internal static class FeeResponseUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("수수료 HTML 계약 오류·인증 유지·사용자 상세 안내 비표시 렌더",()=>WithVm(true,(vm,server)=>
        {
            vm.CheckFees=vm.CheckWallet=vm.CheckListings=vm.CheckInventory=true;vm.CheckApproved=true;Complete(vm.SelfCheckAsync());vm.WorkPage=5;
            var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1,"fee-response-error-small.png");
            var fee=vm.CheckRows.Single(x=>x.Step=="Fees");Check(vm.Connected&&fee.Result!.Error==DiagnosticError.Schema&&fee.Detail.Contains("장터 화면 HTML"),"misleading login/CAPTCHA UI");
            fee.ShowDetails=true;var list=Find<ListBox>(view).Single(x=>ReferenceEquals(x.ItemsSource,vm.CheckRows));list.ScrollIntoView(fee);render(view,1380,900,1.5,"fee-response-error-detail.png");
            list.ScrollIntoView(fee);view.UpdateLayout();var row=list.ItemContainerGenerator.ContainerFromItem(fee)as FrameworkElement;
            Check(row is not null,"fee result not realized after explicit scroll");render(row!,1000,260,1.25,"fee-response-error-row.png");
            Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)is null,"native GUI created");
        }));
        test("공식 수수료 입력 비교 UI 연결·금액 반영·거래 권한 불변",()=>WithVm(false,(vm,server)=>
        {
            Complete(vm.LoadAsync());vm.WorkPage=4;vm.OfficialSeller="100";vm.OfficialBuyer="115";
            var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1,"fee-comparison-small.png");
            var button=Find<Button>(view).Single(x=>Equals(x.Content,"입력값 비교"));Check(ReferenceEquals(button.Command,vm.CompareOfficialFees)&&button.IsEnabled,"comparison button disconnected");
            button.Command.Execute(null);Check(vm.FeeComparison.Contains("계산 일치")&&server.Sends==0&&server.Cancels==0,"comparison wrote transaction");render(view,1380,900,1.5,"fee-comparison-large.png");
            vm.OfficialBuyer="100";button.Command.Execute(null);Check(vm.FeeComparison.Contains("불일치"),"mismatch hidden");
        }));
    }
    private static void WithVm(bool html,Action<SteamWorkspaceViewModel,SteamContractServer> test)
    {
        var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","fee-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer();
        using(var vm=new SteamWorkspaceViewModel(scope=>new SteamExperimentalWorkspace(folder,scope.AppId,scope.ContextId,scope.Name,contractHandler:new Router(server,html),clock:server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
        {Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());test(vm,server);}
        Directory.Delete(folder,true);
    }
    private sealed class Router(SteamContractServer server,bool html):HttpMessageHandler
    {
        private readonly HttpMessageInvoker inner=new(server.Connection());
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>html&&request.RequestUri!.AbsolutePath.Contains("ajaxfeestructure")?Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("<html><div id='market_home'>market</div></html>",System.Text.Encoding.UTF8,"text/html")}):inner.SendAsync(request,token);
        protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
    }
    private static void Complete(Task task){var watch=Stopwatch.StartNew();while(!task.IsCompleted){if(watch.Elapsed>TimeSpan.FromSeconds(20))throw new TimeoutException();var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T found)yield return found;foreach(var nested in Find<T>(child))yield return nested;}}
    private static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
}
