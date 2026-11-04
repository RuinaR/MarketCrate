using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MarketCrate.App;
using MarketCrate.App.Controls;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
using MarketCrate.Tests;
internal static class TbhPriceDisplayUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("빠른 참고 시세는 상세 옵션이며 기본Steam·독립통화·명령 바인딩 유지",()=>
        {
            Complete(TbhPriceDisplayTests.FastReference());
            using var vm=new SteamWorkspaceViewModel();vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("a","P0","P0",1,false,"stash")],0));vm.WorkPage=6;var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1,"fast-reference-option.png");
            Check(vm.TbhPriceSource.StartsWith("Steam")&&vm.QuickTbhPrices.CanExecute(null),"default source or explicit option invalid");
            var detail=Find<Expander>(view).Single(x=>Equals(x.Header,"자료·조회 상세"));detail.IsExpanded=true;render(view,1100,780,1.5,"fast-reference-details.png");Check(Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.QuickTbhPrices)),"reference action disconnected");
        });
        test("30원 제외 체크박스는 기본OFF·바인딩·빈 상태·다양한 폭에서 표시",()=>
        {
            TbhPriceDisplayTests.ThirtyWonFilter();
            using var vm=new SteamWorkspaceViewModel();vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("a","Unknown","Unknown",1,false,"stash")],0));vm.WorkPage=6;
            var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1,"tbh-thirty-filter.png");
            var option=Find<CheckBox>(view).Single(x=>System.Windows.Automation.AutomationProperties.GetName(x)=="조회 단가 30원인 항목 제외");
            Check(option.IsChecked==false,"filter must be opt-in");option.IsChecked=true;Check(vm.TbhExcludeThirtyWon&&vm.TbhSaveRows.Count==1,"unknown price hidden or binding broken");
            render(view,960,720,1.5,"tbh-thirty-filter-150.png");option.IsChecked=false;Check(!vm.TbhExcludeThirtyWon,"off binding broken");
        });
        test("48px 아이콘은 원본 네 모서리를 모두 표시하고 프레임 안에 맞춤",()=>
        {
            using var server=new ImageContractServer();var data=new byte[128*128*4];for(int y=0;y<128;y++)for(int x=0;x<128;x++){var i=(y*128+x)*4;data[i]=(byte)(y>=64?255:0);data[i+1]=(byte)(x>=64?255:0);data[i+2]=(byte)(x<64&&y<64?255:0);data[i+3]=255;}
            var source=System.Windows.Media.Imaging.BitmapSource.Create(128,128,96,96,PixelFormats.Bgra32,null,data,512);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source));using var stream=new MemoryStream();encoder.Save(stream);server.Bytes=stream.ToArray();
            using var cache=new SteamImageCache(server);var thumb=new ItemThumbnail{Width=48,Height=48,Reference=new("quad"),Loader=cache};Complete(thumb.RefreshAsync());render(thumb,48,48,2,"thumbnail48-quad.png");var image=Find<Image>(thumb).Single();var bounds=image.TransformToAncestor(thumb).TransformBounds(new Rect(0,0,image.ActualWidth,image.ActualHeight));Check(bounds.Left>=0&&bounds.Top>=0&&bounds.Right<=48.01&&bounds.Bottom<=48.01,"image escapes the48px frame: "+bounds);
        });
        test("실제 게임 행 BAML의 아이콘 프레임이56px 열을 벗어나지 않음",()=>
        {
            using var vm=new SteamWorkspaceViewModel();vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("a","Quad","Quad",1,false,"stash")],0));vm.WorkPage=6;var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1,"thumbnail-real-row.png");var thumb=Find<ItemThumbnail>(view).Single();Check(thumb.ActualWidth<=56&&thumb.ActualHeight<=56,"BAML thumbnail size "+thumb.ActualWidth+"x"+thumb.ActualHeight+" requested "+thumb.Width+"x"+thumb.Height);
        });
        test("Steam 제한 시각·잔여 시간 바인딩과 비네트워크 UI 타이머 수명",()=>
        {
            var page=SteamHoldInfoTests.Page(["Tradable/Marketable After Nov 9, 2026 (3:00:00) GMT"]);using var vm=new SteamWorkspaceViewModel();var row=new SteamAssetRow(page.Assets.Single(),1,()=>{}){HoldInfo=page.Holds.Single().Value};vm.Assets.Add(row);vm.MarketableOnly=false;vm.WorkPage=1;
            var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1.25,"steam-hold-countdown.png");Check(Find<TextBlock>(view).Any(x=>x.Text.Contains("아이템 교환 제한")&&x.Text.Contains("KST")),"hold countdown binding missing");Check(!row.CanSelect,"countdown changed server marketability");
            var timer=(DispatcherTimer)typeof(SteamWorkspaceView).GetField("holdTimer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(view)!;view.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));Check(timer.IsEnabled,"UI clock not started");view.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));Check(!timer.IsEnabled&&Application.Current.Windows.Count==0,"UI clock retained after unload/window occurred");
        });
        test("게임 금액의 부분 합계·미확인 그룹·장비 재료 필터·지갑 독립 바인딩",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","display23-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer();using var route=new TbhPriceDisplayTests.Router(server.Connection()){BadPrice=true};using var vm=new SteamWorkspaceViewModel(scope=>new(root,scope.AppId,scope.ContextId,scope.Name,contractHandler:route,clock:server.Clock),true){RiskAccepted=true,CredentialsAccepted=true};
            Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadWallet.ExecuteAsync());vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("1","Good","Good",2,false,"창고",Kind:TbhItemKind.Equipment),new("2","Bad","Bad",1,false,"창고",Kind:TbhItemKind.Equipment),new("3","Minor Ruby",null,3,false,"재료창고",Kind:TbhItemKind.Material)],0));Complete(vm.RefreshTbhPrices.ExecuteAsync());vm.WorkPage=6;var view=new SteamWorkspaceView{DataContext=vm};render(view,1380,900,1.25,"tbh23-partial-groups.png");
            Check(Find<TextBlock>(view).Any(x=>x.Text.Contains("가격 미확인 · 순위 없음"))&&vm.TbhValuationCoverage.Contains("1개 항목 금액만")&&vm.WalletBalanceLabel.Contains("1,350"),"scope totals/group/balance unclear");var button=Find<Button>(view).Single(x=>ReferenceEquals(x.Command,vm.LoadWallet));Check(button.Content!.ToString()!.Contains("Steam 지갑"),"wallet command disconnected");var kinds=Find<ComboBox>(view).Single(x=>ReferenceEquals(x.ItemsSource,vm.TbhKinds));kinds.SelectedIndex=2;render(view,1100,780,2,"tbh23-material-filter.png");Check(vm.TbhSaveRows.Count==1&&vm.TbhSaveRows[0].Item.Kind==TbhItemKind.Material,"material filter not connected");kinds.SelectedIndex=1;Check(vm.TbhSaveRows.Count==2,"equipment filter not connected");
            vm.TbhKindFilter=0;vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,Enumerable.Range(0,10000).Select(i=>new TbhOwnedItem(i.ToString(),"Synthetic "+i,null,1,false,"창고")).ToArray(),0));render(view,1100,780,1,"tbh23-10000-grouped.png");Check(Find<ListBoxItem>(view).Count() is >0 and <64,"grouped list lost virtualization");Check(server.Sends==0&&server.Cancels==0&&Application.Current.Windows.Count==0,"trade/window occurred");
        });
        test("불확실 위치·범위 초과 행은 별도 제외하고 정상 항목·필터 UI를 보존",()=>
        {
            using var vm=new SteamWorkspaceViewModel();
            var quotes=(Dictionary<string,PriceSnapshot>)typeof(SteamWorkspaceViewModel).GetField("tbhQuotes",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(vm)!;
            quotes["Shared"]=new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[new(100,1)]){Asks=[new(150,1)]};
            quotes["Huge"]=new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[new(long.MaxValue/2,10)]){Asks=[new(long.MaxValue/2,10)]};
            vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("a","위치 미확인","Shared",1,false,"위치 미확인",Ambiguous:true),new("b","정상 아이템","Shared",1,false,"1번 창고",Kind:TbhItemKind.Material),new("c","금액 초과","Huge",10,false,"2번 창고",Kind:TbhItemKind.Equipment)],0));
            vm.WorkPage=6;var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1.25,"audit24-row-errors.png");
            Check(vm.TbhSaveRows.Count(x=>x.PricedTotal is not null)==1&&vm.TbhValuationCoverage.Contains("미확인 2"),"unsafe row entered total");
            var kinds=Find<ComboBox>(view).Single(x=>ReferenceEquals(x.ItemsSource,vm.TbhKinds));kinds.SelectedIndex=2;render(view,1380,900,2,"audit24-material-recovery.png");
            Check(vm.TbhSaveRows.Single().Item.Kind==TbhItemKind.Material&&vm.TbhSaveRows.Single().PricedTotal==100,"error blocked independent filter");
            Check(Application.Current.Windows.Count==0,"top-level window occurred");
        });
        test("판매호가 미수신과 검증된 빈 주문은 화면·행 툴팁·조회 요약에서 구별",()=>
        {
            using var vm=new SteamWorkspaceViewModel();var quotes=(Dictionary<string,PriceSnapshot>)typeof(SteamWorkspaceViewModel).GetField("tbhQuotes",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(vm)!;
            quotes["Missing"]=new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[new(100,1)]);
            quotes["Empty"]=new("KRW",DateTimeOffset.UtcNow,"synthetic",TimeSpan.Zero,[],[]){SellDepthObserved=true};
            vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("a","Missing","Missing",1,false,"stash"),new("b","Empty","Empty",1,false,"stash")],0));vm.TbhPriceMode=1;vm.WorkPage=6;
            var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1.25,"value-read-missing-versus-empty.png");
            Check(vm.TbhSaveRows.Single(x=>x.Name=="Missing").ReadState==TbhValueReadState.SellDepthMissing&&vm.TbhSaveRows.Single(x=>x.Name=="Empty").ReadState==TbhValueReadState.NoSellOrders,"missing and empty conflated");
            Check(Find<TextBlock>(view).Any(x=>x.DataContext is TbhSaveRow row&&row.ReadState==TbhValueReadState.SellDepthMissing&&x.ToolTip is string detail&&detail.Contains("필드 미수신")),"read evidence tooltip binding missing");
            Check(vm.TbhReadSummary.Contains("판매호가 필드 미수신")&&vm.TbhReadSummary.Contains("조회한 판매주문 없음"),"read status summary lost reason");
        });
        test("거래불가 정상 제외는 미확인 오류와 별도 그룹·집계로 표시",()=>
        {
            using var vm=new SteamWorkspaceViewModel();vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("1","Reliquary",null,1,false,"stash",CanExchangeMarketable:false,TradeEvidence:TbhTradeEvidence.UserConfirmedCategory),new("2","Event unknown",null,1,false,"stash")],0));vm.WorkPage=6;var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1.25,"nonmarketable-normal-exclusion.png");
            Check(vm.TbhValuationCoverage.Contains("거래불가 1개 정상 제외")&&vm.TbhValuationCoverage.Contains("미확인 1개")&&Find<TextBlock>(view).Any(x=>x.Text=="거래불가 · 가격 조회 제외"),"nonmarketable mixed into errors");
        });
        test("가시 썸네일의 공개 상품 메타데이터→Steam CDN 실제 PNG 디코딩",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","icon23-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer();using var route=new TbhPriceDisplayTests.Router(server.Connection());using var pixels=new ImageContractServer();SteamExperimentalWorkspace? workspace=null;using var vm=new SteamWorkspaceViewModel(scope=>workspace=new(root,scope.AppId,scope.ContextId,scope.Name,contractHandler:route,clock:server.Clock,imageHandler:pixels),true){RiskAccepted=true,CredentialsAccepted=true};Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());var image=new ItemThumbnail{MarketName="Synthetic stack",MetadataLoader=workspace!.MarketImages,Loader=workspace.Images};render(image,76,84,1,"tbh23-image-loading.png");Complete(image.RefreshAsync());Check(image.ImageState=="Ready"&&image.HasPixels&&route.ImageCalls==1,"market metadata did not reach pixels");render(image,76,84,2,"tbh23-image-ready.png");image.Release();Check(!image.HasPixels,"recycled pixels retained");
        });
    }
    private static void Complete(Task task){var w=Stopwatch.StartNew();while(!task.IsCompleted){if(w.Elapsed>TimeSpan.FromSeconds(30))throw new TimeoutException();var f=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>f.Continue=false));Dispatcher.PushFrame(f);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
    private static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    private static IEnumerable<T> Find<T>(DependencyObject p)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(p);i++){var c=VisualTreeHelper.GetChild(p,i);if(c is T t)yield return t;foreach(var x in Find<T>(c))yield return x;}}
}
