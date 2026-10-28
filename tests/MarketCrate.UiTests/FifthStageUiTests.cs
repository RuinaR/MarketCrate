using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MarketCrate.App;
using MarketCrate.App.Controls;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure.Steam;

internal static class FifthStageUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("Steam QR 자체 모듈 렌더/승인 대기/로그아웃",()=>
        {
            using var f=new Fixture();Check(f.Server.Calls==0);render(f.View,1100,700,1,"stage5-disconnected.png");
            f.Vm.RiskAccepted=f.Vm.CredentialsAccepted=true;f.Vm.BeginAsync().GetAwaiter().GetResult();Check(f.Vm.QrMatrix is not null);
            var qr=Find<QrCodeView>(f.View).Single();render(f.View,1100,700,1,"stage5-qr.png");Check(qr.Matrix![0,0]&&qr.Matrix[6,6]);
            render(f.View,1100,700,2,"stage5-qr-200.png");f.Server.Clock.Advance();f.Vm.CheckAsync().GetAwaiter().GetResult();Check(f.Vm.Connected&&f.Vm.QrMatrix is null);
            f.Vm.Logout.Execute(null);Check(!f.Vm.Connected&&f.Vm.QrMatrix is null);Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(f.View)==null);
        });
        test("KRW 실제 어댑터 합성 UI 검토/단가 편집/정책 차단",()=>
        {
            using var f=new Fixture();f.Login();f.Vm.LoadAsync().GetAwaiter().GetResult();f.Vm.Assets[0].Selected=true;f.Vm.Assets[0].Quantity=10;f.Vm.RefreshPricesAsync().GetAwaiter().GetResult();
            ((TabControl)f.View.FindName("SteamPages")).SelectedIndex=1;render(f.View,1100,700,1,"stage5-inventory.png");f.Vm.BuildReview();render(f.View,1100,700,1,"stage5-review.png");Check(f.Vm.ReviewRows.Count==3);
            var editor=Find<TextBox>(f.View).First(x=>x.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path=="PriceText");editor.Text="116";editor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();Check(f.Vm.ReviewRows[0].Quote.SellerReceives==101);
            editor.Text="invalid";editor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();Check(!f.Vm.ReviewRows[0].Valid&&!f.Vm.Submit.CanExecute(null));render(f.View,1100,700,1.5,"stage5-invalid-price.png");
            f.Vm.CloseReview.Execute(null);f.Vm.TradeAccepted=true;Check(!f.Vm.EnableTrading.CanExecute(null));Check(f.Server.Sends==0);
        });
        test("실험 연결 화면과 Mock 상단 문구 분리/작은 창 배치",()=>
        {
            var folder=Path.Combine(System.IO.Directory.GetCurrentDirectory(),"runtime","stage5-main-"+Guid.NewGuid().ToString("N"));
            using(var store=new MarketCrate.Infrastructure.SqliteDemoStore(folder))using(var vm=new MainViewModel(new(),store))
            {
                vm.InitializeAsync().GetAwaiter().GetResult();var view=new MainView{DataContext=vm};((TabControl)view.FindName("WorkspaceTabs")).SelectedIndex=4;
                render(view,1100,700,1,"stage5-main-integration.png");Check(Find<TextBlock>(view).Any(x=>x.Text.StartsWith("STEAM EXPERIMENTAL · 사용자")));
                Check(Grid.GetColumnSpan((TabControl)view.FindName("WorkspaceTabs"))==5);Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)==null);
            }
            Directory.Delete(folder,true);
        });
    }
    private sealed class Fixture:IDisposable
    {
        private readonly string folder=Path.Combine(System.IO.Directory.GetCurrentDirectory(),"runtime","stage5-ui-"+Guid.NewGuid().ToString("N"));
        public SteamContractServer Server{get;}=new();public SteamWorkspaceViewModel Vm{get;}public SteamWorkspaceView View{get;}
        public Fixture(){Vm=new(scope=>new SteamExperimentalWorkspace(folder,scope.AppId,scope.ContextId,scope.Name,SteamTradingPolicy.Blocked,Server.Connection(),Server.Clock),true);View=new(){DataContext=Vm};}
        public void Login(){Vm.RiskAccepted=Vm.CredentialsAccepted=true;Vm.BeginAsync().GetAwaiter().GetResult();Server.Clock.Advance();Vm.CheckAsync().GetAwaiter().GetResult();Check(Vm.Connected);}
        public void Dispose(){Vm.Dispose();Server.Dispose();if(Directory.Exists(folder))Directory.Delete(folder,true);}
    }
    private static IEnumerable<T> Find<T>(DependencyObject root)where T:DependencyObject{for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T t)yield return t;foreach(var nested in Find<T>(child))yield return nested;}}
    private static void Check(bool value){if(!value)throw new Exception("Assertion failed");}
}
