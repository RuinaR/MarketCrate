using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MarketCrate.Core;

// Only immutable price/quantity evidence; never an account or asset identifier.
public static class OrderbookFingerprint
{
    public static bool IsValid(string? digest)
    {
        if(digest is null)return false;var hash=IsSell(digest)?digest[4..]:digest;
        return hash.Length==64&&hash.All(char.IsAsciiHexDigit);
    }
    public static bool IsSell(string? digest)=>digest?.StartsWith("ASK:",StringComparison.Ordinal)==true;
    public static string For(PriceSnapshot price,string? digest)=>IsSell(digest)?OfAsk(price):Of(price);
    public static string OfAsk(PriceSnapshot price)
    {
        if(!price.OwnSellExcluded||price.Asks.IsEmpty)throw new ArgumentException("SellOrdersUnavailable");
        // Repricing depends on the best ask price, not competing quantities or our own removed listing count.
        return "ASK:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(price.Currency+":"+price.Asks[0].BuyerPays.ToString(CultureInfo.InvariantCulture))));
    }
    public static string Of(PriceSnapshot price) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        price.Currency + ":" + string.Join(";", price.Bids.Select(x => x.BuyerPays.ToString(CultureInfo.InvariantCulture) + "," + x.Quantity.ToString(CultureInfo.InvariantCulture))))));
}
