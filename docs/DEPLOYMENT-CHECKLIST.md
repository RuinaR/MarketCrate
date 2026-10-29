# Windows 배포 준비 — 3차 구현

### 0.17.0 배포 확정 — 로그인 유지

구현 소스 **1700bfdc6cb3cf0df984e7c76b0a14fff8558372**, origin/main push. Windows x64 self-contained LiveOnly portable **artifacts/releases/MarketCrate-0.17.0-session-1700bfd-win-x64.zip**, **66,486,397바이트 /413파일**, SHA256 **A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC**. 원본 `artifacts/releases/session-1700bfd-windows-x64`, BuildVersion0.17.0+1700bfdc6cb3cf0df984e7c76b0a14fff8558372. 전체 entry SHA/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData DLL/steam-session MIT 전문/고지·인증/DB/로그/개발파일 제외 검사 PASS. PE/BAML은 Mock 부재와 실제 Shell/Steam/AuthStore/refresh-token 검증 코드 존재를 확인했다. 경고·오류0, 실제 EXE/설치/서명은 미실행이다.

최종 자동: 기능270/270·자체WPF55/55·배포계약19/19 PASS, 안전 fixture 정리8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 환경제약). 실제 Windows 자체 CurrentUser DPAPI/ACL은 합성 비밀로 검증했다. 실제 Steam 저장 토큰 재사용·서버 회전/철회·수수료/판매·물리GUI·다른 Windows 사용자 복호화·전원 차단 시험은 미실행/미검증이다. LIVE_VERIFIED를 부여하지 않는다. 최초75개 판정47/15/0/4/9 유지, 실거래는 매 실행 OFF다.

롤백0.16.1 ZIP/SHA **94F7E423D49AA0F0AF6C4651A50486F95548A93B45EAE850B488480C70012C2F** 보존(완전 실사용/실거래 검증이라는 의미 아님), 사용자 관측0.16.0 및 그 이전 배포본/설정/SQLite/저널/진단/운영 인증 자료도 보존했다. 최신/롤백은 ignored artifacts/releases.json에 기록했다. 사용자 자료 삭제0·운영 Auth 접근0·실제 계정/GUI/타 프로세스/전역 입력/화면/시스템 설정 접근0. 최소 사용자 확인은 SESSION-PERSISTENCE의 'QR 전 유지ON→인증/저장 확인→로그아웃 없이 종료/재실행→직접 읽기 확인→명시적 로그아웃' 순서다. 토큰/쿠키/Auth 폴더는 진단 전달 대상이 아니다.

## 0.17.0 로그인 유지 배포 조건

실제 전용 x64 self-contained 유지, Mock/BAML 테스트 주입 제외 검사 필수. ProtectedData10.0.9 DLL과 steam-session MIT LICENSE·원본 커밋/변경 고지를 포함한다. Auth 디렉터리/refresh.authbin/store.authlock/refresh.pending/restore.blocked가 있으면 패키징을 거부한다. 개인 DB/로그/암호문/토큰/QR/개발 파일은 포함하지 않는다. Auth는 사용자 LocalAppData 전용이며 앱 설치/업데이트 디렉터리에 쓰지 않는다.

SQLite 저널/설정 마이그레이션 변경 없음. 최신 후보 생성·전체 entry/소스 커밋 해시 검증 뒤0.16.1을 롤백으로 등록하고 이전 배포본/사용자 자료는 보존한다. 실제 실행·설치/실계정 복원·다른 Windows 사용자·서명/전원 차단을 정적 PASS로 대신하지 않는다. 최종 ZIP/해시는 현재 상태·작업 기록에 확정한다.

### 0.16.1 배포 확정 — 수수료 HTML 오분류 수정

소스 **f5ed123aff841b85fa9a6865fa5a1f4a886004a9**, origin/main push 완료. 최신 Windows x64 self-contained LiveOnly portable: `artifacts/releases/MarketCrate-0.16.1-fees-f5ed123-win-x64.zip` (**66,475,258바이트 /412파일**), SHA256 **94F7E423D49AA0F0AF6C4651A50486F95548A93B45EAE850B488480C70012C2F**. 원본 폴더 `artifacts/releases/fee-f5ed123-windows-x64`, BuildVersion0.16.1+f5ed123aff841b85fa9a6865fa5a1f4a886004a9. Release 빌드·실제 전용 metadata/BAML·CLR/WPF10.0.9/SQLite x64·라이선스·민감/개발파일 제외·전체ZIP entry SHA 일치 검사 PASS. 실제 EXE/설치/서명·실계정/거래 미실행. 초기 NuGet 감사 메타데이터의 sandbox 네트워크 실패는 정상 접근으로 재빌드하여 해소했으며 감사/경고 검사를 끄지 않았다.

최종 기능250/250·WPF52/52·배포계약14/14 PASS, 경고·오류0. 안전정리7 PASS/1 NOT VERIFIED 유지(실제 junction 환경제약), 사용자 산출물 삭제0. 롤백은0.16.0 ZIP/SHA1E758055EB23B5060EEE57BA6E819AEC323E2B698A5AC8518EF307FE48E9704A이며0.15.1 이전본·진단/DB/저널/설정/인증자료도 보존했다. 최신/롤백은 ignored artifacts/releases.json에 기록했다. 바이너리/진단/개인 DB는 Git에 넣지 않는다.

**이 배포는 실제 수수료 성공을 확인한 버전이 아니다.** 확인된 HTML=Challenge 오분류/안전 처리/원인 진단을 수정했다. 실제 사용자 HTML 종류와 계정의 newFeeRates 적용/수수료 계약은 새 버전에서 지갑·수수료만1회 사용자 승인 조회한 정제 ZIP으로 확인해야 한다. 미확인 금액·통화·수수료의 거래는 계속 차단, 제재 위험/LIVE_VERIFIED 미확정 유지. 최초75개 판정47/15/0/4/9 변경없음. 앱·타 프로세스/화면/전역 입력 접근0.

## 0.16.1 수수료 진단 수정 후보

실제 전용 x64 self-contained 구성 유지, Mock 서비스/모델/구화면/BAML·테스트 주입을 제외하는 필수 PE/BAML 검사를 수행한다. 가격·수수료 미검증일 때 전송 차단을 유지한다. 새 ZIP은 버전0.16.1·소스 커밋을 이름/메타데이터에 고정하고 새 artifacts 폴더에만 생성한다. 기존0.16.0을 롤백으로 보존하며 그 이전 ZIP/폴더·모든 사용자 진단/DB/인증/설정/저널은 삭제하지 않는다.

정적 검사와 실제 EXE 실행·설치/서명/Steam 실계정 성공은 구분한다. 이번 작업에서 앱/계정/거래 실행0. 최종 경로·SHA256·전체entry 검사는 배포 확정 기록 참조.

## 0.16.0 LiveOnly 배포

### 0.16.0 LiveOnly 배포 확정

소스 **85756b4f541175cbd388d51c3d3d2fb1349e84e7** origin/main push. 새 x64 self-contained Release: `artifacts/releases/MarketCrate-0.16.0-live-85756b4-win-x64.zip` (66,469,582바이트/412파일), SHA256 **1E758055EB23B5060EEE57BA6E819AEC323E2B698A5AC8518EF307FE48E9704A**. 전체 entry SHA/SourceCommit·CLR/WPF10.0.9·SQLite/라이선스/민감·개발파일 제외 검사 PASS. RuntimeMode=LiveOnly, 필수 PE/BAML 검사로 Mock 구현/모델/구 화면·재시도/구 USD fixture 파서가 없는지 확인했다. 경고·오류0. 실제 EXE/설치·실계정/거래·서명은 미실행이다.

최신/롤백은 ignored artifacts/releases.json에 기록, rollback0.15.1의 SHA106E89E7C801250C1C264F92DDC2BE87710A70CD3591BD09B38ADDFD9BE820F2 재확인. 0.15.0/0.14/0.13과 기존 추출본·사용자 진단 ZIP/DB/저널/설정/인증·NuGet 캐시는 삭제하지 않았다. 실제 지갑의 성공/금액 계약은 사용자 확인이 필요하며 실패를 합성 성공으로 바꾸지 않는다. 최종 기능235/235·자체WPF50/50·배포계약14/14 PASS(개발 구성 회귀), Release 별도 빌드/메타데이터/전체ZIP 무결성 검사 PASS. 마지막 Shell 페이지 진단 이벤트 연결은 Release 빌드에서 확인했다.

Release 기본 MarketCrateProduction=true, 개발/테스트는 false이다. Directory.Build.targets의 조건부 심볼로 Core/Infrastructure/App의 Mock 구현과 legacy MainView BAML을 제외한다. 테스트 프로젝트의 ProjectReference는 명시적 false여서 기존 회귀를 유지한다.

Publish 및 Publish-SelfContained는 배포검사에 더해 Test-ProductionAssembly를 필수 실행한다. PEReader로 타입·메서드·BAML 리소스만 읽으며 앱/네이티브 DLL을 실행하지 않는다. Mock 구현/모델/저장소/재시도·구 USD fixture 파서가 있거나 실제 Shell/서비스가 없으면 실패한다. distribution.json RuntimeMode=LiveOnly를 기록한다. LiveOnly 패키지를 오프라인 Mock QA 패키지로 감싸는 작업도 거부한다.

기존 Demo 데이터는 읽거나 제거하지 않는다. 실제 SteamExperimental 저널 위치는 유지하고 새 비밀정보 없는 설정만 LOCALAPPDATA/MarketCrate/Preferences/preferences.sqlite3에 저장한다. 업데이트/롤백 시 파일을 초기화하지 않는다. 소스 테스트·로그·DB·인증·개발파일은 ZIP에서 제외한다.

## 0.15.0 배포 정책

0.15.1 배포: 소스 d2ee9a4679e24854102f09355ff2d5f125f8c05a, `artifacts/releases/MarketCrate-0.15.1-wallet-d2ee9a4-win-x64.zip` (66,507,841바이트/412파일). SHA256 `106E89E7C801250C1C264F92DDC2BE87710A70CD3591BD09B38ADDFD9BE820F2`. Release/전체entry SHA/SourceCommit·CLR/WPF/SQLite·민감/개발파일 제외검사 PASS, 경고오류0. 실제EXE/지갑/계정은 미실행이며 해결완료 판정 아님. 0.15.0을rollback으로등록하고0.14/0.13·기존추출본/진단ZIP·DB/저널/인증/설정은보존했다.

### 0.15.0 배포 확정

소스 **eb9ebd7ff79585dfbcd20ba17374eb4c4e155031** origin/main push. portable **artifacts/releases/MarketCrate-0.15.0-stage15-eb9ebd7-win-x64.zip** (66,506,796바이트/412파일), SHA256 **EF520920413636DC1456C30A3C94F6ADAD03C08D0082156ED0544983520F43D5**. 전체 ZIP entry SHA와 원본/SourceCommit 일치, x64 CLR/WPF10.0.9·SQLite 네이티브·라이선스 포함, DB/로그/인증정보/개발파일 제외, Release 경고·오류0. 실제 EXE·설치·실계정은 미실행, 디지털서명 미적용.

최신/롤백은 ignored artifacts/releases.json에 기록. 롤백은0.14 ZIP(SHA E1A001291F4AE0F6BE0C5410FA51421BD95E52612472BD9C187890CBED034680), 기존0.13 ZIP·모든추출본/진단/SQLite/저널/설정/인증/캐시는 그대로 보존했다. 이번 차수의 기존산출물 삭제0(성공한 신규합성fixture 자체정리만). 마지막 생성자 권한검사를 DB열기 앞으로 옮겨 차단시 lease잔류를 막고 활성화재요청은 먼저 권한철회하도록 한 뒤 전체기능223/223 재검사(runtime/stage15-last-core.txt), 기존48WPF/14배포검사와 새Release검사 유지. 동의해제·재로그인/계정변경 활성화보존 없음.

새고유버전/커밋폴더에 x64 self-contained publish, 전체ZIP entry hash/SourceCommit/아키텍처/CLR·WPF·SQLite·고지/개발·민감파일 제외검사. 실제창·설치·서명·실계정은 미실행. 0.14 최신본과0.13 이전사용자관측본/추출폴더·진단·저널·설정·계정파일 보존. 새latest index의rollback은0.14이며 구0.13도삭제하지 않는다. 정리는 기존 exact합성fixture dry-run 규칙외 추가없음. optional snapshot/reprice digest는 기존SQLite v3 자료를 초기화하지 않으며 구불명요청을 재실행하지 않는다.

## 0.14.0 배포·정리·롤백

14차 배포 구현커밋 3e5201cd5980a4bf36fb25e7dbb6f7cb5e81960f origin/main push. 새 `artifacts/releases/MarketCrate-0.14.0-stage14-3e5201c-win-x64.zip`:66,498,491바이트/412파일, SHA256 E1A001291F4AE0F6BE0C5410FA51421BD95E52612472BD9C187890CBED034680. 원본전체entry SHA·SourceCommit·CLR/WPF10.0.9/SQLite x64/라이선스/민감·개발파일 제외 PASS, Release경고오류0. 실제EXE/설치·실계정 미실행/서명미적용. 최신·롤백은 artifacts/releases.json.

검증된 새release 등록 후 dry-run11폴더/242파일(45,650바이트) 재검증·제거, skipped0. exact256byte 합성PE/고지fixture만 대상. 큰 실행폴더는 사용중/사용자내용 불확실하여 보존. 원본0.13 ZIP SHA256 2B76B0ACB3440CCBA5F2C2364AAD46BA458D10E89E329A8456400A0ED83BD7C8 재확인. 추출본/진단/SQLite/저널/설정/인증/캐시/LOCALAPPDATA 보존. ignored 증거 artifacts/cleanup-plan-f8ca226ee39e4e5aa6149f38d3fb6690.json 및 cleanup-result-755a1b5fc521421bb440139a33344cb5.json.

새 x64 self-contained는 고유버전/커밋 경로에 생성한다. 기존 CLR/WPF/SQLite 아키텍처·라이선스·민감/개발파일 제외 검사 유지. ZIP 전체entry SHA를 원본과 대조한 후 Register-Release.ps1로 artifacts/releases.json Latest/rollback 등록. 사용자자료는 기존 사용자별 저장소 보존. EXE/설치실행·디지털서명은 별도 미검증이다.

Clean-Artifacts.ps1 기본dry-run은 후보·크기·참조수·보존이유를 ignored artifacts/cleanup-plan-*.json으로 남긴다. -Apply -PlanPath <절대경로>는 현재버전 검증Latest·rollback SHA 일치 이후에만 실행한다. 자동삭제는 exact hash256byte 가짜PE/합성고지22파일의 runtime/distribution-tests-<guid>만 허용. 경로정규화/reparse경계/파일잠금/변조 재검증, 파일별삭제 후 빈폴더만 비재귀제거. unknown/locked는 보존. 실제junction 생성은 환경제약으로 미검증.

실행가능폴더·진단/DB/계정자료·NuGet캐시·프로젝트외부/LOCALAPPDATA는 대상이 아니다. 0.13 ZIP/추출본은 부분실사용 관측 롤백본이며 기능전체 정상인증본이라는 뜻은 아니다. 과거문서 배포경로는 이력으로 보존한다. 성공한 신규합성fixture는 자체정리하고 실패출력은 증거로 보존한다.

## 5차 추가 범위 (최신)

- 일반 앱에 지연 연결 UI를 포함한다. startup/탭 생성은 Steam HTTP나 실저널을 열지 않으며 실제 거래 policy는 기본 Blocked다. 합성 테스트 이외의 인증/계정 시험을 하지 않는다.
- Steam 전용 DB v1→v2 보존, audit/반환/재가격/배치 archive를 지원한다. Demo DB v2와 별개이며 실저널을 Demo 데이터 삭제에 연결하지 않는다. 실행파일/업데이트 버전과 데이터 폴더를 분리한다.
- MIT QR 소스를 고정 버전으로 포함하고 원 LICENSE/저작권 고지를 보존한다. publish에 `licenses/QrCodeGenerator-LICENSE.txt`와 THIRD-PARTY-NOTICES를 복사하고 누락 검사를 추가했다. 신규 패키지/SDK 설치 없음.
- Release 산출물 생성/정적 검사는 실제 설치/계정 연결/라이브 거래 검증이 아니다. 새 기기/모바일 QR 스캔/모니터 DPI/서명·설치·보안 감사는 미검증이다.

## 4차 추가 범위

- 실험적 Steam 소스가 포함되지만 App startup은 계속 Demo이며 연결/토큰/실저널 생성이 자동 실행되지 않는다. 새 패키지/외부 소스 편입 없음.
- 별도 실저널은 실행파일/버전과 분리된 전용 디렉터리에 두도록 composition 계약을 제공하며 Demo 삭제 UI에 연결하지 않았다. 인증정보 persistence 없음. 일반 사용자용 실계정 활성화 UI/업데이트/설치·보안 감사를 완료한 것이 아니다.

현재는 Mock 빌드 산출물 생성/정적 검사이며 정식 배포나 실판매 지원 완료가 아니다.

## 확인한 구조

- `scripts/Publish.ps1`: 설치된 SDK/캐시를 이용해 Release framework-dependent WinExe 생성, 단일 MSBuild 작업, 추가 다운로드 없음.
- 출력: `artifacts/windows-framework-dependent/MarketCrate.App.exe`, 앱/의존 DLL·런타임 설정·SQLite native 자산. Git 제외. PE MZ 헤더와 DB/로그/인증서 파일 제외 검사.
- 실행에는 .NET 10 Windows Desktop Runtime이 필요하다. self-contained/설치 프로그램/자동 업데이트는 구현하지 않았다.
- 데이터 경로 `%LOCALAPPDATA%/MarketCrate/Demo`는 실행파일 위치/버전과 분리되어 있다. 배포 스크립트는 이 경로를 읽거나 삭제하지 않는다. 테스트는 workspace/runtime의 격리 DB만 사용한다.
- SQLite v1→v2에서 설정/레코드를 보존한다. 실패 시 기존 데이터/버전을 유지하며 더 높은 버전은 자동 초기화하지 않는다. 구형 앱으로 DB를 다운그레이드하면 안 된다.
- 실제 Window/App 실행·포커스·입력·Steam 연결을 하지 않는다. 테스트 프로세스 종료 검증은 이 저장소의 headless 테스트 DLL만 CreateNoWindow로 실행하고 자기 프로세스에서 Exit한다. 타 앱을 찾거나 종료하지 않는다.

## 수행한 검증

- [x] Release publish·PE 헤더·출력의 로컬 DB/로그/키 파일 제외.
- [x] 실제 SQLite 파일 손상 거절·v1/v2 이관·실패 rollback·설정 보존.
- [x] 소유 headless 자식이 intent 저장 후 Dispose 없이 종료했을 때 Unknown 복구·재전송 0.
- [x] 소유 headless 자식이 미커밋 SQLite transaction 상태로 종료했을 때 이전 값 복구.
- [x] 고정 스키마 진단·유한 보존. 예외 메시지/인증정보/아이템 이름을 진단 로그 API가 받지 않음.
- [ ] 다른 Windows PC/클린 계정의 설치·실제 실행·삭제·업데이트.
- [ ] 코드 서명/SmartScreen/백신 호환성·종속성 취약점·배포 라이선스 원문 감사.
- [ ] 실제 모니터 DPI·입력·팝업·접근성/스크린리더.
- [ ] 실제 전원 차단·하드웨어/파일시스템 장애·장시간 안정성.

테스트 자식의 의도적인 Exit는 OS 강제 전원 차단이나 실제 GUI 프로세스의 장기 안정성을 재현한 것이 아니다. 산출물 생성/테스트 성공을 RELEASE_READY로 확대 해석하지 않는다.
