namespace MarketCrate.Core;

public static class MockCancellation
{
    public static IReadOnlyList<MockListing> Execute(IDemoDataStore store, IReadOnlyList<MockListing> approved)
    {
        var current = store.ReadListings().ToArray();
        if (approved.Count == 0 || approved.Select(x => x.ListingId).Distinct().Count() != approved.Count ||
           approved.Any(x => x.State != ListingState.Listed || !current.Contains(x)))
            throw new ArgumentException("판매 상태가 변경되었습니다. 취소 검토를 다시 여세요.");
        foreach (var listing in approved)
        {
            var index = Array.FindIndex(current, x => x.ListingId == listing.ListingId);
            current[index] = listing with { State = ListingState.CancelRequested };
            store.SaveListings(current); // Durable intent, never blindly replay CancelRequested after restart.
            current[index] = listing with { State = listing.ListingId == "demo-reject" ? ListingState.Failed : ListingState.Cancelled };
            store.SaveListings(current);
        }
        return current;
    }
    public static IReadOnlyList<MockListing> Recover(IDemoDataStore store)
    {
        var listings = store.ReadListings().Select(x => x.State == ListingState.CancelRequested ? x with { State = ListingState.Unknown } : x).ToArray();
        store.SaveListings(listings); return listings;
    }
}
