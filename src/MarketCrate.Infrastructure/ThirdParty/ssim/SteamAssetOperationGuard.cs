// Copyright 2026 Santer.xyz
// SPDX-License-Identifier: Apache-2.0 (full text in LICENSE in this directory).
// C# adaptation of SSIM MoneyOps.ts, commit 28a503fa79869f3233814d0deaa906a8b3f66621:
// https://github.com/Santerxyz/SSIM/blob/28a503fa79869f3233814d0deaa906a8b3f66621/src/trading/MoneyOps.ts
// Modified for MarketCrate: thread-safe, store-scoped ownership and disposable leases.
namespace MarketCrate.Infrastructure.Steam;

public sealed class SteamAssetOperationGuard
{
    private readonly object gate=new();
    private readonly HashSet<(string Account,string Asset)> inFlight=[];
    public IDisposable? TryClaim(string account,string asset)=>TryClaimAll(account,[asset]);
    public IDisposable? TryClaimAll(string account,IEnumerable<string> assets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(account);
        var keys=assets.Select(asset=>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(asset);
            return (Account:account.ToLowerInvariant(),Asset:asset);
        }).Distinct().ToArray();
        if(keys.Length==0)throw new ArgumentException("AssetOperationKeys");
        lock(gate)
        {
            if(keys.Any(inFlight.Contains))return null; // Claim all or none, including duplicates within one input.
            foreach(var key in keys)inFlight.Add(key);
            return new Lease(this,keys);
        }
    }
    private void Release((string Account,string Asset)[] keys)
    {lock(gate){foreach(var key in keys)inFlight.Remove(key);}}
    private sealed class Lease(SteamAssetOperationGuard owner,(string Account,string Asset)[] keys):IDisposable
    {
        private SteamAssetOperationGuard? held=owner;
        public void Dispose()=>Interlocked.Exchange(ref held,null)?.Release(keys);
    }
}
