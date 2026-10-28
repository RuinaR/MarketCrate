using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MarketCrate.Core;
namespace MarketCrate.Infrastructure.Steam;

public sealed record SteamWallet(string Currency,long MinimumWire,long IncrementWire);
public sealed record SteamWalletObservation(string? Currency,long? MinimumWire,long? IncrementWire,bool? LoggedIn,string Shape)
{
    public bool PricingMetadataAvailable=>Currency is not null&&MinimumWire is >0&&IncrementWire is >0;
    public SteamWallet PricingWallet()=>PricingMetadataAvailable?new(Currency!,MinimumWire!.Value,IncrementWire!.Value):throw new SteamException(SteamFailure.Unsupported);
}
public sealed record SteamFeeStructure(SteamFeeRate Steam,SteamFeeRate Publisher,bool AdditionalFields);
public static class SteamFees
{
    public static SteamWallet Wallet(string html)=>ObserveWallet(html).PricingWallet();
    public static SteamWalletObservation ObserveWallet(string html)
    {
        static Match Match(string input,string pattern){try{return Regex.Match(input,pattern,RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));}catch(RegexMatchTimeoutException){throw new SteamException(SteamFailure.ContractChanged){Diagnostic=DiagnosticError.Parsing};}}
        bool? loggedIn=null;var login=Match(html,@"\bg_bLoggedIn\s*=\s*(true|false)\s*;");
        if(login.Success)loggedIn=login.Groups[1].Value=="true";
        var marker=Match(html,@"\bg_rgWalletInfo\s*=\s*");
        void Fact(string reason,string? currency=null)=>DiagnosticHub.Emit(DiagnosticEvent.WalletContract,DiagnosticArea.Data,requirement:"D09",endpoint:"Wallet",reason:reason,from:loggedIn==true?"LoggedIn":loggedIn==false?"LoggedOut":"Unverified",currency:currency);
        if(!marker.Success){Fact("WalletMissing");return new(null,null,null,loggedIn,"WalletMissing");}
        try
        {
            var reader=new Utf8JsonReader(Encoding.UTF8.GetBytes(html[(marker.Index+marker.Length)..]),new JsonReaderOptions{MaxDepth=32});using var document=JsonDocument.ParseValue(ref reader);var root=document.RootElement;
            if(root.ValueKind==JsonValueKind.Null){Fact("WalletNull");return new(null,null,null,loggedIn,"WalletNull");}
            if(root.ValueKind!=JsonValueKind.Object)throw new SteamException(SteamFailure.ContractChanged);
            var currency=root.TryGetProperty("wallet_currency",out var c)?SteamContract.Currency(checked((int)SteamContract.Integer(c))):null;
            long? Optional(string field){if(!root.TryGetProperty(field,out var value)||value.ValueKind==JsonValueKind.Null)return null;var amount=SteamContract.Integer(value);if(amount<=0||amount>1_000_000)throw new SteamException(SteamFailure.ContractChanged);return amount;}
            var minimum=Optional("wallet_market_minimum");var increment=Optional("wallet_currency_increment");Fact("WalletObject",currency);if(currency is null)Fact("WalletCurrencyMissing");if(minimum is null)Fact("WalletMinimumMissing");if(increment is null)Fact("WalletIncrementMissing");
            return new(currency,minimum,increment,loggedIn,"WalletObject");
        }
        catch(JsonException){throw new SteamException(SteamFailure.ContractChanged){Diagnostic=DiagnosticError.Parsing};}
        catch(Exception error)when(error is InvalidOperationException or KeyNotFoundException or ArgumentException or OverflowException){throw new SteamException(SteamFailure.ContractChanged);}
    }
    public static SteamFeeStructure ObserveStructure(string json)=>SteamContract.Parse<SteamFeeStructure>(json,root=>
    {
        var additional=root.EnumerateObject().Any(x=>x.Name is not("steam_fee" or "publisher_fee" or "success"));
        if(root.TryGetProperty("success",out var success)&&success.ValueKind!=JsonValueKind.True&&!(success.ValueKind==JsonValueKind.Number&&success.GetInt32()==1))throw new SteamException(SteamFailure.Restricted);
        SteamFeeRate Rate(string name)
        {
            var data=root.GetProperty(name);additional|=data.EnumerateObject().Any(x=>x.Name is not("fee_rate" or "fee_cap"));var raw=data.GetProperty("fee_rate");
            var value=decimal.Parse(raw.ValueKind==JsonValueKind.String?raw.GetString()!:raw.GetRawText(),System.Globalization.CultureInfo.InvariantCulture)*1_000_000;
            var cap=data.TryGetProperty("fee_cap",out var limit)?SteamContract.Integer(limit):0;
            if(value!=decimal.Truncate(value)||value is <0 or >1_000_000||cap<0)throw new SteamException(SteamFailure.ContractChanged);
            return new((long)value,cap);
        }
        var steam=Rate("steam_fee");var publisher=Rate("publisher_fee");return new(steam,publisher,additional);
    });
    public static SteamFeeSchedule Structure(string json,SteamWallet wallet)
    {
        var data=ObserveStructure(json);if(data.AdditionalFields)throw new SteamException(SteamFailure.ContractChanged);
        var result=new SteamFeeSchedule(wallet.Currency,wallet.MinimumWire,wallet.IncrementWire,data.Steam,data.Publisher);result.Validate();DiagnosticHub.Emit(DiagnosticEvent.FeeValidated,DiagnosticArea.Pricing,requirement:"D10");return result;
    }
    public static async Task<SteamFeeSchedule> FetchAsync(SteamHttp http,int appId,CancellationToken token)
    {var wallet=await FetchWalletAsync(http,token);return await FetchStructureAsync(http,appId,wallet,token);}
    public static async Task<SteamWalletObservation> FetchWalletObservationAsync(SteamHttp http,CancellationToken token)
    {
        using var correlation=DiagnosticHub.Scope(correlation:true);using var request=new HttpRequestMessage(HttpMethod.Get,"https://steamcommunity.com/market/");var result=ObserveWallet(await http.SendAsync(request,token,true));
        if(result.LoggedIn==false){http.BlockFeature("Wallet");throw new SteamException(SteamFailure.Authorization){Endpoint="Wallet"};}
        return result;
    }
    public static async Task<SteamWallet> FetchWalletAsync(SteamHttp http,CancellationToken token)
    {var result=await FetchWalletObservationAsync(http,token);if(result.Currency is null)throw new SteamException(SteamFailure.WalletUnavailable);return result.PricingWallet();}
    public static async Task<SteamFeeStructure> FetchStructureObservationAsync(SteamHttp http,int appId,CancellationToken token)
    {using var correlation=DiagnosticHub.Scope(correlation:true);using var request=new HttpRequestMessage(HttpMethod.Get,$"https://steamcommunity.com/market/ajaxfeestructure/?appid={appId}");return ObserveStructure(await http.SendAsync(request,token));}
    public static async Task<SteamFeeSchedule> FetchStructureAsync(SteamHttp http,int appId,SteamWallet wallet,CancellationToken token)
    {using var correlation=DiagnosticHub.Scope(correlation:true);using var request=new HttpRequestMessage(HttpMethod.Get,$"https://steamcommunity.com/market/ajaxfeestructure/?appid={appId}");return Structure(await http.SendAsync(request,token),wallet);}
}
