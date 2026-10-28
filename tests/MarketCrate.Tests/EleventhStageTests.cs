using System.Net;
using System.Text;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;
using MarketCrate.Infrastructure.Steam;

namespace MarketCrate.Tests;

public static class EleventhStageTests
{
    public static async Task<(int Passed,int Failed)> RunAsync()
    {
        var tests=new (string,Func<Task>)[]{("QR begin independent protobuf/form contract",RequestContract), ("QR uint64 numeric JSON and fractional interval",NumericResponse),
            ("QR malformed/missing/changed response phases and secret exclusion",ResponseFailures),
            ("QR HTTP200 API denial differs from HTTP and JSON failure",ApiFailures),
            ("QR begin cancellation and explicit logout clear pending session",Cancellation)};
        var passed=0;var failed=0;foreach(var(name,test)in tests)try{await test();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine($"FAIL {name}: {error}");}return(passed,failed);
    }
    internal const string Valid="{\"response\":{\"client_id\":\"18446744073709551614\",\"request_id\":\"AQID\",\"challenge_url\":\"https://s.team/q/1/synthetic-private\",\"interval\":1.5}}";
    private static async Task RequestContract()
    {
        using var handler=new Handler();var access=new SteamAccess();access.Acknowledge(true,true,true,false);using var http=new SteamHttp(access,handler);using var auth=new SteamQrAuthentication(http);
        await auth.BeginAsync();var fields=handler.Form!;var bytes=Convert.FromBase64String(fields["input_protobuf_encoded"]);var root=Decode(bytes);
        Check(root.TryGetValue(1,out var name)&&Encoding.UTF8.GetString((byte[])name)=="MarketCrate experimental","missing top-level device_friendly_name field 1");
        Check(root.TryGetValue(2,out var platform)&&(ulong)platform==2,"missing WebBrowser platform_type field 2");
        var details=Decode((byte[])root[3]);Check(Encoding.UTF8.GetString((byte[])details[1])=="MarketCrate experimental"&&(ulong)details[2]==2,"device_details identity mismatch");
        Check(fields.GetValueOrDefault("format")=="json"&&handler.AcceptJson,"JSON response must be explicitly requested");
        Check(auth.Challenge is not null&&handler.Calls==1&&!auth.Connected,"QR start must not finalize login");
    }
    private static async Task NumericResponse()
    {
        using var handler=new Handler{Body=Valid.Replace("\"18446744073709551614\"","18446744073709551614",StringComparison.Ordinal).Replace("1.5","0.5",StringComparison.Ordinal)};
        var access=new SteamAccess();access.Acknowledge(true,true,true,false);using var http=new SteamHttp(access,handler);using var auth=new SteamQrAuthentication(http);await auth.BeginAsync();Check(auth.Challenge is not null,"exact uint64 number rejected");
    }
    private static async Task ResponseFailures()
    {
        var cases=new (string,SteamAuthStage,DiagnosticError)[]{
            ("{secret-never-log",SteamAuthStage.ResponseParse,DiagnosticError.Parsing),
            ("{\"response\":{}}",SteamAuthStage.ResponseFields,DiagnosticError.Schema),
            (Valid.Replace("AQID","not-base64!",StringComparison.Ordinal),SteamAuthStage.ResponseFields,DiagnosticError.Schema),
            (Valid.Replace("\"18446744073709551614\"","18446744073709551616",StringComparison.Ordinal),SteamAuthStage.ResponseFields,DiagnosticError.Schema),
            (Valid.Replace("https://s.team/q/1/synthetic-private","https://evil.example/q/secret-never-log",StringComparison.Ordinal),SteamAuthStage.ChallengeValidation,DiagnosticError.Schema),
            (Valid.Replace("1.5","0",StringComparison.Ordinal),SteamAuthStage.ResponseFields,DiagnosticError.Schema)
        };
        foreach(var(body,stage,error)in cases)await Failure(body,200,null,stage,error);
    }
    private static async Task ApiFailures()
    {
        await Failure("{}",200,"8",SteamAuthStage.ApiResult,DiagnosticError.Authentication);
        await Failure("{}",401,null,SteamAuthStage.HTTP,DiagnosticError.Authentication);
        await Failure("{}",403,null,SteamAuthStage.HTTP,DiagnosticError.Forbidden);
        await Failure("{}",429,null,SteamAuthStage.HTTP,DiagnosticError.RateLimited);
    }
    private static async Task Failure(string body,int status,string? apiResult,SteamAuthStage stage,DiagnosticError error)
    {
        var folder=Path.Combine(Directory.GetCurrentDirectory(),"runtime","stage11-auth-"+Guid.NewGuid().ToString("N"));
        try
        {
            using var log=new JsonDiagnosticLog(folder);using var scope=DiagnosticHub.Use(log);using var operation=DiagnosticHub.Scope();
            using var handler=new Handler{Body=body,Status=status,ApiResult=apiResult};var access=new SteamAccess();access.Acknowledge(true,true,true,false);using var http=new SteamHttp(access,handler);using var auth=new SteamQrAuthentication(http);
            try{await auth.BeginAsync();throw new Exception("bad auth accepted");}catch(SteamException){}
            Check(auth.LastFailureStage==stage&&auth.Challenge is null&&!auth.Connected,"wrong failure stage/session retained");
            var failure=log.Recent.Single(x=>x.EventId==(int)DiagnosticEvent.AuthContract);
            Check(failure.Reason==stage.ToString()&&failure.Error==error&&failure.Endpoint=="AuthBegin"&&failure.Context.OperationId==DiagnosticHub.Context!.OperationId,"missing causal diagnostic classification");
            if(apiResult is not null)Check(failure.ServerCode==8&&!log.Recent.Any(x=>x.EventId==(int)DiagnosticEvent.HttpResponse&&x.Outcome==DiagnosticOutcome.Succeeded),"API error mislabeled successful");
            await log.FlushAsync();var text=System.Text.Json.JsonSerializer.Serialize(log.Recent);foreach(var secret in new[]{"secret-never-log","s.team","synthetic-private","AQID"})Check(!text.Contains(secret),"diagnostic leaked auth payload");
            try{await auth.BeginAsync();}catch(SteamException){}Check(handler.Calls==1,"halted request retried");
        }finally{Directory.Delete(folder,true);}
    }
    private static async Task Cancellation()
    {
        using var handler=new Handler();var access=new SteamAccess();access.Acknowledge(true,true,true,false);using var http=new SteamHttp(access,handler);using var auth=new SteamQrAuthentication(http);
        using(var cancel=new CancellationTokenSource()){cancel.Cancel();try{await auth.BeginAsync(cancel.Token);}catch(OperationCanceledException){}Check(handler.Calls==0&&auth.Challenge is null,"cancel sent auth");}
        await auth.BeginAsync();auth.Dispose();Check(auth.Challenge is null&&!auth.Connected,"logout retained QR");
    }
    // Independent decoder: no production Proto helpers, protobuf package or live endpoint.
    private static Dictionary<int,object> Decode(byte[] data)
    {
        var result=new Dictionary<int,object>();var offset=0;
        ulong Varint(){ulong value=0;for(var shift=0;shift<64;shift+=7){Check(offset<data.Length,"truncated varint");var b=data[offset++];value|=(ulong)(b&127)<<shift;if((b&128)==0)return value;}throw new Exception("varint overflow");}
        while(offset<data.Length){var tag=Varint();var field=(int)(tag>>3);Check(field>0,"invalid field");object value;if((tag&7)==0)value=Varint();else{Check((tag&7)==2,"unsupported request wire type");var count=checked((int)Varint());Check(count<=data.Length-offset,"truncated field");value=data[offset..(offset+count)];offset+=count;}Check(result.TryAdd(field,value),"duplicate field");}return result;
    }
    internal sealed class Handler:HttpMessageHandler
    {
        public string Body=Valid;public int Status=200;public string? ApiResult=null;public int Calls;public bool AcceptJson;public Dictionary<string,string>? Form;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;AcceptJson=request.Headers.Accept.Any(x=>x.MediaType=="application/json");var text=await request.Content!.ReadAsStringAsync(token);
            Form=text.Split('&').Select(x=>x.Split('=',2)).ToDictionary(x=>Uri.UnescapeDataString(x[0].Replace('+',' ')),x=>Uri.UnescapeDataString(x[1].Replace('+',' ')));
            var response=new HttpResponseMessage((HttpStatusCode)Status){Content=new StringContent(Body)};if(ApiResult is not null)response.Headers.Add("x-eresult",ApiResult);return response;
        }
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
}
