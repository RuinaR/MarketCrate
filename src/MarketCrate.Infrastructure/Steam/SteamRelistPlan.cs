using System.Collections.Immutable;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;
namespace MarketCrate.Infrastructure.Steam;

// Cancellation and NEW asset approval are separate intents. Product equality is not return proof.
public sealed record SteamRelistPlan(string Key,string Account,DateTimeOffset CreatedAt,
    ImmutableArray<SteamListing> Listings,Guid? SubmissionBatch=null);

public sealed partial class SteamStateStore
{
    private (string Key,SaleBatchSnapshot Snapshot)? relistApproval;
    // Memory-only until the coordinator's first durable batch write. A rejected preflight consumes no plan.
    public void StageRelistApproval(string key,SaleBatchSnapshot snapshot)
    {
        var plan=Read<SteamRelistPlan>("relist",key)??throw new InvalidDataException("RelistPlanMissing");
        ValidateRelistPlan(plan);BatchValidation.Validate(snapshot,DateTimeOffset.UtcNow);
        var expected=plan.Listings.GroupBy(x=>(x.Asset.AppId,x.Asset.ContextId,x.HashName)).ToDictionary(x=>x.Key,x=>x.Sum(r=>r.Quantity));
        var actual=snapshot.Lines.GroupBy(x=>(x.Asset.Key.AppId,x.Asset.Key.ContextId,x.Asset.MarketHashName)).ToDictionary(x=>x.Key,x=>x.Sum(r=>r.Quantity));
        if(plan.SubmissionBatch is not null||plan.Account!=snapshot.AccountReference||expected.Count!=actual.Count||expected.Any(x=>!actual.TryGetValue(x.Key,out var count)||count!=x.Value)||snapshot.Lines.Any(x=>x.Asset.Stackable||x.Quantity!=1))throw new InvalidDataException("RelistApprovalMismatch");
        if(plan.Listings.Any(x=>ReadCommand(plan.Key+":"+x.ListingId) is not {Kind:"cancel",Accepted:true} command||command.Account!=plan.Account))throw new InvalidDataException("RelistCancellationUnconfirmed");
        relistApproval=(key,snapshot);
    }
    public void ClearRelistApproval(Guid batchId){if(relistApproval?.Snapshot.BatchId==batchId)relistApproval=null;}
    public void SaveRelistPlan(SteamRelistPlan plan)
    {
        ValidateRelistPlan(plan);
        var previous=Read<SteamRelistPlan>("relist",plan.Key);
        if(plan.SubmissionBatch is not null&&previous?.SubmissionBatch!=plan.SubmissionBatch)throw new InvalidDataException("RelistBindingRequiresBatchJournal");
        if(previous is not null&&(previous.Account!=plan.Account||previous.CreatedAt!=plan.CreatedAt||!previous.Listings.SequenceEqual(plan.Listings)||
           previous.SubmissionBatch is not null&&previous.SubmissionBatch!=plan.SubmissionBatch))throw new InvalidDataException("RelistPlanImmutable");
        Save("relist",plan.Key,plan);
        DiagnosticHub.Emit(DiagnosticEvent.Reprice,DiagnosticArea.Storage,count:plan.Listings.Length,journal:"Committed",requirement:"B10",reason:plan.SubmissionBatch is null?"Prepared":"SubmissionStarted");
    }
    public IReadOnlyList<SteamRelistPlan> RelistPlans(string account)
    {
        using var command=connection.CreateCommand();command.CommandText="SELECT value FROM state WHERE kind='relist'";
        using var reader=StorageDiagnostics.Reader(command);var result=new List<SteamRelistPlan>();
        while(reader.Read()){var plan=System.Text.Json.JsonSerializer.Deserialize<SteamRelistPlan>(reader.GetString(0))??throw new InvalidDataException("RelistPlanInvalid");ValidateRelistPlan(plan);if(plan.Account==account)result.Add(plan);}
        return result;
    }
    private static void ValidateRelistPlan(SteamRelistPlan plan)
    {
        if(!Guid.TryParseExact(plan.Key,"N",out _)||plan.Account is null||plan.Account.Length!=17||plan.Account.Any(c=>!char.IsAsciiDigit(c))||
           plan.Listings.IsDefaultOrEmpty||plan.Listings.Length>100||plan.Listings.Any(x=>x is null||x.Quantity!=1||x.State!=SaleState.Listed||string.IsNullOrEmpty(x.ListingId)||string.IsNullOrEmpty(x.HashName)||x.Asset.AppId<1)||
           plan.Listings.Select(x=>x.ListingId).Distinct().Count()!=plan.Listings.Length||plan.Listings.Select(x=>(x.Asset.AppId,x.HashName)).Distinct().Count()>10)throw new InvalidDataException("RelistPlanInvalid");
    }
}
