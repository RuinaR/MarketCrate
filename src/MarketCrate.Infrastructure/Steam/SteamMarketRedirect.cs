using MarketCrate.Core;
namespace MarketCrate.Infrastructure.Steam;

public sealed partial class SteamHttp
{
    // One same-origin canonical/locale GET only. No POST, login, security, region or arbitrary query redirects.
    private async Task<HttpResponseMessage> SendWalletHeadersAsync(HttpRequestMessage request,CancellationToken token,bool wallet)
    {
        var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
        if(!wallet||request.Method!=HttpMethod.Get||request.RequestUri!.AbsolutePath!="/market/"||response.Headers.Location is not {} location||
            (int)response.StatusCode is not(301 or 302 or 303 or 307 or 308))return response;
        Uri target;
        try{target=location.IsAbsoluteUri?location:new Uri(request.RequestUri!,location);}catch(UriFormatException){return response;}
        RecordMarketRedirect(target,(int)response.StatusCode,0);
        if(response.Headers.Contains("x-eresult"))return response;
        if(!NormalMarketTarget(request.RequestUri!,target))return response;
        DiagnosticHub.Emit(DiagnosticEvent.HttpResponse,DiagnosticArea.HTTP,DiagnosticOutcome.Completed,endpoint:"Wallet",method:"GET",status:(int)response.StatusCode,reason:"MarketCanonicalRedirect");
        response.Dispose();
        var wait=last+gap-DateTimeOffset.UtcNow;if(wait>TimeSpan.Zero)await Task.Delay(wait,token);
        if(Halted is {} halted)throw new SteamException(halted);
        if(!Access.ResearchRiskAccepted)throw new SteamException(SteamFailure.Disabled);
        last=DateTimeOffset.UtcNow;
        using var follow=new HttpRequestMessage(HttpMethod.Get,target);
        DiagnosticHub.Emit(DiagnosticEvent.HttpRequest,DiagnosticArea.HTTP,DiagnosticOutcome.Started,endpoint:"Wallet",method:"GET",reason:"MarketCanonicalRedirect",count:1);
        var followed=await client.SendAsync(follow,HttpCompletionOption.ResponseHeadersRead,token);
        if((int)followed.StatusCode is >=300 and <400&&followed.Headers.Location is {} second)
        {
            try{RecordMarketRedirect(second.IsAbsoluteUri?second:new Uri(target,second),(int)followed.StatusCode,1);}catch(UriFormatException){/* existing fail-closed response processing handles this */}
        }
        return followed;
    }
    internal static bool NormalMarketTarget(Uri source,Uri target)
    {
        if(source==target||source.Host!="steamcommunity.com"||target.Scheme!="https"||target.Host!=source.Host||target.Port!=443||target.UserInfo.Length!=0||target.Fragment.Length!=0||target.AbsolutePath is not("/market/" or "/market"))return false;
        return target.Query is "" or "?l=english" or "?l=koreana";
    }
    internal static (string Host,string Path,string Query) DescribeMarketRedirect(Uri target)
    {
        var host=target.Scheme!="https"||target.Port!=443||target.UserInfo.Length!=0?"RedirectUnsafeAuthority":target.Host switch{"steamcommunity.com"=>"RedirectCommunity","store.steampowered.com"=>"RedirectStore","login.steampowered.com"=>"RedirectLoginHost",_=>"RedirectExternal"};
        var path=target.AbsolutePath switch{"/market/"=>"RedirectMarketSlash","/market"=>"RedirectMarketNoSlash","/"=>"RedirectRoot",var p when p=="/login"||p.StartsWith("/login/",StringComparison.Ordinal)=>"RedirectLoginPath",var p when p.StartsWith("/market/",StringComparison.Ordinal)=>"RedirectMarketOther",var p when p.StartsWith("/agecheck/",StringComparison.Ordinal)||p.StartsWith("/captcha",StringComparison.Ordinal)=>"RedirectSecurity",var p when p.StartsWith("/account",StringComparison.Ordinal)=>"RedirectAccount",_=>"RedirectOtherPath"};
        var query=target.Fragment.Length>0?"RedirectFragment":target.Query switch{""=>"RedirectNoQuery","?l=english" or "?l=koreana"=>"RedirectLanguageQuery",_=>"RedirectOtherQuery"};
        return(host,path,query);
    }
    private static void RecordMarketRedirect(Uri target,int status,int hop)
    {
        var facts=DescribeMarketRedirect(target);
        DiagnosticHub.Emit(DiagnosticEvent.ReadContract,DiagnosticArea.HTTP,DiagnosticOutcome.Completed,endpoint:"Wallet",status:status,from:facts.Host,to:facts.Query,reason:facts.Path,count:hop);
    }
}
