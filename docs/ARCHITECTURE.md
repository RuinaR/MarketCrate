# MarketCrate — 아키텍처 (최초 Mock 구현 / 후속 계획)

## 0.20 제품 경로 보완

UI→SteamWorkspaceViewModel→기존 서비스 구조 유지. 상품별 시세 행 갱신은 Bulk로 묶어 항목마다 전체 선택/정산을 재계산하지 않는다. UI 화면 캐시와 retainedSelections를 분리해 view eviction이 선택 배치 대상을 누락하지 않도록 했다. BatchUiProgress는 Coordinator가 이어지는 호출 UI 문맥에서 변경된 결과 행만 갱신하며 늦은 다른 계정/로그아웃 결과를 버린다. NavigationIndex는 기존 WorkPage 인덱스와 새 표시 순서를 분리한다. 카드 보기는 기존 VirtualizingTilePanel을 실제 보관함에 연결한다.

SteamStateStore.StageRelistApproval은 검토 스냅샷/계정/상품별 수량/취소 접수만 메모리에서 준비한다. 실제 Coordinator 사전검증 후 최초 SaveAsync가 새 batch와 relist.SubmissionBatch를 동일 트랜잭션으로 기록한다. 실패는 두 기록 모두 rollback이며 SQLite schema3/기존 데이터 형식·DPAPI 저장 형식은 유지한다. 0.19에서 이미 제출 시도로 묶인 과거 계획을 추측으로 해제하지 않는다.

SteamAccess.CancellationOnly+SteamTradingPolicy.CancellationOnly를 공유 가드로 사용한다. CanCancel은 인증·명시적 동의·계정 결속·서버 제한을, CanExecute는 그 조건과 판매 허용을 추가 확인한다. 취소 전용 활성화는 최신 자기 판매목록만 읽으며 실제 POST는 별도 전체 검토 승인 후다. 외부 등록/취소 전용은 AvailableBefore=-1로 반환 미확인을 보존하며 취소를 위해 수수료/보관함을 강제 조회하지 않는다. 새 판매는 기존 전체 가격·자산·수수료 계약 검증을 계속 요구한다.


## 0.19.0 추가 — 외부 등록의 새 자산 재판매

SteamMarketHelper의 취소→보관함 재조회→같은 상품의 새 자산 ID 선택 방식을 실제 서비스에 연결했다. 전체 취소 승인 후 서버 접수·목록 부재를 확인하고, 실제 보관함·가용 수량·수수료·공유 호가로 새 대상을 제안한다. 원래 아이템의 반환 확정은 아니며 다른 기존 보유분일 수 있음을 표시한다. 새 실제 자산 ID·수량·가격을 전체 검토해 판매등록 한 번 승인하면 기존 Coordinator가 배치 처리한다. 항목별 추가 판매 클릭은 없다. SQLite 취소 계획/새 배치 연결을 보존하고 불명 취소·원래 예약은 추측으로 해제하지 않는다. 최근 미제출 계획은 사용자 요청 시 재조회하며 자동 거래하지 않는다. 개별 수량1, 최대100등록/10상품만 지원한다.

추가 합성6개/비표시WPF1개: 변경ID·재시작 후 새 승인, 취소만으로 판매0, Unknown 재전송0, 전체 수량 부족·호가 변경 차단, 계획 불변·OperationId/비밀정보 보호. 최종 기능305/305·WPF61/61·배포19/19 PASS, 경고/오류0. 취소/새 대상 검토125%·200% RTB를 직접 검토했다. 실제 계정/수수료·판매/물리 입력은 미검증이다. 상세 호출 경로·보존/재시도 제약은 [판매 실행 계약](SALE-EXECUTION-SPEC.md)의 0.19 추가 절을 따른다.


## 0.19.0 카탈로그·일괄 재등록

SteamListingRepriceViewModel partial은 기존 SalePlanner/SteamFees/MarketData/Executor를 사용한다. 가격을 상품별 한 번 조회하고 같은 상품 잔량을 공유한다. 5분 검토 기한/전체 서버 대조/계정 지갑·수수료 확인 후에만 순차 RepriceAsync를 호출한다. 취소·반환/등록 의도는 기존 SQLite에 선기록하며 원래 명령 상태로 UI 결과를 표시한다. 외부 등록은 근거 없이 취소부터 하지 않는다.

InventoryScope.Pinned는 카탈로그 JSON의 추가 선택/관측 메타데이터다. 이전 데이터는 false로 읽고 옛 기본 추천만 숨김 보존하며 수동/관측 항목은 true로 저장한다. 거래 DB 스키마·Auth 형식 변경/초기화 없음. 개발3추천은 MARKETCRATE_DEMO에서만 생성한다.

BusyIndicator는 자체 ControlTemplate/ProgressBar AutomationPeer이며 활성·표시·축소 상태의 Storyboard 시작/해제를 사용한다. 프레임별 폴링·전역 입력·다른 프로세스 접근이 없다. 새 실제 전용 명령도 기본 거래 OFF/계약 차단을 유지한다.


## 0.18 — 모드 기반 수수료와 제품/개발 구성

정상 경로는 SteamWorkspace.LoadFees/LoadWallet→SteamExperimentalWorkspace→SteamFees.FetchResolvedAsync→FeeContractResolver→(WalletContext 또는 explicit new API)→SteamFeeSchedule→자산 publisher 대조→검토/최종 executor 재검증이다. 세션별 wallet 관측은 별도 메타데이터로 보존하며 견적/권한과 동일시하지 않는다. 미확인 모드에서 예전 공식이나 고정 수수료로 폴백하지 않는다. 재가격에서도 같은 경로를 사용한다.

Production=true에서는 DevelopmentReadCheckView의Page/BAML/code-behind, SteamReadSelfCheckViewModel, SteamReadSelfCheck/계획·결과 모델을 제외한다. partial 개발 hook은 구현이 없을 때 호출도 제거된다. 공통 WorkPage는 별도 SteamNavigationViewModel에 두고 정식5페이지/개발6페이지 범위를 구분한다. 테스트는 기존 dev ProjectReference를 사용해 전 회귀를 보존한다. Test-ProductionAssembly는 Mock과 자가검증 타입/리소스 부재 및 실제 resolver/Auth/서비스 존재를 PE만 읽어 검사한다.

단일 최종 승인·불변 배치·순차 요청·저널 선기록·Unknown/예약·모바일 승인/서버 게시 분리·반환 인과관계는 변경하지 않았다. DB/Auth 스키마와 사용자 데이터 이관/초기화 없음. 상세 현재 공식 계산은 STEAM-INTEGRATION-FEASIBILITY 참조.


## 16차 — 동의 기반 인증 저장과 복원

App→ProductionShellViewModel(authDirectory=DataRoot/Auth)→SteamWorkspaceViewModel.InitializePersistence. Loaded에서 RestoreRememberedAsync를 호출하되 정상 저장 동의/파일이 있고 실행당 최초일 때만 Run한다. 기본 생성은 네트워크를 보내지 않는다. 개발/비표시 tests는 별도 authDirectory 또는 저장소 없는 composition을 사용한다.

SteamAuthStore는 Preferences/SteamStateStore와 별도이며 DPAPI CurrentUser·사용자 SID ACL·경로/파일 경계·암호화 pending/차단 표식/lease를 소유한다. TakeForRestore는 네트워크 전에 disarm, SteamSavedLogin(secret buffer)→Authentication.RestoreAsync→JWT claims→finalizelogin→Community transfer→계정 쿠키 검사→SaveRememberedLogin(expected cipher fingerprint) 후만 rearm한다. 파일/저장 실패는 실제 인증 실패와 별도 안내하고 평문/쿠키를 저장하지 않는다.

명시적 Logout=EndSession(true)→로컬 저장 삭제·메모리 종료·거래권한 철회. Dispose=EndSession(false)→메모리 종료만 하고 사용자가 선택한 저장은 유지한다. 거래 실행/과거 승인 재생/자동 목록 갱신은 없고 RefreshRequests는 기존 저널을 읽기만 한다. 계정마다 증거를 다시 검증하며 Unknown/예약은 유지한다. 자동 refresh-token 회전·서버 철회는 이 버전의 지원 계약이 아니다. 자세한 상태/파일/검증은 SESSION-PERSISTENCE 참조.

## 0.16.0 생산 구성 경계

App→ProductionShellViewModel→SteamWorkspaceViewModel/DiagnosticsViewModel만 일반 시작 경로다. 소유권은 App.owner→Shell.Dispose→Steam.Dispose/preferences.Dispose로 정리하며 기본 생성은 Steam 로그인/HTTP/거래를 시작하지 않는다. Demo 저장소 lease에 실제 앱 시작이 종속되지 않는다. 비밀 없는 SQLite 설정과 거래 저널은 별도이다.

Wallet HTTP는 root→정확한 eligibilitycheck/goto→고정 root 복귀 상태를 추적한다. 서버 CookieContainer 수신과 동일 origin을 유지하며 상태/반복/권한 제한을 우회하지 않는다. Release에서 Mock 실행 코드·데이터 모델·legacy 화면은 미컴파일, 개발 테스트는 분리된 참조 속성으로 유지한다. enum/기존 저널 JSON 계약을 임의 재번호화하지 않는다.

## 14차 읽기 증거 계층

### 15차 세션 조건부 거래

SteamTradingViewModel.PrepareTradingAsync→Workspace.EnableUserTradingAsync는 **읽기 전용 preflight**이며 POST를 보내지 않는다. 기본policyBlocked를 세션별 UserInitiated/VerifiedApps로 바꾸는 유일한 일반앱경로다. Access.TradingAccount와 QR계정이 일치해야 Executor.CanExecute가 참이다. ContractTest는 일반UI에 노출하지 않고 enabled policy 생성자 주입도 합성 handler 경계에서만 허용한다. 동의철회·인증재시작·로그아웃은 binding을 제거하고 활성화/인증비밀은 SQLite에 저장하지 않는다.

executor는 scope별 inventory/provider와 앱별fee를 검증한 뒤 한 coordinator/저널로 처리한다. optional OrderbookDigest는 SaleLineSnapshot 및 SteamRepriceIntent에 저장하며 가격/잔량/통화의 불변증거다. 기존JSON의 누락필드는 null로 복원하고 실제 사용자모드에서 새검토 없이 실행하지 않는다. SQLite v3 schema/기존거래 데이터 초기화 없음. 계정이 다른 활성저널은 새배치로 덮어쓰지 않는다. 기존 계정의 예약이 보수적으로 새계정 가용량을 제한할 수 있어 완전한 다계정 전환 제품지원으로 주장하지 않는다.

QR기록/메모리세션, SteamHttp 전역Halted, endpoint별 FeatureFailure(Authorization), 개별ReadState/ReadCheck를 분리한다. WalletObservation→strict SteamWallet 승격은 완전한 단위/증분정보가 있어야 가능. rates-only fee는 strict contract 대체 불가. InventoryPage VerifiedEmpty/ReportedTotal/More로 오류·빈·부분페이지 구분. UI→SelfCheck→Provider→HTTP→고정진단→요약/상세 경로이며 거래저널·복구구조 변경 없음.

## 13차 읽기 자가검증 경계

SteamReadSelfCheck는 기존 workspace의 인증 기록/목록·보관함 probe/지갑/fee/가격·이력/이미지 adapter만 사용한다. 로그인 시작·executor·store mutation은 호출하지 않는다. 승인 때 고정한 SteamReadCheckPlan은 게임/context·최대 한 자산·이미지 참조·7개 범위를 담고 ToString은 정제한다. metadata 자체를 로그로 직렬화하지 않는다.

Probe는 GetAsync/RefreshAsync와 같은 parser·HTTP 직렬 큐/간격·latch를 사용하되 첫 페이지만 요청하고 완전 스냅샷으로 캐시를 덮지 않는다. 가격/이미지 캐시는 기존 정책을 유지한다. wallet과 fee를 분리해 의존 실패를 실제 무요청/미검증으로 보여준다. UI는 WorkPage 양방향 상태와 전용 결과 목록을 사용하고 로그아웃 중 늦은 callback을 폐기한다. Source/At/Outcome이 거래 저널이나 LIVE readiness를 대체하지 않는다. SQLite 스키마·사용자 데이터 경로 변경 없음.

## 12차 세션/요청/조회 상태 경계

SteamQrAuthentication.HasSession은 성공한 finalize/Community transfer와 메모리 세션 존재, Connected는 인증 만료 여부를 나타낸다. 전역 SteamHttp.Halted는 요청 안전 latch이며 이 값을 로그인 존재와 동일시하지 않는다. VM.CanRead=Connected && !RequestsBlocked, 실행기 CanExecute/CanReconcile은 별도로 http.Halted=null까지 검사한다. 권한을 우회하거나 latch를 지우는 API는 추가하지 않았다.

SteamReadStateViewModel partial은 5개 독립 상태와 사용자 요약을 담당한다. Run은 명령별 채널, IndependentRead는 기존 Load의 fee/listings 부분 성공을 처리한다. 일반 read schema/404/GET transport와 세션 만료/서버 제한을 구분하며 Error는 읽기 실패로 LoginState를 덮어쓰지 않는다. stale fee/Inventory/Prices는 검토 명령을 차단한다. 쓰기 어댑터의 불변 snapshot/저널/Unknown/예약/중복 방지는 유지한다.

게임별 row 뷰 캐시는 계정 VM 내 최대8게임·20,000행, 선택/수량 기억은 별도 asset-key 사전이다. ScrollMemory는 뷰의 game key별100개 이하 offset을 기억하며 unload/reload/키 변경 때만 복원한다. OS 입력/타 창/계정 데이터 영속 저장을 사용하지 않는다. 이 캐시는 거래 판단 원본이 아니며 서버 재검증을 생략하지 않는다. SQLite 스키마·사용자 저장 경로 변경 없음.

7차 추가 보호: 기간 중앙값의 구매자/판매자 결제액 역할과 통화 해석은 live 미검증이므로 `SteamTradingPolicy.MedianPriceContractSupported`가 별도로 필요하며 기본 false다. 사용자의 최종 클릭/정수 계산 성공으로 이를 true로 추정하지 않는다. 합성 positive 경로에서만 명시적으로 켜 검증했다.

## 7차 확장 (기존 MVVM/저널 유지)

- Image metadata를 InventoryAsset/SaleLine의 거래 equality에 추가하지 않고 provider side maps에 둔다. ItemThumbnail은 실현된 컨트롤만 구독, unload/data recycling/Loader 변경 때 구독 취소/decoded Source 해제. byte 캐시는 별도 무인증 SteamImageCache, single-flight subscriber refcount와 bounded LRU/queue, WIC decode는 바이트/픽셀 검증 후 worker→freeze→소유 Dispatcher 반영. cache 실패는 주문 상태에 영향 없음.
- SteamCatalogViewModel partial은 Observable Games/InventoryScope를 다룬다. validated app/context/name만 account-scoped catalog kind로 SteamStateStore v3에 저장. command/result/return/reprice kind와 분리하고 기존 journal 행/버전 삭제·재작성 없음. 현재 scope/관측된 판매목록만 반영하며 과거 상태/건수는 재시작에 확정값으로 복원하지 않음.
- PriceBasis/PriceEvidence는 exact average와 median estimate를 분리한다. 허용 정수 금액·정렬된 기간 표본/거래량으로 하위 가중 중앙값 산출, 원자료 없으면 exact average 차단. immutable line.Evidence에는 source/method/window/count/volume/SHA256 digest를 고정, fresh 검토/서버 재조회 변화 검사. 실제 최신 계약 보장은 아님. 원본 버킷/HTTP 본문은 저널·로그에 저장하지 않음.
- 네트워크 재검증은 같은 상품/기간별 한 번, 결과는 line마다 검증하며 끝에 snapshot 시간도 재검사. 기존 한 번 승인/직렬 제출/durable intent/Unknown 보존 그대로. 근거 없는 stack 반환 proof 추가 안 함.

## 6차 — 진단 경계와 재가격 승인 revision

- Core `Diagnostics.cs`는 typed 이벤트와 AsyncLocal 연결 scope만 정의한다. Infrastructure `Diagnostics/JsonDiagnosticLog.cs`가 Channel single writer/retention/reviewed export를 맡고 `DiagnosticRecordSafety.cs`가 생성·export 이중 whitelist를 적용한다. App은 startup 전에 로거를 구성하고 lifecycle/global exception/binding 이벤트를 연결한다. **로그와 transaction authority를 분리**하며 진단 실패는 주문 흐름에 예외를 전파하거나 재시도를 유발하지 않는다. 구조/보존/위험은 [DIAGNOSTICS](DIAGNOSTICS.md).
- OperationId는 명령→VM→adapter→저장소 문맥을 공유한다. HTTP request/response는 별도 CorrelationId, BatchId는 snapshot GUID, 내부 항목은 로컬 명령 key의 단방향 digest다. 실제 ID·헤더·QR·원문 exception message를 구조화 객체에 받지 않는다. 앱 로그는 user LocalAppData, 초기 INFO/사용자 진단 옵션 복원.
- SteamWorkspaceVM은 전체 모델과 필터된 virtualized 뷰를 분리하고, 현재 실행의 asset key 선택/수량 dictionary를 보존한다. 갱신은 최신 가용량에 맞춰 clamp, 인증 변경 시 전체 선택 메모리 폐기. 가격 cache는 새 승인에 재검증한다.
- `SteamRepriceIntent`에 optional CancellationKey/SupersededBy, Superseded 상태를 추가한다. 취소가 확정된 미전송 의도만 새 승인으로 revision. SQLite transaction에서 이전 의도 폐기/새 의도/audit를 함께 저장한다. 새 의도는 원 취소 proof를 재사용하며 후속 request key를 전송 전에 보존한다. **Steam schema v3**: 기존 v0/v1/v2 row/명령/불명 증거를 그대로 유지하고 header만 올린다. 구 v2 앱은 v3 DB를 거부하여 폐기 승인을 재개할 수 없다. Demo DB는 기존 v2 유지.
- 실제 이미지/게임 자동 발견/정확한 live VWAP/외부 또는 stack 취소 반환 인과 증거/live 확정 실패 재시도는 완료하지 않았다. [요구사항별 코드 경로](REQUIREMENTS-TRACEABILITY.md)에 PARTIAL/MISSING/BLOCKED/UNVERIFIED 근거를 고정한다.

## 5차 UI/통화/예약/대조 — 2026-10-28 (최신)

- 기존 Demo MainViewModel을 재작성하지 않고 지연 factory의 `SteamWorkspaceViewModel`/독립 UserControl을 추가했다. 시작/탭 생성은 네트워크·실저널을 생성하지 않는다. 실제 연결 화면은 Mock 사이드바/합계/상태를 숨기고 별도 출처/시각을 사용한다.
- QR은 MIT 고정 소스의 순수 module matrix를 자체 DrawingContext로 렌더한다. URI를 외부 서비스/바인딩 로그에 내보내지 않는다. 인증 확인/갱신은 사용자 요청이며 background timer 없음. 취소·로그아웃·늦은 응답 및 거래 동의 철회를 처리한다.
- Core `Money`: KRW 원/USD cent, wire 변환 별도. `SteamFeeSchedule`: 서버 floor/increment·정수 millionths·cap, checked 계산, floor/nearest increment 규칙, 정확한 역산 불가 금액 거절. SteamFees는 공식 최신 ajaxfeestructure 소비 코드 기반이며 미확인 응답/추가 fee는 차단한다. 기존 Demo Pricing은 별도 유지한다.
- `SalePlanner` 공유 호가 key에서 context를 제거했다(App/상품/통화가 동일하면 같은 장터 잔량). AssetKey의 소유권 식별은 App/context/asset로 유지한다. stack quantity·로컬 의도/반환 증거·서버 보관함 상한을 함께 사용해 보수적 가용 수량을 계산한다.
- `SteamTradingPolicy`: 실제 실행 근거/capability와 session risk/trading 동의를 분리, 기본 Blocked. 스택 amount capability 별도. 테스트 policy는 합성 handler에만 사용한다. 사용자 체크를 Valve 허가로 해석하지 않는다.
- Steam store v2는 v1 보존 이관, 기본값이 없는 과거 correlation 증거는 없는 것으로 처리한다. command/reprice transition audit를 같은 transaction에 저장한다. 확정 batch archive는 snapshot을 보관하고 command/evidence/reservation을 삭제하지 않는다. 모호한 활성 배치는 archive하지 않는다.
- 제출 전 wallet/fee/current assets/book 재검증, 서버 price/fee 모순 시 해당 요청 Unknown 및 후속 중단. listingid 없는 요청은 before IDs/SentAt/자산·수량·가격·시각별 후보 report를 보존하지만 인과관계 증명 없이 자동 확정하지 않는다.
- 취소는 접수·완전한 목록 부재·원래 비스택 단일 자산의 동일 ID 반환 proof로만 예약 해제한다. 재가격은 immutable target→취소→반환 proof→새 preflight→replacement key 선행 저장→제출. 불명/ID 교체/5분 stale은 중단, 명시적 재개에서도 기존 replacement를 중복 실행하지 않는다.
- 한 game/context의 활성 배치, 명시적인 세 게임 scope, stale 재가격 target revision/새 ID 매핑/실제 계정 eligibility/라이브 실패 retry 분류는 남은 제한이다. 자세한 최신 근거는 STEAM-INTEGRATION-FEASIBILITY.

## 4차 실험적 Steam 경계 — 2026-10-28 (최신)

- Infrastructure/Steam에 독립 구현. `SteamExperimentalWorkspace`는 개발자 composition이며 App.OnStartup은 여전히 Demo만 구성한다. 새 패키지/외부 소스 복사 없음.
- `SteamAccess`: 매 세션 위험별 명시적 동의 및 reads/trading 분리, 저장/자동 복원 없음. `SteamQrAuthentication`: WebBrowser QR Begin/Poll→finalize→Community settoken, 자체 CookieContainer만. 비밀번호/외부 쿠키/프로세스/Steam Guard 비밀 접근 없음. QR WPF UI는 미완성이다.
- `SteamHttp`: 고정 HTTPS endpoint·redirect 거절·proxy OFF·직렬3초·25초 timeout·8MiB 제한·단일 latch·자동 retry 없음. HttpMessageHandler 주입 계약 테스트는 실제 네트워크를 대체한다.
- 기존 IInventoryProvider/IMarketDataProvider/IMarketOrderProvider/IMarketListingExecutor를 구현한다. 동기 가격/목록 getter는 cache만 반환하고 명시적 비동기 갱신으로 읽는다. 정확한 VWAP를 만들지 않고 중간가격 이력은 별도 타입으로 둔다.
- `IAuthorizedListingExecutor`는 실행/대조 가능 여부와 최종 async preflight를 제공한다. coordinator는 Mock 또는 이 계약을 충족한 executor만 받아 기존 단일 승인·intent-first 흐름을 재사용한다. live retry는 아직 차단한다. Domain 통화 검사는 KRW와 USD를 허용하지만 실험 제출은 exact USD만, 기존 KRW Mock 산식은 유지한다.
- `SteamStateStore`: Demo와 다른 디렉터리의 독점 SQLite/FULL/user_version1. immutable 활성 배치 헤더+변경 result 행/command 의도 저장. 토큰·쿠키·raw response·서버 메시지·전체 URL은 저장하지 않는다. 재시작은 대조만, 자동 재전송 없음. 실제 store는 일반 앱의 삭제 버튼에 연결하지 않는다.
- 등록은 seller net 전송, success는 Submitted/Guard 대기, 서버 listingid와 필드 일치가 있어야 Listed. 응답에 ID가 없거나 취소 목록이 사라진 것만으로는 게시/취소를 확정하지 않는다. 원화/스택·판매 제한 전체 해석/실제 재가격 saga/실계정 UI/확정 취소 이력은 미완성이다.
- 기능 범위와 승인 제한의 최신 근거는 STEAM-INTEGRATION-FEASIBILITY. 아래 1~3차의 “HTTP 없음/무조건 Mock” 문구는 당시 상태다.

## 3차 구현 — 2026-10-28 (최신)

- 기존 계층/뷰를 유지했다. `ReviewLineViewModel`은 미산정 행·개별 단가/Steam·게임 수수료/실수령·수동 여부·조회 신선도를 표시한다. 합계는 편집 이벤트에서 갱신하며 프레임별 폴링은 없다.
- `SalePlanner.Unpriced`로 원래 선택 수량 전체를 보존한다. 부족 가격은 생성하지 않고 직접 입력 전 최종 명령을 막는다. 전략 변경/검토 닫기/갱신은 초안과 수동값을 폐기한다.
- `LineResult.Attempt`와 `SaleBatchCoordinator.RetryFailedAsync`: 확정 Failed + 검토한 기존 시도 번호가 일치할 때만 명시적으로 Failed→Submitting을 허용한다. 원래 스냅샷은 불변, 키는 `BatchId:LineId:attempt:N`. 대조는 현재 시도 키를 사용하고 성공/Unknown/Guard 대기는 제출하지 않는다.
- `MockListingManagement`: 취소 확인→새 SubmitRequested→AwaitingSteamGuard의 재가격 saga. intent 직후 중단은 Unknown, 취소 확인 이후 후속 intent가 없는 경우만 사용자 검토로 재개. 교체 목록의 최종 취소까지 원래 자산 예약을 유지한다.
- `ListingAction.Registration`과 관리 작업의 Action을 구분한다. 구형 Unknown(Action=None)을 과거 등록 증거로 덮어쓰지 않는다. 성공 응답을 게시 증거로 취급하지 않는다.
- SQLite schema v2: v1 보존 이관, quick_check, 고정 스키마 diagnostics. `objects`의 batch/journal 불변 헤더와 result/listing별 변경 행을 트랜잭션으로 저장한다. 전체 배치/판매 배열의 반복 JSON 쓰기를 제거했다. 단일 writer 아래의 불변 레코드 캐시이며 Clear/Delete 후 무효화한다.
- `ReadRecord`는 헤더와 최신 결과 행을 합쳐 복구한다. 복수 결과 갱신 중 실패하면 모든 변경을 rollback한다. 이전 v1 헤더/결과도 읽는다. `ReadBatches` 역시 최신 결과를 재구성해 예약 검사에 사용한다.
- 구조화 진단은 코드/시각/건수만 최대500개, UI는 최근12개를 표시한다. 앱 시작 DB 오류는 별도 bounded 고정 코드 로그. 자유 텍스트 예외/키/쿠키/아이템/계정 문자열을 수집하지 않는다.
- 테스트 전용 headless 프로세스는 프로젝트 DLL만 실행하고 자체 Exit한다. 실제 창/다른 프로세스/전역 입력은 대상이 아니다. 배포는 framework-dependent publish만 수행한다.

## 2차 구현 — 2026-10-28 (아래 최초 구현 설명보다 우선)

- `MainWindow`는 일반 앱 실행 시의 호스트만 담당하고 실제 화면은 `MainView : UserControl`로 분리했다. `MarketCrate.UiTests`는 plain Application의 리소스만 로드해 MainView를 직접 생성하며 App.OnStartup/Window/Popup을 실행하지 않는다.
- 카드의 `VirtualizingTilePanel : VirtualizingPanel, IScrollInfo`는 고정 258×312 DIP 셀, viewport+overscan만 생성하고 WPF recycling generator를 사용한다. 목록은 기존 VirtualizingStackPanel을 유지한다. 필터 Reset·끝 스크롤·모드 전환을 검사한다.
- `RangeObservableCollection.ReplaceAll`과 전체 선택의 알림 일괄 처리로 아이템 수만큼 UI Reset/합계를 다시 계산하지 않는다. 결과 화면도 변경된 행만 교체한다.
- `SalePlanner`는 공유 호가 잔량 소비를 합계·검토에서 재사용한다. 최종 명령에서 현재 inventory/가격/잔량·선택 수량을 재대조한다. coordinator도 과거 배치의 예약 수량을 검사한다.
- 프로덕션 구성의 현재 저장소는 **합성 데이터 전용 `SqliteDemoStore`**다. `IDemoJournal`/`IDemoDataStore`, SQLite 스키마 v1 `objects(kind,id,value)`에 설정/인벤토리/활성 배치/배치 이력/합성 결과 증거/판매 목록을 저장한다. 네이티브 SQLite 의존성은 앱 저장소에만 사용하며 타 프로세스 주입/호킹이 아니다.
- 단일 connection/파일 lease, parameterized SQL, transaction, FULL 동기화, 미래 스키마 거절, 손상 스냅샷 검사. JSON v1은 동일 lease 하에서 단 한 번 이관하며 원본 보존·migration tombstone으로 삭제 후 재이관을 차단한다. 자동 백그라운드 DB 작업/네트워크는 없다.
- `MockListingExecutor`의 요청 키별 결과는 SQLite의 **합성 서버 증거**이며 실제 서버가 아니다. 명시적 fixture 결과 주입 이후 대조만 Listed를 확정한다. 근거가 없으면 Unknown 유지, Ready 자동 재시도 없음.
- `MockOrderProjection`은 배치 결과를 판매 목록으로 연결한다. `MockCancellation`은 최종 취소 검토의 선택 전체를 처리하며 intent/개별 결과를 저장한다. 중단된 CancelRequested는 Unknown으로 복구한다. 취소된 요청 키의 예약만 명시적으로 해제하고 취소/실패 결과를 배치 대조가 덮어쓰지 않는다.
- DB 설정·코어 검증은 콘솔 러너, WPF 렌더·상태·바인딩은 독립 STA 콘솔 러너에서 순차 검사한다. 실제 모니터 DPI/사용자 입력/팝업·설치/다중 활성 배치/장시간 장애 검증은 미완료다.

## 솔루션
- `src/MarketCrate.App`: WPF UI, MVVM/ViewModels, 커스텀 ControlTemplates, 애니메이션 및 라우팅.
- `src/MarketCrate.Core`: 순수 데이터 모델, 가격 산식, 품목별 주문 매핑, 배치 스냅샷/상태 머신. 외부 네트워크 참조 없음.
- `src/MarketCrate.Infrastructure`: 허용된 조회 Provider, SQLite, 캐시, 로그, 제한된 HTTP 클라이언트.
- `tests/MarketCrate.Tests`: 순수 계산·Mock 배치·UI 상태 로직 단위/통합 테스트 (실계정 접근 금지).

## 주요 경계/인터페이스
- `IInventoryProvider`: 게임/컨텍스트/페이지/시각/지원 상태와 보유 개수 조회.
- `IMarketDataProvider`: 현재 거래가·집계기간·거래량·수수료·호가별 잔량 조회.
- `IListingsProvider`: 판매 중·승인 대기·거래 내역 읽기(허용된 범위).
- `IPriceStrategy`: Average / ImmediateEstimate. 주문 원자료와 가격 제안/신뢰 상태 분리.
- `IFeeCalculator`: 통화 최소단위 정수 연산. Steam/게임별 수수료·반올림 검증.
- `ISaleBatchCoordinator`: 검토 → 불변 스냅샷 → 실행 잠금 → 항목별 결과 → 재조회.
- `IMarketListingExecutor`: `Mock`, `Blocked`, **권한 확인된 경우만** 실제 연동. 정책 승인 없이 live 주입 금지.
- `IBatchJournal`: 비밀값 없이 진행 기록/실패·중복 방지/재시작 복원.
- `ICapabilityRegistry`: `Mock / ReadOnly / Limited / Blocked / Live` 기능 상태와 이유 노출.

## 필수 모델
`InventoryAsset(AccountReference, AppId, ContextId, AssetId, MarketHashName, Marketable, Tradable, Quantity, LockState)`
`PriceSnapshot(Currency, CapturedAt, Source, Window, Volume, AveragePrice, BidLevels, Availability)`
`SelectedSaleLine(AssetRef, Quantity, PriceStrategy, ProposedGross, ProposedNet, Currency)`
`SaleBatchSnapshot(BatchId, FinalizedAt, AccountRef, IReadOnlyList<SaleLineSnapshot>, EstimatedTotals)`
`SaleLineStatus(Ready, Submitting, Submitted, AwaitingConfirmation, Listed, Sold, Failed, Unknown, Reconciled)`

## 판매가격/원칙
- `최근 평균`은 임의의 현행 `median_price`와 다름. 시세가 완전하지 않으면 Unsupported/InsufficientData로 표시.
- `즉시거래` 호가별 분배는 **예상 등록 제안**. 실 체결/정확한 해당 주문 할당을 보장하지 않음.
- 통화 최소단위 정수 사용, 셀에 `구매자 지불`, `판매자 수령` 분리. 수수료 역변환과 반올림·현지화 테스트.
- 거래 서버 상태 불명 시 자동 재전송 금지; 원격 상태 확인 후 제한적으로만 재시도.
- HTTP 조회를 뷰모델에서 직접 수행하지 않음; 취소 가능 비동기/캐시/명시적 오류 코드 처리.
- 대량 데이터는 가상화/지연 로딩/요청 제한. 백그라운드 자동 갱신 기본 비활성.

## 개발 시 보안 가드
`BlockedListingExecutor`를 기본 DI 등록. Mock은 명시적 Demo 모드에서만. 생산환경에 Mock 결과를 실거래처럼 보여주지 않음. 실연동은 Valve 허용 근거 문서화 + 사용자 승인 + 별도 격리 테스트가 선행되어야 함.

## 문서/검증 원칙
아키텍처 변경은 이 문서와 WORKLOG에 근거 포함 갱신. 작은 단위로 구현하며 코드가 구현되지 않은 기능은 CURRENT-STATUS에서 `NOT_IMPLEMENTED` 유지.

## 구현된 경계 (2026-10-28)
- Core: immutable record/ImmutableArray, `Pricing`, `BatchValidation`, `SaleBatchCoordinator`, provider/executor/journal 인터페이스. 네트워크/GUI 의존성 없음.
- Infrastructure: `DemoDataProvider`가 inventory/market/orders 계약 제공, `MockListingExecutor`, `BlockedListingExecutor`, `FileBatchJournal`. HTTP/로그인/SQLite 의존성 없음.
- App: 직접 구현한 Observable/ICommand 기반 MVVM, WPF 바인딩, 커스텀 NumericStepper, 중앙 토큰/컨트롤 ResourceDictionary. 프레임워크 추가 없음. 상태 변경의 UI 갱신은 await된 UI 컨텍스트에서 인라인 진행 통지를 사용하여 뒤늦은 Progress 큐가 완료 상태를 덮어쓰지 않게 한다.
- Tests: App ViewModel 소스를 링크한 순수 콘솔 러너. Windows 창을 만들지 않고 실제 선택→검토→최종 명령 및 중복 실행을 검사한다. 별도 테스트 SDK/네트워크 설치 없이 종료 코드로 판정한다.
- `NuGet.Config`는 온라인 소스를 비우고, 설치된 SDK/참조 팩만 사용한다. 추가 의존성 도입 때 별도 재검토한다.

## 저널·수명 설계와 제한
- 현 단계 SQLite 도입을 유보하고 **합성 Demo 데이터만** JSON 스키마 v1에 저장한다. 임시 파일 직렬화→WriteThrough/Flush(true)→동일 디렉터리 rename, 앱 수명 동안 독점 파일 잠금. 손상/지원하지 않는 스키마는 실행 차단.
- coordinator는 앱당 하나. semaphore로 실행/대조를 직렬화하고, 기존 저널이 있으면 새 배치를 제출하지 않는다. `Submitting`을 디스크에 먼저 저장한 뒤 요청한다. 저장 실패면 제출하지 않는다.
- 하나의 활성 Mock 배치만 보존한다. 앱 재시작·취소·불명 이후 자동 재개/재시도 없음. 데모 기록 삭제로 새 배치를 시작할 수 있으나 실거래에서 이 정책을 재사용하면 안 된다.
- 타임아웃/취소/IO 불확실성은 Unknown 후 후속 제출 중단. 준비되지 않은 개별 수동 재시도 기능을 된다고 표시하지 않는다.
- Mock 대조는 실제 서버 증거가 없으므로 Unknown으로 남긴다. 공식 모바일 승인을 시뮬레이션해 Listed로 만들지 않는다.
- 앱 종료는 진행 중 쓰기가 끝나기 전 보류한다. 작업별 취소 토큰은 앱 수명 토큰과 분리하여 취소가 이후 모든 갱신을 영구적으로 막지 않는다.
- 향후 live 경계에는 허용 증거·옵트인·자산/계정 재검증·실제 서버 대조·다중 배치 저장/원자적 전이·데이터 보호·통화별 수수료 검증이 추가로 필요하다.

## 10차 연결 보완

`Core.PriceDisplay`는 표시 이름·설명·이전 설정 alias의 경계다. Mock 전략은 내부 `PriceBasis`를 사용하고 설정은 `Immediate/ExactAverage`로 쓴다. 기존 저장값은 읽기 호환되며 배치/저널 schema와 Steam 가격 모드 인덱스는 유지한다.

`SteamConnectionViewModel` partial은 기존 세션/Orders/Fees 어댑터를 독립 명령에 연결한다. `UpdateChallenge`는 기존 auth의 회전된 URI를 로컬 QR matrix에 반영한다. `ReadContract4202`는 현재 UI OperationId 안의 고정 endpoint/count/currency 관측이며 실응답 인증이나 거래 권한 증명이 아니다. 공식 장터 URI는 고정 값이며 명시적 사용자 명령만 기본 브라우저를 연다. 테스트는 delegate로 대체한다. 실제 쓰기 코디네이터·Unknown·SQLite 저널·예약은 변경하지 않았다.
