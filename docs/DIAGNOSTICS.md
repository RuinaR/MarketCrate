# 진단 시스템 (10차, 2026-10-28 KST)

## 14차 — 읽기 권한·빈 페이지 증거

WalletContract **4205**는 shape·통화/최소가격/증분 필드존재·명시적로그인플래그만 기록한다. InventoryContract **4206**은 보고총계·description수·검증된0·부분페이지/모순·count만 기록한다. 원본HTML/JSON·계정/자산ID·redirectURL·인증정보는 기록하지 않는다. 기존 OperationId/HTTP요청ID/고정endpoint와 ZIP 검토·보존/회전 유지.

예: SelfCheck Wallet → HTTP302/redirect=Login → Authorization/Wallet 제한 → 후속 Blocked. 세션기록 보존, 독립endpoint는 사용자가 별도 요청 가능. Inventory HTTP200 → 명시total0/no-more → EmptyVerified → Empty. 총계누락/모순은 ContractMismatch, JSON문법오류는 Parsing. HTTP401/403/429/CAPTCHA와 불명서버제한 보호 유지.

Authentication / Authorization / AccessRestricted / ContractMismatch / Parsing / Unsupported / Empty / Transport / RateLimited 및 무전송 Blocked를 구분한다. 0.13 Authentication은401/redirect/API오류 복수경로 가능, 사용자원인은 미확정이다. 진단로그는 SQLite 거래복구 저널을 대체하지 않는다.

## 13차 연결 자가검증

**ReadSelfCheck4204**(Data,A04), Step=고정 Endpoint SessionCheck/Listings/Inventory/Marketable/Wallet/Fees/Orderbook/History/Image. Running/Succeeded/Failed/Blocked/Unsupported/Unverified/Cancelled을 기존 outcome/error로 기록한다. GUI SelfCheckAsync의 OperationId를 9개 행과 provider/HTTP correlation까지 공유한다. 표시 detail/게임명/아이템명/plan/QR·쿠키·원문은 로그에 직렬화하지 않고 고정 step·상태·원인·count·통화만 허용한다.

예: `UiCommand(O)→ReadSelfCheck(SelfCheck)→SessionCheck(Succeeded:메모리 기록)→HTTP(Listings,R,403)→ReadSelfCheck(Listings,Blocked,Forbidden,O)→나머지허용step(Blocked,미전송,O)`. parse/503은 해당 step Failed 후 다른 허용 read를 진행할 수 있고 재시도하지 않는다. Wallet 실패→Fees(Unverified)에는 HTTP Fees가 없어야 한다. Currency mismatch는 Orderbook/Failed/Currency, publisher fee mismatch는 Fees/Failed/Validation이다. 이미지가 캐시 적중이면 HTTP 대신 기존 ImageLoad cache 이벤트가 있을 수 있다.

행의 UTC/KST는 검사 시각이며 원자료의 서버 수집 시각을 보증하지 않는다. 성공은 개별 관측이고 QR 발급/세션 존재/전체 읽기/실거래 권한을 동일시하지 않는다. 자동 ZIP 전송 없음. 기존 진단 검색·범위 검토·내보내기·정제·회전/보존 정책과 SQLite 복구 저널 분리는 그대로다.

## 12차 인증과 조회 상태 진단

**ReadState4203**(Area Data,A04): Inventory/Listings/Fees/Orderbook/History 고정 Endpoint별 NotQueried→Loading→Ready/Empty/Failed/Unsupported/AccessBlocked/Cancelled/Stale 전이를 기록한다. OperationId는 GUI Run→provider→HTTP Correlation→UI state까지 공유하며 원본 URL/본문/계정·자산·인증자료는 없다. per-read 마지막 At/OperationId는 로컬 상태이며 복구 저널을 대체하지 않는다.

합성 예: `UiCommand(O,LoadAsync) → ReadState(Inventory,Loading) → HTTP(R,200) → ResponseParsed(Parsing) → ReadState(Inventory,Failed,Schema,O) → UiFailure(O)`. **AuthState(ContractFailure)가 나와서는 안 된다.** 다른 사용자 작업의 Listings/Fees는 새 O에서 계속 조회 가능하다. 401은 AuthState(Expired)+세션 정리,403/429는 인증 상태와 별도로 요청 gate를 유지한다. RequestAccessText와 개별 실패 사유는 별개다.

GET 통신 실패/일반 파싱은 개별 조회 결과만 실패시키고 자동 재시도하지 않는다. POST 응답 불명, auth 제한,403/429/CAPTCHA/쿠키 이상/초과 body/반복 pagination 등의 기존 안전 latch는 유지한다. History 예외에는 고정 endpoint만 전달해 composite 가격 조회 실패가 이력 문제인지도 추적한다. Read 파서의 문법은 ResponseParsed/Parsing, 스키마는 Schema, HTTP 상태·통신/timeout은 기존 HttpFailure로 구분한다.

## 11차 QR 계약 실패 분류

AuthContract **EventId2005**, Area Authentication, Requirement A03. Reason은 고정 enum만 허용한다: AuthBegin(사전조건), RequestEncoding, HTTP(상태/통신), ApiResult(HTTP200이지만 x-eresult 거절), ResponseParse(JSON 문법), ResponseFields(필수 필드·형식·범위), ChallengeValidation(허용 QR URL). UI ContractFailure에도 LastFailureStage를 표시한다. AuthBegin/AuthPoll/AuthFinalize/AuthTransfer는 고정 Endpoint 식별자이며 실제 URL·계정·request_id를 기록하지 않는다.

예: `Login(Operation O) → HttpRequest(AuthBegin,Correlation R) → HttpResponse(200,Failed,ServerCode8,R) → AuthContract(2005,ApiResult,Authentication,O) → AuthState(Blocked,O)`. malformed JSON은 HTTP 성공 후 AuthContract(ResponseParse,Parsing), 필드 누락은 ResponseFields/Schema, 잘못된 challenge host는 ChallengeValidation/Schema다. 로그가 거래 저널을 대체하지 않으며 자동 재요청을 유발하지 않는다.

EleventhStageTests.ResponseFailures/ApiFailures는 동일 OperationId, 고정 endpoint, 실패 분류, HTTP200 API 오류 성공 오표시 방지 및 QR/응답 sentinel 부재를 검증한다. 원본 HTTP/QR을 내보내지 않으므로 사용자 ZIP만으로 응답 전체를 복원할 수는 없다. 이번 사용자 오류의 실제 단계는 새 빌드에서 사용자가 관찰해야 한다.

## 10차 실제 연결 계약 관측

ReadContract **4202**: 기존UI operation 안에서 파싱이 끝난 AuthTransfer/Inventory/Listings/Fees/Orderbook/History의 고정endpoint·건수·KRW/USD·UTC·OperationId를기록한다. HTTP는같은operation의서로다른CorrelationId로연결한다. Synthetic/Observed를구분하고Observed는라이브계약검증완료가아니다. QR갱신은QrReady/SourceChanged만, URI/셀/토큰로그없음.

QR화면ContractSummary는마지막관측시간·개수·내부작업ID만표시한다. 실패후이전자료는마지막관측으로유지하며새검토는연결상태필수. 계정·asset/listingid·hashname·JSON필드덤프·HTTP원문을진단항목으로추가하지않았다. 로그에남지않는필드때문에정확한실제스키마차이를확정할수없는경우는UNVERIFIED로남긴다.

실사용자는진단탭OperationId필터/Refresh→package범위검토→Reviewed→로컬ZIP내보내기를사용한다. 6~9차정제·UTC정렬·회전·보존상한·오류실패독립·저널분리는유지. 진단친화레이블은Immediate를즉시거래가능가로표시하되JSONEventId/Reason은기존키다.

## 9차 진단 패키지 결함 수정

파일 복사/복원으로 mtime 순서가 이벤트 UTC와 달라지면 기존 ReadSafe가 같은 OperationId의 실패를 시작보다 먼저 ZIP에 넣었다. 신규 재현 검사의 수정 전 FAIL을 확인했고, 크기 제한 조기 반환과 정상 반환 모두 `OrderBy(Utc)`로 수정했다. 동일 시각은 안정 정렬하며 UTC 순서가 clock skew나 모든 인과관계를 증명한다고 주장하지 않는다. 연결 ID·저널이 판단 기준이다.

NinthStageTests.OrderedExport: 역전된 파일mtime/선택operation/오류분류/시간순2개 이벤트/정제2-entryZIP·로컬exports 경로·무DB를 확인한다. 기존 검토 동의/white list/보존·회전/상한/쓰기 실패·자동 외부송신 금지는 그대로다. 실제 guest 로그는 아직 없으며 합성 진단 테스트 결과만 있다.

guest 수동24개 결과는 별도 `IsolatedGuiQa/QaResults/gui-GUID.json`이고 시작/종료/exitcode를 관찰 결과 PASS로 바꾸지 않는다. 앱 이벤트와 수동기록은 OperationId로 연결한다. 앱 진단 ZIP은 `IsolatedGuiQa/Logs/exports`에 남고 결과JSON·원본DB·HTTP/QR는 자동 포함하지 않는다.

## 8차 GUI·계약 진단 연결

기존 보존/회전/화이트리스트/사용자 검토 ZIP 정책 유지. AppStart.reason=Demo 또는 IsolatedQa로 실행 모드 구분, QA 로그는 별도 사용자 루트. VM 여부를 추측·로그하지 않는다.

- 모달 binding → ModalNavigation → 기존 UiCommand의 OperationId로 Modal9005 Opened/Closed/FocusDeferred(Debug)/FocusRejected(Warning). 실제 자기 Window가 활성·Loaded인 경우만 자체 탐색/복귀. detached는 FocusDeferred이며 focus 성공으로 해석하지 않는다. 마우스/키 입력 내용·요소 텍스트·타 앱 정보 없음.
- AuthState2004: QR 계약 오류는 endpoint AuthBegin/AuthPoll + Error.Schema + Blocked + 같은 작업ID. 취소는 Cancelled. 원본 URL/QR/토큰/메시지 제외. 공개 선택 필드가 기본 빈값일 때 오류로 취급하지 않는다.
- HttpFailure3002: 요청 토큰의 사용자 취소는 Cancelled와 현재 Queue/Headers/Body 단계. 세션을 Transport halt로 바꾸지 않는다. 진짜 timeout/transport/403/429는 기존 중단 규칙 그대로. 판매·취소 응답 유실은 여전히 Unknown; read 취소 보완이 쓰기 재전송을 허용하지 않는다.
- 대표 합성 추적: UiCommand(OperationId X) → Inventory request(CorrelationId Y) → HttpFailure[Cancelled, phase] → VM Busy=false/Connected 유지. UiCommand(OperationId Z) → Modal[Opened] → Modal[FocusDeferred, detached] → Modal[Closed]. ZIP은 기존 operation filter/UTC 순서로 검토한다.
- Dispatcher/AppDomain/UnobservedTask·BindingDiagnostics는 기존 release handler 유지. 실제 Window/Popup focus 오류를 재현한 것은 아니며 관련 동작은 게스트 시나리오에 NOT VERIFIED로 남긴다.

7차 추가 보호: 기간 중앙값의 구매자/판매자 결제액 역할과 통화 해석은 live 미검증이므로 `SteamTradingPolicy.MedianPriceContractSupported`가 별도로 필요하며 기본 false다. 사용자의 최종 클릭/정수 계산 성공으로 이를 true로 추정하지 않는다. 합성 positive 경로에서만 명시적으로 켜 검증했다.

## 7차 이벤트/운영 보완

- ImageMetadata 4100 / ImageLoad 4101 / GameCatalog 4200 / InventoryScope 4201 / PriceIndicator 5100 추가. InventoryScope는 고정 상태의 이전/새 상태/판매 가능 종류 수를 기록한다. app/context/name/계정 원문은 기록하지 않는다.
- metadata parser와 inventory/listings/orderbook/history/fees HTTP는 부모 OperationId와 요청 CorrelationId를 연결한다. 서로 다른 HTTP는 서로 다른 correlation, 사용자 명령 전체는 같은 operation. parser는 해당 request scope 안에서 동작하도록 보완했다. 일부 기존 auth parser는 operation으로 연결한다.
- thumbnail은 row의 조회 OperationId를 이어받아 UI 로딩/취소/디코딩 오류에 사용. CDN 요청은 endpoint=Image/고정 상태·HTTP 분류만, URL/opaque token/bytes/쿠키/원문 제외. 성공 metadata/cache-hit는 Debug로 제한하고 실패/전송은 기본 수준에서 추적한다. 이미지는 메모리 캐시이며 SQLite에 쓰는 것처럼 가짜 transaction 이벤트를 만들지 않는다.
- catalog 검증/저장과 가격 window/basis/indicator/final digest 검증은 같은 사용자 operation 문맥. 이름/AppID/context/표본 원문을 로그에 넣지 않고 고정 reason·count·통화·오류 사용. 원자료와 추정 지표를 같은 성공으로 기록하지 않는다.
- Steam 화면의 작업 ID와 **현재 Steam 작업의 진단 보기**는 Diagnostics.OperationFilter로 연결해 바로 해당 operation을 찾는다. 설정/보존/보안/ZIP 재정제/사용자 검토 정책은 변경 없다.

## 목적과 저장 경계

진단은 **MarketCrate 자체 작업**의 인과관계를 좁히기 위한 보조 자료다. 거래 복구의 근거는 별도 SQLite 저널이다. 로그 누락/쓰기 오류/ZIP 오류는 판매 요청 재시도, 예약 해제, 상태 확정, 저널 변경의 근거가 될 수 없다. 다른 프로세스/화면/입력/네트워크를 감시하지 않는다.

정상 앱 시작은 `%LOCALAPPDATA%/MarketCrate/Logs`에 로거를 먼저 구성한다. Demo DB는 `MarketCrate/Demo`, 실험 저널은 `MarketCrate/SteamExperimental`이며 배포 디렉터리 밖에 보존한다. 새 패키지 설치 없이 BCL Channel/JSON/ZIP을 사용한다. 진단 모드는 사용자의 Demo 설정 DB에 boolean만 저장하며 다음 실행에 복원한다. 인증정보/QR/원본 응답은 어떤 설정/로그에도 저장하지 않는다.

## 스키마 1

`Core/Diagnostics.cs`의 `DiagnosticRecord`는 JSON Lines 한 행/이벤트다. enum 값은 사람이 읽을 수 있는 문자열로 저장한다. UTC `Utc`, Level(Trace/Debug/Information/Warning/Error/Critical), 고정 EventId/EventName, RequirementId, Area/Outcome, SessionId, Context(OperationId/CorrelationId/BatchId/OperationItemId), AppVersion/BuildVersion/Commit, StartedAt/DurationMs, Error, 고정 Endpoint, Method, HttpStatus/안전한 숫자 ServerCode, PreviousState/NewState/Reason, Count, Retry, JournalState, 정제 Exception을 제공한다. 가격 이벤트에는 allowlist Currency(KRW/USD)와 정수 FeeQuote Amounts가 선택적으로 붙는다.

- SessionId: 로거 실행마다 생성. OperationId: 사용자 명령/서비스 operation의 AsyncLocal scope. 하위 서비스/HTTP/저장소에 같은 operation 전달.
- CorrelationId: 실제 HTTP 전송 하나의 내부 GUID. 요청/응답/타임아웃이 같은 ID. 분리된 파서 호출은 부모 OperationId/순서로 연결되며 별도의 HTTP CorrelationId를 항상 유지하지는 않는다.
- BatchId: 도메인 불변 배치 GUID, 재시작 후에도 저널의 값 사용. OperationItemId: **로컬 요청 키**의 도메인 구분 SHA-256 앞 16 bytes. 원문 자산/계정 ID를 입력하지 않는다. request key가 보존되면 세션 간 연결 가능. 직접 account/asset/listing ID는 로그에 넣지 않는다.
- Operation의 Dispose 이벤트 `Completed`는 scope 종료/소요 시간이다. **성공으로 해석하면 안 된다.** 실제 성공/대기/실패는 결과 이벤트·상태 및 커밋된 저널로 확인한다. Submitting은 durable intent 저장 후, 결과 전이는 가능한 한 journal commit 후 기록한다.
- 정보가 없으면 null이다. 오류를 분류할 수 없는 것을 임의 성공으로 바꾸지 않는다. AppVersion은 Core assembly, BuildVersion은 informational version, Commit은 MSBuild SourceRevisionId metadata. 수정 중 만든 바이너리의 commit은 작업 기준 커밋이며 서명된 소스 일치 증명이 아니다.
- 시간은 UTC 원본, UI는 UTC와 `+09:00` KST를 함께 표시한다. 사용자의 Windows 시간대/DPI 설정은 변경하지 않는다.

## 보안: 자유 텍스트를 받지 않는 경계

DiagnosticHub는 URL/쿼리/HTTP 헤더/본문/계정/아이템명/검색어/QR 문자열용 인자를 제공하지 않는다. endpoint/reason/state/journal은 고정 token allowlist만 통과한다. 원본 예외 Message/Data/ToString/StackTrace 문자열을 저장하지 않는다. 예외 타입은 allowlist로 제한하며, 현재 프로세스의 이미 로드된 1st-party assembly 메타데이터와 일치하는 메서드명+숫자 IL offset만 최대8개 남긴다. 내부 예외는 깊이3/각2개, 경로/소스 파일/줄 내용 없음. IL 위치의 PDB 대조는 같은 빌드 소스에서 개발자가 수행한다.

Sink 입구와 ZIP 내보내기에서 `DiagnosticRecordSafety.Clean`으로 다시 검사한다. 파일을 수동 편집해 인증 URL/문자열/가짜 프레임/이벤트 이름을 삽입해도 해당 값은 제외/고정 이름으로 교체한다. 구조화 타입 외 데이터를 일괄 직렬화하지 않는다. 변조된 null stack 프레임도 제외해 패키지 검토 전체가 중단되지 않게 한다. JSONL은 사용자 계정 권한의 로컬 평문이며 별도 암호화나 보안 제품은 아니다. ZIP에는 시각/내부 작업 ID/가격 등이 있으므로 사용자가 범위를 확인하고 직접 공유한다.

## 주요 경로와 이벤트

앱 시작/종료/예외(Dispatcher/AppDomain/UnobservedTask), QR/auth, inventory/listings/orderbook/history/cache, 가격 계획/수동 편집/수수료/최종 검증, 배치 의도/예약/항목 결과/재시작, 취소/재가격/증거, SQLite open/migration/transaction/오류, UI command/page/입력/binding에 연결했다. 원시 키 입력·마우스 이동·프레임 렌더는 기록하지 않는다.

WPF binding trace 문자열에는 입력값이 들어갈 수 있으므로 **텍스트를 통째로 버리고** `UiBinding` 오류 코드만 프로세스당 최대20건 기록한다. 이 때문에 binding 식 자체를 로그에서 복원할 수는 없다. 사용자 Window를 표시하지 않는 분리 테스트에서 별도의 binding trace로 회귀 검사한다. 관리 명령/기존 JSON legacy 경로까지 연결했지만 모든 작은 예외 분기에 독립적인 domain reason이 있는 것은 아니다(LOG-04/11 PARTIAL).

| EventId | EventName |
|---|---|
| 1000 | AppStart |
| 1001 | AppStop |
| 1002 | AppFault |
| 1003 | Settings |
| 1004 | ResourceCleanup |
| 2000 | Login |
| 2001 | QrReady |
| 2002 | LoginCheck |
| 2003 | Logout |
| 2004 | AuthState |
| 3000 | HttpRequest |
| 3001 | HttpResponse |
| 3002 | HttpFailure |
| 3003 | ResponseParsed |
| 3004 | ResponseBody |
| 4000 | InventoryPage |
| 4001 | InventoryLoaded |
| 4002 | ListingsLoaded |
| 4003 | PriceLoaded |
| 4004 | HistoryLoaded |
| 4005 | CacheRead |
| 5000 | PricePlan |
| 5001 | PriceEdited |
| 5002 | FeeValidated |
| 5003 | ReviewOpened |
| 5004 | FinalValidation |
| 6000 | BatchCreated |
| 6001 | ItemReserved |
| 6002 | ItemResult |
| 6003 | BatchFinished |
| 6004 | BatchRecovered |
| 6005 | Retry |
| 7000 | CancelRequested |
| 7001 | CancelResult |
| 7002 | EvidenceChecked |
| 7003 | Reprice |
| 8000 | StoreOpen |
| 8001 | Migration |
| 8002 | Transaction |
| 8003 | JournalWrite |
| 8004 | StoreRecovery |
| 9000 | UiCommand |
| 9001 | UiFailure |
| 9002 | UiPage |
| 9003 | UiBinding |
| 9004 | UiSelection |
| 9005 | Modal |
| 10000 | DiagnosticMode |
| 10001 | Export |
| 10002 | Retention |

HTTP는 고정 목적명(AuthBegin/AuthPoll/AuthFinalize/AuthTransfer/Wallet/Fees/Inventory/Orderbook/History/Listings/Sell/Cancel)만 사용한다. 400→ClientRequest, 401→Authentication, 403→Forbidden, 429→RateLimited, 5xx→Server를 HTTP 응답 이벤트에 구분한다. 전송 예외와 파서/스키마 오류는 별개다. timeout은 caller 취소와 구분하며 QueueWait/ConnectOrHeaders/Body 단계와 소요 시간을 남긴다. transport exception의 원문은 버린다. 제한 응답은 기존 halt latch로 다음 전송을 막으며 자동 재시도하지 않는다. DNS/TLS/소켓 세부 내부 상태나 원본 서버 오류 본문은 수집하지 않는다.

## 운영 한도/쓰기 실패

- 기본 Information 이상, 사용자가 상세 진단 모드를 켜면 Trace/Debug까지. 자동 네트워크 폴링/원격 텔레메트리 없음.
- single async writer, 큐 256, 각 이벤트 최대32 KiB, 최근 메모리 512. UI/거래 스레드는 TryWrite만 하고 꽉 차면 버린다. Health.Dropped/WriteFailures/Accepted/Written으로 간극을 표시한다. 마지막 writer 실패는 자유 텍스트 대신 고정 LastFailure 분류·숫자 NativeError·FailedOperationId로 UI/manifest에 연결한다. 접근 거부와 알려진 OS disk-full/lock 코드는 구분하며 메시지의 단어를 보고 추측하지 않는다.
- UTC 일자 또는 기본 2 MiB 파일 회전, 최대20개/전체20 MiB/14일. 회전 전 한 파일 여유를 확보한다. 한 이벤트가 파일 한도보다 크면 최대32 KiB의 유한 초과가 가능하다. 파일을 쓰는 중에도 FileShare.ReadWrite로 안전 요약을 읽는다.
- 자동 정리는 지정 폴더의 `session-*.jsonl` 일반 파일만 대상으로 하며 링크 파일/루트는 배제한다. 보존 한도 검사/삭제 실패 시 health에 실패가 드러나며 임의 다른 폴더를 정리하지 않는다. 수동 정리는 UI 명령으로 실행한다.
- 디스크 오류/권한 오류 후 writer는 해당 실행에서 쓰기를 중단하고 health=WriteFailure를 표시한다. 메모리 최근 기록은 남을 수 있다. 거래/취소는 이 오류 때문에 재전송하지 않는다. 재시작 후 파일 권한/공간을 확인한다.
- 종료 시 queue 완료와 최대1초 최선 노력 flush, 치명적 예외는 최대500ms flush. 전원 차단/강제 kill 직전 마지막 로그는 보장하지 않는다. 별도 거래 SQLite의 FULL/저널을 확인해야 한다.
- 로그 저장 실패는 포함 패키지도 비어 있거나 생성 실패할 수 있다. 파일이 전혀 쓰이지 않아도 ZIP manifest의 health로 누락을 알 수 있다. 실제 디스크 가득 참/Windows 강제 종료는 시험하지 않았다.

## 사용자 진단 절차

1. **진단 / 로그** 탭에서 상태·누락 수·쓰기 실패·Session을 확인한다. 필요할 때 상세 모드를 켜고 해당 작업을 다시 **관찰**한다. Unknown 판매를 재현하려고 재전송하지 않는다.
2. **최근 이벤트·오류 갱신**/최근 오류만을 선택한다. 최대512개 메모리 중 마지막100개를 표시하며 OperationId를 읽기 전용 입력에서 복사할 수 있다. 자동 클립보드 조작 없음.
3. OperationId를 입력하고 **관련 로그 검색 / ZIP 범위 검토**를 실행한다. 비우면 보존된 전체 작업이며 형식 오류면 비활성. 저장 파일 전체 검색(최대20 MiB)과 재정제를 수행한다. 검색 중 필터가 바뀌면 기존 검토를 적용하지 않는다.
4. 이벤트/오류/제외 수·범위를 확인하고 사용자 확인 체크 후 **검토한 진단 ZIP 로컬 저장**. 검토 ID는 1회만 사용한다. 필터 변경 시 기존 승인은 폐기된다.
5. 결과 경로의 ZIP을 사용자가 직접 검토/공유한다. 자동 업로드/브라우저 열기/타 앱 전송은 없다. **로그 폴더 열기**도 사용자가 해당 버튼을 눌렀을 때만 지정된 자체 폴더를 shell로 연다. 이번 QA는 callback 대체로 실제 Explorer를 실행하지 않았다.

ZIP은 `Logs/exports/marketcrate-diagnostics-<review-id>.zip`. 임시 파일을 완성한 후 이름을 이동하므로 실패한 부분 ZIP을 성공으로 노출하지 않는다. manifest.json(앱/빌드/커밋, 검토 범위, health, CLR/자체 아키텍처/Windows 요약), events.jsonl(정제 이벤트) **두 파일만** 포함한다. DB·인증·QR·메모리 덤프·원본 설정·원본 HTTP 응답·사용자 경로는 포함하지 않는다. 공개/비밀 journal을 가려내기 위한 억측 export 대신 **저널 자체는 제외**했다. 개별 transaction 이벤트의 JournalState만 보조 자료다. exports는 사용자가 요청한 독립 산출물로 로그 자동 보존 정리 대상이 아니므로 사용자가 필요 없어진 ZIP을 직접 정리한다.

## 추적 예 (실제 계정 기록 아님)

정상 합성 흐름: `UiCommand(O1) → Login → HttpRequest(R1, AuthBegin) → HttpResponse(200,R1) → AuthState(Connected)`. 최종 승인에서 `BatchCreated(O2,B1) → FinalValidation → ItemReserved(I1,Submitting,IntentDurable) → HttpRequest(R2,Sell) → ItemResult(I1,AwaitingSteamGuard,Pending,Committed) → BatchFinished(Completed)`. 사용자의 별도 조회 `BatchRecovered(O3,B1) → EvidenceChecked(I1,Listed,Committed)`가 나와야 게시 상태를 확정한다. 다른 항목은 같은 B1/O2와 다른 I2/I3로 찾는다.

오류 합성 흐름: `HttpFailure(R2,Timeout,ConnectOrHeaders) → command Unknown journal → ItemResult(I1,Unknown,EvidenceMissing,Committed) → BatchFinished(Completed)`; 다시 시작하면 새 Session/O3에서 같은 B1/I1의 `EvidenceChecked(Unknown)`를 찾는다. **새 HttpRequest(Sell)가 없어야 한다.** `S6.UnknownTrace`에서 판매 전송 횟수1을 함께 검증한다. fee conflict는 FeeValidated(Unknown,Validation,FieldsMismatch)와 로컬 정수 Quote를 남기고 뒤 분할 행은 Ready로 유지한다.

```json
{"SchemaVersion":1,"EventId":6002,"EventName":"ItemResult","RequirementId":"F03","Area":"Sales","Outcome":"Pending","Context":{"OperationId":"00000000-0000-0000-0000-000000000001","BatchId":"00000000-0000-0000-0000-000000000002","OperationItemId":"00000000-0000-0000-0000-000000000003"},"PreviousState":"Submitting","NewState":"AwaitingSteamGuard","JournalState":"Committed","Currency":"KRW","Amounts":{"BuyerPays":115,"SellerReceives":100,"SteamFee":5,"GameFee":10}}
```

위는 필드 설명용 부분 예시이며 실제 JSON에는 UTC/버전/Session 등 스키마 필드도 포함된다. 예외 없는 테스트 통과가 아니라 예상 상태/전송 횟수/저널/연결 ID/진단 이벤트/민감 문자열 부재를 함께 검증했다. 상세 결과와 한계는 QA-MATRIX / REQUIREMENTS-TRACEABILITY 참고.
