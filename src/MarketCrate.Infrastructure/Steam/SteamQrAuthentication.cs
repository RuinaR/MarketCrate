using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MarketCrate.Infrastructure.Steam;

// Intentionally not a record: secrets must not be included in generated ToString/debug output.
public sealed class SteamQrAuthentication(SteamHttp http, TimeProvider? timeProvider = null) : IDisposable
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private ulong clientId;
    private byte[] requestId = [];
    private DateTimeOffset expires, nextPoll;
    private TimeSpan interval;
    public Uri? Challenge { get; private set; }
    public string? SteamId { get; private set; }
    internal string? SessionId { get; private set; }
    public bool Connected => SteamId is not null && SessionId is not null && http.Halted is null && http.Access.AuthenticatedReads;
    public Task<Uri> BeginAsync(CancellationToken token = default) => GuardAsync(() => BeginCoreAsync(token));
    private async Task<Uri> BeginCoreAsync(CancellationToken token)
    {
        if (!http.Access.AuthenticatedReads) throw new SteamException(SteamFailure.Disabled);
        Dispose();
        // Minimal independent encoding of public protocol field numbers; no protobuf dependency/source copying.
        var details = Proto.Text(1, "MarketCrate experimental").Concat(Proto.Number(2, 2)).ToArray();
        var request = Proto.Bytes(3, details);
        using var form = AuthForm("BeginAuthSessionViaQR", request);
        using var document = SteamHttp.Json(await http.SendAsync(form, token));
        var root = document.RootElement.GetProperty("response");
        clientId = ulong.Parse(root.GetProperty("client_id").GetString()!, CultureInfo.InvariantCulture);
        requestId = Convert.FromBase64String(root.GetProperty("request_id").GetString()!);
        if (requestId.Length is < 1 or > 256 || clientId == 0) throw new SteamException(SteamFailure.ContractChanged);
        Challenge = ValidateChallenge(root.GetProperty("challenge_url").GetString()!);
        var seconds = root.GetProperty("interval").GetDouble();
        if (!double.IsFinite(seconds) || seconds < 1 || seconds > 60) throw new SteamException(SteamFailure.ContractChanged);
        interval = TimeSpan.FromSeconds(seconds); nextPoll = clock.GetUtcNow() + interval; expires = clock.GetUtcNow().AddMinutes(2);
        return Challenge;
    }
    // No background polling. Each call must be made by an explicit user login/check action.
    public Task<bool> CheckAsync(CancellationToken token = default) => GuardAsync(() => CheckCoreAsync(token));
    private async Task<bool> CheckCoreAsync(CancellationToken token)
    {
        if (Challenge is null || clock.GetUtcNow() > expires) throw new SteamException(SteamFailure.Expired);
        if (clock.GetUtcNow() < nextPoll) return false;
        nextPoll = clock.GetUtcNow() + interval;
        using var form = AuthForm("PollAuthSessionStatus", Proto.Number(1, clientId).Concat(Proto.Bytes(2, requestId)).ToArray());
        using var document = SteamHttp.Json(await http.SendAsync(form, token));
        var root = document.RootElement.GetProperty("response");
        if(root.TryGetProperty("captcha_needed",out var captcha)&&captcha.ValueKind==JsonValueKind.True)throw new SteamException(SteamFailure.Challenge);
        if(root.TryGetProperty("agreement_session_url",out var agreement)&&!string.IsNullOrEmpty(agreement.GetString()))throw new SteamException(SteamFailure.Restricted);
        if(root.EnumerateObject().Any(x=>x.Name is not("new_client_id" or "new_challenge_url" or "refresh_token" or "access_token" or "had_remote_interaction" or "account_name" or "new_guard_data" or "agreement_session_url")))throw new SteamException(SteamFailure.ContractChanged);
        if (root.TryGetProperty("new_client_id", out var id) && ulong.Parse(id.GetString()!, CultureInfo.InvariantCulture) is >0 and var nextId) clientId = nextId;
        if (root.TryGetProperty("new_challenge_url", out var challenge)) Challenge = ValidateChallenge(challenge.GetString()!);
        if (!root.TryGetProperty("refresh_token", out var refresh) || string.IsNullOrEmpty(refresh.GetString())) return false;
        // Refresh token exists only in this stack/response. No disk persistence, JWT logging or third-party transfer.
        await FinalizeAsync(refresh.GetString()!, token);
        CryptographicOperations.ZeroMemory(requestId); requestId = []; Challenge = null;
        return true;
    }
    private async Task FinalizeAsync(string refresh, CancellationToken token)
    {
        var csrf = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
        using var form = SteamHttp.Form(new("https://login.steampowered.com/jwt/finalizelogin"), new Dictionary<string, string>
        { ["nonce"] = refresh, ["sessionid"] = csrf, ["redir"] = "https://steamcommunity.com/login/home/?goto=" });
        using var document = SteamHttp.Json(await http.SendAsync(form, token));
        var root = document.RootElement;
        var steamId = SteamContract.Id(root.GetProperty("steamID"));
        var transfers = root.GetProperty("transfer_info").EnumerateArray().Where(x => x.GetProperty("url").GetString() == "https://steamcommunity.com/login/settoken").ToArray();
        if (transfers.Length != 1) throw new SteamException(SteamFailure.ContractChanged);
        var parameters = transfers[0].GetProperty("params");
        var fields = new Dictionary<string, string> { ["steamID"] = steamId, ["nonce"] = parameters.GetProperty("nonce").GetString()!, ["auth"] = parameters.GetProperty("auth").GetString()! };
        using var transfer = SteamHttp.Form(new("https://steamcommunity.com/login/settoken"), fields);
        using var result = SteamHttp.Json(await http.SendAsync(transfer, token));
        if (result.RootElement.GetProperty("result").GetInt32() != 1 || http.Cookies.GetCookies(new("https://steamcommunity.com/"))["steamLoginSecure"] is null)
            throw new SteamException(SteamFailure.Authentication);
        http.Cookies.Add(new System.Net.Cookie("sessionid", csrf, "/", "steamcommunity.com") { Secure = true });
        SessionId = csrf; SteamId = steamId;
    }
    private static HttpRequestMessage AuthForm(string method, byte[] data) => SteamHttp.Form(new($"https://api.steampowered.com/IAuthenticationService/{method}/v1/"),
        new Dictionary<string, string> { ["input_protobuf_encoded"] = Convert.ToBase64String(data) });
    private static Uri ValidateChallenge(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "s.team" || uri.Port != 443 || !uri.AbsolutePath.StartsWith("/q/", StringComparison.Ordinal) || uri.UserInfo.Length != 0)
            throw new SteamException(SteamFailure.ContractChanged);
        return uri;
    }
    private async Task<T> GuardAsync<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (Exception error) when (error is SteamException or JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException or OperationCanceledException)
        {
            var code = error is SteamException steam ? steam.Code : SteamFailure.ContractChanged;
            Dispose(); http.Stop(code); throw new SteamException(code);
        }
    }
    public void Dispose()
    {
        SteamId = SessionId = null; Challenge = null; CryptographicOperations.ZeroMemory(requestId); requestId = [];
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
