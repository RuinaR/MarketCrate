# MarketCrate — 권장 아키텍처 (초기 설계, 미구현)

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
