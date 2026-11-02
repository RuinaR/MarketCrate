using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MarketCrate.App;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;

internal static class TbhSaveUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("TBH 전용 기본 scope·별도 세이브 화면·잠금/장착 필터·위치 바인딩",()=>
        {
            using var vm=new SteamWorkspaceViewModel();Check(vm.Games.Count==1&&vm.Game.AppId==TbhScope.AppId,"Non-TBH product scope");vm.WorkPage=6;
            vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("110001","Minor Ruby","Minor Ruby",2,false,"3번 창고 · 2열 1행"),new("110002","Minor Sapphire","Minor Sapphire",1,true,"가방 · 슬롯 1"),new("110003","Minor Topaz","Minor Topaz",1,false,"영웅 1 · 장비 슬롯 1",Equipped:true)],0));
            var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1.25,"tbh-save-default-125.png");Check(vm.TbhSaveRows.Count==1&&vm.NavigationIndex==((TabControl)view.FindName("SteamPages")).SelectedIndex,"Save tab/filters disconnected");
            Find<CheckBox>(view).Single(x=>Equals(x.Content,"잠금 아이템 포함")).IsChecked=true;Check(vm.TbhSaveRows.Count==2,"Lock filter");Find<CheckBox>(view).Single(x=>Equals(x.Content,"장착 아이템 포함")).IsChecked=true;Check(vm.TbhSaveRows.Count==3,"Equipment filter");
            render(view,1100,780,2,"tbh-save-all-200.png");Check(Find<Grid>(view).Any(x=>x.ToolTip?.ToString()=="3번 창고 · 2열 1행"),"Location tooltip disconnected");Check(Find<Button>(view).Single(x=>ReferenceEquals(x.Command,vm.ReadTbhSave)).IsEnabled&&!vm.RefreshTbhPrices.CanExecute(null),"Offline read / authenticated prices boundary");Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)==null,"Visible GUI");
        });
        test("게임 세이브 시세는 합성 실제 어댑터 조회·가격 기준 전환·판매 요청0",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","save-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer();server.OrderbookOverride="{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[11500,3],\"rgCompactSellOrders\":[57500,1]}}}";
            using(var vm=new SteamWorkspaceViewModel(scope=>new(folder,scope.AppId,scope.ContextId,scope.Name,new(true,"Synthetic",true),server.Connection(),server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("1","A","Synthetic stack",2,false,"1번 창고 · 1열 1행"),new("2","B","Synthetic stack",2,false,"1번 창고 · 2열 1행")],0));vm.WorkPage=6;Complete(vm.RefreshTbhPrices.ExecuteAsync());Check(vm.TbhSaveRows.Sum(x=>x.PricedTotal)==345,"Shared bid allocation missing");var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1.25,"tbh-save-prices-125.png");Find<ComboBox>(view).Single(x=>ReferenceEquals(x.ItemsSource,vm.TbhPriceModes)).SelectedIndex=1;Check(vm.TbhSaveRows.All(x=>x.UnitPrice==575),"Save price mode binding");render(view,1100,780,2,"tbh-save-asks-200.png");Check(server.Sends==0&&server.Cancels==0&&!vm.ReviewOpen&&!vm.ManagementOpen,"Save valuation sent a trade");
            }
            Directory.Delete(folder,true);
        });
    }
    private static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    private static IEnumerable<T> Find<T>(DependencyObject p)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(p);i++){var c=VisualTreeHelper.GetChild(p,i);if(c is T t)yield return t;foreach(var x in Find<T>(c))yield return x;}}
    private static void Complete(Task task){var w=Stopwatch.StartNew();while(!task.IsCompleted){if(w.Elapsed>TimeSpan.FromSeconds(20))throw new TimeoutException();var f=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>f.Continue=false));Dispatcher.PushFrame(f);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
}
