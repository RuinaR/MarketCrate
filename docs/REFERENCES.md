# MarketCrate — 외부 자료·레퍼런스

> 2026-10-28 KST 기준 최초 확인. Codex는 작업 시작 시 링크/라이선스/현재 작동 상태를 다시 확인하고 확인 날짜를 갱신한다. 아래 자료는 기술적 사례이지 Valve의 허가가 아니다.

| 출처 | 조사/참고할 항목 | 주의점 |
|---|---|---|
| https://steamhelper.app/ | 보관함 → 다중 선택 → 가격 검토표 → 등록 UX | 화면/브랜드/아이콘 직접 복제 금지 |
| https://cslytics.com/ | PC 대량 판매, 진행 상태, 보관함·판매 목록 및 대시보드 | AGPL 프로젝트로 소개됨; 소스 재사용 시 라이선스 영향 확인 |
| https://github.com/federicogiorgi/SteamMarketHelper | 판매목록·중복·가격 시각화, 작은 스크립트 구조 | 사용자 스크립트 구현 방식 자체가 허용의 증거는 아님 |
| https://github.com/f00d4tehg0dz/steam-cards-bulk-seller | 페이징, 가격 캐시, 수수료, 대량 판매 엔진 사례 | MIT 표기. 복사 시 원문 라이선스 확인; 자체 코드는 분리 작성 |
| https://store.steampowered.com/subscriber_agreement/ | Steam 무허가 자동화/계정 제재 제한 | 읽고 실거래 연동 차단 조건 정할 것 |
| https://help.steampowered.com/en/faqs/view/2E6E-A02C-5581-8904 | 모바일 확인이 게시 전에 필요한 이유 | 승인 대기와 게시 완료 구분 |
| https://steamcommunity.com/dev | Steam 공식 Web API 제공 범위 | 로그인 ID/데이터 읽기/거래 권한 구별 |
| https://help.steampowered.com/en/faqs/view/61F0-72B7-9A18-C70B | 구매 주문 처리, 체결 우선순위 재확인 | 호가단계 가격 제안이 체결 보장은 아님 |
| https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/styles-and-templates | ControlTemplate, VisualStateManager, WPF 테마 구현 | 직접 구현해도 접근성/가상화 유지 |

## 레퍼런스 채택 규칙
- 기능적 패턴/레이아웃 정보는 참고하되 경쟁 제품 코드·아이콘·시각 자산/문구는 그대로 재사용하지 않는다.
- 라이선스를 확인하지 못한 코드/서드파티 라이브러리는 프로젝트에 추가하지 않는다.
- `검증한 정보`, `추정`, `미확인`, `실험적`을 구별하고 외부 변경은 기록한다.
- 다른 앱의 실제 Steam 계정 연동 사례가 있다고 해서 계정 제재 위험이 낮다는 결론을 내리지 않는다.
