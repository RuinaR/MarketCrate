// Web refresh-token validation and cookie-issuance flow adapted from steam-session (MIT).
// Copyright (c) 2022 Alex Corn. Upstream 3ac0f34fd964b3f886ba18ef4824ac43c942e030.
// See ThirdParty/steam-session/LICENSE. Community-only, no automatic retries or mobile approval.
using MarketCrate.Core;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MarketCrate.Infrastructure.Steam;

public enum SteamAuthStage { AuthBegin, RequestEncoding, HTTP, ApiResult, ResponseParse, ResponseFields, ChallengeValidation, TokenValidation, AccountValidation }

// Intentionally not a record: secrets must not be included in generated ToString/debug output.
public sealed class SteamQrAuthentication(SteamHttp http, TimeProvider? timeProvider = null) : IDisposable
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private ulong clientId;
    private SteamAuthStage stage;
    private string activeEndpoint="AuthBegin";
    public SteamAuthStage? LastFailureStage {get;private set;}
    private byte[] requestId = [];
    private DateTimeOffset expires, nextPoll;
    private TimeSpan interval;
    private byte[] rememberedRefresh=[];
    public bool RememberRequested{get;set;}
    public bool CanRemember=>Connected&&rememberedRefresh.Length>0;
    public void ForgetRememberedToken(){RememberRequested=false;CryptographicOperations.ZeroMemory(rememberedRefresh);rememberedRefresh=[];}
    public Uri? Challenge { get; private set; }
    public string? SteamId { get; private set; }
    internal string? SessionId { get; private set; }
    public bool HasSession => SteamId is not null && SessionId is not null && http.Access.AuthenticatedReads;
    public bool Connected => HasSession && http.Halted is not (SteamFailure.Authentication or SteamFailure.Expired);
    public Task<Uri> BeginAsync(CancellationToken token = default) => GuardAsync(() => BeginCoreAsync(token), "AuthBegin");
    private async Task<Uri> BeginCoreAsync(CancellationToken token)
    {
        if (!http.Access.AuthenticatedReads) throw new SteamException(SteamFailure.Disabled);
        Dispose();
        // Minimal independent encoding of public protocol field numbers; no protobuf dependency/source copying.
        stage=SteamAuthStage.RequestEncoding;
        const string friendlyName="MarketCrate experimental";
        const ulong webBrowser=2; // EAuthTokenPlatformType.WebBrowser, not OS or SteamClient.
        var details = Proto.Text(1, friendlyName).Concat(Proto.Number(2, webBrowser)).ToArray();
        var request = Proto.Text(1,friendlyName).Concat(Proto.Number(2,webBrowser)).Concat(Proto.Bytes(3,details)).Concat(Proto.Text(4,"Community")).ToArray();
        using var form = AuthForm("BeginAuthSessionViaQR", request);
        using var document = await ReadResponseAsync(form,token,"AuthBegin");
        var root = document.RootElement.GetProperty("response");
        clientId = ReadClientId(root.GetProperty("client_id"));
        var encoded=root.GetProperty("request_id").GetString();
        if(string.IsNullOrEmpty(encoded)||encoded.Length>348)throw new SteamException(SteamFailure.ContractChanged);
        requestId = Convert.FromBase64String(encoded);
        if (requestId.Length is < 1 or > 256 || clientId == 0) throw new SteamException(SteamFailure.ContractChanged);
        stage=SteamAuthStage.ChallengeValidation;
        Challenge = ValidateChallenge(root.GetProperty("challenge_url").GetString()!);
        stage=SteamAuthStage.ResponseFields;
        var seconds = root.GetProperty("interval").GetDouble();
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > 60) throw new SteamException(SteamFailure.ContractChanged);
        interval = TimeSpan.FromSeconds(seconds); if(interval<=TimeSpan.Zero)throw new SteamException(SteamFailure.ContractChanged); nextPoll = clock.GetUtcNow() + interval; expires = clock.GetUtcNow().AddMinutes(2);
        return Challenge;
    }
    // No background polling. Each call must be made by an explicit user login/check action.
    public Task<bool> CheckAsync(CancellationToken token = default) => GuardAsync(() => CheckCoreAsync(token), "AuthPoll");
    private async Task<bool> CheckCoreAsync(CancellationToken token)
    {
        if (Challenge is null || clock.GetUtcNow() > expires) throw new SteamException(SteamFailure.Expired);
        if (clock.GetUtcNow() < nextPoll) return false;
        nextPoll = clock.GetUtcNow() + interval;
        using var form = AuthForm("PollAuthSessionStatus", Proto.Number(1, clientId).Concat(Proto.Bytes(2, requestId)).ToArray());
        using var document = await ReadResponseAsync(form,token,"AuthPoll");
        var root = document.RootElement.GetProperty("response");
        if(root.TryGetProperty("captcha_needed",out var captcha)&&captcha.ValueKind==JsonValueKind.True)throw new SteamException(SteamFailure.Challenge);
        if(root.TryGetProperty("agreement_session_url",out var agreement)&&!string.IsNullOrEmpty(agreement.GetString()))throw new SteamException(SteamFailure.Restricted);
        if(root.EnumerateObject().Any(x=>x.Name is not("new_client_id" or "new_challenge_url" or "refresh_token" or "access_token" or "had_remote_interaction" or "account_name" or "new_guard_data" or "agreement_session_url")))throw new SteamException(SteamFailure.ContractChanged);
        if (root.TryGetProperty("new_client_id", out var id) && ReadClientId(id) is >0 and var nextId) clientId = nextId;
        if (root.TryGetProperty("new_challenge_url", out var challenge) && !string.IsNullOrEmpty(challenge.GetString())) {stage=SteamAuthStage.ChallengeValidation;Challenge = ValidateChallenge(challenge.GetString()!);stage=SteamAuthStage.ResponseFields;}
        if (!root.TryGetProperty("refresh_token", out var refresh) || string.IsNullOrEmpty(refresh.GetString())) return false;
        var refreshValue=refresh.GetString()!;
        string? tokenOwner=null;
        if(RememberRequested){try{stage=SteamAuthStage.TokenValidation;tokenOwner=SteamWebRefreshToken.Inspect(refreshValue,clock.GetUtcNow()).Account;}catch(SteamException){/* QR can connect, but an unrecognized token is not saved. */}}
        await FinalizeAsync(refreshValue, token,tokenOwner);
        if(RememberRequested)
        {
            try{SteamWebRefreshToken.Inspect(refreshValue,clock.GetUtcNow(),SteamId);rememberedRefresh=Encoding.UTF8.GetBytes(refreshValue);}
            catch(SteamException){DiagnosticHub.Emit(DiagnosticEvent.AuthContract,DiagnosticArea.Authentication,DiagnosticOutcome.Blocked,LogLevel.Warning,DiagnosticError.Unsupported,reason:"TokenValidation");}
        }
        CryptographicOperations.ZeroMemory(requestId); requestId = []; Challenge = null;
        return true;
    }
    // WebBrowser getWebCookies contract from node-steam-session, restricted to Community.
    // No GenerateAccessTokenForApp: that route is not supported for the WebBrowser audience.
    public Task<bool> RestoreAsync(SteamSavedLogin saved,CancellationToken token=default)=>GuardAsync(async()=>
    {
        if(!http.Access.AuthenticatedReads)throw new SteamException(SteamFailure.Disabled);
        Dispose();stage=SteamAuthStage.TokenValidation;
        var refresh=Encoding.UTF8.GetString(saved.Token);SteamWebRefreshToken.Inspect(refresh,clock.GetUtcNow(),saved.Account);
        await FinalizeAsync(refresh,token,saved.Account);token.ThrowIfCancellationRequested();
        if(RememberRequested)rememberedRefresh=saved.Token.ToArray();
        DiagnosticHub.Emit(DiagnosticEvent.AuthState,DiagnosticArea.Authentication,reason:"SessionRestored");return true;
    },"AuthRestore");
    public void SaveRememberedLogin(SteamAuthStore store,SteamSavedLogin? restored=null)
    {
        if(!OperatingSystem.IsWindows()||!CanRemember)throw new SteamException(SteamFailure.Unsupported);
        store.Save(SteamId!,rememberedRefresh,clock.GetUtcNow(),restored);
        CryptographicOperations.ZeroMemory(rememberedRefresh);rememberedRefresh=[];
    }
    private async Task FinalizeAsync(string refresh, CancellationToken token,string? expectedAccount=null)
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.AuthState,DiagnosticArea.Authentication,"A02");
        var csrf = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
        using var form = SteamHttp.Form(new("https://login.steampowered.com/jwt/finalizelogin"), new Dictionary<string, string>
        { ["nonce"] = refresh, ["sessionid"] = csrf, ["redir"] = "https://steamcommunity.com/login/home/?goto=" });
        using var document = await ReadResponseAsync(form,token,"AuthFinalize");
        var root = document.RootElement;
        if(root.TryGetProperty("error",out var error)&&error.TryGetInt32(out var resultCode)&&resultCode!=1)
            throw new SteamException(resultCode==27?SteamFailure.Expired:resultCode==15?SteamFailure.Denied:SteamFailure.Authentication,resultCode);
        var steamId = SteamContract.Id(root.GetProperty("steamID"));
        if(steamId.Length!=17||expectedAccount is not null&&expectedAccount!=steamId){stage=SteamAuthStage.AccountValidation;throw new SteamException(SteamFailure.Authentication){Diagnostic=DiagnosticError.Authorization};}
        var transfers = root.GetProperty("transfer_info").EnumerateArray().Where(x => x.GetProperty("url").GetString() == "https://steamcommunity.com/login/settoken").ToArray();
        if (transfers.Length != 1) throw new SteamException(SteamFailure.ContractChanged);
        var parameters = transfers[0].GetProperty("params");
        var fields = new Dictionary<string, string> { ["steamID"] = steamId, ["nonce"] = parameters.GetProperty("nonce").GetString()!, ["auth"] = parameters.GetProperty("auth").GetString()! };
        using var transfer = SteamHttp.Form(new("https://steamcommunity.com/login/settoken"), fields);
        activeEndpoint="AuthTransfer";stage=SteamAuthStage.HTTP;
        var transferBody=await http.SendAsync(transfer,token);var explicitSuccess=false;
        if(!string.IsNullOrWhiteSpace(transferBody))
        {
            stage=SteamAuthStage.ResponseParse;using var result=JsonDocument.Parse(transferBody);stage=SteamAuthStage.ResponseFields;
            if(result.RootElement.ValueKind!=JsonValueKind.Object)throw new SteamException(SteamFailure.ContractChanged);
            if(result.RootElement.TryGetProperty("result",out var code)){if(code.GetInt32()!=1)throw new SteamException(SteamFailure.Authentication);explicitSuccess=true;}
        }
        if (http.Cookies.GetCookies(new("https://steamcommunity.com/"))["steamLoginSecure"] is not {Secure:true,Expired:false,Value.Length:>0})
            throw new SteamException(SteamFailure.Authentication);
        // Upstream accepts cookie-bearing settoken responses without a JSON result.
        // Here they additionally require an account-bound cookie, even for a new QR session.
        if((expectedAccount is not null||!explicitSuccess)&&!Uri.UnescapeDataString(http.Cookies.GetCookies(new("https://steamcommunity.com/"))["steamLoginSecure"]!.Value).StartsWith(steamId+"||",StringComparison.Ordinal))
        {stage=SteamAuthStage.AccountValidation;throw new SteamException(SteamFailure.Authentication){Diagnostic=DiagnosticError.Authorization};}
        http.Cookies.Add(new System.Net.Cookie("sessionid", csrf, "/", "steamcommunity.com") { Secure = true });
        SessionId = csrf; SteamId = steamId;
    }
    private static HttpRequestMessage AuthForm(string method, byte[] data)
    {
        var request=SteamHttp.Form(new($"https://api.steampowered.com/IAuthenticationService/{method}/v1/"),
            new Dictionary<string,string>{["input_protobuf_encoded"]=Convert.ToBase64String(data),["format"]="json"});
        request.Headers.Accept.ParseAdd("application/json");return request;
    }
    private async Task<JsonDocument> ReadResponseAsync(HttpRequestMessage request,CancellationToken token,string endpoint)
    {
        activeEndpoint=endpoint;stage=SteamAuthStage.HTTP;
        string body;
        try{body=await http.SendAsync(request,token);}catch(SteamException error)when(error.ApiResult is not null){stage=SteamAuthStage.ApiResult;throw;}
        stage=SteamAuthStage.ResponseParse;
        var document=JsonDocument.Parse(body,new JsonDocumentOptions{MaxDepth=32});
        stage=SteamAuthStage.ResponseFields;return document;
    }
    private static ulong ReadClientId(JsonElement value)=>value.ValueKind switch
    {
        JsonValueKind.String when ulong.TryParse(value.GetString(),NumberStyles.None,CultureInfo.InvariantCulture,out var id)=>id,
        JsonValueKind.Number when value.TryGetUInt64(out var id)=>id,
        _=>throw new SteamException(SteamFailure.ContractChanged)
    };
    private static Uri ValidateChallenge(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 4096 || !Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "s.team" || uri.Port != 443 || !uri.AbsolutePath.StartsWith("/q/", StringComparison.Ordinal) || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            throw new SteamException(SteamFailure.ContractChanged);
        return uri;
    }
    private async Task<T> GuardAsync<T>(Func<Task<T>> action, string endpoint)
    {
        using var operation=DiagnosticHub.Operation(endpoint=="AuthBegin"?DiagnosticEvent.Login:DiagnosticEvent.LoginCheck,DiagnosticArea.Authentication,endpoint=="AuthBegin"?"A01":"A03");
        activeEndpoint=endpoint;stage=SteamAuthStage.AuthBegin;LastFailureStage=null;
        try { return await action(); }
        catch (OperationCanceledException) { Dispose(); DiagnosticHub.Emit(DiagnosticEvent.AuthState, DiagnosticArea.Authentication, DiagnosticOutcome.Cancelled, LogLevel.Warning, DiagnosticError.Cancelled, requirement: "A03", endpoint: endpoint); throw; }
        catch (Exception error) when (error is SteamException or JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException)
        {
            var code = error is SteamException steam ? steam.Code : SteamFailure.ContractChanged;
            LastFailureStage=stage;
            var classification=error is JsonException?DiagnosticError.Parsing:error is SteamException typed?SteamDiagnostics.Classify(typed):SteamDiagnostics.Classify(code);
            DiagnosticHub.Emit(DiagnosticEvent.AuthContract,DiagnosticArea.Authentication,DiagnosticOutcome.Blocked,LogLevel.Error,classification,requirement:"A03",endpoint:activeEndpoint,reason:stage.ToString(),serverCode:(error as SteamException)?.ApiResult,exception:error);
            Dispose(); http.Stop(code);
            DiagnosticHub.Emit(DiagnosticEvent.AuthState, DiagnosticArea.Authentication, DiagnosticOutcome.Blocked, LogLevel.Error, classification, requirement: "A03", endpoint: activeEndpoint, reason: code.ToString(), exception: error);
            throw new SteamException(code,(error as SteamException)?.ApiResult){Diagnostic=classification};
        }
    }
    public void Dispose()
    {
        http.Access.RevokeTrading();
        SteamId = SessionId = null; Challenge = null; CryptographicOperations.ZeroMemory(requestId); requestId = [];
        CryptographicOperations.ZeroMemory(rememberedRefresh);rememberedRefresh=[];
        foreach (System.Net.Cookie cookie in http.Cookies.GetAllCookies()) cookie.Expired = true;
    }
    public override string ToString() => "SteamQrAuthentication (redacted)";
    private static class Proto
    {
        public static byte[] Number(int field, ulong value) => Varint((ulong)(field << 3)).Concat(Varint(value)).ToArray();
        public static byte[] Text(int field, string value) => Bytes(field, Encoding.UTF8.GetBytes(value));
        public static byte[] Bytes(int field, byte[] value) => Varint((ulong)((field << 3) | 2)).Concat(Varint((ulong)value.Length)).Concat(value).ToArray();
        private static IEnumerable<byte> Varint(ulong value) { while (value > 127) { yield return (byte)((value & 127) | 128); value >>= 7; } yield return (byte)value; }
    }
}
