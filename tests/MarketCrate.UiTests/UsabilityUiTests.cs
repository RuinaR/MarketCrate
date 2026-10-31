using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using MarketCrate.App;
using MarketCrate.App.Controls;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
internal static class UsabilityUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("일반 보관함은 미확인3게임을 표시하지 않고 상태 알림을 반영",()=>
        {
            using var vm=new SteamWorkspaceViewModel();var view=new SteamWorkspaceView{DataContext=vm};vm.WorkPage=1;
            render(view,1100,780,1,"usability-product-inventory.png");Check(vm.Games.Count==1&&vm.Game.Name=="Steam 커뮤니티","invented owned games");vm.Game.Update(InventoryScopeState.Empty,0);render(view,1100,780,1.25,"usability-product-empty.png");Check(Find<TextBlock>(view).Any(x=>x.Text=="빈 보관함"),"scope notification absent");
        });
        test("드롭다운 선택 텍스트와 화살표 높이 정렬·포커스 링 보존",()=>
        {
            var combo=new ComboBox{Style=(Style)Application.Current.FindResource(typeof(ComboBox)),Width=260,Height=64,ItemsSource=new[]{"Steam 커뮤니티","긴 게임 이름"},SelectedIndex=0};render(combo,280,84,1.5,"usability-combo-alignment.png");
            var content=(ContentPresenter)combo.Template.FindName("SelectionContent",combo);var button=(ToggleButton)combo.Template.FindName("DropButton",combo);
            Check(content.VerticalAlignment==VerticalAlignment.Center&&button.VerticalContentAlignment==VerticalAlignment.Center&&combo.Template.FindName("FocusRing",combo)is not null,"combo alignment/focus regression");
        });
        test("비동기 조회 중 즉시 Busy 아이콘·작업·중단 표시와 해제",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","busy-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer();
            using(var vm=new SteamWorkspaceViewModel(scope=>new(root,scope.AppId,scope.ContextId,scope.Name,contractHandler:server.Connection(),clock:server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());server.InventoryStarted=new(TaskCreationOptions.RunContinuationsAsynchronously);server.InventoryRelease=new(TaskCreationOptions.RunContinuationsAsynchronously);
                var task=vm.LoadAsync();Complete(server.InventoryStarted.Task);var view=new SteamWorkspaceView{DataContext=vm};vm.WorkPage=1;render(view,1100,780,1,"usability-loading.png");
                var busy=(BusyIndicator)view.FindName("GlobalBusyIndicator");Check(busy.IsActive&&busy.Visibility==Visibility.Visible&&vm.OperationLabel.Contains("보관함"),"busy feedback missing");var peer=UIElementAutomationPeer.CreatePeerForElement(busy);Check(peer?.GetAutomationControlType()==AutomationControlType.ProgressBar,"status accessibility absent");
                vm.CancelOperation.Execute(null);server.InventoryRelease.SetResult();Complete(task);render(view,1100,780,1,"usability-cancelled.png");Check(!busy.IsActive&&busy.Visibility==Visibility.Collapsed,"busy never cleared");
            }
            Directory.Delete(root,true);
        });
        test("판매 중 가격 기준·일괄 검토·수수료 표시 자체 렌더",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","bulk-ui-"+Guid.NewGuid().ToString("N"));using var server=new BulkRepriceContractServer();server.External(3);
            using(var vm=new SteamWorkspaceViewModel(scope=>new(root,scope.AppId,scope.ContextId,scope.Name,contractHandler:server.Connection(),clock:server.Clock)){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadListingsAsync());vm.Game=vm.Games.Single(x=>x.AppId==730);vm.WorkPage=2;vm.SelectAllListings.Execute(null);
                var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,800,1.25,"usability-sales-pricing.png");Check(Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.PrepareListingReprice)),"batch proposal disconnected");
                Complete(vm.PrepareListingRepriceAsync());render(view,1100,800,1,"usability-bulk-review-small.png");render(view,1380,900,2,"usability-bulk-review-200.png");
                Check(vm.ManagementOpen&&vm.ManagementSummary.Contains("Steam")&&vm.ManagementSummary.Contains("실수령")&&server.Cancels==0&&server.Sells==0,"review missing or wrote trade");Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)is null,"native window created");
            }
            Directory.Delete(root,true);
        });
        test("외부 취소 후 새 자산 전체 검토·단일 승인 명령 연결",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","new-target-ui-"+Guid.NewGuid().ToString("N"));using var server=new BulkRepriceContractServer();server.External(2);server.ChangedReturn=true;server.Available.Clear();SteamExperimentalWorkspace? workspace=null;
            using(var vm=new SteamWorkspaceViewModel(scope=>workspace=new(root,scope.AppId,scope.ContextId,scope.Name,new(true,"synthetic fixture only"),server.Connection(),server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadListingsAsync());vm.SelectAllListings.Execute(null);Complete(vm.PrepareListingRepriceAsync());vm.TradeAccepted=true;workspace!.Access.Acknowledge(true,true,true,true);
                var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,800,1,"usability-new-target-cancel.png");Check(Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.CancelForNewReview)&&x.IsEnabled),"cancel bridge disconnected");
                Complete(vm.CancelForNewReviewAsync());Complete(vm.ReviewCancelledTargetsAsync());render(view,1100,800,1.25,"usability-new-target-review.png");render(view,1380,900,2,"usability-new-target-review-200.png");
                Check(vm.ReviewOpen&&vm.ReviewRows.Count==2&&Find<TextBlock>(view).Any(x=>x.Text.Contains("3100"))&&Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.Submit)&&x.IsEnabled)&&server.Sells==0,"new actual asset approval missing");
                Complete(vm.SubmitAsync());Check(server.Sells==2&&server.Cancels==2,"whole sale did not execute once");
            }
            Directory.Delete(root,true);
        });
        test("전체 배치 승인 후 공식 모바일 확인 안내·게시 재조회 명령·진행 결과 렌더",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","guard-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer{PendingAlias=true};
            using(var vm=new SteamWorkspaceViewModel(scope=>new(root,scope.AppId,scope.ContextId,scope.Name,new(true,"synthetic fixture only",true),server.Connection(),server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadAsync());vm.Assets.Single().Selected=true;vm.Assets.Single().Quantity=10;
                Complete(vm.RefreshPricesAsync());vm.BuildReview();vm.TradeAccepted=true;vm.EnableTrading.Execute(null);Complete(vm.SubmitAsync());vm.WorkPage=3;
                var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,800,1.25,"guard-handoff-125.png");render(view,1380,900,2,"guard-handoff-200.png");
                Check(vm.SelectedRequest is not null&&Find<ComboBox>(view).Any(x=>ReferenceEquals(x.ItemsSource,vm.RequestOptions)&&Equals(x.SelectedValue,vm.SelectedRequest)),"empty request picker after submission");
                var picker=Find<ComboBox>(view).Single(x=>ReferenceEquals(x.ItemsSource,vm.RequestOptions));Check(Find<TextBlock>(picker).Any(x=>x.Text.StartsWith("등록 ·"))&&!Find<TextBlock>(picker).Any(x=>x.Text.Contains("Key =")),"DisplayMemberPath ignored by selection template");
                Check(vm.HasApprovalHandoff&&Find<TextBlock>(view).Any(x=>x.Text.Contains("푸시 알림이 없더라도"))&&Find<Button>(view).Any(x=>Equals(x.Content,"승인 후 판매 중 보기")&&ReferenceEquals(x.Command,vm.ShowCurrentListings)),"Guard handoff/command disconnected");
                server.Publish=true;Complete(vm.ShowCurrentListings.ExecuteAsync());render(view,1100,800,1,"guard-posted.png");
                Check(vm.WorkPage==2&&server.MarketHistoryReads==0&&vm.Listings.Count==3,"Normal handoff unexpectedly followed history");
                Check(!vm.HasApprovalHandoff&&vm.Results.All(x=>x.State==SaleState.Listed)&&server.Sends==3&&Application.Current.Windows.Count==0,"false completion/additional submission/native window");
            }
            Directory.Delete(root,true);
        });
        test("ID 없는 요청의 서버 게시 관측은 휴대폰 승인 대기와 별도로 렌더",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","idless-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer{Free=1,MissingId=true};
            using(var vm=new SteamWorkspaceViewModel(scope=>new(root,scope.AppId,scope.ContextId,scope.Name,new(true,"synthetic fixture only"),server.Connection(),server.Clock),true){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadAsync());vm.Assets.Single().Selected=true;Complete(vm.RefreshPricesAsync());vm.BuildReview();vm.TradeAccepted=true;vm.EnableTrading.Execute(null);
                Complete(vm.SubmitAsync());server.Publish=true;Complete(vm.RecoverAsync());vm.WorkPage=3;var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,800,1.25,"idless-server-observation.png");
                Check(vm.ServerPostedObservationCount==1&&!vm.HasApprovalHandoff&&vm.Results.Single().State==SaleState.AwaitingSteamGuard&&Find<TextBlock>(view).Any(x=>x.Text.Contains("귀속은 미확인"))&&server.Sends==1,"observation confused with attribution or awaiting phone");
            }
            Directory.Delete(root,true);
        });
    }
        // Detached control only; no desktop input, popup, or native window.
    private static void Complete(Task task){var watch=Stopwatch.StartNew();while(!task.IsCompleted){if(watch.Elapsed>TimeSpan.FromSeconds(20))throw new TimeoutException();var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
    private static IEnumerable<T> Find<T>(DependencyObject root)where T:DependencyObject{for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);if(c is T found)yield return found;foreach(var n in Find<T>(c))yield return n;}}
    private static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
}
