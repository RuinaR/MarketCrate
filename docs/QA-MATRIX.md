# 정상·오류·복구 QA 매트릭스 — 6차 (2026-10-28 KST)

## 실행 범위

결과 집계는 최종 실행 후 아래에 기록한다. PASS는 **합성/Mock/격리 SQLite/비표시 WPF 범위**다. 실제 Steam 계정/응답 수집/거래/Guard 작업은 전혀 실행하지 않았다. 테스트 HTTP는 소켓 없는 HttpMessageHandler다. 이전 112개 기능 검사와 20개 UI 검사를 제거하지 않았다. 새로운 진단/누락 수정 검사는 상태·전송 횟수·영속 상태·연결 이벤트/정제 결과까지 확인한다.

최종 Debug 집계: **139/139 기능·합성 HTTP·저장소·진단 PASS, FAIL0; 23/23 비표시 WPF PASS, FAIL0; 빌드 경고0/오류0**. 기존112+신규27, UI기존20+신규3. Steam schema v3 변경 후 전체 회귀도 통과했다. Release 산출물 검사는 별도 마감 기록 참조. `scripts/Verify.ps1` 순차 빌드/전체 테스트, `scripts/Publish.ps1` Release PE/로컬 데이터 제외/고지 검사. 실제 앱 실행·설치는 포함하지 않는다. 콘솔 출력과 PNG는 ignored `runtime/stage6-*.txt`, `runtime/ui-tests/stage6-*.png`에만 보관한다.

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
| E29 | 로그 권한 거부/가짜 disk full 예외 | WriteFailure health, 판매 결과/호출 수 불변 | S6.DiskFailure | PASS(주입); 실제 운영 디스크를 채우지 않음 |
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
