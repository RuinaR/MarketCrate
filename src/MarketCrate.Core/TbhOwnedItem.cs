namespace MarketCrate.Core;

public static class TbhScope
{
    public const int AppId=3678970;
    public const string Context="2";
    public const string Name="TBH: Task Bar Hero";
}
public enum TbhItemKind { Unknown, Equipment, Material, Other }
// Game IDs are local lookup keys only. They are never Steam asset IDs or sale commands.
public sealed record TbhOwnedItem(string ItemKey,string Name,string? MarketHash,int Quantity,bool Locked,string Location,bool Ambiguous=false,bool Equipped=false,TbhItemKind Kind=TbhItemKind.Unknown,string Grade="",string? ImageMarketHash=null,bool CatalogKnown=true);
public sealed record TbhSaveSnapshot(DateTimeOffset CapturedAt,DateTimeOffset FileWrittenAt,IReadOnlyList<TbhOwnedItem> Items,int UnknownKeys);

// Read evidence states, not transaction/journal states.
public enum TbhValueReadState { NotQueried, UnknownItemKey, MarketMappingMissing, SourceAmbiguous, Ready, PartialDepth, NoBuyOrders, SharedDepthExhausted, NoSellOrders, SellDepthMissing, RequestFailed, AmountRange }
