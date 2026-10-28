# MarketCrate — 누적 작업 로그

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
