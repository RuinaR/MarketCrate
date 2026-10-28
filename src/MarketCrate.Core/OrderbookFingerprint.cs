using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MarketCrate.Core;

// Only immutable price/quantity evidence; never an account or asset identifier.
public static class OrderbookFingerprint
{
    public static string Of(PriceSnapshot price) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        price.Currency + ":" + string.Join(";", price.Bids.Select(x => x.BuyerPays.ToString(CultureInfo.InvariantCulture) + "," + x.Quantity.ToString(CultureInfo.InvariantCulture))))));
}
