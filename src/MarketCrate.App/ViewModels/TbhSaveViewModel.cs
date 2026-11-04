using System.Net.Http;
using System.IO;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
using MarketCrate.Infrastructure.Tbh;

namespace MarketCrate.App.ViewModels;

public sealed record TbhSaveRow(TbhOwnedItem Item,string PriceText,long? UnitPrice,long? PricedTotal,string? Currency,int Missing,ItemImageReference? Image)
{
    public TbhValueReadState ReadState{get;init;}
    public string ReadDetails{get;init;}="";
    public static string ReasonLabel(TbhValueReadState state)=>state switch{TbhValueReadState.NonMarketableItem=>"거래불가 · 정상 제외",TbhValueReadState.UnknownItemKey=>"아이템 사전 미등록",TbhValueReadState.MarketMappingMissing=>"Steam 상품 연결 미확인",TbhValueReadState.SourceAmbiguous=>"위치·이동 상태 미확인",TbhValueReadState.PartialDepth=>"일부 수량만 가격 확인",TbhValueReadState.NoBuyOrders=>"조회한 매수주문 없음",TbhValueReadState.SharedDepthExhausted=>"다른 항목에 매수 잔량 배분됨",TbhValueReadState.NoSellOrders=>"조회한 판매주문 없음",TbhValueReadState.SellDepthMissing=>"판매호가 필드 미수신",TbhValueReadState.RequestFailed=>"요청·응답 검증 실패",TbhValueReadState.AmountRange=>"계산 금액 범위 초과",TbhValueReadState.Ready=>"가격 확인",_=>"아직 조회하지 않음"};
    public string Name=>Item.MarketHash??Item.Name+(Item.Grade.Length>0?" · "+Item.Grade:"");
    public string PriceGroup=>ReadState==TbhValueReadState.NonMarketableItem?"거래불가 · 가격 조회 제외":PricedTotal is not null?"가격 확인 · 단가 높은순":"가격 미확인 · 순위 없음";
    public string Location=>Item.Location;
    public string? IconHash=>Item.ImageMarketHash??Item.MarketHash;
    public string ImageDescription=>Item.ImageMarketHash is {} hash&&hash!=Item.MarketHash?"같은 이름·스프라이트의 대표 아이콘 · 등급/변형은 표시명을 확인하세요":"Steam 상품 아이콘";
    public string Description=>$"{Item.Quantity:N0}개 · {KindLabel(Item.Kind)}{(Item.CanExchangeMarketable==false?" · 장터 거래불가":"")} · {(Item.Locked?"잠금":"잠금 해제")}{(Item.Equipped?" · 장착 중":"")}";
    public static string KindLabel(TbhItemKind kind)=>kind switch{TbhItemKind.Equipment=>"장비",TbhItemKind.Material=>"재료",TbhItemKind.Other=>"기타",_=>"종류 미확인"};
}

public sealed partial class SteamWorkspaceViewModel
{
    private static string TbhAmount(long value,string currency)=>currency=="BRL"?"BRL R$ "+(value/100m).ToString("N2",System.Globalization.CultureInfo.InvariantCulture):new Money(value,currency).ToString();
    private TbhSaveSnapshot? tbhSnapshot;
    internal TbhIndexMarketClient TbhIndexClient{get;set;}=new();
    private IReadOnlyDictionary<string,TbhIndexQuote> tbhIndexQuotes=new Dictionary<string,TbhIndexQuote>();
    private bool tbhIndexSelected;
    public string TbhPriceSource=>tbhIndexSelected?"TBH Index 참고 시세 · BRL · 호가 잔량/수수료 미포함 · 판매 검토에 사용하지 않음":"Steam 직접 조회 · 지갑 통화 · 수량별 호가 잔량 반영";
    public AsyncCommand QuickTbhPrices{get;private set;}=null!;
    private void SetTbhPriceSource(bool index){tbhIndexSelected=index;Raise(nameof(TbhPriceSource));}
    public Task QuickTbhPricesAsync()=>Run(async token=>
    {
        if(tbhSnapshot is null)return;
        using var diagnostics=DiagnosticHub.Operation(DiagnosticEvent.TbhSavePrices,DiagnosticArea.Pricing,"TBH-SAVE");
        OperationLabel="TBH Index 공개 참고 시세 일괄 조회 중";
        try
        {
            var data=await TbhIndexClient.ReadAsync(token:token);token.ThrowIfCancellationRequested();tbhIndexQuotes=data;
            SetTbhPriceSource(true);RebuildTbhRows();TbhSaveStatus="TBH Index 참고 가격을 표시합니다. BRL 기준이며 환율 추정·주문 잔량 배분을 하지 않습니다. 실제 등록은 Steam 보관함에서 별도로 검토하세요.";
            DiagnosticHub.Emit(DiagnosticEvent.TbhSavePrices,DiagnosticArea.Pricing,DiagnosticOutcome.Completed,endpoint:"TbhIndex",reason:"ReferenceOnlyBRL",count:data.Count);
        }
        catch(OperationCanceledException){throw;}
        catch(Exception error)when(error is HttpRequestException or InvalidDataException or System.Text.Json.JsonException)
        {
            TbhSaveStatus="참고 시세를 읽지 못했습니다. 이전 목록은 보존했습니다. Steam 정밀 갱신을 선택할 수 있습니다.";
            DiagnosticHub.Emit(DiagnosticEvent.TbhSavePrices,DiagnosticArea.Pricing,DiagnosticOutcome.Failed,LogLevel.Warning,DiagnosticError.Validation,endpoint:"TbhIndex",reason:error is HttpRequestException?"ReferenceTransport":error is System.Text.Json.JsonException?"ReferenceParse":"ReferenceContract",status:error is HttpRequestException httpError?(int?)httpError.StatusCode:null);
        }
    });
    private readonly Dictionary<string,PriceSnapshot> tbhQuotes=new(StringComparer.Ordinal);
    private bool includeLocked,includeEquipped,excludeThirtyWon;
    private int tbhThirtyWonHiddenCount;
    private int tbhPriceMode,tbhKindFilter;
    private readonly Dictionary<string,(SteamFailure Code,string? Stage)> tbhQuoteErrors=new(StringComparer.Ordinal);
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
    public AsyncCommand ForceTbhPrices{get;private set;}=null!;
    public string[] TbhPriceModes{get;}=[PriceDisplay.Immediate,PriceDisplay.LowestSell];
    public string TbhSaveStatus{get=>tbhSaveStatus;private set=>Set(ref tbhSaveStatus,value);}
    public bool TbhIncludeLocked{get=>includeLocked;set{if(Set(ref includeLocked,value))RebuildTbhRows();}}
    public bool TbhIncludeEquipped{get=>includeEquipped;set{if(Set(ref includeEquipped,value))RebuildTbhRows();}}
    public bool TbhExcludeThirtyWon{get=>excludeThirtyWon;set{if(Set(ref excludeThirtyWon,value))RebuildTbhRows();}}
    public int TbhThirtyWonHiddenCount=>tbhThirtyWonHiddenCount;
    public string TbhPriceFilterText=>TbhExcludeThirtyWon?$" · 단가 ₩30 제외 {tbhThirtyWonHiddenCount:N0}행":"";
    public string TbhEmptyTitle=>tbhSnapshot is null?"아직 불러온 아이템이 없습니다":"현재 조건에 맞는 아이템이 없습니다";
    public string TbhEmptyHint=>tbhSnapshot is null?"세이브 읽기로 시작하세요.":"검색어·종류·잠금·장착·단가 ₩30 제외 조건을 확인하세요.";
    public int TbhPriceMode{get=>tbhPriceMode;set{if(value is >=0 and <=1&&Set(ref tbhPriceMode,value))RebuildTbhRows();}}
    public string TbhSearch{get=>tbhSearch;set{if(Set(ref tbhSearch,value))RebuildTbhRows();}}
    public bool TbhEmpty=>TbhSaveRows.Count==0;
    public string TbhSnapshotText=>tbhSnapshot is {} s?$"파일 저장 {s.FileWrittenAt.ToLocalTime():MM-dd HH:mm:ss} · 복사 확인 {s.CapturedAt.ToLocalTime():HH:mm:ss} · 원본 읽기 전용 · 현재 게임 상태와 시차가 있을 수 있음":"세이브 미조회 · 파일 읽기는 로그인 없이 가능 · 시세 조회는 Steam 연결 필요";
    public string TbhCountText=>$"{TbhSaveRows.Count:N0}개 항목 · 가격 확인 항목만 단가순 · 미확인 항목은 순위 없음{TbhPriceFilterText}";
    public string TbhValuationCoverage=>$"{TbhSaveRows.Count(x=>x.PricedTotal is not null):N0}개 항목 금액만 포함 · 거래불가 {TbhSaveRows.Count(x=>x.ReadState==TbhValueReadState.NonMarketableItem):N0}개 정상 제외 · 미확인 {TbhSaveRows.Count(x=>x.PricedTotal is null&&x.ReadState!=TbhValueReadState.NonMarketableItem):N0}개 항목 제외";
    public string TbhValuationText
    {
        get
        {
            try
            {
                var totals=TbhSaveRows.Where(x=>x.PricedTotal is not null&&x.Currency is not null).GroupBy(x=>x.Currency).Select(g=>TbhAmount(g.Sum(x=>x.PricedTotal!.Value),g.Key!));
                return string.Join(" / ",totals) is {Length:>0} text?text:tbhQuotes.Count>0?"현재 기준으로 합산할 가격 없음":"가격 미확인";
            }
            catch(OverflowException){return "합계 범위 확인 필요";}
        }
    }
    public string TbhReadSummary=>string.Join(" · ",TbhSaveRows.GroupBy(x=>x.ReadState).Select(x=>TbhSaveRow.ReasonLabel(x.Key)+$" {x.Count():N0}행"));
    private void TraceTbhReadStates(){foreach(var group in TbhSaveRows.GroupBy(x=>x.ReadState))DiagnosticHub.Emit(DiagnosticEvent.TbhReadContract,DiagnosticArea.Data,group.Key==TbhValueReadState.Ready?DiagnosticOutcome.Succeeded:group.Key==TbhValueReadState.NonMarketableItem?DiagnosticOutcome.Completed:DiagnosticOutcome.Unknown,reason:group.Key.ToString(),count:group.Count());}
    private static string PriceReadStageLabel(string stage)=>stage switch{"BookEnvelope"=>"주문장 응답 구조","BookCurrency"=>"통화","BookBuyDepth"=>"매수 가격·수량","BookBuyAmount"=>"매수 금액·통화 단위 변환","BookBuyQuantity"=>"매수 수량","BookBuyConsistency"=>"매수 잔량 정합성","BookSellDepth"=>"판매 가격·수량","BookSellAmount"=>"판매 금액·통화 단위 변환","BookSellQuantity"=>"판매 수량","BookSellConsistency"=>"판매 잔량 정합성",_=>"HTTP·요청"};
    public string TbhSaveSummary
    {
        get
        {
            try
            {
            var groups=TbhSaveRows.Where(x=>x.PricedTotal is not null&&x.Currency is not null).GroupBy(x=>x.Currency);
            var totals=string.Join(" / ",groups.Select(x=>TbhAmount(x.Sum(y=>y.PricedTotal!.Value),x.Key!)+" (조회된 수량만)"));
            return $"표시 {TbhSaveRows.Count:N0}행 · 가격 미확인/부족 {TbhSaveRows.Count(x=>x.ReadState!=TbhValueReadState.NonMarketableItem&&(x.UnitPrice is null||x.Missing>0)):N0}행 · 단가 높은순\n가격 확인된 수량의 주문금액 합계: {(totals.Length==0?"미조회":totals)} · 수수료 차감 전 · 체결/판매 가능 보장 아님\n{TbhReadSummary}";
            }
            catch(OverflowException){return "합계 금액 범위 초과 · 각 항목은 유지하며 전체 금액을 확정하지 않습니다.";}
        }
    }
    private void InitializeTbhSave()
    {
        QuickTbhPrices=new(QuickTbhPricesAsync,Error,()=>!Busy&&!AnyReview&&tbhSnapshot is not null);
        ReadTbhSave=new(ReadTbhSaveAsync,Error,()=>!Busy&&!AnyReview);
        RefreshTbhPrices=new(RefreshTbhPricesAsync,Error,()=>CanRead&&!Busy&&!AnyReview&&tbhSnapshot is not null);
        ForceTbhPrices=new(()=>RefreshTbhPricesCoreAsync(true),Error,()=>CanRead&&!Busy&&!AnyReview&&tbhSnapshot is not null);
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
    {tbhSnapshot=snapshot;RebuildTbhRows();TraceTbhReadStates();Raise(nameof(TbhSnapshotText));Changed();}
    public Task RefreshTbhPricesAsync()=>RefreshTbhPricesCoreAsync(false);
    private Task RefreshTbhPricesCoreAsync(bool force)=>Run(async token=>
    {
        RequireConnection();if(tbhSnapshot is null)return;SetTbhPriceSource(false);RebuildTbhRows();
        using var priority=workspace!.PrioritizePriceReads();
        using var diagnostics=DiagnosticHub.Operation(DiagnosticEvent.TbhSavePrices,DiagnosticArea.Pricing,"TBH-SAVE");
        var hashes=tbhSnapshot.Items.Where(x=>x.CanExchangeMarketable!=false&&!x.Ambiguous&&x.MarketHash is not null&&(TbhIncludeLocked||!x.Locked)&&(TbhIncludeEquipped||!x.Equipped)&&TbhKindMatches(x)&&(x.Name.Contains(tbhSearch,StringComparison.OrdinalIgnoreCase)||x.MarketHash?.Contains(tbhSearch,StringComparison.OrdinalIgnoreCase)==true)).Select(x=>x.MarketHash!).Distinct(StringComparer.Ordinal).ToArray();
        // Explicit, bounded user action; serialized and rate limited by the existing SteamHttp.
        var pending=hashes.Where(x=>!tbhQuotes.TryGetValue(x,out var q)||DateTimeOffset.UtcNow-q.CapturedAt>TimeSpan.FromMinutes(5)).ToArray();
        // Never-queried products must progress even when earlier pages have become stale.
        var unseen=hashes.Where(x=>!tbhQuotes.ContainsKey(x)&&!tbhQuoteErrors.ContainsKey(x));
        var targets=unseen.Concat(hashes.Where(x=>tbhQuoteErrors.ContainsKey(x))).Concat(pending).Concat(force?hashes:[]).Distinct(StringComparer.Ordinal).Take(200).ToArray();
        var cached=hashes.Count(x=>tbhQuotes.TryGetValue(x,out var quote)&&DateTimeOffset.UtcNow-quote.CapturedAt<=TimeSpan.FromMinutes(5)&&!tbhQuoteErrors.ContainsKey(x));
        DiagnosticHub.Emit(DiagnosticEvent.CacheRead,DiagnosticArea.Pricing,DiagnosticOutcome.CacheHit,reason:"PriceCacheReused",count:force?0:cached);
        int completed=0,failed=0,consecutiveSchema=0;
        try
        {
        for(int i=0;i<targets.Length;i++)
        {
            using var itemDiagnostics=DiagnosticHub.Scope(item:"TbhQuote:"+targets[i],correlation:true);
            token.ThrowIfCancellationRequested();OperationLabel=$"게임 보유 아이템 시세 {i+1}/{targets.Length} · 서버 제한 준수";
            try{var quote=await workspace!.Prices.RefreshNameAsync(TbhScope.AppId,targets[i],token);token.ThrowIfCancellationRequested();tbhQuotes[targets[i]]=quote;tbhQuoteErrors.Remove(targets[i]);completed++;consecutiveSchema=0;RebuildTbhRows();}
            catch(OperationCanceledException){TbhSaveStatus="시세 중단 · 완료한 조회와 이전 목록 보존";throw;}
            catch(SteamException error)
            {
                tbhQuotes.Remove(targets[i]);if(tbhQuoteErrors.Count>=512)tbhQuoteErrors.Remove(tbhQuoteErrors.Keys.First());tbhQuoteErrors[targets[i]]=(error.Code,error.ReadStage);failed++;RebuildTbhRows();
                DiagnosticHub.Emit(DiagnosticEvent.TbhSavePrices,DiagnosticArea.Pricing,DiagnosticOutcome.Failed,LogLevel.Warning,SteamDiagnostics.Classify(error),requirement:"D07",endpoint:"Orderbook",reason:error.ReadStage??"ItemQuoteFailed",count:i+1);
                if(workspace!.Failure is not null||workspace.FeatureFailure("Orderbook") is not null||error.Code is SteamFailure.Authentication or SteamFailure.Expired or SteamFailure.Forbidden or SteamFailure.RateLimited or SteamFailure.Challenge or SteamFailure.Authorization or SteamFailure.Restricted or SteamFailure.Disabled)
                {TbhSaveStatus=$"시세 {completed}건 완료 · 서버 접근/인증 제한으로 중단했습니다. 실패를 주문 없음으로 표시하지 않습니다.";throw;}
                consecutiveSchema=error.Code==SteamFailure.ContractChanged?consecutiveSchema+1:0;
                if(consecutiveSchema>=3){TbhSaveStatus="연속 응답 형식 오류로 추가 조회를 중단했습니다. 완료 항목은 보존하며 진단을 확인하세요.";throw;}
                // A failed distinct item is not an automatic retry of the failed request.
            }
        }
        }
        finally{TraceTbhReadStates();}
        while(tbhQuotes.Count>512)tbhQuotes.Remove(tbhQuotes.Keys.First());
        TbhSaveStatus=$"가격 조회 {completed}종 완료 · 최근5분 자료 {(force?0:cached)}종 재사용 · 실패 {failed}종 · 대상 {hashes.Length}종 · 최대200종/회. 실패/미조회는 주문 없음과 다르며 다시 갱신할 수 있습니다.";
        Status=TbhSaveStatus;DiagnosticHub.Emit(DiagnosticEvent.TbhSavePrices,DiagnosticArea.Pricing,DiagnosticOutcome.Completed,reason:"LocalValuation",count:targets.Length,requirement:"TBH-SAVE");
    });
    private void ResetTbhSessionPrices(){tbhQuotes.Clear();tbhQuoteErrors.Clear();if(tbhSnapshot is not null){RebuildTbhRows();TbhSaveStatus="Steam 연결 변경 · 이전 시세 해제 · 게임 파일 목록은 보존했습니다.";}}
    public string TbhNextAction=>tbhSnapshot is null?"① 세이브 복사·읽기 → ② Steam 연결 → ③ 장터 가격 갱신":!Connected?"기본 Steam 가격 갱신은 로그인 후 이용하세요. 별도 TBH Index 참고 조회는 자료·조회 상세에서 선택할 수 있습니다.":"장터 가격 갱신을 누르세요. 최근5분 자료는 재사용합니다. 최신 서버 시세가 꼭 필요하면 자료·조회 상세의 전체 재조회를 이용하세요. 가격은 수수료 차감 전이며 실제 판매 전 별도 재검증합니다.";
    private static string TradeEvidenceLabel(TbhOwnedItem item)=>item.TradeEvidence switch{TbhTradeEvidence.ItemDefinition=>item.CanExchangeMarketable==true?"공개 아이템 정의상 전환 가능 · 개별 이벤트/Steam 판매 자산 여부는 별도 확인":"공개 아이템 정의상 전환 불가",TbhTradeEvidence.UserConfirmedCategory=>"사용자가 확인한 유물·유물함 거래불가 규칙",_=>"현재 저장본에 직접 판정할 속성 없음 · 거래불가로 추정하지 않음"};
    private void UpdateTbhRows(TbhSaveRow[] next)
    {
        if(TbhSaveRows.Count+next.Length>2048||TbhSaveRows.Count==0){TbhSaveRows.ReplaceAll(next);return;}
        var priorItems=TbhSaveRows.Select(x=>x.Item).ToHashSet();
        if(next.Length>0&&!next.Any(x=>priorItems.Contains(x.Item))){TbhSaveRows.ReplaceAll(next);return;}
        for(int i=0;i<next.Length;i++)
        {
            if(i>=TbhSaveRows.Count){TbhSaveRows.Add(next[i]);continue;}
            if(TbhSaveRows[i].Item!=next[i].Item)
            {var found=-1;for(int j=i+1;j<TbhSaveRows.Count;j++)if(TbhSaveRows[j].Item==next[i].Item){found=j;break;}if(found>=0)TbhSaveRows.Move(found,i);else{TbhSaveRows.Insert(i,next[i]);continue;}}
            if(TbhSaveRows[i]!=next[i])TbhSaveRows[i]=next[i];
        }
        while(TbhSaveRows.Count>next.Length)TbhSaveRows.RemoveAt(TbhSaveRows.Count-1);
    }
    private void RebuildTbhRows()
    {
        if(tbhSnapshot is null)return;
        var items=tbhSnapshot.Items.Where(x=>(includeLocked||!x.Locked)&&(includeEquipped||!x.Equipped)&&TbhKindMatches(x)&&(x.Name.Contains(tbhSearch,StringComparison.OrdinalIgnoreCase)||x.MarketHash?.Contains(tbhSearch,StringComparison.OrdinalIgnoreCase)==true)).ToArray();
        var remaining=new Dictionary<string,List<BidLevel>>(StringComparer.Ordinal);var rows=new List<TbhSaveRow>();
        var imageLookup=new Dictionary<string,ItemImageReference>(StringComparer.Ordinal);
        foreach(var asset in AllSteamRows())if(asset.Asset.Key.AppId==TbhScope.AppId&&asset.Image is {} img)imageLookup.TryAdd(asset.Asset.MarketHashName,img);
        foreach(var listing in Listings)if(listing.Listing.Asset.AppId==TbhScope.AppId&&listing.Image is {} img)imageLookup.TryAdd(listing.Listing.HashName,img);
        foreach(var item in items)
        {
            long? unit=null,total=null;string? currency=null;int missing=item.Quantity;
            var readState=item.CanExchangeMarketable==false?TbhValueReadState.NonMarketableItem:item.Ambiguous?TbhValueReadState.SourceAmbiguous:!item.CatalogKnown?TbhValueReadState.UnknownItemKey:item.MarketHash is null?TbhValueReadState.MarketMappingMissing:TbhValueReadState.NotQueried;
            var message=item.CanExchangeMarketable==false?"장터 거래불가 · 정상적으로 가격 조회·합계에서 제외":item.Ambiguous?"위치/이동 상태 미확인 · 합계 제외":item.MarketHash is null?"Steam 상품 연결 미확인 · 합계 제외":"가격 미조회 · 합계 제외";
            if(!tbhIndexSelected&&item.CanExchangeMarketable!=false&&!item.Ambiguous&&item.MarketHash is {} failedHash&&tbhQuoteErrors.TryGetValue(failedHash,out var failure))
            {readState=TbhValueReadState.RequestFailed;message=failure.Code switch{SteamFailure.ContractChanged=>"가격 응답 불완전/형식 오류 · 주문 없음이 아님 · 다시 갱신",SteamFailure.Unsupported=>"상품 조회 경로 미지원 · 상품 연결 확인 필요",_=>"가격 조회 실패 · 주문 없음이 아님 · 다시 갱신"};if(failure.Stage is {} stage)message+=" · 확인 단계: "+PriceReadStageLabel(stage);}
            try
            {
            if(tbhIndexSelected&&item.CanExchangeMarketable!=false&&!item.Ambiguous&&item.MarketHash is {} indexHash&&tbhIndexQuotes.TryGetValue(indexHash,out var reference))
            {
                currency="BRL";unit=tbhPriceMode==1?reference.SellMinor:reference.BuyMinor;
                if(reference.ObservedAt is null){unit=null;message="TBH Index 자료 시각 미확인 · 합계 제외";}
                else if(unit is {} value){total=checked(value*item.Quantity);missing=0;readState=TbhValueReadState.Ready;message=$"TBH Index 참고 · {(tbhPriceMode==1?PriceDisplay.LowestSell:PriceDisplay.Immediate)} {TbhAmount(value,currency)} · 단가×수량 {TbhAmount(total.Value,currency)} · 잔량/수수료 미확인 · 원화 환산 없음 · 자료 {reference.ObservedAt.Value.ToLocalTime():MM-dd HH:mm:ss}"+(DateTimeOffset.UtcNow-reference.ObservedAt.Value>TimeSpan.FromMinutes(5)?" · 오래된 자료":"");}
                else message="TBH Index 해당 호가 가격 미제공 · 주문 없음으로 단정하지 않음 · Steam 정밀 갱신 가능";
            }
            else if(!tbhIndexSelected&&item.CanExchangeMarketable!=false&&!item.Ambiguous&&item.MarketHash is {} hash&&tbhQuotes.TryGetValue(hash,out var price))
            {
                currency=price.Currency;var stale=DateTimeOffset.UtcNow-price.CapturedAt>TimeSpan.FromMinutes(5)?" · 오래된 자료":"";
                if(tbhPriceMode==1)
                {
                    if(!price.Asks.IsEmpty){readState=TbhValueReadState.Ready;unit=price.Asks[0].BuyerPays;total=checked(unit.Value*item.Quantity);missing=0;message=$"최저 판매주문호가 {new Money(unit.Value,currency)} · {item.Quantity:N0}개 주문금액 합계 {new Money(total.Value,currency)}";}
                    else{readState=price.HasSellDepth?TbhValueReadState.NoSellOrders:TbhValueReadState.SellDepthMissing;message=price.HasSellDepth?"이 상품의 현재 판매주문 없음 · 합계 제외":"판매호가 필드 미수신 · 주문 없음으로 판단할 수 없음 · 다시 갱신";}
                }
                else
                {
                    if(!remaining.TryGetValue(hash,out var book))remaining.Add(hash,book=price.Bids.ToList());
                    var allocation=Pricing.Allocate(item.Quantity,book);missing=allocation.UnpricedQuantity;
                    if(allocation.Lines.Length>0)
                    {
                        readState=missing>0?TbhValueReadState.PartialDepth:TbhValueReadState.Ready;unit=allocation.Lines[0].BuyerPays;total=allocation.Lines.Sum(x=>checked(x.BuyerPays*x.Quantity));var used=item.Quantity-missing;
                        for(int i=0;i<book.Count&&used>0;i++){var count=Math.Min(used,book[i].Quantity);used-=count;book[i]=book[i] with{Quantity=book[i].Quantity-count};}book.RemoveAll(x=>x.Quantity==0);
                        message=$"구매주문호가 {new Money(unit.Value,currency)}부터 · 가격 확인 {item.Quantity-missing:N0}/{item.Quantity:N0}개 합계 {new Money(total.Value,currency)}{(missing>0?$" · 주문 잔량 부족 {missing:N0}개":"")}";
                    }
                    else{readState=price.Bids.IsEmpty?TbhValueReadState.NoBuyOrders:TbhValueReadState.SharedDepthExhausted;message=price.Bids.IsEmpty?"이 상품의 현재 매수주문 잔량 없음 · 최저 판매주문호가도 확인하세요":"같은 상품의 다른 항목에 매수 잔량을 이미 배분했습니다 · 이 행의 추가 수량은 합계 제외";}
                }
                message+=$" · 수집 {price.CapturedAt.ToLocalTime():HH:mm:ss}{stale}";
            }
            }
            catch(OverflowException)
            {readState=TbhValueReadState.AmountRange;unit=total=null;currency=null;missing=item.Quantity;message="수량·가격 금액 범위 초과 · 합계 제외 · 원본 수량은 변경하지 않았습니다.";}
            var icon=item.MarketHash is {} iconHash?imageLookup.GetValueOrDefault(iconHash):null;
            rows.Add(new(item,message,unit,total,currency,missing,icon){ReadState=readState,ReadDetails=$"{TbhSaveRow.ReasonLabel(readState)}\n게임 아이템 사전: {(item.CatalogKnown?"해당 키 확인":"해당 키 미등록")}\n장터 전환 속성: {TradeEvidenceLabel(item)}\nSteam 조회 상품: {(item.CanExchangeMarketable==false?"거래불가로 조회하지 않음":item.MarketHash??"정확한 연결 미확인 · 비슷한 이름의 가격으로 대체하지 않음")}\n{message}"});
        }
        // Filter only after quantity allocation: hidden rows must not give their bid depth to another item.
        tbhThirtyWonHiddenCount=TbhExcludeThirtyWon?rows.Count(x=>x.Currency=="KRW"&&x.UnitPrice==30&&x.PricedTotal is not null):0;
        var visible=rows.Where(x=>!TbhExcludeThirtyWon||x.Currency!="KRW"||x.UnitPrice!=30||x.PricedTotal is null);
        UpdateTbhRows(visible.OrderBy(x=>x.ReadState==TbhValueReadState.NonMarketableItem?2:x.PricedTotal is not null?0:1).ThenByDescending(x=>x.UnitPrice??-1).ThenByDescending(x=>x.PricedTotal??-1).ThenBy(x=>x.Name,StringComparer.Ordinal).ToArray());
        Raise(nameof(TbhThirtyWonHiddenCount));Raise(nameof(TbhPriceFilterText));Raise(nameof(TbhEmptyTitle));Raise(nameof(TbhEmptyHint));
        Raise(nameof(TbhReadSummary));Raise(nameof(TbhSaveSummary));Raise(nameof(TbhCountText));Raise(nameof(TbhValuationText));Raise(nameof(TbhValuationCoverage));Raise(nameof(TbhEmpty));Raise(nameof(TbhNextAction));
    }
}
