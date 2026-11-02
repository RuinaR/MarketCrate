using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using MarketCrate.App;
using MarketCrate.App.ViewModels;
using MarketCrate.App.Controls;

internal static class CleanWorkspaceUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("간결한 TBH 작업 공간·좌측 탐색·내장 한글 글꼴·크기별 가독성",()=>
        {
            using var vm=new SteamWorkspaceViewModel();vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("110001","Minor Ruby","Minor Ruby",14,false,"재료창고"),new("110002","아주 긴 아이템 이름을 가진 장비 · Celestial C","Synthetic",1,false,"3번 창고 · 2열 1행")],0));
            var view=new SteamWorkspaceView{DataContext=vm};
            foreach(var size in new[]{(1100,760,1d),(1380,860,1.25),(1100,760,2d)})render(view,size.Item1,size.Item2,size.Item3,$"clean22-items-{size.Item1}-{size.Item3}.png");
            var tabs=(TabControl)view.FindName("SteamPages");Check(tabs.TabStripPlacement==Dock.Left&&tabs.ItemContainerStyle==Application.Current.FindResource("WorkspaceNavigationTab"),"sidebar disconnected");
            Check(new Typeface(view.FontFamily,FontStyles.Normal,FontWeights.Normal,FontStretches.Normal).TryGetGlyphTypeface(out var face)&&face.FontUri.ToString().Contains("MarketCrate.App")&&face.Weight==FontWeights.Normal&&face.CharacterToGlyphMap.ContainsKey('한'),"bundled Korean font unresolved");
            Check(new Typeface(view.FontFamily,FontStyles.Normal,FontWeights.SemiBold,FontStretches.Normal).TryGetGlyphTypeface(out var bold)&&bold.Weight==FontWeights.SemiBold,"bundled semibold unresolved");
            var details=Find<Expander>(view).Single(x=>Equals(x.Header,"자료·조회 상세"));Check(!details.IsExpanded,"technical details expanded by default");var toggle=(System.Windows.Controls.Primitives.ToggleButton)details.Template.FindName("Disclosure",details);toggle.IsChecked=true;Check(details.IsExpanded,"disclosure not linked");toggle.IsChecked=false;
            vm.WorkPage=1;render(view,1100,760,1.25,"clean22-steam-empty.png");Check(vm.TbhSaveRows.Count==2,"page change lost save snapshot");vm.WorkPage=2;render(view,1100,760,1.25,"clean22-listings-empty.png");vm.WorkPage=0;render(view,1100,760,1.25,"clean22-account.png");
            Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)==null,"visible window");
        });
        test("좌측 탐색 Header 전용 Hover·선택·키보드 포커스·Disabled 복원",()=>
        {
            var tab=new TabItem{Header="게임 아이템",Style=(Style)Application.Current.FindResource("WorkspaceNavigationTab"),Tag=Application.Current.FindResource("IconItems")};render(tab,160,52,1,"clean22-nav-normal.png");var border=(Border)tab.Template.FindName("B",tab);var normal=EleventhStageUiTests.Snapshot(border);
            EleventhStageUiTests.State(tab,"IsMouseOver",true,typeof(UIElement));Check(EleventhStageUiTests.Snapshot(border)==normal,"content-inclusive hover leaked");EleventhStageUiTests.State(tab,"IsMouseOver",false,typeof(UIElement));EleventhStageUiTests.State(border,"IsMouseOver",true,typeof(UIElement));Check(EleventhStageUiTests.Snapshot(border)!=normal,"header hover absent");EleventhStageUiTests.State(border,"IsMouseOver",false,typeof(UIElement));Check(EleventhStageUiTests.Snapshot(border)==normal,"hover residue");
            tab.IsSelected=true;var selected=EleventhStageUiTests.Snapshot(border);Check(selected!=normal,"selection absent");EleventhStageUiTests.State(tab,"IsKeyboardFocused",true,typeof(UIElement));Check(((FrameworkElement)tab.Template.FindName("FocusRing",tab)).Visibility==Visibility.Visible,"keyboard cue absent");EleventhStageUiTests.State(tab,"IsKeyboardFocused",false,typeof(UIElement));tab.IsEnabled=false;Check(border.Opacity==0.4,"disabled missing");tab.IsEnabled=true;Check(EleventhStageUiTests.Snapshot(border)==selected,"disabled residue");tab.IsSelected=false;Check(EleventhStageUiTests.Snapshot(border)==normal,"selection residue");
            var owner=new TabControl{Style=(Style)Application.Current.FindResource("WorkspaceTabs")};owner.Items.Add(tab);render(owner,1100,760,1,"clean22-native-selector.png");var peer=new TabControlAutomationPeer(owner);Check(peer.GetChildren()?.Any(x=>x.GetPattern(PatternInterface.SelectionItem)is ISelectionItemProvider)==true,"native selector accessibility lost");
        });
    }
    private static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    private static IEnumerable<T> Find<T>(DependencyObject p)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(p);i++){var c=VisualTreeHelper.GetChild(p,i);if(c is T t)yield return t;foreach(var child in Find<T>(c))yield return child;}}
}
