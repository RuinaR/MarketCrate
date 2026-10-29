using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

public enum FeeContractMode { VerifiedLegacy, VerifiedNew, Unsupported, Unverified, Restricted }
public sealed record FeeContractEvidence(FeeContractMode Mode, SteamWalletObservation Wallet, long? PublisherMillionths, string Reason);

// React receives newFeeRates/walletInfo in data-props. The classic inventory selects
// SellItemDialog with explicit g_bReactSellDialog=false and the same current economy_common formula.
// The obsolete wallet_fee_base formula is not that branch. No failure-driven fallback is allowed.
public static class FeeContractResolver
{
    private static readonly TimeSpan Timeout=TimeSpan.FromMilliseconds(100);
    public static FeeContractEvidence Observe(string html, int appId, SteamWalletObservation wallet)
    {
        FeeContractEvidence Result(FeeContractMode mode,string reason,long? publisher=null)
        {
            DiagnosticHub.Emit(DiagnosticEvent.FeeValidated,DiagnosticArea.Pricing,
                mode is FeeContractMode.VerifiedLegacy or FeeContractMode.VerifiedNew?DiagnosticOutcome.Completed:DiagnosticOutcome.Blocked,
                requirement:"D10",reason:mode.ToString(),from:reason,to:mode.ToString(),currency:wallet.Currency);
            return new(mode,wallet,publisher,reason);
        }
        if(appId<=0)throw new ArgumentOutOfRangeException(nameof(appId));
        if(wallet.LoggedIn==false)return Result(FeeContractMode.Restricted,"LoginRequired");
        try
        {
            // Parse attribute values, not arbitrary newFeeRates text inside script/comment content.
            var markup=Regex.Replace(html,@"<!--.*?(?:-->|$)|<script\b[^>]*>.*?(?:</script\s*>|$)|<style\b[^>]*>.*?(?:</style\s*>|$)","",RegexOptions.Singleline|RegexOptions.IgnoreCase,Timeout);
            var targets=Regex.Matches(markup,@"<[a-zA-Z][^>]*\bdata-featuretarget\s*=\s*(['""])marketselldialog\1[^>]*>",RegexOptions.IgnoreCase,Timeout);
            var classicFlag=ReadInlineBoolean(html,"g_bReactSellDialog");
            DiagnosticHub.Emit(DiagnosticEvent.FeeValidated,DiagnosticArea.Pricing,DiagnosticOutcome.Completed,requirement:"D10",endpoint:"Fees",reason:"FeeUiObserved",from:classicFlag.Invalid?"ModeInvalid":classicFlag.Value==false?"ClassicUiSelected":classicFlag.Value==true?"ReactUiSelected":"ModeMissing",count:targets.Count);
            if(classicFlag.Invalid)return Result(FeeContractMode.Unverified,"ModeAmbiguous");
            JsonElement? props=null;
            foreach(Match target in targets)
            {
                var attributes=Regex.Matches(target.Value,@"([\w-]+)\s*=\s*(['""])(.*?)\2",RegexOptions.Singleline,Timeout);
                var values=attributes.Cast<Match>().Where(x=>x.Groups[1].Value=="data-props").ToArray();
                if(values.Length!=1)return Result(FeeContractMode.Unverified,"ModeMissing");
                using var json=JsonDocument.Parse(WebUtility.HtmlDecode(values[0].Groups[3].Value),new JsonDocumentOptions{MaxDepth=32});
                if(props is not null)return Result(FeeContractMode.Unverified,"ModeAmbiguous");
                props=json.RootElement.Clone();
            }
            var classic=props is null;
            var newer=false;
            JsonElement info;
            if(classic)
            {
                if(classicFlag.Value!=false)return Result(FeeContractMode.Unverified,classicFlag.Value==true?"ReactPropsMissing":"ModeMissing");
                if(!SteamHttp.ViewingOwnInventory(html))return Result(FeeContractMode.Unverified,"OwnerPageMissing");
                if(wallet.LoggedIn!=true)return Result(FeeContractMode.Unverified,"LoginRequired");
                var ownWallet=ReadAssignedObject(html,"g_rgWalletInfo");
                if(ownWallet is not {} own)return Result(FeeContractMode.Unverified,"WalletMissing");
                info=own;
            }
            else
            {
                var p=props!.Value;
                if(p.ValueKind!=JsonValueKind.Object)return Result(FeeContractMode.Unverified,"ModeInvalid");
                if(p.EnumerateObject().Select(x=>x.Name).Distinct(StringComparer.Ordinal).Count()!=p.EnumerateObject().Count())return Result(FeeContractMode.Unverified,"ModeAmbiguous");
                if(classicFlag.Value==false)return Result(FeeContractMode.Unverified,"ModeAmbiguous");
                // Missing newFeeRates is false only inside a genuine sell-dialog props object.
                if(p.TryGetProperty("newFeeRates",out var flag))
                {if(flag.ValueKind is not(JsonValueKind.True or JsonValueKind.False))return Result(FeeContractMode.Unverified,"ModeInvalid");newer=flag.GetBoolean();}
                if(!p.TryGetProperty("walletInfo",out info)||info.ValueKind!=JsonValueKind.Object)return Result(FeeContractMode.Unverified,"WalletMissing");
            }
            var embedded=SteamFees.ObserveWallet("g_rgWalletInfo="+info.GetRawText()+";");
            if(embedded.Currency!=wallet.Currency||embedded.MinimumWire!=wallet.MinimumWire||embedded.IncrementWire!=wallet.IncrementWire||embedded.LegacyFees!=wallet.LegacyFees)
                return Result(FeeContractMode.Unverified,"WalletChanged");
            if(!wallet.PricingMetadataAvailable)return Result(FeeContractMode.Unsupported,"PricingMetadataMissing");
            if(newer)return Result(FeeContractMode.VerifiedNew,"ExplicitNewMode");
            if(wallet.LegacyFees is not {Complete:true,Enabled:not null} old)return Result(FeeContractMode.Unverified,"LegacyIncomplete");
            // Current official false branch does not consume base; a conflicting historical profile
            // cannot silently be interpreted with either old or new arithmetic.
            if(old.BaseWire!=0||old.MinimumWire>wallet.MinimumWire||old.Enabled==false&&old.SteamMillionths!=0)
                return Result(FeeContractMode.Unsupported,"LegacyFormulaConflict");
            var app=ReadAssignedObject(html,"g_rgAppContextData");
            if(app is not {} apps||!apps.TryGetProperty(appId.ToString(System.Globalization.CultureInfo.InvariantCulture),out var context))
                return Result(FeeContractMode.Unverified,"AppContextMissing");
            var publisher=context.TryGetProperty("market_pubfee_rate",out var rate)&&rate.ValueKind!=JsonValueKind.Null?Rate(rate):old.DefaultPublisherMillionths;
            if(publisher is null)return Result(FeeContractMode.Unverified,"PublisherMissing");
            return Result(FeeContractMode.VerifiedLegacy,classic?"ClassicInventoryContext":"WalletAndAppContext",publisher);
        }
        catch(Exception error)when(error is JsonException or RegexMatchTimeoutException or FormatException or OverflowException or InvalidOperationException or SteamException)
        {return Result(FeeContractMode.Unverified,"ModeInvalid");}
    }
    private static (bool? Value,bool Invalid) ReadInlineBoolean(string html,string name)
    {
        var markup=Regex.Replace(html,@"<!--.*?(?:-->|$)","",RegexOptions.Singleline,Timeout);
        var values=Regex.Matches(markup,@"<script\b[^>]*>(.*?)</script\s*>",RegexOptions.Singleline|RegexOptions.IgnoreCase,Timeout).Cast<Match>()
            .Select(script=>Regex.Replace(script.Groups[1].Value,@"'(?:\\.|[^'\\])*'|""(?:\\.|[^""\\])*""|`(?:\\.|[^`\\])*`|/\*.*?\*/|//[^\r\n]*",m=>m.Value.StartsWith('/')?" ":"__quoted__",RegexOptions.Singleline,Timeout))
            .SelectMany(script=>Regex.Matches(script,@"(?:^|(?<=[;\r\n]))\s*(?:var\s+)?"+Regex.Escape(name)+@"\s*=\s*([^;\r\n]+)\s*;",RegexOptions.CultureInvariant,Timeout).Cast<Match>()).Select(x=>x.Groups[1].Value.Trim()).ToArray();
        return values.Length==0?(null,false):values.Length!=1||values[0] is not("true" or "false")?(null,true):(values[0]=="true",false);
    }
    internal static long Rate(JsonElement value)
    {
        var n=decimal.Parse(value.ValueKind==JsonValueKind.String?value.GetString()!:value.GetRawText(),System.Globalization.CultureInfo.InvariantCulture)*1_000_000;
        if(n is <0 or >1_000_000||n!=decimal.Truncate(n))throw new FormatException();return(long)n;
    }
    private static JsonElement? ReadAssignedObject(string html,string name)
    {
        var matches=Regex.Matches(html,@"\b"+name+@"\s*=\s*",RegexOptions.CultureInvariant,Timeout);if(matches.Count!=1)return null;var match=matches[0];
        var reader=new System.Text.Json.Utf8JsonReader(Encoding.UTF8.GetBytes(html[(match.Index+match.Length)..]),new JsonReaderOptions{MaxDepth=32});
        using var doc=JsonDocument.ParseValue(ref reader);return doc.RootElement.ValueKind==JsonValueKind.Object?doc.RootElement.Clone():null;
    }
    public static SteamFeeSchedule Legacy(FeeContractEvidence evidence)
    {
        if(evidence.Mode!=FeeContractMode.VerifiedLegacy||evidence.PublisherMillionths is null)throw new SteamException(SteamFailure.Unsupported){Endpoint="Fees"};
        var wallet=evidence.Wallet.PricingWallet();var legacy=evidence.Wallet.LegacyFees!;
        var schedule=new SteamFeeSchedule(wallet.Currency,wallet.MinimumWire,wallet.IncrementWire,new(legacy.SteamMillionths!.Value),new(evidence.PublisherMillionths.Value));
        schedule.Validate();return schedule;
    }
}
