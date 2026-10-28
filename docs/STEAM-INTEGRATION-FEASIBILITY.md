# Steam 연동 가능성 조사 — 2026-10-28 KST

## 결론

**현재 MarketCrate의 개인 계정용 실제 보관함·장터 쓰기를 활성화할 허용 근거/권한을 확보하지 못했다. IntegrationBlocked를 유지한다.** 구현 사례의 존재와 Valve의 허용은 다르다. 이 문서는 법률 의견/Valve 허가서/실계정 검증 결과가 아니다. 공개 문서만 조회했으며 로그인·키 발급·실제 API 호출·판매·취소·모바일 승인을 수행하지 않았다.

최종 제품의 필수 목표는 한 번의 `전체 판매등록` 승인으로 선택 전체를 제출하는 것이다. 이 목표는 유지하지만, 현 단계의 Mock 승인/게시 결과는 실제 Steam 성공이 아니다.

## 기능별 판정

| 기능 | 확인한 공식 범위 | 기술적 조건 / MarketCrate 판정 |
|---|---|---|
| 신원 인증 | Steam OpenID로 SteamID 확인 가능 | 장터 쓰기 권한·세션 위임이 아니다. 공식 브라우저 흐름/응답 검증을 별도 구현할 수 있으나 이번에는 미구현·미연결. [공식 안내](https://steamcommunity.com/dev) |
| 일반 Web API | 공개/사용자 키/publisher 키별 권한이 다름 | publisher 키는 보호된 서버에서 관리하고 클라이언트에 배포하지 말아야 함. 다른 회사 게임의 권한이 생기는 것이 아님. [인증 문서](https://partner.steamgames.com/doc/webapi_overview/auth) |
| 보관함 | `IInventoryService/GetInventory` 문서 존재 | Economy 권한 publisher 키와 앱/사용자 식별자를 요구하며 secure server 전용. 타사 게임 전체의 개인용 보관함을 수집할 일반 권한으로 해석할 수 없음. Community 공개 보관함 웹 인터페이스의 기술적 접근 사례도 API 허용 근거와 별개. [공식 문서](https://partner.steamgames.com/doc/webapi/IInventoryService) |
| 가격/상품 정보 | `ISteamEconomy/GetAssetPrices`, GetAssetClassInfo 등이 문서화됨 | 구매할 수 있는 아이템 가격·분류를 설명하며 최근 장터 VWAP/전체 매수 호가 깊이의 대체 API라는 근거가 없음. 앱·키·통화 범위를 확인해야 함. [공식 문서](https://partner.steamgames.com/doc/webapi/ISteamEconomy) |
| 장터 조회·취소 | `IEconMarketService`는 partners의 제한된 장터 접근 | GetMarketEligibility/GetAssetID/GetPopular 및 CancelAppListingsForUser 등이 문서화됨. publisher 권한·소유 앱/보안 서버 제한이 있으며, 취소 메서드는 특정 앱의 사용자 등록 **전체** 취소여서 검토한 개별 품목만 취소하는 계약과도 다름. [공식 문서](https://partner.steamgames.com/doc/webapi/IEconMarketService) |
| 개인 장터 등록/판매 중 목록/재가격 | 확인한 공식 문서에서 일반 제3자 데스크톱용 대량 등록 권한을 찾지 못함 | 일반 로그인 세션·웹 인터페이스가 필요한 비공식 사례는 있으나 이를 안전/허용으로 채택하지 않음. “공식 API가 절대 존재하지 않는다”는 단정이 아니라 **이번 조사에서 허용 경로 미확보** 판정 |
| 단일 최종 승인 일괄 등록 | 클라이언트 배치 오케스트레이션은 기술적으로 구성 가능 | 사용자 클릭 1회·모바일 수동 확인이 자동화 허가를 부여하지 않음. 허용된 제출 수단이 확보되어야 실등록 활성화 가능 |
| Steam Guard | 모바일 인증기를 쓰면 앱으로, 아니면 이메일 확인을 안내 | 확인 전 등록은 게시되지 않음. 승인 자동화·shared secret 수집 없이 사용자가 공식 절차를 진행하고 상태 재조회로 게시를 확정해야 함. [Steam Support](https://help.steampowered.com/en/faqs/view/2E6E-A02C-5581-8904) |

Support URL의 직접 open은 본문 대신 이미지/접근 실패를 반환했다. 이번에는 공식 도메인의 검색 색인 결과에서 확인 절차 본문을 읽었다. 로그인 상태나 실제 모바일 앱 동작은 확인하지 않았다.

## 정책·호출 제한과 계정 보호

- Subscriber Agreement §4는 무허가 변조·자동화와 계정 제한/종료 가능성을 설명한다. 외부 구현이 동작한다거나 마지막에 사람이 클릭한다는 사실만으로 위험을 없앨 수 없다. 탐지 회피·프록시 회전·쿠키 추출·클라이언트 주입을 대안으로 사용하지 않는다. [공식 약관](https://store.steampowered.com/subscriber_agreement/)
- Web API 약관에는 하루 100,000회 한도가 기재되어 있지만 **모든 장터 웹 요청에 적용되는 허용량이나 안전 목표가 아니다**. 서비스/메서드별 권한·제한은 별도다. 허용이 확보된 조회도 사용자 요청만, 캐시·중복 억제·단일 큐·취소를 기본으로 하며 403/429에서 즉시 중단한다. [API 약관](https://steamcommunity.com/dev/apiterms)
- 비밀번호·세션 쿠키·Steam Guard 비밀/복구 코드를 수집·저장·외부 전송하지 않는다. 서버가 필요한 publisher API를 쓰기 위해 개인 인증비밀을 서버로 보내는 설계를 도입하지 않는다. publisher 자격 자체도 현재 보유/승인했다고 가정하지 않는다.

## 존재하는 구현·라이선스

| 사례 / 조사 범위 | 확인 내용 | 채택 여부 |
|---|---|---|
| [steam-cards-bulk-seller](https://github.com/f00d4tehg0dz/steam-cards-bulk-seller), [content.js](https://raw.githubusercontent.com/f00d4tehg0dz/steam-cards-bulk-seller/main/content.js) | 브라우저 확장, 페이지네이션/캐시, 세션 bridge 및 장터 쓰기 코드 존재. 저장소 MIT 표기; LICENSE 본문 URL은 이번 도구에서 Cache miss | 구조적 비교만. 코드/세션 방식/비공식 쓰기/429 재시도는 도입하지 않음 |
| [SteamMarketHelper](https://github.com/federicogiorgi/SteamMarketHelper) | 이전 조사에서 userscript·MIT LICENSE 확인. 이번 raw LICENSE 조회 실패 | 등록/취소 흐름 비교만. 스크립트 주입 미채택 |
| [node-steamcommunity](https://github.com/DoctorMcKay/node-steamcommunity/wiki/SteamCommunity), [LICENSE](https://github.com/DoctorMcKay/node-steamcommunity/blob/master/LICENSE) | Community 기능 구현 라이브러리와 MIT 고지 확인 | 인증/확인 비밀을 요구하는 흐름은 프로젝트 안전 계약과 맞지 않아 미도입. 특정 메서드나 계정 안전성을 보증하지 않음 |
| [CSlytics](https://cslytics.com/), [SteamHelper](https://steamhelper.app/) | 공개 사이트에서 일괄 관리·가격 수정/검토·활동 내역 소개. CSlytics는 AGPL로 명시 | 사용 흐름만 참고. 실행·로그인·그래픽/코드 복사 안 함. 사이트 홍보를 정책 준수의 증거로 사용하지 않음 |

## 향후 안전한 개발 계획과 해제 조건

1. **현재:** 로컬 합성 데이터·SQLite·Mock 제출/취소/재시도/재가격·불명 상태 대조를 검증한다. 기능 상태는 Mock/Unavailable/IntegrationBlocked로 표시한다.
2. **신원 확인 후보:** 별도 승인 시 공식 OpenID만 사용하고 state/nonce·리턴 URL 검증·SteamID 최소 저장을 설계한다. 신원 확인 성공을 장터 연결 성공으로 표시하지 않는다.
3. **읽기 후보:** 지원할 게임·필드별 공식 API/권한/이용 조건·원천/통화/시각을 문서화하고, 허용된 게임부터 제한된 읽기 어댑터를 검토한다. 허용 미확인 보관함/시세는 Mock/Unavailable 유지. 최저 매도가·median을 VWAP로 둔갑시키지 않는다.
4. **쓰기 해제 조건:** Valve의 허용 근거와 실제 계정별 제출/취소/조회 권한, 안전한 인증 위임 방식, 호출 제한, 요청 중복/불명 대조 수단을 확보한다. 현재 조건을 충족하지 않는다. publisher 한정 경로가 필요하면 이 프로젝트의 여러 타사 게임 관리 범위를 다시 평가한다.
5. 조건 확보 후에도 실연동 옵트인과 별도 실환경 테스트 승인이 필요하다. 최종 검토 승인 1회→전체 불변 배치 제출→항목별 결과→공식 모바일 수동 확인→서버 게시 재확인을 검증한다. 허용 근거가 없으면 제품 실등록 목표는 계속 BLOCKED다.
