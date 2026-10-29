using System.Net;
using System.Text.RegularExpressions;
using MarketCrate.Core;
namespace MarketCrate.Infrastructure.Steam;

public sealed partial class SteamHttp
{
    private static readonly HttpRequestOptionsKey<bool> FeeVanity=new("MarketCrate.FeeContextVanity");
    private static readonly HttpRequestOptionsKey<Uri> FeeResolved=new("MarketCrate.FeeContextResolved");
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Uri,Uri> feeContexts=[];
    internal void RememberFeeContext(HttpRequestMessage request)
    {
        if(request.Options.TryGetValue(FeeResolved,out var target)&&FeeContextRedirectKind(request.RequestUri!,target) is "FeeContextVanityRedirect" or "FeeContextCanonicalRedirect")
            feeContexts[request.RequestUri!]=target;
    }
    private async Task<HttpResponseMessage> SendFeeContextInitialAsync(HttpRequestMessage request,CancellationToken token)
    {
        if(!feeContexts.TryGetValue(request.RequestUri!,out var target))return await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
        using var direct=new HttpRequestMessage(HttpMethod.Get,target);
        request.Options.Set(FeeVanity,FeeContextRedirectKind(request.RequestUri!,target)=="FeeContextVanityRedirect");request.Options.Set(FeeResolved,target);
        DiagnosticHub.Emit(DiagnosticEvent.ReadContract,DiagnosticArea.HTTP,endpoint:"FeeContext",reason:"FeeContextRouteReused",count:1);
        var response=await client.SendAsync(direct,HttpCompletionOption.ResponseHeadersRead,token);
        try
        {
            if(!response.IsSuccessStatusCode)feeContexts.TryRemove(request.RequestUri!,out _);
            CaptureCookies(response,target);return response;
        }
        catch{response.Dispose();throw;}
    }
    internal static bool FeeContextUsesVanity(HttpRequestMessage request)=>request.Options.TryGetValue(FeeVanity,out var value)&&value;
    internal static bool ViewingOwnInventory(string html)
    {
        var timeout=TimeSpan.FromMilliseconds(100);
        var markup=Regex.Replace(html,@"<!--.*?(?:-->|$)","",RegexOptions.Singleline,timeout);
        var values=Regex.Matches(markup,@"<script\b[^>]*>(.*?)</script\s*>",RegexOptions.Singleline|RegexOptions.IgnoreCase,timeout).Cast<Match>()
            .SelectMany(script=>Regex.Matches(script.Groups[1].Value,@"(?:^|(?<=[;\r\n]))\s*(?:var\s+)?g_bViewingOwnProfile\s*=\s*(true|false)\s*;",RegexOptions.CultureInvariant,timeout).Cast<Match>()).Select(x=>x.Groups[1].Value).ToArray();
        return values.Length==1&&values[0]=="true";
    }
    internal static string FeeContextRedirectKind(Uri source,Uri target)
    {
        if(target.Scheme!="https"||target.Host!="steamcommunity.com"||target.Port!=443||target.UserInfo.Length!=0||target.Fragment.Length!=0||target.Query is not("" or "?l=english" or "?l=koreana"))return "FeeContextOtherRedirect";
        if(Regex.IsMatch(target.AbsolutePath,@"^/id/[A-Za-z0-9_-]{1,64}/inventory/?$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100)))return "FeeContextVanityRedirect";
        var own=Regex.Match(source.AbsolutePath,@"^/profiles/([0-9]{17})/inventory/$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
        if(own.Success&&(target.AbsolutePath==source.AbsolutePath||target.AbsolutePath==source.AbsolutePath.TrimEnd('/')))return source==target?"FeeContextOtherRedirect":"FeeContextCanonicalRedirect";
        return target.AbsolutePath.StartsWith("/profiles/",StringComparison.Ordinal)?"FeeContextOtherProfile":"FeeContextOtherRedirect";
    }
    private async Task<HttpResponseMessage> SendFeeContextHeadersAsync(HttpRequestMessage request,HttpResponseMessage response,CancellationToken token)
    {
        if((int)response.StatusCode is not(301 or 302 or 303 or 307 or 308)||response.Headers.Location is not {} location)return response;
        var source=request.RequestUri!;var target=location.IsAbsoluteUri?location:new Uri(source,location);var kind=FeeContextRedirectKind(source,target);
        DiagnosticHub.Emit(DiagnosticEvent.ReadContract,DiagnosticArea.HTTP,endpoint:"FeeContext",status:(int)response.StatusCode,reason:kind);
        if(kind is not("FeeContextVanityRedirect" or "FeeContextCanonicalRedirect")||response.Headers.Contains("x-eresult"))return response;
        try
        {
            CaptureCookies(response,source);response.Dispose();
            var wait=last+gap-DateTimeOffset.UtcNow;if(wait>TimeSpan.Zero)await Task.Delay(wait,token);
            if(Halted is {} blocked)throw new SteamException(blocked);
            if(!Access.ResearchRiskAccepted||!Access.AuthenticatedReads)throw new SteamException(SteamFailure.Disabled);
            token.ThrowIfCancellationRequested();last=DateTimeOffset.UtcNow;
            using var follow=new HttpRequestMessage(HttpMethod.Get,target);
            request.Options.Set(FeeVanity,kind=="FeeContextVanityRedirect");
            request.Options.Set(FeeResolved,target);
            DiagnosticHub.Emit(DiagnosticEvent.HttpRequest,DiagnosticArea.HTTP,DiagnosticOutcome.Started,endpoint:"FeeContext",method:"GET",reason:kind,count:1);
            var result=await client.SendAsync(follow,HttpCompletionOption.ResponseHeadersRead,token);
            try{CaptureCookies(result,target);return result;}catch{result.Dispose();throw;}
        }
        catch{response.Dispose();throw;}
    }
}
