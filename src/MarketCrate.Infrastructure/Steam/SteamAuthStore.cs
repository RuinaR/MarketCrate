using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace MarketCrate.Infrastructure.Steam;

public enum StoredLoginState { Missing, Saved, Paused, Damaged, Unavailable }
// No generated ToString or public token property. Never pass this to a logger/serializer.
public sealed class SteamSavedLogin : IDisposable
{
    internal byte[] Token{get;}
    internal string Account{get;}
    internal byte[] Fingerprint{get;}
    public DateTimeOffset VerifiedAt{get;}
    internal SteamSavedLogin(byte[] token,string account,DateTimeOffset verified,byte[] fingerprint){Token=token;Account=account;VerifiedAt=verified;Fingerprint=fingerprint;}
    public void Dispose(){CryptographicOperations.ZeroMemory(Token);CryptographicOperations.ZeroMemory(Fingerprint);}
    public override string ToString()=>"Saved Steam login (redacted)";
}

// Own MarketCrate files only. A failed restore is disarmed BEFORE any network request.
[SupportedOSPlatform("windows")]
public sealed class SteamAuthStore
{
    private readonly string directory,path,pending,paused,lease;
    private static readonly byte[] entropy=Encoding.UTF8.GetBytes("MarketCrate.WebRefresh.v1");
    private readonly SecurityIdentifier owner=WindowsIdentity.GetCurrent().User??throw new CryptographicException();
    public SteamAuthStore(string directory)
    {this.directory=Path.GetFullPath(directory);path=Path.Combine(this.directory,"refresh.authbin");pending=Path.Combine(this.directory,"refresh.pending");paused=Path.Combine(this.directory,"restore.blocked");lease=Path.Combine(this.directory,"store.authlock");}
    public StoredLoginState State
    {
        get{try{CheckPaths();if(File.Exists(paused)||File.Exists(pending))return StoredLoginState.Paused;if(!File.Exists(path))return StoredLoginState.Missing;CheckFile(path);return StoredLoginState.Saved;}catch(CryptographicException){return StoredLoginState.Damaged;}catch(Exception e)when(IsStorage(e)){return StoredLoginState.Unavailable;}}
    }
    private static bool IsStorage(Exception e)=>e is IOException or UnauthorizedAccessException or CryptographicException or System.Security.SecurityException or JsonException or ArgumentException or InvalidOperationException;
    private void CheckPaths()
    {
        for(var candidate=new DirectoryInfo(directory);candidate is not null;candidate=candidate.Parent)
            if(candidate.Exists&&(candidate.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("AuthPathBoundary");
        foreach(var file in new[]{path,pending,paused,lease})if(File.Exists(file)&&(File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new IOException("AuthFileBoundary");
    }
    private void Prepare()
    {
        CheckPaths();Directory.CreateDirectory(directory);
        var security=new DirectorySecurity();security.SetOwner(owner);security.SetAccessRuleProtection(true,false);
        security.AddAccessRule(new FileSystemAccessRule(owner,FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
        new DirectoryInfo(directory).SetAccessControl(security);CheckPaths();
    }
    private FileStream Acquire(){Prepare();return new(lease,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
    private void CheckFile(string file)
    {
        var info=new FileInfo(file);if(info.Length is <8 or >65536)throw new CryptographicException();
        var acl=info.GetAccessControl();if(!owner.Equals(acl.GetOwner(typeof(SecurityIdentifier))))throw new CryptographicException();
        foreach(FileSystemAccessRule rule in acl.GetAccessRules(true,true,typeof(SecurityIdentifier)))
            if(rule.AccessControlType==AccessControlType.Allow&&!owner.Equals(rule.IdentityReference))throw new CryptographicException();
    }
    private void PauseCore(){using var mark=new FileStream(paused,FileMode.OpenOrCreate,FileAccess.Write,FileShare.None,1,FileOptions.WriteThrough);mark.WriteByte(1);mark.Flush(true);}
    public void Pause(){using var locked=Acquire();PauseCore();}
    public void Save(string account,ReadOnlySpan<byte> refresh,DateTimeOffset verified,SteamSavedLogin? restored=null)
    {
        using var locked=Acquire();
        if(restored is not null&&(!File.Exists(path)||!File.Exists(paused)||!CryptographicOperations.FixedTimeEquals(SHA256.HashData(File.ReadAllBytes(path)),restored.Fingerprint)))throw new IOException("AuthCredentialChanged");
        PauseCore();if(File.Exists(pending))throw new IOException("AuthIncompleteWrite");
        // Token is never put into a plaintext file/SQLite/environment or an exception message.
        var token=Encoding.UTF8.GetString(refresh);var claims=SteamWebRefreshToken.Inspect(token,DateTimeOffset.UtcNow,account);
        var plain=JsonSerializer.SerializeToUtf8Bytes(new{Version=1,Owner=owner.Value,Account=account,Token=token,Verified=verified,Expires=claims.Expires,Consent=true});
        byte[] encrypted=[];
        try
        {
            encrypted=ProtectedData.Protect(plain,entropy,DataProtectionScope.CurrentUser);
            using(var stream=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){stream.Write("MCAUTH01"u8);stream.Write(encrypted);stream.Flush(true);}
            var fileSecurity=new FileSecurity();fileSecurity.SetOwner(owner);fileSecurity.SetAccessRuleProtection(true,false);fileSecurity.AddAccessRule(new FileSystemAccessRule(owner,FileSystemRights.FullControl,AccessControlType.Allow));new FileInfo(pending).SetAccessControl(fileSecurity);
            CheckFile(pending);File.Move(pending,path,true);CheckFile(path);File.Delete(paused);
        }
        finally{CryptographicOperations.ZeroMemory(plain);CryptographicOperations.ZeroMemory(encrypted);}
    }
    // Paused marker remains on any failure, so restart cannot repeat a rejected/unknown request.
    public SteamSavedLogin? TakeForRestore()
    {
        if(State==StoredLoginState.Missing)return null;
        if(State!=StoredLoginState.Saved)throw new IOException("AuthRestorePaused");
        using var locked=Acquire();if(File.Exists(paused)||File.Exists(pending))throw new IOException("AuthRestorePaused");PauseCore();CheckFile(path);
        var data=File.ReadAllBytes(path);byte[] plain=[];
        try
        {
            if(!data.AsSpan(0,8).SequenceEqual("MCAUTH01"u8))throw new CryptographicException();
            plain=ProtectedData.Unprotect(data.AsSpan(8).ToArray(),entropy,DataProtectionScope.CurrentUser);
            using var json=JsonDocument.Parse(plain,new JsonDocumentOptions{MaxDepth=8});var root=json.RootElement;
            if(root.GetProperty("Version").GetInt32()!=1||root.GetProperty("Owner").GetString()!=owner.Value||!root.GetProperty("Consent").GetBoolean())throw new CryptographicException();
            var account=root.GetProperty("Account").GetString()!;var token=root.GetProperty("Token").GetString()!;
            var claims=SteamWebRefreshToken.Inspect(token,DateTimeOffset.UtcNow,account);
            if(root.GetProperty("Expires").GetDateTimeOffset()!=claims.Expires)throw new CryptographicException();
            return new(Encoding.UTF8.GetBytes(token),account,root.GetProperty("Verified").GetDateTimeOffset(),SHA256.HashData(data));
        }
        finally{CryptographicOperations.ZeroMemory(data);CryptographicOperations.ZeroMemory(plain);}
    }
    public void Delete()
    {
        if(!Directory.Exists(directory))return;
        using var locked=Acquire();PauseCore();
        foreach(var file in new[]{path,pending})File.Delete(file);
        File.Delete(paused);
    }
    public override string ToString()=>"Windows CurrentUser Steam credential store (redacted)";
}
