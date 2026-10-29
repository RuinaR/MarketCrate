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
