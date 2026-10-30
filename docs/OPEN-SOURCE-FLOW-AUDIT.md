# 실제 오픈소스 전체 경로 대조 — 0.20.5

확인일: 2026-10-29 KST. README의 기능 설명이 아니라 아래 고정 소스와 MarketCrate의 실행 가능한 호출 경로를 비교했다. 공개 소스와 합성 HTTP 검사는 실계정 검증이나 Valve 허가 증거가 아니다.

## 확인한 원본

- [node-steam-session LoginSession.ts](https://github.com/DoctorMcKay/node-steam-session/blob/3ac0f34fd964b3f886ba18ef4824ac43c942e030/src/LoginSession.ts): MIT, Alex Corn. 기존 제한된 C# 이식과 MIT 전문을 유지한다.
- [node-steamcommunity](https://github.com/DoctorMcKay/node-steamcommunity/tree/fa8d89759077152b0c38175f154e03d0eacd2d1e): MIT. index.js의 자체 cookie jar/sessionid를 확인했다. components/market.js에 sellitem 구현이 있다고 주장하지 않는다.
- [SteamMarketHelper userscript v1.3.0](https://github.com/federicogiorgi/SteamMarketHelper/blob/e186d51efb742ddfb527d2ccb248c64fcee9c976/steam-market-helper.user.js): MIT, Federico Giorgi. inventory/orderbook/mylistings/sellItem/removeListing 및 재등록 흐름을 확인했다.
- [SSIM AccountTrader.ts](https://github.com/Santerxyz/SSIM/blob/28a503fa79869f3233814d0deaa906a8b3f66621/src/trading/AccountTrader.ts), [MarketService.ts](https://github.com/Santerxyz/SSIM/blob/28a503fa79869f3233814d0deaa906a8b3f66621/src/trading/MarketService.ts): Apache-2.0. 요청·부분 실패·불명 결과 처리를 비교했다. 이번에는 코드를 복사하거나 종속성으로 추가하지 않았다.
- Steam 공식 [economy_v2.js](https://community.fastly.steamstatic.com/public/javascript/economy_v2.js), [economy_common.js](https://community.fastly.steamstatic.com/public/javascript/economy_common.js), [marketselldialog.js](https://community.fastly.steamstatic.com/public/javascript/applications/community/marketselldialog.js). 로컬 확인본 SHA256은 순서대로 A3BBDF0A135524102CD055FD89D37EB3FD672FAE20BFBA0ABC700648DCB52512 / 84429D06A9EF8C250AC567F9FEDC9FB155D23CEB7442C92A19B815840C102784 / CB84BB8A9B2970F71D277626A0B4E922B47CCE90BF437F704CF8F5100FA66726. 변경 가능한 공개 JS이며 인증된 사용자 응답의 대체 증거는 아니다.

## UI부터 저장·복구까지 대조

| 경로 | 실제 원본 동작 | MarketCrate 연결 / 판정 |
|---|---|---|
| QR·로그인 복원 | LoginSession WebBrowser refreshToken → getWebCookies → 웹 세션 | 계정 화면 → SteamQrAuthentication / SteamWebRefreshToken → SteamAuthStore DPAPI CurrentUser. 자체 세션만 사용. 사용자0.17 재시작 복원 관측은 유지하며 장기 회전·만료는 미검증 |
| 인증과 조회 오류 | 인증된 자체 세션으로 서비스별 요청 | SteamWorkspaceViewModel → 독립 ReadStatuses / SteamHttp. 파싱 실패가 로그인 성공을 취소하지 않으며401/403/429/Challenge는 안전 중단. 로그인 복원이 판매를 자동 실행하지 않음 |
| 게임·보관함 | helper inventory assets/descriptions + more_items/last_assetid | 게임 카탈로그/직접 AppID-context → SteamInventoryProvider → SteamContract.Inventory. classid/instanceid·범위·중복·페이지 검증. 추천 게임을 보유 게임으로 표시하지 않음 |
| 이미지 | description icon_url 계열 | 카드/목록/검토 → 검증 CDN 이미지 공급자·상한 캐시·취소. 쿠키 없는 별도 이미지 요청. 이미지 실패를 자산 부재로 취급하지 않음 |
| 판매 중 목록 | SSIM norender JSON, helper HTML rows/hovers도 지원 | 판매 중 → SteamOrderProvider → SteamContract.Listings. 실제8건 관측 경로 보존. 임의 HTML fallback은 미지원이며 누락 bucket을 빈 목록 성공으로 대체하지 않음 |
| 호가·수량 배분 | helper orderbook 압축 가격/잔량·통화 | 시세 갱신 → SteamMarketDataProvider → SalePlanner. 동일 app/hash/통화의 모든 자산이 주문 잔량 공유. 부족 수량 표시, 실제 체결 보장 없음 |
| 거래가격 이력 | Steam pricehistory의 기간별 가격·거래량 | PriceIndicators.MedianEstimate는 기간별 중앙값의 거래량 가중 하위 중앙값 추정. 실제 체결 원자료의 VWAP/평균으로 표시하지 않음. 정확 평균 원자료 없으면 제공 불가 |
| 통화·수수료 | 공식 classic 앱/지갑 rates, React newFeeRates 분기 | SteamFees / FeeContractResolver / 정수 Money. 서버가 선택한 모드·AppID·수수료 증거를 검증. helper/SSIM의 CS2·USD 상수나 옛 base 산식을 KRW에 복사하지 않음. KRW 표시 정수와 Steam wire 단위 분리 |
| 최종 검토·여러 게임 | helper는 선택별 순차 제출, SSIM은 제한 AppID 지원 | Review → PrepareTrading → 앱별 최신 지갑/자산/수수료/목록 → SaleBatchSnapshot. **이번 수정:** 이미 게임A 권한이 있어도 게임B 추가 선택 시 재검증. 실패 시 전체 검토 갱신, 임의 제외/다른 가격 대체 없음 |
| 판매 요청 | 공식 UI/helper POST sessionid,appid,contextid,assetid,amount,price; price는 판매자 수령액 | Submit → SaleBatchCoordinator → SteamListingExecutor → SteamStateStore intent 선기록 → SteamHttp 직렬 POST. 자체 최신 CSRF, inventory Referer. 최종 승인 한 번이 전체 단위 제출, 서버 원자성·idempotency 보장 없음 |
| 승인 대기 | helper 수동 안내. SSIM은 maFile.identity_secret로 mobileconf 자동 승인 | **자동 승인은 채택하지 않음.** 응답 mobile/email/불명 채널을 분리해 저장·표시. 사용자 공식 앱/이메일 확인 후 명시적 재조회. HTTP400/Unknown은 승인 대기가 아님 |
| 게시 대조 | SSIM listed/unconfirmed 구분 | Recover/선택 요청 확인 → Executor.ReconcileAsync → 서버 listing ID·가격·수량·자산 대조 → 저널 → Results. ID 없는 후보가 우연히 일치해도 Listed로 확정하지 않음 |
| 취소 | helper HTTP200 빈 본문도 ACK 처리 | Cancel → 관리 검토 → Executor.CancelAsync. **이번 수정:** HTTP200 빈 본문/[]는 접수로만 해석. 후속 목록 부재·반환 인과 증거 전에는 취소 완료 아님. 빈 HTTP400은 Unknown |
| 재가격 | helper 취소·재등록 반복 루프 | 취소 검토 → 반환/서버 상태 대조 → 새 자산·가격 검토 → 새 승인. 불명/합쳐진 스택/바뀐 ID의 단순 수량 증가는 반환 증거 아님. 자동 반복 재등록하지 않음 |
| 재시작·오류 | SSIM commit ambiguity 처리 | 계정별 SQLite intent/예약/저널 → 명시적 복구. 타임아웃/미확정 자동 재전송 금지. 승인 채널 추가 필드는 이전 JSON과 호환, DB schema3 유지 |
| 진단·사용성 | helper 진행/상태, SSIM 제한 대응 | UI Run OperationId → 서비스/HTTP/SQLite 결과. 원문·쿠키·QR·계정 식별정보 제외. 빈 보존 요청 선택 상자 숨김·빈 상태 안내. 기본 실거래 모드 선택과 실제 전송 권한은 구분 |

## 채택하지 않은 방식과 남은 조건

SSIM의 identity_secret 자동 승인, Chromium fingerprint/UA 모사·proxy, helper의 다른 브라우저 DOM 주입·지속 재등록을 적용하지 않는다. 다른 프로젝트의 기술적 성공은 Valve 허가 근거가 아니다. CSlytics AGPL 코드는 직접 가져오지 않았다.

사용자0.20.3 sellitem HTTP400의 구체 서버 원인은 원본/정제 오류 응답이 없어 미확정이다. 이전 Unknown은 예약 유지하며 재전송하지 않는다. 추가한 응답 분류와 새 버전의 실제 접수·승인·게시 성공은 사용자 검증이 필요하다. listing ID 없는 게시의 확정 대조, 스택 부분 판매/반환, 미확인 통화·fee 모드, 정확 거래평균은 제한을 유지한다.

## 수정과 검증 근거

- AdditionalGame: 게임A 권한 뒤 게임B 추가 검토. 수정 전 기능341/342 PASS로 해당1건 실패 → 수정 후 정상 scope 검증, 선택 보존, 최종 승인 전 POST0.
- ApprovalChannels / EmailJournal / PendingWithoutId: 채널 구분, 이전 저널 호환/재시작 보존, ID 없는 우연 일치를 자동 게시 확정/재전송하지 않음.
- EmptyCancellation: HTTP200 빈 본문 접수→별도 반환 증거 확인, HTTP400 빈 본문은 Unknown. 개별 반환 테스트는 비스택 자산이며 스택 보호를 완화하지 않았다.
- ProductAuditUiTests: 승인 채널·불명 결과와 빈 요청 선택 UI의 비표시 STA 렌더. 물리 입력/모니터 DPI/실제 팝업은 미검증.
- 전체 기능346/346·비표시WPF67/67·배포 계약19/19·정리 fixture9/9 PASS. Release 검사는 배포 확정 기록 참조. 최초75개 판정47 COMPLETE /16 PARTIAL /0 MISSING /3 BLOCKED /9 UNVERIFIED 유지. 실계정 성공/REAL_FEE_VERIFIED/LIVE_SELL_VERIFIED/RELEASE_READY로 승격하지 않는다.

## 0.20.7 — SteamMarketHelper·SSIM 기능 기준 전체 재대조

확인일2026-10-29 KST. 기준은 기존에 고정한 SteamMarketHelper e186d51(MIT)와 SSIM28a503f(Apache-2.0)이다. 이번 추가 확인 파일은 SSIM `src/core/SessionManager.ts`, `src/core/InventoryService.ts`, `src/trading/AccountTrader.ts`, `src/trading/MarketService.ts`, `src/trading/commitAmbiguity.ts`, `src/pricing/MarketPricing.ts`, `test/marketListingsTruncated.test.ts`, `test/marketOrdersPartial.test.ts`다. 원본을 읽고 계약/상태 의미를 비교했으며 코드를 복사하거나 런타임/패키지를 추가하지 않았다. 아래가 최신 기준이며 위0.20.5의 로컬 예약 표시 설명은0.20.7에서 대체됐다.

| 제품 경로 | 두 프로젝트에서 확인한 동작 | MarketCrate 작업·남은 한계 |
|---|---|---|
| 인증·세션 | helper는 현재 브라우저 세션, SSIM은 SessionManager 토큰/웹 쿠키 | 자체 QR→Community·DPAPI 동의 저장/복원 유지. 브라우저 쿠키/maFile/password 가져오기는 채택하지 않음. 이전 사용자 복원 성공과 새 버전 미검증 구분 |
| 게임 범위 | helper는 현재 app/context, SSIM 판매는730/440 한정 | 관측 카탈로그·수동 AppID/context·TBH 지원. 미관측 게임 소유를 만들지 않음. SSIM의2게임 범위를 모든 게임 계약으로 일반화하지 않음 |
| 보관함 | helper fetchInventory는 assets/descriptions를 합치고 서버 amount 사용 | **로컬 과거 요청을 보관함 수량에서 차감하던 결함 수정.** Steam 응답 수량·marketable로 선택/수량/가격 검토. 전송 이력은 별도 상태 |
| 거래/판매 플래그 | SSIM은 게임별 unsellable 분류 | tradable과 marketable 독립 유지. 거래 제한만으로 장터 판매 가능 아이템을 임의 제외하지 않음 |
| 페이징·불완전 조회 | SSIM 테스트는3000상한/다음 페이지429를 partial로 표시 | 보관함/목록 전체 계약 검증 및 실패 시 이전 목록 보존. 현재 목록의 부분 행 신규 표시 기능은 제한이며 전체/빈 성공으로 속이지 않음 |
| 이미지 | helper descriptions metadata | 검증 CDN·쿠키 없는 요청·상한 캐시·중복 다운로드 억제·취소·가상화 유지 |
| 검색·선택·수량 | helper 선택 대상과 서버 amount | 게임 전환/보기 전환 선택 보존. TBH 서버1개/과거 미확정1건이어도 선택·시세 조회 가능. 새 전송 안전 조건은 별도 |
| 시세·통화 | helper orderbook, SSIM 지갑 통화 우선 | 현재 계정 지갑·KRW/USD/wire를 검증. 미지원 통화·미확인 wallet 추정 금지. SSIM의 EUR fallback/proxy 가격조회 경로 미채택 |
| 가격 전략 | SSIM lowestAsk/custom, helper buy depth | 즉시거래가능가는 실제 잔량 공동 소비·가격별 분할. 직접 가격 편집/최종 합계 연결. 평균 원자료 없음은 명시적으로 이용 불가 |
| 수수료 | SSIM feesForNet은 게임별0.05/0.10·통화 minimum, helper wallet/app | 공식 모드·실제 지갑·앱 rate를 쓰는 기존 resolver 유지. 해당 상수를 KRW/모든 게임에 복사하지 않음. 실계정 금액 대조 미완료 |
| 검토·일괄 등록 | helper sellItem, SSIM mass-sell 순차 작업/진행 | 전체 선택·불변 snapshot·단일 최종 승인·intent 선기록·직렬 POST·개별 결과 유지. 새 게임 scope 누락 수정은0.20.5 근거 유지 |
| 요청 계약 | helper sellitem6필드·seller receives, SSIM 자체 sessionid | 동일 필드·단위·현재 자체 CSRF/Referer. 실제 HTTP400 원인은 여전히 미확정, 합성 통과를 실판매로 판정하지 않음 |
| 승인 | helper 수동 안내, SSIM identity_secret 자동 mobileconf | 공식 앱/이메일 수동 승인·접수/대기/게시 분리. 자동 승인·비밀키 수집은 채택하지 않음 |
| 서버 동기화 | SSIM getListedAssets/미확인 bucket/후속 reconciliation | **정상 판매목록 갱신에서 기존 listing ID·상품·가격·수량이 일치하면 저널/결과를 동기화.** ID 없는 가격 일치나 목록 부재는 확정 근거로 사용하지 않음 |
| 실패·불명 | SSIM commitAmbiguity는 명확한 거절과 응답 유실 구분, 애매하면 유지 | UI의 선택/시세 기능은 열고 전송 시점의 중복 보호만 유지. 과거 본문 없는400을 뒤늦게 확정 실패로 바꾸지 않음. 새 요청 기록을 자동 재전송하지 않음 |
| 취소 | helper 빈200 ACK·SSIM cancelMarketListing | 빈200 ACK 지원과 후속 서버 상태 확인 유지. 취소 접수를 반환 완료로 표시하지 않음 |
| 재가격 | helper 반복 재등록, SSIM 취소/관리 | 현재호가 분배→전체 검토→확정 취소→반환 근거→새 등록. 외부 등록은 새 자산 검토 경로. 평균 기준은 원자료 부족으로 비활성. 상주 자동 재가격은 미채택 |
| 스택 | helper amount, SSIM 제한 게임 계약 | 보관함 전체 수량은 그대로 표시. 부분 판매/취소 반환의 실서버 계약 미확인 범위는 전송 차단. 단순 수량 증가를 반환 인과로 추측하지 않음 |
| 재시작·저장 | SSIM MoneyOpJournal/ambiguous commit gate | SQLite intent·Unknown·중복 키·계정 분리·기존 스키마 보존. 이전 최종 승인을 재실행하지 않음 |
| UI·진단 | helper 진행 상태, SSIM 상태/부분 결과/오류 | 서버 수량과 로컬 미확정 이력을 분리, 선택 가능 여부와 전송 가능 여부 안내 분리. 거래 권한 활성인데도 무조건 '차단'으로 표시하던 판매목록 안내 수정. OperationId→HTTP/저널/결과 연결 유지 |

SSIM의 매수주문 생성·trade offers·다계정 봇 운용은 최초75개 MarketCrate 요구사항에 없는 별도 범위이며 이번에 추가하지 않았다. fingerprint/proxy/IP분산·자동 Guard·백그라운드 반복 매도는 금지 조건과 충돌하므로 복제하지 않는다. 공개 구현이 Valve 허가나 계정 안전 보장은 아니다.

0.20.7 검증: 기능350/350·비표시WPF68/68·배포19/19·정리fixture9/9 PASS. 로컬 차감 UI를 기대하던 기존2건은 서버 수량/별도 전송 보호 계약으로 갱신했고 스택 반환·중복 전송 검사는 유지했다. 이전 Unknown은 보존하되 선택을 막는 Steam 예약처럼 표시하지 않는다. 알려진 ID 게시 동기화·ID/금액 상충 시 미확정 보존을 검사했다.75개 판정47/16/0/3/9 유지. 두 프로젝트와 기능 패리티 전체 완료나 LIVE 검증 완료를 선언하지 않는다.
최신성 확인: GitHub commits?per_page=1 재확인 결과 SSIM28a503f(2026-08-28 UTC), SteamMarketHelper e186d51(2026-09-06 UTC)이 현재 default branch 최신 커밋이었다. 최종 검토의 로컬 보호 확인은 검토 대상별 해시 조회로 수행해 다른 화면의 선택이 새 대상 검토를 막지 않게 했으며 관련16개 계약을 다시 통과했다.

## 0.20.8 — 즉시거래가능가 단일 옵션·수량 계약·ID 없는 관측

사용자 요청으로 보관함/판매 중 재등록의 평균·중앙값 가격 선택기와 기간/이력 버튼을 제거하고 **즉시거래가능가만** 제공한다. 단일 옵션을 불필요한 드롭다운 대신 고정 레이블로 표시하며 최종 검토의 직접 가격 수정은 유지한다. Release의 과거 가격 인덱스는0으로 정규화한다. 기존 enum/저장 계약·개발 회귀용 계산/이력 테스트는 보존하지만 일반 UI에서 선택할 수 없다. 최초75개 ID도 삭제하지 않으며 평균 관련 최초 요구는 이번 사용자 요청으로 제품 범위에서 철회됐음을 기록한다.

SteamMarketHelper e186d51의 sellItem(amount=item.amount)와 SSIM28a503f의 sellOnMarket(amount=1)/getListedAssets를 실제 소스로 대조했다. SSIM은 스택 판매의 근거가 아니며, 자동 Guard·비밀키·proxy/fingerprint는 도입하지 않았다. 공식 economy_v2.js를 인증 없이 다시 받아2026-10-29 SHA256 A3BBDF0A135524102CD055FD89D37EB3FD672FAE20BFBA0ABC700648DCB52512와 SellItemDialog의 amount=m_nConfirmedQuantity / price=m_nConfirmedPrice를 확인했다. 코드를 복사하거나 새 런타임/패키지를 추가하지 않고 현재 C# 어댑터에 해당 요청 계약을 적용했다.

인증된 소유 classic 페이지·동일 지갑·앱 수수료 계약으로 검증한 AppID에만 SingleRequestStackApps를 부여한다. 해당 앱의 스택은 **단일 가격·단일 요청**으로 서버 보유 수량 이하의 전체/부분 수량을 전송할 수 있다. 실제 계정 동작은 미검증이다. 여러 가격으로 쪼갠 동일 스택의 연속 전송, 미확인 React/new 수량 계약, 취소 후 ID 변경/합산 반환은 계속 차단한다. 최종 제출 직전 classic 계약이 바뀌면 intent/전송 전 중단한다. 기존 StackAmountsSupported 개발 플래그를 전체 실계정에서 켜지 않는다.

ID 없는 요청은 재조회한 **동일 asset/app/context·상품·수량·통화·가격·수수료**가 하나만 일치할 때 ObservedAssetState/ObservedListingId를 별도 증거로 남긴다. 현재 서버의 판매 중/승인 대기 관측은 표시하지만 이 요청이 그 등록을 만들었다고 확정하지 않는다. 원 command.ListingId를 후보로 채우거나 Unknown을 재전송/해제하지 않는다. 다른 자산/앱/금액·중복 후보는 관측을 확정하지 않는다. 이전400의 상세 원인과 실제 게시 성공은 여전히 미확정이다.

보관함·장터 표시와 로컬 전송 저널 분리, 단일 최종 승인 전체 배치·계정/통화/수수료·중복 전송 보호·DPAPI·기존 schema3/사용자 자료를 유지한다. 실제 GUI/계정/거래 실행0.75개 판정47/16/0/3/9를 실제 검증 없이 승격하지 않는다.
## 0.20.9 후속 — 실제 SSIM 코드 이식과 실행기 공유

사용자의 '실제 기능하는 프로젝트 코드 활용' 요청으로 SSIM28a503f의 MoneyOps.ts / MarketService.ts와 SteamMarketHelper e186d51의 sellItem/fetchInventory를 실제 소스로 재확인했다. SSIM은 현재 진행 중인 자산 잠금과 영구 거래 저널을 분리하며, MoneyOps 잠금은 작업이 끝나면 해제한다. 이 알고리즘을 C# SteamAssetOperationGuard로 부분 이식했다. Apache-2.0 LICENSE/NOTICE와 Copyright2026 Santer.xyz, 원본 커밋·경로·변경 사실을 보존했다. 브랜딩/비밀키/Node 런타임/자동 승인/네트워크 우회는 도입하지 않았다.

MarketCrate의 SetGame은 Executor를 재생성하므로 인스턴스별 semaphore만으로는 이전/새 Executor의 동시 작업을 보호하지 못했다. 수정 전 실행기로 동시 제출을 재현했을 때 SQLite SQL logic error가 발생했다(추가 중복 POST 성공을 확인했다는 뜻은 아님). 자산 잠금을 저장소 범위로 공유하고 SQLite 쓰기 큐도 공유해 같은 자산의 동시 제출/취소를 미전송 결과로 거절하고, 다른 자산은 직렬 처리한다. 잠금 소유 lease만 해제하며 대기 중 취소도 정리한다. 영속 Unknown은 해제/재전송하지 않고 별도 유지한다.

기존 f3b7594는 활성화 수정 중간 소스이며 그 publish 폴더는 검사만 했고 최신 ZIP/latest로 등록하지 않았다. 최종 SSIM 포함 배포는 후속 소스 커밋과 ZIP 해시 절을 기준으로 한다. 이전 후보·사용자 데이터·진단·롤백본 삭제0.
## 0.20.10 서버 동기화·새 승인

SSIM MoneyOps.ts/MarketService.ts 고정 커밋28a503f 재확인: 영구 예약으로 UI를 차단하지 않고 실행 중 자산 claim을 종료 시 해제하며 listed/unconfirmed 자산을 사전 조회한다. MoneyOps C# 실도입 코드를 재사용하고 서버 상태 기반 새 승인 경로를 보완했다. SSIM 자동 확인/다계정/재전송·fallback은 적용하지 않았다.
