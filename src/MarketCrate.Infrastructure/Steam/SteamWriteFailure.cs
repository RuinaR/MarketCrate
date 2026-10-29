using System.Text;
using System.Text.Json;
using MarketCrate.Core;
namespace MarketCrate.Infrastructure.Steam;

public sealed partial class SteamHttp
{
    // Inspect only a bounded response in memory. Never log/return the server message or body.
    private async Task<SteamException> InspectWriteFailureAsync(HttpResponseMessage response,string endpoint,CancellationToken token)
    {
        var status=(int)response.StatusCode;var kind=SteamBodyKind.OtherBody;var failure=SteamFailure.Restricted;
        var reason="ErrorBodyUnverified";var state="Unverified";int? code=null;
        try
        {
            const int limit=16384;await using var stream=await response.Content.ReadAsStreamAsync(token);
            var bytes=new byte[limit+1];var count=0;
            while(count<bytes.Length){var read=await stream.ReadAsync(bytes.AsMemory(count),token);if(read==0)break;count+=read;}
            if(count>limit)reason="ErrorBodyTooLarge";
            else
            {
                var body=Encoding.UTF8.GetString(bytes,0,count);kind=SteamResponseContract.Inspect(body);
                string? media;try{media=response.Content.Headers.ContentType?.MediaType;}catch(FormatException){media="invalid";}
                SteamResponseContract.Record(endpoint,media,kind,status);
                if(kind==SteamBodyKind.HtmlChallenge||SteamResponseContract.RequiresChallenge(body)){failure=SteamFailure.Challenge;reason="ChallengeValidation";}
                else if(kind==SteamBodyKind.HtmlAccessDenied){failure=SteamFailure.Forbidden;reason="AccessRestricted";}
                else if(kind==SteamBodyKind.HtmlLogin){failure=SteamFailure.Authorization;reason="LoginRequired";}
                else if(kind==SteamBodyKind.JsonBody)
                {
                    using var json=JsonDocument.Parse(body,new JsonDocumentOptions{MaxDepth=16});var root=json.RootElement;
                    if(root.ValueKind==JsonValueKind.Object&&root.EnumerateObject().Select(x=>x.Name).Distinct(StringComparer.Ordinal).Count()==root.EnumerateObject().Count())
                    {
                        if(root.TryGetProperty("eresult",out var result)&&result.TryGetInt32(out var value)&&value is >=0 and <=200)code=value;
                        if(root.TryGetProperty("success",out var success))state=success.ValueKind==JsonValueKind.False?"ServerSuccessFalse":success.ValueKind==JsonValueKind.True?"ServerSuccessTrue":"Unverified";
                        reason=root.TryGetProperty("message",out var message)&&message.ValueKind==JsonValueKind.String?
                            message.GetString()!.Contains("session",StringComparison.OrdinalIgnoreCase)?"MessageMentionsSession":"ServerMessagePresent":"ServerMessageAbsent";
                    }
                }
            }
        }
        catch(Exception error)when(error is IOException or HttpRequestException or JsonException or InvalidOperationException){reason="ErrorBodyUnreadable";}
        DiagnosticHub.Emit(DiagnosticEvent.ReadContract,DiagnosticArea.HTTP,DiagnosticOutcome.Unknown,LogLevel.Warning,DiagnosticError.ClientRequest,endpoint:endpoint,status:status,serverCode:code,reason:reason,from:kind.ToString(),to:state);
        return new(failure,code){HttpStatus=status,BodyKind=kind,Diagnostic=failure==SteamFailure.Restricted?DiagnosticError.ClientRequest:SteamDiagnostics.Classify(failure)};
    }
}
