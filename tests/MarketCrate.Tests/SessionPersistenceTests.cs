using System.IO.Compression;
using System.Net;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;
using MarketCrate.Infrastructure.Steam;

namespace MarketCrate.Tests;

[SupportedOSPlatform("windows")]
public static class SessionPersistenceTests
{
    internal const string Account="76561198000000001";
    internal static string Token(string account=Account,string[]? audience=null,int minutes=120,string nonce="synthetic-refresh-canary")
    {
        static string Encode(byte[] bytes)=>Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
        return Encode("{\"alg\":\"synthetic\"}"u8.ToArray())+"."+Encode(JsonSerializer.SerializeToUtf8Bytes(new{sub=account,aud=audience??["web","derive"],exp=DateTimeOffset.UtcNow.AddMinutes(minutes).ToUnixTimeSeconds(),jti=nonce}))+"."+Encode("synthetic-signature-not-Steam"u8.ToArray());
    }
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{("Refresh token requires web derive audience account and unexpired claims",Claims),
            ("DPAPI CurrentUser file has private ACL and no plaintext token or account",Encryption),
            ("QR remember OFF never creates credential file",Off),
            ("QR remember ON survives close and restores exactly one Community session",Restart),
            ("Explicit logout deletes credentials and disables trading",Logout),
            ("Delete saved login preserves only current memory session",Delete),
            ("Expired token never reaches network and pauses future startup",Expired),
            ("Rejected server token is not retried on next startup",Rejected),
            ("Finalize and cookie account mismatches never establish restored login",Mismatch),
            ("Corrupt credential and incomplete atomic write do not become a successful session",Corrupt),
            ("Atomic replacement failure preserves old cipher but disarms restoration",AtomicFailure),
            ("Another instance deleting credentials prevents restore from resurrecting them",DeleteRace),
            ("New QR token atomically replaces prior credentials without unsupported renewal API",Replacement),
            ("Network failure pauses restoration and sanitized ZIP contains no credentials",Diagnostics),
            ("Restore cancellation never saves or submits a trade",Cancel),
            ("Encrypted envelope version and Windows owner mismatch are rejected",OwnerVersion),
            ("Stored consent OFF deletes and does not automatically fetch inventory",ConsentOff),
            ("Cookie-bearing settoken may omit JSON result but never account proof",CookieContract),
            ("Credential save failure turns remember OFF without falsifying successful QR login",SaveFailure),
            ("Restored authentication preserves Unknown journal and reserved asset without replay",Journal)};
        int passed=0,failed=0;foreach(var(name,test)in tests)try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}return(passed,failed);
    }
    private static Task Claims()
    {
        var valid=SteamWebRefreshToken.Inspect(Token(),DateTimeOffset.UtcNow,Account);Check(valid.Account==Account,"claims mismatch");
        foreach(var token in new[]{Token(audience:["web"]),Token(audience:["derive","client"]),Token(minutes:-1),"not-a-token",Token(account:"76561198000000002")})Expect(()=>SteamWebRefreshToken.Inspect(token,DateTimeOffset.UtcNow,Account));return Task.CompletedTask;
    }
    private static Task Encryption()
    {
        using var f=new Fixture();var token=Token();f.Store.Save(Account,Encoding.UTF8.GetBytes(token),DateTimeOffset.UtcNow);var bytes=File.ReadAllBytes(f.Credential);var text=Encoding.UTF8.GetString(bytes);
        Check(!text.Contains(token)&&!text.Contains(Account)&&!text.Contains("synthetic-refresh-canary")&&f.Store.State==StoredLoginState.Saved,"plaintext storage");
        var user=WindowsIdentity.GetCurrent().User!;var acl=new FileInfo(f.Credential).GetAccessControl();Check(user.Equals(acl.GetOwner(typeof(SecurityIdentifier)))&&acl.GetAccessRules(true,true,typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().All(r=>r.AccessControlType!=AccessControlType.Allow||user.Equals(r.IdentityReference)),"broad ACL");
        using var saved=f.Store.TakeForRestore();Check(saved is not null&&saved.ToString().Contains("redacted")&&f.Store.State==StoredLoginState.Paused,"store read falsely rearms");return Task.CompletedTask;
    }
    private static async Task Off(){using var f=new Fixture();f.NewVm();await f.Login(false);Check(f.Vm!.Connected&&!File.Exists(f.Credential)&&f.Server.Sends==0,"OFF persisted");}
    private static async Task Restart()
    {
        using var f=new Fixture();using var log=new JsonDiagnosticLog(Path.Combine(f.Root,"Logs"));using var capture=DiagnosticHub.Use(log);f.NewVm();await f.Login(true);Check(File.Exists(f.Credential),"QR not saved");f.Close();Check(File.Exists(f.Credential),"close logged out persisted login");f.NewVm();await f.Vm!.RestoreRememberedAsync();
        Check(f.Vm.Connected&&f.Vm.RememberLogin&&f.Reads.SequenceEqual(new[]{"/jwt/finalizelogin","/login/settoken"})&&f.Workspace!.Access.Trading==false&&f.Vm.Assets.Count==0&&f.Vm.Listings.Count==0,"restore is not minimal/default-off");var count=f.Reads.Count;await f.Vm.RestoreRememberedAsync();Check(f.Reads.Count==count,"automatic restore repeated");
        var operation=log.Recent.Last(x=>x.Reason=="SessionRestored").Context.OperationId;Check(log.Recent.Any(x=>x.Reason=="CredentialSaved"&&x.Context.OperationId==operation)&&log.Recent.Any(x=>x.Endpoint=="AuthTransfer"&&x.Context.OperationId==operation)&&log.Recent.Any(x=>x.Endpoint=="AuthFinalize"&&x.Context.OperationId==operation),"restore diagnostic chain lost");
    }
    private static async Task Logout(){using var f=new Fixture();f.NewVm();await f.Login(true);f.Vm!.Logout.Execute(null);Check(!f.Vm.Connected&&!File.Exists(f.Credential)&&!f.Vm.RememberLogin,"logout retained credentials");}
    private static async Task Delete(){using var f=new Fixture();f.NewVm();await f.Login(true);f.Vm!.DeleteSavedLogin.Execute(null);Check(f.Vm.Connected&&!File.Exists(f.Credential)&&!f.Vm.RememberLogin&&f.Vm.SavedLoginMessage.Contains("서버 토큰 철회는 수행하지"),"local delete claimed server revoke");}
    private static async Task Expired()
    {
        using var f=new Fixture();f.WriteEnvelope(Token(minutes:-1));f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(!f.Vm.Connected&&f.Reads.Count==0&&f.Store.State==StoredLoginState.Paused,"expired sent network");f.Close();f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(f.Reads.Count==0,"expired auto retry");
    }
    private static async Task Rejected()
    {
        foreach(var code in new[]{15,27}){using var f=new Fixture();f.Save();f.Route=r=>r.RequestUri!.AbsolutePath=="/jwt/finalizelogin"?Json("{\"error\":"+code+"}"):null;f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(!f.Vm.Connected&&f.Reads.Count==1&&f.Store.State==StoredLoginState.Paused,"reject retried");f.Close();f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(f.Reads.Count==0,"rejected token retried on next launch");}
    }
    private static async Task Mismatch()
    {
        foreach(var cookie in new[]{false,true}){using var f=new Fixture();f.Save();f.Route=r=>!cookie&&r.RequestUri!.AbsolutePath=="/jwt/finalizelogin"?Json("{\"steamID\":\"76561198000000002\",\"transfer_info\":[]}"):cookie&&r.RequestUri!.AbsolutePath=="/login/settoken"?Cookie("76561198000000002"):null;f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(!f.Vm.Connected&&f.Server.Sends==0&&f.Reads.Count==(cookie?2:1),"wrong account connected");}
    }
    private static async Task Corrupt()
    {
        using var f=new Fixture();f.Save();var bytes=File.ReadAllBytes(f.Credential);bytes[^1]^=0xff;File.WriteAllBytes(f.Credential,bytes);f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(!f.Vm.Connected&&f.Reads.Count==0,"corrupt became connection");f.Close();f.Store.Delete();f.Save();File.WriteAllBytes(Path.Combine(f.Auth,"refresh.pending"),[1,2]);f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(!f.Vm.Connected&&f.Reads.Count==0&&f.Store.State==StoredLoginState.Paused,"incomplete write fallback");
    }
    private static Task AtomicFailure()
    {
        using var f=new Fixture();f.Save();var old=File.ReadAllBytes(f.Credential);using(var locked=new FileStream(f.Credential,FileMode.Open,FileAccess.Read,FileShare.None))Expect(()=>f.Store.Save(Account,Encoding.UTF8.GetBytes(Token(nonce:"replacement")),DateTimeOffset.UtcNow));
        Check(File.ReadAllBytes(f.Credential).SequenceEqual(old)&&f.Store.State==StoredLoginState.Paused,"failed replace lost or auto-reused old token");Expect(()=>f.Store.TakeForRestore());return Task.CompletedTask;
    }
    private static Task DeleteRace(){using var f=new Fixture();f.Save();using var saved=f.Store.TakeForRestore()!;f.Store.Delete();Expect(()=>f.Store.Save(Account,saved.Token,DateTimeOffset.UtcNow,saved));Check(!File.Exists(f.Credential),"deleted login resurrected");return Task.CompletedTask;}
    private static async Task Replacement(){using var f=new Fixture();f.Save();var old=File.ReadAllBytes(f.Credential);var next=Token(nonce:"rotated-test");f.Store.Save(Account,Encoding.UTF8.GetBytes(next),DateTimeOffset.UtcNow);Check(!File.ReadAllBytes(f.Credential).SequenceEqual(old),"replacement absent");f.ExpectedToken=next;f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(f.Vm.Connected&&!f.Reads.Any(x=>x.Contains("GenerateAccessToken")),"unsupported WebBrowser renewal");}
    private static async Task Diagnostics()
    {
        using var f=new Fixture();f.Save();using var log=new JsonDiagnosticLog(Path.Combine(f.Root,"Logs"));using var capture=DiagnosticHub.Use(log);f.Route=r=>r.RequestUri!.AbsolutePath=="/jwt/finalizelogin"?throw new HttpRequestException(f.ExpectedToken+" cookie-private-canary"):null;f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(!f.Vm.Connected&&f.Store.State==StoredLoginState.Paused,"network failure rearmed");await log.FlushAsync();var review=await log.ReviewExportAsync(null);var zip=await log.ExportAsync(review.ReviewId,true);using var archive=ZipFile.OpenRead(zip);foreach(var entry in archive.Entries){using var reader=new StreamReader(entry.Open());var text=await reader.ReadToEndAsync();Check(!text.Contains(f.ExpectedToken)&&!text.Contains(Account)&&!text.Contains("cookie-private-canary")&&!text.Contains("Owner"),"auth in ZIP");}Check(log.Recent.Any(x=>x.Endpoint=="AuthFinalize"&&x.Context.OperationId!=Guid.Empty),"auth operation disconnected");
    }
    private static async Task Cancel()
    {
        using var f=new Fixture();f.Save();f.Hold=true;f.NewVm();var restore=f.Vm!.RestoreRememberedAsync();await f.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));f.Vm.CancelOperation.Execute(null);await restore;Check(!f.Vm.Connected&&f.Store.State==StoredLoginState.Paused&&f.Server.Sends==0,"cancel restored or traded");
    }
    private static async Task OwnerVersion()
    {
        foreach(var wrongOwner in new[]{false,true}){using var f=new Fixture();f.WriteEnvelope(Token(),wrongOwner?1:999,wrongOwner?"S-1-5-18":null);f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(!f.Vm.Connected&&f.Reads.Count==0,"version or owner ignored");}
    }
    private static async Task ConsentOff(){using var f=new Fixture();f.NewVm();await f.Login(true);f.Vm!.RememberLogin=false;Check(f.Vm.Connected&&!File.Exists(f.Credential),"off lost current connection or retained file");f.Close();f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(f.Reads.Count==0&&!f.Vm.Connected,"OFF restored");}
    private static async Task CookieContract()
    {
        foreach(var empty in new[]{true,false}){using var f=new Fixture();f.Save();f.Route=r=>r.RequestUri!.AbsolutePath=="/login/settoken"?WithoutResult(empty):null;f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(f.Vm.Connected,"cookie-bearing result omitted rejected");}
        using var missing=new Fixture();missing.Save();missing.Route=r=>r.RequestUri!.AbsolutePath=="/login/settoken"?Json(""):null;missing.NewVm();await missing.Vm!.RestoreRememberedAsync();Check(!missing.Vm.Connected,"empty response without cookie accepted");
        using var fresh=new Fixture();fresh.NewVm();fresh.Route=r=>r.RequestUri!.AbsolutePath=="/login/settoken"?Cookie("76561198000000002"):null;fresh.Vm!.RememberLogin=true;await fresh.Vm.BeginAsync();fresh.Server.Clock.Advance();await fresh.Vm.CheckAsync();Check(!fresh.Vm.Connected&&!File.Exists(fresh.Credential)&&fresh.Vm.LoginState==SteamLoginState.ContractFailure,"new remembered QR failed account binding");
        static HttpResponseMessage WithoutResult(bool empty){var response=Cookie(Account);response.Content=new StringContent(empty?"":"{}");return response;}
    }
    private static async Task SaveFailure(){using var f=new Fixture();f.Save();File.WriteAllBytes(Path.Combine(f.Auth,"refresh.pending"),[1]);f.NewVm();await f.Login(true);Check(f.Vm!.Connected&&!f.Vm.RememberLogin&&f.Store.State==StoredLoginState.Paused,"save failure faked success or destroyed valid session");}
    private static async Task Journal()
    {
        using var f=new Fixture();f.Save();var asset=new InventoryAsset(new(730,"2","5001"),"Synthetic Game","Synthetic Item","Synthetic Hash",1,false,true,true);
        var line=new SaleLineSnapshot("synthetic-line",asset,1,"KRW",new(115,100,5,10),DateTimeOffset.UtcNow);var command=new SteamCommandRecord("synthetic-unknown",Account,line,"sell",SaleState.Unknown,SentAt:DateTimeOffset.UtcNow,AvailableBefore:1);
        using(var store=new SteamStateStore(Path.Combine(f.Root,"Steam")))store.SaveCommand(command);
        f.NewVm();await f.Vm!.RestoreRememberedAsync();Check(f.Vm.Connected&&f.Workspace!.Store.ReadCommand(command.RequestKey)==command&&f.Workspace.Store.Available(asset)==0&&f.Server.Sends==0&&f.Server.Cancels==0&&f.Reads.Count==2,"journal changed, reservation released or transaction replayed");
    }
    internal sealed class Fixture:IDisposable
    {
        internal readonly string Root=Path.Combine(Directory.GetCurrentDirectory(),"runtime","session-test-"+Guid.NewGuid().ToString("N"));
        internal string Auth=>Path.Combine(Root,"Auth");internal string Credential=>Path.Combine(Auth,"refresh.authbin");internal SteamAuthStore Store=>new(Auth);
        internal readonly SteamContractServer Server=new();internal SteamExperimentalWorkspace? Workspace;internal SteamWorkspaceViewModel? Vm;
        internal readonly List<string> Reads=[];internal Func<HttpRequestMessage,HttpResponseMessage?>? Route;internal bool Hold;internal readonly TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal string ExpectedToken=Token();
        internal void Save()=>Store.Save(Account,Encoding.UTF8.GetBytes(ExpectedToken),DateTimeOffset.UtcNow);
        internal void NewVm(){Reads.Clear();Vm=new(scope=>Workspace=new(Path.Combine(Root,"Steam"),scope.AppId,scope.ContextId,scope.Name,contractHandler:new Router(this),clock:Server.Clock),true,authDirectory:Auth){RiskAccepted=true,CredentialsAccepted=true};}
        internal async Task Login(bool remember){Vm!.RememberLogin=remember;await Vm.BeginAsync();Server.Clock.Advance();await Vm.CheckAsync();Check(Vm.Connected,"synthetic QR failed");}
        internal void Close(){Vm?.Dispose();Vm=null;Workspace=null;}
        internal void WriteEnvelope(string token,int version=1,string? sid=null)
        {
            Save();var plain=JsonSerializer.SerializeToUtf8Bytes(new{Version=version,Owner=sid??WindowsIdentity.GetCurrent().User!.Value,Account,Token=token,Consent=true,Verified=DateTimeOffset.UtcNow,Expires=DateTimeOffset.FromUnixTimeSeconds(JsonDocument.Parse(Convert.FromBase64String(token.Split('.')[1].Replace('-','+').Replace('_','/').PadRight((token.Split('.')[1].Length+3)/4*4,'='))).RootElement.GetProperty("exp").GetInt64())});
            var encrypted=ProtectedData.Protect(plain,Encoding.UTF8.GetBytes("MarketCrate.WebRefresh.v1"),DataProtectionScope.CurrentUser);File.WriteAllBytes(Credential,"MCAUTH01"u8.ToArray().Concat(encrypted).ToArray());CryptographicOperations.ZeroMemory(plain);
        }
        public void Dispose(){Close();Server.Dispose();if(Directory.Exists(Root))Directory.Delete(Root,true);}
        private sealed class Router(Fixture f):HttpMessageHandler
        {
            private readonly HttpMessageInvoker inner=new(f.Server.Connection());
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
            {
                var path=request.RequestUri!.AbsolutePath;f.Reads.Add(path);
                if(path=="/jwt/finalizelogin")
                {
                    var body=await request.Content!.ReadAsStringAsync(token);Check(body.Contains(Uri.EscapeDataString(f.ExpectedToken)),"restored wrong refresh token");
                    if(f.Hold){f.Started.TrySetResult();await Task.Delay(Timeout.Infinite,token);}
                }
                if(f.Route?.Invoke(request)is{} response)return response;
                if(path.Contains("PollAuthSessionStatus"))return Json(JsonSerializer.Serialize(new{response=new{refresh_token=f.ExpectedToken}}));
                if(path=="/login/settoken")return Cookie(Account);
                return await inner.SendAsync(request,token);
            }
            protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
        }
    }
    private static HttpResponseMessage Json(string body)=>new(HttpStatusCode.OK){Content=new StringContent(body)};
    private static HttpResponseMessage Cookie(string account){var response=Json("{\"result\":1}");response.Headers.Add("Set-Cookie","steamLoginSecure="+account+"%7C%7Csynthetic-access; Path=/; Secure; HttpOnly");return response;}
    private static void Expect(Action action){try{action();}catch(Exception e)when(e is IOException or UnauthorizedAccessException or CryptographicException or ArgumentException){return;}throw new Exception("expected rejection");}
    private static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
}
