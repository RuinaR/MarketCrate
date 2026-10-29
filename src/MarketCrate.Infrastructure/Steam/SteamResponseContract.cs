using System.Text.RegularExpressions;
using System.Text.Json;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

// Fixed categories only. Never return page text, titles, form values or a raw Content-Type.
public enum SteamBodyKind { JsonBody, EmptyBody, OtherBody, HtmlMarket, HtmlLogin, HtmlChallenge, HtmlAccessDenied, HtmlUnknown }
internal static class SteamResponseContract
{
    internal static string Media(string? type) => type?.ToLowerInvariant() switch
    {
        "application/json" => "MediaJson", "text/html" => "MediaHtml", "text/plain" => "MediaText",
        null or "" => "MediaMissing", _ => "MediaOther"
    };
    internal static SteamBodyKind Inspect(string body)
    {
        var trimmed=body.AsSpan().TrimStart().TrimStart('\uFEFF').TrimStart();
        if(trimmed.IsEmpty)return SteamBodyKind.EmptyBody;
        if(trimmed[0] is '{' or '[')return SteamBodyKind.JsonBody;
        if(trimmed[0]!='<')return SteamBodyKind.OtherBody;
        // Markers outside the inspection limit remain unknown, never assumed safe.
        var sample=trimmed[..Math.Min(trimmed.Length,128*1024)].ToString();
        try
        {
            var visible=Regex.Replace(sample,@"<!--.*?(?:-->|$)|<script\b[^>]*>.*?(?:</script\s*>|$)|<style\b[^>]*>.*?(?:</style\s*>|$)","",RegexOptions.IgnoreCase|RegexOptions.Singleline|RegexOptions.NonBacktracking,TimeSpan.FromMilliseconds(100));
            bool Has(string pattern)=>Regex.IsMatch(visible,pattern,RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
            if(Has("<(?:div|iframe|form)\\b[^>]{0,2048}(?:class|id)\\s*=\\s*[\"'][^\"']{0,256}\\b(?:g-recaptcha|h-captcha|cf-chl-widget|challenge-form)\\b"))return SteamBodyKind.HtmlChallenge;
            if(Has("<form\\b[^>]{0,2048}action\\s*=\\s*[\"'](?:https://steamcommunity\\.com)?/login/(?:dologin|home)/?[\"']")&&Has("<input\\b[^>]{0,2048}type\\s*=\\s*[\"']password[\"']"))return SteamBodyKind.HtmlLogin;
            if(Has("<(?:title|h1)\\b[^>]*>\\s*(?:Access Denied|Forbidden)\\s*</(?:title|h1)\\s*>"))return SteamBodyKind.HtmlAccessDenied;
            if(Has("<(?:div|main|section)\\b[^>]{0,2048}(?:id|class)\\s*=\\s*[\"'][^\"']{0,256}\\bmarket_home(?:_|\\b)"))return SteamBodyKind.HtmlMarket;
        }
        catch(RegexMatchTimeoutException){ }
        return SteamBodyKind.HtmlUnknown;
    }
    internal static void Record(string endpoint,string? media,SteamBodyKind kind,int status)
    {
        var declared=Media(media);
        var mismatch=declared=="MediaJson"&&kind!=SteamBodyKind.JsonBody||declared=="MediaHtml"&&kind==SteamBodyKind.JsonBody;
        DiagnosticHub.Emit(DiagnosticEvent.ResponseContract,DiagnosticArea.HTTP,DiagnosticOutcome.Completed,
            mismatch?LogLevel.Warning:LogLevel.Information,endpoint:endpoint,status:status,from:declared,to:kind.ToString(),reason:mismatch?"MediaTypeMismatch":"ResponseShape");
    }
    internal static bool RequiresChallenge(string body)
    {
        try
        {
            using var json=JsonDocument.Parse(body,new JsonDocumentOptions{MaxDepth=32});
            static bool Required(JsonElement element)=>element.ValueKind==JsonValueKind.Object&&
                ((element.TryGetProperty("captcha_needed",out var captcha)&&captcha.ValueKind==JsonValueKind.True)||
                 (element.TryGetProperty("requires_captcha",out var required)&&required.ValueKind==JsonValueKind.True));
            var root=json.RootElement;
            return Required(root)||root.ValueKind==JsonValueKind.Object&&root.TryGetProperty("response",out var response)&&Required(response);
        }
        catch(JsonException){return false;} // The endpoint parser still rejects malformed JSON; never succeeds from this result.
    }
}
