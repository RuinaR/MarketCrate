using MarketCrate.Core;
namespace MarketCrate.Infrastructure.Steam;

public sealed partial class SteamHttp
{
    // Bounded protocol: one canonical move, one server eligibility check, one fixed return.
    // Login/challenge/restriction replies are never bypassed, retried or rewritten.
    private async Task<HttpResponseMessage> SendWalletHeadersAsync(HttpRequestMessage request,CancellationToken token,bool wallet)
    {
        var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
        if(wallet&&request.Method==HttpMethod.Get&&IsFeeContext(request.RequestUri!))return await SendFeeContextHeadersAsync(request,response,token);
        if(!wallet||request.Method!=HttpMethod.Get||request.RequestUri!.AbsolutePath!="/market/")return response;
        var current=request.RequestUri!;var canonical=false;Uri? eligibilityReturn=null;var returned=false;var hop=0;
        try
        {
            while(true)
            {
                CaptureCookies(response,current);
                if(current.AbsolutePath=="/market/eligibilitycheck/"&&response.StatusCode==System.Net.HttpStatusCode.OK)
                {DiagnosticHub.Emit(DiagnosticEvent.ReadContract,DiagnosticArea.HTTP,DiagnosticOutcome.Blocked,LogLevel.Warning,DiagnosticError.Authorization,endpoint:"Wallet",status:200,reason:"EligibilityIncomplete");BlockFeature("Wallet");throw new SteamException(SteamFailure.Authorization){Endpoint="Wallet"};}
                if((int)response.StatusCode is not(301 or 302 or 303 or 307 or 308)||response.Headers.Location is not {} location)return response;
                Uri target;try{target=location.IsAbsoluteUri?location:new Uri(current,location);}catch(UriFormatException){return response;}
                RecordMarketRedirect(target,(int)response.StatusCode,hop);
                if(response.Headers.Contains("x-eresult")||hop>=3)return response;
                string reason;
                if(current.AbsolutePath=="/market/eligibilitycheck/")
                {
                    if(returned||eligibilityReturn is null||target!=eligibilityReturn)return response;
                    returned=true;reason="MarketEligibilityReturn";
                }
                else if(eligibilityReturn is null&&EligibilityTarget(target) is {} back)
                {eligibilityReturn=back;reason="MarketEligibilityRedirect";}
                else if(!canonical&&NormalMarketTarget(current,target))
                {canonical=true;reason="MarketCanonicalRedirect";}
                else return response;
                DiagnosticHub.Emit(DiagnosticEvent.HttpResponse,DiagnosticArea.HTTP,DiagnosticOutcome.Completed,endpoint:"Wallet",method:"GET",status:(int)response.StatusCode,reason:reason);
                response.Dispose();
                var wait=last+gap-DateTimeOffset.UtcNow;if(wait>TimeSpan.Zero)await Task.Delay(wait,token);
                if(Halted is {} halted)throw new SteamException(halted);
                if(!Access.ResearchRiskAccepted||!Access.AuthenticatedReads)throw new SteamException(SteamFailure.Disabled);
                token.ThrowIfCancellationRequested();last=DateTimeOffset.UtcNow;current=target;hop++;
                using var follow=new HttpRequestMessage(HttpMethod.Get,target);
                DiagnosticHub.Emit(DiagnosticEvent.HttpRequest,DiagnosticArea.HTTP,DiagnosticOutcome.Started,endpoint:"Wallet",method:"GET",reason:reason,count:hop);
                response=await client.SendAsync(follow,HttpCompletionOption.ResponseHeadersRead,token);
            }
        }
        catch{response.Dispose();throw;}
    }
    internal static Uri? EligibilityTarget(Uri target)
    {
        if(target.Scheme!="https"||target.Host!="steamcommunity.com"||target.Port!=443||target.UserInfo.Length!=0||target.Fragment.Length!=0||target.AbsolutePath!="/market/eligibilitycheck/"||!target.Query.StartsWith("?goto=",StringComparison.Ordinal)||target.Query.Contains('&'))return null;
        var path=Uri.UnescapeDataString(target.Query[6..]);
        return path is "/market/" or "/market" or "/market/?l=english" or "/market/?l=koreana" or "/market?l=english" or "/market?l=koreana"?new Uri("https://steamcommunity.com"+path):null;
    }
    private void CaptureCookies(HttpResponseMessage response,Uri uri)
    {
        if(!response.Headers.TryGetValues("Set-Cookie",out var values))return;
        try{foreach(var cookie in values)Cookies.SetCookies(uri,cookie);}
        catch(System.Net.CookieException){Stop(SteamFailure.ContractChanged);throw new SteamException(SteamFailure.ContractChanged);}
    }
    internal static bool NormalMarketTarget(Uri source,Uri target)
    {
        if(source==target||source.Host!="steamcommunity.com"||target.Scheme!="https"||target.Host!=source.Host||target.Port!=443||target.UserInfo.Length!=0||target.Fragment.Length!=0||target.AbsolutePath is not("/market/" or "/market"))return false;
        return target.Query is "" or "?l=english" or "?l=koreana";
    }
    internal static (string Host,string Path,string Query) DescribeMarketRedirect(Uri target)
    {
        var host=target.Scheme!="https"||target.Port!=443||target.UserInfo.Length!=0?"RedirectUnsafeAuthority":target.Host switch{"steamcommunity.com"=>"RedirectCommunity","store.steampowered.com"=>"RedirectStore","login.steampowered.com"=>"RedirectLoginHost",_=>"RedirectExternal"};
        var path=target.AbsolutePath switch{"/market/"=>"RedirectMarketSlash","/market"=>"RedirectMarketNoSlash","/market/eligibilitycheck/"=>"RedirectEligibilityPath","/"=>"RedirectRoot",var p when p=="/login"||p.StartsWith("/login/",StringComparison.Ordinal)=>"RedirectLoginPath",var p when p.StartsWith("/market/",StringComparison.Ordinal)=>"RedirectMarketOther",var p when p.StartsWith("/agecheck/",StringComparison.Ordinal)||p.StartsWith("/captcha",StringComparison.Ordinal)=>"RedirectSecurity",var p when p.StartsWith("/account",StringComparison.Ordinal)=>"RedirectAccount",_=>"RedirectOtherPath"};
        var query=target.Fragment.Length>0?"RedirectFragment":target.Query switch{""=>"RedirectNoQuery","?l=english" or "?l=koreana"=>"RedirectLanguageQuery",_=>"RedirectOtherQuery"};
        return(host,path,query);
    }
    private static void RecordMarketRedirect(Uri target,int status,int hop)
    {
        var facts=DescribeMarketRedirect(target);
        DiagnosticHub.Emit(DiagnosticEvent.ReadContract,DiagnosticArea.HTTP,DiagnosticOutcome.Completed,endpoint:"Wallet",status:status,from:facts.Host,to:facts.Query,reason:facts.Path,count:hop);
    }
}
