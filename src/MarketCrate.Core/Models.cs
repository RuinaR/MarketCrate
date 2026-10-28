using System.Collections.Immutable;

namespace MarketCrate.Core;

public enum Capability { Mock, PermittedReadOnly, Unavailable, IntegrationBlocked }
public enum SaleState { Draft, Ready, Submitting, Submitted, AwaitingSteamGuard, Listed, Failed, Unknown, Reconciled }
public record AssetKey(int AppId, string ContextId, string AssetId);
public record InventoryAsset(AssetKey Key, string Game, string Name, string MarketHashName,
    int Quantity, bool Stackable, bool Marketable, bool Tradable, bool Locked = false);
public record BidLevel(long BuyerPays, int Quantity);
public record TradeSample(long BuyerPays, int Volume);
public record PriceSnapshot(string Currency, DateTimeOffset CapturedAt, string Source,
    TimeSpan Window, ImmutableArray<TradeSample> Trades, ImmutableArray<BidLevel> Bids);
public record FeePolicy(int SteamBasisPoints = 500, int GameBasisPoints = 1000, long MinimumSteam = 1, long MinimumGame = 1);
public record FeeQuote(long BuyerPays, long SellerReceives, long SteamFee, long GameFee)
{
    public long Fees => checked(BuyerPays - SellerReceives);
}
public record Allocation(long BuyerPays, int Quantity);
public record AllocationResult(ImmutableArray<Allocation> Lines, int UnpricedQuantity);
public record SaleLineSnapshot(string LineId, InventoryAsset Asset, int Quantity, string Currency,
    FeeQuote UnitQuote, DateTimeOffset MarketTime);
public record SaleBatchSnapshot(Guid BatchId, string AccountReference, DateTimeOffset FinalizedAt,
    ImmutableArray<SaleLineSnapshot> Lines)
{
    public long Gross => Lines.Sum(x => checked(x.UnitQuote.BuyerPays * x.Quantity));
    public long Net => Lines.Sum(x => checked(x.UnitQuote.SellerReceives * x.Quantity));
}
public record LineResult(string LineId, SaleState State, string Message);
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
public interface IMarketListingExecutor
{
    Capability Capability { get; }
    Task<LineResult> SubmitAsync(SaleLineSnapshot line, string requestKey, CancellationToken cancellationToken);
    Task<LineResult> ReconcileAsync(SaleLineSnapshot line, string requestKey, CancellationToken cancellationToken);
}
public interface IBatchJournal
{
    Task<BatchRecord?> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(BatchRecord record, CancellationToken cancellationToken);
}

public interface IDemoJournal : IBatchJournal { void ClearDemo(); }
public enum ListingState { Listed, AwaitingSteamGuard, CancelRequested, Cancelled, Failed, Unknown, Submitted }
public record MockListing(string ListingId, string Name, int Quantity, long BuyerPays, ListingState State);
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
