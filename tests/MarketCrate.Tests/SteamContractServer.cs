using System.Net;
using System.Net.Http;
using System.Text.Json;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;

// No listener/socket: HttpMessageHandler simulates a server in this test process.
public sealed class SteamContractServer : HttpMessageHandler
{
    public sealed class TestClock : TimeProvider {private DateTimeOffset now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>now;public void Advance(int seconds=2)=>now=now.AddSeconds(seconds);}
    public TestClock Clock {get;}=new();
    public string Currency="KRW";public int Free=10;public bool MissingId,TimeoutSell,TimeoutCancel,HideReturnedAsset,FeeConflict,Denied,HoldLogin,RequireCaptcha,AuthNetworkFailure;
    public int Sends,Cancels,Calls;public bool Publish;public List<(string Id,int Quantity,long Seller)> Rows {get;}=[];
    private readonly Dictionary<string,long> created=[];
    public string? IconToken;public bool DynamicInventory,UnsupportedInventory,PrivateInventory;public string? HistoryOverride;public bool HidePublisherFee;public int InventoryReads;public TaskCompletionSource? HistoryStarted,HistoryRelease;
    public int ListingAppId=730;public string ListingContext="2";
    public int ManyAssets;
    public TaskCompletionSource? InventoryStarted,InventoryRelease;
    public SteamFeeSchedule Schedule=>new(Currency,Currency=="KRW"?100:1,Currency=="KRW"?100:1,new(50000),new(100000));
    // A fresh handler forwards to the same synthetic state for reconnect tests without reusing disposed HttpClient handlers.
    public HttpMessageHandler Connection()=>new Forward(this);
    private sealed class Forward(SteamContractServer server):HttpMessageHandler{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken t)=>server.Respond(r,t);}
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>Respond(request,token);
    private async Task<HttpResponseMessage> Respond(HttpRequestMessage request,CancellationToken token)
    {
        Calls++;var path=request.RequestUri!.AbsolutePath;string json;
        if(path.Contains("BeginAuthSession")){if(AuthNetworkFailure)throw new HttpRequestException("synthetic auth network failure");json="{\"response\":{\"client_id\":\"123\",\"request_id\":\"AQID\",\"challenge_url\":\"https://s.team/q/1/synthetic-only\",\"interval\":1}}";}
        else if(path.Contains("PollAuthSession"))
        {
            if(Denied){var denied=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{}")};denied.Headers.Add("x-eresult","15");return denied;}
            json=RequireCaptcha?"{\"response\":{\"captcha_needed\":true}}":HoldLogin?"{\"response\":{}}":"{\"response\":{\"refresh_token\":\"synthetic-refresh-not-a-token\"}}";
        }
        else if(path=="/jwt/finalizelogin")json="{\"steamID\":\"76561198000000001\",\"transfer_info\":[{\"url\":\"https://steamcommunity.com/login/settoken\",\"params\":{\"nonce\":\"synthetic\",\"auth\":\"synthetic\"}}]}";
        else if(path=="/login/settoken")
        {var response=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"result\":1}")};response.Headers.Add("Set-Cookie","steamLoginSecure=synthetic-cookie; Path=/; Secure; HttpOnly");return response;}
        else if(path.StartsWith("/inventory/"))
        {
            InventoryReads++;InventoryStarted?.TrySetResult();if(InventoryRelease is not null)await InventoryRelease.Task;token.ThrowIfCancellationRequested();
            if(UnsupportedInventory)return new(HttpStatusCode.NotFound){Content=new StringContent("{}")};
            if(PrivateInventory)return new(HttpStatusCode.Forbidden){Content=new StringContent("{}")};
            json=InventoryJson(HideReturnedAsset?0:Free);if(HidePublisherFee){var node=System.Text.Json.Nodes.JsonNode.Parse(json)!;foreach(var d in node["descriptions"]!.AsArray())d!.AsObject().Remove("market_fee");json=node.ToJsonString();}
            if(DynamicInventory){var parts=path.Split('/');json=json.Replace("\"appid\":730","\"appid\":"+parts[3],StringComparison.Ordinal).Replace("\"contextid\":\"2\"","\"contextid\":\""+parts[4]+"\"",StringComparison.Ordinal);}
        }
        else if(path=="/market/")json="<script>var g_rgWalletInfo = "+JsonSerializer.Serialize(new{wallet_currency=Currency=="KRW"?16:1,wallet_market_minimum=Schedule.MinimumWire,wallet_currency_increment=Schedule.IncrementWire})+";</script>";
        else if(path=="/market/ajaxfeestructure/")json="{\"steam_fee\":{\"fee_rate\":\"0.05\",\"fee_cap\":\"0\"},\"publisher_fee\":{\"fee_rate\":\"0.1\",\"fee_cap\":\"0\"}}";
        else if(path=="/market/orderbook")json=JsonSerializer.Serialize(new{data=new{success=true,data=new{eCurrency=Currency=="KRW"?16:1,rgCompactBuyOrders=new long[]{new Money(115,Currency).Wire,3,new Money(Currency=="KRW"?113:112,Currency).Wire,4,new Money(110,Currency).Wire,3}}}});
        else if(path=="/market/pricehistory/"){HistoryStarted?.TrySetResult();if(HistoryRelease is not null)await HistoryRelease.Task.WaitAsync(token);json=HistoryOverride??"{\"success\":true,\"prices\":[[\"Oct 28 2026 01: +0\",115,\"3\"]]}";}
        else if(path=="/market/mylistings/render/")json=ListingsJson();
        else if(path=="/market/sellitem/")
        {
            var form=await Fields(request);var quantity=int.Parse(form["amount"]);var seller=Money.FromWire(long.Parse(form["price"]),Currency).Minor;
            Sends++;if(quantity>Free)return Json("{\"success\":false}");Free-=quantity;var id=(500+Sends).ToString();Rows.Add((id,quantity,seller));created[id]=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if(TimeoutSell)throw new HttpRequestException("synthetic response loss");
            var body=new Dictionary<string,object>{["success"]=true,["needs_mobile_confirmation"]=true};if(!MissingId)body["listingid"]=id;if(FeeConflict)body["fee"]=999999;json=JsonSerializer.Serialize(body);
        }
        else if(path.StartsWith("/market/removelisting/"))
        {
            Cancels++;var id=path.Split('/').Last();var row=Rows.SingleOrDefault(x=>x.Id==id);if(row!=default){Rows.Remove(row);Free+=row.Quantity;}if(TimeoutCancel)throw new HttpRequestException("synthetic cancel response loss");json="[]";
        }
        else throw new InvalidOperationException("Unexpected synthetic route: "+path);
        return Json(json);
    }
    private static HttpResponseMessage Json(string json)=>new(HttpStatusCode.OK){Content=new StringContent(json)};
    private static async Task<Dictionary<string,string>> Fields(HttpRequestMessage request)=>(await request.Content!.ReadAsStringAsync()).Split('&').Select(x=>x.Split('=',2)).ToDictionary(x=>Uri.UnescapeDataString(x[0]),x=>Uri.UnescapeDataString(x[1]));
    public string InventoryJson(int quantity)=>quantity==0?"{\"success\":1,\"total_inventory_count\":0}":JsonSerializer.Serialize(new{success=1,total_inventory_count=ManyAssets>0?ManyAssets:1,assets=Enumerable.Range(0,ManyAssets>0?ManyAssets:1).Select(i=>new{appid=730,contextid="2",assetid=(101+i).ToString(),classid="1",instanceid="0",amount=(ManyAssets>0?1:quantity).ToString()}),descriptions=new[]{new{appid=730,classid="1",instanceid="0",name="Synthetic stack",icon_url=IconToken??"",icon_url_large=IconToken??"",market_hash_name="Synthetic stack",marketable=1,tradable=1,market_fee="0.1"}}});
    public string ListingsJson()
    {
        foreach(var row in Rows)created.TryAdd(row.Id,DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var rows=Rows.Select(x=>new{listingid=x.Id,price=new Money(x.Seller,Currency).Wire,fee=new Money(Schedule.FromSeller(x.Seller).Fees,Currency).Wire,currencyid=Currency=="KRW"?2016:2001,time_created=created[x.Id],asset=new{appid=ListingAppId,contextid=ListingContext,id="101",amount=x.Quantity.ToString()}}).ToArray();
        var assets=new Dictionary<string,object>{[ListingAppId.ToString()]=new Dictionary<string,object>{[ListingContext]=new Dictionary<string,object>{["101"]=new{market_hash_name="Synthetic stack",icon_url=IconToken??""}}}};
        return JsonSerializer.Serialize(new{success=true,total_count=Publish?rows.Length:0,listings=Publish?rows:[],listings_to_confirm=Publish?[]:rows,listings_on_hold=Array.Empty<object>(),assets});
    }
}
