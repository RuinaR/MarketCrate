# MarketCrate — 현재 상태 (2026-10-29 KST, 17차)

### 0.18.0 배포 확정 — 수수료 모드/제품 화면

구현 커밋 **e6ee865861adbfaf46d91ae8a6680b2a0a7c2cb7**, origin/main push 완료. 새 로컬 portable ZIP **artifacts/releases/MarketCrate-0.18.0-fee-mode-e6ee865-win-x64.zip**, **66,478,248바이트 /413파일**. SHA256 **552CF3704C5A0B1A6C176351A314B7F2BE5922E09F8ABD8346C6F36B2556F44B**. 원본 `artifacts/releases/fee-mode-e6ee865-windows-x64`, BuildVersion0.18.0+e6ee865…. CLR/WPF10.0.9·SQLite x64·ProtectedData·기존 MIT 고지 포함, 경고·오류0. 전체 ZIP entry SHA와 소스 폴더 일치, 버전/commit/구성/라이선스·사용자 DB/로그/Auth/PDB 제외 검사 PASS. PE/BAML에서 Mock/자가검증 화면·모델 부재와 실제 resolver/Auth/거래 서비스 존재를 확인했다. 실제 EXE/설치·서명/계정·거래는 Codex 미실행이다.

최종 기능 **286/286**, 비표시 WPF **56/56**, 배포 계약 **19/19 PASS**. 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 제약). 사용자 운영 파일/배포본 정리0. 소스 변경 경로/키·JWT literal 검사에서 민감/산출물 경로0·실제 비밀값0을 확인했다. 기록은 ignored runtime/product17-*.txt/자체 RTB에 있고 개인 자료를 Git/배포로 전송하지 않았다.

롤백은 사용자 로그인 복원이 관측된 **0.17.0** ZIP, SHA256 **A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC**이며 이전0.16.1·사용자 진단/저널/Auth/설정도 보존했다. latest/rollback은 ignored artifacts/releases.json에 기록한다. 바이너리는 로컬 생성이며 GitHub Release로 업로드한 것은 아니다.

IMPLEMENTED/SYNTHETIC_VERIFIED는 위 범위이며 REAL_READ_VERIFIED는 사용자0.17 관측에 한정한다. 새 실제 수수료 모드/공식3구간 금액/등록·게시·취소·스택·새 버전 복원/물리 GUI는 NOT VERIFIED. **REAL_FEE_VERIFIED·LIVE_SELL_VERIFIED·RELEASE_READY 미달**, 최초75개47/15/0/4/9 유지. 사용자 최소 재확인은 새 폴더에서 직접 실행→로그인 복원→지갑·수수료1회→공식 화면3구간 대조이며 실패 시 거래/반복 조회 없이 문제 해결 ZIP을 내보낸다.

## 17차 / 0.18.0 — 수수료 모드와 실사용 화면

**구현:** FeeContractResolver(VerifiedLegacy/VerifiedNew/Unsupported/Unverified/Restricted), 공식 sell props와 현재 지갑·앱 context 대조, 조건부 fee API 호출. FeesAsync·실거래 사전검증·배치 제출·재가격3곳을 같은 resolver로 연결했다. Wallet 통화 관측과 수수료 실패를 분리해 지갑 단독 조회를 유지한다. 자산 publisher 비율 누락/충돌은 검토 전에 차단한다. 기존 등록이 없더라도 최신 공식 모드/서버 지갑·앱·품목 계약을 충족하는 경우만 사전검증 가능하며, 기존 목록의 수수료 충돌 veto는 유지한다.

정식 UI에서 전용 자가검증 페이지·범위/동의·임시 실행 명령/모델을 컴파일 제외했다. 일반 계정/보관함/목록/가격/수수료/최종 검토/등록·취소·재가격/결과/로그는 보존했다. 상시 내부 코드·OperationId를 제거하고 문제 해결→진단 ZIP으로 모았다. 공식 수수료 비교는3구간×4금액, 게임·통화·시각·최소 가격/수수료 구간·증분별 일치를 표시하며 입력은 거래 활성화 근거가 아니다.

**새 사용자 실환경 증거(0.17.0):** 종료/재실행 후 저장 로그인 복원 성공, QR/Community 성공, 판매목록8건, CS2 730/context2 명시적 빈 보관함, KRW wallet 객체/기존 수수료5필드 관측, fee API200 HtmlMarket. 호가/이력/이미지는 선택 품목 없음으로 미검증이다. 과거 아래 '복원 미검증'은 당시 개발자 검증 범위이며 이번 사용자 보고로 0.17 복원 한 경로를 보완한다. 새0.18 복원·장기 만료/회전·타 Windows 사용자까지 확대한 것은 아니다.

| 판정 | 현재 증거 |
|---|---|
| IMPLEMENTED | 모드 resolver/정수 견적/단일 승인 배치·저널·Guard 대기·대조·취소/재가격·로그인 유지·실제 전용 UI 코드 |
| SYNTHETIC_VERIFIED | 기능286/286, 비표시WPF56/56, 배포계약19/19 PASS. 경고/오류0. 새 수수료 모드16개 및 기존 전 회귀 포함 |
| REAL_READ_VERIFIED | 사용자의0.17 로그인 복원·목록 첫8건·명시적 빈CS2·KRW wallet 관측에 한함. 모든 읽기 지원 완료 아님 |
| REAL_FEE_VERIFIED | **NOT VERIFIED**. 실제 sell props/newFeeRates·공식 UI3구간·API JSON 계약 미확보 |
| LIVE_SELL_VERIFIED | **NOT VERIFIED**. Codex 실계정/등록·취소0 |
| RELEASE_READY | **미달**. 실제 수수료/실판매·새 버전 물리 GUI/실행 검증이 남음. 정적 배포 성공과 구별 |

75개 판정47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED 유지. 운영 Auth/설정/SQLite/Unknown 저널/진단·과거 배포본 삭제0. 실거래 매 실행 OFF, 자동 승인/재전송0. 상세 근거는 STEAM-INTEGRATION-FEASIBILITY와 QA-MATRIX. 새 ZIP/해시는 배포 확정 절에 추가한다.


### 0.17.0 배포 확정 — 로그인 유지

구현 소스 **1700bfdc6cb3cf0df984e7c76b0a14fff8558372**, origin/main push. Windows x64 self-contained LiveOnly portable **artifacts/releases/MarketCrate-0.17.0-session-1700bfd-win-x64.zip**, **66,486,397바이트 /413파일**, SHA256 **A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC**. 원본 `artifacts/releases/session-1700bfd-windows-x64`, BuildVersion0.17.0+1700bfdc6cb3cf0df984e7c76b0a14fff8558372. 전체 entry SHA/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData DLL/steam-session MIT 전문/고지·인증/DB/로그/개발파일 제외 검사 PASS. PE/BAML은 Mock 부재와 실제 Shell/Steam/AuthStore/refresh-token 검증 코드 존재를 확인했다. 경고·오류0, 실제 EXE/설치/서명은 미실행이다.

최종 자동: 기능270/270·자체WPF55/55·배포계약19/19 PASS, 안전 fixture 정리8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 환경제약). 실제 Windows 자체 CurrentUser DPAPI/ACL은 합성 비밀로 검증했다. 실제 Steam 저장 토큰 재사용·서버 회전/철회·수수료/판매·물리GUI·다른 Windows 사용자 복호화·전원 차단 시험은 미실행/미검증이다. LIVE_VERIFIED를 부여하지 않는다. 최초75개 판정47/15/0/4/9 유지, 실거래는 매 실행 OFF다.

롤백0.16.1 ZIP/SHA **94F7E423D49AA0F0AF6C4651A50486F95548A93B45EAE850B488480C70012C2F** 보존(완전 실사용/실거래 검증이라는 의미 아님), 사용자 관측0.16.0 및 그 이전 배포본/설정/SQLite/저널/진단/운영 인증 자료도 보존했다. 최신/롤백은 ignored artifacts/releases.json에 기록했다. 사용자 자료 삭제0·운영 Auth 접근0·실제 계정/GUI/타 프로세스/전역 입력/화면/시스템 설정 접근0. 최소 사용자 확인은 SESSION-PERSISTENCE의 'QR 전 유지ON→인증/저장 확인→로그아웃 없이 종료/재실행→직접 읽기 확인→명시적 로그아웃' 순서다. 토큰/쿠키/Auth 폴더는 진단 전달 대상이 아니다.

### 0.17.0 최종 자동 검증

기능 **270/270 PASS**, 자체 비표시 WPF **55/55 PASS**, 배포 파일계약 **19/19 PASS**, 안전 fixture 정리 **8 PASS/0 FAIL/1 NOT VERIFIED**(실제 junction 생성 환경제약). 최종 빌드 경고·오류0. 신규 세션20/자체UI3개와 기존 전 회귀 포함. 직접 Windows CurrentUser DPAPI·ACL/암호문만 저장, 로그·검토ZIP canary 미노출, 인증 복원 시 Unknown 저널/예약 보존·거래0을 확인했다. 마지막 저장 예외 분류/계정 검증을 반영한 최종 source 증거는 runtime/session-build.txt·session-core.txt·session-ui.txt·session-distribution.txt·session-cleanup.txt에 있다.

RTB session-optin/session-restored-small/large/session-paused를 직접 확인했다. 실제 Windows 사용자 간 복호화·실제 GUI/물리입력·전원 차단·Steam 저장 토큰 재사용/회전·수수료·판매/취소는 NOT VERIFIED. NuGet 취약성 메타데이터를 끄거나 SDK/Windows 설정을 설치/변경하지 않았다. 최초75개 판정47/15/0/4/9 유지. 기존 운영 데이터/진단/릴리즈 삭제0. Release·ZIP 해시와 커밋은 확정 기록에 추가한다.

## 16차 / 0.17.0 — 동의 기반 Steam 로그인 유지

현재0.16.1의 실제 전용 구조를 유지하며 MIT steam-session의 WebBrowser token 검증/getWebCookies 경로를 C#으로 제한 이식했다. 로그인 유지 기본OFF, 사용자가 QR 전 선택하면 DPAPI CurrentUser·사용자 전용 ACL로 Auth에 암호화 저장하고 다음 시작에서 한 번만 새 Community 세션을 발급받는다. 앱 종료는 저장을 보존하고 명시적 로그아웃은 로컬 저장/메모리/거래 권한을 정리한다. 로컬 삭제가 서버 철회라는 주장은 하지 않는다.

실패/만료/거절/손상/중단은 자동 반복하지 않으며 미완료 파일/구 토큰 fallback도 없다. 자동 보관함/목록 갱신·거래0, Unknown 저널/예약 보존, 실거래는 매 실행 OFF다. 계정·쿠키 일치 확인 전 인증 완료가 아니다. 저장 실패는 유지OFF로 안내하며 현재 QR 인증 성공을 지우지 않는다.

실제 서버의 저장 토큰 재사용/자동 회전/철회·수수료/KRW·스택/판매·취소와 물리GUI는 아직 미검증이다. WebBrowser에 맞지 않는 GenerateAccessTokenForApp를 강행하지 않으며 자동 Refresh Token 회전은 미지원, 만료/철회 시 새 QR이 필요하다. 수수료 HTML 문제를 새 로그인 기능의 성공으로 덮어쓰지 않는다. 최초75개47/15/0/4/9 유지. SESSION-PERSISTENCE의 최소 사용자 확인 절차를 따른다. 최종 테스트·배포는 확정 기록으로 추가한다.

### 0.16.1 배포 확정 — 수수료 HTML 오분류 수정

소스 **f5ed123aff841b85fa9a6865fa5a1f4a886004a9**, origin/main push 완료. 최신 Windows x64 self-contained LiveOnly portable: `artifacts/releases/MarketCrate-0.16.1-fees-f5ed123-win-x64.zip` (**66,475,258바이트 /412파일**), SHA256 **94F7E423D49AA0F0AF6C4651A50486F95548A93B45EAE850B488480C70012C2F**. 원본 폴더 `artifacts/releases/fee-f5ed123-windows-x64`, BuildVersion0.16.1+f5ed123aff841b85fa9a6865fa5a1f4a886004a9. Release 빌드·실제 전용 metadata/BAML·CLR/WPF10.0.9/SQLite x64·라이선스·민감/개발파일 제외·전체ZIP entry SHA 일치 검사 PASS. 실제 EXE/설치/서명·실계정/거래 미실행. 초기 NuGet 감사 메타데이터의 sandbox 네트워크 실패는 정상 접근으로 재빌드하여 해소했으며 감사/경고 검사를 끄지 않았다.

최종 기능250/250·WPF52/52·배포계약14/14 PASS, 경고·오류0. 안전정리7 PASS/1 NOT VERIFIED 유지(실제 junction 환경제약), 사용자 산출물 삭제0. 롤백은0.16.0 ZIP/SHA1E758055EB23B5060EEE57BA6E819AEC323E2B698A5AC8518EF307FE48E9704A이며0.15.1 이전본·진단/DB/저널/설정/인증자료도 보존했다. 최신/롤백은 ignored artifacts/releases.json에 기록했다. 바이너리/진단/개인 DB는 Git에 넣지 않는다.

**이 배포는 실제 수수료 성공을 확인한 버전이 아니다.** 확인된 HTML=Challenge 오분류/안전 처리/원인 진단을 수정했다. 실제 사용자 HTML 종류와 계정의 newFeeRates 적용/수수료 계약은 새 버전에서 지갑·수수료만1회 사용자 승인 조회한 정제 ZIP으로 확인해야 한다. 미확인 금액·통화·수수료의 거래는 계속 차단, 제재 위험/LIVE_VERIFIED 미확정 유지. 최초75개 판정47/15/0/4/9 변경없음. 앱·타 프로세스/화면/전역 입력 접근0.

### 0.16.1 최종 자동 검증

기능 **250/250 PASS**, 자체 비표시WPF **52/52 PASS**, 배포 파일계약 **14/14 PASS**, 최종FAIL0·빌드 경고/오류0. 안전정리7 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 환경제약), 이번 사용자 배포/데이터 정리0. 신규 수수료/복구 오류 주입·JSONL 원인 연결/비밀 canary 미노출 및 기존 거래 어댑터·가격·SQLite/Unknown 회귀 포함. runtime/fee-final-core.txt·fee-final-ui.txt·fee-final-build.txt·fee-distribution-contracts.txt·fee-cleanup-contracts.txt에 로컬 증거를 남겼다. RTB fee-response-error-row/fee-comparison-small/large·오류 화면 직접검토, 물리GUI/모니터DPI/설치·실계정 수수료/거래·서명 미실행. 최초75개 판정47/15/0/4/9 유지. 중간1회 기존 복구 검사 실패/미확정 원인과 결정적 게이트 보강은 WORKLOG에 보존했다. Release 정적 검사와 실제 실행 검증을 혼동하지 않는다.

## 0.16.1 — 수수료 응답 계약 분리와 비교 도구

사용자0.16.0 진단으로 eligibility 왕복 후 Wallet HTTP200/객체 파싱 성공이 관측됐다. 목록 첫8건·CS2 서버총0건은 기존 범위만 인정한다. Fees200 HTML의 실제 내용/Content-Type은 구 로그에 없어 서버 쪽 원인은 미확정이다. 확인된 코드 결함은 'JSON 기대 경로의 모든 HTML=Challenge'라는 오분류이며 이를 수정했다.

- SteamHttp→SteamResponseContract는 선언 media/본문 형식/고정 HTML 표식을 별도3005 이벤트로 기록. 장터 HTML은 수수료 endpoint 계약오류, 로그인 폼은 해당 조회 권한, 보안 위젯/접근거부는 안전중단, 분류불가 HTML은 계약미확정 안전중단. HTML/200만으로 성공·CAPTCHA·세션 만료를 추측하지 않는다. 같은 endpoint는 재전송하지 않는다.
- 공식 sell dialog는 newFeeRates 조건에서만 현재 fee GET을 사용한다. 기존 지갑 fee 필드는 관측 정보로 분리하며, 기본 비율/단위·구형 모드를 추측해 자동 판매용 계약으로 대체하지 않는다. 유사 프로그램의 고정 CS2/USD floor 계산을 KRW에 이식하지 않았다.
- 고급 설정의 공식 장터 단가 비교는 현재 게임의 조회된 정확한 fee schedule이 있을 때만 가능. 수동 입력은 판매계획/권한에 반영하지 않고 불일치/입력오류를 표시한다. 수수료 미확인 상태에서는 버튼이 비활성화된다.
- 인증/조회 계약 오류의 상단 안내도 분리했다. 실제 전용 Release/Mock 컴파일 제외·기본 거래OFF·저널·Unknown/예약/중복 보호 유지. 실제 fee/읽기 전체/거래/KRW/스택의 LIVE_VERIFIED는 없음. 실계정 수수료 계약이 확인되지 않으면 전송을 계속 차단한다.

최종 검증·ZIP/커밋은 아래 확정 기록에 기재한다. 최초75개는47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED 유지한다. 사용자0.16.0 Wallet 관측은 0.16.1의 실계정 수수료/판매 성공을 뜻하지 않는다.

## 0.16.0 — 실제 Steam 전용 배포 및 자격 확인 처리

### 0.16.0 LiveOnly 배포 확정

소스 **85756b4f541175cbd388d51c3d3d2fb1349e84e7** origin/main push. 새 x64 self-contained Release: `artifacts/releases/MarketCrate-0.16.0-live-85756b4-win-x64.zip` (66,469,582바이트/412파일), SHA256 **1E758055EB23B5060EEE57BA6E819AEC323E2B698A5AC8518EF307FE48E9704A**. 전체 entry SHA/SourceCommit·CLR/WPF10.0.9·SQLite/라이선스/민감·개발파일 제외 검사 PASS. RuntimeMode=LiveOnly, 필수 PE/BAML 검사로 Mock 구현/모델/구 화면·재시도/구 USD fixture 파서가 없는지 확인했다. 경고·오류0. 실제 EXE/설치·실계정/거래·서명은 미실행이다.

최신/롤백은 ignored artifacts/releases.json에 기록, rollback0.15.1의 SHA106E89E7C801250C1C264F92DDC2BE87710A70CD3591BD09B38ADDFD9BE820F2 재확인. 0.15.0/0.14/0.13과 기존 추출본·사용자 진단 ZIP/DB/저널/설정/인증·NuGet 캐시는 삭제하지 않았다. 실제 지갑의 성공/금액 계약은 사용자 확인이 필요하며 실패를 합성 성공으로 바꾸지 않는다. 최종 기능235/235·자체WPF50/50·배포계약14/14 PASS(개발 구성 회귀), Release 별도 빌드/메타데이터/전체ZIP 무결성 검사 PASS. 마지막 Shell 페이지 진단 이벤트 연결은 Release 빌드에서 확인했다.

새 사용자 진단은 Wallet302의 목적지가 RedirectCommunity / RedirectMarketOther / RedirectOtherQuery임을 보여준다. 공식 자격 확인 경로는 해당 분류에 부합한다. 원문 경로가 없으므로 동일 경로였다고 확정하지 않는다. 공식 공개 eligibilitycheck/goto=Market를 무인증 GET하여302→고정 Market 복귀를 확인했고, 이 정상 프로토콜을 기존 코드가 모두 차단하던 누락을 수정했다.

Wallet 흐름은 canonical 이동1회·자격 확인1회·정확한 goto 복귀1회(최대3 hop)만 처리한다. 서버 Set-Cookie를 앱 자체 메모리 세션에서 수신하며 쿠키 위조/타 앱 추출은 없다. goto는 고정 Market root/기존 허용언어만, 중첩·외부·추가쿼리는 거부한다. 자격 확인에서200으로 멈추면 권한 미확인으로 Wallet만 차단한다. 401/403/429·로그인/보안/알 수 없는 이동·순환은 중단하고 데이터 생성/자동 재시도하지 않는다.

일반 시작 경로를 ProductionShellViewModel/ProductionShellView로 변경했다. Demo 저장소를 열거나 Fake 데이터를 만들지 않는다. 실제 Steam 공급자/검토/저널/복구와 진단만 연결한다. Release에서 MainView/MainViewModel·DemoDataProvider/SqliteDemoStore·Mock 실행/관리/모델/재시도·구 USD fixture 파서를 컴파일 제외한다. 테스트 구성만 개발용 Mock 소스를 유지한다. Release의 격리 Mock 인자 및 합성 handler/모드 주입도 거부한다.

비밀정보 없는 설정은 새 사용자별 Preferences/preferences.sqlite3에 저장한다. 기존 Demo DB는 열거나 삭제하지 않는다. SteamExperimental의 기존 실거래 저널 경로는 유지한다. 새 설정의 손상/미지원 버전은 초기화하지 않고 오류로 중단한다.

회귀: 기능235/235·자체 WPF50/50·배포계약14/14 PASS, 경고·오류0. 정리7 PASS/1 NOT VERIFIED(실제junction 생성불가) 유지. Release 메타데이터/BAML에서 Mock 구현 부재와 실제 서비스 존재를 확인했고, 개발 DLL을 넣으면 검사 거부됨을 확인했다. 자체 렌더에서 연결/진단/작은 창 배치를 직접 검토했다. 실계정 지갑 성공·거래·물리 GUI·설치·DPI·서명은 미검증이며 Codex가 실행하지 않았다. ‘실제 전용 구성’은 LIVE_VERIFIED/제품 전체 완료를 뜻하지 않는다. 최초75개 판정47/15/0/4/9 유지.

## 0.15.1 — 사용자 진단 ZIP의 Wallet302 후속 수정

0.15.1 배포: 소스 d2ee9a4679e24854102f09355ff2d5f125f8c05a, `artifacts/releases/MarketCrate-0.15.1-wallet-d2ee9a4-win-x64.zip` (66,507,841바이트/412파일). SHA256 `106E89E7C801250C1C264F92DDC2BE87710A70CD3591BD09B38ADDFD9BE820F2`. Release/전체entry SHA/SourceCommit·CLR/WPF/SQLite·민감/개발파일 제외검사 PASS, 경고오류0. 실제EXE/지갑/계정은 미실행이며 해결완료 판정 아님. 0.15.0을rollback으로등록하고0.14/0.13·기존추출본/진단ZIP·DB/저널/인증/설정은보존했다.

검증: 기능227/227·비표시WPF48/48·배포계약14/14 PASS, FAIL0·경고오류0. 정리검사7 PASS/1 NOT VERIFIED(실제junction 환경제약) 유지. runtime/wallet-patch-verify.txt 및 최종core 재검사 증거. 실제Steam지갑 성공은 미검증이며 Codex 실계정/앱/거래 실행0. 최초75개 판정과 거래보호 변경없음.

제공된 두 ZIP은 각각0.14.0/0.15.0의 동일경로를 보여준다: 세션기록 성공→판매목록8→CS2 명시적자산0→Wallet HTTP302/OtherRedirect/Unsupported. WalletContract/ResponseBody 단계에 도달하지 않았으므로 지갑파싱·지갑미설정·인증만료로 단정할 수 없다. 실제 Location은 구버전 로그에서 제거되어 목적지 미확정이다. 사용자 ZIP은 제자리에서 읽기만 했고 원문·계정자료를 저장소/외부로 전송하지 않았다.

공식 공개 /market 및 /market/를 쿠키없이 확인했으며 둘다200·익명wallet없음이다. /market도유효한데 기존단일이동허용목록은 /market/만받던 누락을 수정했다. 같은origin/HTTPS·고정언어query·한번만 허용, 임의query/인증/보안/다른host/반복이동과401/403/429/CAPTCHA 제한은 유지한다. **실제 사용자목적지가 /market이었는지는 미확정이며 실지갑 해결완료를 주장하지 않는다.**

새 진단은 Host역할/Path유형/Query유형과hop0/1만화이트리스트로 남긴다. 원본Location/host/path/query값/fragment·인증정보는 저장하지 않는다. 미지원302 결과는 ‘다른페이지로 이동해 중단’으로 설명하고 로그인실패나 지갑없음으로 오인하지 않게 한다. 기존세션·독립조회·거래OFF·저널/Unknown 보호 유지. 사용자 직접 지갑범위만1회확인이 다음단계다.

## 15차 — 조건부 비공식 실거래 경로·최종 승인 보호 (2026-10-29 KST)

### 0.15.0 배포 확정

소스 **eb9ebd7ff79585dfbcd20ba17374eb4c4e155031** origin/main push. portable **artifacts/releases/MarketCrate-0.15.0-stage15-eb9ebd7-win-x64.zip** (66,506,796바이트/412파일), SHA256 **EF520920413636DC1456C30A3C94F6ADAD03C08D0082156ED0544983520F43D5**. 전체 ZIP entry SHA와 원본/SourceCommit 일치, x64 CLR/WPF10.0.9·SQLite 네이티브·라이선스 포함, DB/로그/인증정보/개발파일 제외, Release 경고·오류0. 실제 EXE·설치·실계정은 미실행, 디지털서명 미적용.

최신/롤백은 ignored artifacts/releases.json에 기록. 롤백은0.14 ZIP(SHA E1A001291F4AE0F6BE0C5410FA51421BD95E52612472BD9C187890CBED034680), 기존0.13 ZIP·모든추출본/진단/SQLite/저널/설정/인증/캐시는 그대로 보존했다. 이번 차수의 기존산출물 삭제0(성공한 신규합성fixture 자체정리만). 마지막 생성자 권한검사를 DB열기 앞으로 옮겨 차단시 lease잔류를 막고 활성화재요청은 먼저 권한철회하도록 한 뒤 전체기능223/223 재검사(runtime/stage15-last-core.txt), 기존48WPF/14배포검사와 새Release검사 유지. 동의해제·재로그인/계정변경 활성화보존 없음.

### 15차 최종 자동 검증

기능 **223/223 PASS**, 자체비표시WPF **48/48 PASS**, 배포파일계약 **14/14 PASS**, FAIL0, Debug 경고/오류0. 안전정리 **7 PASS/0 FAIL/1 NOT VERIFIED**(실제junction 생성불가, 기존경계/잠금/변조검사유지). 신규16기능/2WPF는 실제어댑터의 합성HTTP 주입이며 사용자모드의 조건부활성화→Guard대기→수동게시대조→취소반환→재가격·감사저널도 검증했다. runtime/stage15-release-verify.txt가 최종전체회귀 증거이고 runtime/stage15-focused-final.txt는 추가user-mode saga 검사 증거다. RTB stage15-trading-controls.png·stage15-multi-game-review.png 직접검토. Codex 실계정/거래·실제창/물리입력/Popup/DPI/설치·서명 미실행. 최초75개판정47/15/0/4/9 유지. 실제 Wallet302/등록취소/KRW/스택 계약·LIVE_VERIFIED는 미확정.

0.15.0. 사용자0.14 관측: 세션 존재/목록 첫8건, CS2 HTTP200 명시0건, Wallet302 OtherRedirect/Unsupported. 목록·보관함 관측만 인정한다. Wallet 실제 Location/인증된 HTML 증거는 없으므로 원인과 전체읽기 계약은 미확정이다.

- Wallet은 단일 same-origin HTTPS /market/ 정규/공개언어 이동만 지원(빈query 또는 l=english/koreana). POST/로그인/보안/지역·다른host/임의query는 따라가지 않고 두번째redirect도 중단한다. 401/403/429/CAPTCHA 우회 없음. 공개 Market은 익명200, 실제302 목적지라고 단정하지 않는다.
- 사용자 요청으로 제품 정책을 변경: 실제 거래 기본OFF 유지, 고급설정의 위험동의+‘실제 거래’ 입력+사용자 시작 읽기 preflight를 거쳐 이 실행 세션만 비공식 거래모드를 선택 가능. Valve 허용 근거와 동일시하지 않는다. 기존 AGENTS의 허가확인 필수 규칙에 대한 이번 명시적 사용자 요청의 예외를 AGENTS에 기록했다.
- 활성화 필수조건: QR Community 연결, Market g_steamID와 QR계정 일치/로그인플래그, 통화·명시적 minimum/increment, strict 앱fee, marketable/잠금없는 비스택자산, 같은게임 기존 실제등록의 수수료 비교. 증거 없으면 무전송 차단. 기존 등록이 없는 계정의 첫bootstrap은 아직 제한된다. 스택/중앙값 제출은 기본차단. 활성화 게임은 세션별 allowlist, 새게임은 별도 preflight. 개발용 합성모드/초기policy와 분리, 동의해제·로그아웃·새인증은 거래권한 철회. 실제서버 계약이 충분히 검증됐다는 판정은 아님.
- 여러게임/context를 scope별 최신보관함·게임fee·통화/자산으로 전체검증하고 한 배치에서 직렬실행. UI는 방문한게임의 선택/수량을 보존해 전체 검토에 포함, 상품(App/hash)별 호가잔량 공유. 모든 품목의 호가지문을 승인snapshot에 고정하고 변경 시 첫intent 전 StaleReview·새검토 요구. 통화·fee·보유량/marketable·시간 불일치는 무전송. 낮은가격 자동대체 없음.
- 취소배치 전체 대상 사전대조 후 개별전송. 응답이 요청전 기존listingID를 재사용하면 Unknown. 취소/반환 인과증거 없는 스택/ID변경은 Unknown·재등록 차단. 재가격의 승인호가지문을 intent에 저장하고 취소 전/반환 후 비교. 기존 journal v3에 optional필드만 추가, 구 요청/예약/증거 보존. 미확정/ID없는 요청 자동재전송 금지.
- IMPLEMENTED: 조건부 활성화→검토→단일승인 배치·Guard대기→수동조회 대조·취소/조건부 재가격 코드와 UI. CONTRACT_VERIFIED: 합성계약만, 실제서버 금액/등록·취소·스택은 미검증. LIVE_VERIFIED: 없음. 실사용 제품 전체완료 선언 안 함. 기본OFF/실계정시험·GUI 실행0.
- 검증 최종집계/배포결과는 아래 확정기록으로 갱신한다. 최초75개47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED 유지. 물리GUI/DPI/설치·계정제재 위험/first-bootstrap·스택반환 인과증거·실서버302 및 KRW계약은 다음 차단요인이다.

### 14차 최종 배포·정리 결과

구현커밋 **3e5201cd5980a4bf36fb25e7dbb6f7cb5e81960f**, origin/main push 완료. 새 **0.14.0 win-x64 self-contained**: `artifacts/releases/MarketCrate-0.14.0-stage14-3e5201c-win-x64.zip` (66,498,491바이트,412파일). SHA256 **E1A001291F4AE0F6BE0C5410FA51421BD95E52612472BD9C187890CBED034680**. 원본폴더 `artifacts/releases/stage14-3e5201c-windows-x64`의 모든파일과 ZIP entry hash 일치, ProductVersion/SourceCommit·CLR/WPF10.0.9·SQLite x64·라이선스·DB/로그/인증/개발파일 제외 PASS. Release 경고·오류0. EXE/설치/물리GUI·실계정은 Codex 미실행, 서명 미적용.

검증된 release index 생성 후 dry-run11폴더/242파일을 재검증해 **45,650바이트(44.58KiB)** 제거, skipped0. 프로젝트생성22파일짜리 exact 비실행 합성 fixture만 대상. 큰 배포폴더는 실행중 여부/사용자내용 불확실성 때문에 보존했으며 수GB 정리를 주장하지 않는다. 롤백0.13 ZIP `artifacts/MarketCrate-0.13.0-stage13-dd7e276-win-x64.zip` SHA256 **2B76B0ACB3440CCBA5F2C2364AAD46BA458D10E89E329A8456400A0ED83BD7C8** 재확인, 사용자추출본·이전실행폴더·진단·SQLite/저널/설정/인증·캐시·LOCALAPPDATA 보존. dry-run/적용증거는 ignored `artifacts/cleanup-plan-f8ca226ee39e4e5aa6149f38d3fb6690.json`, `artifacts/cleanup-result-755a1b5fc521421bb440139a33344cb5.json`. 최신/롤백경로는 `artifacts/releases.json`.

## 14차 — 개별 읽기 권한·빈 응답 검증·안전 정리 (2026-10-29 KST)

0.14.0. 사용자0.13 관측: QR 인증기록 성공, 판매목록 첫8건/활성8건, CS2 730/2 첫0건·marketable0, 지갑 인증확인 차단 및 후속차단. 부분 실계정 증거이며 전체 계약 검증이 아니다. Codex는 실제 인증·계정조회·거래·앱 실행을 하지 않았다.

- 확인된 코드 결함: GET의 모든 redirect/API오류를 Authentication으로 묶던 처리, 불완전한 inventory 응답을 정상0건으로 받아들이던 처리. 알려진 Steam 로그인redirect/API15는 endpoint Authorization으로 제한하고 세션을 보존한다. 다른 공식redirect는 Unsupported·무추적, 위험redirect는 안전중단. 401/만료27·403/429/CAPTCHA·불명 서버제한은 기존 보호 유지. 재전송/우회 없음.
- 사용자 지갑실패 원인은 미확정(실패응답/ZIP 미제공). 공식 공개JS는 wallet_currency와 선택적 minimum/increment 및 fee비율을 소비한다. 익명 Market200의 wallet marker 부재는 사용자 인증실패의 증거가 아니다. 새 WalletObservation은 필드존재/로그인플래그를 구분하며 통화·가격단위를 생성하지 않는다. 통화만 확인되면 통화관측만 성공, 수수료금액/판매준비는 미검증. 엄격한 실제 money contract 유지.
- 정상 빈 inventory는 명시적 success/total0·추가페이지 없음·모순 없음 필요. 필드누락/타입/총계모순은 ContractMismatch/Parsing, 페이지일부와 marketable0/보유0 구분. 사용자 CS2 실제0건의 원인은 기존 요약만으로 판정 불가.
- 자가검증 상단 성공/실패/차단/미검증 요약·다음행동, 행별 접힌 작업상세(분류/시각/OperationId). 전역제한과 개별권한 분리로 독립조회 보존. 한 실행의 Authorization 이후는 차단·무전송, 독립조회는 사용자가 별도로 선택한다.
- 기능207/207·비표시WPF46/46·배포계약14/14 PASS, FAIL0, 경고/오류0. 안전정리7 PASS/0 FAIL/1 NOT VERIFIED(실제junction 생성불가). RTB 요약/상세 직접검토, 실제창/물리입력/Popup/DPI/설치 대체 아님. 최초75개47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED 유지.
- 새 release검증 후 exact 합성fixture만 정리. 실행폴더·0.13 ZIP/추출본·진단·SQLite/저널·설정/계정자료·캐시·LOCALAPPDATA 보존. 최신/롤백은 artifacts/releases.json에 명시. 실거래 IntegrationBlocked, LIVE/READ_ONLY_READY 승격 없음. 비공식 자동화 제재위험 유지.

## 13차 — 기존 인증 활용 검토와 읽기 전용 자가검증 (2026-10-29 KST)

13차 배포: 구현 `dd7e27635104c02fe53c0e282242e9d598c04a4b` origin/main push. **0.13.0 x64 self-contained Release/정적 파일검사 PASS**, 경고·오류0, CLR/WPF10.0.9·SQLite 네이티브·라이선스 포함. `artifacts/MarketCrate-0.13.0-stage13-dd7e276-win-x64.zip` **78.31MiB/412파일**, ZIP 전체 원본 hash 및 ProductVersion/SourceCommit 일치. SHA256 `2B76B0ACB3440CCBA5F2C2364AAD46BA458D10E89E329A8456400A0ED83BD7C8`. DB·로그·인증·PDB/개발파일 제외, 이전 ZIP·사용자 데이터 보존. 실제 EXE/서버 로그인·읽기·거래·설치·서명 미실행. 최소 사용자 절차는 READ-ONLY-VALIDATION의0.13.0절차를 따른다.

기준 main/origin3684d07 clean/fetch 일치. AGENTS/정의/상태/작업기록·인증/읽기/진단 호출 경로 확인. 공식 문서·공개 QR 정의만 GET, 실제 앱/계정 인증·조회·거래·타 프로그램 접근0.

- 공식 OpenID는 브라우저에서 SteamID를 확인하는 수단이고 Community 장터 인증 권한을 주는 경로로 확인되지 않았다. OAuth는 Valve Client ID/권한 협의가 필요한 제한된 서비스다. 기존 세션을 데스크톱 앱으로 전달하는 공식 공개 경로를 찾지 못했으므로 쿠키·클라이언트 파일·자격 증명·프로세스를 읽는 재사용은 구현하지 않았다. 공식 장터 직접 관리 버튼은 사용자 브라우저 안에서만 기존 로그인을 활용한다. 앱 OpenID/OAuth 로그인은 미구현이며 기존 QR 실험 어댑터만 유지한다.
- 새 연결 자가검증 페이지: 7개 선택 범위 기본OFF, 실행별 명시 승인, 범위·게임·품목 변경 시 승인 해제, 실행 시 불변 plan·승인 소비, 실행 중 범위 잠금/중단. 세션 기록·목록·게임 보관함·marketable·지갑·fee·호가·이력·이미지 9결과. 성공은 관측 범위, 실패/차단/미지원/미검증/중단 구분. 각 행 UTC/KST·다음조치·OperationId.
- 목록/보관함은 각각1페이지만 검사, 전체 목록 캐시·선택/수량·저널을 대체하지 않는다. 가격·이미지 캐시는 기존 어댑터대로 채울 수 있다. 선택한 한 품목이 없으면 호가/이력/이미지는 미검증·무요청. fee는 허용된 지갑 조회 성공에만 의존. 403/429/CAPTCHA/인증 요구·안전 latch 시 나머지 미전송. 단순 parse/404/503은 독립 결과만 실패하고 다른 허용된 읽기는 진행, 자동 재시도0.
- 실제 코드 결함 수정: g_rgWalletInfo 변수 부재만으로 Authentication을 추측하던 Wallet 파서를 ContractChanged로 분리. QR Community transfer 결과1에 더해 비어 있지 않고 Secure/미만료인 쿠키를 요구. 지갑·호가 통화/보관함 publisher fee·서버 fee 불일치를 별도 원인으로 보고한다. 새 필드/로그인 경로를 추측해 생성하지 않았다.
- 전체 기능 **199/199**, 비표시 WPF **45/45**, 배포 파일계약 **14/14 PASS**, FAIL0/Debug 경고0·오류0 (runtime/stage13-verified.txt). 기존187/42/14 유지. 실제 QR 발급/승인/최종 세션·읽기 응답/이미지 CDN/물리 GUI·DPI/설치·서명은 미검증. 실제 KRW/스택/판매·취소 계약, 허용 근거는 미확보, 실거래 IntegrationBlocked 및 최초75개 **47/15/0/4/9** 유지.

## 12차 — 로그인/조회 복구 및 Steam UI 재설계 (2026-10-28 KST)

12차 배포: 구현 `49a3ffad9a8b37aa4085d6ace6191ddbd035430e` origin/main push. **0.12.0 Windows x64 self-contained Release PASS**, 경고·오류0, CLR/WPF10.0.9·SQLite 네이티브/고지 포함, DB·로그·세션·PDB·개발파일 제외. `artifacts/MarketCrate-0.12.0-stage12-49a3ffa-win-x64.zip` **78.30MiB/412파일**, ZIP 전체 파일 SHA256가 원본과 일치. SHA256 `EAE969B83190F2EB9A29F7299AD2B879DC7D699502D631E960A6F10308986408`. ProductVersion/SourceCommit 일치. 앱/인증/실거래/설치·서명은 실행하지 않았고 기존 배포본·사용자 데이터는 보존했다.

사용자 최소 확인:① 보관함/판매/진단 탭의 콘텐츠 버튼·체크·입력·드롭다운 위를 이동해 Header 깜빡임이 없는지, Tab/Shift+Tab 점선과 선택 밑줄이 구분되는지 확인.② 직접 QR 인증을 선택한 경우 인증 완료 카드와 개별 조회 상태를 확인.③ 보관함 파싱/404 실패 시 인증 완료가 유지되고 독립 목록/fee 버튼이 동작하는지 확인(401이면 재로그인,403/429/CAPTCHA면 차단이 정상).④ 게임 전환·선택/수량·스크롤·가격 검토를 확인하되 실등록·취소·재가격은 실행하지 않는다. 실패가 남으면 고급 설정의 작업 ID→진단 검토 ZIP으로 원인을 좁히고 QR/원문 응답은 공유하지 않는다.

12차 최종 회귀: **기능187/187 PASS · 비표시 WPF42/42 PASS · 배포 파일 계약14/14 PASS · FAIL0 · Debug 경고0/오류0** (`scripts/Verify.ps1`, ignored runtime/stage12-verified.txt). 기존177/39/14 검사와 거래/Unknown/SQLite 회귀 유지. 사용자 제보의 실제 Steam 보관함 실패 응답은 미확보이므로 해당 실서버 파서 계약이 해결됐다고 보장하지 않는다. 수정한 것은 실패의 범위·인증 소실·UI 동작이며 실계정 파서/응답 형식 검증은 별도다. 실제 앱/입력/Popup/물리 DPI/Steam 인증·거래·설치 미실행.

main/origin `4210269` clean/fetch 일치에서 시작. 인증 네트워크·실거래·실제 GUI/Popup·전역 입력·타 프로세스 접근 없이 진행. 사용자 0.11.0 문제 제보를 바탕으로 재현 검사를 먼저 작성했다.

- 확인된 결함: 일반 read 파서가 공용 SteamHttp.Stop을 호출하고 VM.Error가 LoginState를 덮어씀. Authentication.Connected도 모든 Halted에 묶여 독립 조회까지 비활성화. 이제 서버 결과/Community 세션에 근거한 HasSession/Connected, 공용 요청 제한, 개별 ReadState를 분리했다. 단순 파싱/404/GET 통신 실패는 해당 조회만 실패; 401/인증 만료는 세션 정리·재로그인, 403/429/CAPTCHA/중복 페이지 등 안전 latch는 유지. POST 응답 불명/Unknown/거래 코디네이터 정지 보호 유지, 자동 재시도 없음.
- Inventory/Listings/Fees/Orderbook/History 5개 상태. 로딩 중 기존 목록 보존, fee 실패 시 기존 fee를 검토에 재사용하지 않음. 한 번의 Load 내부에서도 fee 실패 뒤 독립 listings 계속 가능(서버 latch면 중단). 현재 Inventory/Prices/Fees 조건이 불완전하면 최종 검토 명령 비활성. 인증 완료 카드/일부 조회 실패/세션 만료/요청 제한을 분리 표시한다.
- TabItem 전체 IsMouseOver 대신 템플릿 Header Border B만 Hover를 소유한다. 콘텐츠·하위 버튼/체크/드롭다운/입력 Hover와 Header를 분리. Selected 밑줄·키보드 점선은 유지. 실제 물리 깜빡임을 캡처하거나 재현한 것이 아니라 코드 구조와 제어된 DP 검사로 결함을 확인·수정했다.
- Steam 5개 화면 재구성: 계정·조회 상태 / 3열 보관함 / 독립 판매 관리 / 결과 / 고급 설정. 긴 안내·AppID/context·OperationId는 고급으로 이동. 가격 기준·즉시거래가능가/수수료·실수령 예상·부족 수량·최종 검토를 고정 패널로 분리. 기본 컨트롤·가상화 유지, 이미지 안내 한 줄 기호/접근성 툴팁, 선택/게임별 자료/스크롤 보존. view cache 최대8게임/20,000행, 초과 시 오래된 뷰만 제거하며 선택·수량 기억과 저널은 보존.
- ReadState4203/고정 endpoint/OperationId와 기존 HTTP·파서·SQLite 진단 연결. 원문·토큰·QR·계정 정보 제외. 최초75개 ID/판정 **47 COMPLETE / 15 PARTIAL / 0 MISSING / 4 BLOCKED / 9 UNVERIFIED** 유지. 실계정/현재 물리 GUI/DPI·Popup·설치·서명·KRW·스택 실서버 계약은 계속 미검증, 거래 IntegrationBlocked.

## 11차 — 사용자 발견 QR 계약·상태 강조 결함 수정 (2026-10-28 KST)

11차 배포: 구현 커밋 `0705dc2874b64b521e86dd92b3847c1a1b63219c` origin/main push 완료. **0.11.0** self-contained Windows x64 Release 빌드·정적 검사 PASS, 출력 경고/오류0. CLR/WPF10.0.9, x64 apphost/SQLite 네이티브 DLL/라이선스 고지 포함, DB·로그·인증·PDB·개발파일 제외. `artifacts/MarketCrate-0.11.0-stage11-0705dc2-win-x64.zip` **78.29MiB/412파일**, ZIP 전 항목과 원본 SHA256 일치. SHA256 `C3A5AEC49ACB4B8FEA93D4496E49E4D7AB92F2FB7F409F5EA22F1E43A60AEE28`. distribution.json 및 ProductVersion=0.11.0+0705dc2…의 SourceCommit 확인. 실제 EXE/설치/서명/실서버 QR 발급은 미실행. 이전 배포본·사용자별 저장소를 덮어쓰거나 초기화하지 않았다.

기준 main/origin `62663c5` clean/fetch 일치. 실제 인증·앱·Popup·포커스·입력·타 프로세스 접근 없이 공개 정의 GET 및 합성/비표시 검사만 수행했다. IntegrationBlocked/기존 SQLite·저널·수량 예약 계약 변경 없음.

- QR 요청: 공개 HTTP API 메타데이터가 required로 선언한 최상위 device_friendly_name(1,string), platform_type(2,enum WebBrowser=2)을 device_details(3,message)의 내부1/2와 함께 명시. input_protobuf_encoded 표준 base64→UTF8 form 인코딩, 별도 format=json/Accept를 독립 decoder로 검증. uint64 client_id는 문자열·정확한 정수 JSON 모두 처리하고 소수 interval 허용. 누락·범위·URL 이상은 차단한다.
- 확인된 코드 결함: 최상위1/2 누락, 숫자 JSON client_id 거부. 사용자 ContractFailure가 어느 원인인지는 사용자 진단/인증 응답 없이 확정 불가. 실제 BeginAuth 호출/QR 인증은 실행하지 않았으므로 서버에서 해결됐다고 판정하지 않는다.
- AuthContract2005: AuthBegin/HTTP/ApiResult/ResponseParse/ResponseFields/ChallengeValidation 실패 구분, 고정 endpoint/OperationId/안전한 x-eresult만 기록. HTTP200 API 거절을 성공으로 기록하던 문제 수정. QR·토큰·쿠키·원문 본문 제외.
- UI: Action VSM과 Trigger의 중복 속성 제어 제거. Hover/Pressed 배경, Selected 밑줄·선택면, Checked 체크마크, KeyboardFocus 점선 분리. 자체 subtree 입력 방식만 관찰해 마우스 클릭 포커스와 키보드 탐색 구분. Disabled 포인터 효과 차단, ComboBox 이중 opacity 제거. 기본 스타일·키보드 접근성 유지.
- 기존 DP 상태 왕복에서는 사용자 물리 Hover 잔류를 재현하지 못했다. 중복 제어와 상태 시각 혼동은 확인했으며, 실제 IsMouseOver 오작동/브러시 공유 오염을 확인된 원인으로 주장하지 않는다. 비표시 검사는 제어된 DP Trigger와 명시적 VSM 전이이며 물리 입력 검증이 아니다.
- 최초75개 **47 COMPLETE / 15 PARTIAL / 0 MISSING / 4 BLOCKED / 9 UNVERIFIED** 유지. 실계정·현재 GUI·물리 DPI·Popup·실판매는 미검증/차단. 사용자 기존7개 수동 PASS를 이번 빌드 전체 PASS로 재사용하지 않는다.

## 10차 — Steam 사용자 경로 연결 / 즉시거래가능가 (2026-10-28 KST)

기준 main/origin `a0e7c95` clean/fetch. 사용자 보고: **9차 기본 GUI7항목 PASS**(앱실행, hover/click, 선택·수량, 가격전환·검토, Mock배치, 취소·재가격·진단, 재시작 보존). 정확한 테스트 빌드/OS/DPI·24개 전체 결과는 제공되지 않았고 10차 현재 빌드 GUI 성공으로 승격하지 않는다. Codex는 실제 앱/창/Steam 인증·API·거래·Guard를 실행하지 않았다.

- PriceDisplay 공통 표시/설명과 PriceBasis 저장키: 주 모드 `즉시거래가능가`, 기존 한국어 setting alias는 읽고 다음 변경 시 Immediate/ExactAverage 저장. enum/Steam 인덱스/배분/기존 저널 변경 없음. 정확평균 원자료 없음/중앙값 추정 분리. 현재 매수주문 기준 예상이며 실제 즉시 체결 보장 없음.
- 정상 portable 앱 시작은 Steam 탭을 보여주되 인증/조회는 자동 실행하지 않는다. 격리QA는 계속 offline Mock. QR 서버 회전 시 UI matrix 갱신, 빈 기본값 유지/만료 후 재로그인, 연결 중 실수로 새 로그인 시작 차단. logout/인증만료의 이전 데이터 보존·오래된 검토 차단.
- 판매목록/지갑·fee를 보관함과 독립 조회 가능. 404 보관함이 account 판매목록 조회를 막지 않는다(403/429중단은 유지). fee 없어도 취소 **검토만** 가능, 실제 제출은 원래 policy/preflight. 판매목록 시세는 현재 모드로 조회, 최고 구매주문/잔량·현재 단가 차이/중앙값 기간 구분.
- ReadContract4202의 고정 endpoint/count/currency/time/OperationId만 UI/JSONL에 연결. 원문·필드덤프/계정·asset·QR·쿠키·token 없음. 실제 응답이 아니라면 Synthetic으로 표시. 관측 성공을 READ_ONLY_READY로 자동 판정하지 않는다.
- 공식 Steam 장터 버튼은 사용자 클릭 때 기본 브라우저의 고정 공식 URL만 연다. Codex/테스트는 대체 delegate만 사용. 해당 수동 흐름이 단일 승인 앱 일괄 등록 완료를 뜻하지 않는다.
- 최종 기능172/172·detached WPF33/33·파일계약14/14 PASS, FAIL0/Debug 경고0·오류0. 구현 `e5db266` origin/main push 완료. 해당 소스 self-contained x64 Release PASS(경고0/오류0), CLR/WPF10.0.9·SQLite/고지/데이터·PDB 제외/Core commit 확인. `artifacts/MarketCrate-stage10-e5db266-win-x64.zip` 78.3MiB/412개 원본·ZIP hash 일치, 앱 미실행. SHA256 E14B0BA88D314276F485AEB2D8E97A64604B02F7EA7D5A77207C4DC122EC765F. 75개 **47 COMPLETE / 15 PARTIAL / 0 MISSING / 4 BLOCKED / 9 UNVERIFIED** 유지. 실서버·KRW fee·stack/반환 인과·실제 쓰기·현재 GUI·서명/실환경 출시 기준은 여전히 미검증/차단.

## 9차 — 배포·진단 보완, 실제 GUI QA 차단 유지 (2026-10-28 KST)

기준 main/origin `9cc73a4` clean/fetch 일치. 제공된 게스트 없음. 허용된 조회에서 EditionID=Core(Home계열), build26200.9711, X64, Sandbox 실행파일 없음. CIM 가상화 capability 조회는 권한 제한(CimException)으로 미확인; 다른 경로로 반복·권한 상승·VM/프로세스 탐색하지 않음. 공식 지원표상 Home은 Sandbox 미지원. **환경 제공 전 실제 GUI QA BLOCKED, GUI-01~24 모두 NOT VERIFIED(PASS0/FAIL0/NV24).** 호스트 대체 실행/설정 변경/VM 실행 없음.

- 기존 FDD 유지 + `Publish-SelfContained.ps1`: win-x64, CLR/WPF10.0.9·SQLite 포함, 비단일파일/trim·R2R OFF, app PDB/개발소스 제외. 런타임 팩은 공식 NuGet에서 ignored 프로젝트 cache에 복원했고 Windows SDK/Runtime 설치는 안 함. 원본 .NET MIT/제3자 고지 포함. 실제 OS/시작 호환성은 미검증.
- 기존 guest 하네스 재사용: SCD는 시스템 dotnet 검사 안 함, 동일 hash build 재시작 허용/다른 build 덮어쓰기 거부, 결과표는 읽기전용 share 대신 사용자 QAResults에 GUID별 저장. 시작·종료/exit code가 case PASS를 만들지 않음. 이 guest 경로는 AST/정적 검토만, 미실행.
- 실제 진단 결함 재현: 파일 mtime 역전 시 operation ZIP 이벤트 시간 역전(수정 전165/166, 신규1 FAIL) → Utc 정렬, 동일 operation·원인 분류·로컬 ZIP 경로·무DB 단언으로 수정 확인. 거래 저널/Unknown/예약/제출 정책 변경 없음.
- 전체 회귀 **166/166 기능 PASS**, detached WPF **31/31 PASS**, 합성 파일 계약 **14/14 PASS**, FAIL0/Debug 경고0·오류0. 165+31 기존 검사 유지. 75개 판정47/15/0/4/9 유지. 구현 `d19dcc9` origin/main push 완료. 해당 소스의 SCD/FDD Release 모두 PASS(경고0/오류0), x64 apphost/CLR·WPF·SQLite(모드별)·runtimeconfig·고지·DB/로그 제외 정적 확인. SCD CLR/WPF10.0.9·PDB0, guest bundle414개 해시/142.02MiB; FDD40개 해시/33.51MiB. 두 manifest SourceCommit/PackagingCommit/Core metadata 일치, GUI0/0/24. 앱/guest/설치·서명 미실행.
- Steam 인증/이미지/지갑·fee/스택/판매/취소/Guard는 실계정 미검증, IntegrationBlocked. 실제 입력·Popup·물리 DPI·설치·서명·장시간 안정성 미검증. 다음: 별도 Windows11 x64 오프라인 guest 제공/확인 후 기존24개 수동 QA.

## 8차 — 격리 GUI 실행 준비 / 제품 결함 수정 (2026-10-28 KST)

기준 main/origin `4a18077` clean/fetch 일치. 제공된 전용 VM/게스트 접속·실행 환경은 없으며 다른 프로세스/VM/창을 검색하지 않았다. **독립 Windows 확보 및 실제 GUI·설치 실행은 NOT VERIFIED**. 호스트 대체 실행 없음. 최초 75개 ID/판정 **47 COMPLETE / 15 PARTIAL / 0 MISSING / 4 BLOCKED / 9 UNVERIFIED** 유지.

- 게스트 전용 수동 실행 하네스 + 해시 manifest 패키저 + 24개 NOT VERIFIED 시나리오. 패키저는 VM/앱을 실행하지 않는다. `--isolated-qa`는 명시적 게스트 운영자 확인이 없으면 종료4, 별도 사용자 데이터/로그 루트와 Steam factory/로그인 명령 차단을 적용한다. 확인 문자열은 VM 탐지·격리 보증이 아니며 호스트에서 실행하면 안 된다.
- 실제 결함: Steam AnyReview 알림 누락으로 배경 IsEnabled 바인딩 미갱신 → 알림 복구. Mock/Steam 모달의 자체 포커스 scope/Tab·Ctrl+Tab 경계·기존 자체 요소 복귀, 활성 자기 Window에서만 처리. 실제 focus/Popup/키 입력은 여전히 미검증. ComboBox focus/open 테두리, disabled stepper peer 표준 예외 보완.
- 취소된 HTTP/QR을 Transport/Schema 오류로 변환하던 경로 수정. 보관함 조회 취소 후 인증 세션 유지. QR 새 challenge 빈 기본값은 기존 challenge 유지, 비정상 URL/알 수 없는 필드는 차단. 승인 플래그는 bool/정수0·1만 허용, 모호한 응답은 추측하지 않는다.
- 공식 판매 JS 재조회 SHA256은 기존5차와 같음. 공개 proto/maintainer 소스로 QR 선택 필드 근거 대조. 인증된 실제 응답은 미확보. KRW·스택·반환 인과/ID-less/외부 동시 등록은 이전 차단·Unknown 보존.
- 최종 검증: 전체 기능/합성 HTTP/SQLite/진단 165/165 PASS, 비표시 WPF 31/31 PASS, FAIL0, Debug 경고0/오류0. 구현 `f672120` origin/main push 완료. 해당 커밋 Release publish PASS(경고0/오류0), apphost/SQLite x64 PE·Desktop Runtime10 의존성·데이터 제외·라이선스 고지·Core Git metadata 확인. 격리 QA package39개 SHA256 전부 일치, 24개 결과는 NOT VERIFIED. 실제 앱/guest/설치·서명 미실행. 실제 계정/로그인/거래/Guard, 호스트 입력·포커스·화면·타 프로세스 조사는 수행하지 않았다. 디지털 서명/장시간·실제 DPI/설치·업데이트·제거 미검증.

7차 추가 반환 제한: 원래 단일 수량이었어도 취소 후 returned.Stackable/수량 증가·기존 가용 수량이 있으면 proof로 확정하지 않는다. 동일 ID라도 현재 정확히1개/기존0개인 제한 경로만 합성 대조하며 나머지는 Unknown/예약 유지. 실제 nonstack/스택·ID 의미와 인과 증거는 여전히 미검증이다.

### 7차 자동 검증 마감

- 전체 기능/합성 HTTP/SQLite/진단 **158/158 PASS, FAIL0** (기존139+신규19), 비표시 WPF **28/28 PASS, FAIL0** (기존23+신규5), Debug 빌드 경고0/오류0. 마지막 기능 보강 후 전체 회귀 실행. 예상치 못한 binding 오류0/Window·PresentationSource0.
- 실제 PNG 디코딩·재활용60회·전송 중복/취소·재시도/실패 상태와 보관함/판매/검토/추정/진단 이동 이미지를 직접 검사했다. NumericStepper 36px/텍스트 줄바꿈/출처/기준별 부족 수량 안내를 수정·재확인했다.
- 단기 합성 예: 이미지100회 약2ms wall/31ms 자체 process CPU/managed allocation 약1.36MB, LRU4개/280B; 썸네일60회 약1835ms wall/1406ms 자체 CPU/약0.89MB managed allocation, cache210B. 자체 process만 읽었고 CPU 계수 해상도/다른 자체 스레드가 포함된다. 실이미지/네이티브 메모리/장시간 누수/FPS/다른 앱 영향 측정으로 확대하지 않는다.
- 실제 Steam 이미지/인증/보관함/가격/fee/판매/취소/Guard 및 실제 창/물리 입력/모니터 DPI/installer/VM 실행은 NOT VERIFIED. 소스/배포 검사 수정 커밋 `6828294` Release publish PASS(경고0/오류0): PE·로컬 데이터 제외·라이선스 고지 확인, Core DLL의 해당 Git commit metadata 정적 확인. 실행파일·설치 미실행. 구현 `e40492d`, 배포 검사 수정 `6828294`는 origin/main push 완료.

## 7차 최신 상태 — 이미지 / 계정별 context 카탈로그 / 구분된 가격 지표

시작 HEAD/origin/main `158a606`, clean/fetch 확인. 75개 ID 유지/전 행 재평가: **44/16/1/5/9 → COMPLETE47 / PARTIAL15 / MISSING0 / BLOCKED4 / UNVERIFIED9**. 새 COMPLETE **B04/C02/D02**는 로컬 구현·합성 계약·비표시 WPF 범위. B05는 대체 추정치를 연결해 BLOCKED→PARTIAL. 실제 서버/계정/GUI 검증은 승격하지 않았다.

- 이미지: 검증한 classid/instanceid description join과 icon_url_large/icon_url → 메타데이터 별도 maps → 보관함/판매/검토 및 게임 대표 이미지. opt-in 전 네트워크 없음. 인증과 분리한 단일 Steam CDN/고정 128px 경로, URL 토큰 제한, redirects/cookies/proxy OFF. PNG/JPEG 512KiB·512px 이하, 128px off-thread decode/freeze, LRU 128개/16MiB·single-flight·동시 전송1·대기32·15초 timeout·제한 응답 latch. 가상화 unload/재활용/끄기/로그아웃 취소, 늦은 결과 폐기, 없음/꺼짐/미지원/로딩/실패/재시도 UI. 실제 CDN 이미지 미요청/미검증.
- 카탈로그: 3개 seed는 보유 확인이 아니다. 계정별 AppID/context 수동 추가/제거, 판매목록 관측 scope 추가, 기존 Steam v3 SQLite에 원자 저장/복원. context 2 고정 가정 제거. 이름 미확인 App 임시 이름, 판매 가능 종류 수/대표 보관함 이미지/미조회·빈 목록·404·접근 거부·부분 실패 구분. 403만으로 비공개라고 확정하지 않음. 무차별 조회/전체 계정 자동 발견/GetOwnedGames 호출 없음.
- 가격: 즉시 예상가 / 정확한 평균(원자료 없어 이용 불가) / 기간 중앙값의 서버 거래량 가중 중앙값 추정. 24h/7d/30d·수집/기간/구간 수/서버 거래량/방법/한계 표시. 추정은 VWAP/전체 체결 중앙값/즉시 체결가가 아니다. 최저 판매호가는 자료 없음을 표시. 판매목록 선택 최대10상품의 대체 지표도 명시적 조회. 기준/기간 변경은 수동 가격 초기화, 늦은 응답 폐기.
- 보강: Evidence 불변 snapshot/digest, 최종 history 재조회에서 표본 변동(중앙값/합계 같아도)·부족/통화/오래됨 거부. 상품별 preflight 중복 조회 제거/완료 후 승인 freshness 재검사. publisher fee 누락도 차단. 기존 Unknown/스택 반환 인과 증거/취소·재가격 보호 유지. 로그아웃 뒤 늦은 응답이 새 catalog 상태를 덮지 않음.
- 진단: 새 metadata/image/catalog/indicator 이벤트와 operation/request 연결. 원본/URL/토큰/이미지 bytes 로그 없음. 현재 Steam 작업→진단 탭 바로가기. 정책/보존/ZIP 그대로 유지.
- 최종 자동·Release 결과는 7차 QA-MATRIX/WORKLOG 마감 기록 참조. 실제 앱/Window/Popup/전역 입력/다른 앱/실계정/거래/Guard 미실행. [격리 GUI 후속 절차](ISOLATED-GUI-QA.md)는 구성만 준비하고 실행하지 않음.

남은 28개 판정은 추적표에 유지. 핵심 차단: 실제 인증/응답·KRW/스택 계약/정확한 원자료·ID-less/외부 동시 등록/스택 반환의 인과 증거, live 재시도/실환경 QA. **IntegrationBlocked 기본과 최종 단일 승인 전체 실등록 목표 모두 유지.**

## 6차 최신 상태 — 전체 감사 / 구조화 진단 / 안전한 새 재가격 승인

감사 기준 `origin/main=33cd152`(fetch 확인), 기존 사용자 변경 없음. 최초 A–G **75개**: **COMPLETE 44 / PARTIAL 16 / MISSING 1 / BLOCKED 5 / UNVERIFIED 9**. COMPLETE는 로컬/합성/내부 UI 범위이며 실계정 검증은 전부 N. [세부 대응표](REQUIREMENTS-TRACEABILITY.md), [QA 매트릭스](QA-MATRIX.md), [진단 운영](DIAGNOSTICS.md).

- 중앙 DiagnosticHub + whitelist JSONL, 실행/operation/request/batch/internal item 연결, 버전/commit/UTC, 상태/소요 시간/오류 분류/정제 코드 위치. HTTP·인증·데이터·가격·배치·관리·SQLite·UI에 연결. 별도 진단 탭에서 상세 모드/오류·작업 검색/UTC-KST/보존 정책/로컬 검토 ZIP. 토큰·QR·원문·DB export/자동 외부 전송 없음.
- INFO 기본, 상세 모드 설정 복원, async bounded 256 큐/최근512, 2 MiB·20 MiB·20파일·14일 회전·정리. 기록 실패·포화는 health로 표시하고 거래 결과/재전송에 영향 없음. 종료 tail은 최선 노력이며 모든 오류 재현/원인 확정을 보장하지 않음.
- 실험 보관함 검색/정렬/marketable 필터/선택 해제, 실제 판매 게임 필터/해제, 실행 중 갱신/게임 전환 선택·수량 복원/상한 축소. 오래된 가격은 재조회하며 선택을 실제 데이터로 가장하지 않음. UI 가격 이상 경고/신선도 재확인 추가.
- 오래된 재가격은 취소 요청 **전** 차단. 확정 반환 proof가 있는 미전송 의도는 새 가격 검토/승인으로만 새 immutable intent를 만들며 이전 의도를 원자적 Superseded로 보존. 취소/반환/후속 요청 키를 유지해 재시작 중복 방지. 기존 앱이 Superseded를 오해하지 못하도록 Steam schema **v3**로 올리고 v1/v2 내용을 보존한다. 하위 버전 앱으로 DB를 열지 말 것(기존 v2 코드는 v3 거부).
- 창 닫기 검사에 누락된 Steam.Busy 추가. 실제 Window 이벤트는 실행하지 않음. 실행 중 JSONL export 공유 모드 오류를 오류 주입으로 발견·수정. 로그 재정제/부분 ZIP 원자적 완료/검색 중 승인 범위 변경도 방어.
- 검증: 전체 Debug 빌드 경고0/오류0, **139/139** 기능·합성 HTTP·저장소·진단 및 **23/23** 비표시 WPF PASS, FAIL0. Steam v3 migration 후 전체 회귀 통과. 최종 구현 `fab286a` Release도 경고0/오류0, Windows PE·로컬 데이터 제외·MIT 고지 검사 PASS. 실행파일은 생성만 했고 실행하지 않았다. 구현 `5b44e0d`, 진단 보완 `fab286a` origin/main push 완료. 문서 마감 커밋은 최종 보고 참조. 실제 계정 인증/판매/취소/Guard, 실제 앱 실행/Popup/전역 입력/물리 DPI/설치 미실행. 자체 STA 렌더만 허용 범위에서 수행.

**실사용 차단 요소:** 실제 이미지 미구현, 고정 3게임/context 외 계정 게임 발견 미구현, 정확한 실거래 평균 원자료 미확보, 실제 KRW 수수료·스택/반환·인증/응답 계약 미검증, ID 없는 요청/외부 동시 등록 인과 증거 미확보, live 확정 실패 재시도/실환경 QA 미완성. **실제 전체 일괄 판매등록이 최종 목표지만 현재 실판매 완료 아님**, SteamTradingPolicy.Blocked 기본. [최신 허용 근거 검토](STEAM-INTEGRATION-FEASIBILITY.md).

### 이전 5차 기록 (당시 검증)
## 5차 최신 상태: 사용자 시작 연결 UI / 실거래 기본 차단

구현/검증 커밋 `f7bd8aa` origin/main push 완료 (`73ad542..f7bd8aa`). 최종 기록 동기화 커밋은 완료 보고 참조.

- `Steam 실험 연동`의 QR 연결·읽기·선택·가격 검토·판매 관리·수동 대조를 실제 어댑터와 연결했다. Mock의 보관함/합계/상태와 섞지 않으며 앱 시작·탭 생성은 네트워크나 실저널을 열지 않는다. 실제 거래는 `SteamTradingPolicy.Blocked`, 체크만으로 활성화되지 않는다.
- KRW/USD `Money`와 서버 지갑 최소/증분·앱별 fee rate/cap 계산, 수령/수수료/구매자 금액 분리·정확한 역산·모순 중단. 공식 최신 판매 번들의 구조에 근거하며 **인증된 서버 응답/실계정 계산과 비교한 것은 아니다**.
- 동일 상품의 호가를 asset/context 간 공유, 스택 분할·부족 수량 검토·예약/반환 증거·재시작을 구현했다. 스택 제출 capability는 별도다. 외부 수량 증가/ID 변경 불확실성은 보수적 가용량/차단으로 처리한다.
- listingid 없는 후보의 이전 ID/시각/가격/수량/판정 및 감사 이력을 수동 도구로 제공한다. 후보 1개여도 자동 소유 요청 확정 없음. 취소 접수+목록 부재+원래 비스택 단일 자산의 동일 ID 반환을 확인한 경우만 재가격한다. target/후속키 저장·명시적 재개·중복 차단, 5분 stale은 거절한다.
- Steam DB v2는 v1 불명 의도를 보존한다. command/reprice audit 원자 저장, 확정 batch archive 후에도 의도·증거·예약을 보존하고 다음 배치를 처리한다. 실저널 삭제 UX는 제공하지 않는다.
- 최종 `scripts/Verify.ps1`: 전체 Debug 빌드 경고0/오류0, **112/112** 도메인·합성 HTTP와 **20/20** 비표시 WPF PASS, FAIL0. 새 adapter-backed 10,000개 목록/알림 batching과 이전 회귀도 통과했다. [QA-CHECKLIST](QA-CHECKLIST.md).
- 최신 Release publish·PE·로컬 데이터 제외·QR MIT LICENSE/고지 포함 검사 PASS. 자체 QR/가격 검토/보관함/통합 화면 PNG를 생성·검토했다. 실행파일 실행/설치·실제 입력·모니터 DPI·실계정 검증은 수행하지 않았다.
- 남은 제한: 실계정 QR 스캔/인증/보관함/지갑/fee/등록/취소/Guard, 정책 허용 근거, 게임 scope 자동 발견, 같은 ID로 반환되지 않는 취소/스택, ID 없는 인과관계 확정, stale 재가격 target revision UI·live 확정 실패 retry, 실제 입력/Popup/모니터 DPI/설치·보안 감사.
- **실제 단일 승인 전체 판매등록의 제품 목표는 미완료**다. 합성 성공은 실판매 성공이 아니며 LIVE_READY/RELEASE_READY가 아니다. [최신 근거·지원 범위](STEAM-INTEGRATION-FEASIBILITY.md).

## 4차 상태 기록 (당시 기준)

## 4차 최신 상태: 실험적 HTTP 소스 구현 / 실계정 제품 흐름 미완성

구현·검증 커밋 `61fbd58` origin/main push 완료. 최종 기록 커밋은 완료 보고 참조.

- 일반 앱은 계속 Mock/IntegrationBlocked. 새 Infrastructure/Steam 코드는 App.OnStartup에 주입하지 않는다. 비공식 경로 소스 구현 허용과 Valve 허가·실계정 테스트 승인을 구분한다.
- 구현: session-only 위험별 옵트인, QR Begin/Poll/finalize/Community token, 자체 메모리 쿠키, 명시적 보관함·호가·중간가격 이력·목록 갱신, 직렬 실제 sellitem/removelisting 전송, immutable batch preflight, 별도 SQLite intent/delta results, 서버ID 대조·불명 복구. 기존 도메인 provider/executor/coordinator 계약을 재사용한다.
- 자동 검증: `scripts/Verify.ps1` 5개 프로젝트 빌드 경고0/오류0, 도메인/저장/MVVM/합성 Steam HTTP **87/87 PASS**, 비표시 WPF **17/17 PASS**, FAIL0. 새29개 계약 검사 포함. 이전58개 및16개 회귀 유지.
- 최신 `scripts/Publish.ps1` Release 실행파일 생성·PE/로컬 데이터 제외 PASS. 실행·설치·서명·공개 배포는 수행하지 않았다.
- 자체 `stage4-integration-status.png` 렌더를 검토해 좁은 안전 화면 오른쪽 여백을 수정하고 카드 경계 검사 PASS. 창/PresentationSource는 생성하지 않았다. 실제 입력·팝업·모니터 DPI는 미검증이다.
- 새 Steam 저널128행 delta 저장/재열기·1MiB 미만 payload, 두 번째 결과 쓰기 실패 전체 rollback, 미래 스키마 원본 보존·독점, auth marker 비저장 검사 PASS. 10,000개 합성 parser/UI 가상화 검사 PASS. 최대 규모/장시간/실네트워크 성능 인증 아님.
- 실제 네트워크 검증은 공개 자료와 무인증 orderbook 1회 HTTP200뿐이다. 인증/계정/판매/취소/Guard는 테스트 handler로만 처리했다. 실제 계정 로그인·등록·취소·승인 없음. 전체 화면/타 앱/전역 입력/프로세스 조사 없음.
- **미완성/차단:** QR/실계정 WPF 연결, 원화 등 USD 외 실등록·스택 분할, 전체 계정 거래 제한·보류 해석, 응답 listingid 없는 결과의 자동 확정, 확정 취소 이력·새 자산ID 매핑·실제 취소→재등록 saga, live 수동 retry, 다중 실배치 관리/보안·설치·서명 감사.
- Steam 중간가격 이력은 정확한 거래 VWAP가 아니다. live 제출은 USD 비스택 개별 자산과 exact fee만 허용하는 **실험적 계약**이며 실사용 검증을 뜻하지 않는다. 실저널은 일반 Demo 삭제 버튼과 분리했다. 현재 라이브 활성화 UI는 없다.
- 비공식 제재 위험은 인증 보안을 강화해도 사라지지 않는다. 상세 근거·라이선스·직접 관측 한계·개발자 옵트인 절차: [STEAM-INTEGRATION-FEASIBILITY.md](STEAM-INTEGRATION-FEASIBILITY.md). 실제 일괄 등록 제품 목표는 계속 미완료, LIVE_READY/RELEASE_READY가 아니다.

## 3차 상태 기록 (당시 기준)

**핵심 판매·관리 Mock UX와 비표시 렌더·복구 검증 완료. 실제 Steam 등록 기능 완료가 아니며 LIVE_INTEGRATION_BLOCKED를 유지한다.**

| 기능 | 상태 | 구현/검증 범위 |
|---|---|---|
| 최종 개별 가격 검토 | MOCK / PASS | 구매자 단가·수량·Steam/게임 수수료·실수령·합계 즉시 갱신 |
| 호가별 분배/부족 잔량 | MOCK / PASS | 공유 잔량, 미산정 수량 별도 빈 가격 행, 직접 입력 전 전체 제출 차단 |
| 수동 가격/전략/신선도 | MOCK / PASS | 열린 검토만 유효, 전략/닫기/갱신 초기화, 5분 신선도·최종 정합성 검사 |
| 단일 승인 전체 등록 | MOCK / PASS | 불변 스냅샷, 전체 선택·개별 결과, 실제 Steam 제출 아님 |
| 판매 목록 개별/전체 선택·취소 | MOCK / PASS | 별도 검토, 부분 취소 실패·불명·승인 대기 구분 |
| 판매 가격 변경 | MOCK / PASS | 취소 확인→새 가격 재등록, intent/부분 실패/Guard 대기 |
| 수동 재시도 | MOCK / PASS | 확정 실패만 검토, 시도 번호·새 요청 키, 기존 성공/Unknown 재전송 차단 |
| 재가격 중단 후 재개 | LIMITED / PASS | 취소 확인 및 후속 미전송 증거가 있을 때만 명시적 재개 |
| SQLite v2·진단 | MOCK / PASS | v1 보존 이관, quick_check, 변경 결과/판매 행 저장, 고정 코드 최대500개 |
| 대량 표시/배치 | MOCK / PASS | 10,000개 뷰, 반복 스크롤 선택 보존, 128행 실제 SQLite 배치 재열기 |
| Windows 실행파일 생성 | GENERATED / STATIC PASS | Release framework-dependent publish, PE/데이터 제외 검사, 실행/설치 안 함 |
| 실제 로그인·보관함·장터 조회 | Unavailable | 허용 경로/권한·실계정 검증 없음 |
| 실제 등록·취소·재등록 | LIVE_INTEGRATION_BLOCKED | 일반 개인용 쓰기 허용 근거 미확보 |

## 실제 검증 결과

- `scripts/Verify.ps1`: 5개 프로젝트 빌드 경고0/오류0, 도메인/SQLite/MVVM **58/58 PASS**, detached STA WPF **16/16 PASS**, FAIL 0.
- 새 관리 화면까지 포함한 바인딩 오류 검사, 수동 가격/부족 수량/전략 초기화/가격 변경·재시도/로딩·Mock 조회 실패 렌더 검사. 자체 PNG를 생성하고 직접 시각 검토했다.
- 1100×700~1600×900 DIP, 비트맵 출력 밀도 100/125/150/200%. 실제 모니터 DPI 전환 시험이 아니다. Application.Windows=0 및 PresentationSource=null 검사.
- 10,000개 카드 viewport 생성 9개→끝7개, 반복 스크롤 후 선택 보존. 마지막 fixture+복수 렌더 관측282ms는 비교 벤치마크/장기 안정성 인증이 아니다.
- 128행 Mock 배치를 실제 SQLite로 처리·재열기하고 헤더/결과 DB payload <1MB를 검사했다. 대량 전체 JSON 반복 쓰기를 변경 행 저장으로 교체했다.
- 실제 SQLite 손상 거절·스키마 이관 실패 rollback·복수 결과 변경 rollback·설정 보존. 프로젝트 headless 테스트 자식만 자기 Exit하여 intent 복구/미커밋 transaction rollback을 확인했다.
- `scripts/Publish.ps1`: 최신 Release 실행파일 생성/PE 헤더·출력 DB/로그/키 제외 PASS. 앱·설치 프로그램은 실행하지 않았다.

## Steam 조사

[STEAM-INTEGRATION-FEASIBILITY.md](STEAM-INTEGRATION-FEASIBILITY.md)에 공식 인증/API/약관/호출 제한·Support 확인 절차·오픈소스/라이선스·해제 조건을 기록했다. publisher/소유 앱/secure server 제한을 개인 계정의 쓰기 권한으로 사용할 수 없었다. 기술적 사례는 존재하지만 Valve 허가로 해석하지 않는다. 이번 조사로 실연동을 활성화하지 않았다.

## 안전 및 남은 제한

- 실제 최상위 앱 창·활성화·전역 입력·타 앱 조사/종료·전체 화면 캡처·실제 Steam 로그인/거래/Guard는 수행하지 않았다. Process.Start는 검증 대상인 프로젝트 headless 자식뿐이며 CreateNoWindow로 실행 후 자식이 자기 프로세스에서 종료했다.
- 실제 사용자 입력/Tab/휠/포커스, 열린 팝업, 모니터 DPI, 스크린리더, 클린 Windows 설치·업데이트/서명/배포, 전원 차단·장시간 장애는 NOT VERIFIED.
- 관리 요청이 Unknown이면 외부 증거 없이 성공/실패 확정·재전송하지 않는다. 현재 Mock에 근거 없는 불명 관리 요청을 수동 확정하는 도구는 없다. 목적을 알 수 없는 구형 실패/불명도 차단한다.
- 단일 활성 배치 및 고정 높이 카드 유지. 다중 배치 탐색·임의 높이/범위 선택/드래그·실제 온라인 페이징/요청 제한은 후속 작업이다.
- 재시도 가격은 원래 승인 가격/수량이며 새 검토·현재 자산/시세 확인을 요구한다. 실제 Steam 통화별 수수료·체결을 검증한 것이 아니다.
- 실제 단일 승인 실등록 목표는 미완료다. READ_ONLY_READY/LIVE_READY/RELEASE_READY가 아니다.

## Git

시작 `9ce53df`와 fetch한 origin/main 일치, 기존 미커밋 변경 없음. 조사 마일스톤 `db8a55c` push 완료. 3차 구현/검증 커밋 `1e6c280`도 origin/main push 완료. 최종 기록 동기화 커밋은 최종 보고에서 확인한다.
11차 최종 회귀: **기능177/177 PASS · 비표시 WPF39/39 PASS · 배포 파일 계약14/14 PASS · FAIL0 · Debug 경고0/오류0**. 기존172/33/14 전부 유지. `scripts/Verify.ps1`, ignored `runtime/stage11-verify.txt` 근거. 실제 서버/물리입력/Popup/설치·서명 미실행.

사용자 최소 재확인: (1) 새 ZIP 버전·커밋 확인 후 Steam 탭에서 명시적 QR 시작으로 QR 표시 여부만 확인(실거래 금지), 실패하면 표시된 단계와 검토한 진단ZIP 확인. QR 원문은 공유하지 않는다. (2) 진단 탭/최근 오류 체크를 클릭한 뒤 포인터를 밖으로 이동: 선택 밑줄·체크 표시만 유지되는지 확인. (3) Tab/Shift+Tab의 점선 포커스, 빠른 탭 전환, 비활성→활성, 드롭다운 열기·닫기를 직접 확인. Codex는 이 작업을 실행하지 않았다.
