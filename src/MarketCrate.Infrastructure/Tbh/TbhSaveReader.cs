using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Tbh;

// ES3 decoding protocol independently implemented from the public TBHIndex/MIT read-only reference.
// No Steam secrets, process inspection, game-install access, save rewriting or server upload.
public static class TbhSaveReader
{
    public const int MaximumBytes=20*1024*1024;
    public static string DefaultPath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"AppData","LocalLow","TesseractStudio","TaskbarHero","SaveFile_Live.es3");
    private sealed record CatalogRow(string Key,string Name,string? MarketHash,string Kind="Unknown",string Grade="",string? ImageHash=null);
    private static readonly Lazy<Dictionary<string,CatalogRow>> Catalog=new(()=>
    {
        using var stream=typeof(TbhSaveReader).Assembly.GetManifestResourceStream("MarketCrate.Tbh.items.json")!;
        return JsonSerializer.Deserialize<CatalogRow[]>(stream)!.ToDictionary(x=>x.Key,StringComparer.Ordinal);
    });
    public static async Task<TbhSaveSnapshot> ReadAsync(string path,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        var full=Path.GetFullPath(path);
        if(!string.Equals(Path.GetFileName(full),"SaveFile_Live.es3",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("UnsupportedSaveName");
        for(var dir=new DirectoryInfo(Path.GetDirectoryName(full)!);dir is not null;dir=dir.Parent)
            if((dir.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("SavePathBoundary");
        if((File.GetAttributes(full)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("SavePathBoundary");
        for(int attempt=0;attempt<3;attempt++)
        {
            byte[]? first=null,second=null;
            try
            {
                var written=File.GetLastWriteTimeUtc(full);first=await CopyAsync(full,token);second=await CopyAsync(full,token);
                if(written!=File.GetLastWriteTimeUtc(full)||!CryptographicOperations.FixedTimeEquals(SHA256.HashData(first),SHA256.HashData(second)))
                {if(attempt==2)throw new IOException("SaveChanging");await Task.Delay(100,token);continue;}
                token.ThrowIfCancellationRequested();return ParseEncrypted(first,new(written,TimeSpan.Zero));
            }
            finally{if(first is not null)CryptographicOperations.ZeroMemory(first);if(second is not null)CryptographicOperations.ZeroMemory(second);}
        }
        throw new IOException("SaveChanging");
    }
    private static async Task<byte[]> CopyAsync(string path,CancellationToken token)
    {
        // A memory snapshot: no decrypted or encrypted user copy is persisted to disk.
        await using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete,81920,FileOptions.Asynchronous|FileOptions.SequentialScan);
        if(file.Length<32||file.Length>MaximumBytes)throw new InvalidDataException("SaveSize");
        using var copy=new MemoryStream();var buffer=new byte[81920];
        try{int n;while((n=await file.ReadAsync(buffer,token))>0){if(copy.Length+n>MaximumBytes)throw new InvalidDataException("SaveSize");copy.Write(buffer,0,n);}return copy.ToArray();}
        finally{CryptographicOperations.ZeroMemory(buffer);if(copy.TryGetBuffer(out var bytes))CryptographicOperations.ZeroMemory(bytes.AsSpan());}
    }
    public static TbhSaveSnapshot ParseEncrypted(byte[] encrypted,DateTimeOffset written)
    {
        string stage="SaveDecrypt";
        try{return ParseEncryptedCore(encrypted,written,value=>stage=value);}
        catch(Exception error)when(stage!="SavePlayerData"&&(error is CryptographicException or InvalidDataException or IOException))
        {DiagnosticHub.Emit(DiagnosticEvent.TbhReadContract,DiagnosticArea.Data,DiagnosticOutcome.Failed,LogLevel.Warning,error is CryptographicException?DiagnosticError.Parsing:DiagnosticError.Schema,reason:stage,exception:error);throw;}
    }
    private static TbhSaveSnapshot ParseEncryptedCore(byte[] encrypted,DateTimeOffset written,Action<string> stage)
    {
        if(encrypted.Length<32||encrypted.Length>MaximumBytes||(encrypted.Length-16)%16!=0)throw new InvalidDataException("SaveFormat");
        // Public application format password, not a user's credential; never read game binaries for it.
        var key=Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes("emuMqG3bLYJ938ZDCfieWJ"),encrypted.AsSpan(0,16),100,HashAlgorithmName.SHA1,16);
        byte[]? plain=null;
        try
        {
            using var aes=Aes.Create();aes.Key=key;plain=aes.DecryptCbc(encrypted.AsSpan(16),encrypted.AsSpan(0,16),PaddingMode.PKCS7);
            if(plain.Length<2)throw new InvalidDataException("SaveFormat");
            if(plain[0]==31&&plain[1]==139)
            {
                stage("SaveCompression");using var input=new MemoryStream(plain);using var gzip=new GZipStream(input,CompressionMode.Decompress);using var output=new MemoryStream();var block=new byte[8192];
                try{int n;while((n=gzip.Read(block))>0){if(output.Length+n>MaximumBytes)throw new InvalidDataException("SaveExpansion");output.Write(block,0,n);}CryptographicOperations.ZeroMemory(plain);plain=output.ToArray();}
                finally{CryptographicOperations.ZeroMemory(block);if(output.TryGetBuffer(out var b))CryptographicOperations.ZeroMemory(b.AsSpan());}
            }
            stage("SavePlayerData");return ParsePlain(plain,written);
        }
        finally{CryptographicOperations.ZeroMemory(key);if(plain is not null)CryptographicOperations.ZeroMemory(plain);}
    }
    public static TbhSaveSnapshot ParsePlain(ReadOnlyMemory<byte> plain,DateTimeOffset written)
    {
        string stage="SavePlayerData";
        try{return ParsePlainCore(plain,written,value=>stage=value);}
        catch(Exception error)when(error is JsonException or InvalidOperationException or KeyNotFoundException or InvalidDataException or ArgumentException or OverflowException)
        {DiagnosticHub.Emit(DiagnosticEvent.TbhReadContract,DiagnosticArea.Data,DiagnosticOutcome.Failed,LogLevel.Warning,error is JsonException?DiagnosticError.Parsing:DiagnosticError.Schema,reason:stage,exception:error);throw;}
    }
    private static TbhSaveSnapshot ParsePlainCore(ReadOnlyMemory<byte> plain,DateTimeOffset written,Action<string> stage)
    {
        if(plain.Length>MaximumBytes)throw new InvalidDataException("SaveSize");
        using var root=JsonDocument.Parse(plain,new(){MaxDepth=64});
        // Intentionally ignore AccountSaveData and every unrelated root field.
        using var player=JsonDocument.Parse(root.RootElement.GetProperty("PlayerSaveData").GetProperty("value").GetString()!,new(){MaxDepth=64});
        stage("SaveOwnedItems");var ps=player.RootElement;var items=ReadArray(ps,"itemSaveDatas",true).ToDictionary(x=>Id(x.GetProperty("UniqueId")),StringComparer.Ordinal);
        DiagnosticHub.Emit(DiagnosticEvent.TbhReadContract,DiagnosticArea.Data,reason:"SaveOwnedItems",count:items.Count);
        stage("SaveStorageSlots");var refs=new List<(string Id,JsonElement Slot,string Location,bool Blocked)>();
        foreach(var (field,label) in new[]{("stashSaveDatas","창고"),("inventorySaveDatas","가방"),("remakeTradingStashSaveDatas","거래 선적"),("tradingStashSaveDatas","거래 선적")})
        {
            if(field=="tradingStashSaveDatas"&&ps.TryGetProperty("remakeTradingStashSaveDatas",out _))continue;
            foreach(var slot in ReadArray(ps,field,field is "stashSaveDatas" or "inventorySaveDatas"))
            {
                var id=Id(slot.GetProperty("ItemUniqueId"));if(id=="0")continue;var index=slot.GetProperty("Index").GetInt32();if(index is <0 or >100000)throw new InvalidDataException("SlotIndex");
                var where=field=="stashSaveDatas"?$"{index/49+1}번 창고 · {index%7+1}열 {index%49/7+1}행 (7×7 기준)":$"{label} · 슬롯 {index+1}";
                refs.Add((id,slot,where,Flag(slot,"IsBlocked")));
            }
        }
        DiagnosticHub.Emit(DiagnosticEvent.TbhReadContract,DiagnosticArea.Data,reason:"SaveStorageSlots",count:refs.Count);
        stage("SaveEquipment");var heroes=ReadArray(ps,"heroSaveDatas",false);
        DiagnosticHub.Emit(DiagnosticEvent.TbhReadContract,DiagnosticArea.Data,ps.TryGetProperty("heroSaveDatas",out _)?DiagnosticOutcome.Succeeded:DiagnosticOutcome.Unknown,reason:"SaveEquipment",count:ps.TryGetProperty("heroSaveDatas",out _)?heroes.Length:null);
        foreach(var hero in heroes)
        {
            var equip=ReadArray(hero,"equippedItemIds",false);var blocked=ReadArray(hero,"equippedItemBlocked",false);
            for(int i=0;i<equip.Length;i++){var id=Id(equip[i]);if(id!="0")refs.Add((id,default,$"영웅 {Id(hero.GetProperty("heroKey"))} · 장비 슬롯 {i+1}",i<blocked.Length&&BoolFlag(blocked[i])));}
        }
        // New dedicated material storage has no grid Index. Its item key is an
        // additional cross-check, not a replacement for the owned item reference.
        stage("SaveMaterialStorage");var materials=ReadArray(ps,"materialStashSaveDatas",false);
        DiagnosticHub.Emit(DiagnosticEvent.TbhReadContract,DiagnosticArea.Data,ps.TryGetProperty("materialStashSaveDatas",out _)?DiagnosticOutcome.Succeeded:DiagnosticOutcome.Unknown,reason:"SaveMaterialStorage",count:ps.TryGetProperty("materialStashSaveDatas",out _)?materials.Length:null);
        foreach(var slot in materials)
        {
            var quantity=slot.GetProperty("Quantity").GetInt32();
            if(quantity<0||quantity>1000000000)throw new InvalidDataException("SaveQuantity");
            if(quantity==0)continue;
            var id=Id(slot.GetProperty("ItemUniqueId"));var key=Id(slot.GetProperty("ItemKey"));
            if(id=="0"||!items.TryGetValue(id,out var material)||Id(material.GetProperty("ItemKey"))!=key)
                throw new InvalidDataException("MaterialReferenceMismatch");
            refs.Add((id,slot,"재료창고",Flag(slot,"IsBlocked")));
        }
        if(refs.Count>100000)throw new InvalidDataException("SaveRows");var duplicate=refs.GroupBy(x=>x.Id).Where(x=>x.Count()>1).Select(x=>x.Key).ToHashSet();var result=new List<TbhOwnedItem>();int unknown=0;
        stage("SaveCatalogMapping");int unmapped=0;
        foreach(var row in refs)
        {
            if(!items.TryGetValue(row.Id,out var item))throw new InvalidDataException("MissingItemReference");
            var key=Id(item.GetProperty("ItemKey"));var transferred=key.EndsWith("900",StringComparison.Ordinal)&&key.Length>6;var lookup=transferred?key[..^3]:key;
            Catalog.Value.TryGetValue(lookup,out var definition);if(definition is null)unknown++;
            var quantity=row.Slot.ValueKind==JsonValueKind.Object?row.Slot.GetProperty("Quantity").GetInt32():1;if(quantity is <1 or >1000000000)throw new InvalidDataException("SaveQuantity");
            var ambiguous=duplicate.Contains(row.Id)||transferred||row.Location.StartsWith("거래 선적",StringComparison.Ordinal);
            if(definition is not null&&!ambiguous&&definition.MarketHash is null)unmapped++;
            result.Add(new(key,definition?.Name??"알 수 없는 아이템 "+key,ambiguous?null:definition?.MarketHash,quantity,row.Blocked||Flag(item,"IsBlocked"),row.Location,ambiguous,row.Location.StartsWith("영웅",StringComparison.Ordinal),Enum.TryParse<TbhItemKind>(definition?.Kind,out var kind)?kind:TbhItemKind.Unknown,definition?.Grade??"",definition?.ImageHash,definition is not null));
        }
        DiagnosticHub.Emit(DiagnosticEvent.TbhReadContract,DiagnosticArea.Data,DiagnosticOutcome.Unknown,reason:"CatalogUnknown",count:unknown);
        DiagnosticHub.Emit(DiagnosticEvent.TbhReadContract,DiagnosticArea.Data,DiagnosticOutcome.Unknown,reason:"CatalogUnmapped",count:unmapped);
        DiagnosticHub.Emit(DiagnosticEvent.TbhReadContract,DiagnosticArea.Data,reason:"CatalogMatched",count:result.Count(x=>x.MarketHash is not null));
        return new(DateTimeOffset.UtcNow,written,result,unknown);
    }
    private static JsonElement[] ReadArray(JsonElement obj,string name,bool required)
    {
        if(!obj.TryGetProperty(name,out var value)){if(required)throw new InvalidDataException("SaveFieldMissing");return [];}
        if(value.ValueKind==JsonValueKind.String){using var nested=JsonDocument.Parse(value.GetString()!,new(){MaxDepth=64});return nested.RootElement.EnumerateArray().Select(x=>x.Clone()).ToArray();}
        return value.EnumerateArray().Select(x=>x.Clone()).ToArray();
    }
    private static bool Flag(JsonElement obj,string name)=>obj.TryGetProperty(name,out var flag)&&BoolFlag(flag);
    private static bool BoolFlag(JsonElement value)=>value.ValueKind switch{JsonValueKind.True=>true,JsonValueKind.False=>false,_=>throw new InvalidDataException("SaveFlagType")};
    private static string Id(JsonElement value)
    {var s=value.ValueKind==JsonValueKind.String?value.GetString()!:value.GetRawText();if(s.Length is <1 or >30||s.Any(x=>!char.IsAsciiDigit(x)))throw new InvalidDataException("SaveIdentifier");return s;}
}
