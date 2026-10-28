# MarketCrate — 현재 상태 (2026-10-28 KST, 2차 구현)

**Mock 기능과 창 없는 WPF 자체 렌더 검증 완료. 실제 사용자 입력·팝업·모니터 DPI·실거래는 미검증/차단. 실등록 제품 완료가 아니다.**

| 기능 | 현재 상태 | 근거 |
|---|---|---|
| WPF 커스텀 UI | DETACHED_RENDER_VERIFIED | MainView/컨트롤을 STA에서 렌더, 실제 Window 미생성 |
| 대량 카드/목록 | MOCK / PASS | 10,000개 카드, 생성 9개→끝 7개, 목록 recycling·필터 Reset |
| 가격/공유 호가 분배 | MOCK / PASS | 합계/검토 동일 SalePlanner, 잔량 부족·수량 보존 |
| 최종 승인→전체 배치 | MOCK / PASS | 분할 단가·수량/가격 편집/직전 정합성/부분 결과 |
| 중복·미확정 결과 관리 | MOCK / PASS | intent 저장, 재전송 금지, 이력 예약 수량/상태 역행 차단 |
| SQLite 영속화 | MOCK / PASS | 설정/합성 inventory/배치 이력/증거/판매 목록, 트랜잭션·v1 이관 |
| 판매 목록 개별 선택/취소 | MOCK / PASS | 배치 결과 연결, 별도 검토, 취소/실패, 취소 불명 복구 |
| 재시작/승인 대기/결과 확인 | MOCK / PASS | 합성 증거 대조, 명시적 fixture→Listed, 실제 승인 아님 |
| 실제 Steam 읽기·계정 연결 | Unavailable | 허용 경로·권한 미확보, 네트워크 어댑터 없음 |
| 실제 전체 판매등록·취소·재등록 | LIVE_INTEGRATION_BLOCKED | Valve 허용 연동·활성화·실환경 검증 필요 |
| 가격 변경·불명 항목 수동 재시도·배포 | NOT_IMPLEMENTED | 후속 작업 |

## 실제 검증

- `scripts/Verify.ps1`: 5개 프로젝트, 단일 빌드 작업, **경고 0 / 오류 0**. 도메인/SQLite/MVVM **41/41 PASS**, 창 없는 WPF **10/10 PASS**.
- 1100×700 / 1280×800 / 1380×820 / 1600×900 DIP, 비트맵 출력 밀도 100/125/150/200%. 실제 모니터 DPI 전환 시험이 아니다.
- 상태 시트의 Button CommonStates/FocusStates, Check/Radio/Toggle 선택, 가격 오류 바인딩/명령 차단, NumericStepper 범위/managed peer, Tooltip 자체 템플릿, 예상치 못한 바인딩 오류 및 Window/PresentationSource 미생성 검사.
- 보관함·검토·가격 오류·배치 대기/완료·취소 검토/결과·대량/빈 상태·컨트롤 PNG를 `runtime/ui-tests/`에 생성하고 직접 시각 검토했다. Git 제외.
- 10,000개 카드에서 viewport 주변 컨테이너 9개, 끝 7개. 한 번의 fixture+복수 렌더 관측 259ms는 비교 벤치마크나 장시간 성능 인증이 아니다.
- SQLite 실제 파일 재열기·독점 lease·JSON 일회 이관·삭제 후 재이관 방지·저장 실패 롤백·응답 유실 대조·확정 상태 보존·예약 수량 중복 차단 검증.

## 안전/미검증

- **실제 최상위 창 표시·활성화, 포커스 이동, 전역 입력, 데스크톱 캡처, 타 프로그램 열거/조사/조작, Steam 로그인/연결/거래는 수행하지 않았다.** 테스트는 MainView의 자체 렌더와 모의 상태/명령만 사용했다.
- 실제 마우스/키보드/휠/Tab/Esc, 열린 ComboBox/ContextMenu/ToolTip, 스크린리더, 모니터별 DPI, 긴 이름/폰트 확대의 전체 검증, 장시간 부하·강제 전원 차단은 NOT VERIFIED.
- 신규 실제 데이터/인증비밀을 저장하지 않는다. SQLite도 합성 Demo 전용이다. 실제 서버 결과를 Mock 증거로 대체할 수 없다.

## 다음 우선순위

1. 승인 범위가 확대된 경우에만 실제 앱 입력·팝업·모니터 DPI·접근성 검증. 현재 범위를 넘어 실행하지 않는다.
2. 가격 변경·재등록 Mock 검토, 불명 항목 수동 대조/재시도 UX, 다중 배치 이력 탐색, 임의 높이/긴 이름 카드·범위 선택/드래그.
3. 허용된 실제 읽기·쓰기 경로와 권한 확보. 확보 전 LIVE_READY/RELEASE_READY 차단 유지.
4. 의존성/라이선스·설치/배포 감사, 장시간 안정성 및 추가 저장소 스키마 마이그레이션.

## Git

시작 HEAD `65e3ef4`와 fetch한 `origin/main` 일치, 기존 미커밋 변경 없음. 이번 구현 커밋/푸시 결과는 WORKLOG의 후속 기록과 최종 보고에서 확인한다.
