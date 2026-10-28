namespace MarketCrate.Core;

// User-facing terminology and migration boundary. Price calculations use PriceBasis, never these labels.
public static class PriceDisplay
{
    public const string Immediate = "즉시거래가능가";
    public const string ImmediateExplanation = "현재 구매주문 기준 예상 가격이며 실제 즉시 체결을 보장하지 않습니다.";
    public const string MockAverage = "24시간 거래량 가중 평균가";
    public const string UnavailableAverage = "최근 정확한 평균 (원자료 없어 이용 불가)";
    public const string MedianEstimate = "중앙값 기반 추정 (정확한 평균 아님)";
    public static string Difference(long proposed, long listed, string currency)
    {
        var difference = checked(proposed-listed);
        return (difference>=0?"+":"−")+new Money(Math.Abs(difference),currency);
    }
    public static bool TryReadSetting(string? value, out PriceBasis basis)
    {
        basis = PriceBasis.Immediate;
        if (value is "Immediate" or Immediate or "구매주문 기반 예상가" or "즉시판매 예상가" or "즉시판매가") return true;
        if (value is "ExactAverage" or MockAverage) { basis = PriceBasis.ExactAverage; return true; }
        return false;
    }
}
