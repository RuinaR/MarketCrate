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
internal static class InstantSaleRefreshUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("즉시 체결 가능성이 있는 목록 부재는 승인 대기 대신 불명·거래내역 안내",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","instant-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer{Free=1,MissingId=true};
            using(var vm=new SteamWorkspaceViewModel(scope=>new(root,scope.AppId,scope.ContextId,scope.Name,new(true,"synthetic",true),server.Connection(),server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadAsync());vm.SelectAll.Execute(null);Complete(vm.RefreshPricesAsync());vm.BuildReview();vm.TradeAccepted=true;vm.EnableTrading.Execute(null);Complete(vm.Submit.ExecuteAsync());
                if(!vm.HasApprovalHandoff)throw new Exception("Initial Guard receipt missing");
                vm.WorkPage=3;var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1,"instant-sale-before.png");
                server.Rows.Clear();Complete(vm.Recover.ExecuteAsync());render(view,1100,780,1.25,"instant-sale-unknown-125.png");
                if(vm.Results.Single().State!=SaleState.Unknown||vm.HasApprovalHandoff||server.Sends!=1||!Find<TextBlock>(view).Any(x=>x.Text.Contains("거래내역")))throw new Exception("Disappeared receipt misrepresented or guidance unbound");
                render(view,1100,780,2,"instant-sale-unknown-200.png");
                if(Application.Current.Windows.Count!=0||PresentationSource.FromVisual(view)!=null)throw new Exception("Window displayed");
            }
            Directory.Delete(root,true);
        });
        test("거래내역 일치한 즉시 체결은 판매 완료·단일 전송·재시작용 저널 반영",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","sold-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer{Free=1};SteamExperimentalWorkspace? workspace=null;
            using(var vm=new SteamWorkspaceViewModel(scope=>workspace=new(root,scope.AppId,scope.ContextId,scope.Name,new(true,"synthetic",true),server.Connection(),server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadAsync());vm.SelectAll.Execute(null);Complete(vm.RefreshPricesAsync());vm.BuildReview();vm.TradeAccepted=true;vm.EnableTrading.Execute(null);Complete(vm.Submit.ExecuteAsync());
                var command=workspace!.Store.Commands().Single();var id=command.ListingId!;var at=command.SentAt!.Value.ToUnixTimeSeconds();var asset=command.Line.Asset.Key;var received=SteamContract.ToWire(command.Line.UnitQuote.SellerReceives,"KRW");
                server.Rows.Clear();server.MarketHistoryOverride=System.Text.Json.JsonSerializer.Serialize(new{success=true,events=new[]{new{listingid=id,purchaseid="880",event_type=3,time_event=at}},listings=new Dictionary<string,object>{{id,new{listingid=id,currencyid=2016,original_price=received,asset=new{appid=asset.AppId,contextid=asset.ContextId,id=asset.AssetId}}}},purchases=new Dictionary<string,object>{{id+"_880",new{listingid=id,purchaseid="880",time_sold=at,received_currencyid="2016",received_amount=received,asset=new{appid=asset.AppId,contextid=asset.ContextId,amount="1"},funds_held=1}}}});
                Complete(vm.Recover.ExecuteAsync());vm.WorkPage=3;var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1.25,"instant-sale-sold-125.png");render(view,1100,780,2,"instant-sale-sold-200.png");
                if(vm.Results.Single().State!=SaleState.Sold||vm.HasApprovalHandoff||server.Sends!=1||!Find<TextBlock>(view).Any(x=>x.Text=="판매 완료")||!Find<TextBlock>(view).Any(x=>x.Text.Contains("Steam 보류")))throw new Exception("Actual result binding lost sale/held status");
                if(Application.Current.Windows.Count!=0||PresentationSource.FromVisual(view)!=null)throw new Exception("Window displayed");
            }
            Directory.Delete(root,true);
        });
    }
    private static void Complete(Task task){var watch=Stopwatch.StartNew();while(!task.IsCompleted){if(watch.Elapsed>TimeSpan.FromSeconds(20))throw new TimeoutException();var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T value)yield return value;foreach(var nested in Find<T>(child))yield return nested;}}
}
