using MarketCrate.Core;

namespace MarketCrate.App.ViewModels;

public sealed partial class SteamWorkspaceViewModel
{
    private string officialSeller="",officialBuyer="",feeComparison="공식 장터에서 같은 게임·통화·단가를 직접 확인해 비교하세요. 이 입력은 판매 계획이나 거래 권한을 변경하지 않습니다.";
    public string OfficialSeller{get=>officialSeller;set{if(Set(ref officialSeller,value))InvalidateFeeComparison();}}
    public string OfficialBuyer{get=>officialBuyer;set{if(Set(ref officialBuyer,value))InvalidateFeeComparison();}}
    public string FeeComparison{get=>feeComparison;private set=>Set(ref feeComparison,value);}
    public string FeeComparisonAvailability=>CanCompareFee?"현재 선택 게임의 조회된 수수료로 단가만 비교합니다. 입력은 KRW 원 / USD 달러입니다.":"비교 불가 · 현재 게임의 지갑 통화·최소 단위·수수료 조회가 먼저 필요합니다. 임의 수수료는 사용하지 않습니다.";
    private bool CanCompareFee=>Connected&&!Busy&&!AnyReview&&FeesRead.State==SteamReadState.Ready&&fees is not null&&gameFees.GetValueOrDefault(Game.AppId)==fees;
    public Command CompareOfficialFees{get;private set;}=null!;
    private void InitializeFeeComparison()=>CompareOfficialFees=new(CompareFee,()=>CanCompareFee);
    private void InvalidateFeeComparison()=>FeeComparison="입력 또는 조회 상태 변경 · 다시 비교하세요. 비교 결과는 실거래 검증이나 활성화 근거가 아닙니다.";
    private void CompareFee()
    {
        try
        {
            var schedule=fees!;
            var seller=SteamReviewRow.ParsePrice(OfficialSeller,schedule.Currency);
            var buyer=SteamReviewRow.ParsePrice(OfficialBuyer,schedule.Currency);
            var quote=schedule.FromSeller(seller);
            var same=buyer==quote.BuyerPays;
            FeeComparison=$"{(same?"입력한 공식 화면 값과 계산 일치":"불일치 · 판매하지 말고 통화·게임·수수료 계약을 확인하세요")} · 실수령 {new Money(seller,schedule.Currency)} · 계산 수수료 {new Money(quote.Fees,schedule.Currency)} · 계산 구매자 {new Money(quote.BuyerPays,schedule.Currency)} · 사용자 입력 비교이며 서버 검증/거래 활성화가 아닙니다.";
            DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Pricing,same?DiagnosticOutcome.Completed:DiagnosticOutcome.Failed,
                same?LogLevel.Information:LogLevel.Warning,same?DiagnosticError.None:DiagnosticError.Validation,requirement:"D10",reason:"OfficialFeeComparison",currency:schedule.Currency);
        }
        catch(Exception error)when(error is ArgumentException or FormatException or OverflowException)
        {FeeComparison="금액 형식·최소 가격·가격 증분을 확인하세요. 판매 계획은 변경되지 않았습니다.";DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.Pricing,DiagnosticOutcome.Failed,LogLevel.Warning,DiagnosticError.Validation,reason:"OfficialFeeComparison");}
    }
}
