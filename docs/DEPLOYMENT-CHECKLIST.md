# Windows 배포 준비 — 3차 구현

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
