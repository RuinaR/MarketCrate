using MarketCrate.Core;

namespace MarketCrate.App.ViewModels;

public sealed class ItemViewModel(InventoryAsset asset, PriceSnapshot price, Action changed) : Observable
{
    public InventoryAsset Asset { get; } = asset;
    public PriceSnapshot Price { get; } = price;
    public string Name => Asset.Name;
    public string Game => Asset.Game;
    public string Flags => $"{(Asset.Marketable ? "판매 가능" : "판매 불가")} · {(Asset.Tradable ? "교환 가능" : "교환 불가")}{(Asset.Locked ? " · 보류" : "")}";
    public bool CanSelect => Asset.Marketable && !Asset.Locked;
    public Command ToggleSelection => new(() => Selected = !Selected, () => CanSelect);
    public int Maximum => Asset.Quantity;
    public string Average => $"₩{Pricing.WeightedAverage(Price.Trades):N0}";
    public string Bid => $"₩{Price.Bids[0].BuyerPays:N0}";
    private bool selected;
    public bool Selected { get => selected; set { if (Set(ref selected, value && CanSelect)) changed(); } }
    private int quantity = 1;
    public int Quantity { get => quantity; set { if (Set(ref quantity, Math.Clamp(value, 1, Maximum))) changed(); } }
}
public sealed class ReviewLineViewModel : Observable
{
    public InventoryAsset Asset { get; }
    public string Name => Asset.Name;
    public string Identity => $"{Asset.Game} · {Asset.Key.AppId}/{Asset.Key.ContextId}/{Asset.Key.AssetId}";
    public int Quantity { get; }
    public long SuggestedPrice { get; }
    public DateTimeOffset CapturedAt { get; }
    public string MarketTimeDisplay => $"시세 {CapturedAt.ToOffset(TimeSpan.FromHours(9)):MM-dd HH:mm:ss} KST";
    public string Source { get; }
    private long price;
    private readonly Action changed;
    public ReviewLineViewModel(ItemViewModel item, Allocation allocation, Action changed)
    { Asset = item.Asset; Quantity = allocation.Quantity; SuggestedPrice = price = allocation.BuyerPays; CapturedAt = item.Price.CapturedAt; Source = item.Price.Source; this.changed = changed; }
    public long Price { get => price; set { if (Set(ref price, value)) { Raise(nameof(Fee)); Raise(nameof(Net)); changed(); } } }
    public bool Valid => Price is >= 3 and <= 1_000_000_000;
    public FeeQuote Quote => Pricing.FromBuyer(Price, new());
    public string Fee => Valid ? $"₩{Quote.Fees * Quantity:N0}" : "가격 오류";
    public string Net => Valid ? $"₩{Quote.SellerReceives * Quantity:N0}" : "—";
    public SaleLineSnapshot Snapshot(int index) => new($"line-{index}", Asset, Quantity, "KRW", Quote, CapturedAt);
}
