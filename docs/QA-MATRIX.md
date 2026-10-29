# 정상·오류·복구 QA 매트릭스 —0.20.0 (2026-10-29 KST)

0.20 마지막 대량 갱신 보완: 동일 상품10,000개에 SetPrice를 적용할 때 각 행의 Changed가 전체 선택 요약을 다시 계산하던 경로를 기존 Bulk로 묶었다. PriceNotifications 합성 회귀는10,000개 가격 전부 반영·요약 알림10회 미만·거래 전송0을 확인한다. 이는 실행 시간/CPU 개선 비율을 측정한 벤치마크는 아니다. 최종 기준315개 기능/65개 비표시WPF이며 아래 중간312·314 기록과 구분한다.

### 0.20 최종 자동 검증

**기능315/315·비표시WPF65/65·배포계약19/19·안전 정리 fixture9/9 PASS**, Debug/Release 경고·오류0. 기준0.19의305/61과 전 회귀를 유지했다. 신규10기능/4WPF에는 실시간 진행, 취소 대조·취소 전용, 원자 저장 오류 주입, 이전 계획 선택, 로그아웃 늦은 결과, 캐시 선택 보존, 계정 보관 보호와50개 Command Binding 검사가 포함된다. 배포 PE/BAML에서 실제 서비스·진행/작업 선택 타입 존재와 Mock/자가검증/테스트 타입 부재를 검사한다. 자체 RTB의 새 내비게이션·카드/목록·compact 검토·진단 복귀를 직접 열람했다. 개발 구성의 자가검증 탭은 Release에서 제외되며 렌더와 실제 출하 검사를 구분한다.

제한 실행에서 개발 전용 JSON 저널 파일 교체1건은 FAIL이었다. 동일 바이너리 정상 권한312/312 이후, 마지막 선택·보관 가드를 포함한 최종315/315 정상 권한 검사를 확인했다. 실제 GUI/물리 입력/Popup/모니터DPI·장시간 리소스·실서버 수수료/거래는 미검증이다. 실제 창/로그인·인증/조회·판매·취소·Guard 실행0, 타 프로세스/호스트 입력·화면/설정 접근0, 운영 데이터·이전 배포본 삭제0이다. ZIP/SourceCommit/해시는 배포 확정 절에 추가한다.


## 0.20 추가 정상·오류·복구 검증

UsabilityTests: 기존305개 회귀 외에 Progress(두 번째 HTTP 응답 대기 중 첫 행 Guard/다음 Submitting/건수 표시), CancelUiPath(선택 취소 실제 대조·전송0), OlderPlan(이전 미제출 계획 선택), AtomicRelist(SQLite trigger 쓰기 실패 시 계획·배치 모두 rollback, 성공 시 함께 commit), CancellationOnly/Reset(지갑·수수료·보관함 요청0·새/기존 Executor 판매 불가·로그아웃 OFF), ProgressLogout(늦은 UI 결과 폐기·Unknown 영속·Busy 중 OperationId 보존), SelectionEviction(10게임 이동 후 선택/수량 유지·해제), ArchiveOwnership(다른 계정 배치 보관 금지)을 추가했다. NewBookChanged는 사전검증 실패에서 계획이 남고 새로운 검토/승인 후에만 제출됨을 검사한다.

ProductAuditUiTests4개: 제품3뷰 Command Binding50개/ICommand 실객체, 표시 내비게이션과 기존 WorkPage 양방향 매핑/게임 관리 버튼, 진단 복귀 시 동일 뷰·선택 유지, 실제 보관함 카드/목록10,000행 유한 생성·수량 보존. 기존 Hover/Pressed/Focus/TabHeader·이미지 재활용·SQLite/Auth·정수 가격/수수료 회귀를 유지한다. 자체100/125/150/200% RTB와 실제 모니터 DPI·물리 입력·Popup을 구분한다.

제한 실행의 개발 JSON 저널 파일 교체1건은 FAIL로 보존했다. 같은 바이너리 정상 권한312/312 PASS를 확인했으며, 마지막 변경을 포함한 최종 결과는 배포 확정 절에 기록한다. 실제 계정·최상위 창·다른 앱 입력/화면은 미실행이다.


## 0.19.0 추가 — 외부 등록의 새 자산 재판매

SteamMarketHelper의 취소→보관함 재조회→같은 상품의 새 자산 ID 선택 방식을 실제 서비스에 연결했다. 전체 취소 승인 후 서버 접수·목록 부재를 확인하고, 실제 보관함·가용 수량·수수료·공유 호가로 새 대상을 제안한다. 원래 아이템의 반환 확정은 아니며 다른 기존 보유분일 수 있음을 표시한다. 새 실제 자산 ID·수량·가격을 전체 검토해 판매등록 한 번 승인하면 기존 Coordinator가 배치 처리한다. 항목별 추가 판매 클릭은 없다. SQLite 취소 계획/새 배치 연결을 보존하고 불명 취소·원래 예약은 추측으로 해제하지 않는다. 최근 미제출 계획은 사용자 요청 시 재조회하며 자동 거래하지 않는다. 개별 수량1, 최대100등록/10상품만 지원한다.

추가 합성6개/비표시WPF1개: 변경ID·재시작 후 새 승인, 취소만으로 판매0, Unknown 재전송0, 전체 수량 부족·호가 변경 차단, 계획 불변·OperationId/비밀정보 보호. 최종 기능305/305·WPF61/61·배포19/19 PASS, 경고/오류0. 취소/새 대상 검토125%·200% RTB를 직접 검토했다. 실제 계정/수수료·판매/물리 입력은 미검증이다. 상세 호출 경로·보존/재시도 제약은 [판매 실행 계약](SALE-EXECUTION-SPEC.md)의 0.19 추가 절을 따른다.


0.19 최종 자동 검증: **기능305/305·비표시 WPF61/61·배포계약19/19 PASS**, 경고·오류0. 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 제약). 기존286/56 전 회귀 유지. 신규19기능/5WPF에 재등록 Guard→서버 Listed 읽기 대조(추가 전송0), 타 계정 저널 불변 검사를 포함했다. 재등록 재조회는 현재 계정 최근 최대10건이며 다른 의도는 선택 대조한다. RTB usability-loading/combo-alignment/product-empty/bulk-review-small/200을 직접 검토했다. 물리 입력/팝업/모니터 DPI·장시간 안정성/실계정은 미검증. 최종 로컬 ZIP/해시는 배포 확정 절 참조.

0.19 추가 감사: 재등록 후 상태 재조회가 원 배치만 읽던 경로를 보완하여 현재 계정의 최근 재등록 최대10건을 실제 명령/서버와 대조합니다. Guard→Listed 합성 경로를 확인했으며, 타 계정 배치를 복구해 변경하지 않도록 계정 필터와 저널 불변 회귀를 추가했습니다. 과거 기록은 보존합니다.

## 0.19.0 사용자 결함 회귀

신규 기능13개/WPF4개, 기존286/56을 유지한다. 최종 결과는 배포 확정 절에 기록한다. Default/Catalog: 미확인3추천 제거·753명명·이전 카탈로그 보존·수동 재추가 Pinned 재시작. Notifications: 상태/개수 PropertyChanged. Depth/Shortage:10개 공유 잔량3/4/3,11개 선택에서1부족이면 검토/취소0. Average: 원자료 부재 명시·실행 불가. Batch: 기존 실제 어댑터+합성 HTTP로 저널 있는2항목을 최종1승인/이중 클릭 방지→취소2/새 요청2→Guard2. External: 반환 증거 없는 외부 항목은 취소0·NoCausalEvidence/OperationId·민감정보 미노출. Changed: 호가 변경 시 취소0. Unknown/Return: 첫 취소 응답 유실/ID 변경은 Unknown, 나머지 Ready·재시작 전송0. Busy: 작업명/중단 가능·취소 후 비활성 해제.

WPF: 일반 카탈로그/빈 상태 바인딩, Combo 텍스트·벡터 화살표 가운데/FocusRing, 비동기 Busy 아이콘/접근성 ProgressBar/중단, 판매 중 기준·일괄 검토·수수료·100/125/200% 자체 RTB. 기존100/150%/TabHeader 상태·가상화·SQLite/Auth 회귀 유지. 새 실제 창/마우스/팝업·물리 DPI·Steam 인증/거래는 NOT VERIFIED. RTB는 합성/비표시 컨트롤 검증이지 실제 GUI 성공이 아니다.

사용자 ZIP0c9de…는 지정 경로에 없어 서버 오류 분석 NOT VERIFIED. 스크린샷의 기본 게임/정렬/진행 UX 결함만 확인했다. 정확 평균·외부 자동 재등록·변경ID/스택 반환은 지원 완료로 판정하지 않는다.


### 0.18.0 배포 확정 — 수수료 모드/제품 화면

구현 커밋 **e6ee865861adbfaf46d91ae8a6680b2a0a7c2cb7**, origin/main push 완료. 새 로컬 portable ZIP **artifacts/releases/MarketCrate-0.18.0-fee-mode-e6ee865-win-x64.zip**, **66,478,248바이트 /413파일**. SHA256 **552CF3704C5A0B1A6C176351A314B7F2BE5922E09F8ABD8346C6F36B2556F44B**. 원본 `artifacts/releases/fee-mode-e6ee865-windows-x64`, BuildVersion0.18.0+e6ee865…. CLR/WPF10.0.9·SQLite x64·ProtectedData·기존 MIT 고지 포함, 경고·오류0. 전체 ZIP entry SHA와 소스 폴더 일치, 버전/commit/구성/라이선스·사용자 DB/로그/Auth/PDB 제외 검사 PASS. PE/BAML에서 Mock/자가검증 화면·모델 부재와 실제 resolver/Auth/거래 서비스 존재를 확인했다. 실제 EXE/설치·서명/계정·거래는 Codex 미실행이다.

최종 기능 **286/286**, 비표시 WPF **56/56**, 배포 계약 **19/19 PASS**. 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 제약). 사용자 운영 파일/배포본 정리0. 소스 변경 경로/키·JWT literal 검사에서 민감/산출물 경로0·실제 비밀값0을 확인했다. 기록은 ignored runtime/product17-*.txt/자체 RTB에 있고 개인 자료를 Git/배포로 전송하지 않았다.

롤백은 사용자 로그인 복원이 관측된 **0.17.0** ZIP, SHA256 **A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC**이며 이전0.16.1·사용자 진단/저널/Auth/설정도 보존했다. latest/rollback은 ignored artifacts/releases.json에 기록한다. 바이너리는 로컬 생성이며 GitHub Release로 업로드한 것은 아니다.

IMPLEMENTED/SYNTHETIC_VERIFIED는 위 범위이며 REAL_READ_VERIFIED는 사용자0.17 관측에 한정한다. 새 실제 수수료 모드/공식3구간 금액/등록·게시·취소·스택·새 버전 복원/물리 GUI는 NOT VERIFIED. **REAL_FEE_VERIFIED·LIVE_SELL_VERIFIED·RELEASE_READY 미달**, 최초75개47/15/0/4/9 유지. 사용자 최소 재확인은 새 폴더에서 직접 실행→로그인 복원→지갑·수수료1회→공식 화면3구간 대조이며 실패 시 거래/반복 조회 없이 문제 해결 ZIP을 내보낸다.

## 17차 / 0.18.0 (2026-10-29)

| 범위 | 결과 및 증거 |
|---|---|
| 기능 전 회귀 | **286/286 PASS**, runtime/product17-core.txt. 새 FeeModeTests16개, 기존270개 유지 |
| 모드·오류 주입 | legacy API호출0/new HTML→실패·fallback0/필드만으로 모드 추정 금지/잘못된·중복·script/comment 모드/지갑 변경/앱 override0/기본 수수료 충돌/403 중단/계정 불일치/독립 지갑·목록 보존 PASS |
| 정수 금액 | KRW1→3,15→18,1000→1150 및1..999 역산/최소/절사 후 증분 half-up/불가능 구매자17 거부·명시적 하한16/USD15의 수수료1+1 PASS. 모두 합성 서버 메타데이터; 실제 Steam UI 비교값 아님 |
| 첫 판매·거래 보호 | 실제 resolver 계약을 통과한 합성 서버에서 이전 등록0이어도 사전검증(전송0), 충돌/필수 증거 누락 차단. 단일 승인·다중게임·스택 계약OFF·Unknown/재시작·Guard·취소/반환·재가격 기존 회귀 PASS |
| 공식 UI 대조 기능 |3독립 사례×판매자/구매자/Steam/게임4금액,3단가 일치 및 수수료1원 불일치/재입력 무효화, 거래 권한/저널 불변 PASS. **실제 사용자가 공식 화면에서 확인한3사례는 미확보** |
| 비표시 WPF | **56/56 PASS**, runtime/product17-ui.txt. 제품 비교 화면12입력 바인딩·125/200% RTB, 기존100/150%/TabHeader/가상화/선택/로그인 복원 포함. Window/PresentationSource0; 물리 입력·Popup·모니터 DPI 대체 아님 |
| 배포·정리 | 배포계약19/19 PASS; 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 제약). 운영 파일 정리0 |
| 실환경 사용자 관측 |0.17 재시작 로그인 복원·QR/Community·목록8·CS2 명시적0·KRW wallet/legacy5필드 확인. fee200 HtmlMarket은 실패, 모드/수수료/가격/실판매는 미검증 |
| 새 EXE/설치/서명/실계정 | Codex 미실행. 새 버전 실제 복원/모드/수수료/등록·취소·장기 누수는 NOT VERIFIED |

초기9실패:5개 fixture가 새 sell-context GET을 정의하지 않아 실패했고,4개 가격 이력 fixture는 Oct28 고정 날짜가24시간 창 밖으로 만료되어 실패했다. 실제 계약 shape를 추가하고 이력을 현재 시각 기준으로 만들어 해결했으며 가격 신선도 기준은 낮추지 않았다. 중간 WPF1실패는 삭제한 상시 '계약 요약' 표시를 요구하던 구 UI assertion을 정상화하고 '일반 화면 내부 코드 부재' 회귀로 대체했다. 최종 FAIL0.

RTB product-fees-125/200, live-only-shell-small, 세션/기존 탭 상태 출력을 직접 검토했다. 개발 렌더에는 테스트용 탭이 보일 수 있으며 정식 PE/BAML의 자가검증 타입/리소스 부재 검사는 별도로 수행한다. 라이선스·전체 ZIP 해시 결과는 배포 확정 기록 참조.


### 0.17.0 최종 자동 검증

기능 **270/270 PASS**, 자체 비표시 WPF **55/55 PASS**, 배포 파일계약 **19/19 PASS**, 안전 fixture 정리 **8 PASS/0 FAIL/1 NOT VERIFIED**(실제 junction 생성 환경제약). 최종 빌드 경고·오류0. 신규 세션20/자체UI3개와 기존 전 회귀 포함. 직접 Windows CurrentUser DPAPI·ACL/암호문만 저장, 로그·검토ZIP canary 미노출, 인증 복원 시 Unknown 저널/예약 보존·거래0을 확인했다. 마지막 저장 예외 분류/계정 검증을 반영한 최종 source 증거는 runtime/session-build.txt·session-core.txt·session-ui.txt·session-distribution.txt·session-cleanup.txt에 있다.

RTB session-optin/session-restored-small/large/session-paused를 직접 확인했다. 실제 Windows 사용자 간 복호화·실제 GUI/물리입력·전원 차단·Steam 저장 토큰 재사용/회전·수수료·판매/취소는 NOT VERIFIED. NuGet 취약성 메타데이터를 끄거나 SDK/Windows 설정을 설치/변경하지 않았다. 최초75개 판정47/15/0/4/9 유지. 기존 운영 데이터/진단/릴리즈 삭제0. Release·ZIP 해시와 커밋은 확정 기록에 추가한다.

## 16차 / 0.17.0 인증 유지 QA

| 검사 | 자동 검증 범위 |
|---|---|
| 최초 QR·유지 ON/OFF·닫기/재시작 | 합성 QR/HTTP로 DPAPI 저장·새 VM의 단일 복원, 실제 GUI/계정 미검증 |
| DPAPI/파일 보안 | 실제 CurrentUser Protect/Unprotect·사용자 SID만 허용된 ACL·평문 토큰/계정 미포함. 다른 Windows 사용자의 실제 복호화 시도는 NOT VERIFIED |
| 계정/토큰/응답 | web/derive·sub/exp/nbf·계정/쿠키 일치. result 생략 cookie 계약도 검증. 잘못된 소유자 envelope/버전·만료·거절·누락 쿠키는 연결 불가 |
| 실패/중단/재시작 | pending/손상/네트워크/취소/쓰기 잠금은 차단 표식 보존, 반복 자동 요청0. 잠금 교체 실패 후 이전 암호문 보존. 실제 강제전원차단은 NOT VERIFIED |
| 교체/삭제 | 새 QR 토큰 원자 저장·다른 인스턴스 삭제 후 재생성 차단. 서버 측 이전 토큰 무효화/회전·철회는 미검증/미지원 |
| 로그아웃/로컬 삭제 | 유지OFF/명시적 로그아웃은 파일 삭제, 현재 메모리만 유지하는 삭제 동작 구분, 저장 실패는 OFF·정상 인증과 분리 |
| 거래/저널 | 자동판매/목록·보관함 갱신0, 인증POST만2개, 실거래 기본OFF. Unknown intent/예약 보존·재전송0 |
| 진단/배포 | OperationId/HTTP/저장 이벤트 연결, JSONL/검토ZIP 토큰·쿠키·SID·계정 미노출. Auth폴더/authbin/authlock/pending/blocked 배포 차단 |
| WPF | 체크박스/삭제 명령 바인딩·기본OFF·복원 완료/중단 RTB. 물리입력·실제 창/Popup/모니터DPI는 NOT VERIFIED |

신규 SessionPersistenceTests20개·SessionPersistenceUiTests3개. 기존 전체 회귀를 유지한다. MIT 소스 검토와 합성 HTTP 성공은 실제 Steam 서버 복원/수수료/거래의 LIVE_VERIFIED가 아니다. 최종 집계/Release 결과는 현재상태·작업기록에 누적한다.

### 0.16.1 최종 자동 검증

기능 **250/250 PASS**, 자체 비표시WPF **52/52 PASS**, 배포 파일계약 **14/14 PASS**, 최종FAIL0·빌드 경고/오류0. 안전정리7 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 환경제약), 이번 사용자 배포/데이터 정리0. 신규 수수료/복구 오류 주입·JSONL 원인 연결/비밀 canary 미노출 및 기존 거래 어댑터·가격·SQLite/Unknown 회귀 포함. runtime/fee-final-core.txt·fee-final-ui.txt·fee-final-build.txt·fee-distribution-contracts.txt·fee-cleanup-contracts.txt에 로컬 증거를 남겼다. RTB fee-response-error-row/fee-comparison-small/large·오류 화면 직접검토, 물리GUI/모니터DPI/설치·실계정 수수료/거래·서명 미실행. 최초75개 판정47/15/0/4/9 유지. 중간1회 기존 복구 검사 실패/미확정 원인과 결정적 게이트 보강은 WORKLOG에 보존했다. Release 정적 검사와 실제 실행 검증을 혼동하지 않는다.

## 0.16.1 — Fees HTML 오류 주입 및 응답 분류

| 경로 | 검증 기준 및 결과 |
|---|---|
| 인증→목록8→보관함0→Wallet성공→Fees HTML | 합성 관측 재현, 세션/빈 응답 사실 보존, fee Schema 실패·판매POST0 |
| 장터 표식 HTML | Challenge 아님·endpoint 계약 차단·독립 목록 사용 가능·fee 재전송0 |
| 분류불가 HTML | Schema/Blocked·후속 미전송·로그인 만료로 변경하지 않음 |
| 로그인 폼 / 보안 위젯 / Access Denied | 각각 Authorization / Challenge / Forbidden, 세션/안전중단 규칙 확인 |
| JSON의 명시적 captcha_needed/requires_captcha | 유효해 보이는 수수료 필드가 함께 있어도 안전중단·후속 요청0 |
| HTTP401/403/429 | 기존 중단 유지, 후속 요청/등록0 |
| JSON/HTML Content-Type 교차·깨진JSON·누락/추가fee | media/shape/parser 별도 진단, 유효 JSON은 endpoint 검증, 잘못된 계약은 성공으로 바꾸지 않음 |
| script/comment/불완전 script의 CAPTCHA 문자열 | 위젯으로 오인하지 않음, unknown 보수중단 |
| legacy wallet필드 | 완전/부분/잘못된 필드 관측, KRW minimum/increment 생성0·구형 fee 자동대체0 |
| KRW/USD 공식 단가 비교 | 일치/불일치/잘못된 입력·로그아웃/새조회 무효화, HTTP/판매POST/권한변경0 |
| JSONL 원인 연결·보안 | 3005→3002→4204 OperationId/CorrelationId 연결, 원문HTML·쿠키·계정 canary 미노출 |

신규 FeeResponseTests14개 + FeeResponseUiTests2개, 기존 개발용 복구 IOException 게이트1개 추가. 최초 기존 재시작 검사1회 실패는 미재현/원인 미확정으로 WORKLOG에 보존했고 복구 오류 시 새 검토가 열리는 별도 경로를 결정적 주입으로 보강했다. 비표시 STA/RTB만 사용하며 실제 창/포인터/키보드/Popup/모니터DPI/계정 인증·거래는 NOT VERIFIED. 게스트/앱 실행0. 전체 결과는 이번 배포 확정 기록 참조. 실제 서버 Fees JSON 및 KRW wire/요율 적용 모드 증거가 없어 실계정 검증으로 승격하지 않는다.

## 0.16.0 실제 전용 검증

| 항목 | 결과 |
|---|---|
| root→자격확인→서버 쿠키 수신→고정 Market 복귀→실제 응답 파서 | 합성 HTTP PASS, 통화만 있으면 가격 단위 생성하지 않음 |
| goto 외부/중첩/이중 인코딩/임의 query/fragment | PASS, 무전송 또는 첫 이동에서 중단 |
| 자격 확인401/403/429 | PASS, 반환 요청 없음·기존 전역 제한 유지 |
| 자격 확인200 미완료 | PASS, Wallet 권한 미확인·재전송 금지·독립 목록 유지 |
| 순환/알 수 없는 Market 경로 | PASS, 제한된 요청 수 후 중단 |
| 실제 전용 Shell·진단 명령·빈 서버 데이터·설정 복원 | 자체 WPF PASS, Demo/운영 저널 부작용 없음 |
| 전체 기능/자체 WPF/배포 계약 | 235/235 ·50/50 ·14/14 PASS |
| Release 구현/BAML 검사 | Mock 공급자/저장소/실행/구 UI·재시도 부재, 실제 서비스 존재 |
| 개발 DLL을 Release로 검사 | 예상 fixture 코드 누출 사유로 거부 |
| 자격 확인의 실제 사용자 원문 경로·지갑 성공 | UNVERIFIED |
| 실제 GUI/계정/거래·설치·서명 | Codex 미실행 |

증거는 ignored runtime/live-only-verify.txt·live-only-release-*-build.txt·live-only-negative-audit.txt·ui-tests/live-only-*.png. 기존 회귀를 삭제하거나 성공 기준을 낮추지 않았다. 실제 데스크톱/다른 프로세스는 조사하지 않았다.

## 0.15.1 Wallet302 패치

검증: 기능227/227·비표시WPF48/48·배포계약14/14 PASS, FAIL0·경고오류0. 정리검사7 PASS/1 NOT VERIFIED(실제junction 환경제약) 유지. runtime/wallet-patch-verify.txt 및 최종core 재검사 증거. 실제Steam지갑 성공은 미검증이며 Codex 실계정/앱/거래 실행0. 최초75개 판정과 거래보호 변경없음.

사용자증거:0.14/0.15 세션기록·목록8·CS2자산0·Wallet302/OtherRedirect, 실제Location 없음. 실계정원인 UNVERIFIED. 기존로그가목적지까지진단하지못한한계를기록한다.

WalletRedirectPatchTests4개는 /market 단일이동 성공, 합성8/0/302 후세션/독립조회보존·이동실패문구, 동일작업ID의정제분류·URL/계정/secret canary미노출, /market↔/market/루프2요청중단을검증한다. 실제Steam계정·지갑실행검증이아니다. 전체최종집계는배포확정기록을따른다.

## 15차 — 실제 어댑터 경로 검증

### 15차 최종 자동 검증

기능 **223/223 PASS**, 자체비표시WPF **48/48 PASS**, 배포파일계약 **14/14 PASS**, FAIL0, Debug 경고/오류0. 안전정리 **7 PASS/0 FAIL/1 NOT VERIFIED**(실제junction 생성불가, 기존경계/잠금/변조검사유지). 신규16기능/2WPF는 실제어댑터의 합성HTTP 주입이며 사용자모드의 조건부활성화→Guard대기→수동게시대조→취소반환→재가격·감사저널도 검증했다. runtime/stage15-release-verify.txt가 최종전체회귀 증거이고 runtime/stage15-focused-final.txt는 추가user-mode saga 검사 증거다. RTB stage15-trading-controls.png·stage15-multi-game-review.png 직접검토. Codex 실계정/거래·실제창/물리입력/Popup/DPI/설치·서명 미실행. 최초75개판정47/15/0/4/9 유지. 실제 Wallet302/등록취소/KRW/스택 계약·LIVE_VERIFIED는 미확정.

FifteenthStageTests: canonical/locale단일redirect·unsafe/임의query/loop/401, 계정불일치·가격metadata/기존fee증거부족 활성화차단, 동의철회/로그아웃binding해제, 호가잔량지문변경시 첫intent전차단, 다중App/context 단일배치/중복승인, 미검증App전체차단, 취소전체사전대조, 기존listingID 재사용Unknown, KRW10개3/4/3 공유잔량, stale재가격 취소전거절, 개발용/사용자모드 분리. 모두 합성HTTP이며 서버 계약 충분성/LIVE_VERIFIED로 확대하지 않는다.

FifteenthStageUiTests: 초기OFF/입력확인/실제명령 바인딩·개발도구분리, 게임전환 선택보존→전체가격→두게임검토→한번승인으로2요청. RTB 전용, Window/HwndSource/전역입력 없음. 렌더에서 빈 확인입력의 폭·안내 누락을 발견해320px/입력문구로 수정했다.

초기 통합11FAIL은 새인증이 기존거래동의를 해제하는 올바른경계에 맞지 않은 구합성setup(로그인전동의)과 재가격 Game표시/provider 비교 변경으로 발생했다. setup은 인증후 합성승인으로 갱신하고 scope와Game에 맞는provider를 선택해 수정했다. 검증기준/성공assertion을 낮추지 않았다. 최종집계는 확정결과에서 확인한다.

## 14차 결과 (2026-10-29)

|경로|결과|
|---|---|
|인증기록·목록8·inventory0·지갑401/로그인redirect·후속차단|합성 PASS; 사용자 실제실패응답 미확보|
|GET API15/8·공식비로그인redirect·세션보존/독립조회/재전송방지|PASS FourteenthStageTests|
|wallet null/부재/통화만/추가fee필드|PASS; 숫자default 없음·strict거래계약 유지|
|명시빈페이지/모순/필드누락/타입/범위·marketable0|PASS; 오류를 빈성공으로 변환하지 않음|
|4205/4206 고정사실·분류·민감정보 제외|PASS 합성 로그 assertions|
|전체 기능/자체WPF/배포파일계약|207/207 ·46/46 ·14/14 PASS; FAIL0|
|안전정리 경계/잠금/변조/DB보존/dry-run/삭제|7 PASS/0 FAIL/1 NOT VERIFIED(실제junction 생성불가)|
|RTB 요약/행상세|자체이미지 직접검토; 실제창/입력/DPI 대체 아님|
|사용자wallet실패/CS2 0건 근본원인|UNVERIFIED; 정제ZIP 증거 필요|
|실계정·실거래·실제GUI·설치|Codex 미실행; IntegrationBlocked|

ignored 증거 runtime/stage14-final-verify.txt, runtime/stage14-ui-final.txt, runtime/ui-tests/stage14-*.png. 기존199/45/14 회귀 유지. 최초75개47/15/0/4/9 판정 유지.

## 13차 읽기 전용 자가검증 (2026-10-29 KST)

13차 배포: 구현 `dd7e27635104c02fe53c0e282242e9d598c04a4b` origin/main push. **0.13.0 x64 self-contained Release/정적 파일검사 PASS**, 경고·오류0, CLR/WPF10.0.9·SQLite 네이티브·라이선스 포함. `artifacts/MarketCrate-0.13.0-stage13-dd7e276-win-x64.zip` **78.31MiB/412파일**, ZIP 전체 원본 hash 및 ProductVersion/SourceCommit 일치. SHA256 `2B76B0ACB3440CCBA5F2C2364AAD46BA458D10E89E329A8456400A0ED83BD7C8`. DB·로그·인증·PDB/개발파일 제외, 이전 ZIP·사용자 데이터 보존. 실제 EXE/서버 로그인·읽기·거래·설치·서명 미실행. 최소 사용자 절차는 READ-ONLY-VALIDATION의0.13.0절차를 따른다.

기능 **199/199**, 비표시 WPF **45/45**, 배포 파일계약 **14/14 PASS**, FAIL0/Debug 경고0·오류0. 기존187/42/14 회귀 유지. 실제 인증/서버/GUI/설치 검증이 아니다.

| 새 검사 | 단언한 증거 |
|---|---|
| T13.Consent/Scope | 승인 없이 직접 호출 요청0, 범위/게임 변경 시 승인 해제, listings-only GET1·완전 목록 캐시 불변·승인1회 소비·거래0 |
| T13.All/Page | 허용된 Community GET6+무인증 이미지1, 모든 step 성공은 합성임을 표시. inventory Next가 있어도 추가 페이지 요청0, 기존 row/선택 동일 |
| T13.Restrictions |401/403/429 후 실제 읽기1회/나머지Blocked, 세션/HTTP latch 구분·인증만료만 로그아웃, 우회0 |
| T13.Independent/WalletFailure | malformed/503은 독립 실패·다른 허용된 조회 진행, g_rgWalletInfo 없는 HTML은Schema·로그인 유지, wallet 미확인이면 fees 요청0 |
| T13.Missing/Cancel | 빈 보관함Count0·품목 없음 미검증/호가·이미지 무요청, 취소 뒤 후속요청0, 로그아웃 중 늦은 결과 폐기 |
| T13.Diagnostics/Cookie/Mismatch |4204·HTTP Correlation·row OperationId 동일, raw/synthetic secret/name/SteamID sentinel 누출0. Community cookie 없음/빈 값/비Secure 거부. 지갑·호가 통화 및 publisher fee 불일치가 특정 Error로 실패, 거래0 |
| U13 3종 | 범위OFF·진입/승인 바인딩·버튼 비활성; 실제 어댑터 합성 성공/실행 중 입력잠금·취소/403차단/OperationId 자체 렌더; VM 명령→페이지와 Selector→VM 양방향 탐색 |

초기 T13 helper가 수수료를 지갑보다 먼저 설정하는 체인 대입 때문에 3개 FAIL: UI는 지갑 선행 조건을 강제하므로 helper를 같은 순서로 고치고 검증 단언을 유지했다. UI 회귀3개는 초기 바인딩 전에 SelectedIndex를 직접 덮던 테스트 설정과 WorkPage 초기 전송의 충돌이었다. 기존 검색/이미지/계약 요약 단언을 삭제하지 않고 VM 탐색 상태로 진입시키며 별도 양방향 Selector 검사를 추가했다. 실제 물리 입력을 재현했다고 판정하지 않는다.

자가검증 성공/차단/실행 중 PNG를 1100×800/1380×900·1/1.25 출력 밀도로 직접 검토. 실제 Window/Popup/화면캡처/모니터 DPI/키보드·마우스 입력은 실행하지 않았다. 실제 Steam 응답이 없으므로 서버 파서 호환성 PASS·READ_ONLY_READY·LIVE_READY 판정은 없다.

## 12차 — 실제 사용자 발견 결함 재현과 수정

12차 배포: 구현 `49a3ffad9a8b37aa4085d6ace6191ddbd035430e` origin/main push. **0.12.0 Windows x64 self-contained Release PASS**, 경고·오류0, CLR/WPF10.0.9·SQLite 네이티브/고지 포함, DB·로그·세션·PDB·개발파일 제외. `artifacts/MarketCrate-0.12.0-stage12-49a3ffa-win-x64.zip` **78.30MiB/412파일**, ZIP 전체 파일 SHA256가 원본과 일치. SHA256 `EAE969B83190F2EB9A29F7299AD2B879DC7D699502D631E960A6F10308986408`. ProductVersion/SourceCommit 일치. 앱/인증/실거래/설치·서명은 실행하지 않았고 기존 배포본·사용자 데이터는 보존했다.

사용자 최소 확인:① 보관함/판매/진단 탭의 콘텐츠 버튼·체크·입력·드롭다운 위를 이동해 Header 깜빡임이 없는지, Tab/Shift+Tab 점선과 선택 밑줄이 구분되는지 확인.② 직접 QR 인증을 선택한 경우 인증 완료 카드와 개별 조회 상태를 확인.③ 보관함 파싱/404 실패 시 인증 완료가 유지되고 독립 목록/fee 버튼이 동작하는지 확인(401이면 재로그인,403/429/CAPTCHA면 차단이 정상).④ 게임 전환·선택/수량·스크롤·가격 검토를 확인하되 실등록·취소·재가격은 실행하지 않는다. 실패가 남으면 고급 설정의 작업 ID→진단 검토 ZIP으로 원인을 좁히고 QR/원문 응답은 공유하지 않는다.

12차 최종 회귀: **기능187/187 PASS · 비표시 WPF42/42 PASS · 배포 파일 계약14/14 PASS · FAIL0 · Debug 경고0/오류0** (`scripts/Verify.ps1`, ignored runtime/stage12-verified.txt). 기존177/39/14 검사와 거래/Unknown/SQLite 회귀 유지. 사용자 제보의 실제 Steam 보관함 실패 응답은 미확보이므로 해당 실서버 파서 계약이 해결됐다고 보장하지 않는다. 수정한 것은 실패의 범위·인증 소실·UI 동작이며 실계정 파서/응답 형식 검증은 별도다. 실제 앱/입력/Popup/물리 DPI/Steam 인증·거래·설치 미실행.

| 경로 | 증거/결과 | 한계 |
|---|---|---|
| 수정 전 인증 소실 | T12 초기 5개 중 파서3종/503 총4 FAIL(178/182 PASS), 404 독립 경로는 기존 PASS | 사용자 실응답을 재현한 것이 아니라 합성 최소 재현 |
| 탭 Header | U12 초기 Header 외부 TabItem.IsMouseOver가 선택 Header 배경을 바꿔 FAIL; SourceName=B로 변경 후 PASS | 물리 포인터/캡처·모니터 미실행 |
| 인증·읽기 분리 | TwelfthStageTests.ReadFailure: inventory/fee/listings parse/404/503 후 인증 보존, 독립 조회 버튼 활성과 성공, 실등록0 | HTTP handler 대체·실서버 아님 |
| 서버 제한 | Restrictions:401 세션 만료/재로그인,403/429 세션 존재와 요청 차단 분리, 직접 메서드 재호출에도 요청1회 | 실제 제한 유발·우회 없음 |
| 부분 성공/재시도 | PartialLoad: inventory Ready + fee Failed + listings Ready, 수동 fee 재조회 복구, 게임 선택·수량 보존 | 자동 네트워크 재시도 없음 |
| 네트워크/이력 | ReadNetwork GET timeout/transport 요청1회·독립 목록 성공. HistoryFailure parse/network를 History 실패로 연결하고 오래된 검토 차단 | 실제 네트워크 설정 변경 없음 |
| 로그 | Diagnostics: 같은 OperationId의 ReadState4203→HTTP Correlation→ResponseParsed/Parsing, AuthState ContractFailure로 잘못 전이 없음, 원문 sentinel 부재 | 원문 응답/비밀 덤프 없음 |
| UI | U12 Header/하위4종 Hover·Selected·포커스 구분,3열 배치/고급필드 이동/실제 명령 연결/100개 표시/게임·페이지 스크롤 및 선택 보존/모달 배경 차단 | 실제 Window/Popup/입력 없음 |
| 자체 렌더 | runtime/ui-tests/stage12-*.png: account/inventory/listings/advanced/review.1100×700,1380×860,1600×900 및1/1.25/1.5/2 출력 밀도 직접 검토 | 출력 밀도는 물리 모니터 DPI 인증이 아님 |

회귀 중 기존 DiagnosticUi가 exports 폴더 생성·명령 busy만 보고 ZIP 완성 전에 단언하는 경쟁을 확인했다. 저장 완료 Status와 실제 ZIP 존재를 함께 기다리도록 수정했고 합격 조건(검토 동의/필터/1개 ZIP)은 유지했다. 기존 UI 검사의 DockPanel 강제 cast는 FrameworkElement로 바꾸고, 고급 기능 이동은 새 고급 페이지에서 같은 명령/정제 요약을 검증한다. 탭 Hover 검사는 새 소유자인 Header B에 주입하고 콘텐츠 Hover 불변 검사를 별도로 추가했다.

## 11차 — 사용자 결함 재현 및 회귀 (2026-10-28 KST)

11차 배포: 구현 커밋 `0705dc2874b64b521e86dd92b3847c1a1b63219c` origin/main push 완료. **0.11.0** self-contained Windows x64 Release 빌드·정적 검사 PASS, 출력 경고/오류0. CLR/WPF10.0.9, x64 apphost/SQLite 네이티브 DLL/라이선스 고지 포함, DB·로그·인증·PDB·개발파일 제외. `artifacts/MarketCrate-0.11.0-stage11-0705dc2-win-x64.zip` **78.29MiB/412파일**, ZIP 전 항목과 원본 SHA256 일치. SHA256 `C3A5AEC49ACB4B8FEA93D4496E49E4D7AB92F2FB7F409F5EA22F1E43A60AEE28`. distribution.json 및 ProductVersion=0.11.0+0705dc2…의 SourceCommit 확인. 실제 EXE/설치/서명/실서버 QR 발급은 미실행. 이전 배포본·사용자별 저장소를 덮어쓰거나 초기화하지 않았다.

| 경로 | 수정 전 증거 → 수정 및 합성/비표시 검증 |
|---|---|
| QR 요청 | 독립 decoder 정상계약 테스트 FAIL(최상위1 누락); API 메타데이터에서 최상위1/2 required 확인. EleventhStageTests.RequestContract: field번호/wiretype/상위·내부 구분/WebBrowser2/base64 form/format·Accept/인증완료 안 함 |
| QR JSON | 숫자 uint64 client_id 합성 응답 FAIL → NumericResponse 정확한 ulong 파싱/0.5초 interval PASS. float 변환으로 정밀도 손실 안 함 |
| 실패 단계/민감정보 | ResponseFailures/ApiFailures: malformed JSON, 누락, 잘못된 base64, uint64 overflow, 잘못된 host, interval0, 200+x-eresult8/401/403/429. 각 단계·원인·OperationId·세션정리·요청1회·secret sentinel 부재 검증 |
| 취소/갱신/만료 | Cancellation: 사전취소 전송0/로그아웃 QR 제거. 기존 T10.QrRotation 및 5차 로그인 만료·거절·로그아웃·늦은 응답 회귀 유지 |
| UI 상태 소유 | 이전 ActionTemplate VSM/Trigger 중복 검사 FAIL, DP 왕복 검사는 PASS. 실제 물리 ghost-hover 재현 아님. VSM 단일 소유+별도 FocusRing으로 수정 |
| 상태 전이 | U11: Button/Toggle/Repeat VSM Normal/Hover/Pressed/Focus/Disabled 왕복; Check/Radio DP Checked/Hover/Pressed/Focus/Disabled; Tab/List/ComboItem/Menu 선택·focus·hover 재활용; Text/Search/Combo/Stepper focus/modality; Thumb hover/drag/disabled. 각각8회, Background/BorderBrush/BorderThickness/Opacity/현재 VSM·ring·mark 확인 |
| 자체 렌더 | runtime/ui-tests/control-states.png 및 stage11-*.png. RenderTargetBitmap만 생성, 실제 Window/Popup/PresentationSource 없음. 실제 입력·Popup·물리 모니터 DPI NOT VERIFIED |

초기 재현: 기능172/174 PASS·2FAIL, UI34PASS·1FAIL(구조 중복). 테스트 개발 중 detached CLR 상태 getter와 DP 상태 주입의 차이로 버튼 테스트가 실패했다. 이를 실제 입력으로 해석하지 않고 공개 VSM API 전이를 별도 구동하며 Trigger는 DP로 검사하도록 분리했다. 합격 기준의 시각 속성/복원 단언은 유지했다.

## 10차 검증 및 사용자 증거

사용자 직접 보고의 9차 기본7항목: **7 PASS/0 FAIL**. 실제 Windows에서 실행/hover·click/보관함선택·수량/가격전환·검토/Mock배치/취소·재가격·진단/재시작 보존을 묶은7개다. 개별 테스트시각·빌드hash·OS·DPI 미제공. 상세 GUI-01~24 전항목 PASS로 변환하지 않으며, 10차 코드의 물리 GUI·읽기연동 결과로도 사용하지 않는다.

| 10차 결함/경로 | 수정과 검증 |
|---|---|
| 한국어 이름이 전략 로직/저장값 | PriceDisplay alias 경계/PriceBasis 내부판단/안정 enum key. T10.Terms 저장·재시작·수동전략/단가 차이 검증 |
| QR 회전 후 옛 matrix | auth Challenge→UpdateChallenge→새 QrMatrix. T10.QrRotation 기본빈값/회전/만료/재로그인, 연결중 Begin 비활성 |
| inventory/fee 실패가 listings를 막음 | 독립 LoadListings/LoadFees와 fee없는 취소검토. T10.IndependentListings inventory404 후 정상 목록/차이·Submit차단/거래0 |
| 인증만료 후 이전 데이터 | T10.ExpiredReads401→Expired/이전row보존/새검토불가/logout정리. 서버자료를Mock으로 대체 안 함 |
| 계약 진단 | T10.ReadDiagnostics UI operation→HTTP correlation→ReadContract endpoint/count·Synthetic; 비밀sentinel/계정·hash원문누출0 |
| 공식 대체 버튼 | T10.OfficialPage 고정URL delegate/연결0/실행0/쓰기0. 실제 브라우저 미실행 |
| 사용자 명칭/진입/검토 | U10.terms/connected: 가격명·툴팁·독립명령·합성계약배너·검토명/중복상태숨김·자체PNG |

최종 기능172/172·detached WPF33/33·파일계약14/14 PASS, FAIL0/Debug 경고0·오류0. 구현 `e5db266` origin/main push 완료. 해당 소스 self-contained x64 Release PASS(경고0/오류0), CLR/WPF10.0.9·SQLite/고지/데이터·PDB 제외/Core commit 확인. `artifacts/MarketCrate-stage10-e5db266-win-x64.zip` 78.3MiB/412개 원본·ZIP hash 일치, 앱 미실행. SHA256 E14B0BA88D314276F485AEB2D8E97A64604B02F7EA7D5A77207C4DC122EC765F. 기존166개 기능/31개 UI/14개 파일 검사를 유지한다. 새 U10 첫 검사에서 같은 전략ComboBox가2곳인 정상구조를 Single로 가정해 FAIL1; 두 컨트롤 모두 같은명/선택/설명을 단언하도록 고쳤다. 기본 cache 경로 미지정 build의 NU1101은 기존 Verify 환경변수로 해소했고 패키지/SDK 설치·우회 없음.

## 9차 결과 (2026-10-28 KST)

**독립 guest 없음 / 실제 GUI 실행 없음. GUI-01~24: PASS0, FAIL0, NOT VERIFIED24.** 호스트 대체 GUI/입력/캡처/VM 실행 없음. 75개 판정47/15/0/4/9 유지.

| 경로 | 증거/결과 | 한계 |
|---|---|---|
| 기존 회귀+ZIP 결함 | 기능166/166 PASS(기존165+재현1), Utc 순서·operation ID·Validation 원인·로컬2-entry ZIP/무DB | 합성 로그/격리 저장소만 |
| detached WPF 기존31 | 31/31 PASS, 예상 밖 binding 오류0/최상위 Window·PresentationSource0 | 열린 Popup/실제 focus/물리 DPI 대체 아님 |
| 배포 파일 오류 주입 | 14/14 PASS: x86 apphost/WPF/SQLite, FDD위장, 고지 누락, DB/sidecar/authJSON/JSONL/.env/key/PDB 차단 | fake PE HEADER/파일 검사, 실행 호환성 아님 |
| self-contained | 구현 `d19dcc9` origin/main push 완료. 해당 소스의 SCD/FDD Release 모두 PASS(경고0/오류0), x64 apphost/CLR·WPF·SQLite(모드별)·runtimeconfig·고지·DB/로그 제외 정적 확인. SCD CLR/WPF10.0.9·PDB0, guest bundle414개 해시/142.02MiB; FDD40개 해시/33.51MiB. 두 manifest SourceCommit/PackagingCommit/Core metadata 일치, GUI0/0/24. 앱/guest/설치·서명 미실행 | 실제 실행/설치/서명 미검증 |
| guest 하네스 | SCD분기/동일build 재시작/사용자 결과경로 코드·AST 검사 | 실제 guest launcher 미실행; 모든 case NV |
| OS 가능 조건 | registry Core/build26200.9711/X64, Sandbox exe 없음; 공식Home 미지원 | CPU 가상화/SLAT/CIM은 권한으로 미확인. 호스트 변경 없음 |

실패→수정 증거: `runtime/stage9-before-fix.txt` 신규 ZIP 검사 FAIL1/기존165 PASS → `stage9-final-verify.txt`166/166. 최초 self-publish는 offline NuGet source로 NU1101 → self 명령에만 공식 source 추가. 복원 후 publish의 runtime license 누락을 정적 FAIL로 검출 → 원본 runtime pack license3개 복사. 파일 검사 .env case의 최초 FAIL1은 PowerShell 동적 변수 scope로 fixture 파일명이 바뀐 테스트 결함이었고 변수 분리 후14/14. 실패를 실행 환경 성공으로 축소하지 않는다.

## 8차 검증 범위 (2026-10-28 KST)

**실제 격리 게스트 미확보 / 실제 GUI 미실행.** 호스트 Window/Popup/입력으로 대체하지 않는다. 자동 결과는 합성 HTTP·자체 SQLite·detached STA만 뜻한다. 전체 기능/합성 HTTP/SQLite/진단 165/165 PASS, 비표시 WPF 31/31 PASS, FAIL0, Debug 경고0/오류0. 구현 `f672120` origin/main push 완료. 해당 커밋 Release publish PASS(경고0/오류0), apphost/SQLite x64 PE·Desktop Runtime10 의존성·데이터 제외·라이선스 고지·Core Git metadata 확인. 격리 QA package39개 SHA256 전부 일치, 24개 결과는 NOT VERIFIED. 실제 앱/guest/설치·서명 미실행.

| 경로 | 단언/증거 | 범위 |
|---|---|---|
| 게스트 profile/오류 args | 게스트 확인 누락·다른 args 거부, 운영 데이터와 별도 경로 | E8.Profile, 실제 프로세스 launch 아님 |
| offline Steam 경로 | UI CanExecute=false, 직접 BeginAsync도 factory 호출0/연결0 | E8.Offline/U8.offline |
| QR 선택 기본값/갱신 | 새ID0/빈URL 유지, 새ID124·새URL 반영 후 다음 protobuf ID124 | E8.QrDefaults, 공개 proto 기반 합성 |
| QR URL/추가필드/취소 | session 정리/스키마 차단, 원문누출0, 작업ID+AuthPoll+Schema; 취소를 Schema로 오인하지 않음 | E8.QrInvalid/QrCancel |
| 보관함 취소 | Connected 유지/Busy 해제/HttpFailure.Cancelled+동일 OperationId/거래0 | E8.ReadCancel |
| 모바일 승인 flags | bool/0·1 해석, 문자열/null/다른 숫자·객체 거부 | E8.Confirmation |
| Steam 검토 열고 닫기 | 배경 실제 IsEnabled false→true, scope/Tab/CtrlTab/자체 렌더·동일 operation Modal logs | U8.modal, focus 실제 이동 아님 |
| ComboBox/stepper 접근성 | 커스텀 trigger 존재/managed Name·Role, disabled RangeValue setter 거부/수량 불변 | U8.peers, OS UIA/스크린리더 아님 |
| x64/런타임/게스트 패키지 | apphost/nativeSQLite PE machine/Runtime dependency/manifest hash/데이터 제외/24 NV 시나리오 | 정적 검사, 실행·설치 아님 |

초기 신규 detached ComboBox 검사는 standalone fixture에 Style 미적용으로 FAIL1. 기존 앱 경로의 바인딩 문제와 구분해 fixture에 커스텀 Style을 명시하고 관련 UI 검사를 재실행했다. 최종 결과는 위 마감 기록에 반영한다. 기존158개와 UI28개를 삭제/완화하지 않았다. 장시간/native memory/자원 누수·실제 Popup/물리 DPI는 이번 단기 검사로 PASS 처리하지 않는다.

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
11차 최종 회귀: **기능177/177 PASS · 비표시 WPF39/39 PASS · 배포 파일 계약14/14 PASS · FAIL0 · Debug 경고0/오류0**. 기존172/33/14 전부 유지. `scripts/Verify.ps1`, ignored `runtime/stage11-verify.txt` 근거. 실제 서버/물리입력/Popup/설치·서명 미실행.

사용자 최소 재확인: (1) 새 ZIP 버전·커밋 확인 후 Steam 탭에서 명시적 QR 시작으로 QR 표시 여부만 확인(실거래 금지), 실패하면 표시된 단계와 검토한 진단ZIP 확인. QR 원문은 공유하지 않는다. (2) 진단 탭/최근 오류 체크를 클릭한 뒤 포인터를 밖으로 이동: 선택 밑줄·체크 표시만 유지되는지 확인. (3) Tab/Shift+Tab의 점선 포커스, 빠른 탭 전환, 비활성→활성, 드롭다운 열기·닫기를 직접 확인. Codex는 이 작업을 실행하지 않았다.


## 0.20.1 후속 보완 (2026-10-29 KST)

기능324/324·비표시WPF66/66·배포계약19/19 PASS, 경고/오류0. FeeModeTests 신규8개: 소유 vanity/canonical 한 번 이동, 소유 표시 누락/거짓/중복·타 계정·외부·임의 쿼리·로그인/보안·두 번째 이동 거절, 403/429 중단/추가 전송0, ModeMissing에서 구형 추측 없음, 인증/독립 조회 보존. UsabilityTests: TBH 카탈로그 추가→제거→재로그인 미재추가, 자동 보관함 요청0. ProductAuditUiTests: 빈 계획 Collapsed→계획 존재 Visible→제거 Collapsed, TBH 미조회 표시. 자체 이미지 직접 검토. 실제 수수료 해결/TBH context2/물리입력·Popup·모니터DPI NOT VERIFIED. 최초75개 판정47 COMPLETE/16 PARTIAL/0 MISSING/3 BLOCKED/9 UNVERIFIED 유지.

## 0.20.2 최종 회귀

추가 classic 정상/지갑 분류/모드 충돌 회귀3건 수정 전 FAIL→수정 후 PASS. 신규5건을 포함한 기능329/329 PASS. explicit false·소유 표시·계정/지갑 일치·앱별 설정 요구, 값 누락/잘못된 타입·중복/주석·문자열 위장/모드 충돌 차단, HTTP200·ModeMissing은 성공 추측 없음. 실제0.20.1 사용자 vanity302→200은 REAL_READ 관측이며 새 classic 계산의 REAL_FEE 검증은 아니다. 비표시 WPF·배포·정리 fixture 결과 및 ZIP 검사는 배포 확정 절에 기록한다. 최초75개 판정47/16/0/3/9 유지.

## 0.20.3 활성화 실패 안내·진단 연결 (2026-10-29 KST)

0.20.3: 계정 불일치/가용 자산 없음/아이템별 publisher 근거 누락/보관함 접근 거부 오류 주입 시 정확한 TradingCheck와 OperationId, 거래 OFF/판매·취소 전송0, 한글 단계 안내, 민감정보 미노출을 검증했다. 실패 후 독립 판매목록 성공에도 진단 ID는 실패 작업 유지, 로그아웃 초기화. 기존 보호 조건과 단일 승인 배치·Unknown·저널 경로 유지. 실제 사용자 활성화 실패의 근본 원인은 제출 ZIP에 해당 작업이 없어 NOT VERIFIED이며 수정이 실거래 성공을 뜻하지 않는다.

## 0.20.3 실제 소스 대조·기본 모드 보완 (2026-10-29 KST)

최종0.20.3 추가 회귀: 앱 계약이 확인된 classic 수수료 + 선택적 아이템 필드 없음→검토→활성화→동일 실제 어댑터 합성 전송→모바일 승인 대기. 수정 전 FeeFor Unsupported FAIL, 수정 후 PASS. 다른 앱/수수료 상충/무근거 schedule은 거절하고 기존 React 필드 누락 보호 회귀도 유지한다. 기본 모드 ON은 검증 전 CanTrade=false, 최종 검토 사전검증/명시적 OFF·취소 전용 미승격/최종 승인 전 판매0을 검증한다. 실서버 성공으로 판정하지 않는다.

최종0.20.3: 기능333/333·비표시WPF66/66·배포계약19/19·정리fixture9/9 PASS, 경고/오류0. 전체 ZIP entry SHA/파일 수/메타데이터·실제전용·런타임/SQLite/라이선스·비밀/사용자자료 출하 제외 PASS. 새 기본 체크/안내 RTB 직접 검토. 실제 Steam 계정/판매·물리입력·모니터DPI는 NOT VERIFIED. 사용자 제출 ZIP에는 활성화 실패 기록이 없어 활성화 성공/실패 원인을 확정하지 않았다.

## 0.20.4 판매 응답·속도 보완 (2026-10-29 KST)

0.20.4 신규 SubmissionTransportTests 8개: 쿠키 회전/POST 현재 CSRF, 정상 AJAX 판매 헤더, HTTP400→Unknown/Guard 오표시 없음/재전송0, JSON 성공 플래그·Challenge·금지 HTML·오류/대용량 본문 안전 처리/민감정보 미기록, 읽기/쓰기 간격 순수 계산, 확인된 canonical 이동 재사용과 신형 모드 재조회, 지갑/수수료 공통 Market GET1회. 수정 전3개 FAIL→수정 후PASS. 실제 HTTP400 원문과 새 버전 실계정 성공/실제 속도는 미검증이다. 기존 불명 저널/예약은 삭제하지 않는다.

## 0.20.5 전체 경로 대조 회귀

게임A 권한 후 게임B 추가 선택의 사전검증 누락을 수정 전 재현(341/342 PASS, 해당1 FAIL). 수정 후346/346 기능·67/67 비표시WPF·19/19 배포 계약·9/9 정리fixture PASS. 새 검사는 AdditionalGame, ApprovalChannels, EmailJournal, PendingWithoutId, EmptyCancellation 및 ProductAuditUiTests 승인채널/빈 요청 선택기다. 이메일 채널의 재시작 영속화·구 저널 호환·ID 없는 후보 오인 금지·200 빈 취소 ACK와400 Unknown을 확인했다. `runtime/ui-tests/flow25-approval-channels.png`를 직접 검토했다. 물리 입력/호스트 GUI/실계정/장시간 사용은 미검증. 원본과 실행 경로 대조는 OPEN-SOURCE-FLOW-AUDIT.md 참조.
