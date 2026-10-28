# MarketCrate — 현재 상태 (2026-10-28 KST)

**프로젝트 초기 시작 패키지만 준비됨. 실제 소스코드/빌드/테스트/계정 연결/커밋은 아직 시작하지 않음.**

| 기능 | 현재 상태 | 확인 내용 |
|---|---|---|
| WPF 앱/커스텀 UI | NOT_IMPLEMENTED | Codex가 최초 시작 후 구현 |
| 가격·호가별 분배 엔진 | NOT_IMPLEMENTED | 요구사항만 문서화 |
| 게임별 보관함/판매 중 조회 | NOT_IMPLEMENTED | 읽기 권한/허용 방식 확인 필요 |
| Mock 배치 등록 | NOT_IMPLEMENTED | 개발 초기 목표 |
| 실계정 전체 판매등록 | LIVE_INTEGRATION_BLOCKED | Valve 허용 연동 확인 전 활성화 금지 |
| Steam Guard 사용자 직접 승인 대기 | NOT_IMPLEMENTED | 향후 UI/상태 구현 |
| 배치 취소/판매 취소 | NOT_IMPLEMENTED | 별도 승인 및 허용 연동 필요 |
| 배포/설치 프로그램 | NOT_IMPLEMENTED | 향후 단계 |

### 최초 마일스톤
1. Git 상태 확인 및 레퍼런스/라이선스·허용 범위 검토.
2. `.sln`, App/Core/Infrastructure/Tests 솔루션과 테마·핵심 커스텀 컨트롤 구현.
3. Mock 보관함 UI, 아이템 전체선택·수량, 가격 전략 및 최종 검토.
4. Mock batch coordinator + 상태 UI + 단위 테스트.
5. README/문서·검증 결과 갱신, 자동 commit/push.

### 주의
`LIVE_INTEGRATION_BLOCKED`는 기능 구현 완료가 아니며 Steam 실판매가 가능한 상태라고 표현하지 않는다.
