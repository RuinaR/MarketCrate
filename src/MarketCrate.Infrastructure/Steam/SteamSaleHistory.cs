using System.Text.Json;
using MarketCrate.Core;
namespace MarketCrate.Infrastructure.Steam;

// Positive recent sale evidence only. Absence in a bounded page proves nothing.
internal sealed record SteamHistorySale(string ListingId,AssetKey Asset,int Quantity,string Currency,long ReceivedWire,DateTimeOffset At,bool Held);
internal sealed class SteamSaleHistory(SteamHttp http)
{
    private Guid? operation;private string? account;private SteamHistorySale[]? cached;
    internal async Task<SteamHistorySale?> FindAsync(SteamCommandRecord command,CancellationToken token)
    {
        if(command.Line.Quantity!=1||command.SentAt is null)return null; // Partial-stack aggregation is not verified.
        var current=DiagnosticHub.Context?.OperationId;
        if(cached is null||current is null||current!=operation||account!=command.Account)
        {
            cached=[];operation=current;account=command.Account; // One attempt per explicit operation, including failure.
            using var correlation=DiagnosticHub.Scope(correlation:true);
            using var request=new HttpRequestMessage(HttpMethod.Get,"https://steamcommunity.com/market/myhistory/render/?start=0&count=100&norender=1");
            request.Headers.Referrer=new("https://steamcommunity.com/market/");request.Headers.Add("X-Requested-With","XMLHttpRequest");
            cached=Parse(await http.SendAsync(request,token));operation=current;account=command.Account;
        }
        var line=command.Line;
        var candidates=cached.Where(x=>command.ListingId is {} id?x.ListingId==id:!(command.BeforeListingIds??[]).Contains(x.ListingId)&&x.Asset==line.Asset.Key).ToArray();
        var assets=candidates.Where(x=>x.Asset==line.Asset.Key).ToArray();
        var quantities=assets.Where(x=>x.Quantity==line.Quantity).ToArray();
        var currencies=quantities.Where(x=>x.Currency==line.Currency).ToArray();
        var amounts=currencies.Where(x=>x.ReceivedWire==SteamContract.ToWire(line.UnitQuote.SellerReceives,line.Currency)).ToArray();
        var matches=amounts.Where(x=>x.At>=command.SentAt.Value.AddSeconds(-5)&&x.At<=DateTimeOffset.UtcNow.AddMinutes(1)).ToArray();
        if(matches.Length!=1)
            DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Sales,DiagnosticOutcome.Unknown,LogLevel.Warning,DiagnosticError.EvidenceMissing,endpoint:"MarketHistory",reason:matches.Length>1?"HistoryMultipleCandidates":candidates.Length==0?"HistoryNoSaleCandidate":assets.Length==0?"HistoryAssetMismatch":quantities.Length==0?"HistoryQuantityMismatch":currencies.Length==0?"HistoryCurrencyMismatch":amounts.Length==0?"HistoryAmountMismatch":"HistoryTimeMismatch",count:matches.Length,requirement:"F09");
        return matches.Length==1?matches[0]:null;
    }
    internal static SteamHistorySale[] Parse(string body)=>SteamContract.Parse(body,root=>
    {
        if(!root.TryGetProperty("success",out var success)||!(success.ValueKind==JsonValueKind.True||success.ValueKind==JsonValueKind.Number&&success.GetInt32()==1))throw new SteamException(SteamFailure.ContractChanged);
        var events=root.GetProperty("events");var listings=root.GetProperty("listings");var purchases=root.GetProperty("purchases");
        if(events.ValueKind!=JsonValueKind.Array||events.GetArrayLength()>100||listings.ValueKind!=JsonValueKind.Object||purchases.ValueKind!=JsonValueKind.Object)throw new SteamException(SteamFailure.ContractChanged);
        var result=new List<SteamHistorySale>();var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var ev in events.EnumerateArray())
        {
            if(SteamContract.Integer(ev.GetProperty("event_type"))!=3)continue; // Sale, not purchase/create/cancel.
            var id=SteamContract.Id(ev.GetProperty("listingid"));var purchase=SteamContract.Id(ev.GetProperty("purchaseid"));
            if(!seen.Add(id+"_"+purchase))throw new SteamException(SteamFailure.ContractChanged);
            if(!listings.TryGetProperty(id,out var listing)||!purchases.TryGetProperty(id+"_"+purchase,out var sale))continue;
            if(SteamContract.Id(listing.GetProperty("listingid"))!=id||SteamContract.Id(sale.GetProperty("listingid"))!=id||SteamContract.Id(sale.GetProperty("purchaseid"))!=purchase)continue;
            if(new[]{"failed","needs_rollback","funds_returned","funds_revoked"}.Any(field=>Flag(sale,field)))continue;
            var currencyId=SteamContract.Integer(sale.GetProperty("received_currencyid"));
            if(currencyId is not(2001 or 2016))continue;
            var currency=SteamContract.Currency((int)currencyId-2000);
            if(SteamContract.Integer(listing.GetProperty("currencyid"))!=SteamContract.Integer(sale.GetProperty("received_currencyid")))continue;
            // History may contain fractional display units from earlier/cross-currency rows.
            // Keep the exact server integer: never round it or apply the live listing grid to the whole page.
            var received=SteamContract.Integer(sale.GetProperty("received_amount"));
            if(received<=0||SteamContract.Integer(listing.GetProperty("original_price"))!=received)
            {DiagnosticHub.Emit(DiagnosticEvent.ReadContract,DiagnosticArea.Data,DiagnosticOutcome.Unknown,LogLevel.Warning,DiagnosticError.Validation,endpoint:"MarketHistory",reason:"HistoryQuoteConflict",requirement:"F09");continue;}
            var asset=listing.GetProperty("asset");var bought=sale.GetProperty("asset");
            var key=new AssetKey(checked((int)SteamContract.Integer(asset.GetProperty("appid"))),SteamContract.Id(asset.GetProperty("contextid")),SteamContract.Id(asset.GetProperty("id")));
            if(key.AppId!=SteamContract.Integer(bought.GetProperty("appid"))||key.ContextId!=SteamContract.Id(bought.GetProperty("contextid")))continue;
            var quantity=checked((int)SteamContract.Integer(bought.GetProperty("amount")));if(quantity!=1)continue;
            var at=DateTimeOffset.FromUnixTimeSeconds(SteamContract.Integer(sale.GetProperty("time_sold")));
            if(Math.Abs((DateTimeOffset.FromUnixTimeSeconds(SteamContract.Integer(ev.GetProperty("time_event")))-at).TotalSeconds)>5)continue;
            result.Add(new(id,key,quantity,currency,received,at,Flag(sale,"funds_held")));
        }
        return result.ToArray();
    });
    private static bool Flag(JsonElement row,string field)=>row.TryGetProperty(field,out var value)&&value.ValueKind!=JsonValueKind.Null&&SteamContract.Integer(value)!=0;
}
