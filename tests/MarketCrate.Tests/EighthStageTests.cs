using System.Net;
using System.Text.Json;
using MarketCrate.App;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;
using MarketCrate.Infrastructure.Steam;

namespace MarketCrate.Tests;

public static class EighthStageTests
{
    public static async Task<(int Passed, int Failed)> RunAsync()
    {
        var tests = new (string, Func<Task>)[] {
            ("Guest profile fail closed and separate user data", Profile),
            ("Offline GUI profile blocks login including direct method", Offline),
            ("QR optional empty/zero defaults and rotated challenge", QrDefaults),
            ("QR unsafe challenge/schema change clears auth and logs safely", QrInvalid),
            ("QR user cancellation is not schema failure", QrCancel),
            ("Read cancellation keeps authenticated session and traces operation", ReadCancel),
            ("Confirmation boolean/int contract rejects ambiguous values", Confirmation)
        };
        var passed = 0; var failed = 0;
        foreach (var (name, action) in tests)
            try { await action(); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception error) { failed++; Console.WriteLine($"FAIL {name}: {error}"); }
        return (passed, failed);
    }
    private static Task Profile()
    {
        var normal = AppRunProfile.Create([], "synthetic-user", null);
        var qa = AppRunProfile.Create(["--isolated-qa"], "synthetic-user", AppRunProfile.GuestAcknowledgement);
        Check(!normal.OfflineQa && qa.OfflineQa && qa.DataRoot == Path.Combine(normal.DataRoot, "IsolatedGuiQa"));
        foreach (var args in new[] { new[] { "--isolated-qa" }, new[] { "--isolated-qa", "extra" }, new[] { "--unknown" } })
        { try { AppRunProfile.Create(args, "synthetic-user", null); throw new Exception("launch accepted"); } catch (ArgumentException) { } }
        return Task.CompletedTask;
    }
    private static async Task Offline()
    {
        var factoryCalls = 0;
        using var vm = new SteamWorkspaceViewModel(_ => { factoryCalls++; throw new Exception("factory must not run"); }, offlineOnly: true)
        { RiskAccepted = true, CredentialsAccepted = true };
        Check(!vm.BeginLogin.CanExecute(null)); await vm.BeginAsync();
        Check(factoryCalls == 0 && !vm.Connected && vm.Status.Contains("Disabled") && vm.ModeText.Contains("Mock"));
    }
    private static async Task QrDefaults()
    {
        using var server = new SteamContractServer(); using var handler = new AuthHandler(server);
        var access = new SteamAccess(); access.Acknowledge(true, true, true, false);
        using var http = new SteamHttp(access, handler); using var auth = new SteamQrAuthentication(http, server.Clock);
        var challenge = await auth.BeginAsync(); server.Clock.Advance();
        handler.Poll = "{\"response\":{\"new_client_id\":\"0\",\"new_challenge_url\":\"\",\"refresh_token\":\"\",\"access_token\":\"\",\"had_remote_interaction\":false}}";
        Check(!await auth.CheckAsync() && auth.Challenge == challenge && http.Halted is null);
        server.Clock.Advance(); handler.Poll = "{\"response\":{\"new_client_id\":\"124\",\"new_challenge_url\":\"https://s.team/q/1/rotated\"}}";
        Check(!await auth.CheckAsync() && auth.Challenge!.AbsolutePath.EndsWith("rotated"));
        server.Clock.Advance(); handler.Poll = "{\"response\":{}}";
        Check(!await auth.CheckAsync()); Check(handler.LastAuthBytes![0] == 8 && handler.LastAuthBytes[1] == 124);
        handler.NullBegin = true;
        try { await auth.BeginAsync(); throw new Exception("null QR accepted"); } catch (SteamException error) { Check(error.Code==SteamFailure.ContractChanged); }
        Check(auth.Challenge is null && !auth.Connected);
    }
    private static async Task QrInvalid()
    {
        foreach (var fields in new[] { "\"new_challenge_url\":\"https://s.team/q/1/synthetic?token=never-log\"", "\"unexpected\":\"never-log\"" })
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "runtime", "stage8-log-" + Guid.NewGuid().ToString("N"));
            using (var log = new JsonDiagnosticLog(folder)) using (DiagnosticHub.Use(log))
            using (var server = new SteamContractServer()) using (var handler = new AuthHandler(server) { Poll = "{\"response\":{" + fields + "}}" })
            {
                var access = new SteamAccess(); access.Acknowledge(true,true,true,false);
                using var http = new SteamHttp(access, handler); using var auth = new SteamQrAuthentication(http, server.Clock);
                using var operation = DiagnosticHub.Scope();
                await auth.BeginAsync(); server.Clock.Advance();
                try { await auth.CheckAsync(); throw new Exception("schema accepted"); } catch (SteamException error) { Check(error.Code == SteamFailure.ContractChanged); }
                Check(auth.Challenge is null && !auth.Connected && http.Halted == SteamFailure.ContractChanged);
                await log.FlushAsync(); Check(!JsonSerializer.Serialize(log.Recent).Contains("never-log"));
                Check(log.Recent.Any(x=>x.Context.OperationId==DiagnosticHub.Context!.OperationId&&x.EventId==(int)DiagnosticEvent.AuthState&&x.Error==DiagnosticError.Schema&&x.Endpoint=="AuthPoll"));
            }
            Directory.Delete(folder,true);
        }
    }
    private static async Task QrCancel()
    {
        using var server = new SteamContractServer(); using var handler = new AuthHandler(server) { CancelPoll = true };
        var access = new SteamAccess(); access.Acknowledge(true,true,true,false);
        using var http = new SteamHttp(access,handler); using var auth = new SteamQrAuthentication(http,server.Clock);
        await auth.BeginAsync(); server.Clock.Advance(); using var cancel = new CancellationTokenSource(); cancel.Cancel();
        try { await auth.CheckAsync(cancel.Token); throw new Exception("cancel ignored"); } catch (OperationCanceledException) { }
        Check(auth.Challenge is null && http.Halted is null);
    }
    private static async Task ReadCancel()
    {
        var folder = Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage8-read-"+Guid.NewGuid().ToString("N"));
        using (var log = new JsonDiagnosticLog(Path.Combine(folder,"logs"))) using (DiagnosticHub.Use(log))
        using (var server = new SteamContractServer())
        using (var vm = new SteamWorkspaceViewModel(scope => new SteamExperimentalWorkspace(Path.Combine(folder,"db"),scope.AppId,scope.ContextId,scope.Name,contractHandler:server.Connection(),clock:server.Clock),true))
        {
            vm.RiskAccepted=vm.CredentialsAccepted=true; await vm.BeginAsync();server.Clock.Advance();await vm.CheckAsync();Check(vm.Connected);
            server.InventoryStarted=new(TaskCreationOptions.RunContinuationsAsynchronously);server.InventoryRelease=new(TaskCreationOptions.RunContinuationsAsynchronously);
            var load=vm.LoadAsync();await server.InventoryStarted.Task;vm.CancelOperation.Execute(null);server.InventoryRelease.SetResult();await load;
            Check(vm.Connected && !vm.Busy && vm.LoginState==SteamLoginState.Connected);
            var id=vm.LastOperationId;var matching=log.Recent.Where(x=>x.Context.OperationId==id).ToArray();
            Check(matching.Any(x=>x.EventId==(int)DiagnosticEvent.HttpFailure&&x.Error==DiagnosticError.Cancelled&&x.Outcome==DiagnosticOutcome.Cancelled));
            Check(server.Sends==0&&server.Cancels==0);
        }
        Directory.Delete(folder,true);
    }
    private static Task Confirmation()
    {
        foreach(var flag in new[]{"true","1"}) Check(SteamContract.Sell("{\"success\":true,\"requires_confirmation\":"+flag+"}").State==SaleState.AwaitingSteamGuard);
        foreach(var flag in new[]{"\"1\"","null","2","{}"})
        {try{SteamContract.Sell("{\"success\":true,\"requires_confirmation\":"+flag+"}");throw new Exception("ambiguous flag accepted");}catch(SteamException error){Check(error.Code==SteamFailure.ContractChanged);}}
        return Task.CompletedTask;
    }
    private sealed class AuthHandler(SteamContractServer server) : HttpMessageHandler
    {
        private readonly HttpMessageInvoker inner = new(server.Connection());
        public string? Poll; public bool CancelPoll, NullBegin; public byte[]? LastAuthBytes;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (NullBegin && request.RequestUri!.AbsolutePath.Contains("BeginAuthSession")) return new(HttpStatusCode.OK){Content=new StringContent("{\"response\":{\"client_id\":\"123\",\"request_id\":\"AQID\",\"challenge_url\":null,\"interval\":1}}")};
            if(request.RequestUri!.AbsolutePath.Contains("PollAuthSession"))
            {
                var body = await request.Content!.ReadAsStringAsync(token);
                LastAuthBytes = Convert.FromBase64String(Uri.UnescapeDataString(body.Split('=',2)[1]));
                if(CancelPoll) throw new OperationCanceledException(token);
                if(Poll is not null) return new(HttpStatusCode.OK){Content=new StringContent(Poll)};
            }
            return await inner.SendAsync(request,token);
        }
        protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
    }
    private static void Check(bool value){if(!value)throw new Exception("Assertion failed");}
}
