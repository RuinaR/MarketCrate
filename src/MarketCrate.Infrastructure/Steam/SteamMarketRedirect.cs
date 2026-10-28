using MarketCrate.Core;
namespace MarketCrate.Infrastructure.Steam;

public sealed partial class SteamHttp
{
    // One same-origin canonical/locale GET only. No POST, login, security, region or arbitrary query redirects.
    private async Task<HttpResponseMessage> SendWalletHeadersAsync(HttpRequestMessage request,CancellationToken token,bool wallet)
    {
        var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
        if(!wallet||request.Method!=HttpMethod.Get||request.RequestUri!.AbsolutePath!="/market/"||response.Headers.Location is not {} location||
            (int)response.StatusCode is not(301 or 302 or 303 or 307 or 308)||response.Headers.Contains("x-eresult"))return response;
        Uri target;
        try{target=location.IsAbsoluteUri?location:new Uri(request.RequestUri!,location);}catch(UriFormatException){return response;}
        if(!NormalMarketTarget(request.RequestUri!,target))return response;
        DiagnosticHub.Emit(DiagnosticEvent.HttpResponse,DiagnosticArea.HTTP,DiagnosticOutcome.Completed,endpoint:"Wallet",method:"GET",status:(int)response.StatusCode,reason:"MarketCanonicalRedirect");
        response.Dispose();
        var wait=last+gap-DateTimeOffset.UtcNow;if(wait>TimeSpan.Zero)await Task.Delay(wait,token);
        if(Halted is {} halted)throw new SteamException(halted);
        if(!Access.ResearchRiskAccepted)throw new SteamException(SteamFailure.Disabled);
        last=DateTimeOffset.UtcNow;
        using var follow=new HttpRequestMessage(HttpMethod.Get,target);
        DiagnosticHub.Emit(DiagnosticEvent.HttpRequest,DiagnosticArea.HTTP,DiagnosticOutcome.Started,endpoint:"Wallet",method:"GET",reason:"MarketCanonicalRedirect",count:1);
        return await client.SendAsync(follow,HttpCompletionOption.ResponseHeadersRead,token);
    }
    internal static bool NormalMarketTarget(Uri source,Uri target)
    {
        if(source==target||source.Host!="steamcommunity.com"||target.Scheme!="https"||target.Host!=source.Host||target.Port!=443||target.UserInfo.Length!=0||target.Fragment.Length!=0||target.AbsolutePath!="/market/")return false;
        return target.Query is "" or "?l=english" or "?l=koreana";
    }
}
