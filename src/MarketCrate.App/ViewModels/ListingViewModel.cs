using MarketCrate.Core;
namespace MarketCrate.App.ViewModels;

public sealed class ListingViewModel(MockListing listing, Action changed, bool canResume = false) : Observable
{
    public MockListing Listing { get; } = listing;
    public string Name => Listing.Name;
    public string Detail => $"{Listing.Quantity}개 × ₩{Listing.BuyerPays:N0} · Mock {Listing.State} · 시도 {Listing.Attempt}{(Listing.PendingPrice is long p ? $" · 변경 요청 ₩{p:N0}" : "")}";
    public bool CanCancel => Listing.State == ListingState.Listed;
    public bool CanRetry => (Listing.State == ListingState.Failed && Listing.Action is ListingAction.Cancel or ListingAction.Reprice) || canResume;
    public bool CanSelect => CanCancel || CanRetry;
    private bool selected;
    public bool Selected { get => selected; set { if (Set(ref selected, value && CanSelect)) changed(); } }
}
