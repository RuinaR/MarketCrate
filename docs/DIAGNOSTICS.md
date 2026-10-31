## 0.20.22 — 변경 없는 재등록

PricePlan/Management/Completed/RepriceUnchanged는 최신 검증을 거친 제안 전체가 현재 구매자·실수령 금액과 같아서 취소/등록 없이 유지했음을 뜻한다. 서버 판매 성공 이벤트가 아니다. 기존 FeeValidated의 PreviousState=AppContextMissing을 SteamException.FeeContractReason으로 UI에 전달한다. 계정/원문 HTTP/쿠키를 기록하지 않는다.

## 0.20.21 수동 부족 가격

FinalValidation / Sales / Completed / Orderbook / ManualShortagePrice는 명시적으로 입력한 수동 가격에 대해 최신 빈 호가·통화·수수료 검증을 통과했음을 의미한다. 서버 접수/체결 성공을 의미하지 않는다. 기존 OperationId/배치 문맥을 유지하며 원본 HTTP·계정 비밀을 추가 기록하지 않는다. 호가 변경/파싱 실패는 기존 오류 경로로 전송 전 중단한다.

## 0.20.20 전송 전 호가 변경

ValidateBatchAsync의 OrderbookChanged는 batch 전송 전 검증에서만 RequestNotSent=true/Endpoint=OrderbookChanged를 설정한다. UI는 이 명시적 증거가 있을 때에만 판매 요청/Steam 확인 미생성과 새 검토 절차를 안내한다. 일반 StaleReview/전송 후 Unknown은 미전송으로 추정하지 않는다.

IDiagnosticFailure.Category로 SteamException의 Validation/Transport/RateLimited 등 기존 분류를 Core coordinator에 전달한다. IOException이라는 상속 이유만으로 Steam 계약 오류를 Storage로 기록하지 않는다. 순수 IO 실패는 기존 Storage로 유지한다. 추가 합성 StaleThenFresh는 BatchFinished Error=Validation/Storage0·판매요청0을 검사한다. 원본 HTTP·인증정보 기록0.

## 0.20.19 기본 현재 목록과 고급 대조 구분

ShowCurrentListingsAsync의 UI OperationId→Listings GET 페이지들→ReadContract/SetListings→판매 중 화면만 연결한다. 일반 승인 후 버튼에서 MarketHistory/BatchRecovered를 호출하지 않는다. 읽기 상태/로딩·취소·실패는 ListingsRead에 반영하고403/429/계약 차단을 준수한다. 고급 Recover/선택 요청 대조를 명시적으로 누른 경우에만 기존 상세 증거 이벤트를 남긴다. 운영 로그/거래 저널 보존 역할과 비밀정보 제외 원칙은 변하지 않는다.

## 0.20.18 거래내역의 불일치 원인

MarketHistory HTTP·본문 파싱은 같은 CorrelationId를 유지한다. EvidenceChecked의 HistoryNoSaleCandidate/HistoryAssetMismatch/HistoryQuantityMismatch/HistoryCurrencyMismatch/HistoryAmountMismatch/HistoryTimeMismatch/HistoryMultipleCandidates로 후보 없는 이유를 분리한다. ReadContract HistoryQuoteConflict는 서버의 이력 원단가와 실수령이 서로 모순된 경우로, 해당 행을 성공 처리하지 않는다. 실제 금액·계정/상품/자산/구매 ID·원본 JSON은 로그에 넣지 않는다. 확정하는 경우만 기존 HistorySaleMatched→Sold→SQLite audit/배치 결과로 이어진다. 새 실패 이유 token은 정적 화이트리스트로 제한한다.

앱 종료는 진단 로그 삭제가 아니다. 이번에는 LocalAppData의 MarketCrate/Logs session JSONL이 보존돼 원인을 분석할 수 있었다. Auth/쿠키·DPAPI 저장소·운영 DB를 읽지 않았고 원본 JSONL은 외부 전송하지 않았다.

## 0.20.17 판매목록 맵 자료형

Listings/ReadContract의 AssetsMapEmptyArray는 root/app/context의 명시적 빈 배열을 수용한 이벤트다. PreviousState=AssetsRoot/AssetsApp/AssetsContext, NewState=JsonArray, Count=0. AssetsMapInvalidShape는 JsonArray(내용 있음)/JsonNull/JsonOther 분류와 Schema/Blocked로 남긴다. 원본 키·자산/계정 값·JSON 조각을 기록하지 않는다. 같은 UI OperationId/요청 CorrelationId로 TradingCheck ListingsRead→HTTP200/JsonBody→맵 형식→권한 준비 또는 차단을 추적한다. 현재 사용자 ZIP은 이전 버전이라 안전한 IL 위치로만 root 비객체 분기를 확인했고 정확한 JSON 종류는 미확정이다.

## 0.20.16 추가 — 체결 대조 진단

HTTP endpoint=MarketHistory로 현재 UI OperationId/CorrelationId를 유지한다. EvidenceChecked reason=HistorySaleMatched는 알려진 원래 등록 ID 및 자산/수량/금액/통화/시각 일치이며 NewState=Sold다. HistoryAssetSaleObserved는 ID 없는 원접수의 동일 자산 체결 관측일 뿐 Outcome/State=Unknown을 유지한다. Command JournalWrite/Audit의 Sold와 BatchRecovered/배치 저장으로 연결한다. 계정/구매자·판매자 식별자, purchaseid, 원본 events/listings/purchases, HTTP 본문은 진단/ZIP에 남기지 않는다. 신규 회귀에서 OperationId/안전 reason 연결 및 평문 인증정보·계정·원본 응답 필드 부재를 검사한다.

## 0.20.16 목록 변경과 현재 승인 증거

Management/FinalValidation의 안전 reason `RepriceSelectionChanged`는 최신 서버 목록 대조에서 이전 선택의 ID·자산·수량·가격·통화·상태 불일치가 발생했다는 뜻이다. 기존 UI Run OperationId로 Listings HTTP 및 수수료 상태와 연결한다. Unsupported 수수료 실패 후 독립 목록 대조는 한 번이며 서버 제한 시 실행하지 않는다. Sales/EvidenceChecked의 `ListingNoLongerPresent`는 알려진 등록 ID가 완전한 현재 목록에 없다는 뜻이다. 즉시 체결을 증명하지 않으며 Error=EvidenceMissing/Outcome=Unknown으로 기록한다. ID 없는 접수는 기존 Inspect 증거와 UI 결과 Unknown으로 연결한다. 원저널과 로그는 별개이며 후보 ID나 Sold를 생성하지 않는다.

사용자 ZIP의 해당 작업은 재가격 중 AppContextMissing/Unverified였다. 승인 갱신·등록 시도 증거는 없으므로 새로운 실제 원인 확정을 주장하지 않는다. 인증정보·계정/자산 원값·원본 HTML/HTTP·운영 진단을 소스/배포에 넣지 않는다.

# 진단 시스템 (0.20.0, 2026-10-29 KST)

## 0.20 연결 보완

UI Run은 Busy 중 거절된 호출로 진행 중 OperationId를 덮어쓰지 않는다. 실제 Coordinator의 durable Submitting/개별 결과를 BatchUiProgress로 표시하며 로그아웃·계정 불일치의 늦은 진행 결과는 폐기한다. RefreshSelectedRequestAsync→EvidenceChecked(RecoverOnly)로 취소/등록 서버 대조를 명시적으로 연결한다. 취소 전용 활성화는 FinalValidation/CancellationEnabled이며 계정/상품/쿠키를 이유 문자열로 넣지 않는다.

재등록은 Prepared/Committed→새 자산 검토→첫 배치 기록과 같은 트랜잭션의 SubmissionStarted/Committed로 이어진다. SQLite trigger 오류 주입에서 한쪽만 저장되지 않음을 검사한다. OperationId/BatchId/항목 키는 기존 구조를 사용한다. JSONL은 저널의 대체가 아니며 새로운 개인정보/원본 HTTP/인증정보를 수집하지 않는다. 사용자 화면은 요청 키 대신 작업 종류·품목·상태·시각을 보여 주고 내부 키는 선택 값/진단에 보존한다.


## 0.19.0 추가 — 외부 등록의 새 자산 재판매

SteamMarketHelper의 취소→보관함 재조회→같은 상품의 새 자산 ID 선택 방식을 실제 서비스에 연결했다. 전체 취소 승인 후 서버 접수·목록 부재를 확인하고, 실제 보관함·가용 수량·수수료·공유 호가로 새 대상을 제안한다. 원래 아이템의 반환 확정은 아니며 다른 기존 보유분일 수 있음을 표시한다. 새 실제 자산 ID·수량·가격을 전체 검토해 판매등록 한 번 승인하면 기존 Coordinator가 배치 처리한다. 항목별 추가 판매 클릭은 없다. SQLite 취소 계획/새 배치 연결을 보존하고 불명 취소·원래 예약은 추측으로 해제하지 않는다. 최근 미제출 계획은 사용자 요청 시 재조회하며 자동 거래하지 않는다. 개별 수량1, 최대100등록/10상품만 지원한다.

추가 합성6개/비표시WPF1개: 변경ID·재시작 후 새 승인, 취소만으로 판매0, Unknown 재전송0, 전체 수량 부족·호가 변경 차단, 계획 불변·OperationId/비밀정보 보호. 최종 기능305/305·WPF61/61·배포19/19 PASS, 경고/오류0. 취소/새 대상 검토125%·200% RTB를 직접 검토했다. 실제 계정/수수료·판매/물리 입력은 미검증이다. 상세 호출 경로·보존/재시도 제약은 [판매 실행 계약](SALE-EXECUTION-SPEC.md)의 0.19 추가 절을 따른다.


## 0.19.0 처리 피드백과 재등록

PrepareListingRepriceAsync→같은 OperationId의 Fees/Orderbook→PricePlan(5000,D05/D08/B10)→BuildManagement→FinalValidation→Reprice(7003)/Cancel→저널/결과를 추적한다. 외부 반환 인과가 없으면5004/Management/Blocked/EvidenceMissing/endpoint RepriceReturn/reason NoCausalEvidence만 기록한다. 실제 URL/이름/계정/자산ID/QR/토큰/원문은 이벤트에 넣지 않는다. 신규 External 검사에서 원인·OperationId와 계정/아이템 이름 미노출을 확인했다.

UI OperationLabel은 사용자 작업명/현재 단계 건수이며 프레임/입력/마우스를 로그로 수집하지 않는다. Busy 아이콘은 자체 WPF RenderTransform만 사용하고 숨김/완료/축소 시 애니메이션을 해제한다. 미전송(Ready)·접수·Guard·Unknown은 원래 저널 결과를 구분한다. 진단 ZIP은 사용자 로컬 내보내기이며 자동 전송하지 않는다. 이번 첨부 ZIP은 없었으므로 실제 오류 원인을 분석했다고 주장하지 않는다.


## 0.18 수수료 모드·독립 지갑 추적

UI LoadFeesAsync/LoadWalletAsync→동일 OperationId→HTTP3000/3001/3005→wallet4205→FeeValidated5002→ReadState4203→UI를 추적한다. FeeContext는 자기 계정의 정확한 inventory HTML GET 목적의 고정 endpoint 식별자이며 URL/계정/HTML/props/쿠키를 기록하지 않는다. mode는 Reason/새 상태에 VerifiedLegacy/VerifiedNew/Unverified/Unsupported/Restricted, 이전 상태에는 ModeMissing/ModeInvalid/ModeAmbiguous/WalletChanged/AppContextMissing/LegacyFormulaConflict/WalletAndAppContext/ExplicitNewMode 등의 고정 화이트리스트 원인만 남긴다.

정상 예: LoadFeesAsync → Wallet200 →5002 Unverified/ModeMissing→FeeContext200→5002 VerifiedLegacy/WalletAndAppContext→FeesRead Ready(API 전송0). 오류 예:5002 VerifiedNew/ExplicitNewMode→Fees200/HtmlMarket→3002 Schema→FeesRead Failed; 인증과 마지막 KRW wallet 관측은 유지, 지갑 단독 갱신과 목록은 독립 사용 가능하다. Challenge로 추측하지 않는다. 모드 증거가 없으면 Unsupported로 종료하며 사용자에게 raw HTML 저장을 요청하지 않는다.

정식 전용 자가검증4204 UI/실행 모델은 제외했지만 과거 JSONL을 읽기 위한 이벤트 식별자는 유지한다. 문제 해결에서 최근 오류→관련 작업 검색→범위 검토→진단 ZIP 로컬 내보내기를 사용한다. 진단 ZIP은 외부 자동 전송0, Auth/QR/토큰/원문/사용자 ID 제외. FeeModeTests.Diagnostics/WalletIndependent와 기존 누출 canary·회전/보존/ZIP 회귀 PASS. 로그는 여전히 거래 저널의 대체가 아니다.


## 16차 — 로그인 유지 진단

새2006 SessionCredential 이벤트는 CredentialLoad/LoadFailed, CredentialSaved/SaveFailed, CredentialDeleted/DeleteFailed, CredentialPauseFailed/Unavailable 고정 분류만 기록한다. AuthRestore/TokenValidation/AccountValidation/SessionRestored를 기존2004/2005와 연결한다. UI Run의 OperationId를 finalizelogin/AuthTransfer HTTP CorrelationId 및 저장 결과까지 유지한다. CryptographicException은 메시지 없이 타입/앱 내부 stack만 허용한다.

로그/ZIP에는 토큰/쿠키/QR/원본 요청·응답/DPAPI blob·SID·계정·인증 파일을 넣지 않는다. AuthStore/SteamSavedLogin의 문자열 표현도 redacted이고 공개 토큰 속성은 없다. ZIP은 기존 JSONL allowlist만 내보내며 Auth 폴더를 탐색하지 않는다. 실패한 저장은 '로그인 성공'과 분리하고, 파일/토큰 검증 실패는 인증 HTTP 미전송으로 안내한다. 서버 거절과 unknown은 disarm 상태라 다음 시작에서 자동 반복하지 않는다. 실제 서버 인증 복원 성공은 사용자 검증이 필요하다.

## 0.16.1 — HTTP 응답 형식의 근거 분리

새 EventId **3005 ResponseContract**: 기존 OperationId/CorrelationId, Endpoint/HTTP200, PreviousState=MediaJson/MediaHtml/MediaText/MediaMissing/MediaOther, NewState=JsonBody/EmptyBody/OtherBody/HtmlMarket/HtmlLogin/HtmlChallenge/HtmlAccessDenied/HtmlUnknown, Reason=ResponseShape 또는 MediaTypeMismatch. 자유 문자열/원문 Content-Type·HTML·title·form값·계정·쿠키·URL·QR은 받지 않는다. 필드/미디어 불일치와 JSON 파서 실패(3003)를 분리한다. 유효한 JSON은 Content-Type만으로 버리지 않고 기존 endpoint 스키마를 모두 검증한다.

HTML은 최대128KiB 표식 검사(전체 읽기8MiB 상한 유지). script/style/comment를 제외하고 고정 DOM 표식을 검사한다. 단어 captcha만으로 Challenge를 판정하지 않는다. JSON의 명시적 captcha_needed/requires_captcha=true는 기존 인증 계약과 같이 안전중단하며, 유효한 수수료 필드가 있어도 통과시키지 않는다. HtmlMarket은 '장터 화면 표식'이지 로그인·수수료 성공이 아니다. HTML 위젯/접근거부는 전역 안전중단, 로그인 폼은 endpoint Authorization, 장터 HTML은 endpoint ContractChanged, 분류불가는 ContractChanged로 추가 요청 중단. 분류 누락 가능성이 있으므로 unknown을 통과시키지 않는다.

예: Fees3000 Started → 3001 HTTP200 → 3005 MediaHtml/HtmlMarket → 3002 Schema/HtmlMarket → 4204 Fees Failed/ContractMismatch. 동일 작업 ID로 Wallet 성공과 구분해 검색한다. HtmlUnknown이면 후속 항목은 Blocked이며 CAPTCHA로 단정하지 않는다. wallet legacy 필드는4205 WalletLegacyComplete/Partial/Invalid와 필드 수만 기록, 값/원문은 기록하지 않는다. OfficialFeeComparison은 사용자 입력 비교일 뿐 서버 검증/거래 활성화가 아니다.

사용자는 자가검증에서 지갑·수수료 범위만 명시 승인해1회 실행한 뒤 해당 작업만 선택하여 ZIP을 검토·내보낸다. 자동 외부 업로드 없음. 새 진단도 원문 실패 HTML이 없으므로 서버의 내부 분기 원인까지 확정할 수 없다는 한계가 있다.

## 0.16.0 자격 확인과 실제 전용 시작

AppStart는 일반 실행 LiveSession, 개발 격리 QA만 IsolatedQa이다. 실제 Release에는 Demo 시작 경로가 없다. ReadContract의 RedirectEligibilityPath와 HTTP MarketEligibilityRedirect/MarketEligibilityReturn, hop 수로 왕복을 연결한다. OperationId/CorrelationId를 유지하며 원문 Location/goto/cookie/HTML/계정값은 남기지 않는다. 서버가 발급한 cookie만 현재 세션에서 수신하고 디스크 저장/외부 추출/위조하지 않는다.

제공된 ZIP은 RedirectMarketOther/OtherQuery까지만 증명하며 실제 eligibilitycheck 여부를 확정하지 않는다. 새 정상 경로는 체크→정확한 복귀, 미완료/권한/속도 제한은 기존 분류로 중단한다. 설정 SQLite 실패는 기존 StorageDiagnostics에 연결하고 초기화로 숨기지 않는다. 정상 사용자 앱에는 실제 JSONL 로거가 연결되며 테스트 렌더의 로거 미연결 표시는 격리 환경만 해당한다.

## 0.15.1 Wallet 리다이렉트 증거

구버전 OtherRedirect만으로는 목적지·지갑미설정·인증문제를 구분할 수 없었다. ReadContract4202에 고정category를 추가: PreviousState=RedirectCommunity/Store/LoginHost/External/UnsafeAuthority, Reason=RedirectMarketSlash/MarketNoSlash/MarketOther/LoginPath/Account/Security/Root/OtherPath, NewState=RedirectNoQuery/LanguageQuery/OtherQuery/Fragment, Count=hop0/1. 모든 값은 내부화이트리스트 상수이며 원본host·Location·path·query이름/값·fragment·계정식별자는 기록하지 않는다. OperationId/CorrelationId 유지.

예: Wallet302→RedirectCommunity/RedirectMarketNoSlash/RedirectNoQuery→한번GET→HTTP200→WalletContract. 지원하지않는경로·임의query면 위분류후 OtherRedirect/Unsupported로중단하고 세션을 지우지 않는다. 2번째302면 새분류를 남기고 추가요청0. 이 category는 URL분류이며 실제 제한의 의미를 추측하지 않는다. 사용자 ZIP은 버전/커밋과이분류만공유, 원문URL/인증응답 수집금지.

## 15차 — 활성화·호가 변경·요청 증거

기존 OperationId→BatchId→OperationItemId→HTTP/SQLite 상태 연결 유지. FinalValidation의 TradingReadiness/TradingEnabled/AccountMismatch/OrderbookChanged와 HTTP MarketCanonicalRedirect는 화이트리스트 고정코드만 기록한다. 계정값은 메모리에서 일치만 비교하고 g_steamID/HTML·Location·QR·쿠키·토큰을 로그에 넣지 않는다.

예: 사용자활성화→Wallet계정일치/metadata→Inventory→기존등록fee비교→TradingEnabled(거래POST없음). 최종승인→scope/fee/최신book 검증→지문변경이면 OrderbookChanged/StaleReview·intent0·POST0. 정상은 배치intent→항목intent→직렬sell→Guard대기→사용자 수동조회→ID/가격/수량 대조. 요청전 존재하던 listingID 응답은 Unknown/자동재전송금지다. 예외/로그실패로 저널을 성공확정하지 않는다.

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


## 0.20.1 후속 보완 (2026-10-29 KST)

FeeContext ReadContract/HttpRequest는 기존 OperationId에 FeeContextVanityRedirect, FeeContextCanonicalRedirect, FeeContextOtherProfile, FeeContextOtherRedirect와 이동 횟수를 연결한다. URL/vanity/계정 ID/HTML/쿠키는 저장하지 않는다. vanity 소유 표시 누락·거짓·중복은 OwnerPageMissing, 현재 인증 계정 일치도 별도 확인. 403/429는 기존 전역 중단 유지. 첨부0.20.0 작업87ff3254-3ccf-4185-a35b-2f427b0d0cad는 Wallet200→ModeMissing→FeeContext302/OtherRedirect이며 Location은 없어 원인 미확정. 수수료 API 실패/CAPTCHA 증거로 분류하지 않는다. 실패 시 수수료 조회 상태와 판매가격 검토 안내에 반영한다.

## 0.20.2 classic 수수료 분기 보완 (2026-10-29 KST)

0.20.2: FeeValidated/FeeUiObserved는 서버의 classic/React 분기 유무(ClassicUiSelected/ReactUiSelected/ModeMissing/ModeInvalid)와 판매창 target 개수만 기록한다. 값·HTML·계정 ID·쿠키는 기록하지 않는다. ClassicInventoryContext→VerifiedLegacy는 런타임 증거 판정이며 실서버 수수료 확인 선언이 아니다. ReactPropsMissing/OwnerPageMissing/WalletChanged/AppContextMissing으로 실패 원인을 구별한다. 첨부0.20.1의 두 번째 ModeMissing은 302 처리 후 도달한 페이지에서 발생했다. 사용자 OperationId f67d8e55-5171-4c25-8c53-01a2524fecb8를 따라 확인했으며 QR/토큰/원본 본문은 읽거나 기록하지 않았다.

## 0.20.3 활성화 실패 안내·진단 연결 (2026-10-29 KST)

0.20.3 TradingCheck(FinalValidation)는 Consent→WalletIdentity→FeeContract→WalletConsistency→InventoryRead→SaleableAssets→PublisherFees→ListingsRead→ListingFees→SessionCheck 단계와 차단된 단계를 기존 OperationId로 연결한다. 원문·계정/자산 ID·토큰 없이 고정 enum만 기록한다. HTTP/파서 오류는 기존 요청 진단에 연결된다. 실패 화면은 한글 확인 단계와 오류 분류를 표시한다. 문제 해결은 마지막 성공 조회보다 최근 실패 작업 ID를 우선 선택하며 로그아웃 시 초기화한다. 수동 필터는 계속 변경 가능하다. 첨부0.20.2 ZIP에는 활성화 기록 없이 호가 조회 성공만 있어 실패 원인을 판단하지 않았다.

사용자 요청으로 일반 앱의 실거래/이미지 기본 선택을 ON으로 변경했다. OpenTradingReviewAsync는 최종 검토 사용자 동작에만 부족한 전송 권한을 검증하며, 실패한 단계는 같은 작업에 남긴다. 로그인 복원 시 대량 조회나 자동 판매는 시작하지 않는다. 기본 선택과 검증된 Access.Trading은 별개다. 이미지 OFF/로그아웃은 실제 로딩을 중단한다.

## 0.20.4 판매 응답·속도 보완 (2026-10-29 KST)

0.20.4: HTTP400 판매/취소 오류는 ClientRequest와 HTTP 상태를 유지하며 최대16KiB 응답을 메모리에서만 조사한다. ResponseContract는 미디어/본문 종류를, ReadContract는 ServerSuccessFalse/True/Unverified, ServerMessagePresent/Absent/MessageMentionsSession, 크기 초과/파싱 불가 같은 고정 분류만 기록한다. 원문 message/HTML/쿠키/토큰/계정 값은 기록하지 않는다. success 또는 승인 플래그가 있어도 비200이면 승인 대기로 확정하지 않고 Unknown/자동 재전송 금지를 유지한다. HttpFailure와 항목 메시지에 HTTP 상태를 연결하고 전체 안내도 승인 대기 미확인으로 표시한다. FeeContextRouteReused는 소유/모드가 확인된 이동 경로를 재사용했다는 뜻이며 내용·가격 캐시 적중이 아니다.

## 0.20.5 승인 채널 추적

기존 OperationId 안의 ReadContract/Sales, endpoint Sell에 MobileConfirmationRequired / EmailConfirmationRequired / EitherConfirmationRequired / ConfirmationChannelUnknown / NoConfirmationFlag를 화이트리스트 이유로 기록한다. 저널 Approval과 상태 전이를 연결하지만 QR·토큰·주소·응답 원문은 기록하지 않는다. HTTP400은 Unknown이며 승인 대기 증거로 간주하지 않는다. ID 없는 후보는 근거 부족으로 유지한다. 로그는 SQLite 거래 상태의 대체물이 아니다. 전체 경로는 OPEN-SOURCE-FLOW-AUDIT.md.

## 0.20.6 예약 진단

갱신 완료의 기존 OperationId 아래 InventoryContract/C10에서 InventoryReservedRows(예약된 자산 종류 수), InventorySelectableRows(선택 가능한 자산 종류 수)를 고정 이유로 기록한다. 이전 요청 열기는 EvidenceChecked/InventoryReservationOpened와 연결하며 현재 계정·자산을 내부 비교할 뿐 ID/상품명/원문은 로그에 넣지 않는다. 예약 수량과 자산/description 총계는 서로 다른 지표다. 사용자99b85195 진단은 조회1건 성공만 증명하며 예약의 상세 원인까지 기록돼 있지 않았다. 앞으로 보관함의 '예약된 항목 확인'→선택 요청 상태 확인 후 해당 작업을 내보내면 서버 대조 근거를 추적할 수 있다. 진단 ZIP에는 사용자 DB/인증/원문을 추가하지 않는다.

## 0.20.7 서버 상태 동기화

0.20.7: InventoryLocalPendingRows는 Steam 예약 수가 아니라 로컬 전송 확인이 필요한 자산 종류 수다. InventorySelectableRows는 서버 snapshot 기준으로 독립 기록한다. 판매목록 갱신의 서버 ID/상품/금액/수량 일치 시 EvidenceChecked/ServerInventorySync에 이전·새 상태를 남긴다. PendingSubmission은 새 전송 차단 이유이며 보관함 선택 실패가 아니다. 원문·계정·쿠키·QR은 기록하지 않는다. 기존 이유 토큰은 과거 로그 해석을 위해 보존한다.


## 0.20.8 단일 가격과 수량 계약

EvidenceChecked/AssetStateObserved는 원요청 성공이 아니라 현재 동일 자산의 서버 상태를 기록한다. 원본/자산ID/등록ID는 로그에 추가하지 않고 to상태만 기록한다. StackContract 오류는 제출 전 mode 변경/미지원 사유를 UI에 표시하고 intent·POST 이전 중단한다. 가격 기준은Immediate만 사용자 선택이며 과거enum/로그 토큰을 삭제하지 않는다.


## 0.20.9 활성화와 전송 검사

0.20.9: 활성화의 SaleableAssets 단계에서 ServerSaleableAssets(실제 서버 플래그/수량과 지원 계약에 맞는 자산 종류), InventoryUnmarketable, InventoryLocked, StackQuantityUnsupported를 각각 기록한다. 별도의 LocalSubmissionPending은 로컬 전송 보호 때문에 새 전송 확인이 필요한 자산 종류 수이며 이 수가 양수라고 활성화를 실패시키지 않는다. 최종 전송의 PendingSubmission은 그대로 차단 사유다. 값/ID/원문 대신 고정 이유와 건수만 기록한다. 사용자 f5413571 진단은 세부조건이 없어 원인 미확정이며 새 버전으로 구분할 수 있다.


## 0.20.9 SSIM 실행 중 자산 잠금

ConcurrentAssetOperation은 실행 중 같은 자산의 다른 작업 때문에 전송 전에 차단된 결과다. ItemResult/CancelResult Outcome=Blocked, State=Failed/미전송으로 표시하고 새로운 intent/HTTP POST가 없어 Unknown으로 처리하지 않는다. 원문·계정/asset key를 로그에 추가하지 않는다. 기존 Unknown의 영속 보호와 이 일시 잠금은 별개다.

## 0.20.10 서버 동기화·새 승인

FreshOwnedAssetNewApproval/UnknownPreserved는 새 승인에서 현재 보유·판매목록을 확인했고 원 불명 기록을 보존했음을 뜻한다. 원 요청 성공/실패 확정이 아니다. PreservedWithoutReplay는 이전 배치가 archive에 원자 보존됨을 나타낸다. PendingSubmissionSettling/ServerInventory/ServerListing/UncertainStack은 각각 직전 요청 대기/서버 수량·판매 가능 검증/현재 게시·승인대기/불명 스택의 전송 전 차단이다. 고정 화이트리스트 이유·건수만 남기며 계정/자산/원문/쿠키/QR을 기록하지 않는다.

## 0.20.11 승인 경로·공통 장터 판별

ListingMetadataUnavailable은 상세 가격/등록 ID가 없는 서버 membership 건수다. 원문/계정/자산/쿠키/QR을 기록하지 않는다. 기존 OperationId/BatchId/OperationItemId·HTTP·가격·intent·승인 채널·게시 대조 이벤트를 유지한다. 이 이벤트나 AwaitingSteamGuard를 휴대폰 푸시 도착/실제 게시 성공으로 해석하지 않는다.

## 0.20.12 기능 동등성 감사

RequestNotSent는 현재 Executor가 판매 제출을 시도하지 않았다는 경계다. ItemResult Blocked/Validation 또는Cancelled와 연결 식별자를 남기고 결과Failed/나머지Ready로 보존한다. ConfirmationFlagsConflict는 명시적 확인 불필요와 확인 채널 요구의 모순이며 Unknown이다. 기존 POST 뒤 ResponseLost/EvidenceMissing은 그대로Unknown/자동 재전송 금지다. 원문·계정·QR·쿠키를 기록하지 않는다.

## 0.20.13 진단/복구 신뢰도

배치 ID가 같아도 계정/가격 snapshot이 다르면 전송 전 불변성 위반으로 종료한다. 취소 후 새 대상은 현재 보관함과 active/pending membership을 재검증하며 새 승인 전 POST하지 않는다. UI pending 수량·취소 원 sell 조회는 현재 계정으로 한정하고 다른 계정의 저널은 삭제하지 않는다. 진단 로그를 거래 확정의 권위로 사용하지 않으며 Unknown 원요청 ID/과거 감사 기록은 유지한다. 기존 민감정보 정제/디스크 실패/연결 ID/ZIP 사용자 검토 회귀는 최종 전체 검사에 포함했다. runtime의 보안/호환/회귀 보고서는 합성 테스트 증거이며 배포 ZIP/운영 진단에 포함하지 않는다.

## 0.20.14 사용자 입력·명령 검증

수량 입력의 유효↔오류 전이만 UiSelection(9004), C07, InputRejected/Committed 및 Validation으로 기록한다. 원문 숫자/입력 문자열·계정/자산 식별자·키 입력 내용은 저장하지 않는다. 키마다 반복 로그를 남기지 않는다. 명시적 scope 제거는 ScopeRemoved 전이이며 저널 삭제가 아니다. AsyncCommand Execute/ExecuteAsync가 동일 UI 작업 scope와 예외 경로를 사용한다. 새 사용자 시나리오 UJ02는 동일 OperationId의 HTTP/JournalWrite/항목 Committed3건 연결과 토큰·쿠키·계정 원문 부재를 검사한다. 기존 정제·회전·ZIP 검토/내보내기·SQLite 권위·Unknown 비재전송 보호는 유지한다.

## 0.20.15 가격 조정

하향 옵션의 명시적 선택은 PricePlan(5000)/E05/LowerValidStep(OFF는Immediate)로 기록한다. 새 reason을 안전 토큰 목록에 추가했다. 원문/계정/인증·HTTP 원문 없이 기존 UI OperationId→최종검토→배치/항목 quote→HTTP→SQLite 결과 경로를 유지한다. 기본/변경 금액 비교는 실제 검토 입력을 사용하며 최종 snapshot은 그 승인 가격이다. 로그나 UI 비교가 서버 체결 증거는 아니다. 기존 ZIP 정제·사용자 검토·저널 권위를 유지한다.
