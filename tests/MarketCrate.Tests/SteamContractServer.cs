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
    public List<Dictionary<string,string>> SellFields{get;}=[];
    public int Sends,Cancels,Calls;public bool Publish;public List<(string Id,int Quantity,long Seller)> Rows {get;}=[];
    private readonly Dictionary<string,long> created=[];
    public string? IconToken;public bool DynamicInventory,UnsupportedInventory,PrivateInventory;public string? HistoryOverride;public bool HidePublisherFee;public int InventoryReads;public TaskCompletionSource? HistoryStarted,HistoryRelease;
    public int ListingAppId=730;public string ListingContext="2";
    public int ManyAssets;
    public int SellStatus=200;public string? SellBody;public Uri? SellReferrer;public bool SellAjax;
    public bool RotateSessionOnListings;private string? requiredSession;
    public bool EmailConfirmation,UnspecifiedConfirmation;
    public bool PendingAlias,PendingMetadataMissing;public int ListingReads;
    public bool EmptyCancelBody;public int CancelStatus=200;
    public string? MarketHistoryOverride;public int MarketHistoryReads;public int MarketHistoryStatus=200;
    public TaskCompletionSource? InventoryStarted,InventoryRelease;
    public TaskCompletionSource? ListingsStarted,ListingsRelease;
    public SteamFeeSchedule Schedule=>new(Currency,Currency=="KRW"?100:1,Currency=="KRW"?100:1,new(50000),new(100000));
    public static string HistoryJson(int gap=1)=>JsonSerializer.Serialize(new{success=true,prices=new object[][]{[DateTimeOffset.UtcNow.AddHours(-3).ToString("MMM dd yyyy HH: '+0'",System.Globalization.CultureInfo.InvariantCulture),115,"2"],[DateTimeOffset.UtcNow.AddHours(-3+gap).ToString("MMM dd yyyy HH: '+0'",System.Globalization.CultureInfo.InvariantCulture),115,"1"]}});
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
        else if(path=="/profiles/76561198000000001/inventory/")json="<script>g_bLoggedIn=true;g_steamID='76561198000000001';</script><div data-featuretarget='marketselldialog' data-props='"+System.Net.WebUtility.HtmlEncode(JsonSerializer.Serialize(new{newFeeRates=true,walletInfo=new{wallet_currency=Currency=="KRW"?16:1,wallet_market_minimum=Schedule.MinimumWire,wallet_currency_increment=Schedule.IncrementWire}}))+"'></div>";
        else if(path=="/market/ajaxfeestructure/")json="{\"steam_fee\":{\"fee_rate\":\"0.05\",\"fee_cap\":\"0\"},\"publisher_fee\":{\"fee_rate\":\"0.1\",\"fee_cap\":\"0\"}}";
        else if(path=="/market/orderbook")json=JsonSerializer.Serialize(new{data=new{success=true,data=new{eCurrency=Currency=="KRW"?16:1,rgCompactBuyOrders=new long[]{new Money(115,Currency).Wire,3,new Money(Currency=="KRW"?113:112,Currency).Wire,4,new Money(110,Currency).Wire,3}}}});
        else if(path=="/market/pricehistory/"){HistoryStarted?.TrySetResult();if(HistoryRelease is not null)await HistoryRelease.Task.WaitAsync(token);json=HistoryOverride??HistoryJson();}
        else if(path=="/market/myhistory/render/"){MarketHistoryReads++;if(MarketHistoryStatus!=200)return new((HttpStatusCode)MarketHistoryStatus){Content=new StringContent("{}")};json=MarketHistoryOverride??"{\"success\":true,\"start\":0,\"pagesize\":100,\"total_count\":0,\"events\":[],\"listings\":{},\"purchases\":{}}";}
        else if(path=="/market/mylistings/render/")
        {
            ListingsStarted?.TrySetResult();if(ListingsRelease is {} release){ListingsRelease=null;await release.Task.WaitAsync(token);}
            ListingReads++;json=ListingsJson();
            if(PendingAlias||PendingMetadataMissing)
            {
                var node=System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
                if(PendingAlias){node["pending_listings"]=node["listings_to_confirm"]!.DeepClone();node.Remove("listings_to_confirm");}
                if(PendingMetadataMissing)
                {
                    foreach(var row in node[PendingAlias?"pending_listings":"listings_to_confirm"]!.AsArray()){row!.AsObject().Remove("listingid");row.AsObject().Remove("price");}
                    node["assets"]=new System.Text.Json.Nodes.JsonObject();
                }
                json=node.ToJsonString();
            }
            if(RotateSessionOnListings){requiredSession="0123456789abcdef01234567";var rotated=Json(json);rotated.Headers.Add("Set-Cookie","sessionid="+requiredSession+"; Path=/; Secure");return rotated;}
        }
        else if(path=="/market/sellitem/")
        {
            SellReferrer=request.Headers.Referrer;SellAjax=request.Headers.TryGetValues("X-Requested-With",out var values)&&values.Contains("XMLHttpRequest");
            var form=await Fields(request);SellFields.Add(form);var quantity=int.Parse(form["amount"]);var seller=Money.FromWire(long.Parse(form["price"]),Currency).Minor;
            if(requiredSession is {} csrf&&form["sessionid"]!=csrf){Sends++;return new(HttpStatusCode.BadRequest){Content=new StringContent("{\"success\":false,\"message\":\"invalid session\"}")};}
            if(SellStatus!=200){Sends++;return new((HttpStatusCode)SellStatus){Content=new StringContent(SellBody??"{}",System.Text.Encoding.UTF8,"application/json")};}
            Sends++;if(quantity>Free)return Json("{\"success\":false}");Free-=quantity;var id=(500+Sends).ToString();Rows.Add((id,quantity,seller));created[id]=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if(TimeoutSell)throw new HttpRequestException("synthetic response loss");
            var body=new Dictionary<string,object>{["success"]=true,[EmailConfirmation?"needs_email_confirmation":UnspecifiedConfirmation?"requires_confirmation":"needs_mobile_confirmation"]=true};if(!MissingId)body["listingid"]=id;if(FeeConflict)body["fee"]=999999;json=JsonSerializer.Serialize(body);
        }
        else if(path.StartsWith("/market/removelisting/"))
        {
            if(CancelStatus!=200){Cancels++;return new((HttpStatusCode)CancelStatus){Content=new StringContent("")};}
            Cancels++;var id=path.Split('/').Last();var row=Rows.SingleOrDefault(x=>x.Id==id);if(row!=default){Rows.Remove(row);Free+=row.Quantity;}if(TimeoutCancel)throw new HttpRequestException("synthetic cancel response loss");json="[]";
            if(EmptyCancelBody)json="";
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
        if(rows.Length==0)assets.Clear(); // Empty market must not invent a held inventory asset.
        return JsonSerializer.Serialize(new{success=true,total_count=Publish?rows.Length:0,listings=Publish?rows:[],listings_to_confirm=Publish?[]:rows,listings_on_hold=Array.Empty<object>(),assets});
    }
}
