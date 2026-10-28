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
