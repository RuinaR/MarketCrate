# 진단 시스템 (6차, 2026-10-28 KST)

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
