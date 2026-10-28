using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

public sealed record SteamInventoryPage(ImmutableArray<InventoryAsset> Assets, string? Next, ImmutableDictionary<AssetKey, int>? PublisherFees = null, ImmutableDictionary<AssetKey,ItemImageReference>? Images=null);
public sealed record SteamListing(string ListingId, AssetKey Asset, string HashName, int Quantity, string Currency, long SellerReceives, long Fees, SaleState State, DateTimeOffset? CreatedAt = null);
public sealed record SteamListingsPage(ImmutableArray<SteamListing> Listings, int TotalActive, ImmutableDictionary<string,ItemImageReference>? Images=null);
public sealed record SteamHistoryPoint(DateTimeOffset At, decimal MedianMajor, int Volume);
public sealed record SteamOrderBook(string Currency, ImmutableArray<BidLevel> Bids);

// Contract shape, not Valve API guarantees. Synthetic tests are derived from source contracts.
public static class SteamContract
{
    public static T Parse<T>(string json, Func<JsonElement, T> parse)
    {
        try { using var document = SteamHttp.Json(json); var result=parse(document.RootElement);DiagnosticHub.Emit(DiagnosticEvent.ResponseParsed,DiagnosticArea.Data,level:LogLevel.Debug); return result; }
        catch(SteamException error){SteamDiagnostics.Failure(error);throw;}
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException)
        { DiagnosticHub.Emit(DiagnosticEvent.ResponseParsed,DiagnosticArea.Data,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticError.Schema,exception:error); throw new SteamException(SteamFailure.ContractChanged); }
    }
    public static string Id(JsonElement value)
    {
        // Never accept JSON floating-point IDs or coerce an already rounded JavaScript number.
        var text = value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText();
        if (text.Length is < 1 or > 20 || text.Any(c => c < '0' || c > '9') || !ulong.TryParse(text, out var number) || number == 0)
            throw new SteamException(SteamFailure.ContractChanged);
        return text;
    }
    private static string InstanceId(JsonElement value)
    {var text=value.GetString()??"";if(text.Length is <1 or >20||text.Any(c=>!char.IsAsciiDigit(c))||!ulong.TryParse(text,out _))throw new SteamException(SteamFailure.ContractChanged);return text;}
    public static long Integer(JsonElement value) => value.ValueKind == JsonValueKind.String ? long.Parse(value.GetString()!, CultureInfo.InvariantCulture) : value.GetInt64();
    private static bool Success(JsonElement root) => root.TryGetProperty("success", out var s) && (s.ValueKind == JsonValueKind.True || s.ValueKind == JsonValueKind.Number && s.GetInt32() == 1);
    public static SteamInventoryPage Inventory(string json, int appId, string context, string game) => Parse<SteamInventoryPage>(json, root =>
    {
        if (!Success(root)) throw new SteamException(SteamFailure.Restricted);
        // Empty inventory may omit both arrays; only accept when explicit total count is zero.
        if (root.TryGetProperty("total_inventory_count", out var total) && total.GetInt32() == 0 && !root.TryGetProperty("assets", out _)) return new([], null);
        var descriptions = root.GetProperty("descriptions").EnumerateArray().ToDictionary(x => (Id(x.GetProperty("classid")), InstanceId(x.GetProperty("instanceid"))));
        var result = ImmutableArray.CreateBuilder<InventoryAsset>();
        var fees = ImmutableDictionary.CreateBuilder<AssetKey, int>();
        var images=ImmutableDictionary.CreateBuilder<AssetKey,ItemImageReference>();
        foreach (var a in root.GetProperty("assets").EnumerateArray())
        {
            if (a.GetProperty("appid").GetInt32() != appId || a.GetProperty("contextid").GetString() != context) throw new SteamException(SteamFailure.ContractChanged);
            var d = descriptions[(Id(a.GetProperty("classid")), InstanceId(a.GetProperty("instanceid")))];
            var count = checked((int)Integer(a.GetProperty("amount")));
            if (count < 1 || d.GetProperty("appid").GetInt32() != appId) throw new SteamException(SteamFailure.ContractChanged);
            // Stackability is not proven by a single unit; stack splits remain conservative.
            result.Add(new(new(appId, context, Id(a.GetProperty("assetid"))), game, d.GetProperty("name").GetString()!,
                d.GetProperty("market_hash_name").GetString()!, count, count > 1, Integer(d.GetProperty("marketable")) == 1, Integer(d.GetProperty("tradable")) == 1));
            images.Add(result[^1].Key,Image(d));
            if (d.TryGetProperty("market_fee", out var fee))
            {
                var rate = decimal.Parse(fee.ValueKind == JsonValueKind.String ? fee.GetString()! : fee.GetRawText(), CultureInfo.InvariantCulture) * 10000;
                if (rate is < 0 or > 10000 || decimal.Truncate(rate) != rate) throw new SteamException(SteamFailure.ContractChanged);
                fees.Add(result[^1].Key, (int)rate);
            }
        }
        if (result.Select(x => x.Key).Distinct().Count() != result.Count) throw new SteamException(SteamFailure.ContractChanged);
        var more = root.TryGetProperty("more_items", out var m) && Integer(m) == 1;
        return new(result.ToImmutable(), more ? Id(root.GetProperty("last_assetid")) : null, fees.ToImmutable(),images.ToImmutable());
    });
    private static ItemImageReference Image(JsonElement data)
    {
        foreach(var name in new[]{"icon_url_large","icon_url"})
        {
            if(!data.TryGetProperty(name,out var value))continue;
            if(value.ValueKind!=JsonValueKind.String){DiagnosticHub.Emit(DiagnosticEvent.ImageMetadata,DiagnosticArea.Data,DiagnosticOutcome.Failed,LogLevel.Warning,DiagnosticError.Schema,requirement:"B04",reason:"ImageInvalid");return new(null,true);}
            var token=value.GetString();if(string.IsNullOrEmpty(token))continue;
            var valid=SteamImageCache.ValidToken(token);
            DiagnosticHub.Emit(DiagnosticEvent.ImageMetadata,DiagnosticArea.Data,valid?DiagnosticOutcome.Succeeded:DiagnosticOutcome.Failed,valid?LogLevel.Debug:LogLevel.Warning,requirement:"B04",reason:valid?"ImageReady":"ImageInvalid");
            return valid?new(token):new(null,true);
        }
        return new(null);
    }
    public static SteamOrderBook OrderBook(string json) => Parse<SteamOrderBook>(json, root =>
    {
        var outer = root.GetProperty("data"); if (!Success(outer)) throw new SteamException(SteamFailure.ContractChanged);
        var data = outer.GetProperty("data"); var currency = Currency((int)Integer(data.GetProperty("eCurrency")));
        var flat = data.GetProperty("rgCompactBuyOrders").EnumerateArray().ToArray();
        if (flat.Length % 2 != 0 || flat.Length > 20000) throw new SteamException(SteamFailure.ContractChanged);
        var bids = ImmutableArray.CreateBuilder<BidLevel>(); long previous = long.MaxValue;
        for (var i = 0; i < flat.Length; i += 2)
        {
            var price = FromWire(Integer(flat[i]), currency); var count = checked((int)Integer(flat[i + 1]));
            if (price <= 0 || count <= 0 || price >= previous) throw new SteamException(SteamFailure.ContractChanged);
            previous = price; bids.Add(new(price, count));
        }
        return new(currency, bids.ToImmutable());
    });
    public static ImmutableArray<SteamHistoryPoint> History(string json) => Parse(json, root =>
    {
        if (!Success(root)) throw new SteamException(SteamFailure.Restricted);
        var rows = ImmutableArray.CreateBuilder<SteamHistoryPoint>();
        foreach (var row in root.GetProperty("prices").EnumerateArray())
        {
            if (row.GetArrayLength() != 3) throw new SteamException(SteamFailure.ContractChanged);
            var text = row[0].GetString()!;
            if (!text.EndsWith(" +0", StringComparison.Ordinal) || !DateTimeOffset.TryParseExact(text[..^3], "MMM dd yyyy HH:", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at))
                throw new SteamException(SteamFailure.ContractChanged);
            var price = row[1].GetDecimal(); var volumeText=row[2].ValueKind==JsonValueKind.String?row[2].GetString()!:row[2].GetRawText();if(!System.Text.RegularExpressions.Regex.IsMatch(volumeText,@"^(?:[0-9]+|[1-9][0-9]{0,2}(?:,[0-9]{3})+)$"))throw new SteamException(SteamFailure.ContractChanged);var volume=int.Parse(volumeText,NumberStyles.AllowThousands,CultureInfo.InvariantCulture);
            if (price <= 0 || volume <= 0) throw new SteamException(SteamFailure.ContractChanged);
            rows.Add(new(at, price, volume));
        }
        return rows.ToImmutable();
    });
    public static SteamListingsPage Listings(string json) => Parse<SteamListingsPage>(json, root =>
    {
        if (!Success(root)) throw new SteamException(SteamFailure.Restricted);
        var rows = ImmutableArray.CreateBuilder<SteamListing>();
        var images=ImmutableDictionary.CreateBuilder<string,ItemImageReference>();
        foreach (var (bucket, state) in new[] { ("listings", SaleState.Listed), ("listings_to_confirm", SaleState.AwaitingSteamGuard), ("listings_on_hold", SaleState.Submitted) })
        {
            // Missing buckets are not interpreted as complete emptiness.
            foreach (var row in root.GetProperty(bucket).EnumerateArray())
            {
                var a = row.GetProperty("asset"); var key = new AssetKey(a.GetProperty("appid").GetInt32(), Id(a.GetProperty("contextid")), Id(a.GetProperty("id")));
                var d = root.GetProperty("assets").GetProperty(key.AppId.ToString(CultureInfo.InvariantCulture)).GetProperty(key.ContextId).GetProperty(key.AssetId);
                var currency = Currency(checked((int)Integer(row.GetProperty("currencyid"))) - 2000);
                var seller = FromWire(Integer(row.GetProperty("price")), currency); var fee = FromWire(Integer(row.GetProperty("fee")), currency);
                var quantity = checked((int)Integer(a.GetProperty("amount")));
                if (key.AppId < 1 || quantity < 1 || seller < 1 || fee < 0) throw new SteamException(SteamFailure.ContractChanged);
                rows.Add(new(Id(row.GetProperty("listingid")), key, d.GetProperty("market_hash_name").GetString()!, quantity, currency, seller, fee, state,
                    row.TryGetProperty("time_created", out var created) ? DateTimeOffset.FromUnixTimeSeconds(Integer(created)) : null));
                if(d.TryGetProperty("appid",out var da)&&da.GetInt32()!=key.AppId)throw new SteamException(SteamFailure.ContractChanged);
                images.Add(rows[^1].ListingId,Image(d));
            }
        }
        var total = root.GetProperty("total_count").GetInt32();
        if (total < 0 || rows.Select(x => x.ListingId).Distinct().Count() != rows.Count) throw new SteamException(SteamFailure.ContractChanged);
        return new(rows.ToImmutable(), total,images.ToImmutable());
    });
    public static (SaleState State, string? ListingId) Sell(string json) => Parse(json, root =>
    {
        if (!Success(root)) return (SaleState.Unknown, null); // success:false is not sufficient evidence of no side effect.
        var pending = false;
        foreach (var name in new[] { "needs_mobile_confirmation", "needs_email_confirmation", "requires_confirmation" })
            if (root.TryGetProperty(name, out var flag))
            {
                if (flag.ValueKind is not (JsonValueKind.True or JsonValueKind.False or JsonValueKind.Number)) throw new SteamException(SteamFailure.ContractChanged);
                pending |= flag.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Number when flag.TryGetInt32(out var value) && value is 0 or 1 => value == 1, _ => throw new SteamException(SteamFailure.ContractChanged) };
            }
        return (pending ? SaleState.AwaitingSteamGuard : SaleState.Submitted, root.TryGetProperty("listingid", out var id) ? Id(id) : null);
    });
    public static bool CancellationAccepted(string json) => Parse(json, root => root.ValueKind == JsonValueKind.Array && root.GetArrayLength() == 0);
    public static string Currency(int id) => id switch { 1 => "USD", 16 => "KRW", _ => throw new SteamException(SteamFailure.Restricted) };
    public static long FromWire(long value, string currency) => currency switch { "USD" => value, "KRW" when value % 100 == 0 => value / 100, _ => throw new SteamException(SteamFailure.ContractChanged) };
    public static long ToWire(long value, string currency) => currency switch { "USD" => value, "KRW" => checked(value * 100), _ => throw new SteamException(SteamFailure.Restricted) };
}
