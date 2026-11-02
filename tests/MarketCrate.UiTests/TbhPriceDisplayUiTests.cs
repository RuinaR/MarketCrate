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
        test("가시 썸네일의 공개 상품 메타데이터→Steam CDN 실제 PNG 디코딩",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","icon23-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer();using var route=new TbhPriceDisplayTests.Router(server.Connection());using var pixels=new ImageContractServer();SteamExperimentalWorkspace? workspace=null;using var vm=new SteamWorkspaceViewModel(scope=>workspace=new(root,scope.AppId,scope.ContextId,scope.Name,contractHandler:route,clock:server.Clock,imageHandler:pixels),true){RiskAccepted=true,CredentialsAccepted=true};Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());var image=new ItemThumbnail{MarketName="Synthetic stack",MetadataLoader=workspace!.MarketImages,Loader=workspace.Images};render(image,76,84,1,"tbh23-image-loading.png");Complete(image.RefreshAsync());Check(image.ImageState=="Ready"&&image.HasPixels&&route.ImageCalls==1,"market metadata did not reach pixels");render(image,76,84,2,"tbh23-image-ready.png");image.Release();Check(!image.HasPixels,"recycled pixels retained");
        });
    }
    private static void Complete(Task task){var w=Stopwatch.StartNew();while(!task.IsCompleted){if(w.Elapsed>TimeSpan.FromSeconds(30))throw new TimeoutException();var f=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>f.Continue=false));Dispatcher.PushFrame(f);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
    private static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    private static IEnumerable<T> Find<T>(DependencyObject p)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(p);i++){var c=VisualTreeHelper.GetChild(p,i);if(c is T t)yield return t;foreach(var x in Find<T>(c))yield return x;}}
}
