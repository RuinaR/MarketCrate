# 정상·오류·복구 QA 매트릭스 — 7차 (2026-10-28 KST)

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
