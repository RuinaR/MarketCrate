using MarketCrate.Core;
namespace MarketCrate.App.ViewModels;

public sealed class ManagementRowViewModel(MockListing listing, bool editable, Action changed) : Observable
{
    public MockListing Listing { get; } = listing;
    public string Name => Listing.Name;
    public string Detail => $"{Listing.Quantity}개 · 기존 구매자 단가 ₩{Listing.BuyerPays:N0} · {Listing.State}";
    public bool Editable { get; } = editable;
    private long price = listing.PendingPrice ?? listing.BuyerPays;
    public long Price { get => price; set { if (Set(ref price, value)) { Raise(nameof(QuoteText)); changed(); } } }
    public bool Valid => Price is >= 3 and <= 1_000_000_000;
    public string QuoteText
    {
        get { if (!Valid) return "가격 오류"; var quote = Pricing.FromBuyer(Price, new()); return $"{(Editable?"변경":"확정")} 구매자 지불 ₩{Price * Listing.Quantity:N0} / Steam ₩{quote.SteamFee * Listing.Quantity:N0} / 게임 ₩{quote.GameFee * Listing.Quantity:N0} / 실수령 ₩{quote.SellerReceives * Listing.Quantity:N0}"; }
    }
}
