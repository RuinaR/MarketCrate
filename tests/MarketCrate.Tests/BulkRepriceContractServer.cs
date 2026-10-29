using System.Net;
using System.Net.Http;
using System.Text.Json;
using MarketCrate.Core;
// Synthetic in-process HTTP only. No listener/socket, Steam account, or credential source.
public sealed class BulkRepriceContractServer:IDisposable
{
    private sealed record Row(string Id,string Asset,long Seller,SaleState State);
    private readonly SteamContractServer common=new();private readonly List<Row> rows=[];
    public HashSet<string> Available{get;}=["101","102"];
    public int Sells,Cancels,TopQuantity=3;public bool CancelTimeout,ChangedReturn;
    public List<string> RequestPaths{get;}=[];
    public TaskCompletionSource? BookStarted,BookRelease;
    public TaskCompletionSource? SellStarted,SaleRelease;
    public int PauseSaleNumber=1;
    public SteamContractServer.TestClock Clock=>common.Clock;
    public SteamFeeSchedule Schedule=>common.Schedule;
    public void External(int count){for(int i=0;i<count;i++)rows.Add(new((8000+i).ToString(),(3000+i).ToString(),100,SaleState.Listed));}
    public void Publish(){for(var i=0;i<rows.Count;i++)rows[i]=rows[i]with{State=SaleState.Listed};}
    public HttpMessageHandler Connection()=>new Handler(this);
    private sealed class Handler(BulkRepriceContractServer owner):HttpMessageHandler
    {
        private readonly HttpMessageInvoker fallback=new(owner.common.Connection());
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var path=request.RequestUri!.AbsolutePath;owner.RequestPaths.Add(path);string body;
            if(path=="/market/mylistings/render/")body=owner.Listings();
            else if(path.StartsWith("/inventory/"))body=JsonSerializer.Serialize(new{success=1,total_inventory_count=owner.Available.Count,assets=owner.Available.Select(id=>new{appid=730,contextid="2",assetid=id,classid="1",instanceid="0",amount="1"}),descriptions=new[]{new{appid=730,classid="1",instanceid="0",name="Synthetic stack",market_hash_name="Synthetic stack",marketable=1,tradable=1,market_fee="0.1"}}});
            else if(path=="/market/orderbook")
            {owner.BookStarted?.TrySetResult();if(owner.BookRelease is not null)await owner.BookRelease.Task.WaitAsync(token);body=JsonSerializer.Serialize(new{data=new{success=true,data=new{eCurrency=16,rgCompactBuyOrders=new long[]{11500,owner.TopQuantity,11300,4,11000,3}}}});}
            else if(path=="/market/sellitem/")
            {
                var form=(await request.Content!.ReadAsStringAsync(token)).Split('&').Select(x=>x.Split('=',2)).ToDictionary(x=>x[0],x=>Uri.UnescapeDataString(x[1]));
                if(form["amount"]!="1"||!owner.Available.Remove(form["assetid"]))throw new InvalidOperationException("Synthetic duplicate/invalid sale");
                var id=(900+ ++owner.Sells).ToString();owner.rows.Add(new(id,form["assetid"],Money.FromWire(long.Parse(form["price"]),"KRW").Minor,SaleState.AwaitingSteamGuard));
                if(owner.Sells==owner.PauseSaleNumber){owner.SellStarted?.TrySetResult();if(owner.SaleRelease is not null)await owner.SaleRelease.Task.WaitAsync(token);}
                body=JsonSerializer.Serialize(new{success=true,needs_mobile_confirmation=true,listingid=id});
            }
            else if(path.StartsWith("/market/removelisting/"))
            {
                owner.Cancels++;var id=path.Split('/')[3];var row=owner.rows.Single(x=>x.Id==id);owner.rows.Remove(row);
                owner.Available.Add(owner.ChangedReturn?(long.Parse(row.Asset)+100).ToString():row.Asset);
                if(owner.CancelTimeout)throw new HttpRequestException("synthetic lost cancellation response");body="[]";
            }
            else return await fallback.SendAsync(request,token);
            return new(HttpStatusCode.OK){Content=new StringContent(body)};
        }
        protected override void Dispose(bool disposing){if(disposing)fallback.Dispose();base.Dispose(disposing);}
    }
    private string Listings()
    {
        object Convert(Row x)=>new{listingid=x.Id,price=new Money(x.Seller,"KRW").Wire,fee=new Money(Schedule.FromSeller(x.Seller).Fees,"KRW").Wire,currencyid=2016,time_created=1700000000,asset=new{appid=730,contextid="2",id=x.Asset,amount="1"}};
        var assets=rows.Select(x=>x.Asset).Distinct().ToDictionary(x=>x,_=>(object)new{appid=730,market_hash_name="Synthetic stack"});
        return JsonSerializer.Serialize(new{success=true,total_count=rows.Count(x=>x.State==SaleState.Listed),listings=rows.Where(x=>x.State==SaleState.Listed).Select(Convert),listings_to_confirm=rows.Where(x=>x.State!=SaleState.Listed).Select(Convert),listings_on_hold=Array.Empty<object>(),assets=new Dictionary<string,object>{["730"]=new Dictionary<string,object>{["2"]=assets}}});
    }
    public void Dispose()=>common.Dispose();
}
