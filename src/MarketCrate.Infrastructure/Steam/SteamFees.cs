using System.Text;
using System.Text.Json;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

public sealed record SteamWallet(string Currency, long MinimumWire, long IncrementWire);
public static class SteamFees
{
    public static SteamWallet Wallet(string html)
    {
        try
        {
            var index = html.IndexOf("g_rgWalletInfo", StringComparison.Ordinal); if (index < 0) throw new SteamException(SteamFailure.ContractChanged);
            index = html.IndexOf('=', index) + 1; var start = html.IndexOf('{', index);
            if (index == 0 || start < index || start - index > 16) throw new SteamException(SteamFailure.ContractChanged);
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(html[start..])); using var document = JsonDocument.ParseValue(ref reader); var root = document.RootElement;
            return new(SteamContract.Currency((int)SteamContract.Integer(root.GetProperty("wallet_currency"))),
                SteamContract.Integer(root.GetProperty("wallet_market_minimum")), SteamContract.Integer(root.GetProperty("wallet_currency_increment")));
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        { throw new SteamException(SteamFailure.ContractChanged); }
    }
    public static SteamFeeSchedule Structure(string json, SteamWallet wallet) => SteamContract.Parse(json, root =>
    {
        if(root.EnumerateObject().Any(x=>x.Name is not("steam_fee" or "publisher_fee" or "success")))throw new SteamException(SteamFailure.ContractChanged);
        if(root.TryGetProperty("success",out var success)&&success.ValueKind!=JsonValueKind.True&&!(success.ValueKind==JsonValueKind.Number&&success.GetInt32()==1))throw new SteamException(SteamFailure.Restricted);
        SteamFeeRate Rate(string name)
        {
            var data = root.GetProperty(name); var raw = data.GetProperty("fee_rate");
            if(data.EnumerateObject().Any(x=>x.Name is not("fee_rate" or "fee_cap")))throw new SteamException(SteamFailure.ContractChanged);
            var value = decimal.Parse(raw.ValueKind == JsonValueKind.String ? raw.GetString()! : raw.GetRawText(), System.Globalization.CultureInfo.InvariantCulture) * 1_000_000;
            if (value != decimal.Truncate(value) || value is < 0 or > 1_000_000) throw new SteamException(SteamFailure.ContractChanged);
            return new((long)value, data.TryGetProperty("fee_cap", out var cap) ? SteamContract.Integer(cap) : 0);
        }
        var result = new SteamFeeSchedule(wallet.Currency, wallet.MinimumWire, wallet.IncrementWire, Rate("steam_fee"), Rate("publisher_fee"));
        result.Validate(); DiagnosticHub.Emit(DiagnosticEvent.FeeValidated,DiagnosticArea.Pricing,requirement:"D10");return result;
    });
    public static async Task<SteamFeeSchedule> FetchAsync(SteamHttp http, int appId, CancellationToken token)
    {
        var wallet=await FetchWalletAsync(http,token);return await FetchStructureAsync(http,appId,wallet,token);
    }
    public static async Task<SteamWallet> FetchWalletAsync(SteamHttp http,CancellationToken token)
    {
        using var correlation=DiagnosticHub.Scope(correlation:true);using var request=new HttpRequestMessage(HttpMethod.Get,"https://steamcommunity.com/market/");return Wallet(await http.SendAsync(request,token,true));
    }
    public static async Task<SteamFeeSchedule> FetchStructureAsync(SteamHttp http,int appId,SteamWallet wallet,CancellationToken token)
    {
        using var correlation=DiagnosticHub.Scope(correlation:true);using var request=new HttpRequestMessage(HttpMethod.Get,$"https://steamcommunity.com/market/ajaxfeestructure/?appid={appId}");return Structure(await http.SendAsync(request,token),wallet);
    }
}
