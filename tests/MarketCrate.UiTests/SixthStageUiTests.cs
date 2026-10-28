using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MarketCrate.App;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure;
using MarketCrate.Infrastructure.Diagnostics;

internal static class SixthStageUiTests
{
    internal static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("진단 커스텀 UI 비표시 100/125/150/200% 자체 렌더",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage6-ui-"+Guid.NewGuid().ToString("N"));
            using(var log=new JsonDiagnosticLog(folder))using(DiagnosticHub.Use(log))
            {
                using(var scope=DiagnosticHub.Scope())DiagnosticHub.Emit(DiagnosticEvent.HttpFailure,DiagnosticArea.HTTP,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticError.RateLimited,endpoint:"Inventory",status:429);
                var vm=new DiagnosticsViewModel(log,_=>throw new Exception("Folder must not open during rendering"));var view=new DiagnosticsView{DataContext=vm};
                foreach(var scale in new[]{1d,1.25,1.5,2})render(view,1100,700,scale,$"stage6-diagnostics-{scale:0.##}.png");
                Check(Find<Button>(view).All(x=>x.Template is not null));Check(vm.Events.Single().Record.HttpStatus==429);
                var toggle=Find<CheckBox>(view).First(x=>(string)x.Content=="최근 오류만");toggle.IsChecked=true;toggle.GetBindingExpression(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty)!.UpdateSource();Check(vm.ErrorsOnly);
                var input=Find<TextBox>(view).First(x=>x.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path=="OperationFilter");input.Text="invalid";input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();Check(!vm.ReviewPackage.CanExecute(null));
                render(view,1100,700,1,"stage6-diagnostics-invalid.png");Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)is null);
            }
            Directory.Delete(folder,true);
        });
        test("진단 탭 전체 너비와 Mock/실험 배치 영역 분리",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage6-main-ui-"+Guid.NewGuid().ToString("N"));
            using(var store=new SqliteDemoStore(folder))using(var vm=new MainViewModel(new(),store))
            {
                var view=new MainView{DataContext=vm};var tabs=(TabControl)view.FindName("WorkspaceTabs");tabs.SelectedIndex=5;
                render(view,1100,700,1,"stage6-main-diagnostics-small.png");Check(Grid.GetColumnSpan(tabs)==5);Check(Find<TextBlock>(view).Any(x=>x.Text.StartsWith("DIAGNOSTICS ·")));
                render(view,1440,900,1.25,"stage6-main-diagnostics-wide.png");Check(Find<DiagnosticsView>(view).Count()==1);
            }
            Directory.Delete(folder,true);
        });
        test("Steam 검색/정렬/해제 UI 바인딩과 신규 재가격 검토 명령",()=>
        {
            using var vm=new SteamWorkspaceViewModel();var view=new SteamWorkspaceView{DataContext=vm};((TabControl)view.FindName("SteamPages")).SelectedIndex=1;
            render(view,1100,700,1,"stage6-steam-filters.png");var search=Find<TextBox>(view).First(x=>x.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path=="Search");search.Text="synthetic";search.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();Check(vm.Search=="synthetic");
            Check(Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.ClearSelection)));((TabControl)view.FindName("SteamPages")).SelectedIndex=4;
            render(view,1100,700,1,"stage6-steam-evidence.png");Check(Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.ReviewStoredReprice)));Check(!vm.ReviewStoredReprice.CanExecute(null));Check(Application.Current.Windows.Count==0);
        });
    }
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T item)yield return item;foreach(var nested in Find<T>(child))yield return nested;}}
    private static void Check(bool condition){if(!condition)throw new Exception("Assertion failed");}
}
