using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

internal static class SteamDiagnostics
{
    internal static DiagnosticError Classify(SteamFailure code)=>code switch
    {
        SteamFailure.Authentication or SteamFailure.Expired or SteamFailure.Denied=>DiagnosticError.Authentication,
        SteamFailure.Forbidden=>DiagnosticError.Forbidden, SteamFailure.RateLimited=>DiagnosticError.RateLimited,
        SteamFailure.Challenge=>DiagnosticError.Challenge, SteamFailure.ContractChanged=>DiagnosticError.Schema,
        SteamFailure.Transport=>DiagnosticError.Transport, _=>DiagnosticError.Validation
    };
    internal static void Failure(SteamException error,string? endpoint=null,long? elapsed=null)=>
        DiagnosticHub.Emit(DiagnosticEvent.HttpFailure,DiagnosticArea.HTTP,DiagnosticOutcome.Blocked,LogLevel.Error,
            Classify(error.Code),endpoint:endpoint,reason:error.Code.ToString(),exception:error,elapsed:elapsed);
}
