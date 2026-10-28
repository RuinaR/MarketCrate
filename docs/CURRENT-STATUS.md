# MarketCrate — 현재 상태 (2026-10-28 KST, 4차)

## 4차 최신 상태: 실험적 HTTP 소스 구현 / 실계정 제품 흐름 미완성

구현·검증 커밋 `61fbd58` origin/main push 완료. 최종 기록 커밋은 완료 보고 참조.

- 일반 앱은 계속 Mock/IntegrationBlocked. 새 Infrastructure/Steam 코드는 App.OnStartup에 주입하지 않는다. 비공식 경로 소스 구현 허용과 Valve 허가·실계정 테스트 승인을 구분한다.
- 구현: session-only 위험별 옵트인, QR Begin/Poll/finalize/Community token, 자체 메모리 쿠키, 명시적 보관함·호가·중간가격 이력·목록 갱신, 직렬 실제 sellitem/removelisting 전송, immutable batch preflight, 별도 SQLite intent/delta results, 서버ID 대조·불명 복구. 기존 도메인 provider/executor/coordinator 계약을 재사용한다.
- 자동 검증: `scripts/Verify.ps1` 5개 프로젝트 빌드 경고0/오류0, 도메인/저장/MVVM/합성 Steam HTTP **87/87 PASS**, 비표시 WPF **17/17 PASS**, FAIL0. 새29개 계약 검사 포함. 이전58개 및16개 회귀 유지.
- 최신 `scripts/Publish.ps1` Release 실행파일 생성·PE/로컬 데이터 제외 PASS. 실행·설치·서명·공개 배포는 수행하지 않았다.
- 자체 `stage4-integration-status.png` 렌더를 검토해 좁은 안전 화면 오른쪽 여백을 수정하고 카드 경계 검사 PASS. 창/PresentationSource는 생성하지 않았다. 실제 입력·팝업·모니터 DPI는 미검증이다.
- 새 Steam 저널128행 delta 저장/재열기·1MiB 미만 payload, 두 번째 결과 쓰기 실패 전체 rollback, 미래 스키마 원본 보존·독점, auth marker 비저장 검사 PASS. 10,000개 합성 parser/UI 가상화 검사 PASS. 최대 규모/장시간/실네트워크 성능 인증 아님.
- 실제 네트워크 검증은 공개 자료와 무인증 orderbook 1회 HTTP200뿐이다. 인증/계정/판매/취소/Guard는 테스트 handler로만 처리했다. 실제 계정 로그인·등록·취소·승인 없음. 전체 화면/타 앱/전역 입력/프로세스 조사 없음.
- **미완성/차단:** QR/실계정 WPF 연결, 원화 등 USD 외 실등록·스택 분할, 전체 계정 거래 제한·보류 해석, 응답 listingid 없는 결과의 자동 확정, 확정 취소 이력·새 자산ID 매핑·실제 취소→재등록 saga, live 수동 retry, 다중 실배치 관리/보안·설치·서명 감사.
- Steam 중간가격 이력은 정확한 거래 VWAP가 아니다. live 제출은 USD 비스택 개별 자산과 exact fee만 허용하는 **실험적 계약**이며 실사용 검증을 뜻하지 않는다. 실저널은 일반 Demo 삭제 버튼과 분리했다. 현재 라이브 활성화 UI는 없다.
- 비공식 제재 위험은 인증 보안을 강화해도 사라지지 않는다. 상세 근거·라이선스·직접 관측 한계·개발자 옵트인 절차: [STEAM-INTEGRATION-FEASIBILITY.md](STEAM-INTEGRATION-FEASIBILITY.md). 실제 일괄 등록 제품 목표는 계속 미완료, LIVE_READY/RELEASE_READY가 아니다.

## 3차 상태 기록 (당시 기준)

**핵심 판매·관리 Mock UX와 비표시 렌더·복구 검증 완료. 실제 Steam 등록 기능 완료가 아니며 LIVE_INTEGRATION_BLOCKED를 유지한다.**

| 기능 | 상태 | 구현/검증 범위 |
|---|---|---|
| 최종 개별 가격 검토 | MOCK / PASS | 구매자 단가·수량·Steam/게임 수수료·실수령·합계 즉시 갱신 |
| 호가별 분배/부족 잔량 | MOCK / PASS | 공유 잔량, 미산정 수량 별도 빈 가격 행, 직접 입력 전 전체 제출 차단 |
| 수동 가격/전략/신선도 | MOCK / PASS | 열린 검토만 유효, 전략/닫기/갱신 초기화, 5분 신선도·최종 정합성 검사 |
| 단일 승인 전체 등록 | MOCK / PASS | 불변 스냅샷, 전체 선택·개별 결과, 실제 Steam 제출 아님 |
| 판매 목록 개별/전체 선택·취소 | MOCK / PASS | 별도 검토, 부분 취소 실패·불명·승인 대기 구분 |
| 판매 가격 변경 | MOCK / PASS | 취소 확인→새 가격 재등록, intent/부분 실패/Guard 대기 |
| 수동 재시도 | MOCK / PASS | 확정 실패만 검토, 시도 번호·새 요청 키, 기존 성공/Unknown 재전송 차단 |
| 재가격 중단 후 재개 | LIMITED / PASS | 취소 확인 및 후속 미전송 증거가 있을 때만 명시적 재개 |
| SQLite v2·진단 | MOCK / PASS | v1 보존 이관, quick_check, 변경 결과/판매 행 저장, 고정 코드 최대500개 |
| 대량 표시/배치 | MOCK / PASS | 10,000개 뷰, 반복 스크롤 선택 보존, 128행 실제 SQLite 배치 재열기 |
| Windows 실행파일 생성 | GENERATED / STATIC PASS | Release framework-dependent publish, PE/데이터 제외 검사, 실행/설치 안 함 |
| 실제 로그인·보관함·장터 조회 | Unavailable | 허용 경로/권한·실계정 검증 없음 |
| 실제 등록·취소·재등록 | LIVE_INTEGRATION_BLOCKED | 일반 개인용 쓰기 허용 근거 미확보 |

## 실제 검증 결과

- `scripts/Verify.ps1`: 5개 프로젝트 빌드 경고0/오류0, 도메인/SQLite/MVVM **58/58 PASS**, detached STA WPF **16/16 PASS**, FAIL 0.
- 새 관리 화면까지 포함한 바인딩 오류 검사, 수동 가격/부족 수량/전략 초기화/가격 변경·재시도/로딩·Mock 조회 실패 렌더 검사. 자체 PNG를 생성하고 직접 시각 검토했다.
- 1100×700~1600×900 DIP, 비트맵 출력 밀도 100/125/150/200%. 실제 모니터 DPI 전환 시험이 아니다. Application.Windows=0 및 PresentationSource=null 검사.
- 10,000개 카드 viewport 생성 9개→끝7개, 반복 스크롤 후 선택 보존. 마지막 fixture+복수 렌더 관측282ms는 비교 벤치마크/장기 안정성 인증이 아니다.
- 128행 Mock 배치를 실제 SQLite로 처리·재열기하고 헤더/결과 DB payload <1MB를 검사했다. 대량 전체 JSON 반복 쓰기를 변경 행 저장으로 교체했다.
- 실제 SQLite 손상 거절·스키마 이관 실패 rollback·복수 결과 변경 rollback·설정 보존. 프로젝트 headless 테스트 자식만 자기 Exit하여 intent 복구/미커밋 transaction rollback을 확인했다.
- `scripts/Publish.ps1`: 최신 Release 실행파일 생성/PE 헤더·출력 DB/로그/키 제외 PASS. 앱·설치 프로그램은 실행하지 않았다.

## Steam 조사

[STEAM-INTEGRATION-FEASIBILITY.md](STEAM-INTEGRATION-FEASIBILITY.md)에 공식 인증/API/약관/호출 제한·Support 확인 절차·오픈소스/라이선스·해제 조건을 기록했다. publisher/소유 앱/secure server 제한을 개인 계정의 쓰기 권한으로 사용할 수 없었다. 기술적 사례는 존재하지만 Valve 허가로 해석하지 않는다. 이번 조사로 실연동을 활성화하지 않았다.

## 안전 및 남은 제한

- 실제 최상위 앱 창·활성화·전역 입력·타 앱 조사/종료·전체 화면 캡처·실제 Steam 로그인/거래/Guard는 수행하지 않았다. Process.Start는 검증 대상인 프로젝트 headless 자식뿐이며 CreateNoWindow로 실행 후 자식이 자기 프로세스에서 종료했다.
- 실제 사용자 입력/Tab/휠/포커스, 열린 팝업, 모니터 DPI, 스크린리더, 클린 Windows 설치·업데이트/서명/배포, 전원 차단·장시간 장애는 NOT VERIFIED.
- 관리 요청이 Unknown이면 외부 증거 없이 성공/실패 확정·재전송하지 않는다. 현재 Mock에 근거 없는 불명 관리 요청을 수동 확정하는 도구는 없다. 목적을 알 수 없는 구형 실패/불명도 차단한다.
- 단일 활성 배치 및 고정 높이 카드 유지. 다중 배치 탐색·임의 높이/범위 선택/드래그·실제 온라인 페이징/요청 제한은 후속 작업이다.
- 재시도 가격은 원래 승인 가격/수량이며 새 검토·현재 자산/시세 확인을 요구한다. 실제 Steam 통화별 수수료·체결을 검증한 것이 아니다.
- 실제 단일 승인 실등록 목표는 미완료다. READ_ONLY_READY/LIVE_READY/RELEASE_READY가 아니다.

## Git

시작 `9ce53df`와 fetch한 origin/main 일치, 기존 미커밋 변경 없음. 조사 마일스톤 `db8a55c` push 완료. 3차 구현/검증 커밋 `1e6c280`도 origin/main push 완료. 최종 기록 동기화 커밋은 최종 보고에서 확인한다.
