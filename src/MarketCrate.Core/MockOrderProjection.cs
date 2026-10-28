namespace MarketCrate.Core;

public static class MockOrderProjection
{
    public static void Update(IDemoDataStore store,BatchRecord batch)
    {
        var listings=store.ReadListings().ToList();
        for(var i=0;i<batch.Results.Length;i++)
        {
            var result=batch.Results[i];
            if(result.State is SaleState.Ready or SaleState.Failed) continue;
            var line=batch.Snapshot.Lines[i]; var key=SaleBatchCoordinator.RequestKey(batch.Snapshot,line);
            var state=result.State switch { SaleState.Listed=>ListingState.Listed,SaleState.AwaitingSteamGuard=>ListingState.AwaitingSteamGuard,SaleState.Submitted=>ListingState.Submitted,_=>ListingState.Unknown };
            var index=listings.FindIndex(x=>x.ListingId==key);
            var listing=new MockListing(key,line.Asset.Name,line.Quantity,line.UnitQuote.BuyerPays,state);
            if(index<0) listings.Add(listing);
            else if(listings[index].State is ListingState.AwaitingSteamGuard or ListingState.Submitted) listings[index]=listing;
        }
        store.SaveListings(listings);
    }
}
