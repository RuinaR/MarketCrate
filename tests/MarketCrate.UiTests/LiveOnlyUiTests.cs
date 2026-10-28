using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MarketCrate.App;
using MarketCrate.App.ViewModels;
internal static class LiveOnlyUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("실제 Steam 전용 셸·빈 데이터·진단 연결·Mock 탭 없음",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","live-shell-"+Guid.NewGuid().ToString("N"));try
            {
                using var vm=new ProductionShellViewModel(folder);var view=new ProductionShellView{DataContext=vm};render(view,1100,780,1,"live-only-shell-small.png");
                Check(!vm.Steam.Connected&&vm.Steam.Assets.Count==0&&vm.Steam.Listings.Count==0,"fabricated data or automatic connection");
                Check(Find<TabItem>(view).All(x=>!x.Header?.ToString()?.Contains("Mock")??true),"Mock UI reachable");
                vm.ShowDiagnostics.Execute(null);Check(vm.Page==1,"diagnostic command disconnected");render(view,1380,900,1.5,"live-only-diagnostics.png");
                Check(Find<DiagnosticsView>(view).Any(x=>ReferenceEquals(x.DataContext,vm.Diagnostics)),"diagnostic view disconnected");
                Check(!Directory.Exists(Path.Combine(folder,"Demo"))&&!Directory.Exists(Path.Combine(folder,"SteamExperimental"))&&Application.Current.Windows.Count==0,"demo/auth/window side effect");
            }finally{Directory.Delete(folder,true);}
        });
        test("실제 셸 SQLite 설정 복원·운영 저널 및 Demo 폴더 미접근",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","live-settings-"+Guid.NewGuid().ToString("N"));try
            {
                using(var vm=new ProductionShellViewModel(folder)){vm.ReducedMotion=false;}
                using(var vm=new ProductionShellViewModel(folder))Check(!vm.ReducedMotion&&vm.Steam.Assets.Count==0,"settings or empty live state mismatch");
                Check(Directory.GetFiles(folder).All(x=>Path.GetFileName(x).StartsWith("preferences.sqlite3",StringComparison.Ordinal)),"unexpected live/demo journal touched");
            }finally{Directory.Delete(folder,true);}
        });
    }
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T match)yield return match;foreach(var next in Find<T>(child))yield return next;}}
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
}
