using MarketCrate.Core;
namespace MarketCrate.App.ViewModels;

public sealed class ListingViewModel(MockListing listing, Action changed) : Observable
{
    public MockListing Listing { get; } = listing;
    public string Name => Listing.Name;
    public string Detail => $"{Listing.Quantity}개 × ₩{Listing.BuyerPays:N0} · Mock {Listing.State}";
    public bool CanCancel => Listing.State == ListingState.Listed;
    private bool selected;
    public bool Selected { get => selected; set { if (Set(ref selected, value && CanCancel)) changed(); } }
}
