using MarketCrate.Core;
namespace MarketCrate.App.ViewModels;

public sealed partial class SteamWorkspaceViewModel
{
    private bool lowerPriceOneStep;
    public bool CanChoosePriceAdjustment=>Connected&&!Busy&&!AnyReview;
    public bool LowerPriceOneStep
    {
        get=>lowerPriceOneStep;
        set
        {
            if(!CanChoosePriceAdjustment||!Set(ref lowerPriceOneStep,value))return;
            priceRevision++;listingPlanRevision++;
            foreach(var row in Listings)row.ClearPriceComparison();
            ListingPriceStatus="가격 조정 변경 · 재등록 가격을 다시 계산하고 전체 검토하세요.";
            Raise(nameof(PriceAdjustmentHint));Changed();
            DiagnosticHub.Emit(DiagnosticEvent.PricePlan,DiagnosticArea.Pricing,requirement:"E05",reason:value?"LowerValidStep":"Immediate");
        }
    }
    public string PriceAdjustmentHint=>LowerPriceOneStep?"각 호가를 유효 가격으로 환산한 뒤 한 단계 더 낮추며, 더 낮출 수 없으면 최저 유효 가격을 유지합니다. 수수료·실수령 감소를 최종 검토하세요. 실제 체결은 보장하지 않습니다.":"기본: 즉시거래가능가 유지. 하향 옵션은 직접 선택해야 적용됩니다.";
    public string ReviewAdjustmentSummary
    {
        get
        {
            var rows=ReviewRows.Where(x=>x.LowerStepRequested).ToArray();if(rows.Length==0)return "";
            if(rows.Any(x=>!x.Valid||x.BaselineQuote is null))return "한 단계 하향: 계산 불가·호가 부족 또는 입력 오류가 있는 항목을 확인하세요. 해당 항목을 제외하거나 원래 가격 옵션으로 새로 검토해야 합니다.";
            try
            {
                var baseline=new FeeQuote(rows.Sum(x=>checked(x.BaselineQuote!.BuyerPays*x.Quantity)),rows.Sum(x=>checked(x.BaselineQuote!.SellerReceives*x.Quantity)),rows.Sum(x=>checked(x.BaselineQuote!.SteamFee*x.Quantity)),rows.Sum(x=>checked(x.BaselineQuote!.GameFee*x.Quantity)));
                var actual=new FeeQuote(rows.Sum(x=>checked(x.Quote.BuyerPays*x.Quantity)),rows.Sum(x=>checked(x.Quote.SellerReceives*x.Quantity)),rows.Sum(x=>checked(x.Quote.SteamFee*x.Quantity)),rows.Sum(x=>checked(x.Quote.GameFee*x.Quantity)));
                return (rows.Any(x=>x.MinimumPricePinned)?$"최저 유효 가격 유지 {rows.Count(x=>x.MinimumPricePinned)}행 · 더 낮출 수 없는 항목 포함\n":"")+"한 단계 하향 선택 · 호가 유지 대비 전체 합계\n"+PriceComparisonText.Format(baseline,actual,rows[0].Currency,1,true);
            }
            catch(OverflowException){return "하향 비교 합계가 지원 범위를 초과합니다. 수량·가격을 줄여 새로 검토하세요.";}
        }
    }
    private void ResetPriceAdjustment(){lowerPriceOneStep=false;Raise(nameof(LowerPriceOneStep));Raise(nameof(PriceAdjustmentHint));}
}

internal static class PriceComparisonText
{
    public static string Format(FeeQuote baseline,FeeQuote actual,string currency,int quantity,bool totals=false)
    {
        var difference=checked((baseline.SellerReceives-actual.SellerReceives)*quantity);
        var percent=baseline.SellerReceives==0?0:(baseline.SellerReceives-actual.SellerReceives)*100m/baseline.SellerReceives;
        var change=difference>=0?$"실수령 감소 {new Money(difference,currency)} ({percent:0.##}%)":$"실수령 증가 {new Money(-difference,currency)} ({-percent:0.##}%)";
        return $"구매자 {new Money(baseline.BuyerPays,currency)} → {new Money(actual.BuyerPays,currency)} · 수수료 {new Money(baseline.Fees,currency)} → {new Money(actual.Fees,currency)}\nSteam {new Money(baseline.SteamFee,currency)} → {new Money(actual.SteamFee,currency)} / 게임 {new Money(baseline.GameFee,currency)} → {new Money(actual.GameFee,currency)}\n실수령 {new Money(baseline.SellerReceives,currency)} → {new Money(actual.SellerReceives,currency)} · {(totals?"전체":quantity+"개")} {change}";
    }
}
