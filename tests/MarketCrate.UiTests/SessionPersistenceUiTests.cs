using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using MarketCrate.App;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure.Steam;

internal static class SessionPersistenceUiTests
{
    public static void Run(Action<string,Action> test,Action<FrameworkElement,int,int,double,string> render)
    {
        test("로그인 유지 기본OFF·사용자 선택 바인딩·저장/서버요청 없음",()=>WithRoot(root=>
        {
            using var vm=new SteamWorkspaceViewModel(authDirectory:Path.Combine(root,"Auth"));var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1,"session-optin-small.png");
            var toggle=Find<CheckBox>(view).Single(x=>x.GetBindingExpression(ToggleButton.IsCheckedProperty)?.ParentBinding.Path?.Path=="RememberLogin");
            Check(toggle.IsChecked==false&&toggle.IsEnabled&&!Directory.Exists(Path.Combine(root,"Auth")),"implicit storage");toggle.IsChecked=true;Check(vm.RememberLogin&&!Directory.Exists(Path.Combine(root,"Auth")),"checkbox saved without authentication");
            Check(Find<Button>(view).Any(x=>ReferenceEquals(x.Command,vm.DeleteSavedLogin)),"delete disconnected");
        }));
        test("QR 저장→재시작 복원·상태·거래OFF·실제 창 없음 자체 렌더",()=>WithRoot(root=>
        {
            using var server=new SteamContractServer();var token=Token();SteamExperimentalWorkspace? workspace=null;int requests=0;
            SteamWorkspaceViewModel Create()=>new(scope=>workspace=new(Path.Combine(root,"Steam"),scope.AppId,scope.ContextId,scope.Name,contractHandler:new Router(server,token,()=>requests++),clock:server.Clock),true,authDirectory:Path.Combine(root,"Auth")){RiskAccepted=true,CredentialsAccepted=true};
            using(var first=Create()){first.RememberLogin=true;Complete(first.BeginAsync());server.Clock.Advance();Complete(first.CheckAsync());Check(first.Connected&&first.RememberLogin,"first QR not saved");}
            using var restored=Create();requests=0;Complete(restored.RestoreRememberedAsync());var view=new SteamWorkspaceView{DataContext=restored};render(view,1100,780,1,"session-restored-small.png");render(view,1380,900,1.5,"session-restored-large.png");
            Check(restored.Connected&&requests==2&&!workspace!.Access.Trading&&restored.Assets.Count==0&&restored.LastAuthenticationText.Contains("정상 인증"),"restored state or extra read");Check(Application.Current.Windows.Count==0&&PresentationSource.FromVisual(view)is null,"native window");
            restored.DeleteSavedLogin.Execute(null);Check(restored.Connected&&!File.Exists(Path.Combine(root,"Auth","refresh.authbin")),"local delete lost session or retained credentials");
        }));
        test("불완전 저장은 자동 복원하지 않고 삭제/재인증 안내",()=>WithRoot(root=>
        {
            var auth=Path.Combine(root,"Auth");Directory.CreateDirectory(auth);File.WriteAllBytes(Path.Combine(auth,"refresh.pending"),[1]);
            using var vm=new SteamWorkspaceViewModel(authDirectory:auth);Complete(vm.RestoreRememberedAsync());var view=new SteamWorkspaceView{DataContext=vm};render(view,1100,780,1,"session-paused-small.png");
            Check(!vm.Connected&&!vm.RememberLogin&&vm.SavedLoginMessage.Contains("자동 재시도하지"),"paused login appeared connected");
        }));
    }
    private static string Token(){static string Encode(byte[] value)=>Convert.ToBase64String(value).TrimEnd('=').Replace('+','-').Replace('/','_');return Encode("{\"alg\":\"synthetic\"}"u8.ToArray())+"."+Encode(JsonSerializer.SerializeToUtf8Bytes(new{sub="76561198000000001",aud=new[]{"web","derive"},exp=DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds()}))+".c3ludGhldGlj";}
    private sealed class Router(SteamContractServer server,string token,Action count):HttpMessageHandler
    {
        private readonly HttpMessageInvoker inner=new(server.Connection());
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellation)
        {
            count();if(request.RequestUri!.AbsolutePath.Contains("PollAuthSessionStatus"))return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(new{response=new{refresh_token=token}}))});
            if(request.RequestUri.AbsolutePath=="/login/settoken"){var response=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{}")};response.Headers.Add("Set-Cookie","steamLoginSecure=76561198000000001%7C%7Csynthetic-access; Path=/; Secure; HttpOnly");return Task.FromResult(response);}
            return inner.SendAsync(request,cancellation);
        }
        protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
    }
    private static void WithRoot(Action<string> test){var root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","session-ui-"+Guid.NewGuid().ToString("N"));try{test(root);}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
    private static void Complete(Task task){var watch=Stopwatch.StartNew();while(!task.IsCompleted){if(watch.Elapsed>TimeSpan.FromSeconds(20))throw new TimeoutException();var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);Thread.Sleep(1);}task.GetAwaiter().GetResult();}
    private static IEnumerable<T> Find<T>(DependencyObject parent)where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T found)yield return found;foreach(var nested in Find<T>(child))yield return nested;}}
    private static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
}
