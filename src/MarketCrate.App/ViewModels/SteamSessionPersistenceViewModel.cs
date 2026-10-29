using MarketCrate.Core;
using System.IO;
using MarketCrate.Infrastructure.Steam;

namespace MarketCrate.App.ViewModels;

public sealed partial class SteamWorkspaceViewModel
{
    private SteamAuthStore? authStore;
    private bool rememberLogin;
    private int restoreAttempted;
    private string savedLoginMessage="저장된 로그인 없음 · 로그인 유지는 선택 사항입니다.";
    private DateTimeOffset? lastAuthentication;
    private static bool CredentialStorageError(Exception error)=>error is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException or System.Security.SecurityException or ArgumentException or InvalidOperationException or System.Text.Json.JsonException or KeyNotFoundException;
    public bool RememberLogin
    {
        get=>rememberLogin;
        set
        {
            if(Busy||!Set(ref rememberLogin,value))return;
            if(!value)DeleteRememberedLogin();
            else SavedLoginMessage="다음 QR 인증 성공 시 이 Windows 사용자만 복호화할 수 있도록 저장합니다. 실거래 활성화는 별도입니다.";
        }
    }
    public bool CanChangeRememberLogin=>!Busy&&authStore is not null;
    public string SavedLoginMessage{get=>savedLoginMessage;private set=>Set(ref savedLoginMessage,value);}
    public string LastAuthenticationText=>lastAuthentication is {} at?$"마지막 정상 인증 확인: {at.ToLocalTime():yyyy-MM-dd HH:mm:ss zzz}":"이 실행의 정상 인증 확인 기록 없음";
    public Command DeleteSavedLogin{get;private set;}=null!;
    private void InitializePersistence(string? directory)
    {
        if(directory is not null&&OperatingSystem.IsWindows())
        {
            try
            {
                authStore=new(directory);var state=authStore.State;rememberLogin=state==StoredLoginState.Saved;
                SavedLoginMessage=state switch{StoredLoginState.Saved=>"암호화된 로그인 존재 · 시작 시 한 번만 복원합니다.",StoredLoginState.Paused=>"이전 복원/저장이 중단됨 · 자동 재시도하지 않습니다. 저장 정보를 삭제하고 QR 재인증하세요.",StoredLoginState.Damaged=>"저장 로그인 손상/소유자 확인 실패 · 자동 복원하지 않습니다. 직접 삭제 후 재인증하세요.",StoredLoginState.Unavailable=>"저장 로그인 확인 불가 · 권한/손상 상태를 확인하고 직접 삭제 또는 재인증하세요.",_=>savedLoginMessage};
            }
            catch(Exception error)when(CredentialStorageError(error)){authStore=null;rememberLogin=false;SavedLoginMessage="암호화 저장소 사용 불가 · 로그인 유지 OFF · QR 연결만 사용할 수 있습니다.";CredentialFailure(error);}
        }
        DeleteSavedLogin=new(DeleteRememberedLogin,()=>!Busy&&authStore is not null);
    }
    private void DeleteRememberedLogin()
    {
        if(!OperatingSystem.IsWindows()||authStore is null)return;
        workspace?.Authentication.ForgetRememberedToken();
        try{authStore.Delete();rememberLogin=false;Raise(nameof(RememberLogin));SavedLoginMessage="이 PC의 저장 로그인 삭제 완료 · 서버 토큰 철회는 수행하지 않았습니다. 현재 메모리 연결은 별도로 로그아웃할 수 있습니다.";DiagnosticHub.Emit(DiagnosticEvent.SessionCredential,DiagnosticArea.Authentication,reason:"CredentialDeleted");}
        catch(Exception error)when(CredentialStorageError(error))
        {rememberLogin=false;Raise(nameof(RememberLogin));SavedLoginMessage="저장 로그인 삭제 실패 · 삭제 완료로 간주하지 않습니다. 앱 종료 후 파일 권한을 확인하세요.";CredentialFailure(error,"CredentialDeleteFailed");}
    }
    private void CredentialFailure(Exception error,string reason="CredentialUnavailable")=>DiagnosticHub.Emit(DiagnosticEvent.SessionCredential,DiagnosticArea.Authentication,DiagnosticOutcome.Blocked,LogLevel.Error,DiagnosticError.Storage,requirement:"A06",reason:reason,exception:error);
    private void SaveLoginIfRequested(SteamSavedLogin? restored=null)
    {
        lastAuthentication=DateTimeOffset.UtcNow;Raise(nameof(LastAuthenticationText));
        if(!RememberLogin||authStore is null||!OperatingSystem.IsWindows())return;
        try{workspace!.Authentication.SaveRememberedLogin(authStore,restored);SavedLoginMessage="DPAPI CurrentUser 암호화 저장 완료 · 다음 실행에서 제한된 단일 복원 · 실거래는 기본 OFF";DiagnosticHub.Emit(DiagnosticEvent.SessionCredential,DiagnosticArea.Authentication,reason:"CredentialSaved");}
        catch(Exception error)when(CredentialStorageError(error))
        {rememberLogin=false;Raise(nameof(RememberLogin));workspace?.Authentication.ForgetRememberedToken();PauseSavedLogin();SavedLoginMessage="로그인 유지 OFF · 암호화 저장/토큰 계약 확인 실패. 기존 파일이 남았다면 직접 삭제하세요. 현재 연결은 유지합니다.";CredentialFailure(error,"CredentialSaveFailed");}
    }
    private void PauseSavedLogin()
    {
        if(!OperatingSystem.IsWindows()||authStore is null)return;
        try{if(authStore.State is not StoredLoginState.Missing)authStore.Pause();SavedLoginMessage="재인증 필요 · 자동 복원을 중단했습니다. 직접 QR 로그인하거나 저장 정보를 삭제하세요.";}
        catch(Exception error)when(CredentialStorageError(error)){CredentialFailure(error);}
    }
    public async Task RestoreRememberedAsync()
    {
        if(Interlocked.Exchange(ref restoreAttempted,1)!=0||!RememberLogin||authStore is null||!OperatingSystem.IsWindows())return;
        await Run(async token=>
        {
            if(!OperatingSystem.IsWindows())return;
            SteamSavedLogin? credential;
            try{DiagnosticHub.Emit(DiagnosticEvent.SessionCredential,DiagnosticArea.Authentication,DiagnosticOutcome.Started,requirement:"A06",reason:"CredentialLoad");credential=authStore.TakeForRestore();}
            catch(Exception error)when(CredentialStorageError(error))
            {
                PauseSavedLogin();LoginState=error is SteamException{Code:SteamFailure.Expired}?SteamLoginState.Expired:SteamLoginState.ContractFailure;
                SavedLoginMessage="저장 자격 증명 확인 실패 · 자동 재시도하지 않습니다. 저장 정보를 삭제한 뒤 QR로 재인증하세요.";
                Status="저장 로그인 파일/토큰 확인 단계에서 중단 · 인증 요청은 전송하지 않았습니다.";
                CredentialFailure(error,"CredentialLoadFailed");return;
            }
            using var saved=credential;if(saved is null)return;
            LoginState=SteamLoginState.Restoring;Status="저장된 로그인 복원 중 · 보관함/판매목록/거래는 자동 실행하지 않습니다.";
            riskAccepted=credentialsAccepted=true;Raise(nameof(RiskAccepted));Raise(nameof(CredentialsAccepted));
            workspace?.Dispose();workspace=factory(Game);workspace.Access.Acknowledge(true,true,true,false);workspace.Authentication.RememberRequested=true;
            try
            {
                await workspace.Authentication.RestoreAsync(saved,token);token.ThrowIfCancellationRequested();
                LoginState=SteamLoginState.Connected;RestoreCatalog();RefreshRequests();SaveLoginIfRequested(saved);
                Status="저장 로그인으로 새 Community 세션 확인 완료 · 읽기는 직접 갱신 · 실제 거래 OFF";
            }
            catch{PauseSavedLogin();throw;}
        });
    }
}
