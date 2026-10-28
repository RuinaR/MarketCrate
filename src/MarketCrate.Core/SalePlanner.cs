using System.Collections.Immutable;

namespace MarketCrate.Core;

public record SaleSelection(InventoryAsset Asset, PriceSnapshot Price, int Quantity);
public record PlannedSale(SaleSelection Selection, Allocation Allocation);
public record UnpricedSale(SaleSelection Selection, int Quantity);
public record SalePlan(ImmutableArray<PlannedSale> Lines, int UnpricedQuantity)
{
    public ImmutableArray<UnpricedSale> Unpriced { get; init; } = [];
}

public static class SalePlanner
{
    public static SalePlan Build(IEnumerable<SaleSelection> selections, bool immediate)
    {
        using var operation=DiagnosticHub.Operation(DiagnosticEvent.PricePlan,DiagnosticArea.Pricing,"D05");
        var selected = selections.ToArray();
        if (selected.Select(x => x.Asset.Key).Distinct().Count() != selected.Length) throw new ArgumentException("중복 선택 자산");
        var remaining = new Dictionary<(int, string, string), List<BidLevel>>();
        var books = new Dictionary<(int, string, string), ImmutableArray<BidLevel>>();
        var result = ImmutableArray.CreateBuilder<PlannedSale>();
        var unpriced = ImmutableArray.CreateBuilder<UnpricedSale>();
        var missing = 0;
        foreach (var entry in selected)
        {
            if (entry.Quantity < 1 || entry.Quantity > entry.Asset.Quantity || !entry.Asset.Marketable || entry.Asset.Locked)
                throw new ArgumentException("가용 수량/판매 상태 변경: 검토를 갱신하세요.");
            AllocationResult plan;
            if (immediate)
            {
                var key = (entry.Asset.Key.AppId, entry.Asset.MarketHashName, entry.Price.Currency);
                if (!remaining.TryGetValue(key, out var bids))
                { books[key] = entry.Price.Bids; remaining[key] = bids = entry.Price.Bids.OrderByDescending(x => x.BuyerPays).ToList(); }
                else if (!books[key].SequenceEqual(entry.Price.Bids)) throw new ArgumentException("동일 품목의 시세가 서로 다릅니다. 다시 갱신하세요.");
                plan = Pricing.Allocate(entry.Quantity, bids);
                var used = entry.Quantity - plan.UnpricedQuantity;
                for (var i = 0; i < bids.Count && used > 0; i++) { var count = Math.Min(used, bids[i].Quantity); used -= count; bids[i] = bids[i] with { Quantity = bids[i].Quantity - count }; }
                bids.RemoveAll(x => x.Quantity == 0);
            }
            else plan = Pricing.WeightedAverage(entry.Price.Trades) is long average ? new([new(average, entry.Quantity)], 0) : new([], entry.Quantity);
            missing = checked(missing + plan.UnpricedQuantity);
            if (plan.UnpricedQuantity > 0) unpriced.Add(new(entry, plan.UnpricedQuantity));
            foreach (var allocation in plan.Lines) result.Add(new(entry, allocation));
        }
        DiagnosticHub.Emit(DiagnosticEvent.PricePlan,DiagnosticArea.Pricing,count:missing,reason:missing>0?"InsufficientDepth":immediate?"Immediate":"Average",requirement:"D06");
        return new(result.ToImmutable(), missing) { Unpriced = unpriced.ToImmutable() };
    }

    public static void ValidateCurrent(SaleBatchSnapshot batch, IReadOnlyList<InventoryAsset> current)
    {
        var assets = current.ToDictionary(x => x.Key);
        foreach (var group in batch.Lines.GroupBy(x => x.Asset.Key))
            if (!assets.TryGetValue(group.Key, out var asset) || asset != group.First().Asset || group.Sum(x => x.Quantity) > asset.Quantity)
                throw new ArgumentException("보관함 수량/상태가 변경되었습니다. 검토를 다시 여세요.");
    }
    public static void ValidateReservations(SaleBatchSnapshot batch, IReadOnlyList<BatchRecord> history, IReadOnlySet<string>? cancelledListings = null)
    {
        var reserved = new Dictionary<AssetKey, int>();
        foreach (var previous in history.Where(x => x.Snapshot.BatchId != batch.BatchId))
            for (var i = 0; i < previous.Results.Length; i++)
            {
                if (previous.Results[i].State is SaleState.Ready or SaleState.Failed or SaleState.Draft) continue;
                var line = previous.Snapshot.Lines[i];
                if (cancelledListings?.Contains(SaleBatchCoordinator.RequestKey(previous.Snapshot, line)) == true) continue;
                reserved[line.Asset.Key] = checked(reserved.GetValueOrDefault(line.Asset.Key) + line.Quantity);
            }
        foreach (var group in batch.Lines.GroupBy(x => x.Asset.Key))
            if (checked(group.Sum(x => x.Quantity) + reserved.GetValueOrDefault(group.Key)) > group.First().Asset.Quantity)
                throw new ArgumentException("이전 배치에서 접수/게시/미확정인 예약 수량과 중복됩니다. 중복 등록을 차단했습니다.");
    }
}
