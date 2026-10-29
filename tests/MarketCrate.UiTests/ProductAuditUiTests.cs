using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MarketCrate.App;
using MarketCrate.App.Controls;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;

internal static class ProductAuditUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("전체 제품 XAML의 모든 주요 Command가 실제 VM 명령 객체로 연결",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","command-audit-"+Guid.NewGuid().ToString("N"));
            using(var vm=new ProductionShellViewModel(folder))
            {
                var count=0;foreach(var(file,target)in new(string,object)[]{("SteamWorkspaceView.xaml",vm.Steam),("DiagnosticsView.xaml",vm.Diagnostics),("ProductionShellView.xaml",vm)})
                foreach(Match match in Regex.Matches(File.ReadAllText(Path.Combine("src","MarketCrate.App",file)),"Command=\"\\{Binding ([A-Za-z][A-Za-z0-9_]*)"))
                {var property=target.GetType().GetProperty(match.Groups[1].Value);Check(property?.GetValue(target)is ICommand,"unbound command: "+file+"/"+match.Groups[1].Value);count++;}
                Check(count>=45,"command audit unexpectedly incomplete");
            }
            Directory.Delete(folder,true);
        });
        test("제품 내비게이션 순서·양방향 매핑·게임 관리 직접 진입",()=>
        {
            using var vm=new SteamWorkspaceViewModel();var view=new SteamWorkspaceView{DataContext=vm};var pages=(TabControl)view.FindName("SteamPages");
            var headers=pages.Items.Cast<TabItem>().Take(5).Select(x=>x.Header?.ToString()).ToArray();Check(headers.SequenceEqual(new[]{"보관함","판매 중","판매 결과","계정 및 설정","게임 · 판매 설정"}),"navigation order");
            foreach(var logical in new[]{1,2,3,0,4}){vm.WorkPage=logical;render(view,1100,780,1,$"audit20-page-{logical}.png");Check(pages.SelectedIndex==vm.NavigationIndex,"navigation mapping");}
            pages.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedIndexProperty,0);render(view,1100,780,1.25,"audit20-inventory-small.png");Check(vm.WorkPage==1,"UI navigation not two-way");
            var button=Find<Button>(view).Single(x=>ReferenceEquals(x.Command,vm.OpenGameSettings));button.Command.Execute(null);Check(vm.WorkPage==4,"game manager unreachable");
        });
        test("실제 셸의 중첩 탭 제거·진단 복귀 시 선택과 뷰 보존",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","shell-audit-"+Guid.NewGuid().ToString("N"));
            using(var vm=new ProductionShellViewModel(folder))
            {
                vm.Steam.WorkPage=1;var asset=new InventoryAsset(new(753,"6","9001"),"Steam 커뮤니티","Synthetic item","Synthetic item",1,false,true,false);var row=new SteamAssetRow(asset,1,()=>{}){Selected=true};vm.Steam.Assets.Add(row);vm.Steam.VisibleAssets.Add(row);
                var view=new ProductionShellView{DataContext=vm};render(view,1100,800,1,"audit20-shell-inventory.png");var original=Find<SteamWorkspaceView>(view).Single();Check(Find<TabControl>(view).Count()==1,"duplicate top navigation remains");
                vm.ShowDiagnostics.Execute(null);render(view,1100,800,1,"audit20-shell-diagnostics.png");Check(vm.DiagnosticsOpen&&!vm.ShowSteam,"diagnostic navigation");vm.ShowDiagnostics.Execute(null);render(view,1100,800,1.5,"audit20-shell-restored.png");Check(vm.ShowSteam&&vm.Steam.WorkPage==1&&row.Selected&&ReferenceEquals(original,Find<SteamWorkspaceView>(view).Single()),"navigation recreated stateful view");
            }
            Directory.Delete(folder,true);
        });
        test("실제 보관함 카드·목록 전환은 선택·수량을 보존하고 10000항목 가상화",()=>
        {
            using var vm=new SteamWorkspaceViewModel();vm.WorkPage=1;var rows=Enumerable.Range(1,10000).Select(i=>new SteamAssetRow(new(new(753,"6",i.ToString()),"Steam 커뮤니티","Synthetic item "+i,"Synthetic item",3,true,true,false),3,()=>{})).ToArray();rows[0].Selected=true;rows[0].Quantity=2;vm.Assets.ReplaceAll(rows);vm.VisibleAssets.ReplaceAll(rows);vm.GridMode=true;
            var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,800,1.25,"audit20-inventory-cards.png");var panel=Find<VirtualizingTilePanel>(view).Single();Check(panel.RealizedCount is >0 and <64,"real inventory tile virtualization lost");panel.SetVerticalOffset(panel.ExtentHeight);render(view,1380,900,2,"audit20-inventory-cards-end.png");Check(panel.RealizedCount<64,"end realization unbounded");vm.GridMode=false;render(view,1100,800,1,"audit20-inventory-list.png");Check(rows[0].Selected&&rows[0].Quantity==2,"view switch changed selection");
        });
        test("보존 작업이 없으면 빈 선택 상자를 숨기고 TBH 표시를 명확히 함",()=>
        {
            using var vm=new SteamWorkspaceViewModel();vm.WorkPage=2;var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1,"fee21-empty-listings.png");
            var combo=Find<ComboBox>(view).Single(x=>ReferenceEquals(x.ItemsSource,vm.PendingRelists));var section=(Grid)combo.Parent;Check(!vm.HasPendingRelists&&section.Visibility==Visibility.Collapsed,"empty plan picker visible");
            var option=new SteamRelistOption("synthetic-only","합성 취소 작업 1건");vm.PendingRelists.Add(option);vm.SelectedRelist=option;render(view,1100,780,1.25,"fee21-plan-picker.png");Check(section.Visibility==Visibility.Visible,"real pending plan picker hidden");vm.PendingRelists.Clear();render(view,1100,780,1,"fee21-plan-cleared.png");Check(section.Visibility==Visibility.Collapsed,"empty picker residue");
            var tbh=new SteamGameScope(3678970,"2","TBH: Task Bar Hero");vm.Games.Add(tbh);vm.Game=tbh;vm.WorkPage=1;render(view,1100,780,1.25,"fee21-tbh.png");Check(Find<TextBlock>(view).Any(x=>x.Text=="TBH: Task Bar Hero")&&tbh.State==InventoryScopeState.NotQueried,"TBH placeholder claimed ownership or not visible");
        });
    }
    private static IEnumerable<T> Find<T>(DependencyObject root)where T:DependencyObject{for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T value)yield return value;foreach(var nested in Find<T>(child))yield return nested;}}
    private static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
}
