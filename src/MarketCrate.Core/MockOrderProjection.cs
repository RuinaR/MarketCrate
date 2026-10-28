namespace MarketCrate.Core;

public static class MockOrderProjection
{
    public static void Update(IDemoDataStore store, BatchRecord batch)
    {
        var listings = store.ReadListings().ToList();
        for (var i = 0; i < batch.Results.Length; i++)
        {
            var result = batch.Results[i];
            if (result.State == SaleState.Ready) continue;
            var line = batch.Snapshot.Lines[i]; var key = SaleBatchCoordinator.RequestKey(batch.Snapshot, line);
            var state = result.State switch { SaleState.Listed => ListingState.Listed, SaleState.AwaitingSteamGuard => ListingState.AwaitingSteamGuard, SaleState.Submitted => ListingState.Submitted, _ => ListingState.Unknown };
            var index = listings.FindIndex(x => x.ListingId == key);
            if (result.State == SaleState.Failed)
            {
                if (index >= 0 && listings[index].Action is ListingAction.None or ListingAction.Registration && listings[index].State != ListingState.Unknown) listings[index] = listings[index] with { State = ListingState.Failed };
                continue;
            }
            var listing = new MockListing(key, line.Asset.Name, line.Quantity, line.UnitQuote.BuyerPays, state, ListingAction.Registration, Attempt: result.Attempt);
            if (index < 0) listings.Add(listing);
            else if ((listings[index].Action == ListingAction.Registration || (listings[index].Action == ListingAction.None && listings[index].State != ListingState.Unknown)) && listings[index].State is (ListingState.AwaitingSteamGuard or ListingState.Submitted or ListingState.Unknown or ListingState.Failed)) listings[index] = listing;
        }
        store.SaveListings(listings);
    }
}
