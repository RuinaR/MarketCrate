# 정상·오류·복구 QA 매트릭스 — 10차 (2026-10-28 KST)

## 12차 — 실제 사용자 발견 결함 재현과 수정

12차 배포: 구현 `49a3ffad9a8b37aa4085d6ace6191ddbd035430e` origin/main push. **0.12.0 Windows x64 self-contained Release PASS**, 경고·오류0, CLR/WPF10.0.9·SQLite 네이티브/고지 포함, DB·로그·세션·PDB·개발파일 제외. `artifacts/MarketCrate-0.12.0-stage12-49a3ffa-win-x64.zip` **78.30MiB/412파일**, ZIP 전체 파일 SHA256가 원본과 일치. SHA256 `EAE969B83190F2EB9A29F7299AD2B879DC7D699502D631E960A6F10308986408`. ProductVersion/SourceCommit 일치. 앱/인증/실거래/설치·서명은 실행하지 않았고 기존 배포본·사용자 데이터는 보존했다.

사용자 최소 확인:① 보관함/판매/진단 탭의 콘텐츠 버튼·체크·입력·드롭다운 위를 이동해 Header 깜빡임이 없는지, Tab/Shift+Tab 점선과 선택 밑줄이 구분되는지 확인.② 직접 QR 인증을 선택한 경우 인증 완료 카드와 개별 조회 상태를 확인.③ 보관함 파싱/404 실패 시 인증 완료가 유지되고 독립 목록/fee 버튼이 동작하는지 확인(401이면 재로그인,403/429/CAPTCHA면 차단이 정상).④ 게임 전환·선택/수량·스크롤·가격 검토를 확인하되 실등록·취소·재가격은 실행하지 않는다. 실패가 남으면 고급 설정의 작업 ID→진단 검토 ZIP으로 원인을 좁히고 QR/원문 응답은 공유하지 않는다.

12차 최종 회귀: **기능187/187 PASS · 비표시 WPF42/42 PASS · 배포 파일 계약14/14 PASS · FAIL0 · Debug 경고0/오류0** (`scripts/Verify.ps1`, ignored runtime/stage12-verified.txt). 기존177/39/14 검사와 거래/Unknown/SQLite 회귀 유지. 사용자 제보의 실제 Steam 보관함 실패 응답은 미확보이므로 해당 실서버 파서 계약이 해결됐다고 보장하지 않는다. 수정한 것은 실패의 범위·인증 소실·UI 동작이며 실계정 파서/응답 형식 검증은 별도다. 실제 앱/입력/Popup/물리 DPI/Steam 인증·거래·설치 미실행.

| 경로 | 증거/결과 | 한계 |
|---|---|---|
| 수정 전 인증 소실 | T12 초기 5개 중 파서3종/503 총4 FAIL(178/182 PASS), 404 독립 경로는 기존 PASS | 사용자 실응답을 재현한 것이 아니라 합성 최소 재현 |
| 탭 Header | U12 초기 Header 외부 TabItem.IsMouseOver가 선택 Header 배경을 바꿔 FAIL; SourceName=B로 변경 후 PASS | 물리 포인터/캡처·모니터 미실행 |
| 인증·읽기 분리 | TwelfthStageTests.ReadFailure: inventory/fee/listings parse/404/503 후 인증 보존, 독립 조회 버튼 활성과 성공, 실등록0 | HTTP handler 대체·실서버 아님 |
| 서버 제한 | Restrictions:401 세션 만료/재로그인,403/429 세션 존재와 요청 차단 분리, 직접 메서드 재호출에도 요청1회 | 실제 제한 유발·우회 없음 |
| 부분 성공/재시도 | PartialLoad: inventory Ready + fee Failed + listings Ready, 수동 fee 재조회 복구, 게임 선택·수량 보존 | 자동 네트워크 재시도 없음 |
| 네트워크/이력 | ReadNetwork GET timeout/transport 요청1회·독립 목록 성공. HistoryFailure parse/network를 History 실패로 연결하고 오래된 검토 차단 | 실제 네트워크 설정 변경 없음 |
| 로그 | Diagnostics: 같은 OperationId의 ReadState4203→HTTP Correlation→ResponseParsed/Parsing, AuthState ContractFailure로 잘못 전이 없음, 원문 sentinel 부재 | 원문 응답/비밀 덤프 없음 |
| UI | U12 Header/하위4종 Hover·Selected·포커스 구분,3열 배치/고급필드 이동/실제 명령 연결/100개 표시/게임·페이지 스크롤 및 선택 보존/모달 배경 차단 | 실제 Window/Popup/입력 없음 |
| 자체 렌더 | runtime/ui-tests/stage12-*.png: account/inventory/listings/advanced/review.1100×700,1380×860,1600×900 및1/1.25/1.5/2 출력 밀도 직접 검토 | 출력 밀도는 물리 모니터 DPI 인증이 아님 |

회귀 중 기존 DiagnosticUi가 exports 폴더 생성·명령 busy만 보고 ZIP 완성 전에 단언하는 경쟁을 확인했다. 저장 완료 Status와 실제 ZIP 존재를 함께 기다리도록 수정했고 합격 조건(검토 동의/필터/1개 ZIP)은 유지했다. 기존 UI 검사의 DockPanel 강제 cast는 FrameworkElement로 바꾸고, 고급 기능 이동은 새 고급 페이지에서 같은 명령/정제 요약을 검증한다. 탭 Hover 검사는 새 소유자인 Header B에 주입하고 콘텐츠 Hover 불변 검사를 별도로 추가했다.

## 11차 — 사용자 결함 재현 및 회귀 (2026-10-28 KST)

11차 배포: 구현 커밋 `0705dc2874b64b521e86dd92b3847c1a1b63219c` origin/main push 완료. **0.11.0** self-contained Windows x64 Release 빌드·정적 검사 PASS, 출력 경고/오류0. CLR/WPF10.0.9, x64 apphost/SQLite 네이티브 DLL/라이선스 고지 포함, DB·로그·인증·PDB·개발파일 제외. `artifacts/MarketCrate-0.11.0-stage11-0705dc2-win-x64.zip` **78.29MiB/412파일**, ZIP 전 항목과 원본 SHA256 일치. SHA256 `C3A5AEC49ACB4B8FEA93D4496E49E4D7AB92F2FB7F409F5EA22F1E43A60AEE28`. distribution.json 및 ProductVersion=0.11.0+0705dc2…의 SourceCommit 확인. 실제 EXE/설치/서명/실서버 QR 발급은 미실행. 이전 배포본·사용자별 저장소를 덮어쓰거나 초기화하지 않았다.

| 경로 | 수정 전 증거 → 수정 및 합성/비표시 검증 |
|---|---|
| QR 요청 | 독립 decoder 정상계약 테스트 FAIL(최상위1 누락); API 메타데이터에서 최상위1/2 required 확인. EleventhStageTests.RequestContract: field번호/wiretype/상위·내부 구분/WebBrowser2/base64 form/format·Accept/인증완료 안 함 |
| QR JSON | 숫자 uint64 client_id 합성 응답 FAIL → NumericResponse 정확한 ulong 파싱/0.5초 interval PASS. float 변환으로 정밀도 손실 안 함 |
| 실패 단계/민감정보 | ResponseFailures/ApiFailures: malformed JSON, 누락, 잘못된 base64, uint64 overflow, 잘못된 host, interval0, 200+x-eresult8/401/403/429. 각 단계·원인·OperationId·세션정리·요청1회·secret sentinel 부재 검증 |
| 취소/갱신/만료 | Cancellation: 사전취소 전송0/로그아웃 QR 제거. 기존 T10.QrRotation 및 5차 로그인 만료·거절·로그아웃·늦은 응답 회귀 유지 |
| UI 상태 소유 | 이전 ActionTemplate VSM/Trigger 중복 검사 FAIL, DP 왕복 검사는 PASS. 실제 물리 ghost-hover 재현 아님. VSM 단일 소유+별도 FocusRing으로 수정 |
| 상태 전이 | U11: Button/Toggle/Repeat VSM Normal/Hover/Pressed/Focus/Disabled 왕복; Check/Radio DP Checked/Hover/Pressed/Focus/Disabled; Tab/List/ComboItem/Menu 선택·focus·hover 재활용; Text/Search/Combo/Stepper focus/modality; Thumb hover/drag/disabled. 각각8회, Background/BorderBrush/BorderThickness/Opacity/현재 VSM·ring·mark 확인 |
| 자체 렌더 | runtime/ui-tests/control-states.png 및 stage11-*.png. RenderTargetBitmap만 생성, 실제 Window/Popup/PresentationSource 없음. 실제 입력·Popup·물리 모니터 DPI NOT VERIFIED |

초기 재현: 기능172/174 PASS·2FAIL, UI34PASS·1FAIL(구조 중복). 테스트 개발 중 detached CLR 상태 getter와 DP 상태 주입의 차이로 버튼 테스트가 실패했다. 이를 실제 입력으로 해석하지 않고 공개 VSM API 전이를 별도 구동하며 Trigger는 DP로 검사하도록 분리했다. 합격 기준의 시각 속성/복원 단언은 유지했다.

## 10차 검증 및 사용자 증거

사용자 직접 보고의 9차 기본7항목: **7 PASS/0 FAIL**. 실제 Windows에서 실행/hover·click/보관함선택·수량/가격전환·검토/Mock배치/취소·재가격·진단/재시작 보존을 묶은7개다. 개별 테스트시각·빌드hash·OS·DPI 미제공. 상세 GUI-01~24 전항목 PASS로 변환하지 않으며, 10차 코드의 물리 GUI·읽기연동 결과로도 사용하지 않는다.

| 10차 결함/경로 | 수정과 검증 |
|---|---|
| 한국어 이름이 전략 로직/저장값 | PriceDisplay alias 경계/PriceBasis 내부판단/안정 enum key. T10.Terms 저장·재시작·수동전략/단가 차이 검증 |
| QR 회전 후 옛 matrix | auth Challenge→UpdateChallenge→새 QrMatrix. T10.QrRotation 기본빈값/회전/만료/재로그인, 연결중 Begin 비활성 |
| inventory/fee 실패가 listings를 막음 | 독립 LoadListings/LoadFees와 fee없는 취소검토. T10.IndependentListings inventory404 후 정상 목록/차이·Submit차단/거래0 |
| 인증만료 후 이전 데이터 | T10.ExpiredReads401→Expired/이전row보존/새검토불가/logout정리. 서버자료를Mock으로 대체 안 함 |
| 계약 진단 | T10.ReadDiagnostics UI operation→HTTP correlation→ReadContract endpoint/count·Synthetic; 비밀sentinel/계정·hash원문누출0 |
| 공식 대체 버튼 | T10.OfficialPage 고정URL delegate/연결0/실행0/쓰기0. 실제 브라우저 미실행 |
| 사용자 명칭/진입/검토 | U10.terms/connected: 가격명·툴팁·독립명령·합성계약배너·검토명/중복상태숨김·자체PNG |

최종 기능172/172·detached WPF33/33·파일계약14/14 PASS, FAIL0/Debug 경고0·오류0. 구현 `e5db266` origin/main push 완료. 해당 소스 self-contained x64 Release PASS(경고0/오류0), CLR/WPF10.0.9·SQLite/고지/데이터·PDB 제외/Core commit 확인. `artifacts/MarketCrate-stage10-e5db266-win-x64.zip` 78.3MiB/412개 원본·ZIP hash 일치, 앱 미실행. SHA256 E14B0BA88D314276F485AEB2D8E97A64604B02F7EA7D5A77207C4DC122EC765F. 기존166개 기능/31개 UI/14개 파일 검사를 유지한다. 새 U10 첫 검사에서 같은 전략ComboBox가2곳인 정상구조를 Single로 가정해 FAIL1; 두 컨트롤 모두 같은명/선택/설명을 단언하도록 고쳤다. 기본 cache 경로 미지정 build의 NU1101은 기존 Verify 환경변수로 해소했고 패키지/SDK 설치·우회 없음.

## 9차 결과 (2026-10-28 KST)

**독립 guest 없음 / 실제 GUI 실행 없음. GUI-01~24: PASS0, FAIL0, NOT VERIFIED24.** 호스트 대체 GUI/입력/캡처/VM 실행 없음. 75개 판정47/15/0/4/9 유지.

| 경로 | 증거/결과 | 한계 |
|---|---|---|
| 기존 회귀+ZIP 결함 | 기능166/166 PASS(기존165+재현1), Utc 순서·operation ID·Validation 원인·로컬2-entry ZIP/무DB | 합성 로그/격리 저장소만 |
| detached WPF 기존31 | 31/31 PASS, 예상 밖 binding 오류0/최상위 Window·PresentationSource0 | 열린 Popup/실제 focus/물리 DPI 대체 아님 |
| 배포 파일 오류 주입 | 14/14 PASS: x86 apphost/WPF/SQLite, FDD위장, 고지 누락, DB/sidecar/authJSON/JSONL/.env/key/PDB 차단 | fake PE HEADER/파일 검사, 실행 호환성 아님 |
| self-contained | 구현 `d19dcc9` origin/main push 완료. 해당 소스의 SCD/FDD Release 모두 PASS(경고0/오류0), x64 apphost/CLR·WPF·SQLite(모드별)·runtimeconfig·고지·DB/로그 제외 정적 확인. SCD CLR/WPF10.0.9·PDB0, guest bundle414개 해시/142.02MiB; FDD40개 해시/33.51MiB. 두 manifest SourceCommit/PackagingCommit/Core metadata 일치, GUI0/0/24. 앱/guest/설치·서명 미실행 | 실제 실행/설치/서명 미검증 |
| guest 하네스 | SCD분기/동일build 재시작/사용자 결과경로 코드·AST 검사 | 실제 guest launcher 미실행; 모든 case NV |
| OS 가능 조건 | registry Core/build26200.9711/X64, Sandbox exe 없음; 공식Home 미지원 | CPU 가상화/SLAT/CIM은 권한으로 미확인. 호스트 변경 없음 |

실패→수정 증거: `runtime/stage9-before-fix.txt` 신규 ZIP 검사 FAIL1/기존165 PASS → `stage9-final-verify.txt`166/166. 최초 self-publish는 offline NuGet source로 NU1101 → self 명령에만 공식 source 추가. 복원 후 publish의 runtime license 누락을 정적 FAIL로 검출 → 원본 runtime pack license3개 복사. 파일 검사 .env case의 최초 FAIL1은 PowerShell 동적 변수 scope로 fixture 파일명이 바뀐 테스트 결함이었고 변수 분리 후14/14. 실패를 실행 환경 성공으로 축소하지 않는다.

## 8차 검증 범위 (2026-10-28 KST)

**실제 격리 게스트 미확보 / 실제 GUI 미실행.** 호스트 Window/Popup/입력으로 대체하지 않는다. 자동 결과는 합성 HTTP·자체 SQLite·detached STA만 뜻한다. 전체 기능/합성 HTTP/SQLite/진단 165/165 PASS, 비표시 WPF 31/31 PASS, FAIL0, Debug 경고0/오류0. 구현 `f672120` origin/main push 완료. 해당 커밋 Release publish PASS(경고0/오류0), apphost/SQLite x64 PE·Desktop Runtime10 의존성·데이터 제외·라이선스 고지·Core Git metadata 확인. 격리 QA package39개 SHA256 전부 일치, 24개 결과는 NOT VERIFIED. 실제 앱/guest/설치·서명 미실행.

| 경로 | 단언/증거 | 범위 |
|---|---|---|
| 게스트 profile/오류 args | 게스트 확인 누락·다른 args 거부, 운영 데이터와 별도 경로 | E8.Profile, 실제 프로세스 launch 아님 |
| offline Steam 경로 | UI CanExecute=false, 직접 BeginAsync도 factory 호출0/연결0 | E8.Offline/U8.offline |
| QR 선택 기본값/갱신 | 새ID0/빈URL 유지, 새ID124·새URL 반영 후 다음 protobuf ID124 | E8.QrDefaults, 공개 proto 기반 합성 |
| QR URL/추가필드/취소 | session 정리/스키마 차단, 원문누출0, 작업ID+AuthPoll+Schema; 취소를 Schema로 오인하지 않음 | E8.QrInvalid/QrCancel |
| 보관함 취소 | Connected 유지/Busy 해제/HttpFailure.Cancelled+동일 OperationId/거래0 | E8.ReadCancel |
| 모바일 승인 flags | bool/0·1 해석, 문자열/null/다른 숫자·객체 거부 | E8.Confirmation |
| Steam 검토 열고 닫기 | 배경 실제 IsEnabled false→true, scope/Tab/CtrlTab/자체 렌더·동일 operation Modal logs | U8.modal, focus 실제 이동 아님 |
| ComboBox/stepper 접근성 | 커스텀 trigger 존재/managed Name·Role, disabled RangeValue setter 거부/수량 불변 | U8.peers, OS UIA/스크린리더 아님 |
| x64/런타임/게스트 패키지 | apphost/nativeSQLite PE machine/Runtime dependency/manifest hash/데이터 제외/24 NV 시나리오 | 정적 검사, 실행·설치 아님 |

초기 신규 detached ComboBox 검사는 standalone fixture에 Style 미적용으로 FAIL1. 기존 앱 경로의 바인딩 문제와 구분해 fixture에 커스텀 Style을 명시하고 관련 UI 검사를 재실행했다. 최종 결과는 위 마감 기록에 반영한다. 기존158개와 UI28개를 삭제/완화하지 않았다. 장시간/native memory/자원 누수·실제 Popup/물리 DPI는 이번 단기 검사로 PASS 처리하지 않는다.

### 7차 자동 검증 마감

- 전체 기능/합성 HTTP/SQLite/진단 **158/158 PASS, FAIL0** (기존139+신규19), 비표시 WPF **28/28 PASS, FAIL0** (기존23+신규5), Debug 빌드 경고0/오류0. 마지막 기능 보강 후 전체 회귀 실행. 예상치 못한 binding 오류0/Window·PresentationSource0.
- 실제 PNG 디코딩·재활용60회·전송 중복/취소·재시도/실패 상태와 보관함/판매/검토/추정/진단 이동 이미지를 직접 검사했다. NumericStepper 36px/텍스트 줄바꿈/출처/기준별 부족 수량 안내를 수정·재확인했다.
- 단기 합성 예: 이미지100회 약2ms wall/31ms 자체 process CPU/managed allocation 약1.36MB, LRU4개/280B; 썸네일60회 약1835ms wall/1406ms 자체 CPU/약0.89MB managed allocation, cache210B. 자체 process만 읽었고 CPU 계수 해상도/다른 자체 스레드가 포함된다. 실이미지/네이티브 메모리/장시간 누수/FPS/다른 앱 영향 측정으로 확대하지 않는다.
- 실제 Steam 이미지/인증/보관함/가격/fee/판매/취소/Guard 및 실제 창/물리 입력/모니터 DPI/installer/VM 실행은 NOT VERIFIED. 소스/배포 검사 수정 커밋 `6828294` Release publish PASS(경고0/오류0): PE·로컬 데이터 제외·라이선스 고지 확인, Core DLL의 해당 Git commit metadata 정적 확인. 실행파일·설치 미실행. 구현 `e40492d`, 배포 검사 수정 `6828294`는 origin/main push 완료.

## 7차 검증 범위 (6차 139/23 회귀 유지)

75개 전 행을 같은 ID로 재평가했다. 7차 최종 실행 항목은 기능 **158개(기존139+신규19)**, 비표시 WPF **28개(기존23+신규5)**이며 위 마감 기록에 실제 결과를 반영했다. 아래 이전 6차 기록은 당시 범위다. 실제 Steam 응답/계정·실제 창/Popup/물리 입력/모니터 DPI·장시간 누수는 이번에도 NOT VERIFIED다.

| 7차 경로/오류 | 실제 단언/연결 | 테스트 |
|---|---|---|
| 이미지 join/icon 우선순위/누락/잘못된 class·instance | 올바른 side map, description mismatch 거부, URL 필드 invalid 처리 | S7.Metadata |
| CDN/token/경로/픽셀 | 단일 host/경로, URL/query/traversal/긴 token 거부, 512px 초과/비이미지 거부 | S7.ImageSecurity |
| 중복/구독별 취소 | 요청1, 다른 구독 유지, 마지막 구독 취소 후 네트워크 중단/캐시 미보존/복구 | S7.ImageDedup/ImageCancel |
| 리디렉션/403/429/5xx/MIME/큰 body | 우회·자동 retry 없음, 403/429 latch 후 새 요청0, 사용자 명령 후 일반 실패 복구 | S7.ImageFaults |
| 메모리 상한 | LRU4/1024B 설정에서100개 고유 이미지, cache 상한/재사용/eviction 단언. wall/자체 CPU/managed allocation 측정 | S7.ImageMemory (실제 이미지/장시간 아님) |
| 실제 WPF decoder/재활용/늦은 응답 | 유효 PNG→frozen pixels, 60회 source 교체/unload 후 Source null, 이전 응답이 새 그림을 덮지 않음, 실패→재시도 | U7 thumbnail/recycling |
| 보관함/게임 대표/판매/검토 이미지 | metadata→VM→template→cache→worker decode→UI, 동일 token 전송1/쿠키·권한·referrer 없음 | U7 integrated image render |
| custom AppID/context/계정 저장·제거 | context6 실제 합성 요청, 왕복 선택/수량 보존, account catalog 복원/기존 journal 유지 | S7.Catalog/JournalCompatibility |
| 404/빈 목록/403/잘못된 입력 | Unsupported/Empty/PrivateOrDenied 구분, 403 원인을 비공개로 확정 안 함, 로컬 오류가 로그인 상태를 만료시키지 않음 | S7.CatalogStates/CatalogInvalid |
| median/window/통화 | lower weighted median / 기간 필터 / 구간·거래량 / KRW 소수 거부 / USD 정확 단위 / 중복·미래 표본 거부 | S7.Indicators |
| 모드 선택/기준 변경/평균 없음 | 수동값/검토 초기화, 정확 평균 검토 불가, late history/로그아웃 후 UI 상태 오염 없음 | S7.UnavailableAverage/LateHistory |
| 추정 검토→승인→Guard→재시작 조회 | Evidence 불변/단일 배치 전송/Listed 대조/전송 횟수1 유지 | S7.EstimateFlow; MedianContractGate(기본 false/전송0) |
| history 부족/표본 변조 | empty 또는 동일 median/count/volume의 다른 표본 digest면 전송0/저널 미생성 | S7.ChangedHistory/StricterContracts |
| fee/stack/불명 | 누락 publisher fee/서버 fee conflict 거부, Unknown 예약/다음 행 Ready, proof 없는 revision 거부 | S7.FeeAndReturn/StricterContracts + 기존 F5/S6 모든 취소·스택·재시작 |
| 로그·진단 이동 | 요청/이미지 결과 operation 연결/토큰 부재, 현재 Steam 작업 ID→진단 필터 실제 RoutedEvent | S7.Logs; U7 diagnostics jump |
| release/사용자 데이터 | publish 정적 검사에 jsonl/zip/dump/env/session/SQLite 변형 파일 제외 강화 | scripts/Publish.ps1, 실제 앱/installer 미실행 |

자체 렌더는 `runtime/ui-tests/stage7-*`: 보관함/대표 이미지/최종 분할·추정/판매/없음·실패·재활용, 100/125/150/200% 배율. root Window/PresentationSource0/예상하지 못한 binding 오류0 확인. NumericStepper 높이 과확대는 육안 발견 후36px로 수정했다. 실제 장터 이미지/설치·네트워크·장시간은 PASS로 승격하지 않는다. 별도 [격리 실제 GUI 절차](ISOLATED-GUI-QA.md)와 `.wsb.example`는 준비만 했으며 실행하지 않았다.

## 실행 범위

아래는 최종 마감 실행 결과다. PASS는 **합성/Mock/격리 SQLite/비표시 WPF 범위**다. 실제 Steam 계정/응답 수집/거래/Guard 작업은 전혀 실행하지 않았다. 테스트 HTTP는 소켓 없는 HttpMessageHandler다. 이전 112개 기능 검사와 20개 UI 검사를 제거하지 않았다. 새로운 진단/누락 수정 검사는 상태·전송 횟수·영속 상태·연결 이벤트/정제 결과까지 확인한다.

최종 Debug 집계: **139/139 기능·합성 HTTP·저장소·진단 PASS, FAIL0; 23/23 비표시 WPF PASS, FAIL0; 빌드 경고0/오류0**. 기존112+신규27, UI기존20+신규3. Steam schema v3 변경 후 전체 회귀도 통과했다. 최종 구현 `fab286a`의 Release 산출물도 PASS, 경고0/오류0, PE/data exclusion/MIT notices 확인. Core DLL에 해당 소스 commit metadata 존재를 정적으로 확인했다. 앱 실행·설치 미실행. `scripts/Verify.ps1` 순차 빌드/전체 테스트, `scripts/Publish.ps1` Release PE/로컬 데이터 제외/고지 검사. 실제 앱 실행·설치는 포함하지 않는다. 콘솔 출력과 PNG는 ignored `runtime/stage6-*.txt`, `runtime/ui-tests/stage6-*.png`에만 보관한다.

약칭은 REQUIREMENTS-TRACEABILITY의 초기/F3/F4/F5/S6/U2/U3/U5/U6와 동일. 기능 PASS라도 해당 오류의 상세 로그를 직접 assert하지 않았다면 **진단 PARTIAL**로 표기한다. 모든 가능한 장애 조합을 검증했다는 의미가 아니다.

## 정상 경로

| ID | 시나리오 / 상태·중복 방지 확인 | 테스트/코드 근거 | 진단 확인 | 판정 |
|---|---|---|---|---|
| N01 | 신규 DB 초기화/설정/저널 생성 | NextStage SQLite 재열기/JSON 이관; S6.StorageFault | StoreOpen/Migration/Transaction | PASS(격리 DB); 실제 App.OnStartup NOT VERIFIED |
| N02 | 기존 설정/선택값/상세 모드 복원 | NextStage 설정 재시작; S6.PriceAndSettings | Settings/StoreOpen | PASS |
| N03 | QR 시작→대기→인증 연결 | S6.BatchTrace; F5.QR expiration | HttpRequest/Response/AuthState Connected, 비밀 부재 | PASS(합성); 실계정 NOT VERIFIED |
| N04 | 게임/보관함 조회/중복 페이지 거부 | F4 inventory paging; S6.Selection | InventoryLoaded/Page, selection restored | PASS(지원된 scope); 계정 게임 자동 발견 PARTIAL |
| N05 | 게임별 전체 선택/해제/갱신·전환 복원 | S6.Selection; F5.10000 rows | UiSelection / InventoryLoaded | PASS |
| N06 | 동일 상품 다중 자산/스택 3/4/3 분할 | F5.Same product; S6.BatchTrace | PricePlan/ItemReserved/ItemResult 각각 내부 항목 ID | PASS(합성); 실제 stack capability 기본 차단 |
| N07 | 최근 평균가 계획/빈 자료 | 초기 가중 평균; F3 전략 변경; SalePlanner | PricePlan Average/부족 Count | PASS(Mock); 실제 원자료 경로 BLOCKED |
| N08 | 구매주문 단계/공유 잔량/부족 계산 | 초기 배분/NextStage 공유 호가/F5 교차 context | PricePlan InsufficientDepth/Count | PASS(합성) |
| N09 | 수동 단가 변경→수수료/합계 즉시 갱신 | F3 검토 단가; F5.Review editable; S6.PriceAndSettings | PriceEdited / FeeValidated | PASS |
| N10 | 최종 검토의 전 대상/수량/분할/시각 표시 | F5.Full synthetic QR; U5.KRW review | ReviewOpened/FinalValidation | PASS(분리 UI/합성) |
| N11 | 한 번 승인→전체 행/가격/수량 제출 | 초기 한 번 승인 전체; S6.BatchTrace | BatchCreated→FinalValidation→ItemReserved→ItemResult | PASS(합성); live BLOCKED |
| N12 | Guard 대기→사용자 조회→Listed | S6.BatchTrace | Pending→EvidenceChecked Listed, journal Committed | PASS(합성); 실제 모바일 NOT VERIFIED |
| N13 | 판매 목록 갱신/게임 필터/개별·전체 선택 해제 | S6.Selection; U6.Steam filters; F3 관리 선택 | UiCommand/ListingsLoaded | PASS(합성) |
| N14 | 개별·일괄 취소/부분 실패/중복 취소 거부 | NextStage Mock 취소; F3 취소 재시도; F4 cancel batch | CancelRequested/CancelResult/transaction | PASS(합성); 모든 기존 분기별 로그 assert는 PARTIAL |
| N15 | 확정 반환 후 재가격/재시작 중복 방지 | F5.Confirmed cancellation; S6.RepriceRevision | Reprice Superseded/Transaction Committed | PASS(합성 비스택 단일); live BLOCKED |
| N16 | 새 가격 승인으로 stale 의도 폐기/불변 보존 | S6.RepriceRevision | old SupersededBy/new cancellation proof/동일 replacement key | PASS(합성) |
| N17 | 로그아웃/동의 철회/메모리 세션 해제 | F5.Logout during load; S6.Selection | Logout/AuthState, 비밀 부재 | PASS(합성); 원격 토큰 폐기는 미제공 |
| N18 | 진단 모드/오류·Operation 검색/검토/로컬 ZIP | S6.DiagnosticUi/Privacy/Rotation; U6 | mode/filter/검토 수/ZIP 2파일 재정제 | PASS(내부 명령); Explorer 미실행 |
| N19 | 정상 종료 자원 정리 | App.OnExit/VM.Dispose; 기존 저장소 재열기 테스트 | AppStop/ResourceCleanup 코드 연결 | PARTIAL: 실제 최상위 앱 시작/종료 이벤트 미실행 |

## 예외·오류 주입

| ID | 주입/조건 | 예상 안전 상태 및 검증 | 진단 증거/테스트 | 판정 |
|---|---|---|---|---|
| E01 | QR 만료/인증 거절 | Expired/Denied, QR 제거, 전송0 | F5.QR expiration; AuthState/HTTP 결과 | PASS(합성), 해당 진단 값 개별 assert PARTIAL |
| E02 | 세션 401/403/429 | 요청1 이후 latch, 재전송0 | S6.HttpFailure(401/403/429): HTTP 상태/분류/동일 correlation/elapsed | PASS |
| E03 | 로그인 통신 불가/CAPTCHA | NetworkFailure/AccessBlocked, QR 폐기, 거래0 | F5.Login challenge/network; S6.Timeout; HttpFailure | PASS(합성) |
| E04 | HTTP 400 | ClientRequest 분류, 중단 | S6.HttpFailure(400) | PASS |
| E05 | HTTP 500/503 | Server 분류, 재시도 없음 | S6.HttpFailure(500/503) | PASS |
| E06 | 요청 타임아웃/응답 유실 | Timeout/Unknown, 자동 재전송0 | S6.Timeout/UnknownTrace; 전송 횟수1/내부 op/상태 이벤트 | PASS |
| E07 | HTML challenge/리디렉션/위조 전송 host | 인증/계약 차단, 쿠키 외부 전달0 | F4 auth latch/transfer host/endpoint pinning | PASS(합성); 분기별 상세 로그 assert PARTIAL |
| E08 | JSON 파손/필수 필드 누락 | Parsing/Schema 구분, 데이터 미생성 | S6.Parsing: ResponseParsed/HttpFailure | PASS |
| E09 | orderbook 구형/unknown schema | fail closed, 임의 가격 미생성 | F4 changed book; S6.Parsing | PASS(합성), 실제 변경 응답 NOT VERIFIED |
| E10 | 통화 불일치/미지원 코드/분수 wire | 전송0, 금액 추측 안 함 | F5.Money/fee/currency tests; F4 changed book | PASS(합성); 세부 Currency 이벤트 분기 PARTIAL |
| E11 | 서버 fee/price conflict | 첫 행 Unknown, 나머지 Ready, 예약 보존 | S6.FeeMismatch: FeeValidated FieldsMismatch/ItemResult Unknown | PASS |
| E12 | 가격 갱신 실패 | 기존 데이터 보존/오류 안내, 자동 판매0 | U3.loading/error; F4 fault responses | PASS(Mock/합성); 모든 stage별 로그 PARTIAL |
| E13 | 오래된/미래 시세 | 검토 invalid, 최종 요청 전 차단 | 초기 오래된 시세; S6.PriceAndSettings | PASS |
| E14 | 급격한 수동 가격 이상 | 경고 표시/Valid=false, 값 자동 수정 안 함 | S6.PriceAndSettings; SteamReviewRow.PriceWarning | PASS(합성) |
| E15 | 구매주문 수량 부족 | UnpricedQuantity, 직접 가격 필요/미체결 안내 | NextStage 공유 호가; F3 미산정 행 | PASS(합성); 해당 이벤트 직접 assert PARTIAL |
| E16 | 자산 수량 감소/누락/판매 불가 | 검토 거부/선택 clamp, 요청0 | NextStage 현재 자산/최종 직전; S6.Selection | PASS(합성) |
| E17 | 등록 중 자체 테스트 프로세스 종료 | Submitting 보존→조회만 복구, 재전송0 | F3 owned crash helper; S6.UnknownTrace | PASS(격리 프로세스); OS 전원 차단 NOT VERIFIED |
| E18 | 일부 Listed/일부 Failed | 결과 독립 보존, 실패만 수동 재시도 | S6.PartialFailure; F3 retry failed | PASS, commit 이후 ItemResult/Amounts assert |
| E19 | listing ID 없음/동일 조건 외부 등록/다중 후보 | CandidateOnly/MultipleCandidates/Unknown, 확정/재전송 없음 | F5.Missing ID/multiple candidates; S6.UnknownTrace | PASS(합성); 인과관계 자동 해결 BLOCKED |
| E20 | 승인 대기 장기화 | 대기를 성공/실패로 강제 변환 금지 | 상태 머신/RecoverAsync 코드; 기존 pending 테스트 | PARTIAL: 시간 경과 장기 시뮬레이션 및 사용자 장기 안내 미완성 |
| E21 | 취소 응답 불명/재시작 | Unknown 보존, 재취소·재등록 없음 | S6.UnknownCancel; F5.Cancel unknown restart | PASS(합성), NoCausalEvidence assert |
| E22 | 취소 후 자산 미반환/스택 우연 증가/ID 변경 | proof 미생성, 예약 유지, 재가격 차단 | F5.Missing returned asset/Stack cancellation; S6.UnknownCancel | PASS(합성 안전 차단); 실제 인과 증거 기능 BLOCKED |
| E23 | 재가격 직전 이전 등록 변경 | current != reviewed 거부, 요청0 | F4 cancel/reprice checks; 코드 감사 | PASS(기존 계약 범위), 상세 domain reason PARTIAL |
| E24 | 오래된 재가격 승인 | **취소 요청 전** StaleReview, 취소0 | S6.StaleCancel | PASS |
| E25 | 새 승인 후 재시작/다시 이전 승인 호출 | 이전 Superseded 거부, 새 replacement 중복0 | S6.RepriceRevision | PASS |
| E26 | SQLite 외부 lock(격리 DB만) | 설정 쓰기 실패/기존 데이터 보존 | S6.StorageLocked: Locked/SqliteErrorCode, SQL/값 미기록 | PASS |
| E27 | SQLite 파손/미지원 migration 버전 | 열기 중단, 원본 파일 보존/lease 정리 | S6.StorageFault; NextStage 스키마/이관 | PASS; 모든 실패의 migration 상세 사유 분류 PARTIAL |
| E28 | transaction 중단/저널 저장 실패 | 롤백, 판매 전송0, 성공 상태 로그0 | NextStage 롤백; S6.JournalFailure | PASS |
| E29 | 로그 권한 거부/가짜 disk full 예외 | WriteFailure health, 판매 결과/호출 수 불변 | S6.DiskFailure: 마지막 오류 분류/연관 OperationId도 assert | PASS(주입); 실제 운영 디스크를 채우지 않음 |
| E30 | SQLite max_page_count 한계 | SQLITE_FULL 13/Disk, 실패 INSERT 행0 | S6.SqliteFull: 자체 in-memory 512B 페이지 제한 | PASS(제한된 SQLite); 실제 OS 디스크 부족 NOT VERIFIED |
| E31 | 느린 로그 스트림/큐 포화 | TryWrite 비차단, 유한 메모리/누락 수 노출 | S6.QueuePressure: 2-slot/TCS 결정적 차단/해제 | PASS |
| E32 | 잘못된 설정/legacy 손상/이관 실패 | 임의 덮어쓰기/실거래 없음 | NextStage/F3 DB/migration/설정 방어 | PASS(기존 기능); 각 설정값 상세 진단 PARTIAL |
| E33 | 예외 message/inner/URL/위조 로그/토큰 문자열 | 원문 불포함, 실제 메서드 위치만, export 재정제 | S6.Privacy/BatchTrace | PASS; 합성 marker 기준 외부 보안 감사 아님 |
| E34 | review 없는 ZIP/필터 변경/재사용 승인 | 내보내기 거부/승인 폐기 | S6.Rotation/DiagnosticUi; DiagnosticsViewModel | PASS(기본 거부/재사용/형식), 경합 분기는 코드 방어 |
| E35 | 대량 10,000개 선택/스크롤/갱신 | 총합 알림1회/재활용/선택 보존 | F5.10000 adapter rows; U3.10000 repeated scroll | PASS(단기 합성); 장시간 UI freeze/메모리 누수 NOT VERIFIED |
| E36 | 최상위 창 닫기 중 Steam Busy | MainWindow.Closing에서 Demo/Steam 모두 검사 | 코드 수정/정적 감사 | UNVERIFIED(실제 Window 생성 금지); 취소/저널 자체 경로는 합성 검증 |

## 렌더링 및 실환경 제한

- U6는 diagnostics 1100×700의 100/125/150/200% RenderTargetBitmap, 1100×700/1440×900 MainView 진단 탭, 필터/검토/결과 UI를 자체 렌더한다. 기존 인벤토리/관리/QR/가격 편집/모달/오류/가상화 렌더도 유지한다.
- PNG 육안 검토: 정보 계층/비활성 상태/텍스트/내부 작업 ID/빈 목록 안내/진단 전체 너비. 캡처 API가 아닌 RenderTargetBitmap이며 게임/다른 앱/데스크톱 픽셀은 포함하지 않는다.
- 실제 창·Popup 표시, 강제 포커스, 전역 입력, 물리 키보드/마우스, 모니터별 DPI, 스크린리더, 실제 Explorer 열기, 앱 설치·배포 후 실행은 **NOT VERIFIED**. RenderTargetBitmap 배율은 실제 per-monitor DPI 시험이 아니다.
- 실제 Steam 로그인·인벤토리/가격/fee 응답·판매·취소·모바일 승인·요청 제한은 **NOT VERIFIED/BLOCKED**. 합성 테스트에서 정책 capability를 켠 것은 live 활성화가 아니다.
- 실제 오류를 전부 재현/진단할 수 있다고 보장하지 않는다. 특히 OS 강제 종료 tail, storage 환경 장애, 변경된 외부 계약, 인과 증거 없는 요청은 불확실성을 보존한다.
11차 최종 회귀: **기능177/177 PASS · 비표시 WPF39/39 PASS · 배포 파일 계약14/14 PASS · FAIL0 · Debug 경고0/오류0**. 기존172/33/14 전부 유지. `scripts/Verify.ps1`, ignored `runtime/stage11-verify.txt` 근거. 실제 서버/물리입력/Popup/설치·서명 미실행.

사용자 최소 재확인: (1) 새 ZIP 버전·커밋 확인 후 Steam 탭에서 명시적 QR 시작으로 QR 표시 여부만 확인(실거래 금지), 실패하면 표시된 단계와 검토한 진단ZIP 확인. QR 원문은 공유하지 않는다. (2) 진단 탭/최근 오류 체크를 클릭한 뒤 포인터를 밖으로 이동: 선택 밑줄·체크 표시만 유지되는지 확인. (3) Tab/Shift+Tab의 점선 포커스, 빠른 탭 전환, 비활성→활성, 드롭다운 열기·닫기를 직접 확인. Codex는 이 작업을 실행하지 않았다.
