using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Steam;

public sealed record SteamInventoryPage(ImmutableArray<InventoryAsset> Assets, string? Next, ImmutableDictionary<AssetKey, int>? PublisherFees = null, ImmutableDictionary<AssetKey,ItemImageReference>? Images=null)
{public bool VerifiedEmpty{get;init;}public int? ReportedTotal{get;init;}}
public sealed record SteamListing(string ListingId, AssetKey Asset, string HashName, int Quantity, string Currency, long SellerReceives, long Fees, SaleState State, DateTimeOffset? CreatedAt = null);
public sealed record SteamListingsPage(ImmutableArray<SteamListing> Listings, int TotalActive, ImmutableDictionary<string,ItemImageReference>? Images=null)
{public ImmutableArray<SteamListingPresence> Presence{get;init;}=[];}
public sealed record SteamHistoryPoint(DateTimeOffset At, decimal MedianMajor, int Volume);
public sealed record SteamOrderBook(string Currency, ImmutableArray<BidLevel> Bids);
public enum SteamApprovalChannel { None, Mobile, Email, Unspecified, Either }
public sealed record SteamSellResponse(SaleState State,string? ListingId,SteamApprovalChannel Approval=SteamApprovalChannel.None);

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
    private static bool InventoryFlag(JsonElement value)
    {
        if(value.ValueKind==JsonValueKind.True)return true;if(value.ValueKind==JsonValueKind.False)return false;
        var number=Integer(value);return number switch{0=>false,1=>true,_=>throw new SteamException(SteamFailure.ContractChanged)};
    }
    private static bool Success(JsonElement root) => root.TryGetProperty("success", out var s) && (s.ValueKind == JsonValueKind.True || s.ValueKind == JsonValueKind.Number && s.GetInt32() == 1);
    public static SteamInventoryPage Inventory(string json, int appId, string context, string game) => Parse<SteamInventoryPage>(json, root =>
    {
        if (!Success(root)) throw new SteamException(SteamFailure.Restricted);
        var reported=root.TryGetProperty("total_inventory_count",out var total)?checked((int)Integer(total)):(int?)null;
        if(reported<0)throw new SteamException(SteamFailure.ContractChanged);
        var hasMore=root.TryGetProperty("more_items",out var flag);var moreValue=hasMore?Integer(flag):0;if(moreValue is not(0 or 1))throw new SteamException(SteamFailure.ContractChanged);var more=moreValue==1;
        if(reported==0&&more)throw new SteamException(SteamFailure.ContractChanged);
        var hasAssets=root.TryGetProperty("assets",out var rawAssets);if(hasAssets&&rawAssets.ValueKind!=JsonValueKind.Array)throw new SteamException(SteamFailure.ContractChanged);
        var assetCount=hasAssets?rawAssets.GetArrayLength():0;
        DiagnosticHub.Emit(DiagnosticEvent.InventoryContract,DiagnosticArea.Data,endpoint:"Inventory",reason:"InventoryReportedTotal",count:reported);
        DiagnosticHub.Emit(DiagnosticEvent.InventoryContract,DiagnosticArea.Data,endpoint:"Inventory",reason:"InventoryDescriptions",count:root.TryGetProperty("descriptions",out var desc)&&desc.ValueKind==JsonValueKind.Array?desc.GetArrayLength():null);
        if(root.TryGetProperty("descriptions",out var described)&&described.ValueKind!=JsonValueKind.Array)throw new SteamException(SteamFailure.ContractChanged);
        if(assetCount==0&&!more)
        {
            if(reported!=0||new[]{"error","Error","error_message"}.Any(name=>root.TryGetProperty(name,out var e)&&e.ValueKind!=JsonValueKind.Null&&!(e.ValueKind==JsonValueKind.String&&string.IsNullOrEmpty(e.GetString()))))
            {DiagnosticHub.Emit(DiagnosticEvent.InventoryContract,DiagnosticArea.Data,DiagnosticOutcome.Failed,LogLevel.Warning,DiagnosticError.Schema,endpoint:"Inventory",reason:reported is null?"EmptyUnproven":"InventoryContradiction");throw new SteamException(SteamFailure.ContractChanged);}
            DiagnosticHub.Emit(DiagnosticEvent.InventoryContract,DiagnosticArea.Data,endpoint:"Inventory",reason:"InventoryEmptyVerified",count:0);return new([],null){VerifiedEmpty=true,ReportedTotal=0};
        }
        if(reported==0&&assetCount>0){DiagnosticHub.Emit(DiagnosticEvent.InventoryContract,DiagnosticArea.Data,DiagnosticOutcome.Failed,LogLevel.Warning,DiagnosticError.Schema,endpoint:"Inventory",reason:"InventoryContradiction");throw new SteamException(SteamFailure.ContractChanged);}
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
            var marketable=InventoryFlag(d.GetProperty("marketable"));var tradable=InventoryFlag(d.GetProperty("tradable"));
            var hash=d.TryGetProperty("market_hash_name",out var hashValue)&&hashValue.ValueKind!=JsonValueKind.Null?hashValue.GetString()??"":"";
            if(marketable&&string.IsNullOrWhiteSpace(hash))throw new SteamException(SteamFailure.ContractChanged);
            var name=d.TryGetProperty("name",out var nameValue)&&nameValue.ValueKind!=JsonValueKind.Null?nameValue.GetString():hash;
            result.Add(new(new(appId,context,Id(a.GetProperty("assetid"))),game,string.IsNullOrWhiteSpace(name)?"이름 미확인":name,
                hash,count,count>1,marketable,tradable));
            images.Add(result[^1].Key,Image(d));
            if (d.TryGetProperty("market_fee", out var fee))
            {
                var rate = decimal.Parse(fee.ValueKind == JsonValueKind.String ? fee.GetString()! : fee.GetRawText(), CultureInfo.InvariantCulture) * 10000;
                if (rate is < 0 or > 10000 || decimal.Truncate(rate) != rate) throw new SteamException(SteamFailure.ContractChanged);
                fees.Add(result[^1].Key, (int)rate);
            }
        }
        if (result.Select(x => x.Key).Distinct().Count() != result.Count) throw new SteamException(SteamFailure.ContractChanged);
        DiagnosticHub.Emit(DiagnosticEvent.InventoryContract,DiagnosticArea.Data,endpoint:"Inventory",reason:more?"InventoryPagePartial":"InventoryPage",count:result.Count);
        return new(result.ToImmutable(), more ? Id(root.GetProperty("last_assetid")) : null, fees.ToImmutable(),images.ToImmutable()){ReportedTotal=reported};
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
        // Known optional pending aliases are compatible. Missing entire membership buckets
        // must not become negative evidence for submission/cancellation.
        if(!root.TryGetProperty("listings",out _)||!root.TryGetProperty("listings_on_hold",out _)||
            !root.TryGetProperty("pending_listings",out _)&&!root.TryGetProperty("listings_to_confirm",out _))throw new SteamException(SteamFailure.ContractChanged);
        var presence=SteamMarketMembership.Read(root);
        foreach (var (row,state) in SteamMarketMembership.Rows(root))
        {
            var a=row.GetProperty("asset");var key=SteamMarketMembership.Identity(a);
            // A pending/held asset without these fields is still present, but has no verified financial quote.
            var listingId=SteamMarketMembership.ListingId(row);
            if(listingId is null||!row.TryGetProperty("currencyid",out var currencyId)||
                !row.TryGetProperty("price",out var price)||!row.TryGetProperty("fee",out var fees)||!a.TryGetProperty("amount",out var amount)||
                !root.TryGetProperty("assets",out var assetMap)||assetMap.ValueKind!=JsonValueKind.Object||!assetMap.TryGetProperty(key.AppId.ToString(CultureInfo.InvariantCulture),out var apps)||
                apps.ValueKind!=JsonValueKind.Object||!apps.TryGetProperty(key.ContextId,out var contexts)||contexts.ValueKind!=JsonValueKind.Object||!contexts.TryGetProperty(key.AssetId,out var d)||
                !d.TryGetProperty("market_hash_name",out var hash)||hash.ValueKind!=JsonValueKind.String||string.IsNullOrWhiteSpace(hash.GetString()))continue;
            var currency=Currency(checked((int)Integer(currencyId))-2000);var seller=FromWire(Integer(price),currency);var fee=FromWire(Integer(fees),currency);
            var quantity=checked((int)Integer(amount));if(quantity<1||seller<1||fee<0)throw new SteamException(SteamFailure.ContractChanged);
            var listing=new SteamListing(listingId,key,hash.GetString()!,quantity,currency,seller,fee,state,
                row.TryGetProperty("time_created",out var created)?DateTimeOffset.FromUnixTimeSeconds(Integer(created)):null);
            if(rows.FirstOrDefault(x=>x.ListingId==listing.ListingId) is {} duplicate)
            {if(duplicate!=listing)throw new SteamException(SteamFailure.ContractChanged);continue;}
            if(d.TryGetProperty("appid",out var da)&&Integer(da)!=key.AppId)throw new SteamException(SteamFailure.ContractChanged);
            rows.Add(listing);images.Add(listing.ListingId,Image(d));
        }
        var total = checked((int)Integer(root.GetProperty("total_count")));
        if (total < 0 || rows.Select(x => x.ListingId).Distinct().Count() != rows.Count) throw new SteamException(SteamFailure.ContractChanged);
        return new(rows.ToImmutable(), total,images.ToImmutable()){Presence=presence};
    });
    public static SteamSellResponse Sell(string json) => Parse<SteamSellResponse>(json, root =>
    {
        if (!Success(root)) return new(SaleState.Unknown,null); // No side-effect inference from success:false.
        bool Flag(string name)
        {
            if(!root.TryGetProperty(name,out var value))return false;
            return value.ValueKind switch{JsonValueKind.True=>true,JsonValueKind.False=>false,JsonValueKind.Number when value.TryGetInt32(out var n)&&n is 0 or 1=>n==1,_=>throw new SteamException(SteamFailure.ContractChanged)};
        }
        var mobile=Flag("needs_mobile_confirmation");var email=Flag("needs_email_confirmation");var required=Flag("requires_confirmation");
        if(root.TryGetProperty("requires_confirmation",out _)&&!required&&(mobile||email))
        {
            DiagnosticHub.Emit(DiagnosticEvent.ReadContract,DiagnosticArea.Sales,DiagnosticOutcome.Unknown,LogLevel.Warning,DiagnosticError.Schema,endpoint:"Sell",reason:"ConfirmationFlagsConflict");
            return new(SaleState.Unknown,null);
        }
        var channel=mobile&&email?SteamApprovalChannel.Either:mobile?SteamApprovalChannel.Mobile:email?SteamApprovalChannel.Email:required?SteamApprovalChannel.Unspecified:SteamApprovalChannel.None;
        var state=channel==SteamApprovalChannel.None?SaleState.Submitted:SaleState.AwaitingSteamGuard;
        DiagnosticHub.Emit(DiagnosticEvent.ReadContract,DiagnosticArea.Sales,state==SaleState.AwaitingSteamGuard?DiagnosticOutcome.Pending:DiagnosticOutcome.Completed,endpoint:"Sell",reason:channel switch{SteamApprovalChannel.Mobile=>"MobileConfirmationRequired",SteamApprovalChannel.Email=>"EmailConfirmationRequired",SteamApprovalChannel.Either=>"EitherConfirmationRequired",SteamApprovalChannel.Unspecified=>"ConfirmationChannelUnknown",_=>"NoConfirmationFlag"},to:state.ToString());
        return new(state,root.TryGetProperty("listingid",out var id)?Id(id):null,channel);
    });
    public static string ApprovalHint(SteamApprovalChannel channel)=>channel switch
    {
        SteamApprovalChannel.Mobile=>"요청 당시 Steam이 모바일 확인을 요구했습니다. 공식 Steam 앱에서 직접 승인한 뒤 판매 상태를 다시 조회하세요. 게시 완료가 아닙니다.",
        SteamApprovalChannel.Email=>"요청 당시 Steam이 이메일 확인을 요구했습니다. 이메일에서 직접 확인한 뒤 판매 상태를 다시 조회하세요. 게시 완료가 아닙니다.",
        SteamApprovalChannel.Either=>"요청 당시 Steam이 모바일/이메일 확인을 요구했습니다. 공식 Steam에서 직접 확인한 뒤 판매 상태를 다시 조회하세요. 게시 완료가 아닙니다.",
        _=>"요청 당시 Steam이 추가 확인을 요구했습니다. 확인 방식은 미확인입니다. 공식 Steam에서 확인한 뒤 판매 상태를 다시 조회하세요."
    };
    // Only called after SteamHttp accepted HTTP200. An acknowledgement is not proof of return.
    public static bool CancellationAccepted(string json) => string.IsNullOrWhiteSpace(json)||Parse(json, root => root.ValueKind == JsonValueKind.Array && root.GetArrayLength() == 0);
    public static string Currency(int id) => id switch { 1 => "USD", 16 => "KRW", _ => throw new SteamException(SteamFailure.Restricted) };
    public static long FromWire(long value, string currency) => currency switch { "USD" => value, "KRW" when value % 100 == 0 => value / 100, _ => throw new SteamException(SteamFailure.ContractChanged) };
    public static long ToWire(long value, string currency) => currency switch { "USD" => value, "KRW" => checked(value * 100), _ => throw new SteamException(SteamFailure.Restricted) };
}
