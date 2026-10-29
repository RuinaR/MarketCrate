# Steam 로그인 유지 — 0.18.0 / 17차 (2026-10-29 KST)

## 17차 사용자 실환경 증거 보완

사용자가 **0.17.0에서 앱 종료 후 재실행 시 실제 Steam 세션 복원 성공**을 직접 보고했다. QR/Community·판매목록8건·KRW wallet 파싱도 관측했다. 따라서 아래 '실제 복원 미검증'은 당시 개발자 자동 검증의 한계이며, 이제 이 특정 재시작 경로는 사용자 실환경 성공 증거가 있다.

0.18은 저장 형식/DPAPI/ACL/토큰 계약/원자 교체·중단/삭제·일회 복원/실거래OFF를 그대로 유지한다. Auth/Preferences/SteamExperimental 초기화0. 새 버전의 실제 복원, 장기 만료·토큰 회전/철회·다른 Windows 사용자 및 모든 조회/수수료/판매 성공을 검증한 뜻은 아니다. 재검증은 기존 계정의 앱 일반 종료/재실행→연결 상태→사용자 직접 조회만으로 충분하며 토큰/Auth 폴더를 전달하지 않는다.


## 범위와 동의

'로그인 상태 유지 · 다음 실행 시 한 번 자동 복원'은 기본 OFF이다. 사용자가 위험 안내와 함께 QR 인증 **전에** 선택한 경우만 앱 자체 QR에서 받은 WebBrowser Refresh Token을 보존한다. 나중에 ON으로 바꾸면 다음 QR 인증부터 저장한다. 기존 Steam/브라우저 쿠키·파일·메모리·Windows 자격 증명 저장소는 읽지 않는다. 비밀번호·Guard 비밀·maFile 수집/자동 승인은 없다.

이 기능은 Valve의 비공식 자동화 허가나 영구적인 토큰 유효성을 뜻하지 않는다. 토큰이 만료/철회되면 QR이 다시 필요하다. 같은 Windows 사용자 권한의 악성코드, 사용자 메모리·디버거·관리자 접근까지 방어한다고 주장하지 않는다.

## 실제 채택한 오픈소스 계약

[steam-session LoginSession.ts](https://github.com/DoctorMcKay/node-steam-session/blob/3ac0f34fd964b3f886ba18ef4824ac43c942e030/src/LoginSession.ts)의 MIT refreshToken 검증과 WebBrowser getWebCookies 경로를 C#으로 제한 이식했다. Copyright(c)2022 Alex Corn/MIT 전문과 원본 커밋을 소스·배포에 보존한다. Node.js 런타임/NPM 모듈은 추가하지 않는다.

- WebBrowser platform2, refresh audience에 web/derive가 있어야 한다. client/mobile 토큰은 받아들이지 않는다. sub의 계정, exp, 선택적 nbf, 입력 크기·형식을 검증한다. **JWT payload 해석은 서명 검증이나 서버 인증 성공이 아니다.**
- 저장 토큰을 nonce로 login.steampowered.com/jwt/finalizelogin POST → 서버 SteamID가 저장 계정과 일치 → 정확히 허용된 steamcommunity.com/login/settoken POST → Secure steamLoginSecure의 계정 일치까지 확인한 뒤 연결 상태가 된다. Community만 사용하며 다른 transfer URL을 임의로 따르지 않는다.
- settoken은 upstream처럼 결과 JSON이 생략될 수 있지만, 이 경우에도 실제 Secure 쿠키와 계정 일치를 요구한다. 쿠키 없는200·엉뚱한 계정·HTML·서버 거절은 성공이 아니다. 기존 QR 요청 encoding/대기/회전도 재사용한다.
- WebBrowser에 GenerateAccessTokenForApp/renewRefreshToken을 강제로 적용하지 않는다. upstream이 해당 플랫폼의 AccessDenied 한계를 명시한다. 이 버전은 저장한 토큰으로 **웹 세션을 새로 발급**하며 서버의 자동 Refresh Token 회전 기능을 완성했다고 주장하지 않는다. 새 QR 토큰의 로컬 교체와 구 파일 무사용은 검증했지만, 구 토큰의 서버 무효화/철회는 수행하지 않는다.

## 사용자별 저장소

`%LOCALAPPDATA%/MarketCrate/Auth`만 사용하며 Preferences/SteamExperimental SQLite와 분리한다.

| 파일 | 역할 |
|---|---|
| refresh.authbin | MCAUTH01 형식 표식 + DPAPI CurrentUser 암호문. 버전1/Windows SID/Steam 계정/토큰/만료/확인시각/동의는 모두 암호문 안에 있음 |
| refresh.pending | 같은 디렉터리의 암호화 임시 저장. 남아 있으면 자동으로 복구/사용하지 않음 |
| restore.blocked | 복원/저장 전에 기록하는 비밀 없는 차단 표식. 실패/중단 후 다음 실행에서 자동 반복 방지 |
| store.authlock | 앱 소유 저장소 작업의 단일 writer 파일 잠금. 다른 프로세스 탐색은 하지 않음 |

Microsoft System.Security.Cryptography.ProtectedData10.0.9의 Protect/Unprotect에 CurrentUser를 명시하고 사용자 SID만 허용하는 파일/폴더 ACL을 적용한다. 저장 파일의 소유자/권한/크기·부모 경로 reparse point도 확인한다. DPAPI entropy는 앱 분리용 공개 상수이며 별도 비밀키라고 주장하지 않는다. 복호화 바이트/보존 토큰 버퍼는 사용 후0으로 지우지만 HTTP/JSON/.NET immutable string의 메모리 잔류를 완전히 제거한다고 보장하지 않는다. 앱은 메모리 덤프를 만들지 않는다.

쓰기: 차단 표식 flush → 암호화 → pending에 WriteThrough/flush → 동일 폴더 교체 → 소유/권한 확인 → 차단 해제. 실패 시 이전 암호문을 성공으로 대체하거나 미완료 파일에서 복원하지 않는다. 실제 전원 차단/파일시스템 내구성 시험은 미실행이며 '원자적 교체'가 모든 디스크 장애에서 영구 보존을 보장하지 않는다. 복원 완료 저장에는 원래 암호문의 지문을 대조하므로 다른 인스턴스의 삭제/교체를 되돌려 만들지 않는다.

## 시작·실패·종료

App Loaded→SteamWorkspaceViewModel.RestoreRememberedAsync는 저장 동의와 정상 파일이 있을 때 **실행당 최대 한 번**만 시도한다. 네트워크 전에 차단 표식을 남긴다. 파일/토큰 확인 실패는 인증 HTTP0, 정상 복원은 인증 POST2개(Community 한정), 자동 보관함/목록/호가/거래 조회0이다. 401/403/429/CAPTCHA·거절·타임아웃을 우회하거나 재전송하지 않는다. 실패/손상/중단/권한 오류는 안내 후 명시적 삭제/새 QR을 요구한다.

복원 후에도 실거래 기본 OFF이며 가격·수수료·자산 계약을 다시 읽고 별도 세션 선택/최종 승인을 거쳐야 한다. 이전 SaleBatchSnapshot/Unknown을 재실행하지 않는다. SQLite intent·수량 예약은 보존한다.

- 일반 앱 종료: 메모리 세션을 끝내며 사용자가 선택한 암호화 저장을 보존한다.
- 명시적 로그아웃/위험 동의 철회: 메모리 세션 종료+로컬 자격 증명 삭제+거래 권한 철회, 거래 저널 유지.
- 로그인 유지 OFF/저장 로그인 삭제: 로컬 파일만 제거한다. 현재 메모리 연결은 별도 로그아웃까지 사용할 수 있다. 서버 토큰 철회를 성공했다고 표시하지 않는다.
- 삭제/암호화 저장 실패: 완료로 가장하지 않고 OFF/확인 불가를 표시한다. 저장소에 접근할 수 없으면 이전 파일이 남을 수 있어 직접 삭제가 필요하다. 쓰기 불가 저장소로는 자동 복원 요청도 시작하지 않는다.

## 검증과 한계

합성 QR/HTTP, 실제 Windows CurrentUser DPAPI·ACL, 재시작·만료·거절·계정 불일치·손상·구버전/잘못된 소유자 envelope·미완료 저장·잠금 실패·교체/삭제 경쟁·취소·로그아웃·진단ZIP/원문 누출 검사를 수행한다. WPF는 비표시 STA/RenderTargetBitmap만 사용한다. 실제 다른 Windows 사용자/별도 PC에서의 복호화 시도, 실제 앱 종료/강제 전원 차단·물리입력, 실제 Steam 토큰 복원/자동 갱신·계정 거래는 NOT VERIFIED다. 실제 소스의 계약·합성 PASS를 실계정 PASS로 승격하지 않는다.

## 최소 사용자 확인

1. 새 portable을 별도 폴더에 풀어 직접 실행한다. 기존 사용자 DB/저널·진단·롤백본은 유지한다.
2. 위험 확인 후 로그인 유지 ON → QR 시작 → 공식 모바일 앱 수동 승인 → 인증 확인. 'Steam 인증 완료'와 'DPAPI 암호화 저장 완료'를 각각 확인한다.
3. **로그아웃 버튼을 누르지 않고** 앱을 종료한 뒤 다시 실행한다. 단일 복원 완료/재인증 필요를 확인한다. 자동 거래/전체 보관함 갱신은 없어야 한다.
4. 연결되면 판매목록·지갑/수수료 자가검증은 직접 실행한다. 기존 HTML 수수료 계약이 해결됐다고 가정하지 말고 거래 OFF를 유지한다.
5. 실패 시 그 작업의 진단 ZIP만 검토·내보낸다. Auth 폴더/토큰/쿠키/QR은 보내지 않는다. 반복 자동 복원은 하지 않으므로 필요하면 저장 로그인 삭제→새 QR을 선택한다.
6. 명시적 로그아웃 후 재시작에서는 QR이 필요함을 확인한다. 로컬 삭제는 Steam 서버 철회가 아니다.