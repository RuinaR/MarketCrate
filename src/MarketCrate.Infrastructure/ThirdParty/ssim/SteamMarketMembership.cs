// Copyright 2026 Santer.xyz
// SPDX-License-Identifier: Apache-2.0 (LICENSE and NOTICE in this directory).
// C# partial adaptation of src/core/MarketModel.ts (parseMyListings / mergeParsed),
// SSIM commit 28a503fa79869f3233814d0deaa906a8b3f66621.
// https://github.com/Santerxyz/SSIM/blob/28a503fa79869f3233814d0deaa906a8b3f66621/src/core/MarketModel.ts
// Modified: strict identities and conflicts; absent money/metadata stays unavailable, never zero-filled.
using System.Collections.Immutable;
using System.Text.Json;
using MarketCrate.Core;
namespace MarketCrate.Infrastructure.Steam;

public sealed record SteamListingPresence(AssetKey Asset,string? ListingId,SaleState State);
internal static class SteamMarketMembership
{
    internal static IEnumerable<(JsonElement Row,SaleState State)> Rows(JsonElement root)
    {
        var shaped=false;
        foreach(var (name,state) in new[]{("listings",SaleState.Listed),("pending_listings",SaleState.AwaitingSteamGuard),("listings_to_confirm",SaleState.AwaitingSteamGuard),("listings_on_hold",SaleState.Submitted)})
        {
            if(!root.TryGetProperty(name,out var bucket))continue;
            if(bucket.ValueKind!=JsonValueKind.Array)throw new SteamException(SteamFailure.ContractChanged);
            shaped=true;foreach(var row in bucket.EnumerateArray())yield return(row,state);
        }
        if(!shaped)throw new SteamException(SteamFailure.ContractChanged);
    }
    internal static AssetKey Identity(JsonElement asset)
    {
        var app=checked((int)SteamContract.Integer(asset.GetProperty("appid")));
        var context=SteamContract.Id(asset.GetProperty("contextid"));
        var id=SteamContract.Id(asset.TryGetProperty("id",out var value)?value:asset.GetProperty("assetid"));
        if(app<1)throw new SteamException(SteamFailure.ContractChanged);
        return new(app,context,id);
    }
    internal static string? ListingId(JsonElement row)
    {
        if(!row.TryGetProperty("listingid",out var value)||value.ValueKind==JsonValueKind.Null||value.ValueKind==JsonValueKind.String&&value.GetString()=="")return null;
        return SteamContract.Id(value);
    }
    internal static ImmutableArray<SteamListingPresence> Read(JsonElement root)
    {
        var found=new Dictionary<(AssetKey Asset,string? Listing),SteamListingPresence>();
        var listingIds=new Dictionary<string,SteamListingPresence>();
        foreach(var (row,state) in Rows(root))
        {
            var asset=Identity(row.GetProperty("asset"));
            var id=ListingId(row);
            var current=new SteamListingPresence(asset,id,state);
            if(id is not null&&listingIds.TryGetValue(id,out var old)&&old!=current)throw new SteamException(SteamFailure.ContractChanged);
            if(id is not null)listingIds[id]=current;
            found[(asset,id)]=current;
        }
        // The assets map is a conservative market-held superset, not proof of a posted listing.
        var referenced=found.Values.Select(x=>x.Asset).ToHashSet();
        if(root.TryGetProperty("assets",out var assets))
        {
            if(assets.ValueKind!=JsonValueKind.Object)throw new SteamException(SteamFailure.ContractChanged);
            foreach(var app in assets.EnumerateObject())foreach(var context in app.Value.EnumerateObject())foreach(var asset in context.Value.EnumerateObject())
            {
                var key=Identity(JsonSerializer.SerializeToElement(new{appid=app.Name,contextid=context.Name,id=asset.Name}));
                if(!referenced.Contains(key))found[(key,null)]=new(key,null,SaleState.Unknown);
            }
        }
        return found.Values.ToImmutableArray();
    }
}
