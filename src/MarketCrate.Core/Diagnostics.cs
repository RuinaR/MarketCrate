using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace MarketCrate.Core;

public enum LogLevel { Trace, Debug, Information, Warning, Error, Critical }
public enum DiagnosticArea { Lifecycle, Authentication, Data, Pricing, Sales, Management, Storage, UI, Diagnostics, HTTP }
public enum DiagnosticOutcome { Started, Completed, Succeeded, Failed, Cancelled, Blocked, Unknown, Pending, Reconciled, CacheHit, CacheExpired }
public enum DiagnosticError { None, Authentication, Forbidden, RateLimited, Challenge, ClientRequest, Server, Timeout, Transport, Parsing, Schema, Currency, Validation, Storage, Locked, Corrupt, Disk, Logging, Binding, Unhandled, Cancelled, EvidenceMissing, Authorization, Unsupported }
public enum DiagnosticEvent
{
    AppStart=1000, AppStop=1001, AppFault=1002, Settings=1003, ResourceCleanup=1004,
    Login=2000, QrReady=2001, LoginCheck=2002, Logout=2003, AuthState=2004, AuthContract=2005, SessionCredential=2006,
    HttpRequest=3000, HttpResponse=3001, HttpFailure=3002, ResponseParsed=3003, ResponseBody=3004, ResponseContract=3005,
    InventoryPage=4000, InventoryLoaded=4001, ListingsLoaded=4002, PriceLoaded=4003, HistoryLoaded=4004, CacheRead=4005, ImageMetadata=4100, ImageLoad=4101, GameCatalog=4200, InventoryScope=4201, ReadContract=4202, ReadState=4203, ReadSelfCheck=4204, WalletContract=4205, InventoryContract=4206, PriceIndicator=5100,
    PricePlan=5000, PriceEdited=5001, FeeValidated=5002, ReviewOpened=5003, FinalValidation=5004,
    BatchCreated=6000, ItemReserved=6001, ItemResult=6002, BatchFinished=6003, BatchRecovered=6004, Retry=6005,
    CancelRequested=7000, CancelResult=7001, EvidenceChecked=7002, Reprice=7003,
    StoreOpen=8000, Migration=8001, Transaction=8002, JournalWrite=8003, StoreRecovery=8004,
    UiCommand=9000, UiFailure=9001, UiPage=9002, UiBinding=9003, UiSelection=9004, Modal=9005,
    DiagnosticMode=10000, Export=10001, Retention=10002
}
public sealed record DiagnosticContext(Guid OperationId, Guid? CorrelationId=null, Guid? BatchId=null, Guid? OperationItemId=null);
public sealed record SafeException(string Type, string? Location, string[] Frames, SafeException[] Inner);
public sealed record DiagnosticRecord(int SchemaVersion, DateTimeOffset Utc, LogLevel Level, int EventId, string EventName,
    string RequirementId, DiagnosticArea Area, DiagnosticOutcome Outcome, Guid SessionId, DiagnosticContext Context,
    string AppVersion, string BuildVersion, string Commit, DateTimeOffset? StartedAt=null, long? DurationMs=null,
    DiagnosticError Error=DiagnosticError.None, string? Endpoint=null, string? Method=null, int? HttpStatus=null,
    int? ServerCode=null, string? PreviousState=null, string? NewState=null, string? Reason=null, int? Count=null,
    bool Retry=false, string? JournalState=null, SafeException? Exception=null,string? Currency=null,FeeQuote? Amounts=null);

public interface IDiagnosticLog
{
    Guid SessionId { get; }
    void Write(DiagnosticRecord record);
}

// Typed whitelist: never accepts raw URL/query/header/body/exception message/account/asset strings.
public static class DiagnosticHub
{
    private sealed record Ambient(IDiagnosticLog? Log, DiagnosticContext? Context);
    private static readonly AsyncLocal<Ambient?> ambient = new();
    private static IDiagnosticLog? defaultLog;
    public static IDiagnosticLog? Current => ambient.Value?.Log ?? Volatile.Read(ref defaultLog);
    public static DiagnosticContext? Context => ambient.Value?.Context;
    public static void SetDefault(IDiagnosticLog? log) => Volatile.Write(ref defaultLog,log);
    public static IDisposable Use(IDiagnosticLog log)
    { var previous=ambient.Value;ambient.Value=new(log,previous?.Context);return new Restore(()=>ambient.Value=previous); }
    public static Guid InternalId(string value)
    { var hash=SHA256.HashData(Encoding.UTF8.GetBytes("MarketCrate.InternalCommand:"+value));return new Guid(hash.AsSpan(0,16)); }
    public static IDisposable Scope(Guid? batch=null,string? item=null,bool correlation=false,Guid? operation=null)
    {
        var previous=ambient.Value;var context=previous?.Context;
        ambient.Value=new(Current,new(operation??context?.OperationId??Guid.NewGuid(),correlation?Guid.NewGuid():context?.CorrelationId,batch??context?.BatchId,item is null?context?.OperationItemId:InternalId(item)));
        return new Restore(()=>ambient.Value=previous);
    }
    public static IDisposable Operation(DiagnosticEvent evt, DiagnosticArea area, string requirement, [System.Runtime.CompilerServices.CallerMemberName] string stage="")
    { return new OperationScope(evt,area,requirement,stage); }
    public static void Emit(DiagnosticEvent evt,DiagnosticArea area,DiagnosticOutcome outcome=DiagnosticOutcome.Succeeded,
        LogLevel level=LogLevel.Information,DiagnosticError error=DiagnosticError.None,string requirement="SYS-01",
        string? endpoint=null,string? method=null,int? status=null,int? serverCode=null,string? from=null,string? to=null,
        string? reason=null,int? count=null,string? journal=null,Exception? exception=null,DateTimeOffset? started=null,long? elapsed=null,bool retry=false,string? currency=null,FeeQuote? quote=null)
    {
        try
        {
            var log=Current;if(log is null)return;
            var assembly=typeof(DiagnosticHub).Assembly;var version=assembly.GetName().Version?.ToString()??"unknown";
            var build=assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion??version;
            var commit=assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(x=>x.Key=="GitCommit")?.Value??"unavailable";
            log.Write(new(1,DateTimeOffset.UtcNow,level,(int)evt,evt.ToString(),System.Text.RegularExpressions.Regex.IsMatch(requirement,@"^(?:[A-G][0-9]{2}|LOG-[0-9]{2}|SYS-[0-9]{2})$")?requirement:"SYS-01",area,outcome,log.SessionId,Context??new(Guid.NewGuid()),version,build,commit.Length==40&&commit.All(Uri.IsHexDigit)?commit:"unavailable",started,elapsed,error,
                Token(endpoint),method is "GET" or "POST"?method:null,status is >=100 and <=599?status:null,serverCode,
                Token(from),Token(to),Token(reason),count,retry,Token(journal),exception is null?null:Sanitize(exception),currency is "KRW" or "USD"?currency:null,quote));
        }
        catch { /* Diagnostics must never change transaction/retry behavior. */ }
    }
    public static string? SafeCode(string? value) => value is not null && value.Length is >0 and <=96 && value.All(c=>char.IsAsciiLetterOrDigit(c)||c is '.' or '_' or '-' or '+') ? value : null;
    private static readonly HashSet<string> tokens=new(
        Enum.GetNames<SaleState>().Concat(Enum.GetNames<ListingState>()).Concat(Enum.GetNames<DiagnosticError>()).Concat(Enum.GetNames<DiagnosticOutcome>()).Concat(Enum.GetNames<InventoryScopeState>()).Concat([
        "AccountValidation","CredentialLoad","CredentialLoadFailed","CredentialSaveFailed","CredentialDeleteFailed","CredentialPauseFailed","AuthRestore","TokenValidation","SessionRestored","CredentialSaved","CredentialDeleted","CredentialUnavailable","MediaJson","MediaHtml","MediaText","MediaMissing","MediaOther","JsonBody","EmptyBody","OtherBody","HtmlMarket","HtmlLogin","HtmlChallenge","HtmlAccessDenied","HtmlUnknown","MediaTypeMismatch","ResponseShape","OfficialFeeComparison","WalletLegacyComplete","WalletLegacyPartial","WalletLegacyInvalid","LoginRequired","ModeMissing","ModeAmbiguous","ModeInvalid","WalletChanged","PricingMetadataMissing","ExplicitNewMode","ReactPropsMissing","ClassicInventoryContext","FeeUiObserved","ClassicUiSelected","ReactUiSelected","LegacyIncomplete","LegacyFormulaConflict","AppContextMissing","PublisherMissing","WalletAndAppContext","VerifiedLegacy","VerifiedNew","Unverified","Restricted","FeeContext","FeeContextVanityRedirect","FeeContextRouteReused","FeeContextCanonicalRedirect","FeeContextOtherProfile","FeeContextOtherRedirect","OwnerPageMissing",
        "EligibilityIncomplete","MarketEligibilityRedirect","MarketEligibilityReturn","RedirectEligibilityPath","LiveSession","RedirectUnsafeAuthority","RedirectCommunity","RedirectStore","RedirectLoginHost","RedirectExternal","RedirectMarketSlash","RedirectMarketNoSlash","RedirectRoot","RedirectLoginPath","RedirectMarketOther","RedirectSecurity","RedirectAccount","RedirectOtherPath","RedirectFragment","RedirectNoQuery","RedirectLanguageQuery","RedirectOtherQuery","MarketCanonicalRedirect","TradingReadiness","PrepareTradingAsync","OpenTradingReviewAsync","TradingCheck","Consent","WalletIdentity","FeeContract","WalletConsistency","InventoryRead","SaleableAssets","PublisherFees","PublisherFromAppContract","ErrorBodyUnverified","MobileConfirmationRequired","EmailConfirmationRequired","EitherConfirmationRequired","ConfirmationChannelUnknown","NoConfirmationFlag","ErrorBodyTooLarge","ErrorBodyUnreadable","ServerSuccessFalse","ServerSuccessTrue","MessageMentionsSession","ServerMessagePresent","ServerMessageAbsent","ListingsRead","ListingFees","SessionCheck","PrepareCancellationAsync","CancellationEnabled","TradingEnabled","TradingRevoked","AccountMismatch","OrderbookChanged","MissingEvidence","LoginRedirect","OtherRedirect","UnsafeRedirect","WalletMissing","WalletNull","WalletObject","WalletCurrencyMissing","WalletMinimumMissing","WalletIncrementMissing","LoggedIn","LoggedOut","InventoryReservationOpened","InventoryReservedRows","InventoryLocalPendingRows","ServerInventorySync","ServerSaleableAssets","LocalSubmissionPending","ConcurrentAssetOperation","RequestNotSent","ConfirmationFlagsConflict","ListingMetadataUnavailable","FreshOwnedAssetNewApproval","UnknownPreserved","PreservedWithoutReplay","PendingSubmissionSettling","ServerInventory","ServerListing","UncertainStack","InventoryUnmarketable","InventoryLocked","StackQuantityUnsupported","AssetStateObserved","StackContract","PendingSubmission","InventorySelectableRows","InventoryReportedTotal","InventoryDescriptions","InventoryEmptyVerified","InventoryContradiction","InventoryPagePartial","InventoryPage","AccessRestricted","ContractMismatch","EmptyUnproven","SelfCheck","SessionCheck","Marketable","Succeeded","Unverified","Running","NotQueried","Loading","Ready","AccessBlocked","Stale","HTTP","ApiResult","RequestEncoding","ResponseParse","ResponseFields","ChallengeValidation","Synthetic","OfficialMarket","ShowCurrentListingsAsync","LoadListingsAsync","LoadFeesAsync","LoadWalletAsync","PrepareListingRepriceAsync","RepriceReturn","CancelForNewReviewAsync","ReviewCancelledTargetsAsync","RefreshSelectedRequestAsync","NewAssetReview","Prepared","SubmissionStarted","IsolatedQa","FocusDeferred","FocusRejected","ImageInvalid","ImageMissing","ImageReady","ImageDisabled","ImageFailed","AssetsRoot","AssetsApp","AssetsContext","JsonArray","JsonNull","JsonOther","AssetsMapEmptyArray","AssetsMapInvalidShape","MarketHistory","HistoryMultipleCandidates","HistoryNoSaleCandidate","HistoryAssetMismatch","HistoryQuantityMismatch","HistoryCurrencyMismatch","HistoryAmountMismatch","HistoryTimeMismatch","HistoryQuoteConflict","HistorySaleMatched","HistoryAssetSaleObserved","ListingNoLongerPresent","RepriceSelectionChanged","LowerValidStep","MedianEstimate","ExactAverage","Immediate","PrivateOrDenied","Partial","Unsupported","Empty","Loaded","Manual","Observed","HistoryChanged","AuthBegin","AuthPoll","AuthFinalize","AuthTransfer","Wallet","Fees","Inventory","Orderbook","History","Listings","Sell","Cancel","Image",
        "BeginAsync","CheckAsync","LoadAsync","RefreshPricesAsync","SubmitAsync","RecoverAsync","InspectAsync","HistoryAsync","ManageAsync","ResumeRepriceAsync","ReviseRepriceAsync","Run","Operation",
        "Disconnected","RequestingQr","AwaitingMobile","Connected","Expired","Denied","NetworkFailure","AccessBlocked","ContractFailure","StaleReview",
        "AwaitingCancellation","AwaitingReturn","Superseded","CandidateOnly","MultipleCandidates","NoCandidate","FieldsMismatch","TimeMissingOrOutside","ExistingBeforeRequest","ServerIdAndFields",
        "IntentDurable","ResponseLost","NoCausalEvidence","RetryForbidden","SchemaChanged","JournalPreserved","LeaseHeld","MigrationRequired","Unsupported","Readonly","Demo","Steam","UserRequested","InputRejected","NotPersisted","RecoverOnly","Committed","RolledBack","Opened","Closed","SourceChanged","SelectionRestored","Cleanup","Selected","Cleared","ExportReviewed","NoRawData","Unchanged","DiskFull","QueueFull","WriteFailure","Rotate","Search","Filter","Sort","Average","Immediate","InsufficientDepth","Fresh","Stale","PendingLong","QueueWait","ConnectOrHeaders","Body","Application","MainView","SteamWorkspaceView","DiagnosticView"
        ]),StringComparer.Ordinal);
    private static string? Token(string? value)=>value is not null&&tokens.Contains(value)?value:null;
    public static string? AllowedToken(string? value)=>Token(value);
    public static SafeException Sanitize(Exception exception,int depth=0)
    {
        // No Message/Data/ToString/raw StackTrace. Only first-party method symbols and numeric IL offsets.
        var type=exception.GetType();var trusted=type.Assembly==typeof(Exception).Assembly||type.Assembly==typeof(DiagnosticHub).Assembly||type.Assembly.GetName().Name?.StartsWith("MarketCrate.",StringComparison.Ordinal)==true||type.Namespace=="Microsoft.Data.Sqlite"||type.Namespace=="System.Text.Json"||type==typeof(HttpRequestException)||type==typeof(CryptographicException);
        var frames=new StackTrace(exception,false).GetFrames()?.Where(f=>f.GetMethod()?.DeclaringType?.Assembly.GetName().Name?.StartsWith("MarketCrate.",StringComparison.Ordinal)==true).Take(8)
            .Select(f=>SafeSymbol(f.GetMethod()!)+"@IL"+Math.Max(0,f.GetILOffset())).ToArray()??[];
        var inner=depth<3?(exception is AggregateException aggregate?aggregate.InnerExceptions.Take(2):exception.InnerException is {} i?[i]:Array.Empty<Exception>()).Select(e=>Sanitize(e,depth+1)).ToArray():[];
        return new(trusted?type.FullName??type.Name:"ExternalException",frames.FirstOrDefault(),frames,inner);
    }
    private static string SafeSymbol(MethodBase method)
    { var name=(method.DeclaringType?.FullName??"Unknown")+"."+method.Name;return new string(name.Take(220).Where(c=>char.IsAsciiLetterOrDigit(c)||"._+<>`".Contains(c)).ToArray()); }
    private sealed class Restore(Action restore):IDisposable {public void Dispose()=>restore();}
    private sealed class OperationScope:IDisposable
    {
        private readonly IDisposable scope;private readonly DiagnosticEvent evt;private readonly DiagnosticArea area;private readonly string requirement,stage;private readonly DateTimeOffset start=DateTimeOffset.UtcNow;private readonly Stopwatch watch=Stopwatch.StartNew();
        public OperationScope(DiagnosticEvent evt,DiagnosticArea area,string requirement,string stage)
        {this.evt=evt;this.area=area;this.requirement=requirement;this.stage=evt==DiagnosticEvent.Retry?"UserRequested":SafeCode(stage)??"Operation";scope=Scope();Emit(evt,area,DiagnosticOutcome.Started,requirement:requirement,reason:this.stage,retry:evt==DiagnosticEvent.Retry);}
        public void Dispose(){Emit(evt,area,DiagnosticOutcome.Completed,requirement:requirement,reason:stage,started:start,elapsed:watch.ElapsedMilliseconds,retry:evt==DiagnosticEvent.Retry);scope.Dispose();}
    }
}
