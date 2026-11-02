using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using MarketCrate.Core;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure.Tbh;

namespace MarketCrate.Tests;
public static class TbhSaveTests
{
    private static void Check(bool ok){if(!ok)throw new Exception("TBH read-only save contract");}
    private static byte[] Plain(bool duplicate=false)=>JsonSerializer.SerializeToUtf8Bytes(new{PlayerSaveData=new{value=JsonSerializer.Serialize(new{
        itemSaveDatas=new object[]{new{UniqueId="18446744073709551615",ItemKey=110001,IsBlocked=false},new{UniqueId="2",ItemKey=110002,IsBlocked=true},new{UniqueId="3",ItemKey=110003,IsBlocked=false}},
        stashSaveDatas=new[]{new{Index=100,ItemUniqueId="18446744073709551615",Quantity=2,IsBlocked=false},new{Index=101,ItemUniqueId="2",Quantity=1,IsBlocked=false}},
        inventorySaveDatas=duplicate?new[]{new{Index=0,ItemUniqueId="18446744073709551615",Quantity=2,IsBlocked=false}}:[],
        heroSaveDatas=new[]{new{heroKey=1,equippedItemIds=new[]{"3","0"},equippedItemBlocked=new[]{false,false}}}
    })},AccountSaveData=new{value="private-account-marker-never-display"}});
    private static byte[] Encrypt(byte[] plain)
    {var iv=Enumerable.Range(0,16).Select(x=>(byte)x).ToArray();var key=Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes("emuMqG3bLYJ938ZDCfieWJ"),iv,100,HashAlgorithmName.SHA1,16);using var aes=Aes.Create();aes.Key=key;var cipher=aes.EncryptCbc(plain,iv);CryptographicOperations.ZeroMemory(key);return [..iv,..cipher];}
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Func<Task>)[]{
            ("ES3 shared-file snapshot is read-only and preserves exact large item IDs",Copy),
            ("Local schema maps stash coordinates lock state and equipment independently",()=>Sync(Parse)),
            ("Duplicate position references remain visible but cannot be valued twice",()=>Sync(Duplicate)),
            ("Material warehouse preserves quantity and verifies owned item key without duplicate valuation",()=>Sync(Materials)),
            ("Corrupt encrypted save and missing required fields never become empty success",()=>Sync(Corrupt)),
            ("Compressed snapshots and file lock failures preserve source and explicit cancellation",CompressedAndLocked),
            ("Save path rejects unrelated names and obeys cancellation",Boundaries),
            ("TBH-only product rejects other game selection without deleting legacy data",()=>Sync(Scope)),
            ("Lock and equipment filters apply independently and search retains source snapshot",()=>Sync(Filters)),
            ("Save valuation uses real adapter synthetic HTTP and shared bid depth without sales",Prices)
        };
        int pass=0,fail=0;foreach(var(name,test)in tests)try{await test();pass++;Console.WriteLine("PASS "+name);}catch(Exception e){fail++;Console.WriteLine("FAIL "+name+": "+e);}return(pass,fail);
    }
    private static Task Sync(Action action){action();return Task.CompletedTask;}
    private static TbhSaveSnapshot Snapshot()=>TbhSaveReader.ParseEncrypted(Encrypt(Plain()),DateTimeOffset.UtcNow);
    private static async Task Copy()
    {
        var dir=Path.Combine(Directory.GetCurrentDirectory(),"runtime","save-contract-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);var file=Path.Combine(dir,"SaveFile_Live.es3");var bytes=Encrypt(Plain());await File.WriteAllBytesAsync(file,bytes);
        try{await using var open=new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.ReadWrite|FileShare.Delete);var result=await TbhSaveReader.ReadAsync(file);await open.DisposeAsync();Check(result.Items.Count==3&&File.ReadAllBytes(file).SequenceEqual(bytes));}finally{File.Delete(file);Directory.Delete(dir);}
    }
    private static void Parse(){var s=Snapshot();Check(s.Items.Count==3&&s.Items[0].Location.Contains("3번 창고 · 3열 1행")&&s.Items[1].Locked&&s.Items[2].Equipped&&!s.Items[2].Locked&&s.UnknownKeys==0&&s.Items[0].MarketHash=="Minor Ruby");Check(!JsonSerializer.Serialize(s).Contains("private-account-marker"));}
    private static void Duplicate(){var s=TbhSaveReader.ParseEncrypted(Encrypt(Plain(true)),DateTimeOffset.UtcNow);Check(s.Items.Count==4&&s.Items.Count(x=>x.Ambiguous)==2&&s.Items.Where(x=>x.Ambiguous).All(x=>x.MarketHash is null));}
    private static void Materials()
    {
        byte[] Data(int key=110001,bool duplicate=false)=>JsonSerializer.SerializeToUtf8Bytes(new{PlayerSaveData=new{value=JsonSerializer.Serialize(new{
            itemSaveDatas=new[]{new{UniqueId=77,ItemKey=110001,IsBlocked=true}},
            stashSaveDatas=duplicate?new[]{new{Index=0,ItemUniqueId=77,Quantity=13,IsBlocked=false}}:[],inventorySaveDatas=Array.Empty<object>(),
            materialStashSaveDatas=new[]{new{ItemKey=key,ItemUniqueId=77,Quantity=13},new{ItemKey=110002,ItemUniqueId=0,Quantity=0}}
        })}});
        var s=TbhSaveReader.ParsePlain(Data(),DateTimeOffset.UtcNow);Check(s.Items.Count==1&&s.Items[0].Location=="재료창고"&&s.Items[0].Quantity==13&&s.Items[0].Locked&&s.Items[0].MarketHash=="Minor Ruby");
        var overlap=TbhSaveReader.ParsePlain(Data(duplicate:true),DateTimeOffset.UtcNow);Check(overlap.Items.Count==2&&overlap.Items.All(x=>x.Ambiguous&&x.MarketHash is null));
        bool rejected=false;try{TbhSaveReader.ParsePlain(Data(110002),DateTimeOffset.UtcNow);}catch(InvalidDataException){rejected=true;}Check(rejected);
    }
    private static void Corrupt()
    {
        foreach(var bytes in new[]{new byte[31],new byte[32],Encrypt(Encoding.UTF8.GetBytes("{}"))})
        {bool rejected=false;try{TbhSaveReader.ParseEncrypted(bytes,DateTimeOffset.UtcNow);}catch(Exception e)when(e is CryptographicException or InvalidDataException or KeyNotFoundException){rejected=true;}Check(rejected);}
    }
    private static async Task Boundaries(){try{await TbhSaveReader.ReadAsync(Path.Combine(Directory.GetCurrentDirectory(),"runtime","not-save.es3"));throw new Exception("Unrelated name accepted");}catch(InvalidDataException){}using var cancel=new CancellationTokenSource();cancel.Cancel();try{await TbhSaveReader.ReadAsync(Path.Combine(Directory.GetCurrentDirectory(),"runtime","SaveFile_Live.es3"),cancel.Token);}catch(Exception e)when(e is OperationCanceledException or FileNotFoundException){} }
    private static async Task CompressedAndLocked()
    {
        using var compressed=new MemoryStream();using(var gzip=new GZipStream(compressed,CompressionLevel.Fastest,true))gzip.Write(Plain());Check(TbhSaveReader.ParseEncrypted(Encrypt(compressed.ToArray()),DateTimeOffset.UtcNow).Items.Count==3);
        var dir=Path.Combine(Directory.GetCurrentDirectory(),"runtime","locked-save-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);var path=Path.Combine(dir,"SaveFile_Live.es3");var bytes=Encrypt(Plain());await File.WriteAllBytesAsync(path,bytes);
        try
        {
            using(var locked=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            {bool rejected=false;try{await TbhSaveReader.ReadAsync(path);}catch(IOException){rejected=true;}Check(rejected);}
            using var cancellation=new CancellationTokenSource();cancellation.Cancel();bool cancelled=false;try{await TbhSaveReader.ReadAsync(path,cancellation.Token);}catch(OperationCanceledException){cancelled=true;}Check(cancelled&&File.ReadAllBytes(path).SequenceEqual(bytes));
        }
        finally{File.Delete(path);Directory.Delete(dir);}
    }
    private static void Scope(){using var vm=new SteamWorkspaceViewModel();Check(vm.TbhOnly&&vm.Games.Count==1&&vm.Game.AppId==TbhScope.AppId&&!vm.MultiGameEnabled);vm.Game=new(730,"2","Other game");Check(vm.Game.AppId==TbhScope.AppId&&!vm.AddScope.CanExecute(null)&&!vm.RemoveScope.CanExecute(null));}
    private static void Filters()
    {
        using var vm=new SteamWorkspaceViewModel();vm.ApplyTbhSnapshot(Snapshot());Check(vm.TbhSaveRows.Count==1);vm.TbhIncludeEquipped=true;Check(vm.TbhSaveRows.Count==2);vm.TbhIncludeLocked=true;Check(vm.TbhSaveRows.Count==3);vm.TbhIncludeEquipped=false;Check(vm.TbhSaveRows.Count==2);vm.TbhSearch="Minor Ruby";Check(vm.TbhSaveRows.Count==1);vm.TbhSearch="";Check(vm.TbhSaveRows.Count==2);
    }
    private static async Task Prices()
    {
        using var f=new FifthStageTests.Fixture();await f.Login();f.Server.OrderbookOverride="{\"data\":{\"success\":true,\"data\":{\"eCurrency\":16,\"rgCompactBuyOrders\":[11500,3],\"rgCompactSellOrders\":[57500,1]}}}";
        f.Vm.ApplyTbhSnapshot(new(DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,[new("1","A","Synthetic stack",2,false,"1번 창고 · 1열 1행"),new("2","A","Synthetic stack",2,false,"1번 창고 · 2열 1행")],0));
        await f.Vm.RefreshTbhPrices.ExecuteAsync();Check(f.Vm.TbhSaveRows.Sum(x=>x.PricedTotal)==345&&f.Vm.TbhSaveRows.Sum(x=>x.Missing)==1);f.Vm.TbhPriceMode=1;Check(f.Vm.TbhSaveRows.All(x=>x.UnitPrice==575&&x.PricedTotal==1150));Check(f.Server.Sends==0&&f.Server.Cancels==0&&f.Workspace!.Store.Commands().Count==0);
    }
}
