# Steam 연동 가능성 조사 —0.20.0 (2026-10-29 KST)

## 0.20 공개 구현과 제품 경로 재검토

SSIM28a503f MarketService.ts의 실제 MassSellJob 단계/건수·deferred/failed 분리와 취소 경로를 재확인했다. SteamHelper/CSlytics 공개 화면도 직접 열람해 목록·선택 요약·가격 검토·전체 승인 구조를 반영했다. 소스 직접 복사/새 런타임·라이브러리 도입0, AGPL 코드 도입0, 기존 MIT 고지는 유지한다.

node-steamcommunity fa8d897 components/users.js의 getUserInventoryContents는 more_items/last_assetid 재귀와 전체 count 전달을 사용한다. 공식 economy_v2.js는 total_inventory_count와 로드된 슬롯 수가 다를 수 있음을 설명하고, marketable 필터 등을 고려해 실제 표시 페이지 수를 보정한다. 따라서 total 숫자만으로 빈 응답·완전성을 임의 확정하거나 모든 경우 강제 동등성을 새로 요구하지 않았다. 기존 명시적 빈 응답/커서·중복/설명 매칭·페이지 제한을 유지한다.

취소는 removelisting의 listing ID/세션 경로를 재사용하며 취소에 불필요한 fee/전체 inventory 의존을 취소 전용 모드에서 제거했다. 반환 근거가 없으면 AvailableBefore=-1·Unknown을 보존한다. 실제 서버의 읽기·수수료·스택·취소 반환·등록 성공은 이번에도 미검증이며, 다른 프로젝트 동작을 Valve 허가의 근거로 사용하지 않는다.


## 0.19.0 추가 — 외부 등록의 새 자산 재판매

SteamMarketHelper의 취소→보관함 재조회→같은 상품의 새 자산 ID 선택 방식을 실제 서비스에 연결했다. 전체 취소 승인 후 서버 접수·목록 부재를 확인하고, 실제 보관함·가용 수량·수수료·공유 호가로 새 대상을 제안한다. 원래 아이템의 반환 확정은 아니며 다른 기존 보유분일 수 있음을 표시한다. 새 실제 자산 ID·수량·가격을 전체 검토해 판매등록 한 번 승인하면 기존 Coordinator가 배치 처리한다. 항목별 추가 판매 클릭은 없다. SQLite 취소 계획/새 배치 연결을 보존하고 불명 취소·원래 예약은 추측으로 해제하지 않는다. 최근 미제출 계획은 사용자 요청 시 재조회하며 자동 거래하지 않는다. 개별 수량1, 최대100등록/10상품만 지원한다.

추가 합성6개/비표시WPF1개: 변경ID·재시작 후 새 승인, 취소만으로 판매0, Unknown 재전송0, 전체 수량 부족·호가 변경 차단, 계획 불변·OperationId/비밀정보 보호. 최종 기능305/305·WPF61/61·배포19/19 PASS, 경고/오류0. 취소/새 대상 검토125%·200% RTB를 직접 검토했다. 실제 계정/수수료·판매/물리 입력은 미검증이다. 상세 호출 경로·보존/재시도 제약은 [판매 실행 계약](SALE-EXECUTION-SPEC.md)의 0.19 추가 절을 따른다.


## 0.19.0 유사 프로젝트 재가격 조사

SSIM MarketService.ts(28a503f, Apache-2.0)의 취소 큐/계정별 결과와 CSlytics market.ts(a5c094b, AGPL-3.0)의 SUBMITTED/DEFINITIVELY_REJECTED/AMBIGUOUS 구분을 실제 코드로 비교했다(정확한 링크는 REFERENCES). 자동 승인·반복 전송·다계정 정책/AGPL 코드는 도입하지 않았다. 이 파일들에서 취소 후 새 자산 ID를 원래 등록에 유일하게 연결하는 서버 증거는 확인하지 못했다. 검증되지 않은 인과관계의 확정 근거가 되지 않는다. MarketCrate는 취소 전에 원등록 저널/대상·금액·호가 전체를 검증하고, 반환 증거가 없는 외부 등록은 취소 이전에 재가격 차단 이유를 표시한다. 사용자가 요청한 외부 판매목록 전체 재등록은 아직 완성되지 않았으며, 별도의 새 대상 선택/새 최종 승인 또는 확인 가능한 서버 반환 계약이 필요하다.

2026-10-29 [SteamMarketHelper 실제 MIT 소스 e186d51](https://github.com/federicogiorgi/SteamMarketHelper/blob/e186d51efb742ddfb527d2ccb248c64fcee9c976/steam-market-helper.user.js)의 act(relist)를 확인했다. 취소→기다림→보관함 재조회→market_hash_name으로 새 자산을 골라 재판매하며, 코드도 취소 후 ID가 바뀐다고 설명한다. 상품별 조회/큐/진행/순차 관리 구조는 참고하고 기존 MarketCrate Planner/Executor에 연결했다. **같은 이름이면 교환 가능한 자산으로 취급하는 반환 선택은 기존 불변 대상/인과·Unknown 조건과 충돌하여 도입하지 않았다.** 소스 복사/라이브러리 추가/AGPL 도입0, 기존 라이선스 고지 보존.

따라서 외부 기존 등록/변경ID·스택 자동 재가격은 여전히 BLOCKED/PARTIAL이며 실제 서버 근거 없이 완료라고 하지 않는다. [node-steamcommunity 문서](https://github.com/DoctorMcKay/node-steamcommunity/wiki/SteamCommunity)도 Market 지원 앱 목록과 보유 게임 목록이 다름을 구분하는 참고 자료이며, 소유 게임 추출이나 무차별 앱 탐색은 구현하지 않는다. 수수료 실제 계정 모드 검증은 기존 상태를 유지한다. Codex 실계정 요청/판매/취소/Guard0.


## 17차 / 0.18.0 — 수수료 API 호출 조건 수정 (2026-10-29 KST)

**확인된 코드 결함:** 기존 SteamFees.FetchAsync는 모드와 무관하게 ajaxfeestructure를 호출했다. 공식 [marketselldialog.js](https://community.fastly.steamstatic.com/public/javascript/applications/community/marketselldialog.js)의 aN은 `!newFeeRates`이면 wallet_fee_percent와 appContext.market_pubfee_rate(없으면 지갑 publisher 기본값)를 사용하고, true일 때만 해당 API를 GET한다. [main.js](https://community.fastly.steamstatic.com/public/javascript/applications/community/main.js)의 QN/$N은 `data-featuretarget=marketselldialog` 요소의 data-props JSON을 컴포넌트에 전달한다. flag 생략은 **실제 유효한 props 객체 안에서만** 공식 !flag 분기로 해석한다. 단순 변수 검색/HTML 실패 기반 추정은 하지 않는다.

사용자0.17의 HTTP200 HtmlMarket은 JSON 계약 실패 증거다. CAPTCHA/로그아웃 또는 실제 계정의 legacy 적용 증거는 아니다. 해당 계정의 원본 HTML/모드 props는 확보하지 않았다. 모드 조건 누락은 확인했지만, 이것이 사용자의 HTML 응답을 모두 설명한다고 단정하지 않는다.

| 모드 | 필요한 관측과 동작 |
|---|---|
| VerifiedLegacy | 현재 세션의 유효한 sell props(false/생략), Market/props 지갑 일치, 명시적 최소/증분, 유효한 구형 5필드, 해당 앱 context 존재. 실제 app publisher override(0 포함), 없으면 서버 지갑 기본값을 사용. API 호출0 |
| VerifiedNew | 명시적 true와 지갑 일치 후에만 수수료 API. strict steam_fee/publisher_fee/rate/cap JSON 검증 성공이 있어야 견적 반환 |
| Unverified | props/앱 증거 누락, 잘못된/중복 모드, 지갑 변경. 견적 반환/전송 없음 |
| Unsupported | 최소/증분 누락, 현재 공식 계산과 충돌하는 역사적 base/minimum/활성 플래그. 임의 옛 공식 적용 없음 |
| Restricted | 로그아웃/권한·403/429/Challenge. HTTP 안전 중단 규칙을 유지하고 우회/재시도 없음 |

정상 FeesAsync→FetchResolvedAsync는 Market1회, 필요 시 **자기 계정의 정확한 /profiles/{account}/inventory/** HTML1회(계정 대조), new일 때 API1회를 직렬 조회한다. 전체 게임/자산을 무차별 탐색하지 않는다. URI·메서드·응답 크기·리다이렉트 제한과3초 요청 간격을 유지한다. 별도 API HTML 실패 후 legacy fallback은 없다. 지갑 통화만 독립 조회할 수 있으며 수수료 실패가 인증/지갑 관측까지 지우지 않는다.

### 계산 근거와 제한

[현재 economy_common.js](https://community.fastly.steamstatic.com/public/javascript/economy_common.js)의 CalculateFee/ToValidMarketPrice/GetTotalWithFees와 현대 sell dialog의 EM/wM/SM 경로는 각 비율×판매자 wire 금액을 먼저 절사하고 최소 금액·통화 증분의 half-up으로 맞춘다. API cap은 그 뒤 적용한다. publisher0은 게임 수수료0이다. [economy_v2.js](https://community.fastly.steamstatic.com/public/javascript/economy_v2.js)의 GetPublisherFeeRate도 앱 context를 지갑 기본값보다 우선한다. 현대 false 분기는 wallet_fee_base를 소비하지 않으므로 base≠0 등의 역사적 프로필은 **Unsupported**, 과거 공식을 강제 적용하지 않는다. 구형 필드 존재만으로 계산식을 결정하지 않는다.

모든 계산은 정수/rational이며 checked overflow는 전송 차단이다. Money.Minor는 KRW **원**, USD **센트**다. Steam의 JS GetPriceValueAsInt는 입력 표시 금액×100 wire를 만든다. KRW 표시1원과 wire100을 구분하고 fractional won은 거부한다. 지갑의 최소·증분은 반드시 실제 명시값을 검증하며 1/100원으로 UI 표시하거나 USD 기본 최소값을 KRW에 대입하지 않는다. 역산은 단조 이진 탐색으로 도달 가능한 구매자 가격만 확정하고 gap은 정확 모드에서 거부한다. 견적용 하한은 명시적으로만 사용한다.

FeeFor는 자산의 실제 publisher 비율과 앱 schedule이 다르거나 누락되면 검토를 차단한다. 제출/취소 후 재가격/새 재가격 승인까지 모두 resolver를 다시 호출한다. 이전에는 재가격3곳에 무조건 API 경로가 남아 있었으며 이번에 제거했다. 최종 요청은 여전히 계정·자산·가격·수량·호가 지문·최신 수수료와 일치해야 한다.

첫 판매의 순환 의존성은 '같은 게임의 과거 등록'을 필수로 요구하는 대신 위 공식 모드·서버 지갑/앱/품목 증거를 필수로 검증하여 해소했다. 기존 등록은 있으면 대조하며 충돌은 계속 차단한다. 사용자 수기 비교나 위험 체크박스는 이 조건을 대신하지 못한다. 스택 지원/Unknown/반환 인과관계/기본 OFF/세션별 승인 보호는 그대로다.

### 공개 구현 비교와 라이선스

SteamMarketHelper e186d51(MIT)의 과거 CalculateAmountToSendForDesiredReceivedAmount/base·round/floor fallback은 현재 공식 false 분기와 다르다. 따라서 그대로 복사하지 않았다. node-steamcommunity fa8d897(MIT) components/market.js와 SSIM28a503f(Apache2) MarketPricing.ts를 재확인했으며 현재 계정의 newFeeRates 증거로 사용할 수 없다. 이번 수수료 소스 복사/새 런타임/AGPL 도입0. 기존 MIT steam-session 이식 고지를 보존한다.

공식 공개 파일을 계정 인증 없이 다시 받아 확인했다(브라우저 도구는 접근 실패, .NET 자동 압축 해제 GET으로 성공). SHA256: sell dialog CB84BB8A9B2970F71D277626A0B4E922B47CCE90BF437F704CF8F5100FA66726; economy_common 84429D06A9EF8C250AC567F9FEDC9FB155D23CEB7442C92A19B815840C102784; economy_v2 A3BBDF0A135524102CD055FD89D37EB3FD672FAE20BFBA0ABC700648DCB52512; main 7045DB3B8FBC9FBAD09A6C1234B36055379873BA41BF93545649020A8355FAED. 원본은 ignored runtime/public-contract에만 있으며 배포하지 않는다.

실서버 모드 props/새API JSON/공식 UI 실제3구간 대조·KRW 등록/취소/스택은 아직 NOT VERIFIED. REAL_FEE_VERIFIED·LIVE_SELL_VERIFIED·RELEASE_READY를 부여하지 않는다. 비공식 자동화의 정책/계정 제재 위험은 남는다.


## 16차 / 0.17.0 — 실제 소스 대조와 로그인 복원

아래 커밋의 실제 소스와 제공된 테스트를 읽었다(2026-10-29 KST). README의 동작 주장과 MarketCrate 실계정 성공은 구분한다. 기존0.16.1 수수료 HTML의 실제 분류/newFeeRates 적용 조건은 여전히 미확정이며, 이번 인증 복원으로 수수료 계약까지 해결됐다고 판단하지 않는다.

| 프로젝트·고정 커밋 | 읽은 실제 코드/테스트와 비교 | 채택/제외 |
|---|---|---|
| [node-steam-session](https://github.com/DoctorMcKay/node-steam-session/tree/3ac0f34fd964b3f886ba18ef4824ac43c942e030) MIT ©2022 Alex Corn | src/LoginSession.ts(refreshToken setter/getWebCookies/renewRefreshToken), AuthenticationClient.ts, auth proto, test/LoginSession.test.ts·LoginSession_live.test.ts. WebBrowser audience web/derive와 계정 일치, finalizelogin→settoken 쿠키 발급. WebBrowser의 GenerateAccessTokenForApp는 AccessDenied 한계가 명시됨 | **MIT 로직을 C#으로 제한 이식**, 원본 커밋/저작권/전문 포함. Community만 허용, 크기/만료/계정/쿠키 검증 추가. debug 원문·5회 재시도·전체도메인 병렬전송·모바일 승인 미도입. Node 런타임 미추가 |
| [node-steam-user](https://github.com/DoctorMcKay/node-steam-user/tree/02818bd08090b58ff54f1d9ce0073319b8e1eef7) MIT | components/09-logon.js의 issuer/client audience/계정 검사,07-web.js의 세션발급. CM/SteamClient audience와 WebBrowser의 차이를 확인. 트리에서 test/tests 폴더 없음 | SteamClient 토큰/CM·프로세스 세션을 WebBrowser에 사용하지 않음. 코드 복사0 |
| [node-steamcommunity](https://github.com/DoctorMcKay/node-steamcommunity/tree/fa8d89759077152b0c38175f154e03d0eacd2d1e) MIT | components/market.js의 Market HTML/앱 목록/세션 요청. 전체 계정 게임 발견이나 최신 Beta fee API 계약을 보증하지 않음. 트리에서 test/tests 폴더 없음 | 기존 URI/세션/읽기 경계와 비교. 광범위 HTML/구 API를 최신 실제 데이터 성공으로 폴백하지 않음. 코드 복사0 |
| [SteamMarketHelper](https://github.com/federicogiorgi/SteamMarketHelper/tree/e186d51efb742ddfb527d2ccb248c64fcee9c976) MIT | userscript의 wallet/market_fee·통화별 round/floor, 정수 호가/공유잔량/판매자 price·sellitem/removelisting, tests/run.js의 가격 gap/파싱. 로그인된 브라우저 페이지 자체 세션을 쓰는 구조 | 정수·양방향 금액·수수료 gap을 기존 순수 엔진과 대조. 페이지주입/기본율/429 재시도·자동 재가격은 미채택, 코드 복사0 |
| [SSIM](https://github.com/Santerxyz/SSIM/tree/28a503fa79869f3233814d0deaa906a8b3f66621) Apache-2.0 | SessionManager/TokenStore/sessionHealth, MarketPricing/MarketListings/MarketService, tokenStoreDegraded.test.ts·marketFeeFloor.test.ts. 누락/손상 분리, 보존 상태, 통화 최소값 차이·수수료 전송 검사를 확인 | 손상파일을 빈 정상 파일로 덮어쓰지 않는 원칙과 저장 실패 분리를 독립 적용. 평문 refresh_tokens.json/maFile·비밀번호·Guard·프록시/핑거프린트·제한회피·재시도는 도입하지 않음. 코드 복사0 |
| [CSlytics](https://github.com/Xpar65/cslytics-desktop/tree/a5c094bc44d12dc0bc99e84e6643fe8a2cc113a8) AGPL-3.0 | adapters-steam/auth.ts·기존 market.ts, ports/fees.ts 비교. 계정/금고/ClientSessionPool 경계, 접수/불명·게시 구분. 요청한 tests/unit/marketplace-fees.test.ts 경로는404, 테스트가 있다고 꾸미지 않음 | 구조만 참고. 직접 코드 복사/배포 라이선스 변경0. CS2/cent 고정표를 KRW 서버 수수료로 쓰지 않음 |

공식 판매 dialog의 newFeeRates 조건부 GET과 구형 지갑 비율은 이전 조사와 대조했다. UI/JSON 스키마가 바뀌어도 실제 fee JSON이 없으면 기본5%/10%·USD floor·중앙값을 검증된 KRW 가격으로 바꾸지 않는다. 가용자산/marketable·지갑/게임fee·호가·Unknown/반환 인과관계가 불충분하면 기존 판매 차단을 유지한다. 쿠키를 발급받았다는 이유만으로 모든 조회 권한/거래 허용을 부여하지 않는다.

IMPLEMENTED: 명시적 저장 동의→DPAPI CurrentUser/사설 ACL→실행당 단일 WebBrowser Community 세션 복원→삭제/실패 정지와 UI, 최소 응답 cookie 계약 보완. CONTRACT_VERIFIED: 공개 구현·프로토콜과 합성 HTTP, 실제 Windows 자체 DPAPI/ACL. LIVE_VERIFIED: 실제 저장 토큰 재사용/회전·수수료·등록/취소는 없음. 자동 Refresh Token 회전/서버 철회는 미지원이며 만료 후 새 QR이 필요하다. 기술적 구현 사례는 Valve 허용 근거가 아니다.

## 0.16.1 — 수수료 HTML 계약 조사 (2026-10-29 KST)

사용자 0.16.0 진단의 관측 범위: QR 메모리 세션, 목록 첫8건, CS2 서버총0건, eligibilitycheck→Market 복귀 후 Wallet200/WalletObject 성공. Fees200 뒤 HTML 탐지로 Challenge가 기록됐다. 당시 Content-Type/HTML 종류는 저장하지 않았으므로 **실제 CAPTCHA·인증 만료·구형/신형 요율 적용 여부는 미확정**이다. 원문 ZIP/HTTP/계정 정보는 Git에 넣지 않았다.

공식 [marketselldialog.js](https://community.fastly.steamstatic.com/public/javascript/applications/community/marketselldialog.js)를 무인증 GET/압축 해제로 재확인했다. SHA256 `CB84BB8A9B2970F71D277626A0B4E922B47CCE90BF437F704CF8F5100FA66726`. `newFeeRates`가 참인 경우만 GET `/market/ajaxfeestructure/?appid=...`, credentials include, `steam_fee`/`publisher_fee` rate/cap을 소비한다. 거짓인 경우 지갑 wallet_fee_percent/게임 market_pubfee_rate 또는 지갑 기본 publisher 비율을 사용한다. 공식 코드의 fallback 0.05/0.1/default unit1을 사용자 계정의 검증된 값으로 채택하지 않았다. MarketCrate의 기존 URL·GET은 맞지만 **모든 계정에 같은 API가 JSON을 준다는 가정은 근거 부족**이었다.

같은 수수료 URL을 쿠키·인증·자동 redirect 없이 공개 GET했을 때200/text-html/장터 HTML과 로그아웃 플래그를 확인했다. CAPTCHA 위젯은 관측되지 않았다. 이는 익명 공개 응답의 근거이며 사용자 HTML이 같았다는 증명은 아니다. JSON Accept 추가는 기대 형식의 명시이며 서버 지원/권한을 보장하지 않는다. 로그인·보안 페이지를 통과시키거나 요청을 재시도하지 않는다.

비공식 프로젝트 실제 소스도 대조했다(참고만, 코드/의존성/자산 복사0).

| 출처 | 확인한 구현과 채택/미채택 이유 | 라이선스/한계 |
|---|---|---|
| [CSlytics market.ts](https://github.com/Xpar65/cslytics-desktop/blob/a5c094bc44d12dc0bc99e84e6643fe8a2cc113a8/packages/adapters-steam/src/market.ts), [fees.ts](https://github.com/Xpar65/cslytics-desktop/blob/a5c094bc44d12dc0bc99e84e6643fe8a2cc113a8/packages/ports/src/fees.ts) | Wallet HTML 관측/접수와 결과불명 구분, 정수 금액 경계를 참고. fees 표의 CS2 500/1000bp·1cent·floor는 KRW/서버 적용 모드 증거가 아니므로 이식하지 않음. 광범위 redirect·원문 보관도 미채택 | AGPL-3.0, 구현 미복사 |
| [SteamMarketHelper](https://github.com/federicogiorgi/SteamMarketHelper/blob/main/steam-market-helper.user.js), [LICENSE](https://github.com/federicogiorgi/SteamMarketHelper/blob/main/LICENSE) | 공식 페이지 계산 함수를 우선 사용, 지갑/아이템별 publisher 값과 통화별 round/floor, 도달 불가능 구매자 가격 고려. 계정 필드 없는 경우의 기본값/0수수료·페이지 주입·자동 재시도는 미채택 | MIT 확인. 브라우저 내부 사용자스크립트 구조를 WPF 세션 방식과 동일시하지 않음 |
| [steam-bulk-seller fees.ts](https://github.com/todd-elvers/steam-bulk-seller/blob/main/shared/fees.ts), [sell.ts](https://github.com/todd-elvers/steam-bulk-seller/blob/main/server/src/steam/sell.ts) | 정수 cents와 판매자 수령액 전송 참고. 소스가 KRW/JPY round 미지원을 명시하므로 기본 USD식 계산을 KRW에 사용하지 않음 | LICENSE 경로404, 이전 조사도 미확인. 직접 재사용 금지 유지 |
| [steampy utils.py](https://github.com/bukson/steampy/blob/master/steampy/utils.py) | 개별 fee floor·최소1cent·역산 한계 비교. 고정 steam 비율과 2-decimal 기준은 KRW의 실제 서버 증거가 아님 | LICENSE 단일 경로404로 이번 재확인 실패, 코드 도입 없음 |

SteamFees는 기존 wallet_fee/percent/minimum/base/publisher-default의 존재·형식만 별도 관측한다. 누락/잘못된 구형 필드는 지갑 통화 성공을 지우지 않으며, 구형 필드가 완전해도 새 요율 모드·KRW 단위·게임별 실제 적용을 추정해 자동 판매용 SteamFeeSchedule로 변환하지 않는다. 이 경우 새 수수료 API 지원 여부/실계정 계약 확인이 계속 차단 요인이다. 정확한 최소금액/증분/통화/비율·cap·품목 비율·실제 등록 수수료의 사전 대조를 통과하지 못하면 전송 불가다.

'구현 사례'는 Valve 허가 근거가 아니다. 새 합성 검증은 CONTRACT_VERIFIED(합성)이며 실계정 수수료 성공/거래 LIVE_VERIFIED는 아직 없다.

## 0.16.0 자격 확인 근거

[Steam 공식 자격 확인](https://steamcommunity.com/market/eligibilitycheck/?goto=%2Fmarket%2F)을 쿠키/인증 없이 수동 redirect 금지 HTTP로 확인했다.302의 목적지는 같은 host /market/·추가query 없음이었다. [공개 steam-user 클라이언트 소스](https://docs.rs/crate/steam-user/latest/source/src/client.rs)도 eligibilitycheck를 포함한 수동 이동 처리를 구현한다. 참고만 했으며 코드/의존성을 복사하지 않았다.

MarketCrate는 광범위10-hop/host 허용을 채택하지 않는다. 정확한 same-origin eligibility 경로와 고정 Market goto/복귀만 처리하고 canonical1회 포함 최대3 hop, 서버 거절/인증요구/보안검사 미완료/429는 중단한다. 서버 체크를 건너뛰거나 cookie를 생성/변조하는 우회가 아니다.

사용자 새 ZIP의 Market하위/추가query 분류는 이 경로와 부합하지만 정확한 Location은 여전히 없으므로 실계정 원인 및 성공은 UNVERIFIED이다. Release의 실제 전용 구성과 계약/실거래 검증은 별개이며 제재 위험·수수료/스택의 미확인 계약은 유지된다.

## 0.15.1 Wallet302 증거 갱신

사용자제공진단0.14/0.15 모두HTTP302/OtherRedirect·HTML파싱전중단, 정확한Location은미기록이다. 로그인실패나wallet미설정을입증하지않는다. [공식 /market](https://steamcommunity.com/market)과 [/market/](https://steamcommunity.com/market/)를쿠키/리다이렉트없이GET하여각200을확인했다. 공개wallet marker부재는익명응답의관측일뿐사용자원인이아니다. 두공식Marketroot 간단일GET과기존고정언어query만지원하며다른이동은분류후차단한다. 실계정의302가이경로였는지와지갑금액계약은여전히미검증이다.

## 15차 공개 근거와 정책 구분 (2026-10-29 KST)

[공식 Market](https://steamcommunity.com/market/), [market.js](https://community.fastly.steamstatic.com/public/javascript/market.js), [marketselldialog.js](https://community.fastly.steamstatic.com/public/javascript/applications/community/marketselldialog.js)를 무인증 GET. JS gzip을 해제해 확인한 SHA는14차와 동일(AC938119E82C7D4C8D0F7843EF168483EFA4AF3E45DC987C068F340BB5AF72EC / CB84BB8A9B2970F71D277626A0B4E922B47CCE90BF437F704CF8F5100FA66726). 공개 footer 언어query와 Market주소, g_steamID=false·wallet metadata 소비를 확인했다. 공개 modern fee 최소/증분 default는 실계정KRW 계약으로 채택하지 않았다.

[공개 node-steamcommunity 구현](https://github.com/DoctorMcKay/node-steamcommunity/blob/master/components/market.js)의 Market GET/서버세션 사용도 비교했다. 코드 복제/의존성 도입 없음. 기술적 구현사례는 Valve의 자동화 허가가 아니다. 사용자 Wallet302의 Location/인증된 HTML은 미확보이며 익명200/공개JS로 원인을 단정하지 않는다. 허용하는 정규/언어 same-origin 이동 외의302는 차단한다.

이번 명시적 사용자 요청으로 ‘허가확인 후에만 활성화’라는 기존 제품정책에 세션별 비공식 실험모드 예외를 추가했다. [Steam 약관](https://store.steampowered.com/subscriber_agreement/)의 허용을 확인한 것은 아니며 제재위험은 남는다. UI 위험동의뿐 아니라 계정일치·실제서버 metadata·기존등록fee 증거·자산 조건이 모두 필요하다. minimum/increment 없는계정·fee증거 없는첫등록·불명스택은 여전히차단한다. KRW/USD 코드는 합성검증, 인증된 서버금액·등록/취소/Guard/반환은 UNVERIFIED. LIVE_VERIFIED 없음.

## 14차 공개 계약 근거 (2026-10-29 KST)

인증/쿠키 없이 공식 [Market](https://steamcommunity.com/market/), [market.js](https://community.fastly.steamstatic.com/public/javascript/market.js), [marketselldialog.js](https://community.fastly.steamstatic.com/public/javascript/applications/community/marketselldialog.js)를 GET 확인했다. g_rgWalletInfo.wallet_currency·wallet_fee_percent, 선택적 wallet_market_minimum/increment, 현대dialog fee_rate/fee_cap 및 추가필드가 관측됐다. JS default1을 검증된KRW wire계약으로 채택하지 않는다. currency-only/fee비율 관측과 strict 정수money계약을 분리한다.

market.js SHA256 AC938119E82C7D4C8D0F7843EF168483EFA4AF3E45DC987C068F340BB5AF72EC, marketselldialog.js CB84BB8A9B2970F71D277626A0B4E922B47CCE90BF437F704CF8F5100FA66726. 공개소스는 ignored research-stage14에서 계약참조만 했으며 코드복제 없음. 익명Market200에 wallet marker 없음/g_bLoggedIn 있음은 사용자인증실패의 증거가 아니다. 지갑미설정·장터권한·세션범위·구조변경 등 실제원인은 추가증거 필요.

사용자0.13 세션기록/목록첫8건만 부분실관측. CS2 0건·wallet/fee·KRW/스택·호가/이력/이미지·등록취소는 미확정. 전체 READ_ONLY_READY/LIVE 승격 없음. [약관](https://store.steampowered.com/subscriber_agreement/)상 비공식자동화 제재위험, IntegrationBlocked·수동Guard·Unknown/저널/중복방지 유지.

## 13차 기존 인증 활용 판정 (공개 자료 확인: 2026-10-28~29 KST)

| 방식 | 공식 근거와 판정 | MarketCrate 적용 |
|---|---|---|
| 기존 기본 브라우저 | 사용자가 공식 장터를 여는 경우 브라우저 내부 로그인 상태를 직접 활용할 수 있으나 재인증 필요 여부는 Steam 판단 | 기존 공식 장터 버튼 유지, 앱에 세션을 전달하지 않음 |
| OpenID | [User Authentication](https://partner.steamgames.com/doc/features/auth#website) 및 [Community 개발 문서](https://steamcommunity.com/dev): SteamID 신원 확인/계정 연결. 장터 쿠키·거래 세션 권한을 반환한다고 명시하지 않음 | 앱 OpenID 미구현. 신원만 얻기 위해 새 callback 서버/로그인 절차를 추가하지 않음 |
| OAuth | [공식 OAuth](https://partner.steamgames.com/doc/webapi_overview/oauth): Valve Client ID 필요, Cloud/Workshop 목적 등 권한 협의. 일반 장터용 session bridge 근거 없음 | Client ID/허가 없음, 타 앱 ID 차용·임의 OAuth 경로 생성 안 함 |
| Steamworks ticket | [공식 인증 문서](https://partner.steamgames.com/doc/features/auth): 게임/백엔드 신원·소유권 검증 | 타 앱 AppID·클라이언트 내부 세션을 이용한 장터 세션 획득으로 해석하지 않음 |
| 독립 QR | [공개 proto](https://github.com/SteamDatabase/Protobufs/blob/master/steam/steammessages_auth.steamclient.proto), [node-steam-session](https://github.com/DoctorMcKay/node-steam-session/blob/master/src/AuthenticationClient.ts)의 Begin/Poll/회전 필드 대조 | 기존 비공식 어댑터 유지, 코드 직접 도입 없음. 실제 서버/계정 검증·Valve 허용을 뜻하지 않음 |

확인한 공식 문서 범위에서 기존 브라우저/Steam 클라이언트 Community 인증 세션을 범용 데스크톱 앱으로 공식 전달하는 방법은 찾지 못했다. 이 부재를 모든 미래/비공개 방법의 불가능 증명으로 확대하지 않는다. 인증 파일/쿠키/메모리/Windows 자격 증명 추출·Web UI 주입은 구현하지 않았다. QR의 client_id는 세션 식별자이며 OAuth 앱 Client ID와 다르다.

자가검증은 새 인증을 시작하거나 권한을 만들지 않는다. 성공한 QR finalize/Community transfer 기록·메모리 세션 존재를 먼저 확인하고, 명시 허용된 endpoint 응답을 각각 검사한다. 세션 존재/개별 응답 파싱 성공은 전체 권한·시장 게시·실거래 계약 검증과 별개다. [Subscriber Agreement §4](https://store.steampowered.com/subscriber_agreement/) 자동화 조항을 재확인했으며 사용자 클릭·읽기 전용·호출 간격만으로 Valve 허용 또는 제재 위험 감소를 보장하지 않는다.

실계정 응답은 수집하지 않았으므로 새로 확인된 서버 계약 불일치 수정이라고 주장할 항목은 없다. Wallet HTML 마커 부재의 잘못된 인증 만료 추론, Secure/빈 쿠키 검증은 코드 논리와 합성 응답으로 수정했다. 실제 QR 발급/승인/finalize/transfer·지갑·수수료·보관함/장터 스키마는 여전히 사용자 직접 검증이 필요하며 READ_ONLY_READY/LIVE_READY 미부여.

## 11차 QR 계약 수정 근거 (2026-10-28 KST)

[Valve 공개 API 메타데이터](https://api.steampowered.com/ISteamWebAPIUtil/GetSupportedAPIList/v1/) GET에서 IAuthenticationService.BeginAuthSessionViaQR v1 POST의 device_friendly_name/string, platform_type/enum, device_details/message는 optional:false, website_id/string은 optional:true임을 확인했다. 인증 endpoint는 호출하지 않았다. 이 required HTTP 선언과 공개 protobuf의 optional 문법은 서로 다른 조건이다.

[공개 protobuf 정의](https://github.com/SteamDatabase/Protobufs/blob/master/steam/steammessages_auth.steamclient.proto)의 최상위1/2/3/4와 DeviceDetails 내부1/2를 구분했다. WebBrowser=2는 OS 값이 아니며 SteamClient=1/MobileApp=3과 다르다. 응답1client_id(uint64),2challenge_url(string),3request_id(bytes),4interval(float)을 JSON 표현에 맞춰 파싱한다. [공식 WebAPI 개요](https://partner.steamgames.com/doc/webapi_overview)의 UTF8 form POST/format 조건을 참고해 format=json을 별도 form 필드로 요청한다.

[node-steam-session AuthenticationClient](https://github.com/DoctorMcKay/node-steam-session/blob/master/src/AuthenticationClient.ts)와 [WebApiTransport](https://github.com/DoctorMcKay/node-steam-session/blob/master/src/transports/WebApiTransport.ts)는 QR Begin에서 device_details 위주로 전송하고 base64 protobuf form을 사용한다. [SteamKit Authentication](https://github.com/SteamRE/SteamKit/blob/master/SteamKit2/SteamKit2/Steam/Authentication/SteamAuthentication.cs)도 CM 경로에서 details/website를 사용한다. 이 사례만으로 HTTP 최상위 조건이 불필요하다고 단정하지 않았다. 어느 자료도 사용자 실패의 실응답 증거 또는 자동화 허가가 아니다. 소스 직접 복사·새 패키지 도입 없음.

확인된 기존 요청 불일치와 numeric JSON 파서 결함을 수정했지만, Valve 서버의 실제 직렬화·QR 발급·승인·finalize/transfer/cookie는 실계정 미검증이다. READ_ONLY_READY/LIVE_READY로 승격하지 않는다. IntegrationBlocked 및 비공식 자동화 제재 위험은 그대로다.

## 10차 실사용 연결 보완 (2026-10-28 KST)

실계정/API/인증/등록/취소/Guard는Codex가실행하지않았다. [공개auth proto](https://github.com/SteamDatabase/Protobufs/blob/master/steam/steammessages_auth.steamclient.proto)와[node-steam-session AuthenticationClient](https://github.com/DoctorMcKay/node-steam-session/blob/master/src/AuthenticationClient.ts)를재확인했다. optional new_client_id/new_challenge_url 전달근거가있어이미구현된auth회전에UI QR도연결했다. 패키지/타소스직접도입없음. 실JSON/토큰finalize/cookie/기간·만료의실계정계약은미검증.

[공식Market](https://steamcommunity.com/market/)은목록조회·편집·제거에로그인이필요함을표시한다. UI에고정공식Market URL을여는명시적사용자명령만추가했다. 기본브라우저쿠키를읽거나앱메모리세션을브라우저로전송하지않는다. 공식페이지사용이앱단일승인전체등록구현완료는아니다.

[Subscriber Agreement](https://store.steampowered.com/subscriber_agreement/) Automation영역을재조회했다. 공개소스에서기술적으로요청이가능하다는사실로비공식자동화허가를추론하지않고기본IntegrationBlocked를유지한다. [확인도움말](https://help.steampowered.com/en/faqs/view/2E6E-A02C-5581-8904)은도구상본문한줄이미지만반환해이번재확인으로치지않는다. 계정제재위험과인증정보보호위험은별개다.

독립목록/fee/보관함/호가/이력의UI→실제어댑터호출과합성예외는연결됐지만READ_ONLY_READY아님. 실제읽기계약을사용자가확인할최소절차는[READ-ONLY-VALIDATION](READ-ONLY-VALIDATION.md). 실제KRW서버fee/스택부분판매·반환ID인과·ID-less외부등록구분·Guard후게시의실응답은여전히필요하다. 쓰기는허용근거+사용자명시활성+실계정검증전미지원이다.

## 8차 공개 계약 근거 대조 (2026-10-28 KST)

실제 인증/판매 API 호출 없음. 공개 문서와 source GET만 수행했고 다운로드한 JS를 실행·복사하지 않았다. 실제 서버 응답 기반 계약/실계정 판정은 여전히 N이다. 실제 전체 일괄 등록이 최종 목표지만 IntegrationBlocked·KRW/stack/median capability 기본 제한은 유지한다.

- [SteamDatabase 공개 auth proto](https://github.com/SteamDatabase/Protobufs/blob/master/steam/steammessages_auth.steamclient.proto): QR Begin의 client_id/request_id/interval/challenge_url, Poll의 optional new_client_id/new_challenge_url/refresh_token 등을 확인. Valve 문서/실응답/허가가 아닌 공개 프로토콜 전사 자료다. 코드 도입 없음. [node-steam-session AuthenticationClient](https://github.com/DoctorMcKay/node-steam-session/blob/master/src/AuthenticationClient.ts)와 [MIT license](https://github.com/DoctorMcKay/node-steam-session/blob/master/LICENSE)를 직접 읽었다. device_details 기반 Begin과 갱신 필드 전달을 대조했다. 제3자 코드/의존성/인증·자동 승인 기능 도입 없음.
- 이 근거로 선택 URL의 빈 기본값을 유효 challenge 교체로 처리하지 않도록 수정. 새ID/URL 회전과 다음 요청ID, malformed/unknown schema/취소 정리 경로는 합성 HTTP로 검증한다. 실제 JSON 직렬화의 기본값 포함 정책/interval 만료/세션 finalize·transfer는 미검증이다.
- [공식 판매 bundle](https://community.fastly.steamstatic.com/public/javascript/applications/community/marketselldialog.js) 재조회 SHA256 `CB84BB8A9B2970F71D277626A0B4E922B47CCE90BF437F704CF8F5100FA66726`은 5차 확인 값과 같다. wallet_market_minimum/currency_increment, fee_rate/cap, appid/contextid/assetid/amount/price, requires_confirmation·needs_mobile_confirmation/listingid 소비 코드 재확인. bytes 일치가 서버 계약/정책 허용을 보증하지 않는다. 응답 flags 모호한 형식은 Unknown/중단하며 추측 coercion 안 함.

| 계약군 | 현재 근거 / 다음에 필요한 증거 |
|---|---|
| QR·세션 | 공개 proto/유지보수자 소스 + 합성; 별도 승인 후 최소 비밀제거 필드/상태 근거 필요 |
| inventory/descriptions/paging/images | 이전 공식 공개 JS/합성 매칭·페이지 계약; 실제 인증 응답·CDN bytes 미확보 |
| listings/ID/orderbook/history | 이전 소비 코드/합성; ID 의미·완전성·호가/버킷 통화/가격 역할·서버변경 미확인 |
| KRW/USD/fee | 공식 bundle 계산 규칙 확인; 실제 지갑 min/increment·게임fee·서버 계산 대조 필요 |
| stack/취소/반환 | amount 입력의 기술 근거만; 부분 판매/ID변경/수량합산의 인과 증거 없음, Unknown 유지 |
| submit/cancel/Guard | 공개 flags/합성; 실제 결과/승인 후 게시/모바일 장기대기·인증만료 후 대조 미검증 |

미래 계약 증거는 계정 연결 별도 승인 및 허용 경로 검토 후 최소 화이트리스트 필드만 정제해 담당 계약과 비교한다. 인증 HTTP 원문/토큰/QR/쿠키를 수집·Git·ZIP에 넣지 않는다. 동일 조건 외부등록·listingID 부재·반환ID 변경·스택 합산은 새 근거가 없어 보수적 Unknown/예약/중복 차단 유지. 실제 응답 없이 통과 capability를 켜지 않는다.

7차 추가 반환 제한: 원래 단일 수량이었어도 취소 후 returned.Stackable/수량 증가·기존 가용 수량이 있으면 proof로 확정하지 않는다. 동일 ID라도 현재 정확히1개/기존0개인 제한 경로만 합성 대조하며 나머지는 Unknown/예약 유지. 실제 nonstack/스택·ID 의미와 인과 증거는 여전히 미검증이다.

7차 추가 보호: 기간 중앙값의 구매자/판매자 결제액 역할과 통화 해석은 live 미검증이므로 `SteamTradingPolicy.MedianPriceContractSupported`가 별도로 필요하며 기본 false다. 사용자의 최종 클릭/정수 계산 성공으로 이를 true로 추정하지 않는다. 합성 positive 경로에서만 명시적으로 켜 검증했다.

## 7차 공개 자료 조사와 구현 범위 — 2026-10-28 KST

실계정 인증/보관함·장터 API/이미지 다운로드/판매·취소·Guard는 실행하지 않았다. 공개 Market HTML과 연결된 공식 JS만 source로 읽었다(실행/코드 복사 없음). 네트워크 DNS sandbox 제한은 공개 문서 GET에만 승인된 경로로 재시도했다. 제재/허용 근거는 새로 확보하지 않았고 기본 거래 차단이다.

- [공개 Market](https://steamcommunity.com/market/)의 이미지 경로 및 [economy.js](https://community.fastly.steamstatic.com/public/javascript/economy.js) ImageURL/icon_url/icon_url_large 사용과 Median Sale Prices 표시를 확인. `community.fastly.steamstatic.com/economy/image/<opaque-token>/128fx128f`만 구성한다. 공식 웹의 사용 사례는 안정적인 API/자동화 허가 보장이 아니다. Steam 원본 이미지 bytes/실계정 메타데이터는 이번에 검증 안 함. 다른 CDN/임의 URL fallback 없음.
- [IPlayerService/GetOwnedGames](https://partner.steamgames.com/doc/webapi/IPlayerService#GetOwnedGames)는 key/노출 권한과 무료 게임 포함 조건이 있다. 계정 inventory context 전체성은 문서의 보장이 아니므로 카탈로그 전체 발견으로 쓰지 않는다. key 수집/해당 API 호출을 구현하지 않았다. 실제 조회한 inventory scope와 판매목록, 사용자 app/context 설정을 반영한다. 404는 해당 요청 경로 미지원이지 게임 영구 미지원 증거가 아니며 403은 비공개 원인 확정이 아니다.
- `pricehistory`는 기간 중앙값/거래량 자료로 취급한다. 정확한 체결 가격 목록이 없어 exact mean/VWAP는 차단한다. 대체값은 **버킷 중앙값의 서버 거래량 가중 하위 중앙값 추정**이며 실제 전체 체결 중앙값도 아니다. 현재 최고 구매호가 기반 예상가와 분리. 최저 판매호가는 현재 계약에 없음을 표시. 합성 형식/정수 계산/기간·digest 재검증만 확인했으며 실제 이력/거래량 단위/버킷 재집계는 미검증이다.
- KRW wire·반올림·fee profile/스택 capability는 이전 소스 기반 합성 범위 그대로. 누락 publisher fee/불일치/시간 만료/변경 history도 요청 전 차단. 스택 취소의 동일 ID/증가 수량은 인과 proof로 승격 안 함. ID 없는 요청·외부 동시 등록·ID remap은 여전히 Unknown.

## 6차 감사 보완 — 2026-10-28 KST (최신)

실계정/인증 응답/판매/취소/모바일 승인은 이번에도 실행하지 않았다. 코드가 존재하고 합성 handler를 통과한 것과 실제 계정 호환성은 구분한다. [75개 요구사항 추적](REQUIREMENTS-TRACEABILITY.md)에서 실제 응답 계약/실계정 검증은 모두 N이다.

- 새 허용 근거는 확보되지 않았다. 공개 [Steam Subscriber Agreement §4.C/D](https://store.steampowered.com/subscriber_agreement/) Automation/Enforcement 본문을 다시 읽었다. 비공식 자동화에 대한 일반적 허용 근거로 최종 클릭/수동 Guard를 해석하지 않고 기본 `IntegrationBlocked`를 유지한다. 계정 제재 위험과 인증정보 보호 위험은 별개이며 위험0을 보장하지 않는다.
- [Trade and Market Confirmations](https://help.steampowered.com/en/faqs/view/2E6E-A02C-5581-8904)는 열었으나 이번 웹 도구 응답은 이미지 한 줄로 본문을 재확인하지 못했다. 이전 확인 기록을 현재 재검증으로 승격하지 않는다. 새로운 비공식 API/실계정 응답 형식 변경은 이번에 live 시험하지 않았다.
- 최초 요구 감사로 이미지 field/전용 CDN provider/UI가 없고, 게임 scope가 3개 고정이며, 실제 거래 원자료 기반 VWAP는 제공하지 않음을 확인했다. 중간가격을 평균 거래가로 표시하지 않는다. 최신 KRW fee/price 단위는 기존 소스 기반 합성 계약일 뿐 인증된 서버 응답 비교가 아니다.
- 5차에서 미지원이었던 **stale 재가격의 새 승인 revision**을 구현했다. 취소 전 stale 검사, 확정 반환 후 새 가격 검토/새 의도/원자 supersede/원 취소 proof 재사용/후속 key 보존. 실험 정책이 차단된 일반 앱에서는 이 코드도 거래를 실행하지 않는다. Steam DB schema v3가 구 버전의 폐기 의도 재개를 거부한다.
- ID 없는 응답의 후보/외부 동시 판매/스택 수량 증가·다른 ID 반환은 인과 증거 부족으로 Unknown/예약 유지. 새 revision은 이 제한을 우회하지 않는다. live 확정 실패의 안전 재시도/지원되지 않는 통화/스택 amount를 임의 활성화하지 않는다.
- 순서: 허용 경로·사용자 별도 실계정 시험 승인 → 최소 지원 game/context/통화의 비밀 제거 계약 근거 → 읽기/세션/fee 계약 대조 → ID/취소/반환 인과 증거 설계 → 실거래 capability 검증과 사용자 활성화. 현재 허용된 공개 문서/합성 범위를 넘어 요청하지 않았다.

## 5차: UI 연결·통화·수량·대조 계약 (최신)

**실제 계정 로그인·보관함/지갑/수수료 API·판매·취소·모바일 승인은 시험하지 않았다.** 최신 공개 소스를 읽고 실제 어댑터에 합성 HTTP를 주입해 검사했다. 일반 앱에 읽기 연결 UI를 추가했지만 자동 시작은 없으며, 거래는 별도의 `SteamTradingPolicy.Blocked`가 기본이다. 위험 동의/최종 클릭이 Valve의 허가가 되지 않는다. 최종 실제 일괄 등록 제품 목표는 아직 실계정/정책 검증을 완료하지 못했다.

### 추가로 직접 확인한 공식 근거

- [공식 Market 페이지](https://steamcommunity.com/market/)가 연결한 [community/main.js](https://community.fastly.steamstatic.com/public/javascript/applications/community/main.js?contenthash=B3VTzWgT)와 동적으로 사용하는 [marketselldialog.js](https://community.fastly.steamstatic.com/public/javascript/applications/community/marketselldialog.js)를 다운로드해 **실행하지 않고 소스**를 확인했다. 조사일 2026-10-28 KST, 판매 번들 SHA256 `CB84BB8A9B2970F71D277626A0B4E922B47CCE90BF437F704CF8F5100FA66726`. 원본은 ignored runtime에만 있다.
- 현 판매 다이얼로그는 지갑의 `wallet_market_minimum`, `wallet_currency_increment`와 `market/ajaxfeestructure/?appid=...`에서 읽는 `steam_fee`/`publisher_fee`의 `fee_rate`·`fee_cap`을 사용한다. 기존 `wallet_fee_minimum`/고정 5%·10%만으로 설명할 수 없어 새 경로로 대체했다. **인증이 필요한 실제 ajaxfeestructure 응답은 조회하지 않았으며**, 파서의 응답 계약은 이 공식 소비 코드와 합성 샘플에 근거한다. 다른 필드/세금/지원하지 않는 형태는 추측하지 않고 차단한다.
- 공식 순방향 계산: 판매자 금액의 최소/증분 조정, 수수료율 곱의 절삭, 수수료 최소·증분의 최근접/절반 올림, 선택적 상한. 독립 정수 계산으로 구현했다. 구매자 금액이 정확히 표현되지 않으면 수수료를 임의 추가하지 않고 입력을 거절한다. 호가 제안만 표현 가능한 하한으로 조정하고 원 호가와 조정 추천을 함께 보여 준다.
- 같은 번들은 `sellitem`에 `appid/contextid/assetid/amount/price`를 전송하고 수량 범위와 `requires_confirmation`을 처리한다. 이는 **기술적 amount 계약의 근거**이며 모든 게임/스택 ID가 부분 판매 후 유지된다는 보장은 아니다. 스택 제출은 별도 capability로 제한한다.
- [공식 global.js](https://community.fastly.steamstatic.com/public/javascript/global.js?l=english)의 통화 ID/whole-unit 정보와 판매 입력의 whole-unit `wire/100` 표시를 확인했다. KRW(ID16)는 원 단위와 wire×100을 구분하고 USD(ID1)는 cent=wire를 사용한다. 국가/문자열 통화 기호로 지갑 통화를 추측하지 않는다. 다른 통화는 아직 미지원이다.

### 현재 구현/검증 범위

| 기능 | 구현 | 제한 |
|---|---|---|
| 사용자 연결 UI | 별도 Steam 탭, 위험별 동의, 로컬 QR, 직접 승인 확인, 성공/만료/거부/취소/네트워크/차단/계약 오류, 로그아웃 | 실계정/실제 QR 스캔 미검증. 자동 polling·secret 입력·외부 QR 서비스·token 저장 없음 |
| 실제 데이터 UI | 명시적 갱신으로 세 게임(730/570/440, context2) 보관함·지갑·목록, 선택한 최대50종 호가/중간가격 이력, 원천·시각·오류 표시 | 게임/context 자동 발견 미구현. 중간가격은 정확한 거래 VWAP 아님. 일반 Mock 데이터로 실패를 대체하지 않음 |
| KRW/USD | `Money(long Minor,Currency)`와 별도 wire 변환, 서버 최소·증분·요율·상한 profile, 항목별 구매자/Steam·게임 수수료/실수령 즉시 갱신 | 새로운/불확실한 통화·반올림·추가 fee 형식은 제출 차단. 실제 지갑/서버 계산 비교 미검증 |
| 스택·동일 품목 | 보관함 수량과 로컬 예약 반영 가용량 분리, App/상품명/통화별 공유 잔량(다른 context도 공유), 3/4/3 분배, 부족 수량 별도 검토, 원자적 intent·예약 보존 | 부분 판매는 `StackAmountsSupported` 필요. 외부 스택 증가/병합·자산 ID 교체가 불확실하면 가용량은 보수적 하한, 강제 우회/새 ID 추측 없음 |
| 등록·대조 | 불변 최종 snapshot→새 지갑/fee/자산/호가 검증→직렬 전송. 서버 price/fee 모순 시 Unknown/중단. 요청 시각·기존 목록 ID·가격·수량·후보·감사 이력 수동 도구 | 사후 모순이 이미 보낸 요청을 되돌린다는 의미가 아님. listingid 없는 후보는 1개여도 인과관계 증명 불가: pending/Unknown 유지, 강제 성공·자동 재전송 없음 |
| 취소·재가격 | 전체 선택/개별 검토, 취소 의도 보존, 접수+목록 부재+원래 비스택 단일 자산의 동일 ID 반환을 모두 확인한 경우만 새 가격 제출. 재가격 target/후속 request key 보존, 명시적 대조·재개 | 다른 ID로 반환/응답 유실·미확정이면 재등록 불가. 승인 5분 경과 시 StaleReview 차단. 기존 재가격 의도의 가격 revision UI는 미지원; 확정 반환 후 새 보관함 검토/배치를 사용해야 함 |
| 저장·재시작 | Steam DB v1→v2 보존, 요청/상태 audit 원자 저장, 반환 증거에 해당하는 예약만 해제, 확정 배치 보관(명령·증거·예약 보존) 후 다음 배치 | Unknown/Guard 대기 배치는 보관 불가, live 확정 실패 재시도 분류·OS 전원 차단·실사용 장기 검증 미완료 |

등록 ID가 서버 응답으로 직접 연결되고 통화/수량/가격/품목이 맞을 때만 Listed를 확정한다. 메타데이터가 일치하는 후보를 자신의 요청으로 확정하는 것은 구현하지 않았다. 이는 사용자 데이터/실거래의 불확실성을 숨기지 않기 위한 필수 제한이다.

**취소 반환의 추가 제한:** 현재 자동 proof는 원래 검토 snapshot의 비스택 단일 자산과 서버 listingid가 직접 연결된 경우에만 허용한다. 스택의 증가 수량은 외부 병합/취득과 구분할 수 없으므로 목록 부재와 증가가 보여도 취소 완료로 확정하지 않고 예약/재가격 차단을 유지한다. 외부에서 등록된 항목이나 반환 ID 변경도 별도 서버 인과 증거가 필요하다. 해당 증거 어댑터는 미구현이다. 이 제한은 기본 거래 차단과 별도로 동작한다.

### QR 소스·배포 고지

[QrCodeGenerator v2.0.0](https://github.com/manuelbl/QrCodeGenerator/tree/11cf58fe6cec5bc44f70aa360e84b8d37570e37d), MIT(Manuel Bleichenbacher / Project Nayuki) 순수 계산 6파일을 고정 버전으로 포함했다. 원 저작권/라이선스 전문과 LICENSE를 보존하고 nullable 지시문만 추가했다. 네트워크/파일/프로세스 접근 코드가 없음을 확인했다. 새 패키지/SDK 설치 없이 로컬 WPF DrawingContext로 모듈·quiet zone을 그린다. 바이너리 배포에 MIT LICENSE와 THIRD-PARTY-NOTICES를 복사/검사한다. 비교 제품의 코드는 도입하지 않았다.

### 사용자 시작 및 거래 해제 경계

1. 앱 시작/Steam 탭 열기만으로 네트워크나 실저널을 만들지 않는다. 사용자가 위험별 체크 후 **QR 로그인 시작**을 누르면 해당 세션에서만 새 인증을 시작하도록 구현했다. 이번 개발에서는 누르지 않았고 합성 handler만 사용했다.
2. 공식 앱에서 직접 인증 후 **모바일 승인 상태 확인**과 데이터/시세 갱신을 요청한다. QR·토큰은 저장/로그/외부 변환하지 않는다. 로그아웃·동의 철회는 취소와 화면 정리를 수행하고 늦은 응답은 반영하지 않는다.
3. 기본 앱 factory는 `SteamTradingPolicy.Blocked`다. 체크박스만으로 거래가 열리지 않는다. Valve 허용 근거·해당 게임/스택/통화 검증과 별도 실계정 시험 승인이 준비된 composition에서만 정책 capability를 교체해야 한다. 합성 테스트의 허용 policy는 테스트 handler에만 사용한다.
4. 승인된 환경에서는 **최종 검토의 전체 판매등록 한 번**으로 선택 전체를 처리한다. Steam Guard 확인은 사용자 공식 앱/이메일에서 직접, 접수/대기와 실제 게시를 구분한다. 403/429/CAPTCHA/불명 요청은 우회·자동 재전송하지 않는다.

계정 제재/정책 위험과 인증 탈취 위험은 독립적이다. 출처 확인·짧은 직렬 요청·수동 확인·메모리 세션이 제재 위험0 또는 침해 불가능성을 보장하지 않는다. 실제 정책/계정/시장 조건·QR 스캔·실제 입력/모니터 DPI·설치/보안 감사는 아직 검증하지 않았다. 아래는 이전 단계의 당시 기록이다.

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

## 12차 로그인·조회 상태 정정

기존 코드가 일반 read schema failure를 계정 인증 실패로 보여주던 문제를 수정했다. 이는 실서버 계약이 검증됐다는 의미가 아니다. 인증 결과/finalize/Community transfer 후 메모리 세션 존재와 HTTP 안전 latch를 분리하고,401은 인증 만료,403/429/CAPTCHA는 요청 중단으로 유지한다. 단순 GET 통신·파싱 실패 후에는 사용자 시작의 독립 읽기만 가능하다. 실제 QR/보관함/fee/지갑/스택·취소/판매/Guard를 이번에 실행하지 않았고 READ_ONLY_READY/LIVE_READY를 부여하지 않는다. IntegrationBlocked·비공식 자동화 제재 위험은 그대로다.


## 0.20.1 후속 보완 (2026-10-29 KST)

FeeContext는 현재 계정 /profiles/.../inventory/ 요청의 동일 Steam canonical/vanity 이동을 한 번만 허용한다. vanity 페이지 g_bViewingOwnProfile=true와 g_steamID 현재 계정 일치 필요. 공개 HTML에서 변수 존재는 확인했으나 실제 인증 소유 페이지의 이동 목적지·fee props는 미검증. 구형 지갑 필드만으로 모드를 정하거나 신형 API 실패를 구형 성공으로 대체하지 않는다. 첨부 진단은 Wallet200→ModeMissing→FeeContext302/OtherRedirect, 목적지 미수집으로 원인 미확정. REAL_FEE_VERIFIED/LIVE_SELL_VERIFIED/RELEASE_READY 미부여. TBH 공식 App3678970, context2는 변경 가능한 조회 후보이며 게임 수수료/스택 계약 증거가 아니다. 비공식 자동화 제재 위험 유지.

## 0.20.2 classic 수수료 분기 보완 (2026-10-29 KST)

0.20.2: 공식 economy_v2 SellCurrentSelection은 g_bReactSellDialog가 false이면 SellItemDialog.Show를 사용한다. classic GetPublisherFeeRate는 앱 market_pubfee_rate, 없으면 실제 지갑 default를 쓰고 GetSteamFeeRate는 지갑 rate, OnInputKeyUp은 economy_common GetTotalWithFees를 사용한다. 따라서 React data-props가 없는 것만으로 모든 계정 계약을 미지원으로 판단하던 결함을 수정했다. 단, 명시적 false·소유/계정·동일 실제 지갑·앱 context를 모두 확인해야 하며 없는 플래그나 새 API 실패로 classic을 추측하지 않는다. 새 API와 구형 지갑 필드만의 fallback은 계속 금지한다. 실제 첨부 페이지의 flag는 미관측으로 REAL_FEE_VERIFIED/LIVE_SELL_VERIFIED/RELEASE_READY 미부여.

## 0.20.3 실제 소스 대조·기본 모드 보완 (2026-10-29 KST)

공식 classic GetPublisherFeeRate는 앱 context→실제 지갑 default를 사용한다. SteamMarketHelper의 market_fee 선택적 처리와도 대조해 필드 부재만으로 모든 검토/전송을 막던 결함을 수정했다. 현재 인증 소유 페이지의 classic 모드/앱/일치 지갑이 검증된 경우만 앱에 바인딩한 증거를 허용하며 명시적 상충 수수료·미확인 React/API 계약은 차단한다. 과거 상수/구형 계산을 복사하지 않았다. 실제 유저 활성화 실패 ZIP에는 해당 작업이 없어 근본 원인은 아직 미확정이다. REAL_FEE_VERIFIED/LIVE_SELL_VERIFIED/RELEASE_READY를 부여하지 않는다. 기본 실거래 선택도 Valve 허가나 자동 거래 권한이 아니다.

## 0.20.4 판매 응답·속도 보완 (2026-10-29 KST)

사용자0.20.3의 실제 판매 요청은 HTTP400으로 끝났고 Unknown 저널을 남겼다. 승인 요청 생성/게시 성공은 확인되지 않아 LIVE_SELL_VERIFIED가 아니다. Wallet/Profile/ClassicInventoryContext VerifiedLegacy/KRW/실제 호가 읽기 통과는 관측했으나 공식 판매 UI 금액 대조까지 완료한 REAL_FEE_VERIFIED로 확대하지 않는다. 기존 오류 본문 미수집으로400의 구체 원인은 미확정이다. 현재 세션 쿠키/정상 AJAX 문맥은 공개 구현과 대조해 수정했지만 실제400 해결은 사용자 재검증이 필요하다. UA/Chromium 지문 위장·프록시/IP회전·제한 우회/자동Guard는 도입하지 않았다. 읽기1.5초는 자체 pacing이며 Valve의 보장 한도가 아니다.
