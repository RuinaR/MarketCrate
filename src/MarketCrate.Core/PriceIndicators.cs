using System.Collections.Immutable;

namespace MarketCrate.Core;

public enum PriceBasis { Immediate, ExactAverage, MedianEstimate }
public sealed record MedianObservation(DateTimeOffset At,long BuyerPays,int Volume);
public sealed record PriceEvidence(string Currency,long Minor,DateTimeOffset CapturedAt,DateTimeOffset WindowStart,DateTimeOffset WindowEnd,int Buckets,long Volume,PriceBasis Basis,string SampleDigest="")
{
    public string Source=>"Steam pricehistory bucket medians (unofficial)";
    public string Method=>"Lower volume-weighted median of period medians; not exact trade VWAP";
    public bool Fresh(DateTimeOffset now)=>SampleDigest is {Length:64}&&SampleDigest.All(Uri.IsHexDigit)&&Buckets>0&&Volume>0&&Minor>0&&CapturedAt<=now.AddSeconds(5)&&now-CapturedAt<=TimeSpan.FromMinutes(5)&&WindowEnd<=CapturedAt&&WindowStart<WindowEnd;
}
public static class PriceIndicators
{
    // Lower weighted median of bucket medians; neither a true VWAP nor the median of all individual trades.
    public static PriceEvidence? MedianEstimate(IEnumerable<MedianObservation> observations,string currency,DateTimeOffset captured,TimeSpan window)
    {
        _=new Money(0,currency);if(window<=TimeSpan.Zero||window>TimeSpan.FromDays(30))throw new ArgumentException("IndicatorWindow");
        var source=observations.ToArray();if(source.Length>20000||source.Any(x=>x.Volume<=0||x.BuyerPays<=0||x.At>captured.AddSeconds(5))||source.Select(x=>x.At).Distinct().Count()!=source.Length)throw new ArgumentException("IndicatorSamples");
        var rows=source.Where(x=>x.At>=captured-window&&x.At<=captured).OrderBy(x=>x.BuyerPays).ToArray();if(rows.Length==0)return null;
        var digest=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join(";",rows.OrderBy(x=>x.At).Select(x=>FormattableString.Invariant($"{x.At.UtcTicks}:{x.BuyerPays}:{x.Volume}"))))));
        var total=rows.Sum(x=>(long)x.Volume);long accumulated=0;
        foreach(var row in rows){accumulated=checked(accumulated+row.Volume);if((decimal)accumulated*2>=total)return new(currency,row.BuyerPays,captured,captured-window,captured,rows.Length,total,PriceBasis.MedianEstimate,digest);}
        throw new InvalidOperationException("IndicatorInvalid");
    }
    public static long MajorToMinor(decimal major,string currency)
    {
        var minor=currency switch{"KRW"=>major,"USD"=>major*100,_=>throw new ArgumentException("IndicatorCurrency")};
        if(minor<=0||minor!=decimal.Truncate(minor))throw new ArgumentException("IndicatorFraction");return checked((long)minor);
    }
}
