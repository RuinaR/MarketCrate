using System.IO;
using System.Net.Http;
using System.Net;

public sealed class ImageContractServer:HttpMessageHandler
{
    public byte[] Bytes=Pixel();
    public static byte[] Pixel()
    {
        using var output=new MemoryStream();output.Write(new byte[]{137,80,78,71,13,10,26,10});
        void Chunk(string name,byte[] data){var length=new byte[4];System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length,data.Length);output.Write(length);var tag=System.Text.Encoding.ASCII.GetBytes(name);output.Write(tag);output.Write(data);uint crc=0xffffffff;foreach(var value in tag.Concat(data)){crc^=value;for(var i=0;i<8;i++)crc=(crc&1)!=0?0xedb88320^(crc>>1):crc>>1;}System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(length,~crc);output.Write(length);}
        Chunk("IHDR",[0,0,0,1,0,0,0,1,8,6,0,0,0]);using var compressed=new MemoryStream();using(var zip=new System.IO.Compression.ZLibStream(compressed,System.IO.Compression.CompressionLevel.Fastest,true))zip.Write(new byte[]{0,73,227,191,255});Chunk("IDAT",compressed.ToArray());Chunk("IEND",[]);return output.ToArray();
    }
    public int Calls;public int Status=200;public string Mime="image/png";public bool CredentialLeak;
    public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource? Release;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
    {
        Interlocked.Increment(ref Calls);CredentialLeak|=request.Headers.Contains("Cookie")||request.Headers.Authorization is not null||request.Headers.Referrer is not null;
        if(request.RequestUri!.Host!="community.fastly.steamstatic.com"||request.RequestUri.Query.Length!=0||!request.RequestUri.AbsolutePath.EndsWith("/128fx128f"))throw new InvalidOperationException("UnpinnedImage");
        Started.TrySetResult();if(Release is not null)await Release.Task.WaitAsync(token);
        var response=new HttpResponseMessage((HttpStatusCode)Status){Content=new ByteArrayContent(Bytes)};response.Content.Headers.ContentType=new(Mime);response.Headers.Add("Set-Cookie","ignored-synthetic-cookie=SECRET; Secure");return response;
    }
}
