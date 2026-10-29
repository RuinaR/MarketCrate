# MarketCrate

Windows용 Steam 보관함·장터 관리 도구입니다. 공식 Steam 제품이 아니며 비공식 연동에는 계정 제재 위험이 있습니다.

## 설치와 로그인

1. `artifacts/releases.json`의 **Latest**에 표시된0.18.0 Windows x64 portable ZIP 전체를 새 폴더에 압축 해제합니다. 별도 .NET 설치 없이 실행하도록 런타임을 포함하며 Windows10/11 x64가 대상입니다.
2. `MarketCrate.App.exe`를 직접 실행하고 **계정 및 설정**에서 위험 안내를 확인합니다. 기존 Steam·브라우저 쿠키를 가져오지 않습니다.
3. 원하면 QR 시작 전에 **로그인 상태 유지**를 선택합니다. 공식 Steam 모바일 앱에서 직접 QR을 승인한 후 앱에서 승인 상태를 확인합니다.
4. 일반 앱 종료는 암호화 저장 로그인을 보존합니다. 명시적 로그아웃/이 PC 저장 로그인 삭제는 로컬 자격 증명을 지웁니다. 사용자0.17에서 실제 재시작 복원이 성공했으며 새 버전/장기 만료·회전까지 확인한 것은 아닙니다.

## 보관함과 판매 관리

- **보관함:** 게임 선택→보관함 갱신→검색/아이템 선택·수량→지갑·수수료 조회→선택 시세 갱신→최종 가격 검토 순서로 사용합니다. 필요한 게임이 없으면 게임·판매 설정에서 확인한 AppID/context를 추가합니다.
- **즉시거래가능가:** 현재 구매주문 기준 예상 가격이며 실제 즉시 체결을 보장하지 않습니다. 동일 상품은 호가 잔량을 공유하고 가격별 수량으로 나뉩니다. 정확한 평균 자료가 없으면 중앙값 추정치를 평균이라고 표시하지 않습니다.
- **판매 중:** 판매목록을 직접 갱신하고 선택·취소 검토·재가격을 진행합니다. 취소/반환이나 등록 결과가 불명이면 자동 재전송하지 않습니다.
- **전체 판매등록:** 최종 검토의 한 번 승인으로 검토된 전체 배치를 시작하는 구조입니다. 실제 거래는 매 실행 기본 OFF이며 계정·자산·통화·수수료·가격을 검증하고 별도로 해당 세션에서 활성화해야 합니다. 공식 Steam 모바일 승인은 사용자가 직접 합니다. 승인 대기는 게시 완료가 아닙니다.

## 수수료 확인과 문제 해결

0.18은 공식 판매 화면이 선택한 수수료 모드를 확인합니다. 구형 wallet/app 설정 모드에서는 불필요한 새 수수료 API를 호출하지 않고, 신형 모드에서만 JSON 계약을 조회합니다. HTML 실패를 성공이나 구형 모드로 대체하지 않습니다. 확인하지 못한 가격·통화·수수료·스택은 전송을 차단합니다. 지갑 통화만 독립 갱신할 수도 있습니다.

**현재 실제 계정의 수수료 계약과 판매 게시 성공은 미검증입니다. 완전 실거래 검증/RELEASE_READY를 선언한 버전이 아닙니다.** 먼저 지갑·수수료를1회 조회하고 공식 Steam 판매 화면의 최소/일반/다른 가격 구간을 비교하세요. 게임·판매 설정에서 판매자 수령액·구매자 결제액·Steam/게임 수수료를 직접 입력할 수 있습니다. 보이지 않는 금액은 추측하지 마세요. 사용자 비교값은 서버 검증/거래 권한이 아닙니다.

오류가 나면 반복 거래하지 말고 **문제 해결→관련 로그 검색→내보낼 범위 검토→진단 ZIP 로컬 저장**을 사용하세요. QR·토큰·쿠키·Auth 폴더·원본 HTTP 응답을 전달하지 않습니다. 진단을 자동으로 외부에 보내지 않습니다.

설정·저장 로그인·SQLite 거래 저널은 `%LOCALAPPDATA%/MarketCrate`에 보존됩니다. 업데이트는 새 앱 폴더에 풀어 실행하며 사용자 데이터 폴더를 삭제하거나 배포 폴더로 복사하지 않습니다. Latest/롤백의 정확한 ZIP·SHA256은 [현재 상태](docs/CURRENT-STATUS.md), [배포 확인](docs/DEPLOYMENT-CHECKLIST.md)을 보세요. 최신0.18과 사용자 복원이 확인된0.17 롤백본을 보존합니다.

일반 앱에는 Mock/연결 자가검증 화면이 없습니다. 타 프로세스·게임 메모리/입력/창/화면에 접근하거나 Steam Guard를 자동화하지 않습니다. 라이선스와 코드 출처는 [THIRD-PARTY-NOTICES](THIRD-PARTY-NOTICES.md), 개발 계약·검증 범위는 [연동 조사](docs/STEAM-INTEGRATION-FEASIBILITY.md), [QA](docs/QA-MATRIX.md), [로그인 유지](docs/SESSION-PERSISTENCE.md)를 확인하세요.

<details>
<summary>이전 배포·개발 기록 (아래 내용은 당시 상태이며 현재 지원 판정이 아닙니다)</summary>

# MarketCrate

### 0.17.0 배포 확정 — 로그인 유지

구현 소스 **1700bfdc6cb3cf0df984e7c76b0a14fff8558372**, origin/main push. Windows x64 self-contained LiveOnly portable **artifacts/releases/MarketCrate-0.17.0-session-1700bfd-win-x64.zip**, **66,486,397바이트 /413파일**, SHA256 **A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC**. 원본 `artifacts/releases/session-1700bfd-windows-x64`, BuildVersion0.17.0+1700bfdc6cb3cf0df984e7c76b0a14fff8558372. 전체 entry SHA/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData DLL/steam-session MIT 전문/고지·인증/DB/로그/개발파일 제외 검사 PASS. PE/BAML은 Mock 부재와 실제 Shell/Steam/AuthStore/refresh-token 검증 코드 존재를 확인했다. 경고·오류0, 실제 EXE/설치/서명은 미실행이다.

최종 자동: 기능270/270·자체WPF55/55·배포계약19/19 PASS, 안전 fixture 정리8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 환경제약). 실제 Windows 자체 CurrentUser DPAPI/ACL은 합성 비밀로 검증했다. 실제 Steam 저장 토큰 재사용·서버 회전/철회·수수료/판매·물리GUI·다른 Windows 사용자 복호화·전원 차단 시험은 미실행/미검증이다. LIVE_VERIFIED를 부여하지 않는다. 최초75개 판정47/15/0/4/9 유지, 실거래는 매 실행 OFF다.

롤백0.16.1 ZIP/SHA **94F7E423D49AA0F0AF6C4651A50486F95548A93B45EAE850B488480C70012C2F** 보존(완전 실사용/실거래 검증이라는 의미 아님), 사용자 관측0.16.0 및 그 이전 배포본/설정/SQLite/저널/진단/운영 인증 자료도 보존했다. 최신/롤백은 ignored artifacts/releases.json에 기록했다. 사용자 자료 삭제0·운영 Auth 접근0·실제 계정/GUI/타 프로세스/전역 입력/화면/시스템 설정 접근0. 최소 사용자 확인은 SESSION-PERSISTENCE의 'QR 전 유지ON→인증/저장 확인→로그아웃 없이 종료/재실행→직접 읽기 확인→명시적 로그아웃' 순서다. 토큰/쿠키/Auth 폴더는 진단 전달 대상이 아니다.

## 0.17.0 — 선택적 Steam 로그인 유지

QR 전에 **로그인 상태 유지**를 선택하면 MarketCrate 자체 Refresh Token을 DPAPI CurrentUser/사용자 전용 권한으로 암호화 저장합니다. 일반 앱 종료 후 다음 시작에서 새 Community 세션을 한 번만 복원하며, 명시적 로그아웃/저장 정보 삭제는 로컬 자격 증명을 지웁니다. 기존 Steam/브라우저 정보를 가져오지 않습니다. 실거래는 매 실행 기본 OFF이고 보관함/목록·과거 거래를 자동 실행하지 않습니다.

MIT steam-session의 검증/웹 쿠키 발급을 C#으로 제한 이식했습니다. Node 런타임/AGPL 코드는 추가하지 않았습니다. 저장 토큰이 만료·거절되면 새 QR이 필요하며 자동 Refresh Token 회전/서버 철회·실계정 호환성까지 검증된 것은 아닙니다. 같은 사용자 악성코드를 DPAPI로 막는다고 주장하지 않습니다. [저장/복원·실패·삭제 및 최소 확인 절차](docs/SESSION-PERSISTENCE.md), [오픈소스 비교](docs/STEAM-INTEGRATION-FEASIBILITY.md), [고지](THIRD-PARTY-NOTICES.md).

수수료 HTML/newFeeRates 적용·KRW 실제 수수료/스택/거래 검증은 여전히 별도입니다. 불완전한 계약을 임의 수수료나 합성 성공으로 대체하지 않습니다. 아래 이전 배포 기록은 이력입니다.

### 0.16.1 배포 확정 — 수수료 HTML 오분류 수정

소스 **f5ed123aff841b85fa9a6865fa5a1f4a886004a9**, origin/main push 완료. 최신 Windows x64 self-contained LiveOnly portable: `artifacts/releases/MarketCrate-0.16.1-fees-f5ed123-win-x64.zip` (**66,475,258바이트 /412파일**), SHA256 **94F7E423D49AA0F0AF6C4651A50486F95548A93B45EAE850B488480C70012C2F**. 원본 폴더 `artifacts/releases/fee-f5ed123-windows-x64`, BuildVersion0.16.1+f5ed123aff841b85fa9a6865fa5a1f4a886004a9. Release 빌드·실제 전용 metadata/BAML·CLR/WPF10.0.9/SQLite x64·라이선스·민감/개발파일 제외·전체ZIP entry SHA 일치 검사 PASS. 실제 EXE/설치/서명·실계정/거래 미실행. 초기 NuGet 감사 메타데이터의 sandbox 네트워크 실패는 정상 접근으로 재빌드하여 해소했으며 감사/경고 검사를 끄지 않았다.

최종 기능250/250·WPF52/52·배포계약14/14 PASS, 경고·오류0. 안전정리7 PASS/1 NOT VERIFIED 유지(실제 junction 환경제약), 사용자 산출물 삭제0. 롤백은0.16.0 ZIP/SHA1E758055EB23B5060EEE57BA6E819AEC323E2B698A5AC8518EF307FE48E9704A이며0.15.1 이전본·진단/DB/저널/설정/인증자료도 보존했다. 최신/롤백은 ignored artifacts/releases.json에 기록했다. 바이너리/진단/개인 DB는 Git에 넣지 않는다.

**이 배포는 실제 수수료 성공을 확인한 버전이 아니다.** 확인된 HTML=Challenge 오분류/안전 처리/원인 진단을 수정했다. 실제 사용자 HTML 종류와 계정의 newFeeRates 적용/수수료 계약은 새 버전에서 지갑·수수료만1회 사용자 승인 조회한 정제 ZIP으로 확인해야 한다. 미확인 금액·통화·수수료의 거래는 계속 차단, 제재 위험/LIVE_VERIFIED 미확정 유지. 최초75개 판정47/15/0/4/9 변경없음. 앱·타 프로세스/화면/전역 입력 접근0.

## 0.16.1 — 수수료 HTML 응답 진단·안전 처리

사용자0.16.0에서 지갑 조회 성공이 관측됐습니다. 수수료는 HTML을 받아 아직 실계정 계약이 확인되지 않았습니다. 이 버전은 HTML을 일괄 CAPTCHA로 판정하던 오류를 수정하고 장터 화면/로그인 폼/보안 위젯/접근 거부/분류불가를 구분합니다. 공식 판매 UI와 유사 오픈소스의 수수료 경로를 대조했으며, 기본5%·10%나 USD 계산식을 검증된 KRW 계약으로 대신 넣지 않습니다.

고급 설정의 공식 단가 비교는 서버에서 조회한 해당 게임의 수수료가 있을 때만 쓸 수 있습니다. 사용자 입력으로 판매 가격이나 거래 권한을 바꾸지 않습니다. **실제 전용은 실거래 검증 완료를 뜻하지 않습니다.** 읽기·수수료·KRW·스택 계약이 부족하면 거래는 계속 차단됩니다. [최소 재확인 절차](docs/READ-ONLY-VALIDATION.md), [소스 조사·미확정 사항](docs/STEAM-INTEGRATION-FEASIBILITY.md). 아래 이전 배포 설명은 이력입니다.

## 0.16.0 — 실제 Steam 전용 Release

최신 portable: `artifacts/releases/MarketCrate-0.16.0-live-85756b4-win-x64.zip` (66,469,582바이트/412파일). SHA256 `1E758055EB23B5060EEE57BA6E819AEC323E2B698A5AC8518EF307FE48E9704A`. RuntimeMode=LiveOnly, Release/Mock 구현 제외/전체 파일 무결성 PASS. 실제 창·실계정 성공 검증은 별도입니다. 새 폴더에 풀어 MarketCrate.App.exe를 직접 실행하세요. 기존 데이터는 삭제하지 마세요.

배포 앱에서 Mock 보관함·판매목록·가짜 배치·데이터 생성·Mock 재시도·Demo 저장소를 제외했습니다. 일반 실행은 실제 Steam 작업 공간과 진단 화면만 표시합니다. 실제 서버 응답이 없으면 빈 상태·실패·미검증으로 표시하며 합성 성공으로 대체하지 않습니다.

지갑은 Steam의 `/market/eligibilitycheck/?goto=%2Fmarket%2F` 자격 확인과 고정 Market 복귀를 처리합니다. 서버가 발급한 쿠키만 현재 앱 세션에서 표준 처리하며, 자격 거절·로그인·403/429·알 수 없는 이동은 우회하지 않습니다. 제공된 ZIP의 유형은 이 흐름과 부합하지만 정확한 원문 경로는 없어 실계정 해결 완료로 단정하지 않습니다.

테스트용 Mock 소스는 회귀 검증을 위해 개발 구성에만 남겼으며 Release DLL/BAML에서는 제외됩니다. Release는 `--isolated-qa`도 거부합니다. 기존 사용자 DB·Steam 저널·인증 자료·진단 ZIP·이전 배포본은 보존합니다. [직접 확인 절차](docs/READ-ONLY-VALIDATION.md), [현재 상태](docs/CURRENT-STATUS.md). 아래 이전 버전 설명은 이력입니다.

## 0.15.1 — 지갑302 후속 수정·안전진단

0.15.1 배포: 소스 d2ee9a4679e24854102f09355ff2d5f125f8c05a, `artifacts/releases/MarketCrate-0.15.1-wallet-d2ee9a4-win-x64.zip` (66,507,841바이트/412파일). SHA256 `106E89E7C801250C1C264F92DDC2BE87710A70CD3591BD09B38ADDFD9BE820F2`. Release/전체entry SHA/SourceCommit·CLR/WPF/SQLite·민감/개발파일 제외검사 PASS, 경고오류0. 실제EXE/지갑/계정은 미실행이며 해결완료 판정 아님. 0.15.0을rollback으로등록하고0.14/0.13·기존추출본/진단ZIP·DB/저널/인증/설정은보존했다.

사용자0.14/0.15진단에서지갑은HTML파싱전302로중단됐습니다. 슬래시없는공식 /market 주소로의제한된단일이동을추가하고 원본URL을저장하지않는목적지유형진단을보완했습니다. 실제302의목적지는구로그에없어실제지갑문제가해결됐다고보장하지않습니다. [지갑항목만1회재확인](docs/READ-ONLY-VALIDATION.md)을따르고실패하면검토된진단ZIP을제공하세요. 거래나인증제한우회는하지않습니다.

## 0.15.0 — 조건부 비공식 실거래 흐름

최신 Windows x64 portable: `artifacts/releases/MarketCrate-0.15.0-stage15-eb9ebd7-win-x64.zip` (66,506,796바이트/412파일). SHA256 `EF520920413636DC1456C30A3C94F6ADAD03C08D0082156ED0544983520F43D5`. 소스eb9ebd7/전체entry검사 PASS, 실제실행·실계정 미검증. 기능223/223·자체WPF48/48·배포계약14/14 PASS. 0.14 롤백본과0.13/사용자자료 보존.

실제 거래는 **기본OFF**입니다. 사용자가 고급설정에서 위험동의·‘실제 거래’ 입력 후 읽기preflight를 통과한 실행세션에만 선택할 수 있습니다. 이는 Valve허가나 실거래검증 완료가 아닙니다. 계정일치·명시적지갑단위/증분·게임fee·판매가능 비스택자산·같은게임 기존등록수수료 증거가 없으면 차단합니다. 스택/중앙값 제출·기존등록없는first-bootstrap은 아직 제한됩니다.

방문한게임의 선택을 전체검토에 포함하고 한 번 승인하면 검증된모든 단위를 직렬처리합니다. 최신호가가 바뀌면 이전승인을 폐기하며 자동저가판매/불명요청 재전송은 하지 않습니다. Steam Guard는 공식앱에서 직접승인하고 명시적새로조회로 게시를 확인합니다. 취소반환의 인과증거가 없으면 재등록하지 않습니다.

Wallet302는 검증한같은host Market/언어이동 한 번만 허용하며 로그인·제한을 우회하지 않습니다. 사용자0.14의 정확한목적지/실계정KRW·등록취소 계약은 미검증입니다. Codex는 실제계정/거래/창을 실행하지 않았습니다. [직접읽기·단계별거래검증](docs/READ-ONLY-VALIDATION.md), [계약과위험](docs/STEAM-INTEGRATION-FEASIBILITY.md). 아래이전버전 배포는 이력입니다.

## 0.14.0 — 개별 읽기 권한·빈 보관함 판정 개선

최신 portable: `artifacts/releases/MarketCrate-0.14.0-stage14-3e5201c-win-x64.zip` (소스3e5201c,63.42MiB/412파일). SHA256 `E1A001291F4AE0F6BE0C5410FA51421BD95E52612472BD9C187890CBED034680`. 0.13 롤백본 보존. 파일 무결성/Release 정적검증 PASS, 실제 실행·실계정은 사용자 확인 필요.

0.13 사용자 관측은 QR기록/목록첫8건·CS2자산0건·지갑차단입니다. 전체실계정 지원 검증은 아닙니다. 새버전은 조회권한실패와 로그인전체실패를 분리하고 불완전응답을 정상빈으로 표시하지 않습니다. 지갑통화만 확인되면 가격단위·수수료를 추측하지 않습니다. 자가검증 요약·접힌작업상세에서 다음행동을 확인하세요.

portable을 별도폴더에 풀어 직접실행하세요. 기존DB/저널 보존. 최신/롤백 경로·SHA는 artifacts/releases.json에 기록하며 0.13 ZIP/추출본을 보존합니다. `pwsh -File scripts/Clean-Artifacts.ps1` 기본dry-run, 실행폴더/개인자료 자동삭제 없음. [읽기검증](docs/READ-ONLY-VALIDATION.md), [배포·정리·롤백](docs/DEPLOYMENT-CHECKLIST.md).

기능207/207·자체WPF46/46·배포계약14/14 PASS. 사용자wallet원인은 정제진단 추가필요. IntegrationBlocked·비공식자동화 제재위험 유지, Codex 실계정/GUI/거래 미실행. 아래 이전배포 경로는 이력입니다.

## 0.13.0 — 사용자 실행 읽기 전용 연결 자가검증

로컬 배포: `artifacts/MarketCrate-0.13.0-stage13-dd7e276-win-x64.zip` (78.31MiB,0.13.0+dd7e276). 런타임 포함·정적 검사 PASS, 실제 앱/인증/조회는 사용자 별도 확인이 필요합니다.

기존 브라우저 로그인은 **공식 Steam 장터에서 직접** 이용할 수 있습니다. 앱으로 브라우저/클라이언트 세션을 넘기는 공개 지원 경로는 확인하지 못했습니다. OpenID는 SteamID 신원 확인이며 장터 세션이 아닙니다. 앱은 기존의 **독립 QR 실험 로그인**을 유지하며 쿠키·클라이언트 파일·자격 증명 저장소를 읽지 않습니다.

로그인 후 **계정 연결 → 읽기 전용 자가검증 화면**에서 게임/한 품목/허용 범위를 선택하고 이번 실행에 동의하세요. 기본 범위는 모두 OFF이며 자동 실행·재시도하지 않습니다. 목록·보관함은 첫 페이지만, 나머지는 선택 항목당 최대1요청입니다. 실패·미지원·차단·미검증과 검사 시각/작업ID를 구분합니다. 실제 판매·취소·재가격은 실행하지 않습니다. [사용자 절차](docs/READ-ONLY-VALIDATION.md), [인증 검토 근거](docs/STEAM-INTEGRATION-FEASIBILITY.md).

합성 기능199/199·비표시 WPF45/45·배포계약14/14 PASS. 실제 Steam 계정/서버 계약·현재 물리 GUI는 미검증이며 IntegrationBlocked를 유지합니다. 이 자가검증도 비공식 자동화의 제재 위험을 없애지 않습니다.

## 0.12.0 — 인증 복구와 Steam 작업 공간

Steam 작업 공간의 **계정 연결 → 보관함 → 판매 중 → 결과·작업 이력 → 고급 설정**으로 작업을 나눴습니다. Mock 탭은 이름부터 구분합니다. 보관함은 게임 목록/검색·아이템/고정 판매 계획의 3열이며 게임 추가·context·계약 코드는 고급 설정에 있습니다.

**Steam 인증 완료**와 조회별 상태가 별도로 표시됩니다. 단순 보관함·수수료·목록 파싱 실패/404/읽기 통신 실패는 인증을 취소하지 않습니다. 실패한 조회만 사용자가 다시 요청할 수 있으며 자동 재시도하지 않습니다. 401은 재로그인이 필요하고 403/429/CAPTCHA·안전 차단은 요청을 중단합니다. 인증이 남아 있다고 요청을 허용하는 것은 아닙니다.

탭 Hover는 Header에만 적용하고 선택 밑줄/키보드 점선을 분리했습니다. 자체 렌더링과 합성 계약 검증이며 **실계정 조회 성공·물리 GUI·LIVE_READY 검증은 아닙니다.** 실제 등록·취소·재가격 기본 차단과 거래 저널/Unknown 보호는 유지합니다. Windows x64 portable은 별도 폴더에 압축 해제하고 사용자별 기존 데이터를 삭제하지 마세요.

## 10차 사용자 연결 빌드

일반 self-contained `MarketCrate.App.exe`를 인자 없이 사용자가 직접 실행하면 Steam 계정·보관함 탭에서 시작합니다. 자동 로그인·조회·거래는 없습니다. `--isolated-qa`는 Steam이 차단된 별도 QA 모드이므로 계정 연결에 사용하지 마세요.

가격 모드의 사용자 명칭은 **즉시거래가능가**입니다. 현재 구매주문 기준 예상 가격이며 실제 즉시 체결을 보장하지 않습니다. 정확한 평균 원자료가 없으면 평균으로 표시하지 않고 중앙값 기반 추정과 구분합니다.

QR 회전/재로그인, 독립적인 판매목록·지갑/수수료 조회, 계약 관측 진단과 공식 장터 직접 관리 버튼을 연결했습니다. **실계정 로그인/조회 계약은 아직 검증되지 않았으며 실제 등록·취소·재가격은 기본 차단입니다.** 사용자 9차 GUI 기본7개 PASS는 기록했지만 LIVE_READY/RELEASE_READY를 뜻하지 않습니다. 최소 수동 검증과 상태 기준은 [읽기 전용 검증](docs/READ-ONLY-VALIDATION.md)을 확인하세요. 공식 장터 대체 흐름도 앱 일괄 등록 완료가 아닙니다.


Windows 10/11 · C# / .NET 10 / WPF / MVVM 기반 Steam 보관함·판매 계획 앱입니다. **Mock 작업 공간과 사용자 시작 실험 연결 화면**을 분리하며 Steam 공식 앱이 아닙니다.

7차: **이미지 opt-in/제한 캐시·재활용**, 계정별 **AppID/context 수동·관측 카탈로그**, **기간 중앙값 기반 추정치(정확 평균 아님)**를 보관함·판매·검토 UI에 연결했습니다. 새 COMPLETE B04/C02/D02는 로컬·합성 범위입니다. **47/15/0/4/9**, 실계정/실제 CDN/물리 GUI는 미검증입니다. 실제 전체 게임 자동 발견/정확한 평균 원자료/인과 증거 없는 stack 반환/live 재시도는 남았습니다. [7차 추적표](docs/REQUIREMENTS-TRACEABILITY.md), [격리 GUI 준비](docs/ISOLATED-GUI-QA.md).

5차: **Steam 실험 연동** 탭에서 위험 확인→로컬 QR→수동 승인 확인→게임별 보관함/지갑/선택 호가/목록 조회→가격 검토·수동 대조를 연결했습니다. 시작만으로 네트워크/로그인/실저널을 만들지 않습니다. KRW/USD 정수 금액과 서버 최소·증분·수수료율·상한, 공유 호가·스택 예약, 원래 비스택 단일 자산 반환을 확인한 취소→재등록 코드를 구현했습니다. 모든 인증·거래 검증은 합성 HTTP입니다. **실제 거래는 정책 근거 미확보로 기본 차단하며, 위험 체크박스만으로 활성화되지 않습니다.** 제재 위험과 인증 탈취 위험은 별개입니다. [근거·지원 범위·남은 제한](docs/STEAM-INTEGRATION-FEASIBILITY.md)을 먼저 확인하세요.

**필수 최종 목표:** 최종 검토의 `전체 판매등록` 한 번 승인으로 선택 전 품목·수량·개별 가격을 실제 Steam 장터에 제출합니다. 현재 읽기는 **Experimental / 실계정 미검증**, 등록·취소·재가격은 **IntegrationBlocked** 기본값입니다. 합성 성공을 실제 판매 완료로 표현하지 않습니다.

6차는 최초 요구 **75개 추적 감사**와 **로컬 구조화 진단/오류 검색/검토 후 ZIP**을 추가했습니다. 현재 판정은 로컬/합성 범위 COMPLETE44·PARTIAL16·MISSING1·BLOCKED5·UNVERIFIED9이며 실계정은 전부 미검증입니다. 실험 화면 필터/선택 보존, 가격 이상 경고, 확정 취소 후 새 재가격 승인도 보완했습니다. **이미지·계정 게임 자동 발견·정확한 live 평균가/실계정 계약 검증은 남았습니다.** [요구사항 추적](docs/REQUIREMENTS-TRACEABILITY.md) · [QA](docs/QA-MATRIX.md) · [진단 사용법](docs/DIAGNOSTICS.md). Steam DB v3는 기존 의도를 보존하며 구 앱의 폐기 승인 재개를 막습니다.

## 현재 기능

- 게임별 합성 보관함, 검색·정렬, marketable/tradable/보류, 전체 선택·수량 편집. 카드와 목록 모두 가상화하며 대량 합성 10,000개 모드를 제공합니다.
- Mock 최근 24시간 VWAP와 구매주문 잔량별 예상가를 분리합니다. 같은 품목의 호가 잔량을 공유하는 단일 계산 경로를 합계·검토에 사용합니다.
- 가격·수량별 검토, 편집, 수수료·실수령 표시. 최종 Mock 명령 한 번으로 전체 스냅샷을 처리하며 직전 수량·가격·잔량 변화는 차단합니다.
- 부분 실패·승인 대기·불명·게시 상태를 구분합니다. 재시작 시 SQLite 저널과 합성 결과 증거를 대조하며 자동 재전송하지 않습니다.
- 판매 목록의 개별 선택→별도 취소 검토→선택 전체 Mock 취소→개별 취소/실패 결과를 저장합니다.
- 판매 중 전체 선택/해제, 취소 확인 후 새 가격으로 Mock 재등록, 확정 실패 등록/취소 작업의 명시적 재시도 검토를 지원합니다. 재시도는 시도 번호별 키를 사용하며 성공·불명·승인 대기는 다시 제출하지 않습니다.
- 잔량 부족 수량은 가격 없는 별도 행으로 표시합니다. 직접 가격을 입력하면 대기 판매 계획에 포함할 수 있지만 즉시 체결은 보장하지 않습니다. 수동 가격은 열린 검토에서만 유효하며 전략 변경/검토 닫기/갱신 시 초기화합니다.
- SQLite에 합성 보관함·설정·배치 이력·요청 증거·판매 목록 저장. JSON v1 저널은 원본을 남기고 한 번만 이관합니다.
- 독자적인 다크 테마·커스텀 템플릿·모션 축소 기본 ON. 전역 입력·타 앱 조사·자동 시작 없음. Steam 연결은 실험 탭에서 사용자가 직접 시작하며 개발 검증에서는 실행하지 않았습니다.

## 빌드·검증

설치된 SDK 10.0.301, Windows Desktop 참조 팩, 로컬 NuGet 캐시의 Microsoft.Data.Sqlite 8.0.31 / SQLitePCLRaw 2.1.12를 사용했습니다. 새 SDK/패키지 다운로드는 하지 않았습니다. 의존성 버전/해시는 Infrastructure의 packages.lock.json에 있습니다.

```powershell
./scripts/Verify.ps1
```

단일 빌드 작업 후 도메인·SQLite·MVVM 콘솔 검사와 **창 없는 STA WPF 검사**를 순차 실행합니다. 테스트 프레임워크 없이 종료 코드로 판정하며 `dotnet test`로 대체하지 않습니다. 저장소의 `runtime/`에 합성 DB/이미지를 만들며 Git에서는 제외합니다. NuGet.Config는 오프라인 소스 정책을 유지하므로 다른 개발 PC에서는 잠금 파일의 패키지를 먼저 준비해야 합니다.

WPF 검사는 MainView/UserControl을 직접 Measure/Arrange하고 RenderTargetBitmap으로 PNG를 만듭니다. **Window 생성·표시, Popup 열기, Application.Run, 포커스 이동, OS 입력, 데스크톱 캡처를 하지 않습니다.** 일반 앱 실행 파일은 생성하지만 개발 검증에서 실행하지 않았습니다.

현재 결과와 이미지 경로는 [QA](docs/QA-CHECKLIST.md)를 참조하세요.

## 저장과 복구

- 일반 앱의 합성 DB: `%LOCALAPPDATA%/MarketCrate/Demo/demo.sqlite`. 테스트 DB는 별도 `runtime/` 아래에만 생성합니다.
- SQLite 스키마 v2와 트랜잭션/FULL 동기화/단일 작성자 lease를 사용합니다. v1의 설정·저널·목록을 보존해 이관하며 미래 스키마·손상 데이터는 차단합니다. 두 번째 앱은 코드 2, DB 초기화 오류는 코드 3으로 종료합니다.
- 불변 배치 스냅샷을 보존하고 변경된 결과/판매 행만 저장합니다. 이전 v1 JSON 레코드도 읽을 수 있습니다. 진단은 시각/정해진 코드/건수만 SQLite 최대 500개에 보존합니다. DB를 열지 못한 시작 오류는 앱 전용 logs에 고정 코드만 남깁니다.
- Submitting을 저장한 뒤 Mock 요청을 실행합니다. Unknown이면 후속 전송을 멈추고 재시작 시 대조만 합니다. 확인 증거가 없으면 Unknown을 유지합니다.
- `Mock 서버 게시 fixture 반영 후 대조`는 사용자가 명시적으로 합성 증거를 바꾸는 개발 기능입니다. Steam Guard 승인이나 실제 게시가 아닙니다.
- 미확정·승인 대기 배치는 보관 해제를 막습니다. 확정 배치를 보관한 뒤에도 이전 이력의 예약 수량을 검사합니다. 자동 재시도/재개는 없으며, 확정 실패만 사용자가 새 검토를 승인할 수 있습니다. 재가격의 취소 확인 직후 미전송 재등록은 별도 명시적 재개가 가능하고, 전송 의도가 기록된 불명 요청은 재전송하지 않습니다.
- `SQLite 합성 데이터·설정·저널 삭제`는 합성 SQLite 내용만 초기화하며 이관 원본 JSON을 보존합니다. 삭제 후 원본을 다시 이관하지 않습니다. 실거래 저장소로 이 동작을 재사용하지 않습니다.

## 제한과 다음 작업

- 실제 입력·키보드 포커스·열린 팝업·스크린리더 및 모니터별 DPI는 **NOT VERIFIED**. 100/125/150/200%는 비트맵 출력 밀도 검사이며 실제 모니터 DPI 전환 인증이 아닙니다.
- 임의 높이 카드·범위 선택/드래그, 다중 배치 탐색, 불명 관리 요청의 외부 증거 대조 도구, 실제 설치/서명/배포·장시간 안정성은 후속 작업입니다. 작업 목적을 알 수 없는 구형 목록의 실패/불명은 추정해 재전송하지 않습니다.
- KRW 수수료는 Demo 추정 정책입니다. 실제 Steam 통화별 산식·호가 매칭·체결을 보장하지 않습니다.
- 실제 읽기/쓰기의 Valve 허용 근거·옵트인·별도 실환경 검증 전에는 LIVE_READY/RELEASE_READY가 아닙니다. 계정 제재 위험 0을 보장하지 않습니다.

[현재 상태](docs/CURRENT-STATUS.md) · [일괄 등록 계약](docs/SALE-EXECUTION-SPEC.md) · [출처](docs/REFERENCES.md) · [외부 의존성 고지](THIRD-PARTY-NOTICES.md)

5차 조사: [Steam 연동 가능성](docs/STEAM-INTEGRATION-FEASIBILITY.md). 공식 Market 최신 판매 번들의 통화·수수료·수량 계약을 추가 확인했습니다. 공식 페이지/비공식 구현 사례는 제3자 자동화 허가가 아니며 실제 거래 기본 차단을 유지합니다.

`./scripts/Publish.ps1`은 framework-dependent Windows 실행파일을 `artifacts/windows-framework-dependent`에 만들고 PE 헤더/로컬 데이터 제외를 검사합니다. 앱을 실행하거나 설치/공개 배포하지 않습니다. [배포 검증 범위](docs/DEPLOYMENT-CHECKLIST.md)를 참조하세요.

프로젝트 자체 배포 라이선스는 아직 선정하지 않았습니다. 비교 제품 코드·시각 자산은 포함하지 않습니다. QR 순수 계산 소스 6파일은 MIT 고지를 보존해 포함하며 배포 시 원 LICENSE를 함께 제공합니다.

### 선택적 Windows x64 self-contained QA 배포

`scripts/Publish-SelfContained.ps1`는 Windows에 .NET을 설치하지 않고 CLR/WPF 런타임을 포함하는 새 artifacts 폴더를 만듭니다. 공식 NuGet 접근과 build pack 복원이 필요할 수 있으며 기존 FDD `scripts/Publish.ps1`도 유지합니다. `Prepare-IsolatedGuiQa.ps1 -Deployment SelfContained`와 **실제 생성한** `-SourceDirectory/-OutputDirectory`를 사용해 기존 guest QA 패키지를 만드세요. 호스트에서 앱/guest launcher를 실행하지 않습니다. 실제 게스트가 없어서 GUI24개는 여전히 NOT VERIFIED입니다. OS/native 전제·런타임 업데이트·라이선스·게스트 수동 절차는 [ISOLATED-GUI-QA](docs/ISOLATED-GUI-QA.md)를 확인하세요.

0.12.0 로컬 배포본: `artifacts/MarketCrate-0.12.0-stage12-49a3ffa-win-x64.zip` (78.30MiB,412파일). 구현 커밋49a3ffa. 기능187/187·비표시 WPF42/42·배포계약14/14 PASS, 실제 현재 GUI/Steam 인증·조회는 미검증입니다. Release 빌드/파일 검사는 실계정·설치·서명 검증을 뜻하지 않습니다.

</details>
