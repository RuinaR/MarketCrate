using MarketCrate.Core;
using System.Collections.ObjectModel;

namespace MarketCrate.App.ViewModels;

public sealed partial class SteamWorkspaceViewModel
{
    public ObservableCollection<OfficialFeeCase> OfficialCases{get;}=[];
    private string feeComparison="공식 장터에서 같은 게임·통화·단가를 직접 확인해 비교하세요. 이 입력은 판매 계획이나 거래 권한을 변경하지 않습니다.";
#if MARKETCRATE_DEMO
    private string officialSeller="",officialBuyer="";
    public string OfficialSeller{get=>officialSeller;set{if(Set(ref officialSeller,value))InvalidateFeeComparison();}}
    public string OfficialBuyer{get=>officialBuyer;set{if(Set(ref officialBuyer,value))InvalidateFeeComparison();}}
#endif
    public string FeeComparison{get=>feeComparison;private set=>Set(ref feeComparison,value);}
    public string FeeComparisonAvailability=>CanCompareFee?"현재 선택 게임의 조회된 수수료로 단가만 비교합니다. 입력은 KRW 원 / USD 달러입니다.":"비교 불가 · 현재 게임의 지갑 통화·최소 단위·수수료 조회가 먼저 필요합니다. 임의 수수료는 사용하지 않습니다.";
    private bool CanCompareFee=>Connected&&!Busy&&!AnyReview&&FeesRead.State==SteamReadState.Ready&&fees is not null&&gameFees.GetValueOrDefault(Game.AppId)==fees;
    public Command CompareOfficialFees{get;private set;}=null!;
    private void InitializeFeeComparison(){CompareOfficialFees=new(CompareFee,()=>CanCompareFee);for(var i=0;i<3;i++)OfficialCases.Add(new(i==0?"최소 가격":i==1?"일반 가격":"다른 가격 구간",InvalidateFeeComparison));}
    private void InvalidateFeeComparison(){FeeComparison="입력 또는 조회 상태 변경 · 다시 비교하세요. 비교 결과는 실거래 검증이나 활성화 근거가 아닙니다.";foreach(var row in OfficialCases)row.Result="입력/조회 변경 · 재비교 필요";}
    private void CompareFee()
    {
        try
        {
            var schedule=fees!;
            var same=true;
            FeeComparison="사용자 입력 비교이며 서버 검증/거래 활성화가 아닙니다.";
#if MARKETCRATE_DEMO
            // Retained for existing developer callers; the product UI uses the independent cases.
            if(OfficialSeller.Length>0||OfficialBuyer.Length>0)
            {
                var seller=SteamReviewRow.ParsePrice(OfficialSeller,schedule.Currency);var buyer=SteamReviewRow.ParsePrice(OfficialBuyer,schedule.Currency);
                var quote=schedule.FromSeller(seller);same=buyer==quote.BuyerPays;
                FeeComparison=$"{(same?"입력한 공식 화면 값과 계산 일치":"불일치 · 판매하지 말고 통화·게임·수수료 계약을 확인하세요")} · 실수령 {new Money(seller,schedule.Currency)} · 계산 수수료 {new Money(quote.Fees,schedule.Currency)} · 계산 구매자 {new Money(quote.BuyerPays,schedule.Currency)} · "+FeeComparison;
            }
#endif
            var full=0;var prices=new HashSet<long>();
            foreach(var row in OfficialCases)
            {
                row.Result="미입력 · 미검증";if(!row.HasInput)continue;
                try
                {
                    var net=SteamReviewRow.ParsePrice(row.Seller,schedule.Currency);var gross=SteamReviewRow.ParsePrice(row.Buyer,schedule.Currency);
                    var steam=SteamReviewRow.ParsePrice(row.Steam,schedule.Currency);var publisher=SteamReviewRow.ParsePrice(row.Publisher,schedule.Currency);
                    var expected=schedule.FromSeller(net);var match=expected.BuyerPays==gross&&expected.SteamFee==steam&&expected.GameFee==publisher;
                    var minimum=Money.FromWire(schedule.MinimumWire,schedule.Currency).Minor;
                    var band=net==minimum?"최소 판매가 사례":expected.SteamFee==minimum||expected.GameFee==minimum?"최소 수수료 적용 구간":"일반 수수료 구간";
                    row.Result=$"{Game.Name} / {schedule.Currency} · {(match?"4금액 일치":"불일치 · 판매 중단")} · {band} · 계산 Steam {new Money(expected.SteamFee,schedule.Currency)}, 게임 {new Money(expected.GameFee,schedule.Currency)} · 증분 {new Money(Money.FromWire(schedule.IncrementWire,schedule.Currency).Minor,schedule.Currency)} · {DateTimeOffset.UtcNow:HH:mm} UTC";
                    if(match){full++;prices.Add(net);}else same=false;
                }
                catch(Exception error)when(error is ArgumentException or FormatException or OverflowException){same=false;row.Result="판매자·구매자·Steam·게임 금액을 모두 같은 통화로 입력하세요. 0 수수료도 직접 확인하세요.";}
            }
            FeeComparison+=$"\n상세 대조 {full}/3개 · 서로 다른 단가 {prices.Count}개. 최소 가격·일반 구간·다른 구간 및 다른 게임은 각각 직접 확인해야 합니다. 사용자 입력은 서버 증거나 거래 허가가 아니며 다른 가격/게임으로 일반화하지 않습니다.";
            DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Pricing,same?DiagnosticOutcome.Completed:DiagnosticOutcome.Failed,
                same?LogLevel.Information:LogLevel.Warning,same?DiagnosticError.None:DiagnosticError.Validation,requirement:"D10",reason:"OfficialFeeComparison",currency:schedule.Currency);
        }
        catch(Exception error)when(error is ArgumentException or FormatException or OverflowException)
        {FeeComparison="금액 형식·최소 가격·가격 증분을 확인하세요. 판매 계획은 변경되지 않았습니다.";DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.Pricing,DiagnosticOutcome.Failed,LogLevel.Warning,DiagnosticError.Validation,reason:"OfficialFeeComparison");}
    }
}

public sealed class OfficialFeeCase(string label,Action changed):Observable
{
    private string seller="",buyer="",steam="",publisher="",result="미입력 · 미검증";
    public string Label{get;}=label;
    public string Seller{get=>seller;set{if(Set(ref seller,value))changed();}}
    public string Buyer{get=>buyer;set{if(Set(ref buyer,value))changed();}}
    public string Steam{get=>steam;set{if(Set(ref steam,value))changed();}}
    public string Publisher{get=>publisher;set{if(Set(ref publisher,value))changed();}}
    public string Result{get=>result;set=>Set(ref result,value);}
    public bool HasInput=>Seller.Length+Buyer.Length+Steam.Length+Publisher.Length>0;
}
