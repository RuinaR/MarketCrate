using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.App.ViewModels;

public sealed partial class SteamWorkspaceViewModel
{
    private string tradingConfirmation="";
    private string tradingPreparation="기본 OFF · 선택 게임의 읽기 계약·계정·수수료를 확인한 뒤 이 실행 세션에서만 활성화할 수 있습니다.";
    public string TradingConfirmation {get=>tradingConfirmation;set{Set(ref tradingConfirmation,value);PrepareTrading?.Refresh();}}
    public string TradingPreparation {get=>tradingPreparation;private set=>Set(ref tradingPreparation,value);}
    private string CaptureRepriceBook(SteamListing listing)
    {
        var asset=new MarketCrate.Core.InventoryAsset(listing.Asset,"Steam",listing.HashName,listing.HashName,listing.Quantity,listing.Quantity>1,true,false);
        var price=workspace!.Prices.Get(asset);
        if(price.Currency!=listing.Currency||DateTimeOffset.UtcNow-price.CapturedAt>TimeSpan.FromMinutes(5))throw new SteamException(SteamFailure.StaleReview);
        return MarketCrate.Core.OrderbookFingerprint.Of(price);
    }
    public AsyncCommand PrepareTrading {get;private set;}=null!;
    private void InitializeTrading()=>PrepareTrading=new(PrepareTradingAsync,Error,()=>!syntheticConnection&&!Busy&&!AnyReview&&CanRead&&TradeAccepted&&TradingConfirmation=="실제 거래");
    public Task PrepareTradingAsync()=>Run(async token=>
    {
        RequireConnection();
        if(syntheticConnection||!TradeAccepted||TradingConfirmation!="실제 거래")throw new SteamException(SteamFailure.Disabled);
        TradingConfirmation="";
        TradingPreparation="선택 게임의 계정·지갑·수수료·자산·판매 화면 설정을 읽기 전용으로 재확인 중 · 거래 전송 없음";
        try
        {
            await workspace!.EnableUserTradingAsync(Game.AppId,Game.ContextId,Game.Name,RiskAccepted&&CredentialsAccepted,TradeAccepted,token);
            TradingPreparation="비공식 실제 거래 모드 · 이 세션 한정 · Valve 허용/실거래 검증 완료를 뜻하지 않습니다. 각 배치는 최종 검토 후 한 번 승인해야 합니다.";
        }
        catch(SteamException)
        {
            TradingPreparation="활성화하지 않았습니다. 계정·지갑 단위·게임별 수수료·판매 가능 자산·공식 판매 화면 설정을 확인해야 합니다. 문제 해결에서 실패 단계를 확인하세요. 자동 재시도하지 않습니다.";
            throw;
        }
        finally{Changed();}
    });
}
