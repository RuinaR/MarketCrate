# Steam 연동 가능성 조사 — 2026-10-28 KST (4차)

## 4차: 구현 후보와 현재 제품 상태 (아래 3차 기록보다 우선)

이번 사용자 요청은 비공식 방식의 **별도 어댑터 소스 구현**을 허용한다. Valve의 허가를 확보한 것으로 취급하지 않는다. 앱 시작 구성은 여전히 Demo이며 `IntegrationBlocked`다. 다음 코드는 실계정 동작을 검증하거나 완성한 기능이 아니라 **실제 HTTP 계약을 갖춘 기본 비활성 실험적 구현**이다. 이 작업에서 인증 세션 생성/로그인/등록/취소/Guard는 실행하지 않았다.

### 근거와 확인 수준

공개 GitHub API로 조사 시점 default branch HEAD를 조회하고 다음 커밋의 관련 소스를 읽었다. 다운로드 자료는 ignored `runtime/research-stage4/`에만 보관했으며 실행·의존성 설치·외부 코드 편입은 없다.

| 프로젝트/고정 소스 | 인증·조회·변경·복구에서 확인한 내용 | 라이선스·채택 판단 |
|---|---|---|
| [CSlytics auth.ts](https://github.com/Xpar65/cslytics-desktop/blob/b8a6be3e4be0e08c44e0e3ae46151c091202b02a/packages/adapters-steam/src/auth.ts), [market.ts](https://github.com/Xpar65/cslytics-desktop/blob/b8a6be3e4be0e08c44e0e3ae46151c091202b02a/packages/adapters-steam/src/market.ts) | steam-session 기반 QR/Guard/refresh-token→자체 web session. norender 목록·sellitem·removelisting, 제출/불명 구분, 휴대폰 확인 대기를 관측. vault 추상 경계와 token audience 검사가 있음. adapter 소스 조사만으로 전체 엔진의 재시작 정확성/게임별 보관함 동작을 인증하지 않음 | AGPL-3.0. 코드/타입/자산 미복사. 접수와 사후 관측을 구분하는 설계만 독립 적용. GC/게임 클라이언트 세션 기능은 제외 |
| [SteamMarketHelper userscript](https://github.com/federicogiorgi/SteamMarketHelper/blob/e186d51efb742ddfb527d2ccb248c64fcee9c976/steam-market-helper.user.js), [LICENSE](https://github.com/federicogiorgi/SteamMarketHelper/blob/e186d51efb742ddfb527d2ccb248c64fcee9c976/LICENSE) | 로그인한 페이지의 세션 이용, 보관함 페이징·호가 cache·sellitem·removelisting. 최신 소스가 orderbook 중첩 형식과 구 histogram 문제를 명시. 자체 UI 및 수수료 gap 처리 코드도 있음. 기존 로그인 페이지에 실행되는 방식은 채택하지 않음 | LICENSE 본문 MIT 확인(GitHub API는 NOASSERTION). 소스 복사 없음. 페이지 주입/자산/개별 가격 추적 자동판매 미도입 |
| [steam-bulk-seller session.ts](https://github.com/todd-elvers/steam-bulk-seller/blob/9de9e5c285e926d1c2066fa06238b6e103f21508/server/src/session.ts), [sell.ts](https://github.com/todd-elvers/steam-bulk-seller/blob/9de9e5c285e926d1c2066fa06238b6e103f21508/server/src/steam/sell.ts), [listings.ts](https://github.com/todd-elvers/steam-bulk-seller/blob/9de9e5c285e926d1c2066fa06238b6e103f21508/server/src/steam/listings.ts), [inventory.ts](https://github.com/todd-elvers/steam-bulk-seller/blob/9de9e5c285e926d1c2066fa06238b6e103f21508/server/src/steam/inventory.ts) | 로컬 서버의 QR/쿠키 세션, 순차 전송과 판매자 수령액 price 전송, 보관함 페이징/SQLite cache, 가격 이력·목록 fallback. 일부 429/5xx 재시도와 목록 HTML fallback, refresh-token 저장 구조가 존재. 새 MarketCrate는 재시도/HTML fallback/평문 token 저장을 따르지 않음 | 현재 트리에서 LICENSE를 찾지 못했고 GitHub license 필드 없음. 공개 열람을 재사용 허가로 해석하지 않음. 코드 복사 금지 |
| [node-steam-session WebApiTransport](https://github.com/DoctorMcKay/node-steam-session/blob/master/src/transports/WebApiTransport.ts), [LoginSession](https://github.com/DoctorMcKay/node-steam-session/blob/master/src/LoginSession.ts), [공개 인증 스키마](https://github.com/SteamDatabase/Protobufs/blob/master/steam/steammessages_auth.steamclient.proto) | QR Begin/Poll의 protobuf field, JSON response, finalize→Community settoken 흐름 비교. WebBrowser audience 사용, 비밀번호·Steam Guard 비밀키 없이 사용자가 공식 앱에서 QR 승인하는 경로. protocol 숫자만 독립 최소 인코더로 작성 | node-steam-session LICENSE MIT 확인. 라이브러리 설치/소스 복사/SteamClient 위장 미도입. 추가 프로토콜 자료는 Valve의 제3자 자동화 허가가 아님 |

일부 CSlytics 공개 README의 기능은 adapter 파일로만 확인 가능했다. 전체 보관함·가격 수집 엔진/실계정 실패 복구를 모두 조사·실행했다고 주장하지 않는다. 각 프로젝트가 현재 계정에서 정상 작동한다는 검증도 하지 않았다.

### Steam 현재 공개 자료에서 직접 확인한 부분

- [공식 Market](https://steamcommunity.com/market/)의 공개 HTML과 거기에 연결된 [market.js](https://community.fastly.steamstatic.com/public/javascript/market.js?l=english), [economy_common.js](https://community.fastly.steamstatic.com/public/javascript/economy_common.js?l=english)를 읽었다. 코드를 실행하지 않았다. 확인 전 목록과 실제 게시를 구분하는 설명이 존재한다.
- [공개 orderbook](https://steamcommunity.com/market/orderbook?q=Load&qp=%5B730%2C%22Recoil%20Case%22%5D)을 인증/쿠키 없이 1회 GET: HTTP200 JSON, `data.success → data.data`, `eCurrency=16`, `rgCompactBuyOrders`의 가격/수량 쌍을 확인했다. 원본 응답은 Git에 저장하지 않는다. 이 공개 조회 성공이 개인 계정의 전체 읽기/쓰기 허가나 앞으로의 지원을 보장하지 않는다.
- [공식 품목 페이지](https://steamcommunity.com/market/listings/730/Recoil%20Case)는 당시 Market Beta 및 commodity 매수/매도 표, **Median Sale Prices**를 표시했다. 가격 이력 표본은 시간 구간의 중간가격과 거래량이며 실제 개별 체결의 정확한 VWAP로 사용할 근거가 없다. 새 provider의 `PriceSnapshot.Trades`는 비워 두고 중간가격 이력은 별도 타입으로 반환한다.
- [multisell](https://steamcommunity.com/market/multisell)은 비로그인 접근에서 공식 로그인 페이지로 이동했다. 인증 후 화면/지원 아이템/확인 횟수/일괄 등록 성공은 확인하지 않았다. 공식 페이지의 존재는 제3자 HTTP 자동화 허가가 아니며, 이번 작업에서 열거나 조작하지 않았다.
- `itemordershistogram`의 현재 실제 응답은 호출하지 않았다. 폐기/104 응답은 최신 userscript의 주장으로만 구분한다. 파서는 이 구형 형식을 받으면 지원 형식으로 추측하지 않고 중단한다.

### 구현된 계약 및 정확한 한계

| 범위 | 구현 | 현재 검증/제한 |
|---|---|---|
| QR 인증 | Begin/Poll protobuf 요청, challenge host 검사, 2분 만료, 서버 interval 준수, finalize 및 Community 전송만 허용 | 합성 HTTP만. QR 이미지/로그인 WPF 화면 미구현. 일반 브라우저 OpenID 로그인은 거래 세션을 주지 않으므로 대체하지 않음 |
| 자체 세션 | CookieContainer, Community domain token, CSRF, 로그아웃 시 만료 처리. token/cookie export/import/persistence 없음 | 메모리 문자열의 완전 zeroization 보장 아님. 기존 브라우저/Steam 프로세스/파일 접근 없음. 토큰 보존을 구현하지 않아 DPAPI 저장도 없음 |
| 보관함 | 명시적 app/context, description join, marketable/tradable 분리, fee metadata, bounded100페이지, 중복/cursor 순환 거절 | 계정별 장터 자격/보류 기간 전체 해석·게임 목록 자동발견 미구현. 알 수 없는 응답은 중단 |
| 가격 | 신규 orderbook 통화/단계 잔량, cache256, 명시적 Refresh, 중간가격 이력 별도 반환 | 실제 계정 호가/이력 미검증. exact VWAP 불가. 종목/네트워크 자동 갱신 없음 |
| 등록 | 기존 IMarketListingExecutor/배치 coordinator 계약, 최종 계정/자산/호가/수수료 재검사→저널→sellitem, price는 판매자 수령액. HTTP 직렬화/최소3초 간격 | 현재 실험 제출 계약은 USD, 비스택 개별 자산만. 원화 전송은 ×100 변환 검사까지만 있으며 실등록 차단. 50% 미만/200% 초과 호가 비교는 새 검토 요구. 제한이 곧 안전 보장은 아님 |
| 승인·게시 | success도 Submitted 또는 AwaitingSteamGuard. 공식 앱/이메일 확인은 사용자 직접. 응답 listingid와 목록의 통화/가격/수량/아이템을 함께 확인한 경우만 Listed | 응답에 listingid가 없으면 비슷한 목록으로 유추하지 않음. 이 경우 pending/Unknown 유지, 외부 근거 대조 도구 필요 |
| 취소 | 개별/배치 요청, 최신 목록과 검토 일치 검사, 취소 의도 먼저 저장, 빈 배열 응답도 Submitted까지만 | 목록 부재는 판매/취소 구분 불가. 취소 완료를 자동 확정하지 않음. **실제 재가격 취소→재등록 saga 미완성/차단** |
| 복구 | Demo와 별도 SQLite FULL/독점 lease, active snapshot·요청별 intent/evidence, 재시작은 대조만. 새 요청 키로 같은 자산/목록을 중복 처리하지 않음 | 단일 활성 배치, 모든 live 실패 자동 재시도 금지. live 수동 재시도/확정 취소 이력·새 asset ID 매핑/다중 배치 보관 UX 미구현 |
| UI | 기존 Mock UI 보존, 상태 안내만 추가 | **실계정 UI 연결·신규 계정/통화 UI·최종 실거래 승인 버튼 연결 미완성.** 일반 앱에는 live workspace를 주입하지 않음 |

### 두 가지 별도 위험

1. **계정 제재/정책 위험:** [Subscriber Agreement §4](https://store.steampowered.com/subscriber_agreement/)는 허가되지 않은 자동화에 제한을 둔다. 사용자의 승인/직렬화/3초 지연/공식 앱 확인은 Valve의 허가나 제재 면책이 아니다. 비공식 경로는 변경·차단·판매 제한·계정 제재 가능성이 있고 위험0을 보장하지 않는다. [Web API 약관](https://steamcommunity.com/dev/apiterms)의 일반 호출 상한을 장터 웹 인터페이스 안전량으로 사용하지 않는다.
2. **인증 탈취 위험:** 자체 QR 세션만 허용, 비밀번호/maFile/shared_secret/identity_secret 입력 없음, 기존 cookie 파일/클라이언트 세션 접근 없음. HTTPS endpoint host/path 고정, redirect 거절, proxy 사용 안 함, credential logs/telemetry/persistence 없음, third-party QR 이미지 서비스 없음. 다만 감염된 OS/의존성/메모리 노출·피싱 위험을 없앤다는 주장은 하지 않는다. 이후 token 저장이 필요하면 Windows current-user 암호화/별도 감사 선행.

403/429/401/redirect/HTML challenge/네트워크 불명/계약 변경은 연결 인스턴스에서 latch 중단한다. 자동 retry·captcha 우회·프록시 회전·사람처럼 위장하는 요청·백그라운드 로그인/판매는 없다. 재시작해도 디스크의 Submitting/Unknown을 자동 재전송하지 않는다.

### 향후 활성화 절차 (현재 일반 앱에는 활성화 UI 없음)

1. 알려진 제한과 제재/인증 위험을 확인하고 지원 통화/게임·확정 취소 대조·QR 화면·세션 경계를 보완한다. 정책 확인과 실제 계정 시험 승인을 별도 기록한다. 현재는 LIVE_READY가 아니다.
2. 개발자 composition `SteamExperimentalWorkspace`를 **Demo와 다른 앱 전용 디렉터리**로 생성한다. 생성/시작만으로 네트워크를 쓰지 않는다. 임의 DLL/스크립트를 Steam에 주입하지 않는다.
3. 사용자가 현 세션에서 `Access.Acknowledge(unofficialAutomationRisk:true, credentialRisk:true, reads:true, trading:false)`로 위험별 동의한 경우에만 `Authentication.BeginAsync`를 호출할 수 있다. challenge를 로컬 QR 렌더러에 연결해야 하며 외부 변환 서비스에 전송하지 않는다. `CheckAsync`는 명시적 확인만, 자동 폴링 없음.
4. 사용자가 공식 Steam 앱으로 로그인 승인 후 읽기만 검토한다. 쓰기는 별도의 명시적 trading 옵트인과 최종 **전체 배치 승인**이 모두 필요하다. 설정 파일/앱 재시작으로 동의를 자동 복원하지 않는다. 이 문서의 예시는 개발 계약이며 이번 세션에서 실행하지 않았다.
5. exact fee/현재 inventory/호가가 맞는 불변 snapshot을 `Batches.ExecuteAsync`에 한 번 넘긴다. 임의 직접 Submit 호출·원화 UI 값 재해석을 하지 않는다. 등록 ID 부재/Unknown/취소 미확정이면 후속 재가격을 차단하고 근거를 먼저 확보한다.
6. `Dispose`/동의 철회로 세션을 종료한다. command journal은 지우지 않는다. 로그인 token을 persistence 설정이나 Git에 저장하는 옵션은 없다.

## 3차 조사 기록 (역사적 상태)

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

CSlytics 사이트가 연결한 실제 저장소 [Xpar65/cslytics-desktop](https://github.com/Xpar65/cslytics-desktop)의 README/AGPL-3.0 표기도 추가 확인했다. Electron UI, engine/ports/Steam adapter 분리를 소개하며 로그인 토큰의 로컬 암호화 저장과 휴대폰 확인을 설명한다. 이는 해당 프로젝트의 설명이며 Valve 허가 확인이 아니다. MarketCrate는 그 인증 저장 방식·코드·자산을 도입하지 않는다. 응답과 사후 관측을 구분하는 구조만 비교했다.

## 향후 안전한 개발 계획과 해제 조건

1. **현재:** 로컬 합성 데이터·SQLite·Mock 제출/취소/재시도/재가격·불명 상태 대조를 검증한다. 기능 상태는 Mock/Unavailable/IntegrationBlocked로 표시한다.
2. **신원 확인 후보:** 별도 승인 시 공식 OpenID만 사용하고 state/nonce·리턴 URL 검증·SteamID 최소 저장을 설계한다. 신원 확인 성공을 장터 연결 성공으로 표시하지 않는다.
3. **읽기 후보:** 지원할 게임·필드별 공식 API/권한/이용 조건·원천/통화/시각을 문서화하고, 허용된 게임부터 제한된 읽기 어댑터를 검토한다. 허용 미확인 보관함/시세는 Mock/Unavailable 유지. 최저 매도가·median을 VWAP로 둔갑시키지 않는다.
4. **쓰기 해제 조건:** Valve의 허용 근거와 실제 계정별 제출/취소/조회 권한, 안전한 인증 위임 방식, 호출 제한, 요청 중복/불명 대조 수단을 확보한다. 현재 조건을 충족하지 않는다. publisher 한정 경로가 필요하면 이 프로젝트의 여러 타사 게임 관리 범위를 다시 평가한다.
5. 조건 확보 후에도 실연동 옵트인과 별도 실환경 테스트 승인이 필요하다. 최종 검토 승인 1회→전체 불변 배치 제출→항목별 결과→공식 모바일 수동 확인→서버 게시 재확인을 검증한다. 허용 근거가 없으면 제품 실등록 목표는 계속 BLOCKED다.
