# MarketCrate — 현재 상태 (2026-10-28 KST)

**최초 WPF Mock 구현 및 계산·배치·ViewModel 자동 검증 완료. GUI는 실행하지 않았으므로 UI/MOCK_READY 전체 판정은 보류. 실판매는 LIVE_INTEGRATION_BLOCKED.**

| 기능 | 현재 상태 | 확인 내용 |
|---|---|---|
| WPF 앱/커스텀 UI | IMPLEMENTED / GUI NOT VERIFIED | 빌드 PASS, 사용자 데스크톱 실행 없음 |
| 가격·호가별 분배 엔진 | MOCK / PASS | 24시간 VWAP, 잔량별 분할, KRW Demo 수수료 정책 |
| 게임별 보관함/판매 중 조회 | MOCK | 합성 7개 자산, 실제 읽기는 Unavailable |
| Mock 배치 등록 | CORE VERIFIED | 단일 최종 명령→전체 스냅샷, 중복 차단, 부분 결과 |
| 실계정 전체 판매등록 | LIVE_INTEGRATION_BLOCKED | Valve 허용 연동 확인 전 활성화 금지 |
| Steam Guard 사용자 직접 승인 대기 | MOCK | AwaitingSteamGuard/Submitted/Listed/Unknown 구분, 자동 승인 없음 |
| 배치 취소/판매 취소 | LIMITED / BLOCKED | 미전송 중단 코어, 실제 취소·가격 변경은 안내만 제공 |
| 디스크 저널·재시작 복구 | MOCK / PASS | v1 원자적 JSON·독점 파일 잠금, 재전송 없이 상태 대조 |
| 배포/설치 프로그램 | NOT_IMPLEMENTED | 향후 단계 |

### 실제 검증 (2026-10-28 KST)
- `scripts/Verify.ps1`: 솔루션 4개 프로젝트 빌드 PASS, 경고 0 / 오류 0. 설치된 SDK 10.0.301 사용, 추가 패키지 설치 없음.
- 의존성 없는 콘솔 assertion runner: **25/25 PASS**. 계산·시세 만료·수량·중복·단일 명령·부분 실패·타임아웃·복구·파일 잠금·MVVM 포함.
- 테스트 데이터는 저장소 내부 `runtime/tests-*` 격리 경로. 초기 OS 임시 폴더의 rename 권한 오류 2건은 경로 변경 후 PASS. 파일 저장 보호 기준을 완화하지 않음.
- NOT VERIFIED: 앱/브라우저 실행·화면 캡처, 실제 hover/키보드/팝업/DPI/스크린리더, 장시간 성능, 실제 Steam 로그인·조회·등록·모바일 승인.
- 타 프로그램 조사·조작 없음. 근거는 소스 정적 검색 및 사용한 도구 범위이며, 타 프로그램을 검사한 결과가 아님.

### 남은 우선순위
1. 사용자 승인 후 앱 자체 창만 수동 검증하여 XAML 런타임·바인딩·Tab/Esc·입력 오류·DPI·대비 문제 수정. 전체 화면 캡처는 하지 않음.
2. 대량 카드 가상화·범위 선택/드래그·상세 취소/가격 변경 Mock 검토, 모든 컨트롤 상태 및 접근성 감사. 현재 목록만 가상화, 카드 WrapPanel은 소량 Mock용.
3. 실제 읽기·쓰기의 Valve 허용 근거 확보. 확보 전 네트워크 어댑터 구현/활성화 없음. 허용 근거가 없으면 실등록 제품 목표는 BLOCKED 유지.
4. 실제 데이터가 필요한 단계에 SQLite 저장·마이그레이션·삭제 UX, 설치/배포 검증. JSON 데모 저널을 실거래 저널로 그대로 사용하지 않음.

### 주의
`LIVE_INTEGRATION_BLOCKED`는 기능 구현 완료가 아니며 Steam 실판매가 가능한 상태라고 표현하지 않는다.
