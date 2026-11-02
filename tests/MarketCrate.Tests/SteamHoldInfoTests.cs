using System.Text.Json;
using MarketCrate.App.ViewModels;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;
namespace MarketCrate.Tests;
public static class SteamHoldInfoTests
{
    private static void Check(bool value){if(!value)throw new Exception("hold display evidence");}
    public static SteamInventoryPage Page(string[] lines,bool tradable=false,bool marketable=false,int days=7,string[]? own=null)
    {
        var asset=new Dictionary<string,object>{["appid"]=TbhScope.AppId,["contextid"]="2",["assetid"]="101",["classid"]="22",["instanceid"]="0",["amount"]="1"};
        object[] Notes(string[] values)=>values.Select(x=>(object)new{type="html",value=x}).ToArray();
        if(own is not null)asset["owner_descriptions"]=Notes(own);
        var description=new{appid=TbhScope.AppId,classid="22",instanceid="0",name="Synthetic item",market_hash_name="Synthetic item",tradable=tradable?1:0,marketable=marketable?1:0,market_tradable_restriction=days,market_marketable_restriction=days,owner_descriptions=Notes(lines)};
        return SteamContract.Inventory(JsonSerializer.Serialize(new{success=true,assets=new[]{asset},descriptions=new[]{description}}),TbhScope.AppId,"2",TbhScope.Name);
    }
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new(string,Action)[]{
            ("Explicit GMT release time preserves trade and market distinction",()=>{var p=Page(["Tradable After Nov 9, 2026 (3:00:00) GMT"]);var h=p.Holds.Single().Value;Check(h.TradeAfter==new DateTimeOffset(2026,11,9,3,0,0,TimeSpan.Zero)&&h.MarketAfter is null);var text=h.Describe(false,false,new(2026,11,2,3,0,0,TimeSpan.Zero));Check(text.Contains("7일 0시간")&&text.Contains("KST")&&text.Contains("장터 판매 불가 · 해제 시각 미확인"));}),
            ("Explicit full-month weekday UTC and ampersand-colon release note is supported",()=>{var h=Page(["<b>Tradable &amp; Marketable After:</b> Monday, November 9, 2026 (3:00:00) UTC"]).Holds.Single().Value;Check(h.TradeAfter==new DateTimeOffset(2026,11,9,3,0,0,TimeSpan.Zero)&&h.MarketAfter==h.TradeAfter&&!h.TimeUnverified);}),
            ("Duration seven is not a per-item remaining seven days",()=>{var h=Page([]).Holds.Single().Value;Check(h.TradeAfter is null&&h.MarketAfter is null&&h.Describe(false,false,DateTimeOffset.UtcNow).Contains("남은 시간으로 추정하지 않음"));}),
            ("Yearless or timezone-less owner text remains unverified without breaking inventory",()=>{foreach(var text in new[]{"Tradable/Marketable After 9 Nov @ 3:00am","Tradable After Nov 9, 2026 (3:00:00)","Tradable After invalid"}){var p=Page([text]);var h=p.Holds.Single().Value;Check(p.Assets.Length==1&&h.TimeUnverified&&h.TradeAfter is null&&h.MarketAfter is null);}}),
            ("Conflicting release notes do not pick an arbitrary date",()=>{var h=Page(["Tradable After Nov 9, 2026 (3:00:00) GMT","Tradable After Nov 10, 2026 (3:00:00) GMT"]).Holds.Single().Value;Check(h.TradeAfter is null&&h.TimeUnverified);}),
            ("Asset-specific release note takes precedence over class description",()=>{var h=Page(["Tradable After Nov 9, 2026 (3:00:00) GMT"],own:["Marketable After Nov 10, 2026 (3:00:00) GMT"]).Holds.Single().Value;Check(h.TradeAfter is null&&h.MarketAfter is not null);}),
            ("Elapsed countdown cannot unlock a server-nonmarketable item",()=>{var p=Page(["Tradable/Marketable After Nov 1, 2020 (3:00:00) GMT"]);var row=new SteamAssetRow(p.Assets.Single(),1,()=>{}){HoldInfo=p.Holds.Single().Value};Check(!row.CanSelect&&row.HoldText.Contains("재조회 필요"));}),
            ("Tradable false does not prohibit a server-marketable item",()=>{var p=Page([],marketable:true);var row=new SteamAssetRow(p.Assets.Single(),1,()=>{}){HoldInfo=p.Holds.Single().Value};Check(row.CanSelect&&!row.Asset.Tradable&&row.Asset.Marketable);}),
            ("Long optional owner note and invalid duration do not crash inventory",()=>{var p=Page([new string('x',5000)],days:-1);Check(p.Assets.Length==1&&p.Holds.Single().Value.TimeUnverified);})
        };
        int pass=0,fail=0;foreach(var(name,test)in tests)try{test();pass++;Console.WriteLine("PASS "+name);}catch(Exception e){fail++;Console.WriteLine("FAIL "+name+": "+e);}await Task.CompletedTask;return(pass,fail);
    }
}
