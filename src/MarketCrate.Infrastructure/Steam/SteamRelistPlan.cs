using System.Collections.Immutable;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;
namespace MarketCrate.Infrastructure.Steam;

// Cancellation and NEW asset approval are separate intents. Product equality is not return proof.
public sealed record SteamRelistPlan(string Key,string Account,DateTimeOffset CreatedAt,
    ImmutableArray<SteamListing> Listings,Guid? SubmissionBatch=null);

public sealed partial class SteamStateStore
{
    public void SaveRelistPlan(SteamRelistPlan plan)
    {
        ValidateRelistPlan(plan);
        var previous=Read<SteamRelistPlan>("relist",plan.Key);
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
