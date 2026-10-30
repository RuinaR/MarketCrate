# 실제 사용자 경로 검증 — 0.20.14 (2026-10-31 KST)

처음 실행 → 사용자 QR 승인 → 보관함 → 선택/수량 → 시세 → 최종 검토 → 단일 승인 전체 배치 → 공식 앱 수동 확인 → 서버 게시 대조 → 취소/재가격 → 종료/재시작을 기준으로 검사했다. 실제 UI에 연결된 ICommand의 CanExecute/실행 경로를 사용하며 HTTP는 프로세스 내부 합성 handler, SQLite는 runtime 아래 새 격리 경로다. 소켓 서버나 실제 Steam 계정/GUI 창을 실행하지 않는다.

임의 길이 입력·무한한 사용자 조작 순서·외부 서버 변경의 모든 조합을 완전 탐색한 것은 아니다. 입력 동등 분할/경계값, 상태별 금지 동작, 중요한 비동기 중단 시점과 기존 회귀로 범위를 명시한다. 비표시 텍스트/명령/Peer 검증을 실제 키보드·마우스·모니터 DPI/Popup 성공으로 바꾸지 않는다.

## 새로 재현하고 수정한 결함

1. NumericStepper의 LostFocus 정수 바인딩 때문에 화면 숫자와 VM 선택 수량이 포커스 이동 전 서로 달랐다. 실제 템플릿 TextBox를 변경하는 수정 전 검사는 FAIL이다. 입력 문자열을 즉시 전달하고 검증한 정수만 모델에 반영한다.
2. 잘못된 수량은 이전 정상 정수를 남겼지만 VM이 그 입력 오류를 알지 못했다. QuantityText/QuantityInputValid를 행에 보존하고 선택 항목의 오류가 있으면 검토를 막는다. 요약에도 이전 정상 수량을 확정값처럼 표시하지 않는다. 게임 전환/가상화에서 two-way Value 재연결이 오류 문자열을 덮던 문제는 Value를 OneWay로, InputText를 명시적인 TwoWay 입력 경로로 분리해 수정했다. 증감/관리된 접근성 Peer는 같은 문자열 경로를 갱신한다.
3. 명시적으로 게임/context를 제거해도 캐시의 선택이 남아 보이지 않는 판매 대상이 될 수 있었다. 제거 저장 성공 후 해당 scope의 보관함/판매목록 선택을 해제한다. 정상 화면/게임 전환과 캐시 정리의 선택 보존은 유지한다. 거래 저널은 삭제하지 않는다.
4. 개별 단가가 유효해도 수량 곱이나 전체 합계가 정수 범위를 넘으면 표시 getter에서 overflow가 발생할 수 있었다. 행의 수량·금액과 전체 합계를 함께 검증하고 오류 문구/비활성 승인으로 처리한다. Core의 기존 배치 총액 검증도 유지한다.

AsyncCommand.Execute와 새 await 가능한 ExecuteAsync는 동일한 CanExecute/중복 실행/예외 처리 본문을 사용한다. 테스트가 ViewModel 서비스 메서드만 직접 호출해 UI 활성 조건을 건너뛰지 않도록 했다. Release에 테스트 전용 실행 인자나 합성 주입 경로를 추가하지 않았다.

## 신규 UI 명령 경로 — UserJourneyTests

| ID | 사용자 동작·입력 | 확인한 상태/부작용 |
|---|---|---|
| UJ01 | 최초 실행, 동의 전 로그인/조회/승인/복구/관리 명령 | CanExecute false, HTTP/등록/취소0, 검토창 없음 |
| UJ02 | 로그인 → 보관함 →10개 → 시세 → 검토 → 승인1회 → 게시 조회 → 전체 취소 → 로그아웃 | 3/4/3 가격 배분, POST3회, Guard 대기와 Listed 구분, 취소3회, 미승인 재전송0, 로그아웃 데이터/권한 정리 |
| UJ03 | 수량12종: 빈값/0/음수/상한초과/int초과/소수/지수/구분자/공백/전각/NaN/문자 | 이전 정상 수량으로 검토 불가, 수정7은 즉시 반영, 직접 숫자 setter 범위도 검사 |
| UJ04 | 가격13종: 빈값/0/음수/소수/지수/구분자/통화기호/공백/NaN/초대형/최소미달/급락/급등 | 승인 불가·전송0·getter 안전, 정상 가격으로 수정하면 승인 가능 |
| UJ05 | 수량 곱 overflow, 개별 행은 유효하나 합계 overflow | 행/총액 오류, UI 예외 및 POST0. 거대 금액 fixture는 검증된0수수료 합성 계약이며 실거래 한도 증거가 아님 |
| UJ06 | 빈 응답과 malformed JSON/HTML/missing field, HTTP400/401/403/404/429/500 | Empty와 오류 구분, 자동 재시도0, 허용된 독립 목록 조회 유지,403/429 이후 전송0 |
| UJ07 | 기존 목록/수량을 둔 채 보관함 갱신 중 중단 | 기존 행·선택·수량 유지, Cancelled 표시, Busy 해제, 인증 유지 |
| UJ08 | 보관함 응답 대기 중 로그아웃 → 늦은 응답 → 재접속 | 늦은 데이터 재표시0, 연결/권한 정리, 새 연결은 거래 권한 자동 복원0 |
| UJ09 | POST 응답 대기 중 승인 재클릭·검토·게임 변경 | 선영속 intent1, POST1, 게임 변경 차단, 완료 후 Busy 해제 |
| UJ10 | 서버 처리 후 응답 유실 → 로그아웃/재연결 → 결과 확인 | Unknown 유지, 자동 재전송0, 이전 승인 재개0 |
| UJ11 | 판매 응답9종: JSON 누락/비JSON/HTML/success:false/400/401/403/429/500 및 두 번째 항목 실패 | 접수/Guard 성공을 꾸미지 않음, Unknown journal, 재클릭 POST 추가0; 부분 배치는 Guard/Unknown/Ready 각각 보존 |
| UJ12 | AppID/context/이름7종 잘못된 입력·중복·경로 문자열 | 카탈로그/인증 유지, 네트워크0, 예외 입력으로 성공 표시0 |
| UJ13 | 선택된 게임 명시적 제거 | 제거한 scope의 숨은 선택0, 검토 비활성, 거래 저널 보존 |
| UJ14 | 검색5종·정렬·페이지·게임 전환·선택 해제 | 행 identity/선택7개 유지, 명시적 해제 후0, POST0 |

UJ02는 동일 OperationId의 HTTP/JournalWrite/각 항목의 Committed 결과를 함께 확인한다. 인증 토큰/쿠키/계정 ID가 로그에 들어가지 않는지도 검사한다. ItemResult는 실행기 시작/종료와 코디네이터 확정 이벤트가 있어 단순 이벤트 총개수와 항목 개수를 혼동하지 않는다.

## 신규 비표시 실제 템플릿 — UserInputUiTests

| ID | 경로 | 검증 |
|---|---|---|
| UIJ01 | 수량 TextBox 직접 변경·포커스 이동 없음 | 화면7 → VM7 즉시 반영. 수정 전 FAIL/후 PASS |
| UIJ02 | 실제 SteamWorkspaceView의 잘못된 수량 → 게임 전환/복귀 → 수정 → 최신 조회 → 검토 → 가격 수정 → 승인 | 오류 문자열/빨간 테두리/검토 비활성 보존, 수량7의3/4 분배, 잘못된 가격은 승인 불가, 단일 승인 POST2·Guard 상태 |
| UIJ03 | 주요5개 화면에 실현된 ButtonBase.Command 바인딩 | 명령 객체 미해결 없음, 로그인/조회/결과/설정 진입 존재. 모든 명령을 물리적으로 클릭했다는 의미는 아님 |
| UIJ04 | 실제 입력 문자열 바인딩과 RangeValue Peer/증감 버튼/Disabled | 같은 모델 값/문자열 갱신, 비활성 상태 수량 변경0. 시스템 전역 UI Automation 미사용 |

실제 렌더 PNG: runtime/ui-tests/journey-invalid-quantity.png(125%), journey-review-200.png(200%), journey-results.png 및5개 페이지. 입력 오류 안내, 단가/수량/합계, 버튼 가독성을 직접 열람했다. CONTRACT TEST 표시는 테스트 구성에만 존재하며 일반 Release에는 Mock/자가검증 화면을 출하하지 않는다.

## 기존 회귀와 연결되는 나머지 경로

- SessionPersistenceTests: 최초 QR/동의ON-OFF/DPAPI/저장/재시작/거절·손상·삭제·계정/사용자 불일치·복원 중단. 신규 UI 명령 검사는 실제 사용자0.17 복원 관측을 대체하지 않는다.
- FeeModeTests/FeeResponseTests/WalletRedirectPatchTests/FifthStageTests: Legacy/New 조건, KRW/USD 정수 수수료·통화·반올림·호가 부족/공유/스택. 불명 계약 성공 fallback 없음.
- UsabilityTests/SubmissionTransportTests/ReleaseSafetyTests: 현재 서버 상태, 다중 게임/분할 가격, 최종 검토 만료/호가 변경/선행 journal 오류/동시 작업, ID 없는 결과, 취소·재가격·새 대상 검토, 계정별 journal, 단일 승인 snapshot 불변성.
- ThirdStageTests/SixthStageTests: SQLite 잠금/손상/마이그레이션/강제 종료·재시작, JSONL 상관관계/정제/회전/디스크 실패/ZIP 사용자 검토. 로그 실패로 거래 재전송0.
- 전체 WPF: Header Hover/Focus/Selected, 키보드 탐색 설정, 가상화10,000행, 이미지 취소/재활용,100/125/150/200% 자체 렌더. 실제 포인터·Popup·OS DPI는 미검증.
- 배포: 실제 전용 PE/BAML, x64 Core/WPF/SQLite/DPAPI, runtime 보안 최소 버전, 라이선스, 사용자 데이터·Auth·로그·개발 코드 제외, ZIP 전 파일 SHA 검증.

재실행: `scripts/Verify.ps1` 전체 순차 검사. 국소 재현은 기능 콘솔 `--user-journeys`, 비표시 WPF 콘솔 `--input-paths`를 사용한다. 이 인자는 테스트 실행 파일 전용이다. 상세 결과/최종 배포 해시는 CURRENT-STATUS와 QA-MATRIX에 기록한다.

## 남은 실환경 검증

새 릴리즈의 실제 Windows 창/물리 키보드·마우스/실제 Popup/모니터 DPI/설치·장시간 리소스, 실제 Steam 계정의 최신 수수료·자산/스택 계약/등록 접수/공식 모바일 승인/게시·취소·반환은 사용자 실행 단계다. 모든 조합100%/LIVE_VERIFIED/전체 RELEASE_READY 판정은 하지 않는다. 최초75개 ID와47 COMPLETE/16 PARTIAL/0 MISSING/3 BLOCKED/9 UNVERIFIED를 유지한다. 다른 앱·프로세스·화면·입력·운영 Auth/SQLite/진단에는 접근하거나 삭제하지 않았다.

최종 실행 결과: 기본 개발 런타임과 자체 포함 Core/WPF10.0.14 환경에서 각각 기능389/389 및 비표시WPF75/75 PASS. runtime/journey-final-regression.txt, journey-security-console-result.txt, journey-security-ui-result.txt에 보존한다. 배포20/20·advisory 검사기6/6, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). .NET/Windows/SDK를 시스템에 설치하지 않았고 자체 포함 테스트 콘솔만 순차 실행했다.
