using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MarketCrate.App;
using MarketCrate.App.Controls;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;
using MarketCrate.Infrastructure.Steam;

internal static class EighthStageUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("Steam 검토 모달 열기/닫기 배경 비활성 갱신과 포커스 범위",()=>
        {
            var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage8-modal-"+Guid.NewGuid().ToString("N"));
            using(var log=new JsonDiagnosticLog(Path.Combine(folder,"logs")))using(DiagnosticHub.Use(log))using(var server=new SteamContractServer())
            using(var vm=new SteamWorkspaceViewModel(scope=>new SteamExperimentalWorkspace(Path.Combine(folder,"db"),scope.AppId,scope.ContextId,scope.Name,contractHandler:server.Connection(),clock:server.Clock),true))
            {
                log.DiagnosticMode=true;vm.RiskAccepted=vm.CredentialsAccepted=true;Complete(vm.BeginAsync());server.Clock.Advance();Complete(vm.CheckAsync());Complete(vm.LoadAsync());vm.SelectAll.Execute(null);Complete(vm.RefreshPricesAsync());
                var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,800,1,"stage8-modal-before.png");
                var body=(FrameworkElement)view.FindName("SteamBody");Check(body.IsEnabled);
                using(var operation=DiagnosticHub.Scope())
                {
                    var id=DiagnosticHub.Context!.OperationId;vm.Review.Execute(null);render(view,1100,800,1,"stage8-modal-open.png");Check(!body.IsEnabled);
                    var modal=Find<Border>(view).Single(x=>ModalNavigation.GetIsOpen(x));
                    Check(FocusManager.GetIsFocusScope(modal)&&KeyboardNavigation.GetTabNavigation(modal)==KeyboardNavigationMode.Cycle&&KeyboardNavigation.GetControlTabNavigation(modal)==KeyboardNavigationMode.Cycle);
                    vm.CloseReview.Execute(null);render(view,1100,800,1,"stage8-modal-closed.png");Check(body.IsEnabled);
                    var events=log.Recent.Where(x=>x.EventId==(int)DiagnosticEvent.Modal&&x.Context.OperationId==id).ToArray();
                    Check(events.Any(x=>x.Reason=="Opened")&&events.Any(x=>x.Reason=="Closed")&&events.Any(x=>x.Reason=="FocusDeferred"));
                }
                Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)is null&&server.Sends==0);
            }
            Directory.Delete(folder,true);
        });
        test("커스텀 ComboBox 포커스·열림 테두리와 managed 접근성 계약",()=>
        {
            var combo=new ComboBox{Style=(Style)Application.Current.FindResource(typeof(ComboBox)),ItemsSource=new[]{"KRW","USD"},SelectedIndex=0,Width=250};AutomationProperties.SetName(combo,"통화 선택");
            render(combo,300,70,1,"stage8-combo-closed.png");
            Check(combo.Template.Triggers.OfType<Trigger>().Any(x=>x.Property==UIElement.IsKeyboardFocusWithinProperty));
            Check(combo.Template.Triggers.OfType<Trigger>().Any(x=>x.Property==ComboBox.IsDropDownOpenProperty));
            var peer=UIElementAutomationPeer.CreatePeerForElement(combo)!;Check(peer.GetName()=="통화 선택"&&peer.GetAutomationControlType()==AutomationControlType.ComboBox);
            var stepper=new NumericStepper{Style=(Style)Application.Current.FindResource(typeof(NumericStepper)),Maximum=10,Value=3};render(stepper,150,45,1,"stage8-stepper.png");
            var range=(IRangeValueProvider)UIElementAutomationPeer.CreatePeerForElement(stepper)!.GetPattern(PatternInterface.RangeValue)!;
            stepper.IsEnabled=false;Check(range.IsReadOnly);
            try{range.SetValue(4);throw new Exception("disabled peer accepted write");}catch(ElementNotEnabledException){}
            Check(stepper.Value==3&&!combo.IsDropDownOpen&&Application.Current.Windows.Count==0);
        });
        test("격리 QA 오프라인 UI와100/125/150/200 출력 밀도 (물리 DPI 아님)",()=>
        {
            using var vm=new SteamWorkspaceViewModel(offlineOnly:true){RiskAccepted=true,CredentialsAccepted=true};var view=new SteamWorkspaceView{DataContext=vm};
            foreach(var scale in new[]{1d,1.25,1.5,2})render(view,1100,700,scale,$"stage8-offline-{scale*100:0}.png");
            var login=Find<Button>(view).Single(x=>ReferenceEquals(x.Command,vm.BeginLogin));Check(!login.IsEnabled);
            Check(Find<TextBlock>(view).Any(x=>x.Text.Contains("Steam 네트워크 차단"))&&Application.Current.Windows.Count==0);
        });
    }
    private static void Complete(Task task)
    {
        var watch=Stopwatch.StartNew();while(!task.IsCompleted){if(watch.Elapsed>TimeSpan.FromSeconds(10))throw new TimeoutException();var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}task.GetAwaiter().GetResult();
    }
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject{for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T item)yield return item;foreach(var next in Find<T>(child))yield return next;}}
    private static void Check(bool value){if(!value)throw new Exception("Assertion failed");}
}
