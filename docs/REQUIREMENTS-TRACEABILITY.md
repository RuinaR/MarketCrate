# 요구사항–구현–검증 추적표 (0.20.0, 2026-10-29 KST)

## 0.20 전체75개 코드·UI 재대조

현재 판정47/16/0/3/9, 기존75개 ID 유지. B08만 BLOCKED→PARTIAL: 명시적 취소 전용 활성화→자기 계정 전체 판매목록 확인→선택 전체 검토→실제 취소/SQLite intent→선택 요청 상태 재조회까지 연결했다. 수수료·보관함에 불필요하게 종속되지 않으며 새 등록/재가격은 차단한다. 실계정 취소 성공 COMPLETE가 아니다.

C12/C13/G11은 과거 개발 테스트만으로 제품 경로가 충분하다고 볼 수 없었다. 실제 Release 보관함에 카드 전환이 없었고, 캐시가8게임을 넘으면 선택 대상이 전체 배치에서 사라졌다. 카드/목록 가상화와 retainedSelections를 제품 VM/템플릿에 연결해 수정했다. F03의 실시간 결과 미연결, B09 취소 대조 UI 누락, B10 최신 계획 강제·사전검증 실패 선소비, F10/F04 다른 계정 보관·로그아웃 늦은 진행 결과를 수정했다. 제품3뷰의 Command Binding50개/ICommand 객체를 반사 검사하고 실제 어댑터/저장소 합성 경로와 연결했다. 외부 페이지/폴더 열기는 자동 실행하지 않았다.

전 행의 기존 담당 소스·서비스 경로·실패 검증을 재검토했고, A01/A02/A04/B01/B03에는 사용자 부분 관측을, A06/AUTH에는 실제 DPAPI 저장을 반영했다. 정확 평균·전체 계정 자동 발견·실거래 확정 실패 재시도·스택/반환·실서버 수수료/Guard/실판매·물리 GUI 검증의 한계는 남는다. COMPLETE는 기존 로컬 계약 범위이며 제품 전 기능 지원을 뜻하지 않는다. 최종 자동/배포 증거는 QA-MATRIX와 CURRENT-STATUS에 기록한다.


0.19 추가 B10/F01/F03/F09: 외부 등록 취소→scope별 실제 보관함 재조회→새 개별 자산/새 가격 전체 검토→최종1승인 배치를 SteamAssistedRelistViewModel/SteamRelistPlan/기존 Executor·Coordinator에 연결했다. 취소 계획은 전송 전 SQLite에 영속 저장하고, 기존 Unknown/예약을 이름만으로 해제하지 않는다. UsabilityTests 신규6개/UsabilityUiTests1개로 변경ID·재시작·단일 승인·불명 중단·수량 부족·시세 변경·민감정보 보호를 검증한다. 실제 수수료/계정 거래·스택 인과 반환은 미검증이므로 기존75개 ID와 판정47/15/0/4/9는 유지한다.

0.19 추가 감사: 재등록 후 상태 재조회가 원 배치만 읽던 경로를 보완하여 현재 계정의 최근 재등록 최대10건을 실제 명령/서버와 대조합니다. Guard→Listed 합성 경로를 확인했으며, 타 계정 배치를 복구해 변경하지 않도록 계정 필터와 저널 불변 회귀를 추가했습니다. 과거 기록은 보존합니다.

## 0.19.0 실사용 경로 재대조 — 전체 완료 아님

최초75개 ID/원본 판정 기준·47/15/0/4/9 집계는 보존한다. COMPLETE는 표에서 한정한 로컬/합성 범위이며 실계정 제품 전체 완료가 아니다. 사용자 지적대로 정확 평균·외부 반환·스택/실제 수수료/실거래 및 물리 GUI에는 미완성이 있다.

| ID | 이번 실제 코드 경로/발견·판정 |
|---|---|
| C02/C13 | SteamCatalogViewModel.Reset/Restore/Observe/SaveCatalog→Pinned/숨김 보존→Games/SQLite. 미확인3게임을 소유 게임처럼 보이게 하던 기본 구성·상태 알림 누락 수정. 전체 계정 게임 자동 발견 아님 |
| G03/G04/G08 | Controls.xaml Combo SelectionContent/Path/FocusRing + BusyIndicator.ProgressBar peer. 가운데 정렬/기존 포커스·상태 회귀. 물리 입력·Popup 미검증이므로 PARTIAL 유지 |
| G05/G06 | Run.OperationLabel→BusyIndicator/작업명/단계 건수/CancelOperation, 로딩·빈 응답·오류 분리. 보이는 동안만 애니메이션·명시적 축소 보존. 새 WPF4개로 검증, 전체 오류 UX가 완성됐다는 뜻 아님 |
| B05/D01 | 실제 체결 원자료 없으므로 정확 평균 기준 재등록은 BLOCKED. 중앙값을 평균으로 이름 변경하지 않음 |
| B10/D05/D08/E06/E09 | 판매 중→PrepareListingRepriceAsync→상품별 Price/Fees→SalePlanner 공유 잔량→NewPriceText/Proposal→BuildManagement→ConfirmManagement1회→ValidateRepriceBatchAsync→RepriceAsync/저널/결과. 기한/전체 사전검증·수수료·가격 변경/이중 클릭 보호. 개별 수량1/소유 저널·반환 근거 한정, B10 PARTIAL |
| B09/C09 | 외부 등록/스택·변경 자산ID 인과 증거 없음. 취소 전 외부 재등록 차단, 변경ID는 Unknown 보존. 안전한 반환 자동화 미완성 |
| F03/F05/F06 | RepriceIntent.ReplacementKey의 실제 명령 상태로 Guard/접수/불명 표시. 전송 전 중단은 Ready, 전송 불명은 Unknown, 나머지 미전송/이전 결과 보존. 로그 이유 NoCausalEvidence와 OperationId 연결 |
| G11/F01/F11 | 새 명령→실제 어댑터→SQLite→결과 연결은 구현·합성 확인. 실제 거래는 미실행·매 실행 OFF. 이를 LIVE/RELEASE_READY로 판정하지 않음 |

검증: UsabilityTests(Default/Catalog/Notifications/Depth/Shortage/Average/Batch/External/Changed/Unknown/Return/Busy/OtherAccount), UsabilityUiTests4개. 첨부 ZIP이 없어 사용자가 겪은 서버 오류는 추가 증거 필요하며 수정 완료로 기재하지 않는다.


## 17차 / 0.18.0 —75개 ID·판정 유지

**47 COMPLETE /15 PARTIAL /0 MISSING /4 BLOCKED /9 UNVERIFIED**, 승격0. D09/D10/E/F/B09–B10: SteamWorkspace.LoadFees→Workspace.FeesAsync→FeeContractResolver→SteamFeeSchedule→FeeFor→BuildReview→SaleBatchCoordinator→Executor.ValidateBatch/재가격→SQLite/결과. FeeModeTests16개와WPF 비교1개를 추가했다. 현재 공식 조건을 확인한 코드/합성 검증과 해당 계정의 실제 수수료 검증은 분리한다.

A01–A06 로그인 유지 부가 경로는 사용자0.17 종료/재시작 실제 복원 성공으로 증거를 보완한다. 새0.18 실서버 복원·회전/장기 만료/다른 Windows 사용자는 미검증이고 원75개 인증/실거래 전체 COMPLETE로 확대하지 않는다. 정식 자가검증 UI 제거는 요구사항 삭제가 아니며 정상 조회·문제 해결·로그/회귀는 유지한다. 원본75행/ID/기준은 아래 그대로 보존한다.


## 16차 — 최초75개 ID/판정 보존

75개 **47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED** 유지. A01–A06 인증·보안: App.Loaded→ProductionShell→SteamWorkspace.RestoreRememberedAsync→SteamAuthStore(DPAPI/ACL/disarm)→SteamQrAuthentication.RestoreAsync→finalizelogin/settoken/계정 쿠키 검증→SaveRememberedLogin→UI 상태. 명시적 Logout와 Dispose를 분리했다. 새 요구 로그인 유지는 SESSION-PERSISTENCE에 추가 추적하며 기존 ID를 삭제/통합하지 않는다.

SessionPersistenceTests20/UiTests3이 QR·저장·재시작·거절·손상·계정/소유자·교체/중단·삭제·누출·저널 보존을 검증한다. 실제 Windows 자체 DPAPI와 ACL은 확인하지만 실제 Steam 복원·다른 Windows 사용자·물리 GUI는 미검증이다. 코드 추가/정책 예외만으로 A항목/실거래를 COMPLETE로 승격하지 않는다. D09/D10/C스택·F일괄등록 계약과 Unknown 보호는 기존 미검증/차단을 유지한다.

## 0.16.1 — ID/판정 보존, 수수료 실패 근거 보강

최초75개 ID·판정 **47 COMPLETE /15 PARTIAL /0 MISSING /4 BLOCKED /9 UNVERIFIED** 유지. A04/D09/D10/G 오류안내 경로: SteamWorkspaceView→SteamReadSelfCheckViewModel→SteamReadSelfCheck→SteamFees.StructureRequest→SteamHttp→SteamResponseContract→3005/3002/4204→CheckRows/ConnectionSummary. legacy wallet fee 관측과 공식 단가 비교는 자동판매의 검증된 수수료를 대신하지 않는다. UI 비교는 조회된 schedule만 읽으며 저널/요청/권한 갱신0. 실제 거래의 SQLite intent/Unknown/재시작 보호 코드는 유지했다.

증거: FeeResponseTests(Market/Unknown/Login/Security/Media/Markers/Limits/Trace/Request/Comparison/Contracts/Observed/Legacy/JsonChallenge), FeeResponseUiTests의 오류 상세/비교 버튼 연결·RTB. 사용자의0.16 Wallet 객체 성공은 관측 증거로 추가하되 실제 수수료/서버모드/KRW 단위·거래 성공이 충분히 검증되지 않았으므로 COMPLETE 승격0이다. 상세 결과는 QA-MATRIX/현재상태 참조.

## 0.16.0 — 기존75개 ID/판정 유지

47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED. A04/D09의 Wallet 자격 확인 왕복과 G UI의 실제 전용 Shell을 구현·합성 검증했다. App→ProductionShell→Steam VM→실제 provider/HTTP/SQLite→UI 경로를 확인하고 Release metadata로 Mock 실행 경로 부재를 검사했다. LiveOnlyTests8개/LiveOnlyUiTests2개와 전체235/50/14 PASS. 사용자 지갑의 실제 성공·실거래·물리 GUI 검증은 없으므로 COMPLETE로 승격하지 않는다. 과거 Mock 검증 이력은 남기되 현재 배포 기능으로 표현하지 않는다.

## 15차 판정 유지 및 경로

최초75개 ID/판정47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED 유지. F01–F12/B08–B10/C09–C13/D09–D10: SteamTradingViewModel.PrepareTradingAsync→SteamExperimentalWorkspace.EnableUserTradingAsync→Wallet/Inventory/Orders→세션계정binding/VerifiedApps→BuildReview→SaleBatchCoordinator→SteamListingExecutor.ValidateBatchAsync→SteamStateStore→Submit/Reconcile/Cancel/Reprice→UI. OrderbookFingerprint/선택게임별fee/context·whole-cancelpreflight·intent호가지문 보호 추가. 새FifteenthStageTests16개와UiTests2개 합성증거. IMPLEMENTED는 조건부코드 범위, CONTRACT_VERIFIED는 합성과 실서버를 구분, LIVE_VERIFIED 없음. 활성화정책 변경만으로 미검증 원요구사항을 COMPLETE로 바꾸지 않는다.

## 14차 재평가

최초75개 ID/행/판정 유지: COMPLETE47/PARTIAL15/MISSING0/BLOCKED4/UNVERIFIED9. 사용자0.13 부분관측(세션기록·목록첫8건)을 전체/실거래 증거로 승격하지 않는다. 수정경로: SteamAccess.SendAsync(권한/제한)→SteamFees.ObserveWallet(관측/정수계약)·SteamContract.Inventory(검증빈페이지)→SteamReadSelfCheck→SteamReadSelfCheckViewModel/SteamWorkspaceView(요약/접힌상세). FourteenthStageTests8개·FourteenthStageUiTests1개와 전체207/46/14 PASS. 실제wallet원인·전체보관함·KRW/스택 계약은 미검증, LIVE차단 유지.

## 13차 판정 유지

최초75개 ID/기준 **47 COMPLETE / 15 PARTIAL / 0 MISSING / 4 BLOCKED / 9 UNVERIFIED**, 변경0. A01~05는 기존 QR+Secure Community cookie 검사, 공식 OpenID identity/브라우저 세션 전달 미지원 구분. A04/B01/C01/D01/D02/D09/D10/B04: UI SelfCheck→불변 plan→SteamReadSelfCheck→기존 probe/fee/price/image→정제4204→row 결과, T13 12개/U13 3개 근거. 실서버·실계정 검증 N, 앱 OpenID/OAuth 미구현, LIVE 차단 유지. 자가검증의 로컬/합성 완성을 원 요구사항의 실제 계정 지원 COMPLETE로 승격하지 않는다.

## 12차 사용자 결함 보완 — 기존75개 판정 유지

**47 COMPLETE / 15 PARTIAL / 0 MISSING / 4 BLOCKED / 9 UNVERIFIED**, ID/기준·판정변경0. A01/A03/A04/A05: SteamQrAuthentication.HasSession/Connected→VM.CanRead/ReadStatuses/Run/Error→UI 인증 카드 및 상태, TwelfthStageTests 10개 합성 검사. B01/C01/D01: provider parse는 독립 실패, HTTP 제한/실행기 latch는 유지. C13: game row/selection cache+ScrollMemory, U12 페이지·게임복원 검사. G03/G04/G08/G09: Header B hover/선택 밑줄·키보드 점선/5페이지 Grid/출력 배율·레이아웃/명령 경로 검사. 실계정 성공·실환경 GUI·물리 DPI가 검증됐다는 판정으로 확대하지 않는다.

## 11차 사용자 결함 보완 — 판정 유지

75개 ID/기준과 **COMPLETE47/PARTIAL15/MISSING0/BLOCKED4/UNVERIFIED9** 유지. A01/A03: SteamWorkspaceVM.Begin→SteamQrAuthentication.BeginCore→AuthForm→SteamHttp→ReadResponse→UI stage 경로의 최상위 요청/정수JSON/실패분류 수정, T11.RequestContract/NumericResponse/ResponseFailures/ApiFailures/Cancellation 근거. 실서버 성공 검증 N. G03/G04/G08: Controls.xaml/FocusCue의 VSM·Trigger 단일소유/키보드점선/Checked·Selected 구분, U11 상태왕복·자체PNG 근거. 물리 입력·Popup·현재 배포 GUI는 NOT VERIFIED이며 이 보완으로 실환경 COMPLETE를 추가하지 않는다.

## 10차 판정과 사용자 보고

75개 기존 ID/기대동작/판정기준 유지: **47/15/0/4/9**, 판정변경0. 사용자9차 수동GUI7개PASS는 부분 실환경 증거로 기록하고 G03/04/08/09/11의 모든 조건/24개 전체를 COMPLETE로 승격하지 않는다. 실계정/실응답 인증 검증 N 유지.

- A01/A02/A03/A05: SteamWorkspaceVM.Begin/Check→기존 Authentication→UpdateChallenge→QrMatrix; 회전UI/만료/재로그인/로그아웃 T10.QrRotation. 공개proto/합성일뿐실계정 성공 아님.
- A04/B01/B05/C01: 독립 LoadListings/LoadFees→기존 Orders/Fees→SetListings/RecordCheck/UI. Inventory404가목록을막던결합제거. T10.IndependentListings/ExpiredReads. 실제 inventory/fee/listings 형식은 계속UNVERIFIED.
- D02/D03/E03/G08: PriceDisplay→PriceBasis/기존SalePlanner→UI/ReviewRows; stable settings alias/enum키·표시, 최고매수주문잔량/단가비교. 기존 공유호가/정수fee/스택/Unknown회귀 그대로.
- F01/F07/B08~11: 기존 IAuthorizedExecutor/BatchCoordinator/SteamStateStore와 defaultBlocked 그대로. 공식시장버튼은대체사용자행동이며F01완료아님. QR최종승인/로그인동의가Valve허가를뜻하지않음.
- LOG: ReadContract4202→화이트리스트 log→OperationFilter/검토ZIP. Count/Currency/고정endpoint·내부ID만, raw응답/QR필드수집안함.

## 9차 판정 유지

기존75개 ID/기대 동작 유지. **COMPLETE47 / PARTIAL15 / MISSING0 / BLOCKED4 / UNVERIFIED9**, 변경0. G03/G04/G08/G09의 실제 입력·Popup·키보드·물리 DPI는 guest 미제공으로 미검증. 실제 UI를 detached PASS로 승격하지 않았다.

배포 경로 추가: Publish-SelfContained→공식 build팩→CLR/WPF/SQLite x64/고지/데이터 제외 검사→기존 Prepare-IsolatedGuiQa/manifest→기존 guest-local launcher(SCD 분기). AppRunProfile의 분리된 사용자 데이터·offline Steam factory는 그대로다. 24개 실제 GUI case는 NV24.

진단 별도 LOG 경로: DiagnosticViewModel.ReviewPackage/Export→JsonDiagnosticLog.ReadSafe(Utc 정렬)→사용자 검토 불변 snapshot→로컬ZIP. NinthStageTests.OrderedExport는 mtime 역전 결함을 수정 전 FAIL로 재현하고 operation·원인·순서·로컬경로를 검증한다. 이 보완이 실계정/실제 GUI 기능 완료를 의미하지 않는다.

## 8차 판정 유지 및 추가 코드 경로

기존75개 ID/행/이전 판정은 삭제·병합하지 않았다. **47/15/0/4/9 유지**, 새 COMPLETE 없음. 실서버/실계정/실제 GUI 검증 승격 없음.

- A03: SteamHttp.SendAsync 취소 분류 → SteamQrAuthentication.GuardAsync 선택 필드/취소 → VM.Run Connected 보존. E8.QrDefaults/QrInvalid/QrCancel/ReadCancel 합성 검증 추가.
- F05/F07: SteamContract.Sell 모호한 confirmation flag 거부 → 기존 Unknown/저널/예약 유지. E8.Confirmation. 공개 판매 코드와 서버 계약은 별개다.
- G03/G04/G08(PARTIAL): Steam VM.Changed AnyReview 알림 → SteamBody.IsEnabled → ModalNavigation 자체 focus scope/탐색 → Modal9005 연결. ComboBox focus/open trigger, NumericStepper disabled peer 예외. U8.modal/peers. 실제 focus/Popup/UIA/키 입력은 미검증이라 승격 안 함.
- G09(PARTIAL): U8.offline 자체 픽셀 배율 렌더 추가는 물리 DPI 증거가 아니다.
- 실제 실행 준비: AppRunProfile→App→독립 QA 데이터/로그→offlineOnly MainVM/SteamVM→Steam factory차단. Prepare-IsolatedGuiQa→manifest/24개NV→게스트 수동 launcher. 하네스 존재는 GUI 실행/환경 확보의 증거가 아니다.

## 7차 변경과 감사 기준

75개 기존 ID/기대 동작을 유지하고 전 행을 재평가했다. 표의 마지막 열에 6차 판정을 보존했다. **44/16/1/5/9 → 47/15/0/4/9**. 새 COMPLETE는 **B04, C02, D02**(각 행의 로컬·합성·비표시 WPF 범위); B05는 BLOCKED→PARTIAL이며 정확한 평균은 제공하지 않는다. 나머지 판정은 그대로 유지한다. 실제 Steam 응답/계정, 실제 창/키보드/모니터 DPI 판정을 승격하지 않았다.

코드 경로 추가: `SteamContract` image metadata maps → provider.Images → VM row.Image → `Controls/ItemThumbnail` → opt-in `SteamExperimentalWorkspace.Images` → 별도 SteamImageCache HTTP/LRU → frozen BitmapImage → UI. metadata maps는 거래 snapshot과 분리해 이미지 변경을 거래 의도 변경으로 취급하지 않는다.

카탈로그: SteamWorkspaceViewModel (SteamCatalogViewModel.cs) → InventoryScope.Validate → SteamStateStore catalog transaction(기존 v3) → Games/current context → 동일 InventoryProvider paging → UI 상태. 실제 조회한 현재 scope/판매목록만 관측하고 자동 AppID 탐색은 없다.

추정 지표: VM mode/window → explicit book/history refresh → PriceIndicators → PriceEvidence(기간/표본/digest) → SalePlanner의 별도 basis → ReviewRow → immutable Evidence → Executor의 재조회/digest 일치 및 최종 freshness 검사. 동일 상품 preflight는 한 번만 조회하며 거래량/중앙값 합계가 우연히 같아도 원 표본 digest가 바뀌면 승인 갱신을 요구한다.

## 판정 범위와 집계

사용자의 최초 A–G 요구를 **75개 추적 단위**로 정규화했다. 이름/가격/수량 등 한 행의 표시 계약은 함께 관리하고, 누락 이미지/평균가는 별도 ID로 분리했다. 6차 진단 요구 LOG-* / SYS-*는 아래 별도 관리하며 최초 요구사항 수에 섞지 않는다.

**COMPLETE 47**, **PARTIAL 16**, **MISSING 0**, **BLOCKED 3**, **UNVERIFIED 9**. 총 **75** (0.20 현재).

COMPLETE는 그 행에 명시한 **로컬 순수 계산·상태 머신·UI 바인딩·합성 계약 범위의 완료**다. 실계정 기능 전체 지원 판정이 아니다. Codex의 실계정 실행 검증은 N이다. 사용자가 직접 관측한0.17 QR/복원·목록8건·빈CS2·KRW 지갑은 U0.17로 구분하며 새 버전/전체 읽기·수수료·거래로 확대하지 않는다. 원본 인증 응답을 수집/재생하지 않았고, 표의 S는 공개 소스 형식을 참고한 **합성 HttpMessageHandler 계약**만 뜻한다. Mock PASS를 live PASS로 승격하지 않는다.

PARTIAL=일부 경로/기능 부족, MISSING=실행 코드 없음, BLOCKED=필수 외부 허용/자료 미확보, UNVERIFIED=코드는 있지만 live 계약 근거 부족. 최신 동작은 이 표와 CURRENT-STATUS를 기준으로 하고 예전 WORKLOG의 '완료' 표현을 확대 해석하지 않는다.

## 코드 인벤토리와 실제 호출 경로

5개 프로젝트(Core, Infrastructure, App, Tests, UiTests)를 유지했다. 아래 경로의 `→`는 실제 소스의 호출·반환/갱신 관계다. 앱/어댑터의 계정·시세·이미지 요청은 실제 서버에서 실행하지 않았고 공개 연구 문서/JS만 읽었다. 계약 테스트에서는 동일 어댑터에 합성 handler를 주입한다.

| 경로 | UI → VM → 서비스/어댑터 → 저장소 → 상태 반영 |
|---|---|
| AUTH | `src/MarketCrate.App/SteamWorkspaceView.xaml` QR 탭 → `ViewModels/SteamWorkspaceViewModel.cs` BeginAsync/CheckAsync/LogoutNow → `src/MarketCrate.Infrastructure/Steam/SteamQrAuthentication.cs` → `SteamAccess.cs` SteamHttp, 자체 CookieContainer(메모리) → LoginState/QrMatrix/Status 바인딩. 사용자 동의 시 SteamAuthStore의 자체 Refresh Token DPAPI 저장/복원만 사용 |
| INV | Steam 보관함 → VM.LoadAsync/Game/Filter → `SteamExperimentalWorkspace.cs` SetGame → `SteamProviders.cs` SteamInventoryProvider.GetAsync → `SteamContract.cs` Inventory → `SteamStateStore.cs` AvailableAll(예약만) → Assets/VisibleAssets/SteamAssetRow. UI 선택은 세션 내 Dictionary, 실보관함 응답은 DB에 저장 안 함 |
| LIST | Steam 판매 탭 → VM.LoadAsync/RecoverAsync → `SteamProviders.cs` SteamOrdersProvider.RefreshAsync → SteamHttp/SteamContract.Listings → 메모리 cache/VM.SetListings → Listings/VisibleListings. 공개 목록 관측을 거래 저널로 대체하지 않음 |
| PRICE | 선택 시세 갱신 → VM.RefreshPricesAsync → `SteamProviders.cs` SteamMarketDataProvider.RefreshAsync/GetMedianHistoryAsync → 메모리 cache → `src/MarketCrate.Core/SalePlanner.cs` Build/`Pricing.cs`/`Money.cs` → SteamReviewRow. 실제 평균가용 Trades는 없음 |
| FINAL | 최종 검토 → VM.BuildReview/SteamReviewRow.PriceText/Quote/Snapshot → 불변 `Models.cs` SaleBatchSnapshot → `SaleBatchCoordinator.cs` ExecuteAsync → `SteamListingExecutor.cs` ValidateBatchAsync → 현재 inventory/SteamFees/orderbook 재검증 → `SteamStateStore.cs` SaveAsync → Results/Status |
| SUBMIT | 최종 한 번 승인 → SaleBatchCoordinator.ExecuteAsync → durable Ready/Submitting → SteamListingExecutor.SubmitAsync → SaveCommand → SteamHttp.Sell → SteamContract.Sell → command/result journal → Results. RecoverAsync는 ReconcileAsync/InspectAsync로 조회만 하고 Unknown 재전송 안 함 |
| CANCEL | 판매 선택 → VM.TryManagement/ManageAsync → SteamListingExecutor.CancelAsync/ReconcileCancellationAsync → SteamStateStore command/return proof → 결과 UI. Mock은 MainViewModel.CancelListingsAsync → Core/MockCancellation.cs / MockListingManagement.cs → SqliteDemoStore → Listings/Results |
| REPRICE | 가격 변경/보존 의도 새 가격 검토 → VM.ManageAsync → SteamListingExecutor.RepriceAsync/ReviseRepriceAsync → 확정 취소 proof → SteamStateStore.SupersedeReprice(원자적 이전 의도 폐기+새 의도 저장) → ValidateBatchAsync/SubmitAsync → 재시작 시 동일 replacement key 유지 |
| UI | `src/MarketCrate.App/MainView.xaml`, `SteamWorkspaceView.xaml`, `DiagnosticsView.xaml` → `ViewModels/Observable.cs` Command/AsyncCommand → VM → Core/Infrastructure → INotifyPropertyChanged/RangeObservableCollection. `Themes/Tokens/Colors.xaml`, `Themes/Controls/Controls.xaml`, `Controls/VirtualizingTilePanel.cs` 공통 시각/가상화 |
| LOG | UI/도메인/HTTP/SQLite → `src/MarketCrate.Core/Diagnostics.cs` DiagnosticHub → `src/MarketCrate.Infrastructure/Diagnostics/DiagnosticRecordSafety.cs` → JsonDiagnosticLog 비동기 bounded writer → 사용자별 JSONL. DiagnosticsViewModel → 검토된 재정제 ZIP. SQLite 저널을 export하지 않음 |
| ALL | 위 실제 경로의 조합. Demo는 MainViewModel → DemoDataProvider/MockListingExecutor → SqliteDemoStore. 실험 factory는 사용자 QR 시작 때만 생성하며 기본 SteamTradingPolicy.Blocked |

소스 약칭: Core 클래스는 `src/MarketCrate.Core/<클래스>.cs`, Steam 클래스는 `src/MarketCrate.Infrastructure/Steam/<클래스>.cs`; 여러 provider는 `SteamProviders.cs`, 모델은 `Models.cs`/`SteamContract.cs`, VM은 `src/MarketCrate.App/ViewModels/`다. `SteamAssetRow`, `SteamListingRow`, `SteamReviewRow`는 SteamWorkspaceViewModel.cs 하단에 있다. Control/Style 경로는 UI 행과 위 표에 명시했다.

테스트 약칭: 초기=`tests/MarketCrate.Tests/Program.cs`의 이름 있는 테스트; F3=`ThirdStageTests.cs`, F4=`FourthStageTests.cs`, F5=`FifthStageTests.cs`; S6=`SixthStageTests.cs`의 해당 메서드/테스트명. U2=`tests/MarketCrate.UiTests/Program.cs`, U3/U5/U6=`ThirdStageUiTests.cs`/`FifthStageUiTests.cs`/`SixthStageUiTests.cs`. 표의 기존 테스트명은 검색 가능한 핵심 구절이다. 전체 오류 주입 판정과 추가 미검증은 QA-MATRIX에 있다.

## 요구사항별 추적

구현 Y=실행 경로 존재, P=부분 코드, N=누락. M=Mock/순수·내부 UI PASS, S=소스 형식 기반 합성 계약 PASS, —=해당 실연동 계약 검증 없음. 실패 칸의 P는 해당 연결 테스트 범위만 의미하며 모든 현실의 실패를 보장하지 않는다.

| ID | 최초 요구/기대 동작 | 담당 소스·클래스/메서드 | UI 진입점 | 호출 경로 | 구현 | 개발 Mock 이력 | 합성 계약 / 실제응답 / 실계정 | 실패·예외 및 테스트 | 판정 | 미완성·위험·판정 근거 | 6차 판정 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| A01 | Steam 계정 로그인 | `SteamQrAuthentication.BeginCoreAsync/CheckCoreAsync` | Steam > QR 연결 | AUTH | Y | M | S / U0.17 / U0.17 | P (범위 한정); S6.BatchTrace | **UNVERIFIED** | 사용자0.17 QR/Community 세션·재시작 복원 성공 관측. 신규0.20/회전·장기 만료 미검증 | UNVERIFIED |
| A02 | 공식 앱 QR 인증 흐름 | `SteamQrAuthentication.ValidateChallenge; QrCodeView` | Steam > QR 연결 | AUTH | Y | M | S / U0.17 / U0.17 | P (범위 한정); F5.QR expiration; U5.QR | **UNVERIFIED** | 공식 도메인을 사용하지만 비공식 인증 클라이언트이며 공식 브라우저 로그인 대체 미구현 | UNVERIFIED |
| A03 | 성공·실패·취소·인증/세션 만료 구분 | `SteamWorkspaceViewModel.Error/LoginState; SteamHttp.SendAsync` | Steam > QR/상태 | AUTH | Y | M | S / N / N | P (범위 한정); F5.QR expiration; S6.HttpFailure | **COMPLETE** | 상태 분기 구현/합성 검증 범위. 실제 Steam 인증 성공을 뜻하지 않음 | COMPLETE |
| A04 | 로그인 후 실제 계정 데이터 표시 | `SteamWorkspaceViewModel.LoadAsync; SteamExperimentalWorkspace` | Steam > 실제 보관함/판매 | INV/LIST | Y | M | S / U0.17일부 / U0.17일부 | P (범위 한정); F5.Full synthetic QR; S6.BatchTrace | **UNVERIFIED** | 사용자 목록8건/빈CS2/KRW wallet 관측. 모든 데이터/페이지·신규0.20 미검증, Release 실제 전용 | UNVERIFIED |
| A05 | 로그아웃·동의 철회·늦은 응답 폐기 | `SteamWorkspaceViewModel.LogoutNow/Run; SteamQrAuthentication.Dispose` | Steam > 취소/로그아웃 | AUTH | Y | M | S / N / N | P (범위 한정); F5.Logout during load; S6.Selection | **COMPLETE** | 로컬 세션 정리 범위 PASS; 서버의 원격 토큰 폐기 API 미제공 | COMPLETE |
| A06 | 인증 비밀 보호 | `SteamAuthStore; SteamWebRefreshToken; SteamQrAuthentication; DiagnosticHub.Sanitize` | Steam > 위험 안내 | AUTH/LOG | P | M | S / N / N | P (범위 한정); S6.Privacy; S6.BatchTrace | **PARTIAL** | 동의 시 자체 Refresh Token만 DPAPI CurrentUser/ACL/원자 저장. 사용자0.17 복원 관측. 완전한 메모리 소거·외부 보안 감사 미검증 | PARTIAL |
| B01 | 현재 판매 전체 조회·페이지 완전성 | `SteamOrdersProvider.RefreshAsync; SteamContract.Listings` | Steam > 실제 판매 > 갱신 | LIST | Y | M | S / U0.17첫페이지 / U0.17 | P (범위 한정); F4.listings pagination; F5.Full synthetic QR | **UNVERIFIED** | 최대 10,000 active 제한. 전체성이 불명확하면 실패. 사용자 첫8건/활성total8 관측, 신규/변동 페이지 계약 미검증 | UNVERIFIED |
| B02 | 게임별 판매 구분/필터 | `SteamWorkspaceViewModel.Filter/VisibleListings` | Steam > 보관함 게임 선택 > 판매 | LIST | Y | M | S / N / N | P (범위 한정); S6.Selection; U6.Steam filters | **COMPLETE** | 서버에서 받은 목록의 appid 필터. 계정 게임 자동 발견과 별개 | COMPLETE |
| B03 | 판매 이름·가격·수량·상태 표시 | `SteamListingRow.Description; SteamContract.Listings` | Steam > 실제 판매 | LIST | Y | M | S / U일부 / U일부 | P (범위 한정); F5.Full synthetic QR; U5.KRW review | **UNVERIFIED** | 표시 바인딩과 합성 계약 구현. 사용자 실제8행/이미지 관측. 모든 상태·수량/신규버전 미검증 | UNVERIFIED |
| B04 | 실제 아이템 이미지 표시 | `SteamContract.Image/Inventory/Listings; SteamImageCache; ItemThumbnail; SteamWorkspaceViewModel` | 보관함/판매 목록 | INV/LIST | Y | M | S / N / N | P (합성/내부 경로); S7.Metadata/ImageSecurity/ImageDedup/ImageCancel/ImageFaults/ImageMemory; U7.thumbnail/recycling/inventory/listings/review | **COMPLETE** | 확인한 단일 Steam CDN, 인증 분리, 제한된 바이트/픽셀/LRU/대기열, 구독 취소/늦은 응답 방어, 실제 PNG 디코딩/재활용 합성 PASS. 실제 Steam CDN 이미지와 실계정 메타데이터는 미검증. | MISSING |
| B05 | 판매 항목의 최근 평균 거래가 표시 | `SteamWorkspaceViewModel.ListingMetrics (SteamCatalogViewModel.cs); SteamListingRow.Metric; SteamMarketDataProvider.RefreshEstimateAsync` | Steam > 결과/이력 | PRICE | P | M | S / N / N | P (합성/내부 경로); U7.listing metrics command; S7.Indicators | **PARTIAL** | 선택 판매목록의 기간 중앙값 기반 대체 추정치를 조회/표시한다(최대10상품). 정확한 최근 평균 원자료는 여전히 없어 평균 지원은 미완성. | BLOCKED |
| B06 | 판매 개별 선택/해제 | `SteamListingRow.Selected` | Steam > 실제 판매 체크박스 | LIST | Y | M | — / N / N | P (범위 한정); F3.management selection; U3.management | **COMPLETE** | 로컬 선택 동작 범위 | COMPLETE |
| B07 | 판매 전체 선택/해제 | `SteamWorkspaceViewModel.SelectAllListings/ClearListings` | Steam > 실제 판매 | LIST | Y | M | S / N / N | P (범위 한정); U6.Steam filters; S6.Selection | **COMPLETE** | 현재 게임/검색 결과 전체 선택, 해제는 보유한 모든 로컬 목록 선택 해제 | COMPLETE |
| B08 | 선택 등록 취소 | `SteamTradingViewModel.PrepareCancellationAsync; SteamExperimentalWorkspace.EnableUserCancellationAsync; SteamWorkspaceViewModel.ManageAsync; SteamListingExecutor.CancelBatchAsync/CancelAsync` | 게임·판매 설정 > 취소 전용 활성화 > 판매 중 > 선택 전체 취소 승인 | CANCEL | P | 개발 회귀 | S / N / N | UsabilityTests.CancellationOnly/CancellationReset/CancelUiPath; 합성 취소·권한 분리 | **PARTIAL** | 기본 OFF·계정 결속·목록 최신 대조·선영속 의도. 수수료/보관함 조회 없이 취소 전용 가능하며 새 등록/재가격 차단. 실서버 취소/반환은 미검증 | BLOCKED |
| B09 | 취소 결과·실제 반환 확인 | `SteamRequestReconciliation.RefreshSelectedRequestAsync; SteamListingExecutor.ReconcileCancellationAsync; SteamStateStore.SaveReturn` | 판매 결과 > 요청 상태 확인 | CANCEL | P | M | S / N / N | P (범위 한정); F5.Confirmed cancellation; S6.UnknownCancel | **PARTIAL** | 직접 연결된 비스택 단일 동일 ID 반환만 proof. 스택/외부 등록/ID 변경 인과관계 미지원 | PARTIAL |
| B10 | 확정 취소 후 새 가격 재등록/새 승인 | `SteamAssistedRelistViewModel; SteamRelistPlan; SteamListingExecutor.RepriceAsync/ReviseRepriceAsync; SteamStateStore.SaveAsync` | 판매 가격 변경 / 결과 > 새 가격 검토 | REPRICE | P | M | S / N / N | P (범위 한정); S6.RepriceRevision; S6.StaleCancel | **PARTIAL** | 외부 취소→새 실제 자산/새 가격→단일 승인 배치 및 원등록 반환 proof 경로. 계획/배치 원자 저장·이전 계획 선택·사전검증 실패 복구. 스택/실계정 미검증 | PARTIAL |
| B11 | 취소·처리·승인대기·실패·완료·불명 구분/재시도 | `MockListingManagement.RetryFailed; SteamWorkspaceViewModel.Results` | Mock 판매/결과; Steam 결과 | CANCEL/REPRICE | P | M | S / N / N | P (범위 한정); F3.retry management; S6.UnknownCancel | **PARTIAL** | Mock 확정 실패 재시도만 제공. 실험 취소는 Reconciled/Unknown으로 표현하며 live 확정 실패 재시도 정책 미완성 | PARTIAL |
| C01 | 계정 게임별 보관함 조회/페이징 | `SteamInventoryProvider.GetAsync; SteamContract.Inventory` | Steam > 게임 선택 > 실제 데이터 갱신 | INV | Y | M | S / N / N | P (범위 한정); F4.inventory pagination; S6.Selection | **UNVERIFIED** | 개별 appid/context 조회 구현. 실제 계정 전체 게임 목록은 미확인 | UNVERIFIED |
| C02 | 게임 목록/보관함 표시 | `InventoryScope; SteamWorkspaceViewModel.AddScope/RemoveScope/ObserveCatalog/RestoreCatalog (SteamCatalogViewModel.cs); SteamStateStore.ReadCatalog/SaveCatalog` | Steam > 보관함 게임 선택 | INV | Y | M | S / N / N | P (합성/내부 경로); S7.Catalog/CatalogStates/CatalogInvalid/JournalCompatibility; U7.catalog bindings | **COMPLETE** | 계정별 app/context 수동 관리와 판매목록 관측, SQLite 복원, 상태/판매 가능 종류 수/대표 이미지. 전체 계정 자동 발견 아님; 이름 미확인 App은 임시 이름. 게임 로고 대신 보관함 대표 이미지를 명시. | PARTIAL |
| C03 | marketable 필터 | `SteamWorkspaceViewModel.MarketableOnly/Filter` | Steam > 장터 판매 가능만 | INV | Y | M | S / N / N | P (범위 한정); S6.Selection; U6.Steam filters | **COMPLETE** | tradable 필터와 분리 | COMPLETE |
| C04 | tradable/marketable/잠금 구분 | `InventoryAsset; SteamAssetRow.CanSelect/Flags; BatchValidation` | 보관함 플래그/체크박스 | INV/FINAL | Y | M | S / N / N | P (범위 한정); 초기 비판매/보류/비 스택; F4.inventory flags | **COMPLETE** | 두 속성을 혼용하지 않는 로컬 계약 | COMPLETE |
| C05 | 개별 선택/해제 | `SteamAssetRow.Selected` | 보관함 체크박스 | INV | Y | M | S / N / N | P (범위 한정); S6.Selection; U5.review | **COMPLETE** | 판매 불가/예약 초과 선택 차단 | COMPLETE |
| C06 | 게임별 전체 선택/해제 | `SteamWorkspaceViewModel.SelectAll/ClearSelection` | 보관함 > 전체 선택/선택 해제 | INV | Y | M | S / N / N | P (범위 한정); F5.10000 rows; S6.Selection | **COMPLETE** | 전체 선택은 현재 표시한 게임·검색·필터 범위 | COMPLETE |
| C07 | 판매 수량 입력/상한 | `SteamAssetRow.Quantity/Maximum; NumericStepper` | 보관함 수량 스테퍼 | INV/FINAL | Y | M | S / N / N | P (범위 한정); 초기 수량 상한; U2.stepper; S6.Selection | **COMPLETE** | 가용 수량 감소 시 clamp 및 재검증 | COMPLETE |
| C08 | 동일 상품 여러 자산 선택 | `SalePlanner.Build; SteamWorkspaceViewModel.BuildReview` | 보관함 선택 > 최종 검토 | PRICE | Y | M | S / N / N | P (범위 한정); F5.Same product across contexts | **COMPLETE** | 자산 키와 상품 키를 분리 | COMPLETE |
| C09 | 수량형 스택 분할 | `SalePlanner; SteamListingExecutor.ValidateBatchAsync/SubmitAsync` | 보관함 수량 > 분할 검토 | SUBMIT | P | M | S / N / N | P (범위 한정); F5.KRW stack split; F5.Stack amount support | **PARTIAL** | 별도 stack capability가 기본 OFF. 실제 게임별 amount 의미/반환 증거 미확인 | PARTIAL |
| C10 | 소유·가용·예약·잔여 수량 보존 | `SteamStateStore.AvailableAll; SteamAssetRow.UpdateAvailable` | 보관함 Flags/수량 | INV/SUBMIT | P | M | S / N / N | P (범위 한정); F5.Restart reserves; F5.Stack cancellation | **PARTIAL** | Unknown/Guard 예약 보존. 스택 취소 반환 증거 부족 시 보수적 예약 유지 | PARTIAL |
| C11 | 검색·정렬·필터 | `SteamWorkspaceViewModel.Filter/Search/Descending` | Steam 보관함 검색/이름 역순 | INV | Y | M | S / N / N | P (범위 한정); S6.Selection; U6.Steam filters | **COMPLETE** | 이번에 실험 화면에도 연결. 검색 내용은 로그에 기록하지 않음 | COMPLETE |
| C12 | 대량 보관함 성능/가상화 | `VirtualizingTilePanel; RangeObservableCollection; SteamWorkspaceView.xaml` | 보관함 목록/카드 | INV/UI | Y | M | S / N / N | P (범위 한정); F5.10000 adapter rows; U3.10000 repeated scroll | **COMPLETE** | 10,000 합성 항목/분리 렌더 범위, 장기 실사용 성능 인증 아님 | COMPLETE |
| C13 | 갱신·게임 전환·화면 이동 선택 보존 | `SteamWorkspaceViewModel.RememberSelection/LoadAsync/SetListings` | 보관함/판매 화면 전환 | INV/LIST | Y | M | S / N / N | P (범위 한정); S6.Selection | **COMPLETE** | 실행 세션 내 자산 키별 선택/수량 복원. 새 계정/로그아웃 시 제거, 가격은 다시 조회 | COMPLETE |
| D01 | 실제 거래 이력 기반 평균 | `SteamMarketDataProvider; Pricing.WeightedAverage` | Mock 가격 기준 / Steam 안내 | PRICE | P | M | S / N / N | P (범위 한정); 초기 가중 평균; F4.history median | **BLOCKED** | 0.20.8 사용자 요청으로 평균 옵션 철회. 과거 개발 Trades는 빈 배열/원자료 미확보 | BLOCKED |
| D02 | 기간·표본·가중 방식 명시 | `PriceIndicators.MedianEstimate; PriceEvidence; SteamMarketDataProvider.RefreshEstimateAsync; SteamWorkspaceViewModel.PricingMode/WindowIndex` | 개발 회귀 전용 · 0.20.8 제품 UI 철회 | PRICE | Y | M | S / N / N | P (합성/내부 경로); S7.Indicators/EstimateFlow/UnavailableAverage/ChangedHistory/StricterContracts; U7.estimate-review | **COMPLETE** | 24h/7d/30d 기간·수집시각·구간 수·서버 거래량·하위 가중 중앙값 방식/원자료 한계 표시. 정확한 평균은 자료 없으면 선택해도 검토 불가. 실제 거래 VWAP 지원 완료 아님. | PARTIAL |
| D03 | VWAP·중앙값 혼동 금지 | `SteamMarketDataProvider.RefreshAsync/GetMedianHistoryAsync` | Steam 보관함/결과 안내 | PRICE | Y | M | S / N / N | P (범위 한정); F4.history median; U5.review | **COMPLETE** | 중간가격 이력을 별도 이름으로 표시, VWAP로 사용 안 함 | COMPLETE |
| D04 | 거래 자료 부족 안내 | `SalePlanner.Build/Unpriced; SteamReviewRow.Insufficient` | 검토 부족 수량 / 평균가 미제공 안내 | PRICE | Y | M | — / N / N | P (범위 한정); 초기 빈 주문/평균; F3.unpriced review | **COMPLETE** | 부족 값을 생성하지 않음 | COMPLETE |
| D05 | 매수 가격 단계별 수량 배분 | `Pricing.Allocate; SalePlanner.Build` | 검토 분할 행 | PRICE | Y | M | S / N / N | P (범위 한정); 초기 호가 3/4/3; F5.KRW split | **COMPLETE** | 높은 구매자 가격 우선의 추정 계산 | COMPLETE |
| D06 | 호가 부족/예상 미체결 수량 | `SalePlan.Unpriced; SteamWorkspaceViewModel.BuildReview` | 검토 부족 행/상태 | PRICE | Y | M | — / N / N | P (범위 한정); 초기 부족/빈 주문; F3.unpriced rows | **COMPLETE** | 부족 수량에 임의 호가를 붙이지 않음 | COMPLETE |
| D07 | 조회 시각/신선도 | `SteamReviewRow.Valid/PriceWarning/RefreshStatus` | 검토 UTC/신선도 다시 확인 | PRICE/FINAL | Y | M | — / N / N | P (범위 한정); S6.PriceAndSettings; 초기 오래된 시세 | **COMPLETE** | 5분 기준/미래 시각 거부. 사용자 요청 갱신, 자동 네트워크 폴링 없음 | COMPLETE |
| D08 | 동일 상품의 주문 잔량 공유 | `SalePlanner.Build remaining key` | 검토 자산별 분할 행 | PRICE | Y | M | S / N / N | P (범위 한정); F5.Same product across contexts | **COMPLETE** | appid/hash/currency 공유, context별 중복 소비 금지 | COMPLETE |
| D09 | KRW 우선/USD/기타 통화 안전 처리 | `Money; SteamContract.Currency; SteamFeeSchedule` | 지갑 통화/가격 입력 | PRICE/FINAL | P | M | S / N / N | P (범위 한정); F5.Money KRW/USD; F5.currency mismatch | **PARTIAL** | KRW/USD 계산 합성 검증. 기타 통화·불명 wire 단위는 차단. 실제 KRW 지갑 미검증 | PARTIAL |
| D10 | Steam/게임별 최소 수수료/반올림/서버 대조 | `SteamFees; SteamFeeSchedule; SteamListingExecutor.SubmitAsync` | 검토 수수료/지갑 설정 | PRICE/SUBMIT | Y | M | S / N / N | P (범위 한정); F5.server floor; S6.FeeMismatch | **UNVERIFIED** | 정확한 live KRW fee structure·단위·게임별 계약 근거 부족 시 등록 차단 | UNVERIFIED |
| D11 | 정수 금액·최소 단위·수령액/결제액 분리 | `Money; FeeQuote; SteamFeeSchedule` | 검토 단가/Steam·게임 수수료/실수령 | PRICE | Y | M | S / N / N | P (범위 한정); F5.integer wire/overflow; 초기 반올림 | **COMPLETE** | 순수 정수 계약 범위. live 수수료 정확성은 D10 별도 | COMPLETE |
| D12 | 예상 체결/실제 체결 비보장 | `SteamWorkspaceViewModel.Status; SteamReviewRow.PriceBasis` | 보관함 호가/검토 안내 | PRICE | Y | M | — / N / N | —; U5.KRW review; 코드/렌더 감사 | **COMPLETE** | 호가 매칭/즉시 체결을 보장하지 않음 | COMPLETE |
| E01 | 전체 판매 대상 검토 진입 | `SteamWorkspaceViewModel.BuildReview` | 최종 가격 검토 | FINAL | Y | M | S / N / N | P (범위 한정); F5.Full synthetic QR; U5.KRW review | **COMPLETE** | Release 실제 전용 검토, 개발 합성 fixture 분리 | COMPLETE |
| E02 | 항목별 선택 수량 표시 | `SteamReviewRow.Identity/Quantity` | 검토 행 | FINAL | Y | M | S / N / N | P (범위 한정); F5.split; U5.review | **COMPLETE** | 최종 snapshot 수량으로 고정 | COMPLETE |
| E03 | 실제 등록 예정 단가 표시 | `SteamReviewRow.PriceText/Snapshot` | 검토 가격 입력 | FINAL | Y | M | — / N / N | P (범위 한정); S6.PriceAndSettings; U5.price edit | **COMPLETE** | 구매자 단가와 서버 전송 판매자 단가 구분 | COMPLETE |
| E04 | 같은 상품의 가격 단계별 분할 표시 | `SalePlanner.Build; SteamReviewRow` | 검토 분할 행 | FINAL | Y | M | S / N / N | P (범위 한정); F5.3/4/3; U5.review | **COMPLETE** | 분할 가격/수량은 배치에서도 유지 | COMPLETE |
| E05 | 개별 가격 직접 수정/합계 즉시 반영 | `SteamReviewRow.PriceText; SteamWorkspaceViewModel.Totals` | 검토 가격 TextBox | FINAL | Y | M | S / N / N | P (범위 한정); F3.review edit strategy; F5.Review editable; S6.PriceAndSettings | **COMPLETE** | Mock 기준 전환/검토 재생성 시 수동값 초기화; Steam은 현재 검토 내 유지 | COMPLETE |
| E06 | 수수료/실수령 구분 표시 | `SteamReviewRow.Amounts; SteamWorkspaceViewModel.Totals` | 검토 Steam/게임 수수료/실수령 | FINAL | Y | M | S / N / N | P (범위 한정); F5.Review editable totals; U5.review | **COMPLETE** | 표시와 산술 범위. 서버 수수료 정확성은 D10 | COMPLETE |
| E07 | 전체 구매자 총액/수수료/실수령 | `SteamWorkspaceViewModel.Totals` | 검토 상단 합계 | FINAL | Y | M | S / N / N | P (범위 한정); F3.review totals; F5.Review editable | **COMPLETE** | 유효하지 않은 단가가 있으면 확정 합계를 생성하지 않음 | COMPLETE |
| E08 | 이상 가격/급변 경고 및 차단 | `SteamReviewRow.PriceOutlier/PriceWarning; SteamListingExecutor.ValidateBatchAsync` | 검토 경고/최종 재검증 | FINAL | Y | M | S / N / N | P (범위 한정); S6.PriceAndSettings; F4.price movement | **COMPLETE** | 조회 최상위 호가 50~200% 안전 범위. 승인 금액을 자동 변경하지 않음 | COMPLETE |
| E09 | 신선도·판매 가능 여부·통화 최종 재검증 | `BatchValidation; SteamListingExecutor.ValidateBatchAsync/SubmitAsync` | 전체 판매등록 직전 | FINAL/SUBMIT | Y | M | S / N / N | P (범위 한정); F4.preflight; F5.currency mismatch; S6.FeeMismatch | **UNVERIFIED** | 합성 검증 존재. 실제 계정 제한·응답·KRW 계약 검증 미실행 | UNVERIFIED |
| E10 | 판매 대상/보관함 변경 감지 | `SalePlanner.ValidateCurrent; SteamListingExecutor.ValidateBatchAsync` | 최종 승인/가용 수량 | FINAL | Y | M | — / N / N | P (범위 한정); 초기 중복/상충 자산; F3.inventory changed | **COMPLETE** | 자산 identity/메타데이터/수량 변경 시 요청 전 거부하는 로컬/합성 계약 | COMPLETE |
| F01 | 한 번의 전체 승인으로 선택 전체 실제 등록 | `SaleBatchCoordinator.ExecuteAsync; SteamListingExecutor.SubmitAsync` | 전체 판매등록 / Mock 전체 처리 | SUBMIT | P | M | S / N / N | P (범위 한정); 초기 한 번 승인 전체; S6.BatchTrace | **BLOCKED** | 실제 HTTP 어댑터 코드는 있음. Valve 허용 근거 미확보·실계정 미시험, 실판매 완료 아님 | BLOCKED |
| F02 | 불변 배치/행 스냅샷 | `SaleBatchSnapshot; BatchRecordGuard; SteamStateStore.SaveAsync` | 최종 승인 | SUBMIT | Y | M | S / N / N | P (범위 한정); 초기 스냅샷; F5.restart; S6.JournalFailure | **COMPLETE** | 배치·행·가격·수량·통화·시각 고정 | COMPLETE |
| F03 | 항목별 처리/성공/실패/Guard/불명 | `SaleState; SaleBatchCoordinator; SteamWorkspaceViewModel.Results` | 배치 결과 | SUBMIT | Y | M | S / N / N | P (범위 한정); 초기 부분 성공; S6.BatchTrace/PartialFailure | **COMPLETE** | 접수와 게시 상태를 구분하는 상태 머신 범위 | COMPLETE |
| F04 | 중복 클릭/명령/자산 등록 차단 | `SaleBatchCoordinator; SteamStateStore.SaveCommand/AvailableAll` | 승인 버튼/재시작 | SUBMIT | Y | M | S / N / N | P (범위 한정); 초기 중복 클릭; F5.restart; S6.UnknownTrace | **COMPLETE** | 직렬 gate·불변 요청·선영속 의도·예약 검사 | COMPLETE |
| F05 | 부분 실패 시 다른 결과 보존 | `SaleBatchCoordinator.ExecuteAsync/RetryFailedAsync` | 배치 결과/Mock 실패 재시도 | SUBMIT | Y | M | — / N / N | P (범위 한정); S6.PartialFailure; F3.retry failed | **COMPLETE** | 확정 성공·대기·불명을 재등록하지 않음 | COMPLETE |
| F06 | 타임아웃/ID 없는 결과 추측 금지/수동 근거 | `SteamListingExecutor.InspectAsync/ReconcileAsync` | 결과 > 증거 대조 | SUBMIT | Y | M | S / N / N | P (범위 한정); F5.Missing ID/multiple candidates; S6.UnknownTrace | **COMPLETE** | ID 없는 일치 후보는 인과 증거 아님. Unknown과 예약 유지 | COMPLETE |
| F07 | 공식 모바일 Steam Guard 수동 승인 | `SteamContract.Sell; SteamQrAuthentication` | QR 연결/Guard 안내 | AUTH/SUBMIT | P | M | S / N / N | P (범위 한정); F5.Full synthetic QR; U5.QR | **BLOCKED** | 사용자 실제 모바일 승인은 금지 범위로 미실행. 자동 승인·공유 비밀 수집 없음 | BLOCKED |
| F08 | Guard 대기를 게시 완료로 오인 금지 | `SaleState.AwaitingSteamGuard; SteamWorkspaceViewModel.Status` | 배치 결과 안내 | SUBMIT | Y | M | S / N / N | P (범위 한정); S6.BatchTrace; F5.pending | **COMPLETE** | 합성 pending→Listed 재조회 검증 | COMPLETE |
| F09 | 모바일 승인 후 서버 재조회 | `SaleBatchCoordinator.RecoverAsync; SteamListingExecutor.ReconcileAsync` | 상태 재조회 | SUBMIT | Y | M | S / N / N | P (범위 한정); S6.BatchTrace; F5.Publish | **UNVERIFIED** | listingid/가격/수량 검증 코드와 합성 PASS. 실제 승인 후 응답 미검증 | UNVERIFIED |
| F10 | 종료/재시작 복구/전송 중단 | `SqliteDemoStore; SteamStateStore; MainWindow.Closing` | 재실행/상태 재조회 | SUBMIT/LOG | P | M | S / N / N | P (범위 한정); F3.owned crash helper; F5.restart; S6.UnknownTrace | **PARTIAL** | 자체 테스트 프로세스 종료/저널 복구 검증. 실제 전원 차단·OS 디스크 고장 미검증 | PARTIAL |
| F11 | 코드 구현·Mock 성공·실계정 성공 구분 | `Capability; SteamWorkspaceViewModel.ModeText/TradingText` | 상단 배너/차단 버튼 | UI/SUBMIT | Y | M | S / N / N | P (범위 한정); F5.Live policy remains blocked; U5.mode separation | **COMPLETE** | 실계정 검증은 모든 요구사항에서 N | COMPLETE |
| F12 | 기본 IntegrationBlocked/동의만으로 해제 금지 | `SteamTradingPolicy.Blocked; SteamExperimentalWorkspace` | 거래 활성화 비활성 안내 | SUBMIT | Y | M | S / N / N | P (범위 한정); F5.Live policy remains blocked | **COMPLETE** | 일반 앱 factory는 사용자 체크만으로 live write 불가 | COMPLETE |
| G01 | 독립형 Windows C# WPF/MVVM | `MarketCrate.App.csproj; MainWindow; MainViewModel` | 앱/배포 PE | UI | Y | M | — / N / N | P (범위 한정); Release PE; U2.detached STA | **COMPLETE** | 실행·설치·실제 입력은 별도 미검증 | COMPLETE |
| G02 | 독자적 일관된 다크 테마 | `Themes/Tokens/Colors.xaml; Controls.xaml` | 전체 화면 | UI | Y | M | — / N / N | —; U2.control states; U5/U6.render | **COMPLETE** | 자체 렌더 육안 검토 범위 | COMPLETE |
| G03 | 버튼/체크/입력/드롭다운/탭/모달/스크롤 등 템플릿 | `Themes/Controls/Controls.xaml; NumericStepper` | 주요 컨트롤 | UI | P | M | — / N / N | P (범위 한정); U2.template inventory/render | **PARTIAL** | 표준 템플릿 정적/분리 렌더 PASS. 실제 열린 Popup/기본 편집 ContextMenu의 OS 동작은 미검증 | PARTIAL |
| G04 | Hover/Pressed/Focus/Selected/Disabled | `Controls.xaml VisualStateManager/Triggers` | 주요 컨트롤 | UI | P | M | — / N / N | P (범위 한정); U2.control-states.png | **PARTIAL** | 상태 강제 전이만 확인. 실제 마우스/포커스/모니터 입력 미검증 | PARTIAL |
| G05 | 절제된 애니메이션/축소 설정 | `Controls.Motion; Controls.xaml; MainViewModel.ReducedMotion` | 안전/설정 | UI | Y | M | — / N / N | P (범위 한정); U2.motion/states | **COMPLETE** | 프레임별 폴링·GPU 고부하 효과 없음 | COMPLETE |
| G06 | 로딩/오류/빈 데이터/처리 중 안내 | `MainViewModel; SteamWorkspaceViewModel.Status/EmptyAssets; DiagnosticsViewModel` | 상태바/빈 목록/진단 | UI/LOG | P | M | S / N / N | P (범위 한정); U3.loading/error; U6.filters; S6.HttpFailure | **PARTIAL** | Steam 상태바 안내 있음. 모든 네트워크 단계별 전용 시각 상태/취소 카드 UX 추가 필요 | PARTIAL |
| G07 | 대량 스크롤/가상화/바인딩 성능 | `VirtualizingTilePanel; RangeObservableCollection; ListBox styles` | 보관함/판매/진단 목록 | UI | Y | M | S / N / N | P (범위 한정); U3.10000 repeated scroll; F5.10000 adapter rows | **COMPLETE** | 10,000 합성·recycling 범위. 장시간/실기기 계측 아님 | COMPLETE |
| G08 | 키보드 탐색/접근성/툴팁 | `Controls.xaml; MainView.InputError/ReviewChanged; AutomationProperties` | 전체 컨트롤/모달 | UI | P | M | — / N / N | P (범위 한정); U2.stepper/automation names; U6.binding errors | **PARTIAL** | 실제 키보드·스크린리더·열린 툴팁/Popup은 미검증 | PARTIAL |
| G09 | 창 크기/100·125·150·200% DPI 배치 | `MainView.xaml; SteamWorkspaceView.xaml; DiagnosticsView.xaml` | 전체 레이아웃 | UI | P | M | — / N / N | P (범위 한정); U2/U5/U6.render matrices | **PARTIAL** | RenderTargetBitmap의 픽셀 배율만 확인, 실제 모니터 DPI/창 활성화 금지로 미검증 | PARTIAL |
| G10 | SteamHelper/CSlytics UX 참고/비복제 | `docs/REFERENCES.md; UI-DESIGN-SPEC.md` | 검토/상태/결과 | UI | Y | M | — / N / N | —; 기존 출처 조사/라이선스 및 이번 렌더 감사 | **COMPLETE** | 비교 제품 코드/자산 새 편입 없음 | COMPLETE |
| G11 | UI→VM→실제 처리 경로 연결 | `ProductionShellView.xaml; SteamWorkspaceView.xaml; DiagnosticsView.xaml; Observable.Commands` | 모든 주요 버튼 | ALL | P | M | — / N / N | P (범위 한정); U5/U6.command bindings; S6.DiagnosticUi | **PARTIAL** | ProductAuditUiTests가 제품3뷰의 모든 Command Binding/공개 ICommand 객체를 대조. 정확한 평균·전체 자동 발견·live 재시도 미완성. 이미지/수동 카탈로그/추정 지표는 7차에 연결.  비활성 거래 버튼은 정책 차단이며 가짜 성공 아님 | PARTIAL |

## 6차 진단 요구 추적 (최초 75개 집계와 별도)

| ID | 구현/진입점 | 검증 | 판정/제한 |
|---|---|---|---|
| LOG-01 | App.OnStartup/OnExit/Fault, DiagnosticHub, 앱 버전/commit/session/UTC | S6.Levels; 코드 감사 | PARTIAL: 강제 OS 종료 직전 tail/실제 전역 예외 이벤트는 미실행 |
| LOG-02 | AsyncLocal operation/correlation/batch/item + 고정 EventId/RequirementId | S6.BatchTrace/UnknownTrace/PartialFailure | COMPLETE(합성); parser는 OperationId로 HTTP와 연결, 모든 외부 하위 호출에 별도 request ID가 있는 것은 아님 |
| LOG-03 | HTTP endpoint allowlist/status/elapsed/phase/error/schema | S6.HttpFailure/Timeout/Parsing | COMPLETE(합성); 원문/URL/헤더 저장 없음 |
| LOG-04 | SQLite transaction/SQL 오류/저널 commit/복구 | S6.StorageLocked/StorageFault/SqliteFull/JournalFailure | PARTIAL: 로컬 모든 직렬화/lease 실패가 별도 상세 코드인 것은 아님 |
| LOG-05 | 생성 시 제외 + sink/export 재정제, exception message/path 제거 | S6.Privacy/BatchTrace | COMPLETE(명시적 whitelist 테스트 범위), 외부 보안 감사 아님 |
| LOG-06 | bounded single writer, drop/오류 health, INFO 기본/상세 모드 | S6.Levels/QueuePressure/DiskFailure | COMPLETE(합성); 종료 flush는 최선 노력, 모든 tail 보장 안 함 |
| LOG-07 | 일자/크기 회전·기간/파일수/용량 상한·수동 정리 | S6.Rotation | COMPLETE(유한 테스트); 실제 14일 경과/달력 변경 실사용은 미검증 |
| LOG-08 | DiagnosticsView/ViewModel, UTC/KST/오류/operation 검색/폴더 명령 | S6.DiagnosticUi; U6.render | COMPLETE(내부 바인딩); Explorer 실제 실행·사용자 입력 미실행 |
| LOG-09 | 범위 검토/재동의/로컬 ZIP/재정제/원자적 이름 변경 | S6.Privacy/Rotation/DiagnosticUi | COMPLETE(합성); DB/원문/설정 미포함, 자동 업로드 없음 |
| LOG-10 | 상세 모드 사용자 설정 복원 | S6.PriceAndSettings | COMPLETE(격리 DB); 인증 관련 설정은 저장하지 않음 |
| LOG-11 | UI command/page/input/binding 오류 | Observable.cs/MainView/BindingDiagnostics | PARTIAL: binding 텍스트 전부 폐기, 프로세스별 최대20건 제한으로 일부 위치 정보 부족 |
| SYS-01 | 안전 기본값/실패 시 재전송 금지/타 앱 비간섭 | S6.DiskFailure/UnknownCancel; 정적 검사 | COMPLETE(현재 코드 경계), 타 프로세스 실제 동시실행을 감지/검사하지 않음 |
| SYS-02 | 사용자별 저장/마이그레이션/정합성/업데이트 경로 | 기존 SQLite 재열기/crash; S6.StorageFault | PARTIAL: 실제 installer/OS 업데이트·전원 차단 미검증 |

## 수정 우선순위와 차기 차단 요소

1. **live 계약/허용 근거**: 실판매 기본 차단을 유지하고 허용 근거와 별도 사용자의 실계정 시험 승인이 준비된 뒤만 진행한다. QR/실조회도 비공식이며 이번에는 실행하지 않았다.
2. **자료 완전성**: 이미지/수동 카탈로그/추정 지표를 구현했지만 계정 전체 자동 발견·정확한 평균 원자료는 없다. 이미지/CDN은 opt-in·인증 분리·바이트/픽셀/호스트 제한·합성 검증을 적용했다. 실제 CDN/메타데이터는 여전히 미검증이며 WPF URI 자동 로드는 없다.
3. **상태 증거**: ID 없는 등록, 외부 동시 등록, 스택 반환/ID 변경의 인과 증거 부족은 추정으로 해결하지 않는다. live 실패 재시도와 구체 취소 UX도 남았다.
4. **실사용 QA**: 물리 키보드/마우스, 실제 Popup/모니터 DPI, 설치/장기 안정성/OS 전원 차단은 별도 NOT VERIFIED다.

추가 기능은 이 표의 ID를 유지하고 DIAGNOSTICS 이벤트와 QA-MATRIX 테스트를 같은 변경에서 갱신한다. 기존 COMPLETE도 검증 범위를 넘어 확대하지 않는다.


## 7차 남은 판정 (ID 유지)

- PARTIAL(15): A06, B05, B09, B10, B11, C09, C10, D09, F10, G03, G04, G06, G08, G09, G11.
- BLOCKED(4): B08, D01, F01, F07.
- UNVERIFIED(9): A01, A02, A04, B01, B03, C01, D10, E09, F09.
- MISSING(0). S7는 `tests/MarketCrate.Tests/SeventhStageTests.cs`, U7는 `tests/MarketCrate.UiTests/SeventhStageUiTests.cs`. 현재 실제 서버 응답/실계정 검증은 모든 항목 N이다.

저장한 관리 의도/반환 대조는 대상 app/context를 선택·갱신한 환경에서 수행해야 한다. 전체 계정 자동 발견/공식 게임 로고 자동 조회 대신 수동 이름·관측 App 이름/대표 이미지이며, 실사용 다른 context 관리 UX는 아직 G11/B10 PARTIAL 범위다.


## 0.20.1 후속 보완 (2026-10-29 KST)

0.20.1 보완: C02/C03 게임 카탈로그에 사용자 요청 TBH App3678970 추가(context2 미검증 후보), 사용자가 제거하면 재추가하지 않음. B/G 판매 화면 빈 재등록 계획 선택창 숨김, D09/D10 수수료 조회 canonical 이동 제한/소유 증거 확인·실패 상태 보완. 합성 기능324/WPF66/배포19 PASS. 실제 서버 수수료·TBH 보관함·거래/물리 GUI 근거는 늘지 않았으므로 기존75개 ID와 판정47/16/0/3/9 유지.

0.20.3: 최초75개 ID/판정47 COMPLETE·16 PARTIAL·0 MISSING·3 BLOCKED·9 UNVERIFIED 유지. D10의 classic 앱/지갑 근거가 확인된 경우 선택적 item market_fee 부재 처리, F01/E09의 기본 실거래 선택→최종 검토 사전검증, B04 이미지 기본 ON과 로그아웃 중단, G08 실패 단계/최근 실패 진단 연결 보완. 기능333/333·비표시WPF66/66·배포19/19 PASS지만 실제 사용자 활성화 실패 작업이 ZIP에 없으며 실계정 수수료/거래를 COMPLETE로 승격하지 않는다.

0.20.4: 기존75개 ID/판정47 COMPLETE·16 PARTIAL·0 MISSING·3 BLOCKED·9 UNVERIFIED 유지. A 세션 CSRF 현재 값 사용, F 제출 HTTP400/Unknown과 실제 승인 대기 분리, G 사용자 상태 안내 및 중복 읽기/이동 제거. 기능341/WPF66/배포19 합성·정적 PASS. 사용자 실제 요청1건 HTTP400은 전송 시도 증거이며 접수/게시/Guard 성공 증거가 아니다. 기존 원문이 없어 실제400 원인과 새 버전 판매 성공은 미검증.

## 0.20.5 전체 경로 재확인

기존75개 ID와47 COMPLETE/16 PARTIAL/0 MISSING/3 BLOCKED/9 UNVERIFIED 유지. 이번 AdditionalGame/ApprovalChannels/EmailJournal/PendingWithoutId/EmptyCancellation 및 자체 UI 회귀로 다중 게임 준비 누락·승인 채널 혼동·빈 취소 ACK 처리 결함을 수정했다. 전체 UI→서비스→어댑터→저널→결과 경로와 미지원 차이는 OPEN-SOURCE-FLOW-AUDIT.md에 매핑했다. 합성346/WPF67 통과만으로 실계정·수수료·실게시 항목을 승격하지 않는다.


## 0.20.7 서버 상태 동기화

0.20.7: 최초75개 ID/판정47 COMPLETE·16 PARTIAL·0 MISSING·3 BLOCKED·9 UNVERIFIED를 유지한다. C09/C10/C13·F05/F09의 서버 수량/로컬 전송 이력 분리와 알려진 서버 등록 상태 동기화를 보완했다. 기존 UI 로컬 차감 기대2건을 서버 수량과 별도 중복 전송 보호 검사로 갱신하고 기능350/WPF68/배포19/정리9 통과. 두 프로젝트의 모든 기능과 동등하거나 실서버/실판매 검증이 완료됐다고 판정하지 않는다.


## 0.20.8 단일 가격과 수량 계약

사용자의 평균가 옵션 제거 요청으로 D01/D02의 평균·기간 선택 제품 UI를 철회했다. 최초75개 ID와 이전 범위 판정47/16/0/3/9는 이력 추적을 위해 유지하지만 철회 항목을 현재 제품에서 이용 가능하다고 해석하지 않는다. 이력·중앙값 개발 테스트/enum은 과거저널 호환을 위해 남긴다. C09/C10의 단일 가격 classic 스택 수량 계약과 F09의 ID 없는 자산 관측을 보완했지만 실계정 입증 없이 승격하지 않는다.


## 0.20.9 활성화와 전송 검사

0.20.9: C10/F05의 서버 자산 준비 검사와 로컬 중복 전송 보호가 혼재하던 경로를 수정했다. 기존 Unknown 재시작 후 준비 허용/최종 전송 거부 및 실제 marketable=false 준비 거부의 정상·오류 경로를 확인한다.75개 ID와47/16/0/3/9 유지, 실계정 원인 미확정/승격 없음.

## 0.20.10 서버 동기화·새 승인

F01/F05/F10/C10: SaleBatchCoordinator→IBatchHistoryJournal→SteamStateStore.FindBatchAsync/SaveAsync와 SteamListingExecutor.ValidateServerQuantity→실제 inventory/orders→intent→Submit 경로를 보완했다. 최초75개 ID/판정47/16/0/3/9 유지. 합성 검사와 현재 사용자의 실제 일괄 판매 실패 원인을 구분하며 실판매 성공으로 승격하지 않는다.

## 0.20.11 승인 경로·공통 장터 판별

B01/B04/B09/F01/F03/F06/F09: SteamMarketMembership→Orders.Presence→Executor 검증/취소 반환 보호→VM 승인 안내→Recover→서버 listing ID/자산/가격/수량 대조를 보완했다. 최초75개 ID와47 COMPLETE/16 PARTIAL/0 MISSING/3 BLOCKED/9 UNVERIFIED를 유지한다. 상세 메타데이터 미확인은 일반 판매 행을 생성하지 않고 관측 및 사용자 안내로 제공하며 실계정 게시/Guard를 COMPLETE로 승격하지 않는다.

## 0.20.12 기능 동등성 감사

C03/C04/F03/F04/F05/F09/F10의 비장터 hash 누락·플래그 호환/전송 전 중단/ID 없는 현재 게시 안내를 보완했다. 최초75개 ID/판정47/16/0/3/9 유지. 합성 wire 일치를 전체100% 또는실계정 성공으로 승격하지 않는다.

## 0.20.13 — 최종 릴리즈 신뢰도 감사 (2026-10-31 KST)

같은 BatchId에 변경된 계정/가격을 전달해 이전 승인을 재사용하는 경로, 취소 후 과거 로컬 전송 기록으로 새 실제 보유 자산 검토가 막히는 경로, 타 계정 AssetKey의 pending 수량 혼합을 수정했다. 서버 inventory와 active/pending membership을 기준으로 새 대상·새 가격·새 최종 승인을 요구하며 기존 Unknown/원요청 ID/감사 저널은 보존한다. 관련 코드·수정 전/후 증거·출하 기준은 [RELEASE-READINESS](RELEASE-READINESS.md)를 따른다.

번들 Core/WPF10.0.14와 Microsoft.Data.Sqlite/ProtectedData10.0.14·SQLitePCLRaw2.1.14(native3.54.0)를 사용한다. 호스트 SDK/런타임 설치·설정 변경0. 구 provider 생성→신 provider 읽기/쓰기→구 provider 재읽기 합성 DB 호환 PASS. NuGet advisory는 명시적 공개 source/프로젝트 coverage를 검사하며 빈 source를 PASS로 판정하지 않는다. 패키지 감사와 런타임 보안 최소 버전 검사는 별개다.

최초75개47 COMPLETE/16 PARTIAL/0 MISSING/3 BLOCKED/9 UNVERIFIED를 유지한다. 새 실제 GUI·실계정·판매/취소/Guard0. 이전 사용자 관측을 새 릴리즈 실증으로 확대하지 않는다. Auth/Preferences/거래 저널/진단/롤백 삭제0. 실제 전용 출하와 단일 최종 승인 전체 배치, DPAPI, 정제 JSONL/ZIP, Unknown 비재전송, 타 프로세스 비간섭을 유지한다.

## 0.20.14 사용자 경로 추가 증거

기존75개 ID와47 COMPLETE/16 PARTIAL/0 MISSING/3 BLOCKED/9 UNVERIFIED를 유지한다. C07 수량, C13 선택 보존/명시적 제거, E05/E09 가격·합계/사전검증, F01/F03/F05/F10 단일 승인/중복/Unknown/복구, G08 입력·접근성의 새 증거는 USER-JOURNEY-QA(UJ01–14/UIJ01–04) 및 UserJourneyTests/UserInputUiTests다. 이전 실패를 직접 수정했으나 합성/WPF 성공만으로 실계정 항목을 승격하지 않는다.
