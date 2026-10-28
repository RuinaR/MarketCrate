using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MarketCrate.App.Controls;

internal static class EleventhStageUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("Action 공통 상태 단일 소유자와 별도 포커스 표시",()=>
        {
            var button=Styled(new Button{Content="상태 검사"});render(button,240,60,1,"stage11-action-before.png");
            Check(!button.Template.Triggers.OfType<Trigger>().Any(x=>x.Property==UIElement.IsMouseOverProperty||x.Property==ButtonBase.IsPressedProperty),"duplicate VSM and trigger ownership");
            Check(button.Template.FindName("FocusRing",button)is FrameworkElement,"focus must have its own cue");
        });
        test("Button/Toggle/Repeat 상태 왕복·브러시 공유 오염 없음",()=>
        {
            foreach(var control in new ButtonBase[]{new Button{Content="Button"},new ToggleButton{Content="Toggle"},new RepeatButton{Content="Repeat"}})
            {
                Styled(control);render(control,240,60,1,"stage11-"+control.GetType().Name+".png");var border=(Border)control.Template.FindName("B",control);var initial=Snapshot(border);
                for(var cycle=0;cycle<8;cycle++)
                {
                    State(control,"IsMouseOver",true,typeof(UIElement));Check(Snapshot(border)!=initial,"hover not applied");State(control,"IsMouseOver",false,typeof(UIElement));Check(Snapshot(border)==initial,"hover residue");
                    State(control,"IsPressed",true,typeof(ButtonBase));State(control,"IsPressed",false,typeof(ButtonBase));Check(Snapshot(border)==initial,"pressed residue");
                    State(control,"IsKeyboardFocused",true,typeof(UIElement));State(control,"IsKeyboardFocused",false,typeof(UIElement));Check(Snapshot(border)==initial,"focus residue");
                    control.IsEnabled=false;Dispatch(control);Check(border.Opacity<1,"disabled opacity missing");control.IsEnabled=true;Dispatch(control);Check(Snapshot(border)==initial,"disabled residue");
                }
                var sibling=Styled(new Button{Content="Sibling"});render(sibling,240,60,1,"stage11-sibling.png");var siblingBorder=(Border)sibling.Template.FindName("B",sibling);var siblingState=Snapshot(siblingBorder);
                State(control,"IsMouseOver",true,typeof(UIElement));Check(Snapshot(siblingBorder)==siblingState,"shared brush changed sibling");State(control,"IsMouseOver",false,typeof(UIElement));
                var groups=VisualStateManager.GetVisualStateGroups(border);Check(groups.OfType<VisualStateGroup>().First(x=>x.Name=="CommonStates").CurrentState?.Name=="Normal","actual state must return Normal");
            }
        });
        test("Check/Radio 체크·포인터·키보드 포커스·비활성 상태 분리",()=>
        {
            foreach(var c in new ToggleButton[]{new CheckBox{Content="최근 오류만"},new RadioButton{Content="가격 선택"}})
            {
                Styled(c);render(c,280,50,1,"stage11-"+c.GetType().Name+".png");var frame=(Border)c.Template.FindName("Frame",c);var baseline=Snapshot(frame);
                for(var n=0;n<8;n++)
                {
                    c.IsChecked=true;Check(Snapshot(frame)==baseline,"checked must not fill hover frame");Check(((FrameworkElement)c.Template.FindName("Mark",c)).Visibility==Visibility.Visible,"check mark missing");c.IsChecked=false;Check(Snapshot(frame)==baseline,"unchecked residue");
                    State(c,"IsMouseOver",true,typeof(UIElement));Check(Snapshot(frame)!=baseline,"hover missing");State(c,"IsMouseOver",false,typeof(UIElement));Check(Snapshot(frame)==baseline,"hover residue");
                    State(c,"IsPressed",true,typeof(ButtonBase));Check(frame.Opacity<1,"pressed missing");State(c,"IsPressed",false,typeof(ButtonBase));Check(Snapshot(frame)==baseline,"pressed residue");
                    State(c,"IsKeyboardFocused",true,typeof(UIElement));var ring=(FrameworkElement)c.Template.FindName("FocusRing",c);Check(ring.Visibility==Visibility.Visible,"keyboard cue missing");FocusCue.SetKeyboardMode(c,false);Check(ring.Visibility==Visibility.Collapsed,"pointer focus should not look hovered");FocusCue.SetKeyboardMode(c,true);State(c,"IsKeyboardFocused",false,typeof(UIElement));Check(ring.Visibility==Visibility.Collapsed,"focus residue");
                    c.IsEnabled=false;Check(frame.Opacity==.4,"disabled missing");c.IsEnabled=true;Check(Snapshot(frame)==baseline,"enabled residue");
                }
                c.IsChecked=true;render(c,280,50,1,"stage11-"+c.GetType().Name+"-checked.png");
            }
        });
        test("Tab/List/Combo 재활용 선택·Hover·Focus 상태 왕복",()=>
        {
            foreach(var c in new Control[]{new TabItem{Header="진단 / 로그"},new ListBoxItem{Content="판매 항목"},new ComboBoxItem{Content="가격 모드"},new MenuItem{Header="메뉴"}})
            {
                Styled(c);render(c,280,60,1,"stage11-"+c.GetType().Name+".png");var b=(Border)c.Template.FindName("B",c);var baseline=Snapshot(b);
                for(var n=0;n<8;n++)
                {
                    State(c,"IsMouseOver",true,typeof(UIElement));Check(Snapshot(b)!=baseline,"hover missing "+c.GetType().Name);State(c,"IsMouseOver",false,typeof(UIElement));Check(Snapshot(b)==baseline,"hover residue");
                    var selection=c is TabItem?TabItem.IsSelectedProperty:c is ListBoxItem?ListBoxItem.IsSelectedProperty:null;
                    if(selection is not null){c.SetValue(selection,true);Check(Snapshot(b)!=baseline,"selected missing");render(c,280,60,1,"stage11-"+c.GetType().Name+"-selected.png");c.SetValue(selection,false);Check(Snapshot(b)==baseline,"selection recycling residue");}
                    var focus=c is ListBoxItem and not ComboBoxItem?"IsKeyboardFocusWithin":"IsKeyboardFocused";
                    State(c,focus,true,typeof(UIElement));Check(((FrameworkElement)c.Template.FindName("FocusRing",c)).Visibility==Visibility.Visible,"focus missing");Check(Snapshot(b)==baseline,"focus must not imitate hover");State(c,focus,false,typeof(UIElement));Check(((FrameworkElement)c.Template.FindName("FocusRing",c)).Visibility==Visibility.Collapsed,"focus residue");
                    c.IsEnabled=false;Check(b.Opacity==.4,"disabled missing");c.IsEnabled=true;Check(Snapshot(b)==baseline,"disabled residue");
                }
            }
        });
        test("Text/Search/Combo/Stepper 포커스·입력 방식 복원 (Popup 미표시)",()=>
        {
            foreach(var c in new Control[]{new TextBox{Text="입력"},new ComboBox{ItemsSource=new[]{"가격"},SelectedIndex=0},new NumericStepper{Value=2,Maximum=5}})
            {
                Styled(c);render(c,280,55,1,"stage11-input-"+c.GetType().Name+".png");var ring=(FrameworkElement)c.Template.FindName("FocusRing",c);
                for(var n=0;n<8;n++)
                {
                    State(c,"IsKeyboardFocusWithin",true,typeof(UIElement));Check(ring.Visibility==Visibility.Visible,"focus missing");FocusCue.SetKeyboardMode(c,false);Check(ring.Visibility==Visibility.Collapsed,"pointer focus residue");FocusCue.SetKeyboardMode(c,true);State(c,"IsKeyboardFocusWithin",false,typeof(UIElement));Check(ring.Visibility==Visibility.Collapsed,"unfocus residue");c.IsEnabled=false;Check(ring.Visibility==Visibility.Collapsed,"disabled focus");c.IsEnabled=true;
                }
                if(c is ComboBox combo){Check(!combo.IsDropDownOpen,"popup opened");Check(combo.Opacity==1,"compound disabled opacity residue");}
            }
            var search=new TextBox{Style=(Style)Application.Current.FindResource("SearchBox")};render(search,280,55,1,"stage11-search.png");State(search,"IsKeyboardFocusWithin",true,typeof(UIElement));Check(((FrameworkElement)search.Template.FindName("FocusRing",search)).Visibility==Visibility.Visible,"search focus");State(search,"IsKeyboardFocusWithin",false,typeof(UIElement));
        });
        test("스크롤 Thumb Hover/Dragging 왕복과 Disabled 우선순위",()=>
        {
            var thumb=Styled(new Thumb());render(thumb,30,120,1,"stage11-thumb.png");var b=(Border)thumb.Template.FindName("B",thumb);var baseline=Snapshot(b);
            for(var n=0;n<8;n++)
            {
                State(thumb,"IsMouseOver",true,typeof(UIElement));Check(Snapshot(b)!=baseline,"thumb hover missing");State(thumb,"IsMouseOver",false,typeof(UIElement));Check(Snapshot(b)==baseline,"thumb hover residue");State(thumb,"IsDragging",true,typeof(Thumb));Check(Snapshot(b)!=baseline,"drag missing");thumb.IsEnabled=false;Check(Snapshot(b)==baseline,"disabled drag highlight");State(thumb,"IsDragging",false,typeof(Thumb));thumb.IsEnabled=true;Check(Snapshot(b)==baseline,"thumb enabled residue");
            }
        });
    }
    internal static void State(DependencyObject control,string name,bool value,Type owner)
    {
        var field=owner.GetField(name+"PropertyKey",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.FlattenHierarchy)??throw new Exception("Missing WPF test property key "+name);
        control.SetValue((DependencyPropertyKey)field.GetValue(null)!,value);if(control is ButtonBase button)Dispatch(button);Pump();
    }
    private static void Dispatch(Control control)
    {
        // No HWND or native input: exercise the public VSM contract explicitly, separately
        // from trigger DP tests. This is not a physical mouse/focus verification.
        VisualStateManager.GoToState(control,!control.IsEnabled?"Disabled":control is ButtonBase&&(bool)control.GetValue(ButtonBase.IsPressedProperty)?"Pressed":(bool)control.GetValue(UIElement.IsMouseOverProperty)?"MouseOver":"Normal",false);
        VisualStateManager.GoToState(control,(bool)control.GetValue(UIElement.IsKeyboardFocusedProperty)?"Focused":"Unfocused",false);Pump();
    }    private static void Pump(){var frame=new System.Windows.Threading.DispatcherFrame();System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,new Action(()=>frame.Continue=false));System.Windows.Threading.Dispatcher.PushFrame(frame);}
    internal static T Styled<T>(T control)where T:Control{control.Style=(Style)Application.Current.FindResource(control.GetType());return control;}
    internal static string Snapshot(Border b)=>$"{b.Background}|{b.BorderBrush}|{b.BorderThickness}|{b.Opacity}";
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
}
