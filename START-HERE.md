# MarketCrate — 최종 최초 시작 패키지

> 작성 기준: 2026-10-28 (KST). **아직 앱이 구현된 것은 아님**. 이 파일들은 Codex가 새 Git 저장소에서 개발을 시작하도록 하는 사전 명세/지침임.

## 개발 환경
- 대상: Windows 10/11 데스크톱. C# / .NET 10 LTS / WPF / MVVM. 프로젝트명 `MarketCrate`.
- Codex가 만든 기능을 단계별로 검증하고, 문서/커밋/푸시를 관리함.
- 사용자는 개발·사용 중 메이플스토리를 실행할 수 있음. 다른 프로세스 관찰이나 조작을 일절 수행하지 않음.

## 딱 한 번만 준비하기
1. GitHub에 이름 `MarketCrate`로 **빈 저장소**를 만듦. 공개/비공개는 사용자 선택. 가급적 Private으로 개발 시작.
2. 원하는 폴더에 `git clone <본인 저장소 URL>` 하여 클론.
3. 이 ZIP을 풀어 **`AGENTS.md` 등 파일이 바로 저장소 루트에 놓이도록** 모든 파일을 클론 폴더에 복사. ZIP의 `docs` 폴더도 같이 복사.
4. VS Code에서 **저장소 루트**를 열고 Codex를 실행.
5. `CODEX_INITIAL_PROMPT.md` 전문을 Codex에 붙여넣어 최초 개발 작업을 시작. 이후 대화에서는 `AGENTS.md`와 `docs/CURRENT-STATUS.md`를 기준으로 이어감.
6. Codex에 Git 원격(origin)이 연결되어 있으면 **논리적 작업 단위마다 필요한 파일만 commit/push**를 요청한 상태임. 권한/인증 실패는 Codex 보고를 보고 해결.

## 중요한 차이
- **필수 최종 제품:** 최종 검토창에서 한 번 `전체 판매등록`을 누르면 선택한 모든 품목/수량/가격의 **실제 Steam 장터 등록 요청**이 시작되는 앱. Steam Guard는 공식 모바일 앱에서 사용자 직접 승인.
- **최초 개발 단계:** 실제 로그인·판매가 아닌 가상 데이터/UI/가격·배치 코어. 실거래 가능성을 확인하기 전에는 실제 계정에 어떤 쓰기 요청도 하지 않음.
- Valve 약관상 승인되지 않은 자동화는 제재 위험이 있으므로, **동작하는 오픈소스 예제가 있다는 이유로 실거래 자동화를 허용된 것으로 취급하지 않음**. 사용자 마지막 버튼 클릭이나 Steam Guard 수동 승인도 약관 위험을 없애지는 않음.
- 허용되는 실판매 연동 수단이 확인되지 않으면 `LIVE_INTEGRATION_BLOCKED`로 보고하고, Mock 기능을 실판매 완성으로 속이지 않음.

## 동봉 파일
- `AGENTS.md` — 저장소 전체에 계속 적용할 Codex 기본 작업 규칙
- `CODEX_INITIAL_PROMPT.md` — **처음 한 번만** Codex에 입력할 최초 개발 프롬프트
- `.gitignore` — 비밀값/빌드 결과물 보호
- `docs/PROJECT-DEFINITION.md` — 사용자 기능·제품 범위·완료 조건
- `docs/UI-DESIGN-SPEC.md` — 독자적 UI와 모든 주요 커스텀 컨트롤 인터랙션
- `docs/SALE-EXECUTION-SPEC.md` — 최종 승인 한 번으로 전부 실등록하는 최종 목표 및 상태 모델
- `docs/SECURITY-AND-STEAM.md` — Steam 계정·메이플 실행 비간섭 규칙 및 금지 기능
- `docs/ARCHITECTURE.md` — 권장 계층/주요 인터페이스/데이터 모델
- `docs/REFERENCES.md` — 조사 대상 출처와 라이선스·정책 근거
- `docs/QA-CHECKLIST.md` — 실제 기능과 모의 기능 분리 검증
- `docs/CURRENT-STATUS.md` — 구현 현황 (현재는 미착수)
- `docs/WORKLOG.md` — 작업 기록 시작점

## 최초 시작 후 성공의 조건
- 문서만 만든 것이 아니라 **WPF 솔루션/다크 UI/커스텀 컨트롤/Mock 기능과 배치 계산 코어**를 실제 구현.
- `Mock 검증 완료`와 `실거래 연동 완료`를 별도 관리.
- Steam 계정 인증/거래와 다른 게임에 간섭하는 테스트는 Codex가 멋대로 수행하지 않음.
- 실제 수행한 작업만 보고하고, 커밋/푸시 결과까지 포함.
