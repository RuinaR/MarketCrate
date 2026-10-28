using System.Collections.ObjectModel;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.App.ViewModels;

public sealed class ReadSelfCheckRow(string step,string name):Observable
{
    public string Step{get;}=step;public string Name{get;}=name;
    public SteamReadCheckResult? Result{get;private set;}
    public string State=>Result?.Outcome switch{ReadCheckOutcome.Running=>"확인 중",ReadCheckOutcome.Succeeded=>"성공 (관측 범위)",ReadCheckOutcome.Failed=>"실패",ReadCheckOutcome.Blocked=>"차단",ReadCheckOutcome.Unsupported=>"미지원",ReadCheckOutcome.Cancelled=>"중단",_=>"미검증"};
    public string Detail=>Result?.Detail??"아직 실행하지 않았습니다.";
    public string Action=>Result?.NextAction??"범위를 선택하고 직접 실행하세요.";
    public string Operation=>Result?.OperationId?.ToString()??"";
    private bool showDetails;public bool ShowDetails{get=>showDetails;set{Set(ref showDetails,value);Raise(nameof(DetailsLabel));}}
    public string DetailsLabel=>ShowDetails?"상세 닫기":"작업 상세 보기";
    public string Classification=>Result?.Classification.ToString()??"Unverified";
    public string At=>Result is {} r?$"{r.At:HH:mm:ss} UTC / {r.At.ToOffset(TimeSpan.FromHours(9)):HH:mm:ss} KST":"—";
    public void Update(SteamReadCheckResult? result){Result=result;if(result is null)ShowDetails=false;Raise(nameof(Classification));Raise(nameof(Result));Raise(nameof(State));Raise(nameof(Detail));Raise(nameof(Action));Raise(nameof(Operation));Raise(nameof(At));}
}
public sealed partial class SteamWorkspaceViewModel
{
    private bool checkApproved,checkListings,checkInventory,checkWallet,checkFees,checkOrders,checkHistory,checkImage;
    private string checkFingerprint="",checkContext="아직 실행하지 않음";
    private int workPage;
    public int WorkPage{get=>workPage;set{if(value is >=0 and <=5)Set(ref workPage,value);}}
    public Command ShowSelfCheck{get;private set;}=null!;
    public AsyncCommand SelfCheck{get;private set;}=null!;
    public bool CheckApproved{get=>checkApproved;set{Set(ref checkApproved,value);SelfCheck?.Refresh();}}
    public bool CheckListings{get=>checkListings;set{if(Set(ref checkListings,value))CheckScopeChanged();}}
    public bool CheckInventory{get=>checkInventory;set{if(Set(ref checkInventory,value))CheckScopeChanged();}}
    public bool CheckWallet{get=>checkWallet;set{if(Set(ref checkWallet,value)){if(!value){checkFees=false;Raise(nameof(CheckFees));}CheckScopeChanged();}}}
    public bool CheckFees{get=>checkFees;set{if(Set(ref checkFees,value&&CheckWallet))CheckScopeChanged();}}
    public bool CheckOrders{get=>checkOrders;set{if(Set(ref checkOrders,value))CheckScopeChanged();}}
    public bool CheckHistory{get=>checkHistory;set{if(Set(ref checkHistory,value))CheckScopeChanged();}}
    public bool CheckImage{get=>checkImage;set{if(Set(ref checkImage,value))CheckScopeChanged();}}
    public string CheckSource=>syntheticConnection?"합성 HTTP 검증 · 실제 Steam 서버 결과가 아닙니다.":"사용자 실행 실험 읽기 · 성공은 해당 관측 범위이며 실거래 지원을 뜻하지 않습니다.";
    public string CheckSummary=>$"{AuthenticationText} · 성공 {CheckRows.Count(x=>x.Result?.Outcome==ReadCheckOutcome.Succeeded)} · 실패 {CheckRows.Count(x=>x.Result?.Outcome==ReadCheckOutcome.Failed)} · 차단 {CheckRows.Count(x=>x.Result?.Outcome==ReadCheckOutcome.Blocked)} · 미검증/미지원 {CheckRows.Count(x=>x.Result is null||x.Result.Outcome is ReadCheckOutcome.Unverified or ReadCheckOutcome.Unsupported)}";
    public string CheckNextAction=>RequestsBlocked?RequestAccessText:FeatureRestricted?"일부 요청 권한이 제한되어 있습니다. 작업 상세의 분류와 진단을 확인하세요. 다른 조회는 별도로 사용할 수 있습니다.":CheckRows.Any(x=>x.Step=="Wallet"&&x.Result?.Outcome is ReadCheckOutcome.Unverified or ReadCheckOutcome.Unsupported or ReadCheckOutcome.Failed)?"지갑·가격 메타데이터 확인이 필요합니다. 통화나 최소 단위를 추정하지 말고 상세 진단을 확인하세요.":"각 성공은 관측 범위만 의미합니다. 실제 빈 보관함 여부는 같은 계정·게임/context의 공식 페이지와 비교하세요.";
    private void UpdateCheckSummary(){Raise(nameof(CheckSummary));Raise(nameof(CheckNextAction));}
    public string CheckContext{get=>checkContext;private set=>Set(ref checkContext,value);}
    public string CheckScope=>$"게임: {Game.Name} ({Game.Key}) · "+(Assets.Count(x=>x.Selected)==1?$"품목: {Assets.Single(x=>x.Selected).Name}":"가격·이력·이미지는 보관함에서 한 품목만 선택해야 합니다.")+"\n목록/보관함 각 첫 페이지, 지갑/수수료/호가/이력/이미지 각 최대 1요청. 범위·품목 변경 시 승인을 다시 확인하세요.";
    public ObservableCollection<ReadSelfCheckRow> CheckRows{get;}=[new("SessionCheck","인증 세션"),new("Listings","판매목록"),new("Inventory","선택 게임 보관함"),new("Marketable","장터 판매 가능 여부"),new("Wallet","지갑 통화"),new("Fees","서버 수수료 설정"),new("Orderbook","선택 품목 구매주문"),new("History","기간별 가격 이력"),new("Image","선택 품목 이미지")];
    private void InitializeSelfCheck()
    {
        ShowSelfCheck=new(()=>WorkPage=5,()=>!AnyReview);
        SelfCheck=new(SelfCheckAsync,Error,()=>CanRead&&!Busy&&!AnyReview&&CheckApproved&&(CheckListings||CheckInventory||CheckWallet||CheckFees||CheckOrders||CheckHistory||CheckImage));
    }
    private void CheckScopeChanged()
    {
        if(game is null)return;
        var selected=Assets.Where(x=>x.Selected).Select(x=>x.Asset.Key.ToString()).Order().ToArray();
        var fingerprint=$"{Game.Key}:{CheckListings}:{CheckInventory}:{CheckWallet}:{CheckFees}:{CheckOrders}:{CheckHistory}:{CheckImage}:"+string.Join('|',selected);
        if(fingerprint!=checkFingerprint){checkFingerprint=fingerprint;CheckApproved=false;Raise(nameof(CheckScope));}
        SelfCheck?.Refresh();
    }
    public Task SelfCheckAsync()=>Run(async token=>
    {
        RequireConnection();if(!CheckApproved||!(CheckListings||CheckInventory||CheckWallet||CheckFees||CheckOrders||CheckHistory||CheckImage))throw new SteamException(SteamFailure.Disabled);
        var selected=Assets.Where(x=>x.Selected).ToArray();var item=selected.Length==1?selected[0]:null;
        var plan=new SteamReadCheckPlan(Game.AppId,Game.ContextId,Game.Name,item?.Asset,item?.Image,CheckListings,CheckInventory,CheckWallet,CheckFees,CheckOrders,CheckHistory,CheckImage);plan.Validate();
        CheckApproved=false;foreach(var row in CheckRows)row.Update(null);CheckContext=$"실행 범위: {Game.Name} · 품목 {(item?.Name??"미지정")} · {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        await new SteamReadSelfCheck(workspace!).RunAsync(plan,result=>{if(!logoutRequested){CheckRows.Single(x=>x.Step==result.Step).Update(result);UpdateCheckSummary();}},token);token.ThrowIfCancellationRequested();
        if(!Connected&&CheckRows.Any(x=>x.Result?.Error==DiagnosticError.Authentication))Error(new SteamException(SteamFailure.Authentication));
        Status="읽기 전용 자가검증 완료 · 성공/실패/미검증과 다음 조치를 확인하세요. 자동 재시도하지 않았습니다.";Changed();
    });
    private void ClearSelfCheck(){CheckApproved=false;CheckContext="아직 실행하지 않음";foreach(var row in CheckRows)row.Update(null);UpdateCheckSummary();}
}
