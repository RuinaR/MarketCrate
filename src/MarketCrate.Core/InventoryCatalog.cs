namespace MarketCrate.Core;

public enum InventoryScopeState { NotQueried, Loaded, Empty, PrivateOrDenied, Unsupported, Failed, Partial }
public sealed record InventoryScope(int AppId,string ContextId,string Name)
{
    public void Validate()
    {
        if(AppId<1||ContextId.Length is <1 or >20||ContextId.Any(c=>c is <'0' or >'9')||!ulong.TryParse(ContextId,out var context)||context==0||context.ToString(System.Globalization.CultureInfo.InvariantCulture)!=ContextId||string.IsNullOrWhiteSpace(Name)||Name.Length>80||Name.Any(char.IsControl))throw new ArgumentException("InventoryScopeInvalid");
    }
}
