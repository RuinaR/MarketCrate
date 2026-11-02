namespace MarketCrate.Core;

public static class TbhScope
{
    public const int AppId=3678970;
    public const string Context="2";
    public const string Name="TBH: Task Bar Hero";
}
// Game IDs are local lookup keys only. They are never Steam asset IDs or sale commands.
public sealed record TbhOwnedItem(string ItemKey,string Name,string? MarketHash,int Quantity,bool Locked,string Location,bool Ambiguous=false,bool Equipped=false);
public sealed record TbhSaveSnapshot(DateTimeOffset CapturedAt,DateTimeOffset FileWrittenAt,IReadOnlyList<TbhOwnedItem> Items,int UnknownKeys);
