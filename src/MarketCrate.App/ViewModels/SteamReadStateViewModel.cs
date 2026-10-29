using System.Collections.ObjectModel;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;

namespace MarketCrate.App.ViewModels;

public enum SteamReadState { NotQueried, Loading, Ready, Empty, Failed, Unsupported, AccessBlocked, Cancelled, Stale }
public sealed class SteamReadStatus(string name,string endpoint):Observable
{
    public string Name{get;}=name;public string Endpoint{get;}=endpoint;
    public SteamReadState State{get;private set;}public SteamFailure? Failure{get;private set;}public Guid? OperationId{get;private set;}public DateTimeOffset? At{get;private set;}
    public string Summary=>State switch{SteamReadState.Loading=>"조회 중 · 기존 자료 보존",SteamReadState.Ready=>"조회 완료",SteamReadState.Empty=>"자료 없음",SteamReadState.Unsupported=>"경로 미지원 · 게임/context 확인",SteamReadState.Failed=>Failure==SteamFailure.ContractChanged?"응답 형식 확인 필요 · 수동 재시도 가능":"통신 실패 · 수동 재시도 가능",SteamReadState.AccessBlocked=>Failure==SteamFailure.Authorization?"이 조회의 읽기 권한 확인 필요 · 재요청 중단":"요청 제한 · 재요청 중단",SteamReadState.Cancelled=>"중단됨 · 기존 자료 보존",SteamReadState.Stale=>"다시 조회 필요",_=>"아직 조회하지 않음"};
    public void Update(SteamReadState value,SteamFailure? failure=null)
    {
        var old=State;State=value;Failure=failure;OperationId=DiagnosticHub.Context?.OperationId;At=DateTimeOffset.UtcNow;
        DiagnosticHub.Emit(DiagnosticEvent.ReadState,DiagnosticArea.Data,value==SteamReadState.Failed?DiagnosticOutcome.Failed:value==SteamReadState.AccessBlocked?DiagnosticOutcome.Blocked:DiagnosticOutcome.Completed,
            value is SteamReadState.Failed or SteamReadState.AccessBlocked?LogLevel.Warning:LogLevel.Information,failure is {} code?SteamDiagnostics.Classify(code):DiagnosticError.None,endpoint:Endpoint,from:old.ToString(),to:value.ToString(),requirement:"A04");
        Raise(nameof(State));Raise(nameof(Failure));Raise(nameof(Summary));Raise(nameof(OperationId));Raise(nameof(At));
    }
}
public sealed partial class SteamWorkspaceViewModel
{
    public string InventoryEmptyTitle=>InventoryRead.State==SteamReadState.Loading?"보관함을 불러오는 중입니다":!Connected?"Steam 계정을 연결하세요":InventoryRead.State==SteamReadState.Empty?"서버에서 빈 보관함을 확인했습니다":InventoryRead.State is SteamReadState.Failed or SteamReadState.Unsupported or SteamReadState.AccessBlocked?"보관함을 표시할 수 없습니다":"표시할 아이템이 없습니다";
    public string InventoryEmptyHint=>InventoryRead.State==SteamReadState.Loading?"조회 중입니다. 아래 작업 상태에서 진행 여부를 확인하거나 중단할 수 있습니다.":!Connected?"계정 및 설정에서 로그인한 뒤 보관함을 직접 갱신하세요.":InventoryRead.State==SteamReadState.Empty?"선택한 보관함의 명시적 빈 응답입니다. 다른 보관함이나 게임/context를 확인하세요.":InventoryRead.State is SteamReadState.Failed or SteamReadState.Unsupported or SteamReadState.AccessBlocked?"조회 상태와 문제 해결을 확인하세요. 오류 응답을 빈 보관함으로 바꾸지 않습니다.":"보관함을 갱신하거나 검색·판매 가능 필터를 확인하세요.";
    public SteamReadStatus InventoryRead{get;}=new("보관함","Inventory");
    public SteamReadStatus ListingsRead{get;}=new("판매 중","Listings");
    public SteamReadStatus WalletRead{get;}=new("지갑 통화","Wallet");
    public SteamReadStatus FeesRead{get;}=new("지갑 · 수수료","Fees");
    public SteamReadStatus PricesRead{get;}=new("구매주문 · 가격 지표","Orderbook");
    public SteamReadStatus HistoryRead{get;}=new("거래가격 이력","History");
    public IEnumerable<SteamReadStatus> ReadStatuses=>[InventoryRead,ListingsRead,WalletRead,FeesRead,PricesRead,HistoryRead];
    private bool authenticating;
    public bool SessionPresent=>workspace?.Authentication.HasSession==true;
    public bool RequestsBlocked=>workspace?.Failure is not null;
    public bool CanReadFeature(string endpoint)=>CanRead&&workspace?.FeatureFailure(endpoint) is null;
    public bool FeatureRestricted=>new[]{"Wallet","Fees","FeeContext","Inventory","Listings","Orderbook","History"}.Any(x=>workspace?.FeatureFailure(x)is not null);
    public bool FeatureContractMismatch=>new[]{"Wallet","Fees","FeeContext","Inventory","Listings","Orderbook","History"}.Any(x=>workspace?.FeatureFailure(x)==SteamFailure.ContractChanged);
    public bool CanRead=>Connected&&!RequestsBlocked;
    public string AuthenticationText=>LoginState switch{SteamLoginState.Connected when Connected=>"Steam 인증 완료",SteamLoginState.AwaitingMobile=>"QR 승인 대기",SteamLoginState.RequestingQr=>"QR 요청 중",SteamLoginState.Restoring=>"저장 로그인 복원 중",SteamLoginState.Expired=>"세션 만료 · 다시 로그인하세요",SteamLoginState.Denied=>"인증 거절됨",SteamLoginState.AccessBlocked=>"인증 요청 제한",SteamLoginState.NetworkFailure=>"인증 통신 실패",SteamLoginState.ContractFailure=>"인증 응답 확인 필요",SteamLoginState.Cancelled=>"인증 취소됨",_=>"연결 안 됨"};
    public string ConnectionSummary=>RequestsBlocked&&Connected?"Steam 인증 완료 · 요청은 안전을 위해 중단됨":!Connected?AuthenticationText:FeatureContractMismatch?"Steam 인증 완료 · 일부 응답 계약 확인 필요":FeatureRestricted?"Steam 인증 완료 · 일부 조회 권한 확인 필요":Busy?"Steam 인증 완료 · 데이터 조회 중":ReadStatuses.Any(x=>x.State is SteamReadState.Failed or SteamReadState.Unsupported or SteamReadState.AccessBlocked)?"Steam 인증 완료 · 일부 조회 실패":"Steam 인증 완료 · 읽기 연결";
    public string RequestAccessText=>workspace?.Failure switch{SteamFailure.Forbidden=>"서버 접근 제한(403) · 요청을 중단했습니다. 제한을 우회하지 않습니다.",SteamFailure.RateLimited=>"서버 호출 제한(429) · 요청을 중단했습니다. 자동 재시도하지 않습니다.",SteamFailure.ContractChanged=>"응답 계약 불일치 · 추가 요청을 중단했습니다. 인증 만료나 CAPTCHA로 단정하지 않습니다.",SteamFailure.Authentication or SteamFailure.Expired=>"세션 만료 · 계정 연결에서 직접 다시 로그인하세요.",{}=>"안전 차단 · 진단에서 원인을 확인하세요. 재전송하지 않습니다.",_=>FeatureContractMismatch?"일부 조회의 응답 형식이 예상 계약과 다릅니다. 해당 요청은 다시 전송하지 않으며 다른 독립 조회는 사용할 수 있습니다.":FeatureRestricted?"일부 조회의 권한 확인이 필요합니다. 제한된 요청은 차단하고 다른 독립 조회만 사용할 수 있습니다.":Connected?"조회 실패는 인증과 별개입니다. 실패한 조회만 직접 다시 시도할 수 있습니다.":"계정 연결에서 QR 인증을 직접 시작하세요."};
    public string SelectionSummary=>$"선택 {SelectedSteamRows().Length}종 · {SelectedSteamRows().Sum(x=>(long)x.Quantity)}개";
    public string SelectionEstimate
    {
        get
        {
            if(fees is null)return "판매가격을 계산할 수 없습니다. 현재 계정의 수수료 계약을 확인하지 못했습니다. 지갑·수수료를 갱신하세요.";
            var rows=SelectedSteamRows();if(rows.Length==0)return "판매할 항목을 선택하세요.";
            if(rows.Any(x=>x.Price is null))return "선택 시세를 갱신한 뒤 최종 검토하세요.";
            try
            {
                var basis=PricingMode switch{1=>PriceBasis.ExactAverage,2=>PriceBasis.MedianEstimate,_=>PriceBasis.Immediate};
                var plan=SalePlanner.Build(rows.Select(x=>new SaleSelection(x.Asset,x.Price!,x.Quantity)),basis);
                long buyer=0,fee=0,seller=0;foreach(var line in plan.Lines){var schedule=FeeFor(line.Selection.Asset);if(schedule.Currency!=fees.Currency)throw new SteamException(SteamFailure.Restricted);var quote=schedule.FromBuyer(line.Allocation.BuyerPays,false);buyer=checked(buyer+checked(quote.BuyerPays*line.Allocation.Quantity));fee=checked(fee+checked(quote.Fees*line.Allocation.Quantity));seller=checked(seller+checked(quote.SellerReceives*line.Allocation.Quantity));}
                return $"예상 결제액 {new Money(buyer,fees.Currency)}\n예상 수수료 {new Money(fee,fees.Currency)}\n예상 실수령 {new Money(seller,fees.Currency)}\n가격 미정 {plan.UnpricedQuantity}개 · 최종 검토에서 확정";
            }
            catch(Exception error)when(error is ArgumentException or InvalidOperationException or OverflowException or SteamException){return "시세·수수료 자료가 부족합니다. 갱신 후 검토하세요.";}
        }
    }
    private SteamReadStatus? ReadFor(string method)=>method switch{nameof(LoadAsync)=>InventoryRead,nameof(LoadListingsAsync) or nameof(RecoverAsync)=>ListingsRead,nameof(LoadFeesAsync)=>FeesRead,nameof(LoadWalletAsync)=>WalletRead,nameof(RefreshPricesAsync) or nameof(ListingMetricsAsync)=>PricesRead,nameof(HistoryAsync)=>HistoryRead,_=>null};
    private void ReadFailed(SteamReadStatus read,Exception error){var code=error is SteamException e?e.Code:SteamFailure.ContractChanged;read.Update(code==SteamFailure.Unsupported?SteamReadState.Unsupported:code==SteamFailure.Authorization?SteamReadState.AccessBlocked:RequestsBlocked?SteamReadState.AccessBlocked:SteamReadState.Failed,code);if(ReferenceEquals(read,FeesRead)){fees=null;gameFees.Remove(Game.AppId);Raise(nameof(CurrencyText));}Changed();}
    private async Task IndependentRead(SteamReadStatus read,Func<Task> action)
    {
        if(!CanRead)return;read.Update(SteamReadState.Loading);
        try{await action();read.Update(read==ListingsRead&&Listings.Count==0?SteamReadState.Empty:SteamReadState.Ready);}
        catch(OperationCanceledException){read.Update(SteamReadState.Cancelled);throw;}
        catch(Exception error){ReadFailed(read,error);Error(error);}
    }
}

public static class SteamStatusText
{
    public static string Label(SaleState value)=>value switch{SaleState.Draft=>"검토 전",SaleState.Ready=>"준비됨",SaleState.Submitting=>"처리 중",SaleState.Submitted=>"접수됨 · 게시 미확인",SaleState.AwaitingSteamGuard=>"모바일 승인 대기",SaleState.Listed=>"판매 등록 확인",SaleState.Failed=>"실패",SaleState.Unknown=>"결과 불명 · 대조 필요",SaleState.Reconciled=>"대조 완료",_=>"확인 필요"};
}
