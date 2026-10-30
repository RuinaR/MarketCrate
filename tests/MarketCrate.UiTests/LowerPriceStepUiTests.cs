using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MarketCrate.App;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
internal static class LowerPriceStepUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("한 단계 하향 체크박스·기준/변경 수수료·실수령 감소·단일 승인 연결",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","lower-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer();
            using(var vm=new SteamWorkspaceViewModel(scope=>new(root,scope.AppId,scope.ContextId,scope.Name,new(true,"synthetic",true),server.Connection(),server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginLogin.ExecuteAsync());server.Clock.Advance();Complete(vm.CheckLogin.ExecuteAsync());Complete(vm.Load.ExecuteAsync());vm.SelectAll.Execute(null);vm.Assets[0].Quantity=10;Complete(vm.RefreshPrices.ExecuteAsync());vm.WorkPage=1;
                var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1,"lower-option-off.png");var option=Find<CheckBox>(view).First(x=>x.Content?.ToString()=="즉시거래가능가보다 한 단계 낮게");Check(option.IsChecked==false&&!vm.LowerPriceOneStep,"Default changed");option.IsChecked=true;Check(vm.LowerPriceOneStep,"Option binding broken");vm.Review.Execute(null);
                vm.TradeAccepted=true;vm.EnableTrading.Execute(null);render(view,1100,780,1.25,"lower-review-125.png");
                Check(vm.ReviewRows.All(x=>x.LowerStepRequested)&&Find<TextBlock>(view).Any(x=>x.Text.Contains("전체 합계"))&&Find<TextBlock>(view).Any(x=>x.Text.Contains("실수령 감소")),"Comparison absent");
                var list=Find<ListBox>(view).Single(x=>ReferenceEquals(x.ItemsSource,vm.ReviewRows));Check(list.ActualHeight>=140,"Comparison displaced rows");render(view,1100,780,2,"lower-review-200.png");
                var row=vm.ReviewRows[0];var before=vm.ReviewAdjustmentSummary;row.PriceText=SteamReviewRow.FormatPrice(row.BaselineQuote!.BuyerPays,"KRW");Check(vm.ReviewAdjustmentSummary!=before,"Manual input did not recalculate comparison");
                Complete(vm.Submit.ExecuteAsync());Check(server.Sends==3&&vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard),"Single approval path broken");
                Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)==null,"Window was displayed");
            }
            Directory.Delete(root,true);
        });
        test("최저 유효 가격은 하향 불가·수동 입력으로 차단 우회 불가",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","lower-min-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer();
            using(var vm=new SteamWorkspaceViewModel(scope=>new(root,scope.AppId,scope.ContextId,scope.Name,new(true,"synthetic",true),server.Connection(),server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadAsync());vm.SelectAll.Execute(null);Complete(vm.RefreshPricesAsync());vm.LowerPriceOneStep=true;vm.BuildReview();
                var row=new SteamReviewRow(vm.Assets[0].Asset,1,30,new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[new(30,1)]),new("KRW",1000,1000,new(50000),new(100000)),()=>{},lowerOneStep:true);vm.ReviewRows.Clear();vm.ReviewRows.Add(row);vm.TradeAccepted=true;vm.EnableTrading.Execute(null);row.PriceText="30";
                Check(!row.Valid&&!vm.Submit.CanExecute(null)&&vm.ReviewAdjustmentSummary.Contains("계산 불가"),"Minimum silently used original price");var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1.25,"lower-minimum-blocked.png");Check(server.Sends==0,"Unexpected submission");
            }
            Directory.Delete(root,true);
        });
    }
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    private static void Complete(Task task){var watch=Stopwatch.StartNew();while(!task.IsCompleted){if(watch.Elapsed>TimeSpan.FromSeconds(20))throw new TimeoutException();var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T value)yield return value;foreach(var nested in Find<T>(child))yield return nested;}}
}
