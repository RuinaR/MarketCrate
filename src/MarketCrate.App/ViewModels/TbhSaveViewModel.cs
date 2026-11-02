using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
using MarketCrate.Infrastructure.Tbh;

namespace MarketCrate.App.ViewModels;

public sealed record TbhSaveRow(TbhOwnedItem Item,string PriceText,long? UnitPrice,long? PricedTotal,string? Currency,int Missing,ItemImageReference? Image)
{
    public string Name=>Item.Name;
    public string Location=>Item.Location;
    public string Description=>$"{Item.Quantity:N0}개 · {(Item.Locked?"잠금":"잠금 해제")} · {(Item.Ambiguous?"위치/이동 표식 미확인 · 평가 제외":Item.MarketHash is null?"장터 매핑 없음":"시세 대응 있음 · Steam 등록 여부 별도")}";
}

public sealed partial class SteamWorkspaceViewModel
{
    private TbhSaveSnapshot? tbhSnapshot;
    private readonly Dictionary<string,PriceSnapshot> tbhQuotes=new(StringComparer.Ordinal);
    private bool includeLocked,includeEquipped;
    private int tbhPriceMode;
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
    public string TbhSaveSummary
    {
        get
        {
            var groups=TbhSaveRows.Where(x=>x.PricedTotal is not null&&x.Currency is not null).GroupBy(x=>x.Currency);
            var totals=string.Join(" / ",groups.Select(x=>new Money(x.Sum(y=>y.PricedTotal!.Value),x.Key!)+" (조회된 수량만)"));
            return $"표시 {TbhSaveRows.Count:N0}행 · 가격 미확인/부족 {TbhSaveRows.Count(x=>x.UnitPrice is null||x.Missing>0):N0}행 · 단가 높은순\n구매자 호가 기준 평가액: {(totals.Length==0?"미조회":totals)} · 수수료 차감 전 · 체결/판매 가능 보장 아님";
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
        var hashes=tbhSnapshot.Items.Where(x=>!x.Ambiguous&&x.MarketHash is not null&&(TbhIncludeLocked||!x.Locked)&&(TbhIncludeEquipped||!x.Equipped)).Select(x=>x.MarketHash!).Distinct(StringComparer.Ordinal).ToArray();
        // Explicit, bounded user action; serialized and rate limited by the existing SteamHttp.
        var pending=hashes.Where(x=>!tbhQuotes.TryGetValue(x,out var q)||DateTimeOffset.UtcNow-q.CapturedAt>TimeSpan.FromMinutes(5)).ToArray();
        var targets=(pending.Length>0?pending:hashes).Take(200).ToArray();
        for(int i=0;i<targets.Length;i++)
        {
            token.ThrowIfCancellationRequested();OperationLabel=$"게임 보유 아이템 시세 {i+1}/{targets.Length} · 서버 제한 준수";
            try{tbhQuotes[targets[i]]=await workspace!.Prices.RefreshNameAsync(TbhScope.AppId,targets[i],token);token.ThrowIfCancellationRequested();RebuildTbhRows();}
            catch(OperationCanceledException){TbhSaveStatus="시세 중단 · 완료한 조회와 이전 목록 보존";throw;}
            catch(SteamException)
            {TbhSaveStatus=$"시세 {i}/{targets.Length}건 완료 후 서버/계약 제한으로 중단했습니다. 실패를0원으로 처리하거나 자동 재전송하지 않습니다.";throw;}
        }
        while(tbhQuotes.Count>512)tbhQuotes.Remove(tbhQuotes.Keys.First());
        TbhSaveStatus=$"시세 {targets.Length}종 갱신 · 요청 범위 {hashes.Length}종 · 최대200종/회 · 잠금 필터별 매수 잔량 배분 · 미조회 품목은 다시 갱신하세요.";
        Status=TbhSaveStatus;DiagnosticHub.Emit(DiagnosticEvent.TbhSavePrices,DiagnosticArea.Pricing,DiagnosticOutcome.Completed,reason:"LocalValuation",count:targets.Length,requirement:"TBH-SAVE");
    });
    private void ResetTbhSessionPrices(){tbhQuotes.Clear();if(tbhSnapshot is not null){RebuildTbhRows();TbhSaveStatus="Steam 연결 변경 · 이전 시세 해제 · 게임 파일 목록은 보존했습니다.";}}
    public string TbhNextAction=>tbhSnapshot is null?"① 세이브 복사·읽기 → ② Steam 연결 → ③ 장터 가격 갱신":!Connected?"게임 파일을 읽었습니다. 가격 조회는 계정 연결에서 Steam 로그인 후 이용하세요.":"장터 가격 갱신을 누르세요. 가격은 수수료 차감 전이며 조회 불가 품목을0원으로 처리하지 않습니다.";
    private void RebuildTbhRows()
    {
        if(tbhSnapshot is null)return;
        var items=tbhSnapshot.Items.Where(x=>(includeLocked||!x.Locked)&&(includeEquipped||!x.Equipped)&&x.Name.Contains(tbhSearch,StringComparison.OrdinalIgnoreCase)).ToArray();
        var remaining=new Dictionary<string,List<BidLevel>>(StringComparer.Ordinal);var rows=new List<TbhSaveRow>();
        foreach(var item in items)
        {
            long? unit=null,total=null;string? currency=null;int missing=item.Quantity;var message=item.Ambiguous?"위치/이동 상태 미확인 · 평가 제외":item.MarketHash is null?"장터 매핑 없음 · 가격 추정하지 않음":"시세 미조회";
            if(item.MarketHash is {} hash&&tbhQuotes.TryGetValue(hash,out var price))
            {
                currency=price.Currency;var stale=DateTimeOffset.UtcNow-price.CapturedAt>TimeSpan.FromMinutes(5)?" · 오래된 자료":"";
                if(tbhPriceMode==1)
                {
                    if(!price.Asks.IsEmpty){unit=price.Asks[0].BuyerPays;total=checked(unit.Value*item.Quantity);missing=0;message=$"최저 판매주문호가 {new Money(unit.Value,currency)} · {item.Quantity:N0}개 평가 {new Money(total.Value,currency)}";}
                    else message="판매주문호가 없음 · 평가 없음";
                }
                else
                {
                    if(!remaining.TryGetValue(hash,out var book))remaining.Add(hash,book=price.Bids.ToList());
                    var allocation=Pricing.Allocate(item.Quantity,book);missing=allocation.UnpricedQuantity;
                    if(allocation.Lines.Length>0)
                    {
                        unit=allocation.Lines[0].BuyerPays;total=allocation.Lines.Sum(x=>checked(x.BuyerPays*x.Quantity));var used=item.Quantity-missing;
                        for(int i=0;i<book.Count&&used>0;i++){var count=Math.Min(used,book[i].Quantity);used-=count;book[i]=book[i] with{Quantity=book[i].Quantity-count};}book.RemoveAll(x=>x.Quantity==0);
                        message=$"즉시거래가능가 {new Money(unit.Value,currency)}부터 · 평가 {new Money(total.Value,currency)} · 잔량 부족 {missing:N0}개";
                    }
                    else message="매수 주문 잔량 없음 · 평가 없음";
                }
                message+=$" · 수집 {price.CapturedAt.ToLocalTime():HH:mm:ss}{stale}";
            }
            var icon=AllSteamRows().FirstOrDefault(x=>x.Asset.Key.AppId==TbhScope.AppId&&x.Asset.MarketHashName==item.MarketHash)?.Image
                ??Listings.FirstOrDefault(x=>x.Listing.Asset.AppId==TbhScope.AppId&&x.Listing.HashName==item.MarketHash)?.Image;
            rows.Add(new(item,message,unit,total,currency,missing,icon));
        }
        TbhSaveRows.ReplaceAll(rows.OrderByDescending(x=>x.UnitPrice??-1).ThenByDescending(x=>x.PricedTotal??-1).ThenBy(x=>x.Name,StringComparer.Ordinal));
        Raise(nameof(TbhSaveSummary));Raise(nameof(TbhEmpty));Raise(nameof(TbhNextAction));
    }
}
