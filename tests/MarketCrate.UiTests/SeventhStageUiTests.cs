using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MarketCrate.App;
using MarketCrate.App.Controls;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure.Steam;

internal static class SeventhStageUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("Steam 이미지 실제 PNG 디코딩·재활용60회·해제·오류 복구 자체 렌더",()=>
        {
            using var server=new ImageContractServer();using var cache=new SteamImageCache(server,4,2048);var control=new ItemThumbnail{Loader=cache,Reference=new("one")};using var own=Process.GetCurrentProcess();var cpuStart=own.TotalProcessorTime;var allocation=GC.GetTotalAllocatedBytes(false);var time=Stopwatch.StartNew();
            for(var i=0;i<60;i++){control.Reference=new("image"+(i%2));Complete(control.RefreshAsync());Check(control.HasPixels&&control.ImageState=="Ready");control.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));Check(!control.HasPixels);}
            Complete(control.RefreshAsync());render(control,100,100,2,"stage7-thumbnail.png");Check(server.Calls==2&&cache.Health.Entries<=2);
            control.Reference=new("fault");server.Status=500;Complete(control.RefreshAsync());Check(control.ImageState=="Failed");render(control,100,100,1,"stage7-thumbnail-failed.png");server.Status=200;Complete(control.RefreshAsync());Check(control.ImageState=="Ready");control.Release();
            Console.WriteLine($"MEASURE detached thumbnail 60 cycles: {time.ElapsedMilliseconds}ms wall / {(own.TotalProcessorTime-cpuStart).TotalMilliseconds:0}ms own CPU, allocated {GC.GetTotalAllocatedBytes(false)-allocation} bytes; cache {cache.Health.Bytes} bytes (short synthetic run)");
        });
        test("썸네일 늦은 응답·재활용 취소가 새 항목을 덮지 않음",()=>
        {
            using var server=new ImageContractServer{Release=new(TaskCreationOptions.RunContinuationsAsynchronously)};using var cache=new SteamImageCache(server);var control=new ItemThumbnail{Loader=cache,Reference=new("old")};var loading=control.RefreshAsync();PumpUntil(()=>server.Calls==1);control.Reference=null;control.Release();server.Release.SetResult();Complete(loading);Check(!control.HasPixels);control.Reference=new("new");Complete(control.RefreshAsync());Check(control.HasPixels);control.Release();
        });
        test("이미지 없음·꺼짐·미지원·로딩 상태 템플릿과 접근성",()=>
        {
            var panel=new StackPanel{Background=(Brush)Application.Current.FindResource("Canvas"),Orientation=Orientation.Horizontal};
            foreach(var reference in new ItemImageReference?[]{null,new(null,true),new("disabled")}){var thumbnail=new ItemThumbnail{Reference=reference};Complete(thumbnail.RefreshAsync());panel.Children.Add(thumbnail);}
            render(panel,400,120,1,"stage7-image-states.png");Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(panel)is null);
        });
        test("Steam 오류 작업 ID에서 진단 화면으로 실제 명령 연결",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage7-jump-"+Guid.NewGuid().ToString("N"));using(var store=new MarketCrate.Infrastructure.SqliteDemoStore(folder))using(var vm=new MainViewModel(new(),store))
            {
                Complete(vm.Steam.BeginAsync());Check(vm.Steam.LastOperationId is not null);var view=new MainView{DataContext=vm};var tabs=(TabControl)view.FindName("WorkspaceTabs");tabs.SelectedIndex=4;render(view,1100,800,1,"stage7-main-workflow.png");Find<Button>(view).Single(x=>Equals(x.Content,"현재 Steam 작업의 진단 보기")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(tabs.SelectedIndex==5&&vm.Diagnostics.OperationFilter==vm.Steam.LastOperationId.ToString());
            }
            Directory.Delete(folder,true);
        });
        test("Steam 게임/context·가격 기준·보관함/판매/검토 이미지 WPF 바인딩",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage7-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer{IconToken="synthetic_image"};using var images=new ImageContractServer();
            using(var vm=new SteamWorkspaceViewModel(scope=>new SteamExperimentalWorkspace(folder,scope.AppId,scope.ContextId,scope.Name,SteamTradingPolicy.Blocked,server.Connection(),server.Clock,images),true))
            {
                vm.RiskAccepted=vm.CredentialsAccepted=true;Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadAsync());vm.ImagesEnabled=true;vm.Assets[0].Selected=true;vm.Assets[0].Quantity=10;Complete(vm.RefreshPricesAsync());
                var view=new SteamWorkspaceView{DataContext=vm};var tabs=(TabControl)view.FindName("SteamPages");vm.WorkPage=1;render(view,1100,800,1,"stage7-inventory-before.png");foreach(var thumb in Find<ItemThumbnail>(view))Complete(thumb.RefreshAsync());
                render(view,1100,800,1,"stage7-inventory.png");Check(Find<ItemThumbnail>(view).Any(x=>x.HasPixels));
                vm.BuildReview();render(view,1100,800,1,"stage7-review-before.png");foreach(var thumb in Find<ItemThumbnail>(view))Complete(thumb.RefreshAsync());render(view,1100,800,1.5,"stage7-review.png");Check(vm.ReviewRows.All(x=>x.Image?.Token=="synthetic_image"));vm.CloseReview.Execute(null);
                vm.PricingMode=2;Complete(vm.RefreshPricesAsync());vm.BuildReview();render(view,1100,800,1,"stage7-estimate-before.png");foreach(var thumb in Find<ItemThumbnail>(view))Complete(thumb.RefreshAsync());render(view,1100,800,1.25,"stage7-estimate-review.png");Check(vm.ReviewRows.Single().PriceBasis.Contains("추정"));vm.CloseReview.Execute(null);
                server.Rows.Add(("900",1,100));server.Publish=true;Complete(vm.LoadAsync());vm.Listings.Single().Selected=true;vm.ListingMetrics.Execute(null);PumpUntil(()=>!vm.Busy);Check(vm.Listings.Single().Metric.Contains("추정"));vm.WorkPage=2;render(view,1100,800,1,"stage7-listings-before.png");foreach(var thumb in Find<ItemThumbnail>(view))Complete(thumb.RefreshAsync());render(view,1100,800,2,"stage7-listings.png");Check(vm.Listings.Single().Image?.Token=="synthetic_image");
                foreach(var thumb in Find<ItemThumbnail>(view))thumb.Release();Check(images.Calls==1&&!images.CredentialLeak);Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)is null);
            }
            Directory.Delete(folder,true);
        });
    }
    private static void Complete(Task task){PumpUntil(()=>task.IsCompleted);task.GetAwaiter().GetResult();}
    private static void PumpUntil(Func<bool> ready)
    {
        var watch=Stopwatch.StartNew();while(!ready()){if(watch.Elapsed>TimeSpan.FromSeconds(10))throw new TimeoutException("Detached test timeout");var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}
    }
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject{for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T item)yield return item;foreach(var next in Find<T>(child))yield return next;}}
    private static void Check(bool value){if(!value)throw new Exception("Assertion failed");}
}
