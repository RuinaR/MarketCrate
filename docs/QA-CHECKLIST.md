# MarketCrate — QA 체크리스트 (자동 검사 완료 / GUI 미실행)

## 5차 최종 결과 — 2026-10-28 KST (최신)

| 검사 | 실제 결과 | 경계 |
|---|---|---|
| 전체 Debug 빌드 | PASS, 경고0/오류0 | 5개 프로젝트, MSBuild1/공유 컴파일 OFF |
| 도메인·SQLite·MVVM·합성 Steam HTTP | **112/112 PASS**, FAIL0 | 기존87 유지 + 신규25, 소켓/실네트워크 없는 handler |
| 비표시 WPF | **20/20 PASS**, FAIL0 | 기존17 + QR/검토/통합 배치 3, 바인딩 오류 없음 |
| 최신 Release publish | PASS | 실행파일·PE·데이터 제외·MIT LICENSE/고지 포함, 실행·설치 안 함 |
| 실제 계정 QR/지갑/fee/보관함/등록/취소/Guard | **NOT VERIFIED** | 인증 세션/거래 요청을 실제 전송하지 않음 |

- [x] KRW 원↔wire100·USD cent↔wire1 정수 타입, 미지원/분수 KRW/오버플로 거절, 서버 floor/step·rate/cap·최소/최근접 fee와 USD gap, 통화 불일치·서버 모순 중단.
- [x] 같은 상품의 다른 asset/context 공유 잔량, 3/4/3 스택 분할·비스택 반환 예약 해제·확정 archive·스택 취소 불명 예약 유지·Unknown 재시작 보존·capability 없는 stack 제출0.
- [x] 동일 UI/실제 어댑터에 합성 HTTP 주입: QR→인증 확인→데이터/선택 호가→KRW 편집/최종 단일 승인→전체 요청/승인 대기, 소유·통화·fee 검증 및 중복 차단.
- [x] QR 만료/거부/CAPTCHA/네트워크·취소/로그아웃/늦은 응답 폐기·동의 철회·기본 policy 차단. 모바일 자동 승인 구현 없음.
- [x] ID 없는 유일/복수 후보는 자동 Listed 불가, before IDs·요청/등록 시각·가격·수량·판정·감사 표시. 불명 요청 재전송0.
- [x] 확정 접수+목록 부재+원래 비스택 단일 자산 반환 후 재가격, 반환 부족/취소 timeout·재시작/중복/만료 시 새 요청 차단. 기존 target/후속키 보존.
- [x] 마지막 안전 검토에서 스택의 외부 증가/병합과 취소 반환을 혼동할 수 있음을 확인해, 스택/외부 등록은 인과 증거 없이 반환 proof 생성·예약 해제·재가격하지 않도록 제한. 합성 수량이 증가한 경우에도 Unknown/예약 유지 검사.
- [x] Steam DB v1→v2의 legacy 불명 의도 보존, 원자적 command/reprice audit, 확정 archive로도 예약 보존. 기존 손상/rollback/헤드리스 종료 회귀 유지.
- [x] 새 adapter-backed 10,000개 목록의 전체 선택→Totals 알림1회. 기존 Mock 10,000개 가상화/재활용 검사도 유지. 최대치/장시간/실서버 성능 인증 아님.
- [x] 자체 `stage5-qr.png`, `stage5-review.png`, `stage5-inventory.png`, `stage5-main-integration.png`, invalid/DPI 출력 PNG 생성·시각 검토. 발견한 글자색/Mock 혼동을 수정했다. 코드/이미지/DB/로그 중 생성물은 Git 제외.
- [ ] 실제 QR 모바일 스캔·계정별 모든 거래 자격/보류, ID가 바뀌는 스택·반환, 인과관계 없는 요청 강제 확정, stale 재가격 revision UI, live 확정 실패 retry, 실제 입력/Popup/모니터 DPI/설치·서명·보안 감사.

초기 연결 검사에서 WPF 프로젝트의 Http using 누락 빌드 실패와 템플릿 전 검색 테스트 실패를 발견·수정했다. 마지막 전체 회귀는 FAIL0이다. 통과한 동일 검사를 이유 없이 반복하지 않고 새 구현/저장·상태 변경/발견 결함 범위에서 다시 검사했다. 아래는 이전 단계 기록이다.

## 4차 최종 결과 — 2026-10-28 KST (최신)

| 검사 | 결과 | 검증 범위 |
|---|---|---|
| 전체 빌드 | PASS 경고0/오류0 | 5프로젝트, MSBuild1, 순차 실행 |
| 도메인/저장/MVVM/Steam 계약 | **87/87 PASS** | 기존58 + 신규29, 합성 HTTP only |
| 비표시 WPF | **17/17 PASS** | 기존16 + 상태 안내, 카드 경계·바인딩/창 미생성 |
| 최신 Release publish | PASS | 실행파일 생성·PE/로컬 데이터 제외, 실행·설치 안 함 |
| 실계정 QR/보관함/가격/판매/취소/Guard | NOT VERIFIED | 세션 요청을 실제 전송하지 않음 |
| 공개 orderbook GET | HTTP200 JSON 확인 | 무인증1회, 실제 계정 API/거래 검증 아님 |

- [x] 초기 비활성 전송0, 위험별/읽기·쓰기 동의 분리, HTTPS host/path/method 고정·redirect 거절, QR interval/만료·허용 transfer 제한·로그아웃 쿠키 폐기.
- [x] 401/403/429/로그인 redirect/HTML challenge latch, 변경된/구형 응답 거절, cursor 순환/식별자/통화/잔량 검증, 중간가격과 거래 VWAP 구분.
- [x] 단일 승인 복수 항목→각기 확정된 seller net 전송, exact 정수 fee·gap 거절, 현재 계정·자산·수량·가격 불일치 전송0.
- [x] 접수≠Listed, mobile/email 대기, listingid/가격/수량/통화/품목 대조, ID 없음/timeout 이후 재시작에서도 재전송0.
- [x] 취소 응답≠취소 완료, 중복 키/새 키 중복 취소 금지, timeout 의도 보존, 목록 부재로 재등록하지 않음. 실제 재가격 saga는 미구현이며 Mock 재가격 회귀만 PASS.
- [x] Steam 전용 DB128행 결과 delta 재열기·payload<1MiB, 두 번째 행 실패 원자 rollback, 미래 schema 원본 보존·독점 lease, auth marker 미저장.
- [x] 10,000개 합성 inventory parser 및 기존 UI 가상화·선택 보존. 새 안전 카드 `runtime/ui-tests/stage4-integration-status.png` 직접 시각 검토.
- [ ] 실제 QR 렌더/로그인 WPF·실계정 종단 흐름·원화/스택·확정 취소 이력/실제 재가격/live 수동 retry.
- [ ] 실제 입력/스크린리더/열린 Popup/모니터 DPI/실제 설치·서명/장시간 장애·보안 감사. 정책 제재 위험0 보장 없음.

검증 중 새 테스트의 구문/형식 오류 및 timeout 후 중단 세션에 대한 기대 불일치를 수정했다. 마지막 전체 회귀 FAIL0이며, 실패했던 실행을 성공으로 기록하지 않는다. 아래는 이전 단계 기록이다.

## 3차 최종 결과 — 2026-10-28 KST (최신)

| 검사 | 실제 결과 | 제한 |
|---|---|---|
| 전체 빌드 | PASS, 경고0/오류0 | 단일 MSBuild 작업 |
| 도메인·SQLite·MVVM | **58/58 PASS** | 순차 synthetic 검사, FAIL 0 |
| 비표시 WPF | **16/16 PASS** | 실제 창/입력/Popup 사용 안 함 |
| Release publish | PASS | PE/로컬 데이터 제외, 실행·설치·공개 배포 안 함 |
| 실제 Steam/Guard | BLOCKED / NOT VERIFIED | 문서 조사만 수행 |

- [x] 개별 단가/Steam·게임 수수료·실수령·검토/우측 합계 즉시 갱신, 수동 가격 전략 전환 초기화, 가격 오류 시 합계/명령 차단.
- [x] 매수 잔량 부족 수량을 빈 단가 행으로 유지, 입력 전 제출 차단, 명시적 직접 단가 후 전체 수량 스냅샷에 보존. 즉시 체결 보장 아님.
- [x] 확정 실패만 현재 시도 번호로 승인→새 시도 키/intent 저장→결과 대조. 기존 성공 유지, stale/double 승인/승인 대기/불명 재전송 거절.
- [x] 판매 전체 선택·개별 선택·가격 변경 검토, 취소 확인→재등록·대기, 취소/관리 실패 수동 재시도, 취소 직후 미전송 재개, 후속 intent 불명 차단·예약 보존.
- [x] v1→v2 설정 보존·마이그레이션 실패 rollback, 손상 SQLite 원본 보호, 두 결과 행 변경 중 두 번째 실패 시 전체 rollback을 실제 DB로 확인.
- [x] headless 소유 자식 Exit 후 committed intent→Unknown/재전송0, 미커밋 SQLite 변경 rollback. OS 전원 차단 시험 아님.
- [x] 128행 SQLite 배치 저장·재열기·최신 결과/이력 복원, 헤더/결과 payload <1MB. 10,000개 UI 반복 스크롤 후 선택 보존·컨테이너 범위 제한.
- [x] 고정 스키마 진단에 임의 설정값/예외·아이템 문자열 없음, DB 최대500개·조회30개, 앱 시작 오류 고정 코드 파일.
- [x] 신규 UI를 포함한 예상치 못한 바인딩 오류 없음. 의도적인 invalid 가격 변환만 제외. 로딩 버튼 영역 검증, Mock 조회 실패의 기존 데이터 보존.
- [ ] 실제 키보드/휠/포커스·열린 팝업/스크린리더/모니터 DPI, 설치·서명·다른 PC, 장시간 부하·전원 차단·실계정. 승인 범위 밖이거나 미실행.

새 자체 렌더: `runtime/ui-tests/stage3-shortage.png`, `stage3-manual-price.png`, `stage3-strategy-reset.png`, `stage3-reprice-review.png`, `stage3-reprice-edited.png`, `stage3-reprice-pending.png`, `stage3-retry-review.png`, `stage3-retry-result.png`, `stage3-cancel-retry.png`, `stage3-loading.png`, `stage3-network-error.png`, `stage3-load-return.png`. 대표 이미지를 직접 열어 검토했고 로딩 패널에서 중단 버튼이 잘리던 문제를 수정했다. 생성물은 Git 제외.

기존 SQLite rollback 검사는 저장 구조가 변경되어 result 행의 두 번째 쓰기에 실패를 주입하도록 조정했다. 실패를 무시하거나 보호 기준을 완화한 변경이 아니다.

## 2차 최종 결과 — 2026-10-28 KST

이 절이 아래 최초 구현 기록보다 최신이다. 실제 창은 표시하지 않았으며 WPF 컨트롤 렌더링은 수행했다.

| 검사 | 결과 | 범위/제한 |
|---|---|---|
| 솔루션 빌드 | PASS | 5개 프로젝트, 경고 0/오류 0, 단일 빌드 작업 |
| 도메인·SQLite·MVVM | 41/41 PASS | 기존 회귀 포함, synthetic only |
| detached STA WPF | 10/10 PASS | Window/PresentationSource 없음, 자체 렌더·모의 상태 |
| 10,000개 카드·목록 | PASS | 생성 컨테이너 시작 9/끝 7, 재활용·끝 스크롤·Reset·모드 전환 |
| 비트맵 밀도/크기 | PASS | 1100×700~1600×900 DIP, 96/120/144/192 DPI 출력 |
| 실제 입력/모니터 DPI/팝업/스크린리더 | NOT VERIFIED | 승인 범위 밖, 실제 창·포커스·입력 사용 안 함 |
| Steam 로그인/읽기/등록/Guard | BLOCKED / NOT VERIFIED | 실제 어댑터 없음 |
| 장시간 부하·전원 차단·설치 | NOT VERIFIED | 짧은 합성 검사로 인증하지 않음 |

- [x] 동일 품목 공유 매수 잔량, 합계/검토 단일 경로, 50개 합성 수량 보존 케이스.
- [x] 최종 직전 자산 수량 감소/누락·호가 변경 차단, 이중 실행·다른 활성 배치·예약 중복·상태 역행 차단.
- [x] SQLite 재시작·설정/inventory/증거 보존, 트랜잭션 롤백, schema99/null 손상 차단, JSON 원본 보존·일회 이관.
- [x] 응답 유실 뒤 재시작은 합성 증거와 대조만 수행, 증거 없는 Unknown 유지, Ready 자동 재개 없음.
- [x] 배치 결과→판매 목록→개별 선택→취소 검토→부분 취소/실패, 취소 intent 복구, 취소된 요청의 예약만 해제.
- [x] 가격 변환 오류 시 검토 명령 차단·검토 닫기 후 오류 복구, 단일 최종 명령·완료 결과 렌더.
- [x] 버튼 Normal/Hover/Pressed/Focus/Disabled VSM 렌더의 실제 색 차이, 선택 상태 및 Radio 원형, Stepper managed RangeValue/상한 overflow, Tooltip 템플릿 자체 렌더.
- [ ] 실제 호버 입력·키보드 Tab/Enter/Esc/휠·팝업 외부 클릭, 스크린리더/모니터 DPI 동작. 시각 상태 모의를 실제 입력 검증으로 표시하지 않음.

생성/시각 검토한 대표 파일: `runtime/ui-tests/inventory.png`, `inventory-100.png`~`inventory-200.png`, `review.png`, `review-error.png`, `batch-pending.png`, `batch-complete.png`, `cancel-review.png`, `listings-results.png`, `control-states.png`, `large-inventory.png`, `large-end.png`, `large-list.png`, `empty.png`, `tooltip-template.png`. 이미지/DB/런타임 로그는 Git에 포함하지 않는다.

이전 회귀 중 검토 열린 상태에서 배경 선택 해제를 하던 테스트는 실제 사용자 경로인 **검토 닫기→선택 해제**로 수정했다. 다른 활성 BatchId 호출은 조용히 이전 결과를 반환하는 대신 예외로 차단하도록 테스트 계약을 강화했다. 보호 기준을 완화하지 않았다.

표시: `[ ]` 미검증 / `[x]` 검증 완료. 실행한 적 없는 테스트는 통과 처리 금지.

## 2026-10-28 실제 결과

`scripts/Verify.ps1`: 4개 프로젝트 빌드 PASS (경고 0 / 오류 0), 콘솔 runner **25/25 PASS**. 테스트 이름과 assertion은 `tests/MarketCrate.Tests/Program.cs`에 있다. WPF 창/브라우저·실계정·전역 입력을 실행하지 않는다.

- [x] 합성 호가 3/4/3 분배·가격 순서·잔량 부족·잘못된 잔량.
- [x] VWAP 거래량 가중/빈 표본/정수 반올림, KRW Demo 수수료 최소값·역산·정수 보존·오버플로. 실제 Steam 통화별 산식은 미검증.
- [x] 스냅샷 불변성·자산 총량·중복 ID·모순 자산·비판매/보류/비 스택·만료/미래 시세·통화 거절.
- [x] 단일 승인 전체 처리·분할 단가 보존·중복 진입·부분 성공/실패/승인 대기·Blocked 거절.
- [x] 타임아웃 후 후속 중단·재전송 금지·Submitting 재시작 대조·선행 저장 실패 시 제출 0·취소 전 제출 0.
- [x] 실제 파일 journal 저장/재열기·독점 잠금·합성 삭제·손상 차단. 강제 전원 차단/장시간 안정성 검증 아님.
- [x] 실제 ViewModel의 게임/검색/필터·선택/수량·검토 편집·단일 최종 ICommand 실행/연속 클릭 차단·입력 오류 차단·저널 복구 시 신규 배치 차단.
- [ ] GUI 렌더링·hover/pressed/focus·포커스 복구·팝업·키보드·Stepper 휠·DPI·대량 가상화 성능·스크린리더. 사용자 승인 없이 실행하지 않음.
- [ ] 실제 읽기 요청·403/429·온라인 페이징·네트워크 캐시/취소·실등록·모바일 승인·서버 대조. 어댑터가 없으므로 미검증/차단.

아래 전체 제품 체크리스트의 복합 항목은 일부 자동 검사만으로 완료 처리하지 않는다.

## 1. 핵심 UI
- [ ] 모든 화면에서 기본 Windows/WPF 버튼·입력·체크·드롭다운 외형이 노출되지 않음.
- [ ] 컨트롤별 Normal/Hover/Pressed/Focused/Selected/Disabled/Loading/Error 상태가 적용 및 기능 동작.
- [ ] 키보드 탭·Enter·Space·Esc, 포커스 복구, 휠, 스크롤, 외부 클릭, 팝업 지연, 더블 클릭 처리.
- [ ] 검색/정렬/게임별 필터/부분선택/전체선택·해제 및 목록·그리드 전환.
- [ ] 높은 DPI/리사이즈/작은 창/긴 아이템 이름/천 단위 단가·수량/대량 품목 가상화.
- [ ] 선택/가격·수량 변화에 따른 합계 실시간 업데이트·모션 축소·CPU 부담 검토.

## 2. 데이터/가격
- [ ] marketable과 tradable, 게임 AppId/ContextId/AssetId, 비판매/거래 제한 항목 구분.
- [ ] 페이지네이션, 중복 응답, 빈 데이터, 취소·타임아웃, 오래된 캐시/403/429 경로.
- [ ] 최근 거래량 가중평균 기간·결측·불충분 데이터 처리.
- [ ] 구매주문 가격 단계별 3/4/3 분배, 수량 부족·호가 변경·거래 가격 불확실성 표기.
- [ ] 구매자 지불액 vs 판매자 실수령, 수수료·원화·환산·정수 반올림/역산.
- [ ] 수량 상한, 중복된 asset ID, 개별 아이템과 스택형 아이템의 가용 수량 검증.

## 3. 일괄 등록 목표 (Mock 단계)
- [ ] 최종 검토에서 사용자 확정 가격/수량/수수료/시점 포함 전체 스냅샷 고정.
- [ ] 최종 버튼 1회 → 선택된 모든 품목을 일괄 배치에 포함.
- [ ] 같은 종류의 아이템도 호가 구간별 서로 다른 단가·수량 보존.
- [ ] 버튼 연속 클릭·중복 이벤트·앱 재시작으로 중복 제출 방지.
- [ ] 일부 성공/실패/Unknown/승인 대기 및 복구 시 상태 동기화.
- [ ] `Submitted`/`AwaitingSteamGuard`/`Listed`/`Sold`를 정확히 구별.
- [ ] Demo/Blocked에서는 어떤 Steam 쓰기 요청도 안 나감.
- [ ] 실제 거래 연동이 미허용/미구현이면 LIVE_READY로 표시하지 않음.

## 4. 보안·프로세스 비간섭
- [ ] 타 프로세스 열거/핸들/후킹/메모리/주입/전역 키/입력/캡처/안티치트 접근이 코드에 없음.
- [ ] 로그인 비밀값·쿠키·Steam Guard 공유 비밀·토큰·개인정보의 출력·저장·커밋 없음.
- [ ] 자동 실행/상주/로그온 시작/작업 스케줄러/포커스 강탈 없음.
- [ ] 요청 캐시/명시적 갱신/제한 시 정지, 탐지 회피 우회 방식 없음.
- [ ] 메이플스토리 자체를 대상으로 하는 실행 여부 감지/조사조차 없음.
- [ ] 개발·테스트에서는 실제 Steam 로그인/실거래/다른 앱 조작이 수행되지 않음.

## 5. Git/보고
- [ ] 기능별 상태 문서와 완료 기준이 실제 코드와 일치.
- [ ] 작업 로그/설계/QA/출처 최신화, 민감정보 스캔, 필요한 파일만 커밋.
- [ ] push 성공/실패 및 커밋 해시·미실행 테스트를 마지막 보고에 명시.

## 실제 테스트 정책
기본은 정적 점검/Mock/순수 계산 테스트. 외부 앱 자동 구동, GUI 캡처, 실계정 쓰기 및 Steam Guard 조작은 별도 승인 없이 수행하지 않음. 메이플이 켜졌는지 확인하기 위해 프로세스를 탐색하는 행위도 금지.
