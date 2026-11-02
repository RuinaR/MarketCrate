using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
using MarketCrate.Infrastructure.Tbh;

namespace MarketCrate.App.ViewModels;

public sealed record TbhSaveRow(TbhOwnedItem Item,string PriceText,long? UnitPrice,long? PricedTotal,string? Currency,int Missing,ItemImageReference? Image)
{
    public string Name=>Item.MarketHash??Item.Name+(Item.Grade.Length>0?" · "+Item.Grade:"");
    public string PriceGroup=>PricedTotal is not null?"가격 확인 · 단가 높은순":"가격 미확인 · 순위 없음";
    public string Location=>Item.Location;
    public string? IconHash=>Item.ImageMarketHash??Item.MarketHash;
    public string ImageDescription=>Item.ImageMarketHash is {} hash&&hash!=Item.MarketHash?"같은 이름·스프라이트의 대표 아이콘 · 등급/변형은 표시명을 확인하세요":"Steam 상품 아이콘";
    public string Description=>$"{Item.Quantity:N0}개 · {KindLabel(Item.Kind)} · {(Item.Locked?"잠금":"잠금 해제")}{(Item.Equipped?" · 장착 중":"")}";
    public static string KindLabel(TbhItemKind kind)=>kind switch{TbhItemKind.Equipment=>"장비",TbhItemKind.Material=>"재료",TbhItemKind.Other=>"기타",_=>"종류 미확인"};
}

public sealed partial class SteamWorkspaceViewModel
{
    private TbhSaveSnapshot? tbhSnapshot;
    private readonly Dictionary<string,PriceSnapshot> tbhQuotes=new(StringComparer.Ordinal);
    private bool includeLocked,includeEquipped;
    private int tbhPriceMode,tbhKindFilter;
    private readonly Dictionary<string,SteamFailure> tbhQuoteErrors=new(StringComparer.Ordinal);
    public string[] TbhKinds{get;}=["전체 아이템","장비 아이템만","재료 아이템만","기타·미분류"];
    public int TbhKindFilter{get=>tbhKindFilter;set{if(value is >=0 and <=3&&Set(ref tbhKindFilter,value))RebuildTbhRows();}}
    public SteamMarketImages? TbhMetadataLoader=>ImagesEnabled&&Connected?workspace?.MarketImages:null;
    public string WalletBalanceLabel=>!Connected?"Steam 지갑 · 연결 필요":workspace?.WalletObservation is {Currency:{} c,BalanceWire:{} wire}?("Steam 지갑 "+Money.FromWire(wire,c)+(WalletRead.State is SteamReadState.Failed or SteamReadState.AccessBlocked or SteamReadState.Unsupported?" · 이전 조회":"")):workspace?.WalletObservation?.BalanceInvalid==true?"Steam 지갑 · 잔액 확인 필요":workspace?.WalletObservation is not null?"Steam 지갑 · 잔액 정보 없음":"Steam 지갑 · 잔액 미조회";
    public string WalletBalanceHint=>workspace?.WalletObservedAt is {} at?$"클릭하면 지갑만 갱신합니다. 마지막 조회 {at.ToLocalTime():MM-dd HH:mm:ss}. 아이템 합계/보류 금액과 별개인 서버 지갑 잔액입니다.":"클릭해서 현재 Steam 지갑 잔액을 조회하세요. 로그인 복원만으로 자동 조회하지 않습니다.";
    private bool TbhKindMatches(TbhOwnedItem item)=>tbhKindFilter switch{1=>item.Kind==TbhItemKind.Equipment,2=>item.Kind==TbhItemKind.Material,3=>item.Kind is TbhItemKind.Other or TbhItemKind.Unknown,_=>true};
    private string tbhSearch="",tbhSaveStatus="게임 보유 아이템을 읽으려면 ‘세이브 복사·읽기’를 누르세요. 원본은 수정하지 않습니다.";
    public RangeObservableCollection<TbhSaveRow> TbhSaveRows{get;}=[];
    public AsyncCommand ReadTbhSave{get;private set;}=null!;
    public AsyncCommand RefreshTbhPrices{get;private set;}=null!;
    public string[] TbhPriceModes{get;}=[PriceDisplay.Immediate,"최저 판매주문호가"];
    public string TbhSaveStatus{get=>tbhSaveStatus;private set=>Set(ref tbhSaveStatus,value);}
    public bool TbhIncludeLocked{get=>includeLocked;set{if(Set(ref includeLocked,value))RebuildTbhRows();}}
    public bool TbhIncludeEquipped{get=>includeEquipped;set{if(Set(ref includeEquipped,value))RebuildTbhRows();}}
    public int TbhPriceMode{get=>tbhPriceMode;set{if(value is >=0 and <=1&&Set(ref tbhPriceMode,value))RebuildTbhRows();}}
    public string TbhSearch{get=>tbhSearch;set{if(Set(ref tbhSearch,value))RebuildTbhRows();}}
    public bool TbhEmpty=>TbhSaveRows.Count==0;
    public string TbhSnapshotText=>tbhSnapshot is {} s?$"파일 저장 {s.FileWrittenAt.ToLocalTime():MM-dd HH:mm:ss} · 복사 확인 {s.CapturedAt.ToLocalTime():HH:mm:ss} · 원본 읽기 전용 · 현재 게임 상태와 시차가 있을 수 있음":"세이브 미조회 · 파일 읽기는 로그인 없이 가능 · 시세 조회는 Steam 연결 필요";
    public string TbhCountText=>$"{TbhSaveRows.Count:N0}개 항목 · 가격 확인 항목만 단가순 · 미확인 항목은 순위 없음";
    public string TbhValuationCoverage=>$"{TbhSaveRows.Count(x=>x.PricedTotal is not null):N0}개 항목 금액만 포함 · 미확인 {TbhSaveRows.Count(x=>x.PricedTotal is null):N0}개 항목 제외";
    public string TbhValuationText
    {
        get
        {
            try
            {
                var totals=TbhSaveRows.Where(x=>x.PricedTotal is not null&&x.Currency is not null).GroupBy(x=>x.Currency).Select(g=>new Money(g.Sum(x=>x.PricedTotal!.Value),g.Key!).ToString());
                return string.Join(" / ",totals) is {Length:>0} text?text:tbhQuotes.Count>0?"현재 기준으로 합산할 가격 없음":"가격 미확인";
            }
            catch(OverflowException){return "합계 범위 확인 필요";}
        }
    }
    public string TbhSaveSummary
    {
        get
        {
            try
            {
            var groups=TbhSaveRows.Where(x=>x.PricedTotal is not null&&x.Currency is not null).GroupBy(x=>x.Currency);
            var totals=string.Join(" / ",groups.Select(x=>new Money(x.Sum(y=>y.PricedTotal!.Value),x.Key!)+" (조회된 수량만)"));
            return $"표시 {TbhSaveRows.Count:N0}행 · 가격 미확인/부족 {TbhSaveRows.Count(x=>x.UnitPrice is null||x.Missing>0):N0}행 · 단가 높은순\n가격 확인된 수량의 주문금액 합계: {(totals.Length==0?"미조회":totals)} · 수수료 차감 전 · 체결/판매 가능 보장 아님";
            }
            catch(OverflowException){return "합계 금액 범위 초과 · 각 항목은 유지하며 전체 금액을 확정하지 않습니다.";}
        }
    }
    private void InitializeTbhSave()
    {
        ReadTbhSave=new(ReadTbhSaveAsync,Error,()=>!Busy&&!AnyReview);
        RefreshTbhPrices=new(RefreshTbhPricesAsync,Error,()=>CanRead&&!Busy&&!AnyReview&&tbhSnapshot is not null);
    }
    public Task ReadTbhSaveAsync()=>Run(async token=>
    {
        OperationLabel="TBH 세이브를 메모리에 복사·일관성 확인 중";
        using var diagnostics=DiagnosticHub.Operation(DiagnosticEvent.TbhSaveRead,DiagnosticArea.Data,"TBH-SAVE");
        try
        {
            var snapshot=await Task.Run(()=>TbhSaveReader.ReadAsync(TbhSaveReader.DefaultPath,token),token);token.ThrowIfCancellationRequested();
            ApplyTbhSnapshot(snapshot);TbhSaveStatus=$"세이브 읽기 완료 · {snapshot.Items.Count:N0}행 · 미확인 키 {snapshot.UnknownKeys:N0}개 · 자동 감시/게임 조작 없음";
            DiagnosticHub.Emit(DiagnosticEvent.TbhSaveRead,DiagnosticArea.Data,DiagnosticOutcome.Succeeded,reason:"LocalSnapshot",count:snapshot.Items.Count,requirement:"TBH-SAVE");Status=TbhSaveStatus;
        }
        catch(OperationCanceledException){TbhSaveStatus="읽기 중단 · 이전 목록 보존";throw;}
        catch(Exception)
        {
            TbhSaveStatus="세이브를 읽지 못했습니다. 저장 중·파일 잠금·지원되지 않는 형식일 수 있습니다. 이전 목록은 유지하며 원본은 수정하지 않았습니다. 잠시 후 직접 다시 읽으세요.";throw;
        }
    });
    public void ApplyTbhSnapshot(TbhSaveSnapshot snapshot)
    {tbhSnapshot=snapshot;RebuildTbhRows();Raise(nameof(TbhSnapshotText));Changed();}
    public Task RefreshTbhPricesAsync()=>Run(async token=>
    {
        RequireConnection();if(tbhSnapshot is null)return;
        using var diagnostics=DiagnosticHub.Operation(DiagnosticEvent.TbhSavePrices,DiagnosticArea.Pricing,"TBH-SAVE");
        var hashes=tbhSnapshot.Items.Where(x=>!x.Ambiguous&&x.MarketHash is not null&&(TbhIncludeLocked||!x.Locked)&&(TbhIncludeEquipped||!x.Equipped)&&TbhKindMatches(x)&&(x.Name.Contains(tbhSearch,StringComparison.OrdinalIgnoreCase)||x.MarketHash?.Contains(tbhSearch,StringComparison.OrdinalIgnoreCase)==true)).Select(x=>x.MarketHash!).Distinct(StringComparer.Ordinal).ToArray();
        // Explicit, bounded user action; serialized and rate limited by the existing SteamHttp.
        var pending=hashes.Where(x=>!tbhQuotes.TryGetValue(x,out var q)||DateTimeOffset.UtcNow-q.CapturedAt>TimeSpan.FromMinutes(5)).ToArray();
        // Never-queried products must progress even when earlier pages have become stale.
        var unseen=hashes.Where(x=>!tbhQuotes.ContainsKey(x)&&!tbhQuoteErrors.ContainsKey(x));
        var targets=unseen.Concat(hashes.Where(x=>tbhQuoteErrors.ContainsKey(x))).Concat(pending).Concat(hashes).Distinct(StringComparer.Ordinal).Take(200).ToArray();
        int completed=0,failed=0,consecutiveSchema=0;
        for(int i=0;i<targets.Length;i++)
        {
            using var itemDiagnostics=DiagnosticHub.Scope(item:"TbhQuote:"+targets[i],correlation:true);
            token.ThrowIfCancellationRequested();OperationLabel=$"게임 보유 아이템 시세 {i+1}/{targets.Length} · 서버 제한 준수";
            try{var quote=await workspace!.Prices.RefreshNameAsync(TbhScope.AppId,targets[i],token);token.ThrowIfCancellationRequested();tbhQuotes[targets[i]]=quote;tbhQuoteErrors.Remove(targets[i]);completed++;consecutiveSchema=0;RebuildTbhRows();}
            catch(OperationCanceledException){TbhSaveStatus="시세 중단 · 완료한 조회와 이전 목록 보존";throw;}
            catch(SteamException error)
            {
                tbhQuotes.Remove(targets[i]);if(tbhQuoteErrors.Count>=512)tbhQuoteErrors.Remove(tbhQuoteErrors.Keys.First());tbhQuoteErrors[targets[i]]=error.Code;failed++;RebuildTbhRows();
                DiagnosticHub.Emit(DiagnosticEvent.TbhSavePrices,DiagnosticArea.Pricing,DiagnosticOutcome.Failed,LogLevel.Warning,SteamDiagnostics.Classify(error.Code),requirement:"D07",endpoint:"Orderbook",reason:"ItemQuoteFailed",count:i+1);
                if(workspace!.Failure is not null||workspace.FeatureFailure("Orderbook") is not null||error.Code is SteamFailure.Authentication or SteamFailure.Expired or SteamFailure.Forbidden or SteamFailure.RateLimited or SteamFailure.Challenge or SteamFailure.Authorization or SteamFailure.Restricted or SteamFailure.Disabled)
                {TbhSaveStatus=$"시세 {completed}건 완료 · 서버 접근/인증 제한으로 중단했습니다. 실패를 주문 없음으로 표시하지 않습니다.";throw;}
                consecutiveSchema=error.Code==SteamFailure.ContractChanged?consecutiveSchema+1:0;
                if(consecutiveSchema>=3){TbhSaveStatus="연속 응답 형식 오류로 추가 조회를 중단했습니다. 완료 항목은 보존하며 진단을 확인하세요.";throw;}
                // A failed distinct item is not an automatic retry of the failed request.
            }
        }
        while(tbhQuotes.Count>512)tbhQuotes.Remove(tbhQuotes.Keys.First());
        TbhSaveStatus=$"가격 조회 {completed}종 완료 · 실패 {failed}종 · 대상 {hashes.Length}종 · 최대200종/회. 실패/미조회는 주문 없음과 다르며 다시 갱신할 수 있습니다.";
        Status=TbhSaveStatus;DiagnosticHub.Emit(DiagnosticEvent.TbhSavePrices,DiagnosticArea.Pricing,DiagnosticOutcome.Completed,reason:"LocalValuation",count:targets.Length,requirement:"TBH-SAVE");
    });
    private void ResetTbhSessionPrices(){tbhQuotes.Clear();tbhQuoteErrors.Clear();if(tbhSnapshot is not null){RebuildTbhRows();TbhSaveStatus="Steam 연결 변경 · 이전 시세 해제 · 게임 파일 목록은 보존했습니다.";}}
    public string TbhNextAction=>tbhSnapshot is null?"① 세이브 복사·읽기 → ② Steam 연결 → ③ 장터 가격 갱신":!Connected?"게임 파일을 읽었습니다. 가격 조회는 계정 연결에서 Steam 로그인 후 이용하세요.":"장터 가격 갱신을 누르세요. 가격은 수수료 차감 전이며 조회 불가 품목을0원으로 처리하지 않습니다.";
    private void RebuildTbhRows()
    {
        if(tbhSnapshot is null)return;
        var items=tbhSnapshot.Items.Where(x=>(includeLocked||!x.Locked)&&(includeEquipped||!x.Equipped)&&TbhKindMatches(x)&&(x.Name.Contains(tbhSearch,StringComparison.OrdinalIgnoreCase)||x.MarketHash?.Contains(tbhSearch,StringComparison.OrdinalIgnoreCase)==true)).ToArray();
        var remaining=new Dictionary<string,List<BidLevel>>(StringComparer.Ordinal);var rows=new List<TbhSaveRow>();
        foreach(var item in items)
        {
            long? unit=null,total=null;string? currency=null;int missing=item.Quantity;var message=item.Ambiguous?"위치/이동 상태 미확인 · 합계 제외":item.MarketHash is null?"Steam 상품 연결 미확인 · 합계 제외":"가격 미조회 · 합계 제외";
            if(item.MarketHash is {} failedHash&&tbhQuoteErrors.TryGetValue(failedHash,out var failure))message=failure switch{SteamFailure.ContractChanged=>"가격 응답 불완전/형식 오류 · 주문 없음이 아님 · 다시 갱신",SteamFailure.Unsupported=>"상품 조회 경로 미지원 · 상품 연결 확인 필요",_=>"가격 조회 실패 · 주문 없음이 아님 · 다시 갱신"};
            try
            {
            if(!item.Ambiguous&&item.MarketHash is {} hash&&tbhQuotes.TryGetValue(hash,out var price))
            {
                currency=price.Currency;var stale=DateTimeOffset.UtcNow-price.CapturedAt>TimeSpan.FromMinutes(5)?" · 오래된 자료":"";
                if(tbhPriceMode==1)
                {
                    if(!price.Asks.IsEmpty){unit=price.Asks[0].BuyerPays;total=checked(unit.Value*item.Quantity);missing=0;message=$"최저 판매주문호가 {new Money(unit.Value,currency)} · {item.Quantity:N0}개 주문금액 합계 {new Money(total.Value,currency)}";}
                    else message="이 상품의 현재 판매주문 없음 · 합계 제외";
                }
                else
                {
                    if(!remaining.TryGetValue(hash,out var book))remaining.Add(hash,book=price.Bids.ToList());
                    var allocation=Pricing.Allocate(item.Quantity,book);missing=allocation.UnpricedQuantity;
                    if(allocation.Lines.Length>0)
                    {
                        unit=allocation.Lines[0].BuyerPays;total=allocation.Lines.Sum(x=>checked(x.BuyerPays*x.Quantity));var used=item.Quantity-missing;
                        for(int i=0;i<book.Count&&used>0;i++){var count=Math.Min(used,book[i].Quantity);used-=count;book[i]=book[i] with{Quantity=book[i].Quantity-count};}book.RemoveAll(x=>x.Quantity==0);
                        message=$"즉시거래가능가 {new Money(unit.Value,currency)}부터 · 가격 확인 {item.Quantity-missing:N0}/{item.Quantity:N0}개 합계 {new Money(total.Value,currency)}{(missing>0?$" · 주문 잔량 부족 {missing:N0}개":"")}";
                    }
                    else message="이 상품의 현재 매수주문 잔량 없음 · 최저 판매호가도 확인하세요";
                }
                message+=$" · 수집 {price.CapturedAt.ToLocalTime():HH:mm:ss}{stale}";
            }
            }
            catch(OverflowException)
            {unit=total=null;currency=null;missing=item.Quantity;message="수량·가격 금액 범위 초과 · 합계 제외 · 원본 수량은 변경하지 않았습니다.";}
            var icon=AllSteamRows().FirstOrDefault(x=>x.Asset.Key.AppId==TbhScope.AppId&&x.Asset.MarketHashName==item.MarketHash)?.Image
                ??Listings.FirstOrDefault(x=>x.Listing.Asset.AppId==TbhScope.AppId&&x.Listing.HashName==item.MarketHash)?.Image;
            rows.Add(new(item,message,unit,total,currency,missing,icon));
        }
        TbhSaveRows.ReplaceAll(rows.OrderByDescending(x=>x.UnitPrice??-1).ThenByDescending(x=>x.PricedTotal??-1).ThenBy(x=>x.Name,StringComparer.Ordinal));
        Raise(nameof(TbhSaveSummary));Raise(nameof(TbhCountText));Raise(nameof(TbhValuationText));Raise(nameof(TbhValuationCoverage));Raise(nameof(TbhEmpty));Raise(nameof(TbhNextAction));
    }
}
