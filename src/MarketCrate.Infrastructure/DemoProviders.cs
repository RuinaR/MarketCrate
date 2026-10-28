using System.Collections.Immutable;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure;

public sealed class DemoDataProvider(int generatedCount = 0) : IInventoryProvider, IMarketDataProvider, IMarketOrderProvider
{
    public Capability Capability => Capability.Mock;
    public Task<IReadOnlyList<InventoryAsset>> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (generatedCount > 0)
            return Task.FromResult<IReadOnlyList<InventoryAsset>>(Enumerable.Range(0, generatedCount).Select(i =>
                new InventoryAsset(new(730, "2", $"synthetic-{i}"), "Counter-Strike 2", $"Synthetic Collection {i:D5}", $"Mock Hash {i:D5}", 1, false, true, i % 2 == 0)).ToArray());
        IReadOnlyList<InventoryAsset> assets = [
            new(new(730,"2","demo-1"), "Counter-Strike 2", "Prism Collection • Case", "Demo Prism Case", 10,true,true,true),
            new(new(730,"2","demo-2"), "Counter-Strike 2", "Cobalt Circuit • Sticker", "Demo Cobalt Sticker", 4,true,true,false),
            new(new(730,"2","demo-3"), "Counter-Strike 2", "Copper Signal • Collectible", "Demo Copper", 1,false,false,true),
            new(new(570,"2","demo-4"), "Dota 2", "Moonlit Archive • Treasure", "Demo Moonlit Treasure", 7,true,true,true),
            new(new(570,"2","demo-5"), "Dota 2", "Verdant Echo • Cosmetic", "Demo Verdant Echo", 1,false,true,true),
            new(new(440,"2","demo-6"), "Team Fortress 2", "Workshop Token • Supply", "Demo Workshop Token", 6,true,true,true),
            new(new(440,"2","demo-7"), "Team Fortress 2", "Bound Keepsake", "Demo Keepsake", 1,false,true,false,true)];
        return Task.FromResult(assets);
    }
    public PriceSnapshot Get(InventoryAsset asset)
    {
        var unit = asset.Key.AppId == 730 ? 1200L : asset.Key.AppId == 570 ? 650L : 250L;
        return new("KRW", DateTimeOffset.UtcNow, "합성 Mock 데이터", TimeSpan.FromHours(24),
            [new(unit, 3), new(unit + 100, 7)], [new(unit, 3), new(unit - 20, 4), new(unit - 50, 3)]);
    }
    public IReadOnlyList<LineResult> GetOrders() => [new("demo-listing", SaleState.Listed,"Mock 판매 중 · 1개 × ₩1,200 · 비교가 ₩1,270"),
        new("demo-pending", SaleState.AwaitingSteamGuard,"Mock 승인 대기 · 게시되지 않음")];
}

public sealed class BlockedListingExecutor : IMarketListingExecutor
{
    public Capability Capability => Capability.IntegrationBlocked;
    public Task<LineResult> SubmitAsync(SaleLineSnapshot line, string requestKey, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("IntegrationBlocked: Valve 허용 연동과 명시적 활성화 필요");
    public Task<LineResult> ReconcileAsync(SaleLineSnapshot line, string requestKey, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("IntegrationBlocked: 실제 서버 조회 불가");
}

public sealed class MockListingExecutor(IDemoDataStore? evidence = null) : IMarketListingExecutor
{
    public Capability Capability => Capability.Mock;
    public int SubmitCount { get; private set; }
    public Task<LineResult> SubmitAsync(SaleLineSnapshot line, string requestKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (evidence?.ReadEvidence(requestKey) is { } known) return Task.FromResult(known);
        SubmitCount++;
        var state = line.Asset.Key.AssetId switch { "demo-2" => SaleState.Failed, "demo-4" => SaleState.Submitted, _ => SaleState.AwaitingSteamGuard };
        var result = new LineResult(line.LineId, state, state == SaleState.Failed ? "Mock 거절 · 이미 성공한 항목 재실행 금지" : "Mock 접수 · 실제 Steam 요청 없음");
        evidence?.WriteEvidence(requestKey, result);
        return Task.FromResult(result);
    }
    // No automatic approval simulation. New process has no remote evidence, so keep uncertainty/pending.
    public Task<LineResult> ReconcileAsync(SaleLineSnapshot line, string requestKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(evidence?.ReadEvidence(requestKey) ?? new LineResult(line.LineId, SaleState.Unknown, "Mock 대조: 실제 서버 증거 없음 · 자동 재전송 안 함"));
    }
}
