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
    public void Acknowledge(bool unofficialAutomationRisk, bool credentialRisk, bool reads, bool trading)
    {
        if (!unofficialAutomationRisk || !credentialRisk) throw new InvalidOperationException("SteamRiskAcknowledgementRequired");
        ResearchRiskAccepted = true; AuthenticatedReads = reads; Trading = reads && trading;
    }
    public void Revoke() { ResearchRiskAccepted = AuthenticatedReads = Trading = false; }
    public override string ToString() => "SteamAccess (session-only)";
}

public enum SteamFailure { Disabled, Authentication, Forbidden, RateLimited, Challenge, ContractChanged, Transport, Restricted, Expired, Denied, StaleReview, Unsupported }
public sealed record SteamTradingPolicy(bool Enabled, string EvidenceReference, bool StackAmountsSupported = false,bool MedianPriceContractSupported = false)
{
    public static SteamTradingPolicy Blocked { get; } = new(false, "Valve permission not established");
    internal static SteamTradingPolicy ContractTest { get; } = new(true, "synthetic contract only", true);
}
public sealed class SteamException(SteamFailure code) : IOException(code.ToString())
{
    public SteamFailure Code { get; } = code;
}

// No browser cookie import, external host, redirects, automatic retries or response-body logging.
public sealed class SteamHttp : IDisposable
{
    private readonly HttpClient client;
    private readonly SemaphoreSlim queue = new(1, 1);
    private DateTimeOffset last;
    private readonly TimeSpan gap;
    internal readonly CookieContainer Cookies = new();
    public SteamAccess Access { get; }
    public SteamFailure? Halted { get; private set; }
    public SteamHttp(SteamAccess access, HttpMessageHandler? testHandler = null, TimeSpan? testGap = null)
    {
        Access = access; gap = testHandler is null ? TimeSpan.FromSeconds(3) : testGap ?? TimeSpan.Zero;
        client = new(testHandler ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true, CookieContainer = Cookies, UseProxy = false });
        client.Timeout = TimeSpan.FromSeconds(25);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MarketCrate/0.4 (experimental; user-initiated)");
    }
    public void Stop(SteamFailure failure) => Halted ??= failure;
    internal static void ValidateUri(Uri uri)
    {
        if (uri.Scheme != "https" || uri.Port != 443 || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0) throw new SteamException(SteamFailure.ContractChanged);
        var path = uri.AbsolutePath;
        var valid = uri.Host switch
        {
            "api.steampowered.com" => path is "/IAuthenticationService/BeginAuthSessionViaQR/v1/" or "/IAuthenticationService/PollAuthSessionStatus/v1/",
            "login.steampowered.com" => path == "/jwt/finalizelogin",
            "steamcommunity.com" => path is "/login/settoken" or "/market/" or "/market/ajaxfeestructure/" or "/market/orderbook" or "/market/pricehistory/" or "/market/mylistings/render/" or "/market/sellitem/" ||
                path.StartsWith("/inventory/", StringComparison.Ordinal) || path.StartsWith("/market/removelisting/", StringComparison.Ordinal),
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
            _=>path.StartsWith("/inventory/",StringComparison.Ordinal)?"Inventory":"Cancel"};
        DiagnosticHub.Emit(DiagnosticEvent.HttpRequest,DiagnosticArea.HTTP,DiagnosticOutcome.Started,endpoint:endpoint,method:request.Method.Method);
        var mustPost = path.Contains("AuthSession", StringComparison.Ordinal) || path is "/jwt/finalizelogin" or "/login/settoken" or "/market/sellitem/" || path.StartsWith("/market/removelisting/", StringComparison.Ordinal);
        if (request.Method != (mustPost ? HttpMethod.Post : HttpMethod.Get)) throw new SteamException(SteamFailure.ContractChanged);
        if (!Access.ResearchRiskAccepted) throw new SteamException(SteamFailure.Disabled);
        var phase="QueueWait";
        try{await queue.WaitAsync(token);}catch(OperationCanceledException error){DiagnosticHub.Emit(DiagnosticEvent.HttpFailure,DiagnosticArea.HTTP,DiagnosticOutcome.Cancelled,LogLevel.Warning,DiagnosticError.Cancelled,endpoint:endpoint,reason:phase,exception:error);throw;}
        try
        {
            if (Halted is { } halted) throw new SteamException(halted);
            var wait = last + gap - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero) await Task.Delay(wait, token);
            if (!Access.ResearchRiskAccepted) throw new SteamException(SteamFailure.Disabled);
            last = DateTimeOffset.UtcNow;
            phase="ConnectOrHeaders";
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                try { foreach (var cookie in cookies) Cookies.SetCookies(request.RequestUri!,cookie); }
                catch (CookieException) { Stop(SteamFailure.ContractChanged); throw new SteamException(SteamFailure.ContractChanged); }
            }
            var status = (int)response.StatusCode;
            int? steamCode=response.Headers.TryGetValues("x-eresult",out var codes)&&int.TryParse(codes.FirstOrDefault(),out var numeric)?numeric:null;
            DiagnosticHub.Emit(DiagnosticEvent.HttpResponse,DiagnosticArea.HTTP, status==200?DiagnosticOutcome.Succeeded:DiagnosticOutcome.Failed,
                status==200?LogLevel.Information:LogLevel.Warning, status switch{401=>DiagnosticError.Authentication,403=>DiagnosticError.Forbidden,429=>DiagnosticError.RateLimited,>=500=>DiagnosticError.Server,>=400=>DiagnosticError.ClientRequest,_=>DiagnosticError.None},
                endpoint:endpoint,method:request.Method.Method,status:status,serverCode:steamCode,started:started,elapsed:watch.ElapsedMilliseconds);
            if (status != 200)
            {
                var failure = status switch { 404 => SteamFailure.Unsupported, 401 => SteamFailure.Authentication, 403 => SteamFailure.Forbidden, 429 => SteamFailure.RateLimited,
                    >= 300 and < 400 => SteamFailure.Authentication, _ => SteamFailure.Transport };
                if(failure!=SteamFailure.Unsupported)Stop(failure); throw new SteamException(failure);
            }
            if (response.Headers.TryGetValues("x-eresult", out var results) && results.FirstOrDefault() is { } result && result != "1")
            { var failure = result == "15" ? SteamFailure.Denied : result == "27" ? SteamFailure.Expired : SteamFailure.Authentication; Stop(failure); throw new SteamException(failure); }
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
            if (text.TrimStart().StartsWith('<') && !(allowWalletHtml && request.RequestUri!.AbsoluteUri == "https://steamcommunity.com/market/")) { Stop(SteamFailure.Challenge); throw new SteamException(SteamFailure.Challenge); }
            DiagnosticHub.Emit(DiagnosticEvent.ResponseBody,DiagnosticArea.HTTP,DiagnosticOutcome.Completed,endpoint:endpoint,method:request.Method.Method,started:started,elapsed:watch.ElapsedMilliseconds);
            return text;
        }
        catch (OperationCanceledException error) when (token.IsCancellationRequested)
        { DiagnosticHub.Emit(DiagnosticEvent.HttpFailure, DiagnosticArea.HTTP, DiagnosticOutcome.Cancelled, LogLevel.Warning, DiagnosticError.Cancelled, endpoint: endpoint, reason: phase, exception: error, started: started, elapsed: watch.ElapsedMilliseconds); throw; }
        catch(SteamException error)
        { SteamDiagnostics.Failure(error,endpoint,watch.ElapsedMilliseconds);throw; }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException || error is IOException and not SteamException)
        { DiagnosticHub.Emit(DiagnosticEvent.HttpFailure,DiagnosticArea.HTTP,token.IsCancellationRequested?DiagnosticOutcome.Cancelled:DiagnosticOutcome.Failed,LogLevel.Error,
            token.IsCancellationRequested?DiagnosticError.Cancelled:error is TaskCanceledException?DiagnosticError.Timeout:DiagnosticError.Transport,endpoint:endpoint,reason:phase,exception:error,started:started,elapsed:watch.ElapsedMilliseconds);
          Stop(SteamFailure.Transport); throw new SteamException(SteamFailure.Transport); }
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
        catch (JsonException error) { DiagnosticHub.Emit(DiagnosticEvent.ResponseParsed,DiagnosticArea.HTTP,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticError.Parsing,exception:error); throw new SteamException(SteamFailure.ContractChanged); }
    }
    public void Dispose() { Access.Revoke(); client.Dispose(); queue.Dispose(); }
}
