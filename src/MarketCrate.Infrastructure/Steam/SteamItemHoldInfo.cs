using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace MarketCrate.Infrastructure.Steam;

// Display evidence only. Never unlock an asset locally or infer purchase time from a duration.
public sealed record SteamItemHoldInfo(DateTimeOffset? TradeAfter,DateTimeOffset? MarketAfter,int? TradeDays,int? MarketDays,bool TimeUnverified)
{
    private static readonly string[] ReleaseFormats=(from date in new[]{"MMM d, yyyy","MMM dd, yyyy","MMMM d, yyyy","dddd, MMMM d, yyyy"} from time in new[]{"H:mm:ss","HH:mm:ss"} from zone in new[]{"GMT","UTC"} select $"{date} ({time}) '{zone}'").ToArray();
    public string Describe(bool tradable,bool marketable,DateTimeOffset now)
    {
        string Until(DateTimeOffset? at)=>at is null?"해제 시각 미확인":at<=now?"안내 시각 경과 · 보관함 재조회 필요":$"약 {(at.Value-now).Days}일 {(at.Value-now).Hours}시간 {(at.Value-now).Minutes}분 남음 · {at.Value.ToOffset(TimeSpan.FromHours(9)):MM-dd HH:mm} KST";
        var lines=new List<string>();
        if(!tradable)lines.Add("아이템 교환 제한 · "+Until(TradeAfter));
        if(!marketable)lines.Add("장터 판매 불가 · "+Until(MarketAfter));
        if(TradeAfter is {} ta&&tradable&&ta>now||MarketAfter is {} ma&&marketable&&ma>now)lines.Add("서버 가능 상태와 해제 안내가 다름 · 다시 조회하세요");
        if(TradeDays is >0&&TradeAfter is null&&!tradable)lines.Add($"구매 후 교환 제한 안내 {TradeDays}일 · 남은 시간으로 추정하지 않음");
        if(MarketDays is >0&&MarketAfter is null&&!marketable)lines.Add($"구매 후 장터 제한 안내 {MarketDays}일 · 남은 시간으로 추정하지 않음");
        if(TimeUnverified)lines.Add("해제 안내의 시각/형식 미확인 · 연도·시간대 추정 안 함");
        return string.Join("\n",lines);
    }
    internal static SteamItemHoldInfo Parse(JsonElement asset,JsonElement description)
    {
        bool uncertain=false,tradeBad=false,marketBad=false;DateTimeOffset? trade=null,market=null;
        int? Days(string name)
        {
            if(!description.TryGetProperty(name,out var value))return null;
            try{var n=SteamContract.Integer(value);if(n is <0 or >365)throw new FormatException();return (int)n;}catch(Exception e)when(e is FormatException or InvalidOperationException or OverflowException){uncertain=true;return null;}
        }
        var tradeDays=Days("market_tradable_restriction");var marketDays=Days("market_marketable_restriction");
        var owner=asset.TryGetProperty("owner_descriptions",out var own)?own:description.TryGetProperty("owner_descriptions",out own)?own:default;
        if(owner.ValueKind!=JsonValueKind.Undefined)
        {
            if(owner.ValueKind!=JsonValueKind.Array||owner.GetArrayLength()>64)uncertain=true;
            else foreach(var row in owner.EnumerateArray())
            {
                if(row.ValueKind!=JsonValueKind.Object||!row.TryGetProperty("value",out var value)||value.ValueKind!=JsonValueKind.String){uncertain=true;continue;}
                var text=value.GetString()!;if(text.Length>4096){uncertain=true;continue;}
                try{text=WebUtility.HtmlDecode(Regex.Replace(text,"<[^>]{0,256}>","",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50))).Trim();}catch(RegexMatchTimeoutException){uncertain=true;continue;}
                var prefixes=new[]{"Tradable/Marketable After","Tradable & Marketable After","Tradable After","Marketable After"};
                var prefix=prefixes.FirstOrDefault(p=>text.StartsWith(p,StringComparison.Ordinal)&&text.Length>p.Length&&(char.IsWhiteSpace(text[p.Length])||text[p.Length]==':'));if(prefix is null)continue;
                bool t=prefix.StartsWith("Tradable",StringComparison.Ordinal),m=prefix.Contains("Marketable",StringComparison.Ordinal);
                var date=text[prefix.Length..].Trim().TrimStart(':').Trim();
                if(!DateTimeOffset.TryParseExact(date,ReleaseFormats,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal,out var at)){uncertain=true;if(t)tradeBad=true;if(m)marketBad=true;continue;}
                if(t){if(trade is {} old&&old!=at)tradeBad=true;trade=at;}if(m){if(market is {} old&&old!=at)marketBad=true;market=at;}
            }
        }
        return new(tradeBad?null:trade,marketBad?null:market,tradeDays,marketDays,uncertain||tradeBad||marketBad);
    }
}
