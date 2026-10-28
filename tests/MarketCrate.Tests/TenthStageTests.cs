using System.Net;
using System.Text.Json;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure;
using MarketCrate.Infrastructure.Diagnostics;
using MarketCrate.Infrastructure.Steam;

namespace MarketCrate.Tests;

public static class TenthStageTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new (string,Func<Task>)[]{
            ("Price terminology legacy settings migrate to stable basis keys",Terms),
            ("Rotated QR updates actual VM matrix and expiry/relogin clears session",QrRotation),
            ("Listings and cancellation review independent of unavailable inventory and fees",IndependentListings),
            ("Session expiry preserves last data but disables stale review and logout clears",ExpiredReads),
            ("Read contract operation traces contain no secrets or account/asset data",ReadDiagnostics),
            ("Official market fallback is explicit and injected without auto trade",OfficialPage)
        };var passed=0;var failed=0;foreach(var(name,action)in tests)try{await action();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine($"FAIL {name}: {error}");}return(passed,failed);
    }
    private static async Task Terms()
    {
        var path=Folder();try
        {
            using var store=new SqliteDemoStore(path);store.WriteSetting("strategy","구매주문 기반 예상가");
            using(var vm=new MainViewModel(new(),store)){await vm.InitializeAsync();Check(vm.SelectedStrategy==PriceDisplay.Immediate);vm.SelectedStrategy=vm.Strategies[1];Check(store.ReadSetting("strategy")=="ExactAverage");}
            using(var vm=new MainViewModel(new(),store)){await vm.InitializeAsync();Check(vm.SelectedStrategy==PriceDisplay.MockAverage);vm.SelectedStrategy=vm.Strategies[0];Check(store.ReadSetting("strategy")=="Immediate");vm.SelectedStrategy="unknown";Check(vm.SelectedStrategy==PriceDisplay.Immediate);}
            Check(PriceDisplay.Difference(950,1000,"KRW")=="−₩50");
            Check(PriceDisplay.TryReadSetting("즉시판매가",out var basis)&&basis==PriceBasis.Immediate);
            Check(PriceDisplay.ImmediateExplanation=="현재 구매주문 기준 예상 가격이며 실제 즉시 체결을 보장하지 않습니다.");
        }finally{Directory.Delete(path,true);}
    }
    private static async Task QrRotation()
    {
        using var f=new Fixture();await f.Vm.BeginAsync();var first=f.Vm.QrMatrix;Check(first is not null);
        f.Override=path=>path.Contains("PollAuthSession")?"{\"response\":{\"new_client_id\":\"124\",\"new_challenge_url\":\"https://s.team/q/1/changed-synthetic-only\"}}":null;
        f.Server.Clock.Advance();await f.Vm.CheckAsync();Check(f.Vm.LoginState==SteamLoginState.AwaitingMobile&&!ReferenceEquals(first,f.Vm.QrMatrix));
        var second=f.Vm.QrMatrix;f.Override=path=>path.Contains("PollAuthSession")?"{\"response\":{\"new_client_id\":\"0\",\"new_challenge_url\":\"\"}}":null;
        f.Server.Clock.Advance();await f.Vm.CheckAsync();Check(ReferenceEquals(second,f.Vm.QrMatrix));
        f.Server.Clock.Advance(130);await f.Vm.CheckAsync();Check(f.Vm.LoginState==SteamLoginState.Expired&&f.Vm.QrMatrix is null);
        f.Override=null;await f.Vm.BeginAsync();f.Server.Clock.Advance();await f.Vm.CheckAsync();Check(f.Vm.Connected&&!f.Vm.BeginLogin.CanExecute(null));
    }
    private static async Task IndependentListings()
    {
        using var f=new Fixture();await f.Login();f.Server.UnsupportedInventory=true;f.Server.Rows.Add(("999",1,200));f.Server.Publish=true;
        await f.Vm.LoadAsync();Check(f.Vm.Connected&&f.Vm.Assets.Count==0&&f.Vm.Game.State==InventoryScopeState.Unsupported);
        await f.Vm.LoadListingsAsync();Check(f.Vm.Listings.Count==1&&f.Vm.CurrencyText.Contains("미조회"));
        f.Vm.SelectAllListings.Execute(null);Check(f.Vm.ReviewCancel.CanExecute(null));f.Vm.ReviewCancel.Execute(null);Check(f.Vm.ManagementOpen&&!f.Vm.ConfirmManagement.CanExecute(null));f.Vm.CloseReview.Execute(null);
        await f.Vm.LoadFeesAsync();Check(f.Vm.CurrencyText.Contains("KRW"));
        f.Vm.ListingMetrics.Execute(null);await Until(()=>!f.Vm.Busy);Check(f.Vm.Listings.Single().Metric.Contains(PriceDisplay.Immediate)&&f.Vm.Listings.Single().Metric.Contains('−'));
        Check(f.Server.Sends==0&&f.Server.Cancels==0);
    }
    private static async Task ExpiredReads()
    {
        using var f=new Fixture();await f.Login();await f.Vm.LoadAsync();f.Vm.SelectAll.Execute(null);await f.Vm.RefreshPricesAsync();Check(f.Vm.Review.CanExecute(null));var row=f.Vm.Assets[0];
        f.FailInventory=true;await f.Vm.LoadAsync();Check(f.Vm.LoginState==SteamLoginState.Expired&&!f.Vm.Connected&&ReferenceEquals(row,f.Vm.Assets[0])&&!f.Vm.Review.CanExecute(null));
        Check(f.Vm.ContractSummary.Contains("마지막 파싱"));f.Vm.Logout.Execute(null);Check(f.Vm.Assets.Count==0&&f.Vm.Listings.Count==0&&f.Vm.ContractSummary.Contains("조회 증거 없음"));
    }
    private static async Task ReadDiagnostics()
    {
        var path=Folder();try
        {
            using var log=new JsonDiagnosticLog(path);using var scope=DiagnosticHub.Use(log);using var f=new Fixture();await f.Login();await f.Vm.LoadListingsAsync();
            var id=f.Vm.LastOperationId;var records=log.Recent.Where(x=>x.Context.OperationId==id).ToArray();
            Check(records.Any(x=>x.EventId==(int)DiagnosticEvent.HttpRequest&&x.Context.CorrelationId is not null)&&records.Any(x=>x.EventId==(int)DiagnosticEvent.ReadContract&&x.Endpoint=="Listings"&&x.Reason=="Synthetic"));
            var json=JsonSerializer.Serialize(log.Recent);foreach(var secret in new[]{"76561198000000001","s.team","synthetic-refresh","steamLoginSecure","Synthetic stack"})Check(!json.Contains(secret));
            Check(f.Vm.ContractSummary.Contains("합성 계약")&&f.Vm.ProductReadiness.Contains("미검증"));
        }finally{Directory.Delete(path,true);}
    }
    private static Task OfficialPage()
    {
        Uri? opened=null;using var vm=new SteamWorkspaceViewModel(openOfficial:uri=>opened=uri);
        vm.OpenOfficialMarket.Execute(null);Check(opened?.AbsoluteUri=="https://steamcommunity.com/market/"&&!vm.Connected&&!vm.CanTrade);return Task.CompletedTask;
    }
    private sealed class Fixture:IDisposable
    {
        private readonly string path=Folder();public SteamContractServer Server{get;}=new();public Func<string,string?>? Override;public bool FailInventory;
        public SteamWorkspaceViewModel Vm{get;}
        public Fixture(){Vm=new(scope=>new SteamExperimentalWorkspace(path,scope.AppId,scope.ContextId,scope.Name,contractHandler:new Routing(this),clock:Server.Clock),true){RiskAccepted=true,CredentialsAccepted=true};}
        public async Task Login(){await Vm.BeginAsync();Server.Clock.Advance();await Vm.CheckAsync();Check(Vm.Connected);}
        public void Dispose(){Vm.Dispose();Server.Dispose();Directory.Delete(path,true);}
        private sealed class Routing(Fixture owner):HttpMessageHandler
        {
            private readonly HttpMessageInvoker inner=new(owner.Server.Connection());
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
            {
                var path=request.RequestUri!.AbsolutePath;if(owner.FailInventory&&path.StartsWith("/inventory/"))return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized){Content=new StringContent("{}")});
                if(owner.Override?.Invoke(path)is {} json)return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json)});
                return inner.SendAsync(request,token);
            }
            protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
        }
    }
    private static string Folder(){var path=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage10-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(path);return path;}
    private static async Task Until(Func<bool> ready){using var timeout=new CancellationTokenSource(5000);while(!ready())await Task.Delay(10,timeout.Token);}
    private static void Check(bool value){if(!value)throw new Exception("Assertion failed");}
}
