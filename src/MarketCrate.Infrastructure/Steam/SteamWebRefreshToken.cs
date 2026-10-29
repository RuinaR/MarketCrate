// Adapted from steam-session LoginSession.refreshToken validation (MIT).
// Copyright (c) 2022 Alex Corn. See ThirdParty/steam-session/LICENSE.
// Upstream 3ac0f34fd964b3f886ba18ef4824ac43c942e030 src/LoginSession.ts.
// C# port adds bounded input, expiration and expected-owner checks. Claims are NOT signature verification.
using System.Text.Json;

namespace MarketCrate.Infrastructure.Steam;

internal static class SteamWebRefreshToken
{
    internal static (string Account,DateTimeOffset Expires) Inspect(string token,DateTimeOffset now,string? expected=null)
    {
        try
        {
            if(token.Length is <20 or >16384)throw new FormatException();
            var parts=token.Split('.');if(parts.Length!=3||parts.Any(p=>p.Length==0||p.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c is not('-' or '_'))))throw new FormatException();
            var text=parts[1].Replace('-','+').Replace('_','/');var bytes=Convert.FromBase64String(text.PadRight((text.Length+3)/4*4,'='));
            try
            {
                using var document=JsonDocument.Parse(bytes,new JsonDocumentOptions{MaxDepth=12});var root=document.RootElement;
                var account=SteamContract.Id(root.GetProperty("sub"));if(account.Length!=17||expected is not null&&account!=expected)throw new SteamException(SteamFailure.Authentication);
                var audiences=root.GetProperty("aud");if(audiences.ValueKind!=JsonValueKind.Array)throw new FormatException();
                var audience=audiences.EnumerateArray().Select(x=>x.GetString()).ToArray();
                if(!audience.Contains("derive")||!audience.Contains("web")||audience.Contains("client")||audience.Contains("mobile"))throw new SteamException(SteamFailure.Unsupported);
                var expiry=DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64());if(expiry<=now)throw new SteamException(SteamFailure.Expired);
                if(root.TryGetProperty("nbf",out var nbf)&&DateTimeOffset.FromUnixTimeSeconds(nbf.GetInt64())>now.AddSeconds(30))throw new SteamException(SteamFailure.Authentication);
                return(account,expiry);
            }
            finally{System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);}
        }
        catch(SteamException){throw;}
        catch(Exception error)when(error is JsonException or FormatException or InvalidOperationException or KeyNotFoundException or ArgumentException or OverflowException)
        {throw new SteamException(SteamFailure.ContractChanged);}
    }
}
