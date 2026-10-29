using System.Globalization;

namespace MarketCrate.Core;

// Minor units: KRW won, USD cents. Wire units are an independent boundary.
public readonly record struct Money
{
    public long Minor { get; }
    public string Currency { get; }
    public Money(long minor, string currency)
    { if (minor < 0 || currency is not ("KRW" or "USD")) throw new ArgumentException("UnsupportedMoney"); Minor = minor; Currency = currency; }
    public long Wire => Currency == "KRW" ? checked(Minor * 100) : Minor;
    public static Money FromWire(long wire, string currency)
    {
        if (wire < 0 || currency == "KRW" && wire % 100 != 0) throw new ArgumentException("UnrepresentableMoney");
        return new(currency == "KRW" ? wire / 100 : wire, currency);
    }
    public override string ToString() => Currency == "KRW" ? $"₩{Minor:N0}" : $"USD {Minor / 100}.{Minor % 100:00}";
}

// All fields use integer wire units/rational rates obtained from server metadata.
public record SteamFeeRate(long Millionths, long CapWire = 0);
public record SteamFeeSchedule(string Currency, long MinimumWire, long IncrementWire, SteamFeeRate Steam, SteamFeeRate Publisher)
{
    // Only the freshly resolved official classic app/wallet contract sets this.
    // Missing optional item metadata is different from an explicit conflicting rate.
    public int? PublisherFallbackAppId {get;init;}
    public bool PublisherMatches(int appId,int? itemBasisPoints)=>itemBasisPoints is {} rate?rate*100L==Publisher.Millionths:PublisherFallbackAppId==appId;
    public FeeQuote FromSeller(long minor)
    {
        Validate(); var seller = new Money(minor, Currency).Wire;
        if (seller < MinimumWire || seller % IncrementWire != 0) throw new ArgumentException("PriceIncrementMismatch");
        long Fee(SteamFeeRate rate)
        {
            if (rate.Millionths == 0) return 0;
            var raw = checked(seller * rate.Millionths) / 1_000_000;
            // Current official dialog: trunc(rate*base), nearest increment (half up), minimum; optional cap.
            var amount = raw <= MinimumWire ? MinimumWire : Math.Max(IncrementWire, checked((raw + IncrementWire / 2) / IncrementWire * IncrementWire));
            return rate.CapWire > 0 ? Math.Min(amount, rate.CapWire) : amount;
        }
        var steam = Money.FromWire(Fee(Steam), Currency).Minor; var publisher = Money.FromWire(Fee(Publisher), Currency).Minor;
        return new(checked(minor + steam + publisher), minor, steam, publisher);
    }
    public FeeQuote FromBuyer(long buyer, bool requireExact = true)
    {
        Validate(); var target = new Money(buyer, Currency).Wire;
        long low = Math.Max(1, checked((MinimumWire + IncrementWire - 1) / IncrementWire)), high = target / IncrementWire;
        if (high < low || FromSeller(Money.FromWire(low * IncrementWire, Currency).Minor).BuyerPays > buyer) throw new ArgumentException("BelowMinimum");
        while (low < high)
        {
            var mid = low + (high - low + 1) / 2;
            if (FromSeller(Money.FromWire(checked(mid * IncrementWire), Currency).Minor).BuyerPays <= buyer) low = mid; else high = mid - 1;
        }
        var quote = FromSeller(Money.FromWire(checked(low * IncrementWire), Currency).Minor);
        if (requireExact && quote.BuyerPays != buyer) throw new ArgumentException("UnrepresentableBuyerPrice");
        return quote;
    }
    public void Validate()
    {
        _ = new Money(0, Currency);
        if (MinimumWire <= 0 || IncrementWire <= 0 || MinimumWire > 1_000_000 || IncrementWire > 1_000_000 ||
            MinimumWire % IncrementWire != 0 || Currency == "KRW" && (MinimumWire % 100 != 0 || IncrementWire % 100 != 0) ||
            new[] { Steam, Publisher }.Any(x => x.Millionths is < 0 or > 1_000_000 || x.CapWire < 0 || x.CapWire % IncrementWire != 0))
            throw new ArgumentException("UnsupportedFeeSchedule");
    }
}
