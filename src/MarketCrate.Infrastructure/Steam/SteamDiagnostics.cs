using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

public static class SteamDiagnostics
{
    public static DiagnosticError Classify(SteamException error)=>error.Diagnostic??Classify(error.Code);
    public static DiagnosticError Classify(SteamFailure code)=>code switch
    {
        SteamFailure.Authentication or SteamFailure.Expired or SteamFailure.Denied=>DiagnosticError.Authentication,
        SteamFailure.Forbidden=>DiagnosticError.Forbidden, SteamFailure.RateLimited=>DiagnosticError.RateLimited,
        SteamFailure.Authorization=>DiagnosticError.Authorization,SteamFailure.Unsupported or SteamFailure.WalletUnavailable=>DiagnosticError.Unsupported,SteamFailure.Challenge=>DiagnosticError.Challenge, SteamFailure.ContractChanged=>DiagnosticError.Schema,
        SteamFailure.Transport=>DiagnosticError.Transport, _=>DiagnosticError.Validation
    };
    internal static void Failure(SteamException error,string? endpoint=null,long? elapsed=null)=>
        DiagnosticHub.Emit(DiagnosticEvent.HttpFailure,DiagnosticArea.HTTP,DiagnosticOutcome.Blocked,LogLevel.Error,
            Classify(error),endpoint:endpoint,reason:error.BodyKind?.ToString()??error.Code.ToString(),exception:error,elapsed:elapsed,serverCode:error.ApiResult);
}
