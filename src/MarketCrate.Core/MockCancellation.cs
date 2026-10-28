namespace MarketCrate.Core;

public static class MockCancellation
{
    public static IReadOnlyList<MockListing> Execute(IDemoDataStore store, IReadOnlyList<MockListing> approved)
    {
        using var operation=DiagnosticHub.Operation(DiagnosticEvent.CancelRequested,DiagnosticArea.Management,"B08");
        var current = store.ReadListings().ToList();
        if (approved.Count == 0 || approved.Select(x => x.ListingId).Distinct().Count() != approved.Count ||
           approved.Any(x => x.State != ListingState.Listed || !current.Contains(x)))
            throw new ArgumentException("판매 상태가 변경되었습니다. 취소 검토를 다시 여세요.");
        foreach (var listing in approved)
        {
            MockListingManagement.CancelOne(store, current, listing);
        }
        return current;
    }
    public static IReadOnlyList<MockListing> Recover(IDemoDataStore store)
    {
        var previous = store.ReadListings();
        var listings = previous.Select(x => x.State is ListingState.CancelRequested or ListingState.SubmitRequested ? x with { State = ListingState.Unknown } : x).ToArray();
        var changed = listings.Where((x, i) => x != previous[i]).Count();
        if (changed > 0) { store.SaveListings(listings); (store as IDiagnosticStore)?.RecordDiagnostic(DiagnosticCode.ManagementRecovered, changed); }
        if(changed>0)DiagnosticHub.Emit(DiagnosticEvent.StoreRecovery,DiagnosticArea.Management,DiagnosticOutcome.Unknown,to:"Unknown",count:changed,journal:"Committed");
        return listings;
    }
}
