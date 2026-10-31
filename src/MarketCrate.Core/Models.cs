using System.Collections.Immutable;

namespace MarketCrate.Core;

public enum Capability { Mock, PermittedReadOnly, Unavailable, IntegrationBlocked, Experimental }
public enum SaleState { Draft, Ready, Submitting, Submitted, AwaitingSteamGuard, Listed, Failed, Unknown, Reconciled, Sold }
public record AssetKey(int AppId, string ContextId, string AssetId);
public record InventoryAsset(AssetKey Key, string Game, string Name, string MarketHashName,
    int Quantity, bool Stackable, bool Marketable, bool Tradable, bool Locked = false);
public record BidLevel(long BuyerPays, int Quantity);
public record TradeSample(long BuyerPays, int Volume);
public record PriceSnapshot(string Currency, DateTimeOffset CapturedAt, string Source,
    TimeSpan Window, ImmutableArray<TradeSample> Trades, ImmutableArray<BidLevel> Bids)
{ public PriceEvidence? Estimate{get;init;} }
public record FeePolicy(int SteamBasisPoints = 500, int GameBasisPoints = 1000, long MinimumSteam = 1, long MinimumGame = 1);
public record FeeQuote(long BuyerPays, long SellerReceives, long SteamFee, long GameFee)
{
    public long Fees => checked(BuyerPays - SellerReceives);
}
public record Allocation(long BuyerPays, int Quantity);
public record AllocationResult(ImmutableArray<Allocation> Lines, int UnpricedQuantity);
public record SaleLineSnapshot(string LineId, InventoryAsset Asset, int Quantity, string Currency,
    FeeQuote UnitQuote, DateTimeOffset MarketTime)
{ public PriceEvidence? Evidence{get;init;} public string? OrderbookDigest{get;init;} }
public record SaleBatchSnapshot(Guid BatchId, string AccountReference, DateTimeOffset FinalizedAt,
    ImmutableArray<SaleLineSnapshot> Lines)
{
    public long Gross => Lines.Sum(x => checked(x.UnitQuote.BuyerPays * x.Quantity));
    public long Net => Lines.Sum(x => checked(x.UnitQuote.SellerReceives * x.Quantity));
}
public record LineResult(string LineId, SaleState State, string Message, int Attempt = 0);
public record BatchRecord(int SchemaVersion, SaleBatchSnapshot Snapshot, ImmutableArray<LineResult> Results);

public interface IInventoryProvider
{
    Capability Capability { get; }
    Task<IReadOnlyList<InventoryAsset>> GetAsync(CancellationToken cancellationToken);
}
public interface IMarketDataProvider
{
    Capability Capability { get; }
    PriceSnapshot Get(InventoryAsset asset);
}
public interface IMarketOrderProvider
{
    Capability Capability { get; }
    IReadOnlyList<LineResult> GetOrders();
}
// Only an executor that knows no submission was attempted may set this marker.
public interface ISubmissionFailure { bool RequestNotSent { get; } }
public interface IMarketListingExecutor
{
    Capability Capability { get; }
    Task<LineResult> SubmitAsync(SaleLineSnapshot line, string requestKey, CancellationToken cancellationToken);
    Task<LineResult> ReconcileAsync(SaleLineSnapshot line, string requestKey, CancellationToken cancellationToken);
}
// An experimental executor must revalidate current account, assets, currency and prices before any intent.
public interface IAuthorizedListingExecutor : IMarketListingExecutor
{
    bool CanExecute { get; }
    bool CanReconcile { get; }
    Task ValidateBatchAsync(SaleBatchSnapshot snapshot, CancellationToken token);
}
public interface IBatchHistoryJournal : IBatchJournal
{
    SemaphoreSlim ExecutionGate { get; }
    Task<BatchRecord?> FindBatchAsync(Guid batchId, CancellationToken cancellationToken);
}
public interface IBatchJournal
{
    Task<BatchRecord?> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(BatchRecord record, CancellationToken cancellationToken);
}

#if MARKETCRATE_DEMO
public interface IDemoJournal : IBatchJournal { void ClearDemo(); }
#endif
public enum ListingState { Listed, AwaitingSteamGuard, CancelRequested, Cancelled, Failed, Unknown, Submitted, SubmitRequested }
public enum ListingAction { None, Cancel, Reprice, Registration }
#if MARKETCRATE_DEMO
public record MockListing(string ListingId, string Name, int Quantity, long BuyerPays, ListingState State,
    ListingAction Action = ListingAction.None, long? PendingPrice = null, int Attempt = 0, string? ReplacementId = null);
#endif
public enum DiagnosticCode { StoreOpened, BatchSaved, RetryStarted, ListingsSaved, ManagementRecovered, SettingsSaved, UiFailure, StorageFailure }
#if MARKETCRATE_DEMO
public record DiagnosticEntry(DateTimeOffset At, DiagnosticCode Code, int Count);
public interface IDiagnosticStore { void RecordDiagnostic(DiagnosticCode code, int count = 0); IReadOnlyList<DiagnosticEntry> ReadDiagnostics(); }
public interface IDemoDataStore
{
    string? ReadSetting(string key);
    void WriteSetting(string key, string value);
    void SaveInventory(IReadOnlyList<InventoryAsset> assets);
    IReadOnlyList<InventoryAsset> ReadInventory();
    IReadOnlyList<MockListing> ReadListings();
    void SaveListings(IReadOnlyList<MockListing> listings);
    LineResult? ReadEvidence(string requestKey);
    void WriteEvidence(string requestKey, LineResult result);
    IReadOnlyList<BatchRecord> ReadBatches();
}
#endif
