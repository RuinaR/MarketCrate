using MarketCrate.Core;
namespace MarketCrate.Infrastructure.Steam;

public enum ReadCheckOutcome { Unverified, Running, Succeeded, Failed, Blocked, Unsupported, Cancelled }
// No generated ToString/serialization of item identity or image token. UI captures this on approval.
public sealed class SteamReadCheckPlan(int appId,string contextId,string game,InventoryAsset? item,ItemImageReference? image,
    bool listings,bool inventory,bool wallet,bool fees,bool orderbook,bool history,bool imageData)
{
    public int AppId{get;}=appId;public string ContextId{get;}=contextId;public string Game{get;}=game;
    public InventoryAsset? Item{get;}=item;public ItemImageReference? Image{get;}=image;
    public bool Listings{get;}=listings;public bool Inventory{get;}=inventory;public bool Wallet{get;}=wallet;public bool Fees{get;}=fees;public bool Orderbook{get;}=orderbook;public bool History{get;}=history;public bool ImageData{get;}=imageData;
    public void Validate(){new InventoryScope(AppId,ContextId,Game).Validate();if(Item is {} item&&(item.Key.AppId!=AppId||item.Key.ContextId!=ContextId))throw new ArgumentException("SelfCheckScopeMismatch");if(Fees&&!Wallet)throw new ArgumentException("SelfCheckWalletRequired");}
    public override string ToString()=>"Approved read-only check plan (redacted)";
}
public enum ReadCheckClassification { None, Authentication, Authorization, AccessRestricted, ContractMismatch, Parsing, Unsupported, Empty, Transport, RateLimited }
public sealed record SteamReadCheckResult(string Step,ReadCheckOutcome Outcome,string Detail,string NextAction,DateTimeOffset At,Guid? OperationId,DiagnosticError Error=DiagnosticError.None,int? Count=null,string? Currency=null)
{
    public bool Partial{get;init;}
    public ReadCheckClassification Classification=>Error switch{DiagnosticError.Authentication=>ReadCheckClassification.Authentication,DiagnosticError.Authorization=>ReadCheckClassification.Authorization,DiagnosticError.Forbidden or DiagnosticError.Challenge=>ReadCheckClassification.AccessRestricted,DiagnosticError.Schema or DiagnosticError.Validation or DiagnosticError.Currency=>ReadCheckClassification.ContractMismatch,DiagnosticError.Parsing=>ReadCheckClassification.Parsing,DiagnosticError.Unsupported=>ReadCheckClassification.Unsupported,DiagnosticError.RateLimited=>ReadCheckClassification.RateLimited,DiagnosticError.Transport or DiagnosticError.Timeout=>ReadCheckClassification.Transport,_=>Outcome==ReadCheckOutcome.Unsupported?ReadCheckClassification.Unsupported:Outcome==ReadCheckOutcome.Succeeded&&Count==0&&!Partial?ReadCheckClassification.Empty:ReadCheckClassification.None};
}

// Deliberately has no executor/store commands. No login initiation, retries or background work.
public sealed class SteamReadSelfCheck(SteamExperimentalWorkspace workspace)
{
    public async Task RunAsync(SteamReadCheckPlan plan,Action<SteamReadCheckResult> report,CancellationToken token=default)
    {
        plan.Validate();SteamInventoryPage? inventory=null;SteamWalletObservation? wallet=null;bool stopped=false,listingsPartial=false;
        async Task Step(string id,bool enabled,Func<Task<(string Detail,int? Count,string? Currency)>> read)
        {
            if(!enabled){Report(ReadCheckOutcome.Unverified,"허용 범위에서 제외됨","원하는 경우 해당 범위만 선택하세요.");return;}
            if(token.IsCancellationRequested){stopped=true;Report(ReadCheckOutcome.Cancelled,"사용자가 중단함","자동 재시도하지 않습니다.");return;}
            if(stopped||workspace.Failure is not null||(id=="Image"&&workspace.Images.Health.Blocked)){stopped=true;Report(ReadCheckOutcome.Blocked,"앞선 제한 또는 안전 차단으로 미전송","진단을 확인하고 서버 제한을 우회하지 마세요.");return;}
            if(workspace.FeatureFailure(id) is {} feature){var category=SteamDiagnostics.Classify(feature);stopped=feature==SteamFailure.Authorization;Report(ReadCheckOutcome.Blocked,category==DiagnosticError.Schema?"해당 조회의 응답 계약 확인 필요":"해당 조회의 읽기 권한 확인 필요","다른 독립 조회는 별도로 사용할 수 있습니다. 이 요청은 다시 전송하지 않습니다.",category);return;}
            Report(ReadCheckOutcome.Running,"확인 중","기존 데이터는 변경하지 않습니다.");
            try{var result=await read();token.ThrowIfCancellationRequested();Report(ReadCheckOutcome.Succeeded,result.Detail,"이 범위의 관측이며 실거래 계약 검증은 아닙니다.",count:result.Count,currency:result.Currency);}
            catch(OperationCanceledException){stopped=true;Report(ReadCheckOutcome.Cancelled,"작업 중단","자동 재시도하지 않습니다.");}
            catch(CheckPartial partial){Report(ReadCheckOutcome.Unverified,partial.Message,"통화·최소 단위·수수료 계약을 추정하지 않습니다.",DiagnosticError.Unsupported);}
            catch(CheckMismatch mismatch){Report(ReadCheckOutcome.Failed,mismatch.Message,"자료를 직접 다시 확인하세요. 실제 거래는 차단 상태를 유지합니다.",mismatch.Error);}
            catch(CheckUnavailable missing){Report(ReadCheckOutcome.Unverified,missing.Message,"범위와 선택 품목을 확인하세요.");}
            catch(Exception error)when(error is SteamException or InvalidDataException or ArgumentException or OverflowException or InvalidOperationException)
            {
                var failure=(error as SteamException)?.Code;var category=error is SteamException steam?SteamDiagnostics.Classify(steam):DiagnosticError.Validation;
                stopped=workspace.Failure is not null||workspace.Images.Health.Blocked||failure is SteamFailure.Authentication or SteamFailure.Expired or SteamFailure.Forbidden or SteamFailure.RateLimited or SteamFailure.Challenge or SteamFailure.Denied or SteamFailure.Authorization;
                if(failure is SteamFailure.Authentication or SteamFailure.Expired)workspace.Authentication.Dispose();
                var outcome=stopped?ReadCheckOutcome.Blocked:failure==SteamFailure.Unsupported?ReadCheckOutcome.Unsupported:failure==SteamFailure.WalletUnavailable?ReadCheckOutcome.Unverified:ReadCheckOutcome.Failed;
                var htmlDetail=(error as SteamException)?.BodyKind switch{SteamBodyKind.HtmlMarket=>"JSON 대신 장터 화면 HTML 수신 · 수수료 API 적용 조건 미확인",SteamBodyKind.HtmlUnknown=>"JSON 대신 분류 불가 HTML 수신 · CAPTCHA/인증 실패로 단정하지 않음",SteamBodyKind.HtmlLogin=>"로그인 폼 HTML 수신 · 해당 조회 권한 확인 필요",SteamBodyKind.HtmlChallenge=>"보안 확인 위젯 HTML 수신 · 우회하지 않음",SteamBodyKind.HtmlAccessDenied=>"접근 거부 화면 HTML 수신",_=>null};
                Report(outcome,htmlDetail??(id=="Wallet"&&error is SteamException{Redirected:true}?"Steam이 다른 페이지로 이동시켜 지갑 조회를 중단했습니다. 지갑 미설정이나 로그인 실패로 판정한 것은 아닙니다.":category switch{DiagnosticError.Authentication=>"인증 세션 확인 필요",DiagnosticError.Authorization=>"해당 조회 읽기 권한 확인 필요 · 기존 세션 기록은 유지",DiagnosticError.Parsing=>"응답 파싱 실패",DiagnosticError.Unsupported=>"지갑 또는 가격 메타데이터를 확인할 수 없음",DiagnosticError.Forbidden=>"서버 접근 제한",DiagnosticError.RateLimited=>"서버 호출 제한",DiagnosticError.Challenge=>"인증/CAPTCHA 요구",DiagnosticError.Schema=>"현재 파서와 응답 계약 불일치",DiagnosticError.Transport=>"통신 실패",_=>"데이터 검증 실패"}),stopped?"추가 요청을 중단했습니다. 제한을 우회하지 마세요.":"진단 작업 ID를 확인하세요. 자동 재시도하지 않습니다.",category);
            }
            void Report(ReadCheckOutcome outcome,string detail,string action,DiagnosticError error=DiagnosticError.None,int? count=null,string? currency=null)
            {
                var result=new SteamReadCheckResult(id,outcome,detail,action,DateTimeOffset.UtcNow,DiagnosticHub.Context?.OperationId,error,count,currency){Partial=id is "Inventory" or "Marketable"?inventory?.Next is not null:id=="Listings"&&listingsPartial};report(result);
                DiagnosticHub.Emit(DiagnosticEvent.ReadSelfCheck,DiagnosticArea.Data,outcome==ReadCheckOutcome.Running?DiagnosticOutcome.Started:outcome==ReadCheckOutcome.Failed?DiagnosticOutcome.Failed:outcome==ReadCheckOutcome.Blocked?DiagnosticOutcome.Blocked:outcome==ReadCheckOutcome.Succeeded?DiagnosticOutcome.Succeeded:DiagnosticOutcome.Completed,
                    outcome is ReadCheckOutcome.Failed or ReadCheckOutcome.Blocked?LogLevel.Warning:LogLevel.Information,error,endpoint:id,reason:outcome.ToString(),from:result.Classification.ToString(),to:outcome.ToString(),count:count,currency:currency,requirement:"A04");
            }
        }
        using var operation=DiagnosticHub.Operation(DiagnosticEvent.ReadSelfCheck,DiagnosticArea.Data,"A04","SelfCheck");
        await Step("SessionCheck",true,()=>{if(!workspace.Authentication.Connected)throw new SteamException(SteamFailure.Authentication);return Task.FromResult(("QR 인증 완료 기록·메모리 세션 존재 확인. 현재 서버 읽기 권한은 각 조회 결과로 구분합니다.",(int?)null,(string?)null));});
        await Step("Listings",plan.Listings,async()=>{var page=await workspace.Orders.ProbeAsync(token);listingsPartial=page.Listings.Count(x=>x.State==SaleState.Listed)<page.TotalActive;return($"첫 페이지 {page.Listings.Length}건 · 서버 활성 총계 {page.TotalActive}건 · 전체 페이지 조회는 미검증",(int?)page.Listings.Length,(string?)null);});
        await Step("Inventory",plan.Inventory,async()=>{inventory=await workspace.ProbeInventoryAsync(plan.AppId,plan.ContextId,plan.Game,token);return($"{(inventory.VerifiedEmpty?"정상 빈 보관함 (명시적 0건 응답)":inventory.Next is not null?"일부 페이지 조회":"첫 페이지 조회")} · 자산 {inventory.Assets.Length}개 · {(inventory.Next is null?"추가 페이지 없음":"추가 페이지 있음 · 전체 수 미검증")}",(int?)inventory.Assets.Length,(string?)null);});
        await Step("Marketable",plan.Inventory,()=>{if(inventory is null)throw new CheckUnavailable("보관함 조회 실패로 판매 가능 여부 미검증");var count=inventory.Assets.Count(x=>x.Marketable);return Task.FromResult(($"관측 페이지의 장터 판매 가능 자산 {count}개 · tradable과 구분",(int?)count,(string?)null));});
        await Step("Wallet",plan.Wallet,async()=>{wallet=await workspace.ProbeWalletAsync(token);if(wallet.Currency is null)throw new SteamException(SteamFailure.WalletUnavailable);return($"지갑 {wallet.Currency} · "+(wallet.PricingMetadataAvailable?"최소 단위/증분 파싱 확인":"최소 단위/증분 미검증")+" · 기존 지갑 수수료 "+(wallet.LegacyFees?.Complete==true?"필드 관측 (적용 모드 미검증)":"필드 불충분/미검증"),(int?)null,(string?)wallet.Currency);});
        await Step("Fees",plan.Fees,async()=>{if(wallet?.Currency is null)throw new CheckUnavailable("지갑 통화 미확인으로 수수료 미검증");var structure=await workspace.ProbeFeeStructureAsync(plan.AppId,token);if(!wallet.PricingMetadataAvailable||structure.AdditionalFields)throw new CheckPartial("서버 수수료 비율 관측 · 최소 단위/증분 또는 추가 응답 필드의 계약 미검증");var pricing=wallet.PricingWallet();var fees=new SteamFeeSchedule(pricing.Currency,pricing.MinimumWire,pricing.IncrementWire,structure.Steam,structure.Publisher);fees.Validate();var compared=plan.Item is {} selected&&inventory?.PublisherFees is {} map&&map.TryGetValue(selected.Key,out var rate);if(compared&&inventory!.PublisherFees![plan.Item!.Key]*100L!=fees.Publisher.Millionths)throw new CheckMismatch(DiagnosticError.Validation,"보관함과 서버의 게임 수수료 비율이 일치하지 않습니다.");return("서버 설정 파싱 확인 · "+(compared?"선택 품목 비율 대조 일치":"품목 비율 대조 미검증")+" · 실거래 계약 검증 아님",(int?)null,(string?)fees.Currency);});
        await Step("Orderbook",plan.Orderbook,async()=>{var item=plan.Item??throw new CheckUnavailable("선택한 품목이 없음 · 보관함에서 하나만 선택하세요.");var price=await workspace.Prices.RefreshAsync(item,token);if(wallet?.Currency is not null&&price.Currency!=wallet.Currency)throw new CheckMismatch(DiagnosticError.Currency,"지갑과 구매주문 통화가 일치하지 않습니다.");return($"구매주문 {price.Bids.Length}단계 · 잔량 관측 · 체결 보장 없음",(int?)price.Bids.Length,(string?)price.Currency);});
        await Step("History",plan.History,async()=>{var points=await workspace.Prices.GetMedianHistoryAsync(plan.Item??throw new CheckUnavailable("선택한 품목이 없음"),token);return($"기간 중앙값·거래량 {points.Length}구간 · 정확한 체결 평균 아님",(int?)points.Length,(string?)null);});
        await Step("Image",plan.ImageData,async()=>{var item=plan.Item??throw new CheckUnavailable("선택한 품목이 없음");var image=inventory?.Images?.GetValueOrDefault(item.Key)??plan.Image;if(image?.Invalid==true)throw new InvalidDataException("ImageMetadata");if(image is null||image.Token is null)throw new CheckUnavailable("확인할 이미지 메타데이터 없음");var bytes=await workspace.Images.GetAsync(image.Token,token);SteamImageCache.ValidateDimensions(bytes);return("무인증 CDN 이미지 바이트·픽셀 상한 확인 · 실제 화면 디코드는 별도",(int?)1,(string?)null);});
    }
    private sealed class CheckPartial(string message):Exception(message);
    private sealed class CheckMismatch(DiagnosticError error,string message):Exception(message){public DiagnosticError Error{get;}=error;}
    private sealed class CheckUnavailable(string message):Exception(message);
}
