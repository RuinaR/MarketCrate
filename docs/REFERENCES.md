# MarketCrate — 외부 자료·레퍼런스

> 확인일: **2026-10-28 KST**. 웹 문서 조회 도구로 공개 페이지/본문/소스 구조를 확인했다. 브라우저 실행·설치·로그인·거래·제품 자체 실행·화면 캡처는 하지 않았다. 홍보 내용은 서비스의 주장이지 독립 검증이나 Valve 허가가 아니다. 외부 코드·이미지·문구는 제품에 복사하지 않았다.

| 출처 / 확인 결과 | 기능·구조에서 참고한 점 | 라이선스 / 채택·미채택 |
|---|---|---|
| https://steamhelper.app/ · 공개 소개 본문 확인 | 게임별 가치·조회 완성도 표시, 추천 단가 수정 후 사용자 검토 흐름. 실제 확장 UI는 실행하지 않음 | 공개 재사용 라이선스 확인 안 됨. 흐름만 참고, 주입·확장/인증·브랜드·자산 미채택 |
| https://cslytics.com/ · 소개/기능/활동 로그 이미지 설명 확인 | 보관함·판매 중·휴대폰 대기 분리, 배치 활동 내역 | 사이트가 AGPL로 명시. 코드/디자인 자산 미채택. 기능 주장으로 허가/안전성 판단 안 함 |
| https://github.com/federicogiorgi/SteamMarketHelper · README/폴더 구조 및 https://github.com/federicogiorgi/SteamMarketHelper/blob/main/LICENSE 확인 | 작은 userscript, 중복 정리·가격 비교·취소와 재등록 구분, 이름별 조회 재사용 아이디어 | 본체 MIT, reference 별도 고지. 소스 도입 없음. 스크립트 주입/실거래 방식 미채택 |
| https://github.com/f00d4tehg0dz/steam-cards-bulk-seller · README/파일 구조 및 https://raw.githubusercontent.com/f00d4tehg0dz/steam-cards-bulk-seller/main/content.js 확인 | 페이지네이션·TTL 캐시·정수 가격/수수료의 책임 분리 참고. 실제 네트워크 구현은 하지 않음 | 저장소 MIT 표기 확인. LICENSE raw 요청은 Cache miss로 본문 미확인. 코드 미채택. session bridge/쓰기/429 재시도는 미채택, 당사 요구는 403/429 즉시 중단 |
| https://store.steampowered.com/agreement/ 및 https://store.steampowered.com/subscriber_agreement/ · §4 본문 확인 | 무허가 자동화·장터/클라이언트 변조 제한, 계정 제한 가능성 | 계약 문서, 코드 라이선스 아님. 실등록 허용 근거 미확보→IntegrationBlocked 판단 |
| https://help.steampowered.com/en/faqs/view/2E6E-A02C-5581-8904 · URL 응답, 본문 추출 불가 | 기존 명세의 대기/게시 구분 유지. 이번 조회로 세부 확인 절차를 독립 검증하지 못함 | Support 자료. 실제 승인·상태 확인 전 재검증 필요, 자동 승인 미채택 |
| https://steamcommunity.com/dev · 공식 문서 본문 확인 | OpenID는 SteamID 신원 인증. 읽기 API/키와 거래 쓰기 권한을 분리 | 서비스 이용 조건 별도. 일반 개인 장터 일괄 쓰기 허가를 확인할 수 없었음. 계정 연결 미구현 |
| https://help.steampowered.com/en/faqs/view/61F0-72B7-9A18-C70B · URL 응답, 본문 추출 불가 | 매칭 우선순위 세부 미확인. 가격 제안을 체결 보장으로 표시하지 않음 | 정책·통화별 실제 동작은 후속 공식 검증 필요 |
| https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/styles-and-templates → https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/styles-templates-overview · 본문 확인 | ResourceDictionary/Style/ControlTemplate로 시각과 입력 로직 분리 | Microsoft 문서, 이번 작업에서 별도 콘텐츠 라이선스 본문 확인 안 함. 예제 코드 복사 없이 자체 템플릿 작성 |

## 레퍼런스 채택 규칙
- 기능적 패턴/레이아웃 정보는 참고하되 경쟁 제품 코드·아이콘·시각 자산/문구는 그대로 재사용하지 않는다.
- 라이선스를 확인하지 못한 코드/서드파티 라이브러리는 프로젝트에 추가하지 않는다.
- `검증한 정보`, `추정`, `미확인`, `실험적`을 구별하고 외부 변경은 기록한다.
- 다른 앱의 실제 Steam 계정 연동 사례가 있다고 해서 계정 제재 위험이 낮다는 결론을 내리지 않는다.
