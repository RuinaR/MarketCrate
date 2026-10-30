using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MarketCrate.App.Controls;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.App;
using MarketCrate.Infrastructure.Steam;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

internal static class UserInputUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("수량 직접 입력은 포커스 이동 전에 실제 선택 수량에 반영",()=>
        {
            var row=new SteamAssetRow(new(new(730,"2","101"),"Synthetic","Synthetic","Synthetic",10,true,true,true),10,()=>{});
            var stepper=new NumericStepper{Maximum=10,Style=(Style)Application.Current.FindResource(typeof(NumericStepper)),DataContext=row};
            stepper.SetBinding(NumericStepper.ValueProperty,new Binding(nameof(SteamAssetRow.Quantity)));
            render(stepper,140,44,1,"journey-quantity-before.png");
            var input=Find<TextBox>(stepper).Single();input.Text="7";
            if(row.Quantity!=7)throw new Exception("Visible quantity differs from selected quantity before focus loss");
        });
        test("실제 보관함 템플릿의 잘못된 수량·게임 전환·수정·검토 버튼 연결",()=>
        {
            var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","input-ui-"+Guid.NewGuid().ToString("N"));using var server=new SteamContractServer();
            using(var vm=new SteamWorkspaceViewModel(scope=>new(root,scope.AppId,scope.ContextId,scope.Name,new(true,"synthetic-only",true),server.Connection(),server.Clock),true,openOfficial:_=>throw new Exception("Browser forbidden")){RiskAccepted=true,CredentialsAccepted=true})
            {
                Complete(vm.BeginLogin.ExecuteAsync());server.Clock.Advance();Complete(vm.CheckLogin.ExecuteAsync());Complete(vm.Load.ExecuteAsync());vm.SelectAll.Execute(null);Complete(vm.RefreshPrices.ExecuteAsync());vm.WorkPage=1;
                var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1,"journey-real-template.png");
                var input=Input(view);input.Text="abc";Check(!vm.Assets[0].QuantityInputValid&&!vm.Review.CanExecute(null)&&vm.SelectionEstimate.Contains("수량"),"Invalid quantity left review enabled");
                render(view,1100,780,1.25,"journey-invalid-quantity.png");var scope=vm.Game;vm.Game=vm.Games[1];render(view,1100,780,1,"journey-other-game.png");vm.Game=scope;render(view,1100,780,1,"journey-return-invalid.png");
                var stepper=Find<NumericStepper>(view).First(x=>x.DataContext is SteamAssetRow);Check(((Border)stepper.Template.FindName("InputErrorOutline",stepper)).Visibility==Visibility.Visible,"Quantity error outline hidden");
                Check(Input(view).Text=="abc"&&!vm.Assets[0].QuantityInputValid,"Recycled view hid invalid input");Input(view).Text="7";Check(vm.Assets[0].Quantity==7&&vm.Assets[0].QuantityInputValid,"Corrected text not propagated");Complete(vm.Load.ExecuteAsync());Complete(vm.RefreshPrices.ExecuteAsync());Check(vm.Review.CanExecute(null),"Fresh inventory/fees/price did not enable review");
                var bound=Find<Button>(view).Single(x=>ReferenceEquals(x.Command,vm.Review));bound.Command.Execute(bound.CommandParameter);Check(vm.ReviewOpen&&vm.ReviewRows.Sum(x=>x.Quantity)==7,"Review binding path broken");
                vm.TradeAccepted=true;vm.EnableTrading.Execute(null);render(view,1100,780,2,"journey-review-200.png");
                var price=Find<TextBox>(view).First(x=>x.DataContext is SteamReviewRow);price.Text="invalid";Check(!vm.Submit.CanExecute(null),"Invalid bound price left submission enabled");price.Text="115";Check(vm.Submit.CanExecute(null),"Valid bound price did not restore submission");
                Complete(vm.Submit.ExecuteAsync());Check(server.Sends==2&&vm.Results.All(x=>x.State==SaleState.AwaitingSteamGuard),"One approval did not submit both price allocations");
                render(view,1100,780,1.25,"journey-results.png");Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)==null,"Visible window created");
            }
            Directory.Delete(root,true);
        });
        test("주요 화면의 실현된 Command 바인딩과 접근성·입력 경로 존재",()=>
        {
            using var vm=new SteamWorkspaceViewModel(openOfficial:_=>throw new Exception("Browser forbidden"));var view=new SteamWorkspaceView{DataContext=vm};var commands=new HashSet<string>();
            foreach(var page in new[]{0,1,2,3,4})
            {
                vm.WorkPage=page;render(view,1100,780,1,$"journey-command-page-{page}.png");
                foreach(var button in Find<ButtonBase>(view))if(button.GetBindingExpression(ButtonBase.CommandProperty)is {} binding)
                {Check(button.Command is not null,"Unresolved command: "+binding.ParentBinding.Path.Path);commands.Add(binding.ParentBinding.Path.Path);}
            }
            Check(commands.Contains("BeginLogin")&&commands.Contains("Load")&&commands.Contains("LoadListings")&&commands.Contains("Recover")&&commands.Contains("PrepareTrading"),"Primary page command missing");
        });
        test("수량의 접근성 Peer·증감 버튼·비활성 경로가 동일 모델에 반영",()=>
        {
            var row=new SteamAssetRow(new(new(730,"2","101"),"Synthetic","Synthetic","Synthetic",10,true,true,true),10,()=>{});
            var stepper=new NumericStepper{Maximum=10,DataContext=row,Style=(Style)Application.Current.FindResource(typeof(NumericStepper))};
            stepper.SetBinding(NumericStepper.ValueProperty,new Binding(nameof(SteamAssetRow.Quantity)){Mode=BindingMode.OneWay});stepper.SetBinding(NumericStepper.InputTextProperty,new Binding(nameof(SteamAssetRow.QuantityText)){Mode=BindingMode.TwoWay});render(stepper,140,44,1,"journey-quantity-peer.png");
            var peer=(IRangeValueProvider)UIElementAutomationPeer.CreatePeerForElement(stepper)!.GetPattern(PatternInterface.RangeValue)!;peer.SetValue(7);Check(row.Quantity==7&&row.QuantityText=="7","Peer failed to update source");
            var up=(Button)stepper.Template.FindName("PART_Up",stepper);up.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));Check(row.Quantity==8,"Increment missed model");stepper.IsEnabled=false;up.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));Check(row.Quantity==8&&peer.IsReadOnly,"Disabled input changed quantity");
        });
    }
    private static TextBox Input(FrameworkElement view)=>Find<NumericStepper>(view).First(x=>x.DataContext is SteamAssetRow).Template.FindName("PART_Input",Find<NumericStepper>(view).First(x=>x.DataContext is SteamAssetRow))as TextBox??throw new Exception("Input part missing");
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    private static void Complete(Task task){var watch=Stopwatch.StartNew();while(!task.IsCompleted){if(watch.Elapsed>TimeSpan.FromSeconds(20))throw new TimeoutException();var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject
    {for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T value)yield return value;foreach(var nested in Find<T>(child))yield return nested;}}
}
