using System.Collections.Immutable;

namespace MarketCrate.Core;

public static class Pricing
{
    // Demo policy, not a claim of exact Steam currency-specific settlement.
    public static FeeQuote FromSeller(long seller, FeePolicy policy)
    {
        if (seller < 1 || policy.SteamBasisPoints < 0 || policy.GameBasisPoints < 0 || policy.MinimumSteam < 0 || policy.MinimumGame < 0)
            throw new ArgumentOutOfRangeException(nameof(seller));
        var steam = Math.Max(policy.MinimumSteam, checked(seller * policy.SteamBasisPoints) / 10000);
        var game = policy.GameBasisPoints == 0 ? 0 : Math.Max(policy.MinimumGame, checked(seller * policy.GameBasisPoints) / 10000);
        return new(checked(seller + steam + game), seller, steam, game);
    }

    public static FeeQuote FromBuyer(long buyer, FeePolicy policy)
    {
        if (buyer < FromSeller(1, policy).BuyerPays) throw new ArgumentOutOfRangeException(nameof(buyer), "최소 수수료를 충족하지 못합니다.");
        long low = 1, high = buyer;
        while (low < high)
        {
            var mid = low + (high - low + 1) / 2;
            if (FromSeller(mid, policy).BuyerPays <= buyer) low = mid; else high = mid - 1;
        }
        var quote = FromSeller(low, policy);
        // Unrepresentable integer gap is explicitly included in Steam fee in this demo policy.
        return quote with { BuyerPays = buyer, SteamFee = checked(quote.SteamFee + buyer - quote.BuyerPays) };
    }

    public static long? WeightedAverage(IEnumerable<TradeSample> trades)
    {
        long value = 0, volume = 0;
        foreach (var trade in trades)
        {
            if (trade.BuyerPays < 1 || trade.Volume < 1) throw new ArgumentException("잘못된 거래 표본");
            value = checked(value + checked(trade.BuyerPays * trade.Volume));
            volume = checked(volume + trade.Volume);
        }
        return volume == 0 ? null : checked((value + volume / 2) / volume);
    }

    public static AllocationResult Allocate(int quantity, IEnumerable<BidLevel> bids)
    {
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        var levels = bids.ToArray();
        if (levels.Any(x => x.Quantity < 1 || x.BuyerPays < 1)) throw new ArgumentException("잘못된 구매주문");
        var lines = ImmutableArray.CreateBuilder<Allocation>();
        foreach (var bid in levels.OrderByDescending(x => x.BuyerPays))
        {
            var count = Math.Min(quantity, bid.Quantity);
            if (count > 0) lines.Add(new(bid.BuyerPays, count));
            quantity -= count;
            if (quantity == 0) break;
        }
        return new(lines.ToImmutable(), quantity);
    }
}

public static class BatchValidation
{
    public static void Validate(SaleBatchSnapshot batch, DateTimeOffset now)
    {
        if (batch.BatchId == Guid.Empty || string.IsNullOrWhiteSpace(batch.AccountReference) || batch.Lines.IsDefaultOrEmpty)
            throw new ArgumentException("빈 배치");
        if (batch.FinalizedAt > now.AddSeconds(5) || now - batch.FinalizedAt > TimeSpan.FromMinutes(5)) throw new ArgumentException("검토를 갱신하세요.");
        if (batch.Lines.Select(x => x.LineId).Distinct().Count() != batch.Lines.Length) throw new ArgumentException("중복 행 ID");
        foreach (var group in batch.Lines.GroupBy(x => x.Asset.Key))
        {
            var asset = group.First().Asset;
            if (group.Any(x => x.Asset != asset) || group.Sum(x => x.Quantity) > asset.Quantity || (!asset.Stackable && group.Sum(x => x.Quantity) > 1))
                throw new ArgumentException("중복 자산 또는 수량 초과");
        }
        foreach (var line in batch.Lines)
        {
            var q = line.UnitQuote;
            if (!line.Asset.Marketable || line.Asset.Locked || line.Quantity < 1 || line.Currency is not ("KRW" or "USD") || q.SellerReceives < 1 || q.SteamFee < 0 || q.GameFee < 0 ||
                q.BuyerPays != checked(q.SellerReceives + q.SteamFee + q.GameFee)) throw new ArgumentException("판매 조건/통화/수수료 오류");
            if (line.MarketTime > now.AddSeconds(5) || now - line.MarketTime > TimeSpan.FromMinutes(5)) throw new ArgumentException("오래된 시세: 검토를 갱신하세요.");
        }
        _ = batch.Gross; _ = batch.Net;
    }
}
