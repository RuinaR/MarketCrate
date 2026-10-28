7차 Release 검사 수정: SQLite 파일명 제외 정규식이 정상 의존성 Microsoft.Data.Sqlite.dll까지 거부했다. DB/sidecar 확장자 끝으로 한정해 수정하고 DLL 허용·sqlite/sqlite3-wal/db-shm/.env/session.json/private.key 차단 7개 파일명 회귀를 확인했다. 실제 비밀파일·DB는 생성하지 않았다. 최초 publish는 이 정적 검사에서 FAIL이며 실행파일은 실행하지 않았다.

## 2026-10-28 KST — 8차 GUI·실연동 계약 검증 준비

- 기준 HEAD/origin `4a18077` clean/fetch 확인. AGENTS/정의/상태/75개·QA/진단/Steam/격리 GUI 문서 검토. 전용 guest 제공 정보 없음. 호스트 대체 GUI/VM 탐색/Windows 기능 변경/다른 프로세스·입력·화면 접근 안 함.
- Steam AnyReview 알림 누락을 실제 binding 상태 테스트로 확인·수정. 모달 focus boundary 및 active own-window guard, ComboBox focus/open trigger, disabled NumericStepper peer 표준 예외 보완. UI 테스트의 standalone style 미설정으로 FAIL1을 확인한 뒤 fixture를 수정해 관련31개 PASS; 물리 GUI 문제로 가장하지 않음.
- QR 갱신 빈 선택필드 처리와 취소 Schema 오분류, HTTP 사용자 취소→Transport halt 및 VM 로그인 상태 소실을 수정. 승인 flag 0/1 외 정수는 계약 오류로 제한. AuthState endpoint/원인/operation 연결과 secret sentinel 누출0 단언 추가. Unknown·예약·반환 인과 기준 완화 없음.
- 게스트 전용24 case/manifest/해시/런타임 확인 수동 launcher와 host packaging-only script, offline App profile/분리 데이터·Steam factory 차단 구현. marker/env는 운영자 선언이며 VM 보증 아님. guest launcher/실앱은 미실행.
- 공식 판매 JS 재다운로드 hash는5차와 동일; 공개 auth proto/maintainer source·MIT 확인, 코드/의존성 도입 없음. 실제 인증 응답·계정/API/이미지·거래/Guard 미실행.
- 전체 기능/합성 HTTP/SQLite/진단 165/165 PASS, 비표시 WPF 31/31 PASS, FAIL0, Debug 경고0/오류0. Release·게스트 패키지 정적 마감은 소스 커밋 후 반영. Release/패키징 정적 결과와 commit은 마감 후 반영. 최초75개 판정47/15/0/4/9 유지, 실제 입력·Popup·DPI·스크린리더·장기누수·설치·서명은 미검증.

# MarketCrate — 누적 작업 로그

7차 추가 반환 제한: 원래 단일 수량이었어도 취소 후 returned.Stackable/수량 증가·기존 가용 수량이 있으면 proof로 확정하지 않는다. 동일 ID라도 현재 정확히1개/기존0개인 제한 경로만 합성 대조하며 나머지는 Unknown/예약 유지. 실제 nonstack/스택·ID 의미와 인과 증거는 여전히 미검증이다.

### 7차 자동 검증 마감

- 전체 기능/합성 HTTP/SQLite/진단 **158/158 PASS, FAIL0** (기존139+신규19), 비표시 WPF **28/28 PASS, FAIL0** (기존23+신규5), Debug 빌드 경고0/오류0. 마지막 기능 보강 후 전체 회귀 실행. 예상치 못한 binding 오류0/Window·PresentationSource0.
- 실제 PNG 디코딩·재활용60회·전송 중복/취소·재시도/실패 상태와 보관함/판매/검토/추정/진단 이동 이미지를 직접 검사했다. NumericStepper 36px/텍스트 줄바꿈/출처/기준별 부족 수량 안내를 수정·재확인했다.
- 단기 합성 예: 이미지100회 약2ms wall/31ms 자체 process CPU/managed allocation 약1.36MB, LRU4개/280B; 썸네일60회 약1835ms wall/1406ms 자체 CPU/약0.89MB managed allocation, cache210B. 자체 process만 읽었고 CPU 계수 해상도/다른 자체 스레드가 포함된다. 실이미지/네이티브 메모리/장시간 누수/FPS/다른 앱 영향 측정으로 확대하지 않는다.
- 실제 Steam 이미지/인증/보관함/가격/fee/판매/취소/Guard 및 실제 창/물리 입력/모니터 DPI/installer/VM 실행은 NOT VERIFIED. 소스/배포 검사 수정 커밋 `6828294` Release publish PASS(경고0/오류0): PE·로컬 데이터 제외·라이선스 고지 확인, Core DLL의 해당 Git commit metadata 정적 확인. 실행파일·설치 미실행. 구현 `e40492d`, 배포 검사 수정 `6828294`는 origin/main push 완료.

## 2026-10-28 KST — 7차 미완성 항목 구현

- 시작 main/origin `158a606`, clean/fetch 확인. AGENTS·75개 추적표·QA/진단/설계 조사 후 같은 ID와 6차 판정을 보존했다. B04/C02/D02 로컬 COMPLETE, B05 PARTIAL로 개선(47/15/0/4/9), 실제 응답/계정/DPI 판정은 승격하지 않음.
- 공식 공개 HTML/JS에서 icon fields/CDN 경로/기간 중앙값 의미, 공식 GetOwnedGames 문서와 Sandbox 구성 자료를 확인했다. 새 SDK/패키지/타 제품 코드 도입 없음, source만 읽고 실행하지 않음.
- 메타데이터 join/side maps, opt-in 무인증 단일 CDN/제한 캐시/중복 억제/취소/재활용·worker decode/freeze, inventory/listings/review/대표 이미지와 오류 UI 구현. 초기 합성 PNG fixture 오류를 BCL CRC/ZLib로 직접 생성한 유효 PNG로 수정, 실제 decoder까지 검증했다.
- account별 manual/observed context catalog와 v3 저장/복원/오류 상태를 추가했다. 로컬 입력 실패가 로그인 상태를 만료시키던 문제를 수정. 로그아웃 후 늦은 load의 새 catalog 오염도 차단. 통계 모드/기간 변경 후 늦은 응답 폐기.
- exact average는 계속 미제공. 대체 weighted median estimate의 기간/표본/한계/불변 evidence/digest를 구현하고 같은 합계의 바뀐 표본도 거부한다. 최종 preflight의 동일 상품 반복 조회를 제거하고 마지막 freshness 검사, publisher fee 누락 차단 보완. 실제 stack return/Unknown 인과 증거 기준은 그대로다.
- 정상/실패/취소/중복/403·429/MIME/픽셀·바이트/캐시/변조 이력/설정·저널 재시작/진단 연결에 합성 테스트를 추가했다. 자체 PNG를 육안 검토해 과도하게 늘어난 스테퍼를 수정. 앱/최상위 창/실제 입력/다른 프로세스·화면/실계정 접근은 없음. 최종 결과/커밋은 마감 기록 참조.

## 2026-10-28 KST — 6차 요구사항 감사 및 실사용 진단

- 최종 마감: 구현 `5b44e0d`, 진단 보완 `fab286a` 각각 origin/main push 완료. 보완 후 전체 **139/139 + WPF 23/23 PASS**, FAIL0/경고0/오류0. `fab286a` Release PE/데이터 제외/MIT 고지 PASS, Core DLL의 소스 commit 포함 정적 확인. 최신 진단/최종 검토 PNG 육안 재확인, 실제 Window/앱/계정/타 프로세스 접근 없음. 남은 항목은 추적표 PARTIAL/MISSING/BLOCKED/UNVERIFIED로 유지한다. 최종 문서 커밋은 완료 보고 참조.

- 구현/감사 커밋 `5b44e0d` origin/main push 및 Release PE/데이터 제외/라이선스 검사 PASS. 마감 점검에서 writer 실패의 원인 분류/OperationId가 health에 부족하고 변조 null stack 프레임이 export를 중단할 수 있음을 발견해 후속 보완했다. 원문 없이 typed 실패/OS 숫자 코드/작업 ID를 화면·manifest에 제공하고 null 프레임은 제외한다.

- origin/main `33cd152` fetch/clean 기준, AGENTS/정의/설계/기록/QA/실연동 자료와 관련 코드 경로를 대조했다. 75개 ID 추적표를 작성했고 COMPLETE44/PARTIAL16/MISSING1/BLOCKED5/UNVERIFIED9(로컬/합성 범위)을 분리했다. 실계정/실제 응답 수집은 전부 미실행이다.
- 실제 결함: 실험 화면 검색·필터·정렬/선택 해제·게임별 판매 필터 누락, 갱신/게임 변경 시 선택 소실, MainWindow 닫기에서 Steam.Busy 누락, stale 재가격의 검증 순서가 취소 이후였음. 관련 코드/UI/수량 상한 복원을 수정했다. 자동 발견/이미지/실평균가는 별도 미완성으로 명시했다.
- 고정 코드/건수 진단을 재사용·연결하면서 중앙 구조화 JSONL 시스템을 추가했다. operation/correlation/batch/item, UTC/버전/commit, 고정 EventId/RequirementId, HTTP·저장소 오류/상태·가격/재시도, 정제 stack/inner 위치와 사용자 진단/로컬 ZIP UI. 읽는 중 writer 파일 공유 오류를 첫 주입 검사에서 찾아 ReadWrite 공유로 수정했다.
- 보안 설계: 자유 텍스트/URL/원문/인증/계정·자산 식별자는 생성 지점부터 제외, sink/export 재정제. 예외 message/path/전체 객체 금지. bounded async writer와 누락/실패 health, 보존 한도, 검토 후 원자 ZIP 저장. 로그 실패로 재판매/취소하지 않는 것을 검증했다. 기존 거래 저널은 별도 유지한다.
- 새 재가격 승인은 원 intent를 변경하지 않고 확정 취소 proof를 새 intent에 연결/원자 Superseded 처리. 이전 앱이 새 상태를 오해하지 않도록 Steam schema v3 마이그레이션, 구 내용/요청/예약 보존. 미확정 취소/이미 전송한 replacement는 revision 불가.
- QA는 합성 handler, 격리 SQLite, 자체 STA/RenderTargetBitmap만 사용. 디스크 full은 writer 예외와 in-memory SQLite page 상한으로 주입(실제 사용자 디스크를 채우지 않음). 비밀 marker/위조 파일/ZIP 재정제, 작업 ID/commit, 부분 실패/저널 실패/호출 횟수/재시작을 assert했다. 최종 전체 Debug **139/139** 및 비표시 WPF **23/23 PASS**, FAIL0, 경고0/오류0. Steam v3 변경 후 회귀도 통과. 최종 App startup의 app-local default binding listener 제거는 정적 보안 점검/Release 컴파일로 확인하고 실제 startup 실행은 하지 않는다. Release/커밋 결과는 마감 기록에 추가한다.
- 공개 약관 Automation/Enforcement 본문을 재확인했다. support confirmations 링크는 이번 도구 응답이 이미지 한 줄이라 본문 재확인 실패로 기록했다. 새로운 API live 호환성/정책 허용을 추정하지 않았다. 다른 앱/프로세스/입력/화면/실계정 접근 없음, 새 SDK/패키지/경쟁 코드 도입 없음.

## 2026-10-28 — 5차 사용자 연결·KRW·스택·대조/재가격

- 시작 HEAD/origin/main `73ad542`, 작업 트리 clean. AGENTS·현황·정의·QA·아키텍처·판매 계약·실연동 조사를 읽고 fetch했다. 실제 계정/거래·외부 앱·입력·화면 접근은 하지 않고 현재 저장소를 이어 수정했다.
- 공식 최신 판매 번들에서 새로운 wallet floor/step + ajaxfeestructure rate/cap 및 amount 계약을 확인했다. 공개 소스만 읽었으며 authenticated fee 응답/실계정 계산은 확인하지 않았다. KRW/USD 정수 Money/wire, 정확한 fee와 역산 불가 금액 차단, 서버 응답 모순 중단을 독립 구현했다.
- 새 Steam MVVM/UserControl에 로컬 QR·직접 로그인 확인·세션 종료/취소·실제 provider 갱신/게임/선택/시세·가격 검토·판매 관리·결과/증거/감사 이력·재가격 재개를 연결했다. 기본 policy Blocked 및 stack capability를 별도화, 사용자 동의로 Valve 허가를 가정하지 않는다. Mock과 실제 source/상태/합계를 분리했다.
- MIT QR v2.0.0 소스 6파일을 고정 커밋으로 도입, 원 고지·LICENSE 유지 및 nullable 지시문만 추가. 순수 계산/무네트워크를 확인하고 output/publish에 LICENSE/고지를 함께 넣도록 했다. 패키지/SDK 설치 없음.
- 공유 호가 key에서 context를 제거해 같은 장터 잔량의 중복 사용을 막았다. 보관함과 예약 반영 가용량, Unknown 보존, 동일 자산의 반환 proof로 해당 예약만 해제, 확정 배치 archive 후 새 배치를 처리한다. v1→v2 이관과 command/reprice audit는 별도 실저널에 보존한다.
- 재가격은 승인 target 불변→취소→목록 부재+동일 자산 수량 반환→새 preflight→후속 key 먼저 저장→등록이다. 스택/외부 등록/ID 교체/불명/5분 stale/중복은 차단한다. listingid 없는 후보는 시각/기존 ID/수량·가격까지 보여 주되 인과관계 없이 확정하지 않는다.
- 합성 HTTP 서버(소켓/listener 없음)와 동일 UI/어댑터로 로그인→KRW 스택 3/4/3→승인 대기, 서버 fee 충돌·currency mismatch, 비스택 취소/반환/재가격/재시작 및 스택 반환 추정 차단, logout 늦은 응답, 동의 철회, QR 만료/거부/CAPTCHA/네트워크, archive·부분 예약 해제·legacy 이관을 검사했다.
- UI 검사에서 템플릿 생성 전 컨트롤 검색 실패를 이름 기반 접근으로 수정했다. 렌더에서 standalone 글자색, QR 빈 상태, 실험 화면의 Mock 문구/가로 폭 혼동을 발견해 실제 소스를 수정했다. 실제 창/사용자 입력 검증으로 확대하지 않는다.
- 새 실험 목록에서도 반복 DB 조회/전체 선택 알림을 발견해 일괄 예약 조회·RangeObservableCollection·알림 batching을 적용했다. adapter-backed 10,000개 합성 목록으로 검사하며 장시간 성능/실서버 부하 검증은 하지 않는다.
- 최종 안전 검토: 스택 수량 증가가 취소 반환의 인과 증명이 되지 않음을 발견했다. 원래 snapshot과 직접 연결된 비스택 단일 자산만 자동 반환 proof를 허용하고 스택/외부 등록은 Unknown·예약 유지로 제한했다. 긍정적인 스택 반환 추정 테스트를 안전한 차단/재시작 보존 회귀로 대체했으며 이 제한을 완료 보고에 공개한다.
- 최종 회귀: `scripts/Verify.ps1`, 5프로젝트 Debug 경고0/오류0, 도메인/합성 HTTP **112/112**, detached STA WPF **20/20 PASS**, FAIL0. 마지막 변경 소스 이후 Release publish·PE/데이터 제외·QR MIT LICENSE/고지 포함 검사 PASS. 산출물 실행/설치/서명/공개 배포는 안 했다.
- QR/검토/보관함/통합 PNG를 직접 검토했고 생성물은 ignored runtime/artifacts에만 둔다. 금지 API·민감값 패턴·diff 점검 후 소스/합성 테스트/문서/허가된 QR 소스만 commit/push한다. 미지원·제재 위험은 현황에 누적하며 실판매 완성을 선언하지 않는다.
- 최종 검토: 금지 API 검색의 Process.Start는 이전 승인된 owned headless 테스트 자식뿐이며 새 타 앱 접근 없음. QR vendor 6파일은 내려받은 고정 원본과 nullable 지시문을 제외하고 문자 단위 일치 확인. upstream 주석/EOF 공백은 원본 보존으로 확인했고 자체 소스·테스트·문서 diff check는 PASS다. 새 소스 제한 이후 전체112/112·WPF20/20·Release/라이선스 검사도 다시 PASS했다.
- 구현·조사·검증 커밋 `f7bd8aa`을 origin/main에 정상 push했다 (`73ad542..f7bd8aa`). 실제 로그인/거래/Guard·타 앱/전역 입력/화면 접근 없음. 다음 우선순위는 별도 승인된 실제 QR/읽기 검증, 정책 근거, 통화/게임별 검증 및 스택/외부 등록의 취소 인과 증거 어댑터다. 실제 일괄 등록 지원 완료/릴리즈 준비 완료를 선언하지 않는다.

## 2026-10-28 — 4차 조사·실험적 연동 구현

- 시작 HEAD/origin/main `9455dc9`, 작업 트리 clean. AGENTS/현재 상태/정의/QA/아키텍처/실행 계약을 확인하고 fetch했다. 사용자 승인으로 비공식 **소스 구현** 범위를 확장했으며 계정 로그인/거래 시험은 승인 범위 밖으로 유지했다.
- GitHub 최신 고정 커밋 세 곳의 관련 소스/라이선스와 공식 Market/약관/OpenID/publisher 문서를 읽었다. 공개 orderbook 1회 무인증 응답의 중첩 JSON/eCurrency/가격·잔량 쌍을 직접 확인했다. 연구 소스/응답은 runtime에만 보관하고 코드 복사/실행/의존성 설치 없음. 최신 지원 주장과 직접 관측 한계를 분리했다.
- Infrastructure/Steam에 QR protocol→새 자체 메모리 쿠키 세션·동의·host 제한 HTTP·inventory/orderbook/median history/listings·sell/cancel·별도 SQLite intent/reconcile를 독립 구현. 기존 provider/executor 도메인 계약과 단일 승인 coordinator를 재사용했다. live 단가 USD exact fee만 허용하며 기존 KRW Mock은 유지, unsupported 통화 회귀 케이스는 EUR로 바꾸고 USD 통화/수수료 경로를 추가 검사했다.
- request key는 로컬 중복 방지용이며 서버 idempotency 지원을 가정하지 않는다. Unknown/서버ID 없음/취소 목록 부재는 자동 성공·재등록으로 바꾸지 않는다. 원화/스택/확정 취소 후 실제 재가격/live 수동 retry/로그인 WPF는 미완성으로 명시했다. 일반 앱에는 live 구성 주입 없음.
- 합성 HTTP로 전송 코드의 form/인증 만료/접근 제한/호가·통화·수수료/승인 대기/서버ID 대조/타임아웃·재시작/취소 불명/중복 키/순차성/자격 불일치/미래 스키마/비밀값 비저장을 검사했다. 초기 검사의 timeout 후 중단된 세션 재실행은 기존 결과 반환이 아닌 차단이 맞아 테스트 기대를 수정했다. 실제 안전 검사를 완화하지 않았다.
- 비표시 안전 안내 렌더에서 좁은 viewport 오른쪽 여백을 보완했고 카드 경계 검사를 추가했다. Steam 저널은 불변 배치+변경 result 행 transaction으로 저장하도록 구현했으며 128행/두 번째 행 실패 rollback 재열기 검사를 추가했다.
- 최종 전체 회귀: 빌드 경고0/오류0, 도메인/저장/MVVM/합성 Steam HTTP **87/87 PASS**, detached STA WPF **17/17 PASS**, FAIL0. 기존58/16 회귀 유지. 새 안전 화면 자체 PNG 시각 검토·카드 경계 검사 PASS. 변경 소스 이후 Release publish·PE/로컬 데이터 제외 검사 PASS. 앱 실행/설치/공개 배포는 안 했다.
- 전체 diff/비밀값 패턴/금지 API 검색에서 문제 없음. 기존 Process.Start는 이전 승인된 프로젝트 headless 테스트 자식뿐이다. raw source/응답/이미지/DB/로그/bin/obj/산출물은 Git 제외. 실제 Steam 인증/거래/Guard·다른 앱/화면/입력/프로세스는 건드리지 않았다. 후속은 QR/실계정 UI와 원화/스택 계약·확정 취소 및 불명 대조 근거 보강이다.
- 구현/조사/검증 커밋 `61fbd58`을 origin/main에 정상 push했다 (`9455dc9..61fbd58`). 현재 일반 앱의 실거래 차단은 유지하며 LIVE_READY를 선언하지 않는다. 최종 기록 동기화 커밋은 최종 보고에 기재한다.

## 2026-10-28 — 3차 구현·검증 마일스톤
- 조사 커밋 `db8a55c`를 origin/main에 정상 push한 뒤 기존 WPF/MVVM/Core/SQLite 구조를 이어 구현했다. 새로운 라이브 연동/의존성 설치/원격 변경 없음.
- 검토: 개별 등록 단위 가격 편집, Steam/게임 수수료·실수령/양쪽 합계 갱신, 부족 잔량 빈 단가 행·예상 미체결 안내, 수동 가격 전략/닫기/갱신 초기화, 조회 시각/신선도 및 최종 재검증.
- 관리: 개별/전체 선택, 취소 후 재등록 가격 변경, 확정 실패 배치·관리 작업만 명시적 재시도. 시도 번호별 요청 키·기존 성공 보존·stale/double 승인 차단. 재가격 취소 확인 이후 후속 미전송만 재개하고 Unknown은 재전송하지 않는다. 후속 교체의 취소 전까지 원래 예약을 유지한다.
- 안전 결함 수정: 구형 관리 Unknown(Action=None)을 과거 등록 증거가 덮어쓰지 않게 Registration/Cancel/Reprice 출처를 구분. 모달/배경 작업 동시 조작과 닫힌 모달 입력 오류 잔류를 보완. Mock 재시도 결과 대조도 현재 시도 키를 사용한다.
- SQLite: v2 보존 이관·quick_check·설정 보존·고정 진단. 대량 배치/관리에서 전체 JSON 반복 쓰기를 발견해 불변 헤더+결과/listing 변경 행 transaction 저장으로 보완. 캐시는 단일 writer의 immutable 레코드이며 초기화 시 무효화. 전체 rollback/재열기 검증 유지.
- UI: 신규 검토/재가격/재시도/로딩·조회 실패/대량 재활용 자체 PNG 생성·직접 시각 검토. 로딩 중단 버튼 clipping을 발견해 패널 여유를 늘리고 위치 검사를 추가했다. 실제 창/포커스·전역 입력/다른 화면은 사용하지 않았다.
- 검증: 마지막 전체 회귀 `scripts/Verify.ps1`, 빌드 경고0/오류0, 도메인/SQLite/MVVM **58/58**, WPF **16/16 PASS**. 이후 UI 오류 수집 범위를 새 화면까지 확장한 관련 UI 검사도 16/16 PASS. FAIL 0. 재실행은 새로운 기능/저장 구조/발견 결함/추가 검사 확인에 한정했다.
- 장애 검증: 프로젝트 테스트 DLL만 CreateNoWindow headless 자식으로 순차 실행하고 자식이 자기 Exit. intent 저장 후 재시작 Unknown/재전송0 및 미커밋 SQLite transaction rollback 확인. 타 앱 검색/종료/메모리/화면/입력 접근 없음. 실제 전원 차단/GUI 강제종료 시험은 아니다.
- 부하/배포: 10,000개 뷰 컨테이너 시작9/끝7 및 반복 스크롤 선택 보존, 128행 SQLite 배치와 저장 payload <1MB 검사. 장시간/최대 규모 실행 인증은 아니다. `scripts/Publish.ps1` 최신 Release framework-dependent exe 생성/PE/DB·로그·키 제외 PASS, 산출물 실행·설치 안 함. 데이터 경로는 기존 LocalAppData 유지.
- 진단: timestamp/enum code/count만 DB 최대500개, 시작 DB 오류는 앱 전용 bounded 고정 코드 파일. 임의 예외 메시지·계정/아이템·비밀정보를 기록하지 않는 검사 추가. 이미지/DB/로그/빌드 결과는 Git 제외.
- 미검증/차단: 실제 입력·열린 팝업·모니터 DPI·스크린리더·설치/서명·장시간/전원 차단·실제 Steam 로그인/거래/Guard. 관리 Unknown의 외부 증거 없는 재전송은 차단. 필수 실제 일괄 등록 목표는 IntegrationBlocked 유지.
- 후속: 허용된 실연동 경로/권한 확보 전 실제 어댑터 금지, 다중 배치 탐색·외부 대조 도구·대량 UI 선택 확장·배포 감사. 최종 diff/민감정보 패턴 검사 PASS. 구현 커밋 `1e6c280`을 origin/main에 정상 push했다. 조사 `db8a55c` 및 구현 커밋 모두 원격 반영 완료.

## 2026-10-28 — 3차 조사 마일스톤
- 시작 HEAD `9ce53df`, 작업 트리 clean. fetch한 origin/main과 일치, AGENTS/현황/QA/아키텍처/판매 계약 확인.
- 공식 Steam OpenID·Web API 인증·Inventory/Economy/Market API 및 약관을 조회. publisher/소유 앱/secure server 제한과 개인용 장터 권한을 구분했다. 공식 Support 직접 본문 접근 실패 후 공식 검색 색인의 확인 절차 본문을 확인했다.
- 오픈소스·서비스 공개 문서만 조사, 설치/실행/로그인/API/거래/모바일 승인 없음. 결과와 한계·라이선스·후속 해제 조건을 STEAM-INTEGRATION-FEASIBILITY에 기록. 실제 쓰기는 IntegrationBlocked 유지.

## 2026-10-28 — 최초 개발 지침 패키지 작성
- 사용자 요구사항을 기반으로 제품 목표, UI/UX, 인증·안전, 일괄 판매 실행 계약, QA, 레퍼런스를 문서화.
- **실제 앱 코드, 실행, Steam 로그인, 실거래, 테스트, Git 푸시는 수행되지 않음.**
- 향후 Codex가 매 작업 단위에 `날짜/목표/수정 파일/설계 결정/실제 확인한 검증/미실행 검증/위험/commit hash/push 결과` 추가.
- 기능 구현 진척은 `CURRENT-STATUS.md`를 단일 현재 상태 문서로 사용.

## 2026-10-28 — 최초 Mock 구현
- 시작: HEAD `8fcad5c`, `main`→`origin/main`, origin `RuinaR/MarketCrate`, 작업 트리 clean. AGENTS 및 동봉 설계/QA/계약 확인. 기존 문서와 요구사항 보존.
- 웹 조사: 지정 6개 레퍼런스, 약관·API 문서·Support URL 확인. Support 본문 추출 실패와 raw LICENSE Cache miss는 REFERENCES에 명시. 외부 코드/자산/인증 흐름 미도입.
- 구현: 4개 프로젝트 솔루션, 다크 토큰/커스텀 템플릿, Mock 보관함·수량·가격 전략·단가 편집 검토·단일 최종 배치 명령·개별 결과·대기/불명 표시, 정수 가격/수수료 엔진, Blocked executor, 디스크 저널/복구.
- 설계: 실연동 경로 미확인으로 실제 읽기·쓰기 미구현/차단. SQLite 대신 합성 Demo v1 JSON을 사용하고 live 저장소로 재사용 금지. journal intent를 요청 전에 저장, Unknown 시 후속 중단·재전송 금지. 단일 활성 배치·독점 파일 lease 유지.
- 수정한 결함: 잘못된 가격 입력이 이전 바인딩 값으로 제출되는 경로를 Validation.Error→명령 차단으로 수정. 작업 취소와 앱 수명 토큰 분리. 지연 Progress 큐로 완료 상태가 덮이지 않도록 인라인 진행 통지 사용.
- 검증: `scripts/Verify.ps1`, SDK 10.0.301, 빌드 경고 0/오류 0, 계산/배치/디스크/MVVM **25/25 PASS**. 초기 테스트 22/24에서 TEMP 파일 이동 권한 오류 2건 확인→허용된 workspace runtime 격리 경로로 변경 후 25/25. 재실행은 이 수정과 추가 최종 명령 테스트 확인 목적.
- 개발 도구: `dotnet --info` workload 정보 수집이 서비스 관리자 권한 오류를 보고했으나 SDK 존재 확인 후 workload resolver 없이 빌드 성공. `dotnet new`의 전역 template cache 쓰기 거절은 workspace DOTNET_CLI_HOME으로 해결. SDK 최초 실행이 HTTPS 개발 인증서 설치 메시지를 출력함; 인증서 신뢰/추가 설치/전역 설정 변경 명령은 실행하지 않음. 이후 검증 스크립트는 인증서 생성·telemetry·workload 갱신을 비활성화.
- NOT VERIFIED: GUI 실행/렌더/키보드/호버/DPI/접근성/장시간 부하·실제 Steam 로그인/조회/등록/승인. 대량 카드 가상화·상세 취소/재시도·SQLite·배포는 미완료.
- 안전: 앱/브라우저/캡처/다른 프로세스 목록·창·입력·메모리 조회 없음. Steam 계정·인증정보·실거래 요청 없음. 정적 금지 API/민감정보 검색과 diff 검토 후 명시 파일만 커밋 예정.
- 다음: 승인된 앱 자체 창 QA, 남은 컨트롤/대량 UI, Valve 허용 경로 검증. 실등록 목표는 LIVE_INTEGRATION_BLOCKED 유지. Git 결과는 아래 후속 기록/최종 보고에 추가.
- 마감 검토: C#/XAML 소스 서식 정리. 솔루션 기반 formatter는 격리 환경 named pipe 권한으로 실패해 권한 확대 없이 폴더/파일 formatter로 대체했다. XAML 정리 후 앱 빌드도 PASS. staged diff의 추가 EOF 공백을 제거했고, 금지 API/민감정보 패턴 검색 결과 일치 없음. 빌드 출력/테스트 runtime/비밀 설정은 Git 제외.
- Git 결과: 구현 커밋 `9b904bf` (`feat: implement MarketCrate WPF mock workspace and durable batch flow`), `origin/main` 정상 push 성공 (`8fcad5c..9b904bf`). 로컬 .git 쓰기 제한에는 승인된 Git 명령만 사용했고, 강제 push/원격 변경/타 브랜치 조작 없음. 이 결과 기록은 별도 문서 커밋으로 누적한다.

## 2026-10-28 — 2차: 창 없는 UI 검증·SQLite·취소/복구

- 시작: HEAD `65e3ef4`, 작업 트리 clean. `git fetch origin main` 후 원격과 동일함을 확인했다. 이번 사용자 승인은 별도 STA 자체 컨트롤 렌더이며 실제 창·활성화·전역 입력·타 앱 접근으로 확대하지 않았다.
- 화면: MainWindow에서 MainView/UserControl 분리, detached STA 검사 프로젝트 추가. RenderTargetBitmap으로 크기/출력 밀도·검토 오류·배치 결과·취소·컨트롤 상태 PNG 생성/시각 검토. Application.Windows=0, PresentationSource=null 확인. VSM Focus 상태는 시각 모의이며 실제 포커스 이동 없음.
- UI 개선: 검색 placeholder·원형 Radio·상태 색 차이·Stepper 범위/peer/overflow/템플릿 핸들러·스크롤 thumb 최소 크기, inventory 전용 context menu 범위. 카드 VirtualizingTilePanel/recycling과 목록 가상화, 필터·전체 선택 알림 일괄 처리, 결과 변경 행만 갱신.
- 렌더 재현으로 찾은 문제: WPF generator 초기화 순서 및 재활용된 컨테이너가 created=false로 돌아올 때 시각 트리에 재부착하지 않는 누락을 수정. ToolTip은 부모에 붙일 수 없어 독립 컨트롤로 렌더했고, 상태 clock은 테스트 dispatcher의 렌더 큐만 처리해 색 차이를 확인했다.
- 저장: 로컬 캐시 Microsoft.Data.Sqlite 8.0.31/SQLitePCLRaw 2.1.12 사용. CLI_HOME과 별개인 기존 캐시를 NUGET_PACKAGES로 지정해 오프라인 복원 문제 해결. 설치/다운로드 없음. SQLite v1·transaction/FULL/단일 lease, 설정/합성 inventory/배치/증거/판매 목록·삭제 UX, JSON 일회 이관/원본 보존/재이관 방지.
- 도메인: 공유 호가를 예상 합계와 검토에 공통 적용해 기존 계산 불일치를 해결. 최종 직전 현재 inventory/선택 수량/가격·잔량 재검증. 다른 활성 배치 차단, immutable snapshot/확정 결과 역행 거절, 보관 후에도 예약 수량 중복 차단.
- 연결: Mock 요청 증거로 응답 유실/재시작 대조, 수동 fixture→Listed→판매 목록 반영. 개별 선택→취소 검토→선택 전체 Mock 취소→부분 결과 저장. 취소 불명은 Unknown, 취소 완료된 요청 키만 예약 해제. 실제 Steam Guard/네트워크 동작 없음.
- 최종 검증: `scripts/Verify.ps1`, 5개 프로젝트 단일 빌드 경고0/오류0, 도메인/SQLite/MVVM **41/41 PASS**, detached UI **10/10 PASS**. 실제 SQLite 롤백/스키마99/손상/저널 재열기/JSON 이관·삭제/상태 보존 검사 포함.
- 자원/성능: 무거운 빌드·테스트를 병렬 실행하지 않았다. 10,000개 카드에서 실현된 컨테이너 시작9/끝7. 마지막 fixture+복수 렌더 관측259ms는 전후 속도 향상/장기 부하 인증이 아니다. 소프트웨어 렌더링, 전역 시스템 설정 변경 없음.
- 미검증/차단: 실제 입력·모니터 DPI·열린 팝업·스크린리더·장시간/전원 차단·설치·가격 변경·수동 재시도. 실제 Steam 로그인/조회/판매/Guard·타 앱 조사/조작/데스크톱 캡처 없음. 실등록 필수 목표는 LIVE_INTEGRATION_BLOCKED 유지.
- Git: 소스/테스트/누적 문서/패키지 잠금·고지만 커밋한다. 생성 이미지·DB·로그·빌드 바이너리·인증정보는 제외한다. 구현 및 push 결과는 아래 후속 기록에 추가한다.
- 완료 기록: 구현 커밋 `35ffbb4` (`feat: validate detached WPF UI and persist safe mock workflows in SQLite`), 기존 `origin/main`으로 정상 push 성공 (`65e3ef4..35ffbb4`). staged diff 공백 검사 PASS, 금지 API/민감정보 패턴 검색 일치 없음. 허용된 일반 앱의 자체 Show/Focus 코드는 남아 있으나 detached 테스트에서 호출하지 않았음을 실행 경로/Window 미생성 검사로 확인했다. 결과 기록은 별도 문서 커밋으로 누적한다.
