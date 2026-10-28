using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

public enum SteamEvidenceReason { ServerIdAndFields, ExistingBeforeRequest, FieldsMismatch, TimeMissingOrOutside, CandidateOnly, MultipleCandidates, NoCandidate }
public sealed record SteamEvidenceCandidate(string ListingId, SteamEvidenceReason Reason, int Quantity, long SellerReceives, DateTimeOffset? CreatedAt);
public sealed record SteamEvidenceReport(string RequestKey, DateTimeOffset CheckedAt, SteamEvidenceReason Verdict, SteamEvidenceCandidate[] Candidates);
public sealed record SteamReturnProof(string CancelKey, string? SellKey, AssetKey ReturnedAsset, int Quantity, DateTimeOffset At);
public enum SteamRepriceState { AwaitingCancellation, AwaitingReturn, Submitting, Submitted, Unknown }
public sealed record SteamRepriceIntent(string Key, string Account, SteamListing Listing, long BuyerPrice, DateTimeOffset CreatedAt, SteamRepriceState State, string? ReplacementKey = null);
public enum SteamAuditAction { Sell, Cancel, Reprice, Archive, Return }
public sealed record SteamAuditEvent(string RequestKey, DateTimeOffset At, SteamAuditAction Action, string State);
