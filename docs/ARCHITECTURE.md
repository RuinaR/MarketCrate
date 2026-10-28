# MarketCrate — 아키텍처 (최초 Mock 구현 / 후속 계획)

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
