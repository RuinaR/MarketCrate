using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MarketCrate.Core;
namespace MarketCrate.Infrastructure.Steam;

public sealed record SteamWallet(string Currency,long MinimumWire,long IncrementWire);
// Historical fields are evidence only. Resolver also requires current server mode/app context.
public sealed record SteamLegacyFeeObservation(int Fields,bool Invalid,long? SteamMillionths,long? MinimumWire,long? BaseWire,long? DefaultPublisherMillionths)
{ public bool Complete=>Fields==5&&!Invalid; public bool? Enabled{get;init;} }
public sealed record SteamWalletObservation(string? Currency,long? MinimumWire,long? IncrementWire,bool? LoggedIn,string Shape)
{
    public SteamLegacyFeeObservation? LegacyFees{get;init;}
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
            var legacy=ObserveLegacyFields(root);
            DiagnosticHub.Emit(DiagnosticEvent.WalletContract,DiagnosticArea.Data,endpoint:"Wallet",requirement:"D10",count:legacy.Fields,reason:legacy.Invalid?"WalletLegacyInvalid":legacy.Complete?"WalletLegacyComplete":"WalletLegacyPartial");
            return new(currency,minimum,increment,loggedIn,"WalletObject"){LegacyFees=legacy};
        }
        catch(JsonException){throw new SteamException(SteamFailure.ContractChanged){Diagnostic=DiagnosticError.Parsing};}
        catch(Exception error)when(error is InvalidOperationException or KeyNotFoundException or ArgumentException or OverflowException){throw new SteamException(SteamFailure.ContractChanged);}
    }
    private static SteamLegacyFeeObservation ObserveLegacyFields(JsonElement root)
    {
        var names=new[]{"wallet_fee","wallet_fee_percent","wallet_fee_minimum","wallet_fee_base","wallet_publisher_fee_percent_default"};
        var count=names.Count(name=>root.TryGetProperty(name,out _));
        try
        {
            long? Rate(string name){if(!root.TryGetProperty(name,out var v))return null;var value=decimal.Parse(v.ValueKind==JsonValueKind.String?v.GetString()!:v.GetRawText(),System.Globalization.CultureInfo.InvariantCulture)*1_000_000;if(value is <0 or >1_000_000||value!=decimal.Truncate(value))throw new FormatException();return (long)value;}
            long? Amount(string name){if(!root.TryGetProperty(name,out var v))return null;var value=SteamContract.Integer(v);if(value is <0 or >1_000_000)throw new FormatException();return value;}
            if(root.TryGetProperty("wallet_fee",out var enabled)&&!(enabled.ValueKind is JsonValueKind.True or JsonValueKind.False)&&SteamContract.Integer(enabled) is not(0 or 1))throw new FormatException();
            return new(count,false,Rate("wallet_fee_percent"),Amount("wallet_fee_minimum"),Amount("wallet_fee_base"),Rate("wallet_publisher_fee_percent_default")){Enabled=root.TryGetProperty("wallet_fee",out var active)?active.ValueKind is JsonValueKind.True or JsonValueKind.False?active.GetBoolean():SteamContract.Integer(active)==1:null};
        }
        catch(Exception error)when(error is FormatException or InvalidOperationException or OverflowException){return new(count,true,null,null,null,null);}
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
    public static async Task<SteamFeeSchedule> FetchResolvedAsync(SteamHttp http,SteamQrAuthentication auth,int appId,CancellationToken token,Action<SteamWalletObservation>? observed=null)
    {
        if(!auth.Connected||auth.SteamId is not {} account)throw new SteamException(SteamFailure.Authentication);
        using var correlation=DiagnosticHub.Scope(correlation:true);
        using var request=new HttpRequestMessage(HttpMethod.Get,"https://steamcommunity.com/market/");
        var html=await http.SendAsync(request,token,true);var wallet=ObserveWallet(html);
        if(wallet.LoggedIn==false){http.BlockFeature("Wallet");throw new SteamException(SteamFailure.Authorization){Endpoint="Wallet"};}
        observed?.Invoke(wallet);
        var evidence=FeeContractResolver.Observe(html,appId,wallet);
        if(evidence.Mode==FeeContractMode.Unverified&&evidence.Reason=="ModeMissing")
        {
            using var profile=new HttpRequestMessage(HttpMethod.Get,$"https://steamcommunity.com/profiles/{account}/inventory/");
            html=await http.SendAsync(profile,token,true);
            if(SteamHttp.FeeContextUsesVanity(profile)&&!SteamHttp.ViewingOwnInventory(html))
            {DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Pricing,DiagnosticOutcome.Blocked,LogLevel.Warning,DiagnosticError.Authorization,endpoint:"FeeContext",reason:"OwnerPageMissing");http.BlockFeature("FeeContext");throw new SteamException(SteamFailure.Authorization){Endpoint="FeeContext"};}
            var identity=Regex.Match(html,@"\bg_steamID\s*=\s*['"" ]?([0-9]{17})['""]?\s*;",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
            if(!identity.Success||identity.Groups[1].Value!=account){http.BlockFeature("FeeContext");DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Pricing,DiagnosticOutcome.Blocked,LogLevel.Warning,DiagnosticError.Authorization,endpoint:"FeeContext",reason:"AccountMismatch");throw new SteamException(SteamFailure.Authorization){Endpoint="FeeContext"};}
            evidence=FeeContractResolver.Observe(html,appId,wallet);
        }
        if(!auth.Connected||auth.SteamId!=account)throw new SteamException(SteamFailure.Authentication);
        return evidence.Mode switch
        {
            FeeContractMode.VerifiedLegacy=>FeeContractResolver.Legacy(evidence),
            FeeContractMode.VerifiedNew=>await FetchStructureAsync(http,appId,wallet.PricingWallet(),token),
            FeeContractMode.Restricted=>throw new SteamException(SteamFailure.Authorization){Endpoint="Fees"},
            _=>throw new SteamException(SteamFailure.Unsupported){Endpoint="Fees"}
        };
    }
    public static async Task<SteamWalletObservation> FetchWalletObservationAsync(SteamHttp http,CancellationToken token)
    {
        using var correlation=DiagnosticHub.Scope(correlation:true);using var request=new HttpRequestMessage(HttpMethod.Get,"https://steamcommunity.com/market/");var result=ObserveWallet(await http.SendAsync(request,token,true));
        if(result.LoggedIn==false){http.BlockFeature("Wallet");throw new SteamException(SteamFailure.Authorization){Endpoint="Wallet"};}
        return result;
    }
    internal static async Task<SteamWallet> FetchTradingWalletAsync(SteamHttp http,string account,CancellationToken token)
    {
        using var request=new HttpRequestMessage(HttpMethod.Get,"https://steamcommunity.com/market/");
        var html=await http.SendAsync(request,token,true);
        // Compare in memory only; account content is never a diagnostic field.
        var identity=Regex.Match(html,@"\bg_steamID\s*=\s*['"" ]?([0-9]{17})['""]?\s*;",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
        if(!identity.Success||identity.Groups[1].Value!=account){DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Sales,DiagnosticOutcome.Blocked,LogLevel.Warning,DiagnosticError.Authorization,endpoint:"Wallet",reason:"AccountMismatch");throw new SteamException(SteamFailure.Authorization){Endpoint="Wallet"};}
        var wallet=ObserveWallet(html);if(wallet.LoggedIn!=true)throw new SteamException(SteamFailure.Authorization){Endpoint="Wallet"};
        try{return wallet.PricingWallet();}catch(SteamException error){error.Endpoint="Wallet";throw;}
    }
    public static async Task<SteamFeeStructure> FetchStructureObservationAsync(SteamHttp http,int appId,CancellationToken token)
    {using var correlation=DiagnosticHub.Scope(correlation:true);using var request=StructureRequest(appId);return ObserveStructure(await http.SendAsync(request,token));}
    internal static async Task<SteamFeeSchedule> FetchStructureAsync(SteamHttp http,int appId,SteamWallet wallet,CancellationToken token)
    {using var correlation=DiagnosticHub.Scope(correlation:true);using var request=StructureRequest(appId);return Structure(await http.SendAsync(request,token),wallet);}
    internal static HttpRequestMessage StructureRequest(int appId)
    {
        if(appId<=0)throw new ArgumentOutOfRangeException(nameof(appId));
        // The official sell dialog fetches this GET only for its newFeeRates mode.
        // JSON is requested explicitly; an HTML result is never a legacy-fee fallback.
        var request=new HttpRequestMessage(HttpMethod.Get,$"https://steamcommunity.com/market/ajaxfeestructure/?appid={appId}");
        request.Headers.Accept.Add(new("application/json"));return request;
    }
}
