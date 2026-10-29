using System.Net;
using System.Diagnostics;
using MarketCrate.Core;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MarketCrate.Infrastructure.Steam;

// Session-only consent. Never deserialize this from settings or enable it at startup.
public sealed class SteamAccess
{
    public bool ResearchRiskAccepted { get; private set; }
    public bool AuthenticatedReads { get; private set; }
    public bool Trading { get; private set; }
    public bool CancellationOnly{get;private set;}
    internal void RestrictToCancellation()=>CancellationOnly=true;
    internal string? TradingAccount { get; private set; }
    internal void BindTradingAccount(string account) => TradingAccount = account;
    internal void RevokeTrading() { Trading=false; TradingAccount=null; CancellationOnly=false; }
    public void Acknowledge(bool unofficialAutomationRisk, bool credentialRisk, bool reads, bool trading)
    {
        if (!unofficialAutomationRisk || !credentialRisk) throw new InvalidOperationException("SteamRiskAcknowledgementRequired");
        ResearchRiskAccepted = true; AuthenticatedReads = reads; Trading = reads && trading;
        if(!Trading){TradingAccount=null;CancellationOnly=false;}
    }
    public void Revoke() { ResearchRiskAccepted = AuthenticatedReads = false; RevokeTrading(); }
    public override string ToString() => "SteamAccess (session-only)";
}

public enum SteamFailure { Disabled, Authentication, Forbidden, RateLimited, Challenge, ContractChanged, Transport, Restricted, Expired, Denied, StaleReview, Unsupported, Authorization, WalletUnavailable }
public sealed record SteamTradingPolicy(bool Enabled, string EvidenceReference, bool StackAmountsSupported = false,bool MedianPriceContractSupported = false, bool UserInitiated = false)
{
    public System.Collections.Immutable.ImmutableArray<int> VerifiedApps {get;init;}=[];
    public bool CancellationOnly{get;init;}
    public static SteamTradingPolicy Blocked { get; } = new(false, "Valve permission not established");
#if MARKETCRATE_DEMO
    internal static SteamTradingPolicy ContractTest { get; } = new(true, "synthetic contract only", true);
#endif
}
public enum SteamTradingCheck { Consent, WalletIdentity, FeeContract, WalletConsistency, InventoryRead, SaleableAssets, PublisherFees, ListingsRead, ListingFees, SessionCheck }
public sealed class SteamException(SteamFailure code, int? apiResult = null) : IOException(code.ToString())
{
    public SteamFailure Code { get; } = code;
    public int? ApiResult { get; } = apiResult;
    public string? Endpoint { get; internal set; }
    public DiagnosticError? Diagnostic { get; internal set; }
    public bool Redirected { get; internal set; }
    public SteamBodyKind? BodyKind { get; internal set; }
    public SteamTradingCheck? TradingCheck { get; internal set; }
}

// No browser cookie import, external host, redirects, automatic retries or response-body logging.
public sealed partial class SteamHttp : IDisposable
{
    private readonly HttpClient client;
    private readonly SemaphoreSlim queue = new(1, 1);
    private DateTimeOffset last;
    private readonly TimeSpan gap;
    internal readonly CookieContainer Cookies = new();
    public SteamAccess Access { get; }
    public SteamFailure? Halted { get; private set; }
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string,SteamFailure> featureBlocks=[];
    public SteamFailure? FeatureFailure(string endpoint)=>featureBlocks.TryGetValue(endpoint,out var failure)?failure:null;
    internal void BlockFeature(string endpoint,SteamFailure failure=SteamFailure.Authorization)=>featureBlocks[endpoint]=failure;
    public SteamHttp(SteamAccess access, HttpMessageHandler? testHandler = null, TimeSpan? testGap = null)
    {
        Access = access; gap = testHandler is null ? TimeSpan.FromSeconds(3) : testGap ?? TimeSpan.Zero;
        client = new(testHandler ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true, CookieContainer = Cookies, UseProxy = false });
        client.Timeout = TimeSpan.FromSeconds(25);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MarketCrate/0.4 (experimental; user-initiated)");
    }
    public void Stop(SteamFailure failure) => Halted ??= failure;
    private static bool IsFeeContext(Uri uri)=>uri.Host=="steamcommunity.com"&&uri.Query.Length==0&&System.Text.RegularExpressions.Regex.IsMatch(uri.AbsolutePath,@"^/profiles/[0-9]{17}/inventory/$",System.Text.RegularExpressions.RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
    internal static void ValidateUri(Uri uri)
    {
        if (uri.Scheme != "https" || uri.Port != 443 || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0) throw new SteamException(SteamFailure.ContractChanged);
        var path = uri.AbsolutePath;
        var valid = uri.Host switch
        {
            "api.steampowered.com" => path is "/IAuthenticationService/BeginAuthSessionViaQR/v1/" or "/IAuthenticationService/PollAuthSessionStatus/v1/",
            "login.steampowered.com" => path == "/jwt/finalizelogin",
            "steamcommunity.com" => path is "/login/settoken" or "/market/" or "/market/ajaxfeestructure/" or "/market/orderbook" or "/market/pricehistory/" or "/market/mylistings/render/" or "/market/sellitem/" ||
                path.StartsWith("/inventory/", StringComparison.Ordinal) || path.StartsWith("/market/removelisting/", StringComparison.Ordinal) || IsFeeContext(uri),
            _ => false
        };
        if (!valid) throw new SteamException(SteamFailure.ContractChanged);
    }
    internal async Task<string> SendAsync(HttpRequestMessage request, CancellationToken token, bool allowWalletHtml = false)
    {
        using var scope=DiagnosticHub.Scope(correlation:DiagnosticHub.Context?.CorrelationId is null);
        var started=DateTimeOffset.UtcNow;var watch=Stopwatch.StartNew();
        ValidateUri(request.RequestUri!);
        var path = request.RequestUri!.AbsolutePath;
        var endpoint=path switch {
            "/IAuthenticationService/BeginAuthSessionViaQR/v1/"=>"AuthBegin", "/IAuthenticationService/PollAuthSessionStatus/v1/"=>"AuthPoll",
            "/jwt/finalizelogin"=>"AuthFinalize", "/login/settoken"=>"AuthTransfer", "/market/"=>"Wallet", "/market/ajaxfeestructure/"=>"Fees",
            "/market/orderbook"=>"Orderbook", "/market/pricehistory/"=>"History", "/market/mylistings/render/"=>"Listings", "/market/sellitem/"=>"Sell",
            _=>IsFeeContext(request.RequestUri!)?"FeeContext":path.StartsWith("/inventory/",StringComparison.Ordinal)?"Inventory":"Cancel"};
        DiagnosticHub.Emit(DiagnosticEvent.HttpRequest,DiagnosticArea.HTTP,DiagnosticOutcome.Started,endpoint:endpoint,method:request.Method.Method);
        var mustPost = path.Contains("AuthSession", StringComparison.Ordinal) || path is "/jwt/finalizelogin" or "/login/settoken" or "/market/sellitem/" || path.StartsWith("/market/removelisting/", StringComparison.Ordinal);
        if (request.Method != (mustPost ? HttpMethod.Post : HttpMethod.Get)) throw new SteamException(SteamFailure.ContractChanged);
        if (!Access.ResearchRiskAccepted) throw new SteamException(SteamFailure.Disabled);
        var phase="QueueWait";
        try{await queue.WaitAsync(token);}catch(OperationCanceledException error){DiagnosticHub.Emit(DiagnosticEvent.HttpFailure,DiagnosticArea.HTTP,DiagnosticOutcome.Cancelled,LogLevel.Warning,DiagnosticError.Cancelled,endpoint:endpoint,reason:phase,exception:error);throw;}
        try
        {
            if (Halted is { } halted) throw new SteamException(halted);
            if(FeatureFailure(endpoint) is {} feature)throw new SteamException(feature){Endpoint=endpoint};
            var wait = last + gap - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero) await Task.Delay(wait, token);
            if (!Access.ResearchRiskAccepted) throw new SteamException(SteamFailure.Disabled);
            last = DateTimeOffset.UtcNow;
            phase="ConnectOrHeaders";
            using var response = await SendWalletHeadersAsync(request, token, allowWalletHtml);
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                try { foreach (var cookie in cookies) Cookies.SetCookies(request.RequestUri!,cookie); }
                catch (CookieException) { Stop(SteamFailure.ContractChanged); throw new SteamException(SteamFailure.ContractChanged); }
            }
            var status = (int)response.StatusCode;
            int? steamCode=response.Headers.TryGetValues("x-eresult",out var codes)&&int.TryParse(codes.FirstOrDefault(),out var numeric)?numeric:null;
            var apiError=steamCode is null or 1?DiagnosticError.None:request.Method!=HttpMethod.Get?DiagnosticError.Authentication:steamCode==27?DiagnosticError.Authentication:steamCode==15?DiagnosticError.Authorization:DiagnosticError.Forbidden;
            DiagnosticHub.Emit(DiagnosticEvent.HttpResponse,DiagnosticArea.HTTP, status==200&&(steamCode is null or 1)?DiagnosticOutcome.Succeeded:DiagnosticOutcome.Failed,
                status==200&&(steamCode is null or 1)?LogLevel.Information:LogLevel.Warning, steamCode is not (null or 1)?apiError:status switch{401=>DiagnosticError.Authentication,403=>DiagnosticError.Forbidden,429=>DiagnosticError.RateLimited,>=500=>DiagnosticError.Server,>=400=>DiagnosticError.ClientRequest,_=>DiagnosticError.None},
                endpoint:endpoint,method:request.Method.Method,status:status,serverCode:steamCode,started:started,elapsed:watch.ElapsedMilliseconds);
            if(status is >=300 and <400 && request.Method==HttpMethod.Get && response.Headers.Location is {} location)
            {
                var target=location.IsAbsoluteUri?location:new Uri(request.RequestUri!,location);
                var official=target.Scheme=="https"&&target.Port==443&&target.UserInfo.Length==0&&target.Host is "steamcommunity.com" or "store.steampowered.com" or "login.steampowered.com";
                var login=official&&(target.AbsolutePath=="/login"||target.AbsolutePath.StartsWith("/login/",StringComparison.Ordinal)||target.Host=="login.steampowered.com");
                DiagnosticHub.Emit(DiagnosticEvent.ReadContract,DiagnosticArea.Data,DiagnosticOutcome.Blocked,LogLevel.Warning,login?DiagnosticError.Authorization:DiagnosticError.Unsupported,endpoint:endpoint,reason:login?"LoginRedirect":official?"OtherRedirect":"UnsafeRedirect",status:status);
                if(login){BlockFeature(endpoint);throw new SteamException(SteamFailure.Authorization){Endpoint=endpoint};}
                if(official)throw new SteamException(SteamFailure.Unsupported){Endpoint=endpoint,Redirected=true};
                Stop(SteamFailure.Challenge);throw new SteamException(SteamFailure.Challenge){Endpoint=endpoint};
            }
            if (status != 200)
            {
                var failure = status switch { 404 => SteamFailure.Unsupported, 401 => SteamFailure.Authentication, 403 => SteamFailure.Forbidden, 429 => SteamFailure.RateLimited,
                    >= 300 and < 400 => SteamFailure.Authentication, _ => SteamFailure.Transport };
                if(failure!=SteamFailure.Unsupported && !(request.Method==HttpMethod.Get && failure==SteamFailure.Transport))Stop(failure); throw new SteamException(failure);
            }
            if (response.Headers.TryGetValues("x-eresult", out var results) && results.FirstOrDefault() is { } result && result != "1")
            { var failure=request.Method==HttpMethod.Get?(result=="15"?SteamFailure.Authorization:result=="27"?SteamFailure.Expired:SteamFailure.Restricted):(result=="15"?SteamFailure.Denied:result=="27"?SteamFailure.Expired:SteamFailure.Authentication);if(failure==SteamFailure.Authorization)BlockFeature(endpoint);else Stop(failure);throw new SteamException(failure,steamCode){Diagnostic=apiError}; }
            // Bounded reads prevent unexpected HTML/large payloads from consuming unbounded memory.
            phase="Body";
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var bytes = new MemoryStream(); var buffer = new byte[8192]; int read;
            while ((read = await stream.ReadAsync(buffer, token)) != 0)
            {
                if (bytes.Length + read > 8 * 1024 * 1024) { Stop(SteamFailure.ContractChanged); throw new SteamException(SteamFailure.ContractChanged); }
                bytes.Write(buffer, 0, read);
            }
            var text = System.Text.Encoding.UTF8.GetString(bytes.ToArray());
            var bodyKind=SteamResponseContract.Inspect(text);
            string? declaredMedia;
            try{declaredMedia=response.Content.Headers.ContentType?.MediaType;}
            catch(FormatException){declaredMedia="invalid";}
            SteamResponseContract.Record(endpoint,declaredMedia,bodyKind,status);
            if(bodyKind==SteamBodyKind.JsonBody&&SteamResponseContract.RequiresChallenge(text))
            {Stop(SteamFailure.Challenge);throw new SteamException(SteamFailure.Challenge);}
            if(bodyKind is SteamBodyKind.HtmlChallenge or SteamBodyKind.HtmlAccessDenied)
            {
                var blocked=bodyKind==SteamBodyKind.HtmlChallenge?SteamFailure.Challenge:SteamFailure.Forbidden;
                Stop(blocked);throw new SteamException(blocked){BodyKind=bodyKind};
            }
            if(bodyKind is SteamBodyKind.HtmlLogin)
            {BlockFeature(endpoint);throw new SteamException(SteamFailure.Authorization){BodyKind=bodyKind};}
            if(bodyKind is (SteamBodyKind.HtmlMarket or SteamBodyKind.HtmlUnknown) && !(allowWalletHtml && (request.RequestUri!.AbsoluteUri == "https://steamcommunity.com/market/"||IsFeeContext(request.RequestUri!))))
            {
                // Unknown HTML still stops further traffic; it is not evidence of CAPTCHA or logout.
                if(bodyKind==SteamBodyKind.HtmlUnknown)Stop(SteamFailure.ContractChanged);else BlockFeature(endpoint,SteamFailure.ContractChanged);
                throw new SteamException(SteamFailure.ContractChanged){BodyKind=bodyKind};
            }
            DiagnosticHub.Emit(DiagnosticEvent.ResponseBody,DiagnosticArea.HTTP,DiagnosticOutcome.Completed,endpoint:endpoint,method:request.Method.Method,started:started,elapsed:watch.ElapsedMilliseconds);
            return text;
        }
        catch (OperationCanceledException error) when (token.IsCancellationRequested)
        { DiagnosticHub.Emit(DiagnosticEvent.HttpFailure, DiagnosticArea.HTTP, DiagnosticOutcome.Cancelled, LogLevel.Warning, DiagnosticError.Cancelled, endpoint: endpoint, reason: phase, exception: error, started: started, elapsed: watch.ElapsedMilliseconds); throw; }
        catch(SteamException error)
        { error.Endpoint??=endpoint;SteamDiagnostics.Failure(error,endpoint,watch.ElapsedMilliseconds);throw; }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException || error is IOException and not SteamException)
        { DiagnosticHub.Emit(DiagnosticEvent.HttpFailure,DiagnosticArea.HTTP,token.IsCancellationRequested?DiagnosticOutcome.Cancelled:DiagnosticOutcome.Failed,LogLevel.Error,
            token.IsCancellationRequested?DiagnosticError.Cancelled:error is TaskCanceledException?DiagnosticError.Timeout:DiagnosticError.Transport,endpoint:endpoint,reason:phase,exception:error,started:started,elapsed:watch.ElapsedMilliseconds);
          if(request.Method!=HttpMethod.Get)Stop(SteamFailure.Transport); throw new SteamException(SteamFailure.Transport){Endpoint=endpoint}; }
        finally { queue.Release(); }
    }
    internal static HttpRequestMessage Form(Uri uri, IEnumerable<KeyValuePair<string, string>> fields)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new FormUrlEncodedContent(fields) };
        request.Headers.Referrer = new("https://steamcommunity.com/");
        request.Headers.Add("Origin", "https://steamcommunity.com");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }
    internal static JsonDocument Json(string body)
    {
        try { return JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 32 }); }
        catch (JsonException error) { DiagnosticHub.Emit(DiagnosticEvent.ResponseParsed,DiagnosticArea.HTTP,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticError.Parsing,exception:error); throw new SteamException(SteamFailure.ContractChanged){Diagnostic=DiagnosticError.Parsing}; }
    }
    public void Dispose() { Access.Revoke(); client.Dispose(); queue.Dispose(); }
}
