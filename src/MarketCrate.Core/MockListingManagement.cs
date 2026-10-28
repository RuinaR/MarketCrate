namespace MarketCrate.Core;

// A mock cancel-then-relist saga. Never overwrite a live listing's price in place.
public static class MockListingManagement
{
    public static IReadOnlyList<MockListing> Reprice(IDemoDataStore store, IReadOnlyList<MockListing> approved, IReadOnlyList<long> prices)
    {
        var current = store.ReadListings().ToList();
        if (approved.Count == 0 || prices.Count != approved.Count || approved.Select(x => x.ListingId).Distinct().Count() != approved.Count ||
            approved.Any(x => x.State != ListingState.Listed || !current.Contains(x)) || prices.Any(x => x is < 3 or > 1_000_000_000))
            throw new ArgumentException("가격/판매 상태 변경: 가격 변경 검토를 갱신하세요.");
        for (var i = 0; i < approved.Count; i++) RepriceOne(store, current, approved[i], prices[i]);
        return current;
    }
    public static IReadOnlyList<MockListing> RetryFailed(IDemoDataStore store, IReadOnlyList<MockListing> approved)
    {
        var current = store.ReadListings().ToList();
        if (approved.Count == 0 || approved.Select(x => x.ListingId).Distinct().Count() != approved.Count || approved.Any(x =>
            (!CanResume(current, x) && (x.State != ListingState.Failed || x.Action is not (ListingAction.Cancel or ListingAction.Reprice))) || !current.Contains(x)))
            throw new ArgumentException("확정 실패 작업만 재시도할 수 있습니다. 불명 결과는 먼저 대조하세요.");
        foreach (var listing in approved)
        {
            if (CanResume(current, listing)) SubmitReplacement(store, current, listing);
            else if (listing.Action == ListingAction.Reprice && listing.PendingPrice is long price) RepriceOne(store, current, listing, price);
            else if (listing.Action == ListingAction.Cancel) CancelOne(store, current, listing);
            else throw new ArgumentException("재시도 근거 부족");
        }
        return current;
    }
    public static void CancelOne(IDemoDataStore store, List<MockListing> current, MockListing listing)
    {
        var index = current.FindIndex(x => x.ListingId == listing.ListingId);
        current[index] = listing with { State = ListingState.CancelRequested, Action = ListingAction.Cancel, Attempt = checked(listing.Attempt + 1) };
        store.SaveListings(current);
        current[index] = current[index] with { State = Reject(listing) ? ListingState.Failed : ListingState.Cancelled };
        store.SaveListings(current);
    }
    private static bool Reject(MockListing listing) => listing.ListingId == "demo-reject" && listing.Attempt == 0;
    public static bool CanResume(IReadOnlyList<MockListing> current, MockListing listing) => listing.State == ListingState.Cancelled && listing.Action == ListingAction.Reprice &&
        listing.PendingPrice is >= 3 and <= 1_000_000_000 && listing.ReplacementId != null && !current.Any(x => x.ListingId == listing.ReplacementId);
    private static void SubmitReplacement(IDemoDataStore store, List<MockListing> current, MockListing original)
    {
        current.Add(new(original.ReplacementId!, original.Name, original.Quantity, original.PendingPrice!.Value, ListingState.SubmitRequested, ListingAction.Reprice, Attempt: original.Attempt));
        store.SaveListings(current);
        current[^1] = current[^1] with { State = ListingState.AwaitingSteamGuard }; store.SaveListings(current);
    }
    private static void RepriceOne(IDemoDataStore store, List<MockListing> current, MockListing listing, long price)
    {
        var index = current.FindIndex(x => x.ListingId == listing.ListingId); var attempt = checked(listing.Attempt + 1);
        var replacement = $"{listing.ListingId}:reprice:{attempt}";
        current[index] = listing with { State = ListingState.CancelRequested, Action = ListingAction.Reprice, PendingPrice = price, Attempt = attempt, ReplacementId = replacement };
        store.SaveListings(current);
        if (Reject(listing)) { current[index] = current[index] with { State = ListingState.Failed }; store.SaveListings(current); return; }
        current[index] = current[index] with { State = ListingState.Cancelled }; store.SaveListings(current);
        SubmitReplacement(store, current, current[index]); // Crash after intent -> Unknown, never blindly resubmit.
    }
    public static IReadOnlySet<string> ReleasedRequests(IReadOnlyList<MockListing> listings)
    {
        var byId = listings.ToDictionary(x => x.ListingId); var released = new HashSet<string>();
        foreach (var listing in listings)
        {
            var visited = new HashSet<string>(); var cursor = listing;
            while (cursor.State == ListingState.Cancelled && visited.Add(cursor.ListingId))
            {
                if (cursor.ReplacementId == null) { released.Add(listing.ListingId); break; }
                if (!byId.TryGetValue(cursor.ReplacementId, out var next)) break;
                cursor = next;
            }
        }
        return released;
    }
}
