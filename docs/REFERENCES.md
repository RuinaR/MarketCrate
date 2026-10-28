# MarketCrate — 외부 자료·레퍼런스

## 5차 조사 (2026-10-28 KST, 최신)

- 공식 Market/main.js가 불러오는 [marketselldialog.js](https://community.fastly.steamstatic.com/public/javascript/applications/community/marketselldialog.js)를 무인증으로 다운로드·정적 열람했다. wallet minimum/increment, ajaxfeestructure의 fee rate/cap, amount 전송/확인 대기 계약을 추가 확인했다. 원문 SHA256/구체적인 채택·확인 한계는 [연동 조사](STEAM-INTEGRATION-FEASIBILITY.md)의 5차 절 참조. 원 소스는 복사하지 않고 정수 계산·파서를 독립 작성했다.
- 공식 [global.js](https://community.fastly.steamstatic.com/public/javascript/global.js?l=english)의 통화 ID/whole unit와 판매 입력 wire/100 표시를 대조했다. 실제 지갑/인증된 fee API/거래는 호출하지 않았다. 비교 프로젝트의 과거 반올림 설명보다 최신 공식 소비 코드를 우선했다.
- [QrCodeGenerator v2.0.0 고정 커밋](https://github.com/manuelbl/QrCodeGenerator/tree/11cf58fe6cec5bc44f70aa360e84b8d37570e37d), MIT / Manuel Bleichenbacher / Project Nayuki. QR 순수 계산 6파일만 원 저작권/MIT 전문·LICENSE와 함께 포함, nullable 지시문 외 변경 없음. 새 SDK/패키지 설치/외부 이미지 서비스는 없다. THIRD-PARTY-NOTICES와 배포 복사·누락 검사를 추가했다.
- 경쟁 프로그램의 코드·시각 자산은 도입하지 않는다. 이전 단계의 “외부 소스 편입 없음” 문구는 당시 기록이며 5차에서는 위 허가된 QR 소스만 예외다. 기술적으로 동작 가능한 경로/합성 검사와 Valve 허가/실계정 검증을 구분한다.

## 4차 조사 (2026-10-28 KST, 최신)

- 지정한 CSlytics `b8a6be3`, SteamMarketHelper `e186d51`, todd-elvers/steam-bulk-seller `9de9e5c` default HEAD의 인증/세션/보관함/가격·판매/취소 관련 소스를 읽었다. 기능별 확인 한계·고정 permalink·라이선스·독립 구현/미채택 이유는 [실연동 조사](STEAM-INTEGRATION-FEASIBILITY.md)의 4차 표 참조.
- CSlytics AGPL-3.0, SteamMarketHelper LICENSE 본문 MIT, todd 프로젝트는 LICENSE 미확보. 자료를 실행하거나 코드를 편입하지 않았다. 별도 node-steam-session MIT 소스와 공개 인증 protobuf의 protocol shape를 비교했다. 새 패키지/브라우저 확장/웹 주입 없음.
- 공식 Market 공개 HTML/JS와 비로그인 multisell redirect를 확인했다. 공개 orderbook 1회 무인증 GET에서 신규 중첩/통화/잔량 쌍을 실제 확인했다. 다른 계정 보관함/세션/판매 API/모바일 승인은 호출하지 않았다. 실제 응답 원본·연구용 소스는 ignored runtime에만 있다.
- `pricehistory`의 중간가격 시계열과 정확한 거래 VWAP를 구분했다. 구 histogram 지원 주장은 최신 userscript 소스의 설명이며 실계정/엔드포인트를 직접 시험한 것이 아니다. 변경 시 fail-closed parser를 독립 구현했다.
- 비공식 자동화 제재 위험과 token 탈취 위험은 별개다. 사용자 승인은 구현 범위에 대한 것이며 Valve 허가가 아니다. 공식 문서/약관 링크와 향후 옵트인·실계정 미검증 범위를 조사 보고서에 누적했다.

## 3차 조사 (2026-10-28 KST)

공식 인증/Inventory/Economy/Market API·API 약관·자동화 약관·확인 절차와 공개 구현을 다시 조사했다. 범위/권한/미확인을 분리한 결과는 [STEAM-INTEGRATION-FEASIBILITY.md](STEAM-INTEGRATION-FEASIBILITY.md). 실제 로그인/API/거래를 시험하지 않았다. SteamHelper/CSlytics의 검토·가격 수정·상태별 관리 UX만 참고하며 코드/그래픽은 도입하지 않는다.

## 2차 재확인 / 채택 (2026-10-28 KST)

- https://steamhelper.app/ 와 https://cslytics.com/ 공개 소개를 재조회했다. 선택→단가 검토, 게임별 자산/판매 상태 분리, 배치 활동/대기 표시를 참고했다. 제품 설치·로그인·실행이나 소스/그래픽 복사는 하지 않았다. 기존 AGPL/미확인 자산 라이선스 제한은 유지한다.
- https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/how-to-create-a-bitmap-from-a-visual : 공식 Visual→RenderTargetBitmap 경로를 확인해 자체 컨트롤 렌더에 채택했다. 화면 캡처 API를 사용하지 않는다. 예제 코드를 복사하지 않고 테스트 뷰에 맞춰 작성했다.
- https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions : 공식 트랜잭션·단일 작성자 동시성 설명을 확인했다. 설정/저널과 배치 이력을 transaction으로 갱신하고 실제 실패 롤백을 검사한다.
- Microsoft.Data.Sqlite/Core 8.0.31 (MIT), SQLitePCLRaw 계열 2.1.12 (Apache-2.0): 기존 로컬 NuGet 캐시의 `.nuspec` 라이선스/종속성 메타데이터를 직접 확인하고 채택했다. 패키지 설치/다운로드 없이 캐시를 사용한다. 버전/해시는 packages.lock.json, 고지는 THIRD-PARTY-NOTICES.md에 기록한다. 전체 취약점·배포 라이선스 감사 완료를 주장하지 않는다.
- 외부 제품의 거래 자동화/인증 방식은 채택하지 않았다. 실제 Steam 쓰기 허용 근거를 새로 확보한 것이 아니므로 IntegrationBlocked를 유지한다.

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


## 6차 추가 확인 (2026-10-28 KST)

| 링크 | 실제 확인/기능 | 라이선스/채택 여부 | 한계 |
|---|---|---|---|
| https://store.steampowered.com/subscriber_agreement/ | §4.C Automation / §4.D Enforcement 공개 본문 확인 | 정책 출처, 코드/문장 직접 복사 없음 | 허용 근거 미확보, 기본 거래 차단 유지. 법적 판단/위험0 보장 아님 |
| https://help.steampowered.com/en/faqs/view/2E6E-A02C-5581-8904 | 열기 시도 | 새 코드 도입 없음 | 이번 응답은 이미지 한 줄로 본문 재검증 실패. 이전 기록과 구분 |

기존 SteamHelper/CSlytics/오픈소스 조사와 라이선스 기록은 유지했다. 이번 단계는 저장소 실행 경로 감사/독립 BCL 진단 구현이며 비교 제품 코드·새 외부 UI/QR 라이브러리를 추가 도입하지 않았다. live API 호환성을 재검증했다고 주장하지 않는다.
