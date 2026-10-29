using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.App.ViewModels;

public sealed partial class SteamWorkspaceViewModel
{
    private string tradingConfirmation="";
    private string tradingPreparation="실거래 모드 기본 선택 · 최종 가격 검토 시 계정·수수료·자산을 검증하며, 전체 판매등록 승인 전에는 전송하지 않습니다.";
    private AsyncCommand prepareReview=null!;
    private bool LiveReviewRequested=>!syntheticConnection&&TradeAccepted&&TradingConfirmation=="실제 거래";
    private bool NeedsLiveReviewPreparation=>LiveReviewRequested&&(!CanTrade||SelectedSteamRows().Any(x=>workspace?.Policy.VerifiedApps.Contains(x.Asset.Key.AppId)!=true));
    public string TradingConfirmation {get=>tradingConfirmation;set{Set(ref tradingConfirmation,value);PrepareTrading?.Refresh();PrepareCancellation?.Refresh();}}
    public string TradingPreparation {get=>tradingPreparation;private set=>Set(ref tradingPreparation,value);}
    private string CaptureRepriceBook(SteamListing listing)
    {
        var asset=new MarketCrate.Core.InventoryAsset(listing.Asset,"Steam",listing.HashName,listing.HashName,listing.Quantity,listing.Quantity>1,true,false);
        var price=workspace!.Prices.Get(asset);
        if(price.Currency!=listing.Currency||DateTimeOffset.UtcNow-price.CapturedAt>TimeSpan.FromMinutes(5))throw new SteamException(SteamFailure.StaleReview);
        return MarketCrate.Core.OrderbookFingerprint.Of(price);
    }
    public AsyncCommand PrepareTrading {get;private set;}=null!;
    public AsyncCommand PrepareCancellation{get;private set;}=null!;
    private void InitializeTrading()
    {
        prepareReview=new(OpenTradingReviewAsync,Error,()=>!Busy&&!AnyReview&&CanRead);
        PrepareTrading=new(PrepareTradingAsync,Error,()=>!syntheticConnection&&!Busy&&!AnyReview&&CanRead&&TradeAccepted&&TradingConfirmation=="실제 거래");
        PrepareCancellation=new(PrepareCancellationAsync,Error,()=>!syntheticConnection&&!Busy&&!AnyReview&&CanRead&&TradeAccepted&&TradingConfirmation=="실제 취소");
    }
    public Task OpenTradingReviewAsync()=>Run(async token=>
    {
        RequireConnection();
        if(NeedsLiveReviewPreparation)
        {
            var scopes=SelectedSteamRows().Select(x=>x.Asset).DistinctBy(x=>(x.Key.AppId,x.Key.ContextId)).ToArray();
            if(scopes.Length==0||scopes.Length>10)throw new SteamException(SteamFailure.Restricted);
            foreach(var scope in scopes)
            {
                OperationLabel=$"{scope.Game} · 실거래 서버 계약 확인 중";
                try{await workspace!.EnableUserTradingAsync(scope.Key.AppId,scope.Key.ContextId,scope.Game,RiskAccepted&&CredentialsAccepted,TradeAccepted,token);}
                catch(SteamException error){TradingPreparation=TradingFailureText(error);throw;}
            }
            token.ThrowIfCancellationRequested();
            TradingPreparation="서버 계약 확인 · 이번 세션 전송 권한 준비 · 최종 전체 판매등록 승인 대기";
        }
        BuildReview();
    });
    public Task PrepareCancellationAsync()=>Run(async token=>
    {
        RequireConnection();if(syntheticConnection||!TradeAccepted||TradingConfirmation!="실제 취소")throw new SteamException(SteamFailure.Disabled);
        TradingConfirmation="";
        try{await workspace!.EnableUserCancellationAsync(RiskAccepted&&CredentialsAccepted,TradeAccepted,token);TradingPreparation="취소 전용 활성 · 이 실행 세션 한정 · 새 판매등록·재가격은 비활성입니다. 선택 취소는 별도 전체 검토에서 승인하세요.";}
        catch{TradingPreparation="취소 전용을 활성화하지 않았습니다. 인증된 판매목록·서버 제한을 확인하세요. 자동 재시도하지 않습니다.";throw;}
        finally{Changed();}
    });
    public Task PrepareTradingAsync()=>Run(async token=>
    {
        RequireConnection();
        if(syntheticConnection||!TradeAccepted||TradingConfirmation!="실제 거래")throw new SteamException(SteamFailure.Disabled);
        TradingPreparation="선택 게임의 계정·지갑·수수료·자산·판매 화면 설정을 읽기 전용으로 재확인 중 · 거래 전송 없음";
        try
        {
            await workspace!.EnableUserTradingAsync(Game.AppId,Game.ContextId,Game.Name,RiskAccepted&&CredentialsAccepted,TradeAccepted,token);
            TradingPreparation="비공식 실제 거래 모드 · 이 세션 한정 · Valve 허용/실거래 검증 완료를 뜻하지 않습니다. 각 배치는 최종 검토 후 한 번 승인해야 합니다."+(HasPendingInventoryRequests||workspace.PendingSubmissionAssets>0?" 이전 미확정 요청이 있는 항목은 새 전송 전에 상태 확인이 필요합니다.":"");
        }
        catch(SteamException error)
        {
            TradingPreparation=TradingFailureText(error);
            throw;
        }
        finally{Changed();}
    });
    public static string TradingFailureText(SteamException error)
    {
        var step=error.TradingCheck switch
        {
            SteamTradingCheck.Consent=>"로그인·이번 실행의 거래 동의",
            SteamTradingCheck.WalletIdentity=>"지갑과 현재 로그인 계정",
            SteamTradingCheck.FeeContract=>"게임별 수수료 계약",
            SteamTradingCheck.WalletConsistency=>"지갑 통화·최소 가격·가격 증분 일치",
            SteamTradingCheck.InventoryRead=>"선택한 게임의 보관함 조회",
            SteamTradingCheck.SaleableAssets=>"서버 판매 가능 자산과 이전 전송 충돌 여부",
            SteamTradingCheck.PublisherFees=>"아이템별 게임 수수료와 견적의 일치",
            SteamTradingCheck.ListingsRead=>"현재 판매목록 조회",
            SteamTradingCheck.ListingFees=>"기존 판매목록과 수수료 계산의 일치",
            SteamTradingCheck.SessionCheck=>"처리 중 로그인 세션 변경 여부",
            _=>"서버 계약"
        };
        var cause=error.Code switch{SteamFailure.Authentication or SteamFailure.Expired=>"인증 확인 필요",SteamFailure.Forbidden or SteamFailure.Authorization=>"접근 권한 제한",SteamFailure.RateLimited=>"요청 제한",SteamFailure.Transport=>"통신 실패",SteamFailure.ContractChanged=>"응답 계약 불일치",SteamFailure.Unsupported=>"지원 계약 미확인",_=>"검증 조건 미충족"};
        return $"실제 거래를 활성화하지 않았습니다. 확인 단계: {step} · {cause}. 거래 요청은 보내지 않았습니다. 문제 해결에서 최근 실패 작업을 확인하세요.";
    }
}
