using System.Reflection;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure.Diagnostics;

internal static class DiagnosticRecordSafety
{
    public static DiagnosticRecord? Clean(DiagnosticRecord r)
    {
        if(r.SchemaVersion!=1||!Enum.IsDefined((DiagnosticEvent)r.EventId)||!Enum.IsDefined(r.Level)||!Enum.IsDefined(r.Area)||!Enum.IsDefined(r.Outcome)||!Enum.IsDefined(r.Error)||r.Context is null||r.Context.OperationId==Guid.Empty)return null;
        return r with{EventName=((DiagnosticEvent)r.EventId).ToString(),RequirementId=Regex.IsMatch(r.RequirementId??"",@"^(?:[A-G][0-9]{2}|LOG-[0-9]{2}|SYS-[0-9]{2})$")?r.RequirementId!:"SYS-01",
            AppVersion=Version(r.AppVersion),BuildVersion=Version(r.BuildVersion),Commit=r.Commit?.Length==40&&r.Commit.All(Uri.IsHexDigit)?r.Commit:"unavailable",
            Endpoint=DiagnosticHub.AllowedToken(r.Endpoint),Method=r.Method is "GET" or "POST"?r.Method:null,HttpStatus=r.HttpStatus is >=100 and <=599?r.HttpStatus:null,
            PreviousState=DiagnosticHub.AllowedToken(r.PreviousState),NewState=DiagnosticHub.AllowedToken(r.NewState),Reason=DiagnosticHub.AllowedToken(r.Reason),JournalState=DiagnosticHub.AllowedToken(r.JournalState),Exception=Safe(r.Exception),Currency=r.Currency is "KRW" or "USD"?r.Currency:null,Amounts=r.Amounts is {} q&&q.BuyerPays>=0&&q.SellerReceives>=0&&q.SteamFee>=0&&q.GameFee>=0?q:null};
    }
    private static string Version(string? value)=>value is not null&&Regex.IsMatch(value,@"^\d+\.\d+\.\d+(?:\.\d+)?(?:\+[a-fA-F0-9]{40})?$")?value:"unavailable";
    private static SafeException? Safe(SafeException? value,int depth=0)
    {
        if(value is null||depth>3)return null;
        var frames=(value.Frames??[]).Where(ValidFrame).Take(8).ToArray();
        var type=value.Type is "System.Exception" or "System.IOException" or "System.IO.IOException" or "System.IO.InvalidDataException" or "System.UnauthorizedAccessException" or "System.InvalidOperationException" or "System.ArgumentException" or "System.ArgumentOutOfRangeException" or "System.FormatException" or "System.OverflowException" or "System.TimeoutException" or "System.OperationCanceledException" or "System.Threading.Tasks.TaskCanceledException" or "System.Net.Http.HttpRequestException" or "System.Text.Json.JsonException" or "Microsoft.Data.Sqlite.SqliteException" or "MarketCrate.Infrastructure.Steam.SteamException"?value.Type:"ExternalException";
        return new(type,frames.FirstOrDefault(),frames,(value.Inner??[]).Take(2).Select(x=>Safe(x,depth+1)).Where(x=>x is not null).Cast<SafeException>().ToArray());
    }
    private static readonly ConcurrentDictionary<Assembly,HashSet<string>> symbols=new();
    private static bool ValidFrame(string? frame)
    {
        if(string.IsNullOrEmpty(frame)||frame.Length>260||!Regex.IsMatch(frame,@"^MarketCrate\.[A-Za-z0-9_.+<>`]+@IL[0-9]+$"))return false;
        var name=frame[..frame.IndexOf("@IL",StringComparison.Ordinal)];
        // Compare with symbols in this process's already-loaded first-party assemblies; never load a path from a log.
        foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x=>x.GetName().Name?.StartsWith("MarketCrate.",StringComparison.Ordinal)==true))
        {
            var names=symbols.GetOrAdd(assembly, a=>
            {
                Type[] types;try{types=a.GetTypes();}catch(ReflectionTypeLoadException){return [];}
                return types.SelectMany(t=>t.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly).Cast<MethodBase>().Concat(t.GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance)).Select(m=>(t.FullName??"")+"."+m.Name)).ToHashSet(StringComparer.Ordinal);
            });
            if(names.Contains(name))return true;
        }
        return false;
    }
}
