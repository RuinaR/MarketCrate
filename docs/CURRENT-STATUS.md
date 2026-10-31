## 0.20.20 — 최저 유효 가격 유지·미전송 원인 안내

사용자는 한 단계 하향 불가 시 최저가 유지를 요청했다. 검증된 최저 가격만 LowerOrMinimumBuyerPrice로 유지하고, 행/전체 검토에 최저 유효 가격 유지·Steam/게임 수수료·실수령 감소0을 표시한다. KRW/USD/수수료0 조합에서 검증했다. 보관함/판매 중 재가격 제안에 같은 규칙을 적용하며 잘못된 통화·수수료·호가 없음·최저가 미만 입력은 계속 차단한다. PreviousValidBuyerPrice의 엄격한 계산 계약은 보존한다.

첨부0.20.19 로그는 최종3건 검토→호가 재조회→OrderbookChanged/StaleReview로 전송 전 중단됐다. Sell HTTP 요청/항목 Submitting intent는 없어서 해당 작업의 Steam 확인 요청도 생성되지 않았다. 실제 승인 알림 전송 장애로 해석하지 않는다. 이 검증 위치에서만 RequestNotSent/OrderbookChanged를 명시하고 사용자에게 시세 갱신→새 최종 검토→전체 판매등록을 안내한다. 호가 변경 차단이나 승인 불변성을 제거하지 않는다. SteamException이 IOException을 상속해 BatchFinished를 Storage로 잘못 분류하던 진단도 실제 실패 분류 인터페이스로 수정했다.

기능430/430·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS, 정리8 PASS/0 FAIL/1 NOT VERIFIED, build 경고/오류0. 3개 개별 자산을 최저 유효가로 한 번 승인→3요청/중복0, 호가 변경0요청→명시적 시세 갱신·새 승인→3요청 합성 검증.125/200% 최저가 검토 RTB를 직접 확인했다. 사용자 실제 계정/새 버전 거래·물리GUI는 NOT VERIFIED. Auth/설정/SQLite·기존 저널·과거 ZIP 삭제0, 75개47/16/0/3/9 유지.

## 최신 배포 — 0.20.19 승인 후 현재 목록 중심

소스 **a3ef735f1d516c22de6d7424423e80cb62a7af19**. `artifacts/releases/MarketCrate-0.20.19-current-listings-a3ef735-win-x64.zip` **66,750,871바이트 /418파일**. SHA256 **2470686121811D9A4BD5A1F1FF09C5BD085996C74DDB41526E2E81B6E8869C91**. 전체 ZIP entry/출하 파일 해시·SourceCommit/버전·x64 CLR/WPF10.0.14·SQLite/DPAPI·라이선스·실제 전용 PE/BAML·Mock/개발UI/Auth/DB/로그/PDB 제외 검사 PASS. Release build 경고/오류0, 앱/설치/실계정 실행은 NOT VERIFIED.

전체 기능428/428·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS. 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가).125/200% 승인 인계와 현재 판매목록 자체 RTB를 직접 검토했다. 75개 판정47/16/0/3/9 유지. 정상 기본 흐름에서 history 호출/후속 체결 추적0, 원래 요청/중복 보호 기록은 보존한다.

롤백0.20.18(d3eb49e, SHA256 15645A9B553A9775EB8DD92A724E53E06A9CBCC253EE4F938A16CF6C92ED2C25)과 모든 사용자 Auth/설정/SQLite/저널/진단·이전 ZIP 보존, 삭제0. 사용자는 새 폴더에 해제→로그인 복원→필요 시 공식 Steam 앱 수동 확인→승인 후 판매 중 보기로 현재 남은 항목을 확인한다. 이미 팔린 항목의 정밀 이력 대조는 일반 사용에 필요하지 않다. 응답 불명 요청을 새로 전송하거나 원기록을 성공으로 조작하지 않는다.

## 0.20.19 — Steam 확인 이후 현재 판매목록 중심

사용자는 Steam Guard 수동 확인 뒤 체결 기록을 계속 관리할 필요 없이, 판매목록 갱신에서 남은 항목만 보기를 요청했다. 기본 버튼을 승인 후 판매 중 보기(ShowCurrentListings)로 연결했다. 현재 Orders.RefreshAsync/SetListings만 수행하고 판매 중 화면으로 이동한다. 체결 이력 조회/Batches.Recover/원본 Unknown 재전송은 실행하지 않는다. 일반 판매목록 갱신도 기존처럼 현재 서버 목록만 보여준다. 사라진 항목의 체결/취소 원인을 추측하거나 허위 완료로 저장하지 않는다.

판매 결과는 판매 요청 결과로 표시하고 요청 당시 기록임을 명시했다. 거래내역 정밀 대조는 선택적 고급 버튼으로 보존했다. 최초 전송 오류/응답 불명·공식 모바일/이메일 확인 안내는 유지하며, 이후 정밀 대조를 일반 사용의 필수 단계로 요구하지 않는다. 저널·중복 전송 보호·Auth/설정/DB·기존 사용자 기록은 삭제하지 않았다.

전체 기능428/428·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS, 정리8 PASS/0 FAIL/1 NOT VERIFIED, build 경고/오류0. 기본 경로가 history403을 호출하지 않음, 이미 사라진 항목은 빈 목록, 남은 게시 항목 표시, 읽기403/취소 시 기존 화면·자료 보존을 검증했다.125/200% 자체 렌더 검토, 실제 GUI/실계정 실행0. 최초75개47/16/0/3/9 유지. 실제 서버 새 버전 미검증이다.

## 최신 배포 — 0.20.18 KRW 거래내역 원본 단위 대조

소스 **d3eb49e282cba23ebf26c418c5cfd048ee633de2**. `artifacts/releases/MarketCrate-0.20.18-history-wire-d3eb49e-win-x64.zip` **66,750,288바이트 /418파일**. SHA256 **15645A9B553A9775EB8DD92A724E53E06A9CBCC253EE4F938A16CF6C92ED2C25**. 전체 ZIP entry/출하 파일 SHA 일치·SourceCommit/버전·x64 CLR/WPF10.0.14·SQLite/DPAPI·라이선스·실제 전용 PE/BAML·Mock/개발UI/DB/Auth/로그/PDB 제외 정적 검사 PASS. Release 경고/오류0. 앱/설치/실계정은 미실행이다.

전체 기능424/424·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS. 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 원화 이력1건의 비정수 표시단위 때문에 페이지 전체가 중단되는 합성 재현을 수정했고 신규5개 회귀를 포함한다. 사용자 실제3건의 새 버전 대조는 아직 NOT VERIFIED다. 75개 판정47/16/0/3/9 유지.

롤백0.20.17(72b7b70, SHA256 9139E51CCD5C4C378E04C9D448DCA428D2DBD6D808C1C35C42BE5D77910D502D)과 모든 기존 배포·운영 로그·Auth·설정·SQLite·저널을 보존했다. 삭제/초기화0. 사용자는 새 폴더에 ZIP을 해제해 로그인 복원 후 기존 판매 결과의 ‘승인 후 게시 확인’ 또는 ‘선택 요청 상태 확인’을 실행하면 된다. 새 판매는 필요하지 않다. 원래 등록 ID/자산/통화/수량/원본 단위 금액/시각이 정확히 일치해야 판매 완료이며, 불일치는 새 이유별 진단과 Unknown을 유지한다. Codex의 실제 계정/GUI 요청0.

## 0.20.18 — 즉시 체결 뒤 KRW 거래내역 전체 중단 수정

사용자는 단일 수량3건이 실제로 체결됐지만 앱에 Unknown으로 남았다고 확인했다. 앱 종료 후에도 남아 있는 MarketCrate JSONL을 읽기 전용 분석했다. 0.20.17의18:02 KST 대조2회 모두 MarketHistory HTTP200 뒤 SteamContract.FromWire@IL64/SteamSaleHistory.Parse@IL646에서 실패했다. 출하 IL과 비교해 received_amount 정수→원화 표시 단위 변환 지점임을 확인했다. 이력의 KRW 원본 금액이100으로 나누어떨어지지 않아 페이지 전체 파싱이 중단됐고 뒤의 항목도 모두 대조되지 않았다. 원본문/개별 금액은 없으므로 해당 이력이 현재3건인지 다른 과거 이력인지는 미확정이다.

SteamSaleHistory는 이제 금액을 ReceivedWire 정수로 보존하고, 확인할 요청의 확정 실수령만 ToWire로 변환해 원본 정수끼리 정확히 비교한다. 원본 금액을 반올림하거나 계정의 현재 표시/등록 증분으로 이력 페이지를 거부하지 않는다. 잘못된 통화·실수령/원단가 모순·ID/자산/수량/시각 불일치·복수 후보는 계속 Unknown이다. 공개 구매계약의 nullable 보류/철회 필드도 수용하며 명시적 철회는 계속 배제한다. 요청 CorrelationId를 파싱까지 유지하고 불일치 이유별 안전 진단을 추가했다. 실제 판매가격·수수료·송신 계약과 SQLite/Auth 스키마는 변경0.

추가5기능 포함 전체424/424·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS. 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가), build 경고/오류0. 새 코드의 사용자 실제3건 재대조는 NOT VERIFIED이며 LIVE 검증으로 승격하지 않는다. 최초75개47/16/0/3/9 유지. 운영 로그/설정/Auth/SQLite/저널·이전 배포 삭제0, 실제 GUI·Steam 요청·타 프로세스 접근0.

## 최신 배포 — 0.20.17 판매목록 빈 자산 맵

소스 **72b7b703caa5133c4719198fe80297b2d39d75c7**. `artifacts/releases/MarketCrate-0.20.17-empty-listings-72b7b70-win-x64.zip` **66,749,751바이트 /418파일**. SHA256 **9139E51CCD5C4C378E04C9D448DCA428D2DBD6D808C1C35C42BE5D77910D502D**. ZIP 모든 entry와 출하 파일 SHA 일치, 버전/커밋·x64 CLR/WPF10.0.14·SQLite/DPAPI·라이선스·실제 전용 PE/BAML·Mock/개발UI/개인DB/Auth/로그/PDB 제외 검사 PASS. Release 경고/오류0, 앱/설치/실계정 실행은 NOT VERIFIED.

기능419/419·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 최초75개47/16/0/3/9 유지. 원본 로그를 저장하지 않아 사용자의 root assets 정확한 JSON 종류는 미확정이며 출하 IL로 비객체 거부 분기만 확정했다. 빈 [] 호환은 합성 계약/활성화 경로로 검증했다. 잘못된 맵/금액/total_count/403·429 보호는 유지한다.

최신0.20.17/롤백0.20.16(209d0a7, SHA256 FAEE88178DE08A507A5852924017CB834CE38F79B68D2CBD307ECB6C1DE68693) 보존. 기존 사용자 Auth·설정·SQLite·저널·진단·이전 ZIP 삭제/초기화0. 사용자는 새 폴더에 ZIP 해제→기존 로그인 복원→보관함/판매목록 갱신→실제 거래 준비 또는 최종 가격 검토로 기존 실패 지점을 재확인할 수 있다. 이 확인만으로 판매 요청은 전송하지 않으며 최종 전체 판매등록은 별도 사용자 승인이다. 실패 시 새 작업 진단을 내보내고 원본문/인증정보를 별도 수집하지 않는다.

0.20.17 최종 전체 검사: 기능419/419·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). build 경고/오류0. 신규8개 합성 계약은 빈 맵·권한 준비·미확인 자산 보존·진단 누출 방지를 검증한다. 실제 계정/앱 실행0.

## 0.20.17 — 판매목록 assets 빈 맵 호환 수정

사용자0.20.16 진단에서 QR 세션/지갑·수수료·서버 자산2건 검증 뒤 Listings HTTP200/JsonBody 파싱이 SteamMarketMembership.Read@IL287에서 실패했다. 출하0.20.16 IL을 읽기 전용 대조해 `assets.ValueKind != Object`의 명시적 throw임을 확인했다. 따라서 로그인·수수료·실제 판매 요청 실패와 다르다. 원본 응답을 수집하지 않아 실제 자료형이 []인지 null인지까지는 미확정이다.

객체만 허용하던 맵 순회를 객체 또는 명시적 빈 배열[]로 확장했다. root/app/context 맵의 빈 배열만 허용하고 null/스칼라/내용 있는 배열은 계속 차단한다. 공식 공개 JS 보존본의 ObjectIsEmpty→MergeWithAssetArray 소비와 호환한다. canonical 판매/승인대기 자산은 metadata 맵이 없어도 유지하고 금액·상품명을 생성하지 않는다. total_count/전체 페이지 검증은 그대로이며 숫자 불일치를 빈 목록으로 확정하지 않는다. 이 수정은 안전 차단 삭제나 합성 실거래 성공 대체가 아니다. 실계정 새 버전 검증은 사용자 직접 단계다.

기존 Auth·설정·SQLite·저널·Unknown/Sold·호가/단일 최종 승인 보존, 스키마/의존성 변경0. 최초75개47/16/0/3/9 유지. 실제 앱/Steam/호스트 UI/타 프로세스 접근0. 최신 배포·전체 결과는 확정 후 기록한다.

## 최신 배포 — 0.20.16 즉시 체결 상태 대조

소스 **209d0a7f958b623c9be490374e0294f371fa40bb**. `artifacts/releases/MarketCrate-0.20.16-instant-sale-209d0a7-win-x64.zip` **66,748,818바이트 /418파일**. SHA256 **FAEE88178DE08A507A5852924017CB834CE38F79B68D2CBD307ECB6C1DE68693**. ZIP 전체 entry/출하 파일 해시 일치·버전/커밋·x64 CLR/WPF10.0.14·SQLite/DPAPI·라이선스·실제 전용 PE/BAML·Auth/개인DB/로그/PDB/Mock/자가검증 제외 정적 검사 PASS. Release build 경고/오류0. 앱/설치/실계정은 미실행이다.

기능411/411·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS. 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). Sold/보류/Unknown125/200% 자체 RTB 시각 검토 PASS. 최초75개47/16/0/3/9 유지. 새 체결 이력 파서는 실계정 NOT VERIFIED다.

최신0.20.16/롤백0.20.15(59e3191, SHA256 3E55B1AECECE9728B381D0BC36DE9E2F155838FDE492EADC53F1BD5BE895A21D) 보존. Auth·설정·SQLite·저널·진단·이전 배포 삭제0. 기존 enum 숫자를 유지하고 Sold를 뒤에 추가했으므로 새 Sold 저널을 구버전이 읽지 못할 수 있다. 이 경우 DB를 삭제/초기화하지 말고 새 앱을 사용한다.

사용자 확인: 새 ZIP을 별도 폴더에 해제 → 로그인 복원 → 판매 결과의 해당 요청 선택 후 ‘선택 요청 상태 확인’ 또는 ‘승인 후 게시 확인’. 등록 ID가 있고 실제 거래내역이 일치하면 판매 완료, ID가 없으면 체결 관측/원요청 귀속 미확인, 범위 밖/반영 지연/스택 부분 체결/모순은 미확인 안내다. 알려진 ID도 판매목록 부재만으로 완료 확정하지 않는다. 추가 판매를 먼저 할 필요는 없으며 기존 요청 상태를 새로 확인할 수 있다. 원본 증거는 보존하며 자동 재전송하지 않는다.

최종0.20.16 검증: 기능411/411·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS. 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). build 경고/오류0. Sold/대금 보류 및 Unknown 안내125/200% 자체 RTB 시각 검토 PASS. 신규 실제 Steam/계정/물리 GUI 실행0. 최초75개 판정47/16/0/3/9 유지.

## 0.20.16 — 즉시 체결 거래내역 대조 추가

사용자가 ZIP과 별개로 ‘등록 직후 체결 → 다음 갱신에서 미확인’을 재확인했다. 원인은 기존 ReconcileAsync가 현재 판매/승인 목록만 조회하고 개인 장터 거래내역을 조회하지 않던 경로다. 현재 목록 부재 시 /market/myhistory/render/의 최근100개 이벤트를 명시적 갱신 작업당 최대1회 조회한다. 서버 listing ID·원래 자산·단일 수량·통화·실수령·요청 이후 시각이 일치하는 판매 이벤트만 Sold(판매 완료)로 저널/배치/UI에 반영한다. 이미 게시 확인한 항목도 후속 체결 대조를 수행한다. ID 없는 응답은 동일 자산 체결 관측을 안내하지만 원요청 ID를 합성하거나 귀속 확정하지 않는다. 스택 부분 체결·100개 범위 밖/반영 지연·계약 불일치/외부 동조건 후보는 미확인을 유지한다. 실패/취소/구매/반환·롤백 이벤트를 체결로 처리하지 않는다. 판매대금 보류는 별도 안내다.

기존 enum 숫자는 유지하고 Sold=9를 뒤에 추가했다. 이전 버전은 새 Sold 저장 기록을 해석하지 못해 안전 차단할 수 있으므로 새 DB를 이전 버전으로 초기화/수정하지 않는다. 과거 게시 증거는 체결 증거가 없다고 Unknown으로 역행시키지 않고 역사적 게시 확인/현재 결과 미확인으로 안내한다. 인증 복원·단일 최종 승인·Unknown 재전송 금지·운영 데이터 보존 유지. 실제 거래내역 응답의 새 파서는 공개 구현 계약 및 합성 검증이며 사용자 실서버 체결 대조는 미검증이다. 아래406/78은 추가 체결 대조 전 중간 검사다.

검증 결과: 기능406/406·비표시WPF78/78·배포20/20·advisory 검사기6/6 PASS. 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). build 경고/오류0. 결과 Unknown/거래내역 안내125/200% 자체 RTB를 직접 검토했다. 실계정 즉시 체결·거래내역 및 물리 GUI는 NOT VERIFIED.

## 0.20.16 — 즉시 체결 뒤 상태 갱신/재가격 보호 (2026-10-31 KST)

첨부 진단의 실제 기록은 PrepareListingRepriceAsync → Wallet/수수료 context HTTP200 → AppContextMissing/Unverified이다. 등록·승인 후 게시 확인 기록은 없어 즉시 체결이 해당 실패의 직접 원인인지는 미확정이다. 오래된 판매목록 선택으로 수수료 조회를 먼저 하던 경로를 수정했다. 새 목록의 ID·자산·상품·수량·통화·실수령·수수료·게시 상태를 먼저 대조하고 하나라도 바뀌면 이전 전체 검토를 폐기한다. 수수료 Unsupported 중에는 허용된 독립 목록을 한 번 더 대조해 조회 도중 소멸도 처리한다. 제한된 수수료 요청을 재전송하거나 캐시/상수로 성공 대체하지 않는다.

등록 ID가 없고 현재 게시/승인 대기 증거도 없는 이전 접수는 현재 UI에서 Unknown으로 표시한다. 기존 Guard 접수 원기록은 보존한다. 알려진 ID도 목록에서 사라지면 즉시 체결/취소/기타 처리 가능성을 안내하며 거래내역 확인을 요구한다. 목록 부재를 Sold로 확정하거나 자동 재전송하지 않는다. 별개 자산의 새 최종 검토/배치는 독립 진행 가능하다. Auth·설정·SQLite 스키마/운영 데이터 변경·삭제0, 실제 계정/GUI 실행0. 최초75개 판정47/16/0/3/9 유지. 아래 이전 배포 기록은 해당 버전의 증거다.

# MarketCrate — 현재 상태 (2026-10-31 KST, 0.20.15)
## 최신 배포 — 0.20.15 한 단계 하향 옵션

소스 **59e3191afe21c3d11a86e53d343c2f96c3e7b779**. `artifacts/releases/MarketCrate-0.20.15-lower-step-59e3191-win-x64.zip` **66,743,543바이트 /418파일**. SHA256 **3E55B1AECECE9728B381D0BC36DE9E2F155838FDE492EADC53F1BD5BE895A21D**. ZIP 모든 entry SHA/출하 파일 일치, SourceCommit/버전·x64 Core/WPF10.0.14/SQLite/DPAPI·라이선스·실제 전용 PE/BAML·Mock/테스트UI/Auth/DB/로그/PDB 제외 정적 검사 PASS. Release build 경고·오류0. 실제 앱/설치는 미실행이다.

전체 기능399/399·비표시WPF77/77·배포20/20·advisory 검사기6/6 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 최종 문구/관리 승인조건 및 수동 증가 비교를 포함한 신규10기능/2WPF는 관련 재검증과 번들 .NET/WPF10.0.14에서도 PASS.125/200% 자체 RTB의 비교/스크롤/버튼을 직접 검토했다. 옵션의 실제 서버 체결/물리 GUI는 미검증이다. 실제 앱·계정·판매·취소·Guard 실행0, 최초75개47/16/0/3/9 유지.

최신0.20.15/롤백0.20.14(1f3d95e, SHA256 FDC735FF70F9CC1264A0EAFB296AE1B0EF80577678C0E3AFF65E4AA102F0238A). 이전 배포·설정·DPAPI Auth·SQLite·저널·진단 삭제0, 외부 바이너리 업로드0. 사용자 확인: 새 폴더에 ZIP 해제 → 로그인/직접 갱신 → ‘즉시거래가능가보다 한 단계 낮게’ 직접 선택 → 최종 검토의 기본/변경 가격·수수료·실수령 감소액/비율 확인 → 원하는 경우 전체 판매등록 한 번 승인 → 공식 앱 수동 확인/서버 게시 대조. 최저 하향 불가 또는 계약/시세 변경은 옵션/대상/자료를 바꿔 새로 검토해야 한다. 자동 반복 인하나 체결 보장은 없다.
## 0.20.15 — 한 단계 낮은 유효 가격 옵션

보관함 판매 계획과 판매 중 화면에 명시적 하향 옵션을 추가했다. 기본OFF이며 실제 서버 수수료·최소/증분으로 기본 유효 가격의 바로 아래 도달 가능한 구매자 총액을 제안한다. 최종 검토에 기준/변경 구매자 가격, Steam/게임 수수료, 실수령, 수량별·전체 실수령 감소액/비율을 표시한다. 수동 입력 시 비교 즉시 갱신, 최저 하향 불가 항목은 유지/제출 차단, 검토·처리 중 옵션 변경 금지다.

단일 전체 승인의 불변 snapshot/직렬 배치와 최신 계정·자산·수수료·호가 재검증·Guard/Unknown·중복 방지를 유지했다. 기본 가격/평균 enum·기존 SQLite·Auth 스키마 변경0, 실제 거래/실계정/물리 GUI 실행0. [사용법·계산·제한](LOWER-PRICE-OPTION.md)을 따른다. 기존 사용자0.20.14 게시 관측을 이 하향 옵션의 실제 체결 증거로 확대하지 않는다. 최초75개47/16/0/3/9 유지.
## 이전 배포 — 0.20.14 사용자 경로 검증

소스 **1f3d95eaceab979197807a3ef4035ca8941cb251**. `artifacts/releases/MarketCrate-0.20.14-user-paths-1f3d95e-win-x64.zip` **66,740,827바이트 /418파일**. SHA256 **FDC735FF70F9CC1264A0EAFB296AE1B0EF80577678C0E3AFF65E4AA102F0238A**. Core/WPF10.0.14 x64 자체 포함·SQLite/DPAPI·실제 전용 PE/BAML·라이선스·사용자 DB/Auth/로그/테스트 UI/Mock/PDB 제외 및 ZIP 모든 파일 해시 일치 PASS. 실제 앱 실행/설치는 수행하지 않았다.

기능389/389·비표시WPF75/75는 기본 및 번들10.0.14 환경에서 각각 PASS다. 배포20/20·advisory 검사기6/6 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가), Release build 경고/오류0. 사용자 입력/실행 경로와 한계는 USER-JOURNEY-QA 참조. 무한 조작 조합100%·새 실계정/물리 GUI·전체 RELEASE_READY를 선언하지 않는다. 최초75개47/16/0/3/9 유지.

최신0.20.14 /롤백0.20.13(f1c8631, SHA256 2F45D2EFD6C21D54AB30271D31F9448C3F15FE5F3E300B49B6AD10CF27B528CE). 기존 Auth·설정·SQLite·거래 저널·진단·과거 배포 삭제0, 원본 운영 자료/테스트 산출물 외부 전송0. 사용자는 수량을 직접 입력한 직후 검토 수량 확인 → 잘못된 입력에서 검토 비활성·게임 전환 후 오류 보존 → 정상 수정/갱신 → 실제 금액 최종 확인 순으로 물리 GUI를 점검할 수 있다. 실제 거래 시험은 사용자 직접 범위이며 Unknown이면 추가 등록을 중단한다.
## 0.20.14 — 사용자 실행·입력 경로

UI에 연결된 ICommand의 활성 조건과 실행을 기준으로 최초 실행부터 로그인·조회·수량/가격·단일 승인 배치·Guard/게시 대조·취소·재접속을 검증했다. 수량 LostFocus 지연/잘못된 입력의 이전 수량 사용·재활용 오류 소실, 제거된 게임의 숨은 선택, 금액/합계 overflow 표시 결함을 수정했다. [사용자 경로 QA](USER-JOURNEY-QA.md)에14개 명령 시나리오·수량12종/가격13종/HTTP·판매응답·카탈로그 입력 행렬과4개 실제 템플릿 검사를 연결했다.

최종 기능389/389·비표시WPF75/75·배포20/20·advisory 검사기6/6 PASS. 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가), build 경고/오류0.125% 수량 오류와200% 최종 검토 자체 RTB 이미지를 직접 검토했다. 실제 창/물리 입력/Popup/모니터DPI·실계정/판매/취소/Guard는 실행하지 않았다. 전체 경우의 수100% 검증 또는 LIVE/전체 RELEASE_READY 선언0. 최초75개 판정47/16/0/3/9 유지. 운영 데이터·과거 배포·Auth/저널/진단 삭제0.
## 이전 배포 — 0.20.13

소스 **f1c86318e1f49a1b9f46fe69e5e56b6d25970248**, Windows x64 self-contained portable. `artifacts/releases/MarketCrate-0.20.13-stability-f1c8631-win-x64.zip` **66,738,853바이트 /418파일**. SHA256 **2F45D2EFD6C21D54AB30271D31F9448C3F15FE5F3E300B49B6AD10CF27B528CE**. 전체 ZIP entry/출하 파일 SHA 일치, SourceCommit/버전·x64 Core/WPF10.0.14/SQLite/DPAPI·원본 라이선스 byte 일치·실제 전용 PE/BAML·Mock/개발자가검증/사용자DB/Auth/로그/PDB 제외 검사 PASS. Release build 경고/오류0. 실제 앱/설치 실행은 미실행이다.

기능375/375·비표시WPF71/71·배포20/20·advisory 검사기6/6 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 공개 NuGet advisory 5개 프로젝트/직접·전이 검사 PASS. 번들10.0.14 콘솔 기능375/375, 구/신/구 provider의 active/archive/Unknown/Preferences 보존 PASS. 중간 개발 파일 저널 접근 거부1회와 정리 fixture 기대 파일 수 수정은 QA-MATRIX에 숨기지 않고 기록했다.

최신본0.20.13 /롤백0.20.12(c75ceff, SHA256 2C0AF766F53FCED230F29FA86A6AC7035E08D51B1876897D60BC64653D6E8534). 과거 배포·사용자 설정·Auth·SQLite·거래 저널·진단 삭제0, 외부 바이너리 업로드0. 최초75개47/16/0/3/9 유지. 새 실계정/휴대폰 승인/실제 게시·물리GUI 미검증이며 전체 RELEASE_READY/LIVE_SELL_VERIFIED/100%를 선언하지 않는다.

최소 사용자 확인: ZIP을 새 폴더에 풀기 → 로그인 복원 → 실제 보관함·판매목록·수수료·호가 직접 갱신 → 단일 항목의 새 가격/수량 최종 검토. 사용자가 거래 위험을 수락해 시험하는 경우에만 전체 판매등록1회 → 필요한 공식 모바일/이메일 확인 → 승인 후 게시 확인. Unknown/계약 불일치/금액 이상 시 추가 제출하지 않고 해당 작업 진단을 내보낸다. 취소 후 새 대상 검토는 원래 반환 확정과 다르며 실제 자산 ID·새 가격을 다시 확인한다.

## 0.20.13 안정화 후보

배치 승인 불변성·취소 후 새 대상 검토·계정별 pending 분리 결함을 수정했다. 번들 Core/WPF10.0.14, Sqlite/DPAPI10.0.14, PCL2.1.14(native3.54.0). 구/신/구 provider SQLite 호환 검사 PASS. 기능375/375·자체WPF71/71·배포20/20·advisory 검사기6/6 PASS. 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가), 공개 NuGet advisory 5개 프로젝트 검사 PASS. 기능375개는 번들10.0.14 콘솔 환경에서도 PASS다.

중간 개발 전용 JSON 저널 접근 거부1회는 QA-MATRIX에 보존했고 독립3회 반복/최종 전체 기능은 PASS다. 라이선스3개 추가에 따른 합성 정리 fixture 파일 수 불일치도 기대 파일 집합25/27로 수정 후 전체 정리 검사를 통과했다. 운영 데이터 삭제0. [릴리즈 신뢰도·남은 실계정 조건](RELEASE-READINESS.md)을 따른다. 아래 이전 배포 기록을 새 검증 결과로 해석하지 않는다.


## 이전 배포 — 0.20.12

소스 c75ceff18c0a353e0cd2c8f47afd5836760c875d. artifacts/releases/MarketCrate-0.20.12-parity-c75ceff-win-x64.zip **66,536,415바이트 /415파일**. SHA256 **2C0AF766F53FCED230F29FA86A6AC7035E08D51B1876897D60BC64653D6E8534**. 전체 ZIP entry/출하 폴더 SHA 일치, SourceCommit/버전·x64 CLR/WPF/SQLite/DPAPI·라이선스/실제 전용 PE/BAML·Mock/자가검증/사용자DB/Auth/로그/PDB 제외 정적 검사 PASS.

기능372/372·비표시WPF71/71·배포19/19 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 마지막 문구 구분 후 관련 판매 계약 전체와 WPF71개 재검증. ID 없는 현재 게시 관측/요청 기록 분리 이미지를 직접 검토했다. 최신본0.20.12/롤백0.20.11(b487acc, SHA256 CC83BA7F779935006FF0437794E7E80213B10119379527896832E35149E9285B)을 보존했다. 이전 배포·Auth·설정·SQLite·저널·진단 삭제0, 원본 진단/공개 JS 다운로드 Git·배포 포함0, 외부 Release 업로드0.

검증 범위는 코드·공식 공개 소스·합성 계약·자체 렌더·배포 정적 검사다. 새 실제 앱·설치·물리 GUI·실계정·등록 접수·휴대폰 승인·실제 게시 확인은 미검증이다. 최초75개47/16/0/3/9 유지,100% 동등/LIVE_SELL_VERIFIED/전체 RELEASE_READY를 선언하지 않는다.

사용자 최소 재검증: 새 ZIP을 별도 폴더에 풀기→로그인 복원→실제 보관함/수수료/호가 갱신→단일 품목의 금액·수량 최종 검토→전체 판매등록1회→개별 결과의 미전송/접수/모바일·이메일 요구 확인→필요한 경우 공식 앱에서 수동 확인→승인 후 게시 확인. 현재 게시 관측과 원요청 귀속 미확인은 별도로 표시한다. Unknown/계약 불일치가 있으면 후속 다중 등록을 중단하고 해당 작업 진단을 확인한다.



## 0.20.12 — 실제 기능 동등성 감사·로그 기반 오류 분리

공개 기준 소스와 실제 UI→Provider→Executor→Coordinator→Store→UI 경로의 공통 계약/차이/미검증을 OPEN-SOURCE-FLOW-AUDIT에 기록했다. 판매 여섯 form 필드를 독립 예상 wire 값으로 대조했다. 새 실계정 판매/휴대폰 증거 없이100% 동작/LIVE_SELL_VERIFIED를 선언하지 않는다.

수정한 결함: 전송 전 권한 상실·queue 취소를 Unknown으로 만들던 경로를 RequestNotSent로 분리(POST/Steam command0, 이후Ready 유지). 시장명 없는 비장터 항목/boolean flag의 전체 보관함 파싱 실패를 수정하되 marketable/tradable·가격키를 추정하지 않는다. 공식 공개 JS의 확인 분기와 모순되는 flag는 성공/모바일 대기로 꾸미지 않는다.

ID 없는 요청은 현재 동일 자산의 게시 관측과 원요청 귀속을 별도로 표시한다. 최근5분 내 정확한 자산/금액/수량의 게시 증거가 있으면 그 항목에 계속 휴대폰 승인이 필요하다고 안내하지 않는다. 원래 Awaiting/Unknown·등록ID 미확정·감사 기록은 그대로 보존하고 재전송하지 않는다. 관측은 명시적 서버 대조 시에만 갱신하며 매 진행 이벤트마다 SQLite를 재조회하지 않는다.

실계정/실제 GUI/전역 입력/타 프로세스 접근0. Auth·설정·SQLite·저널·진단·롤백 삭제0. 최초75개47/16/0/3/9 유지.


## 이전 배포 — 0.20.11

소스 **b487acc4df8f19c322fe6d2959fe19849c4b3c10**. `artifacts/releases/MarketCrate-0.20.11-guard-handoff-b487acc-win-x64.zip` **66,534,680바이트 /415파일**. SHA256 **CC83BA7F779935006FF0437794E7E80213B10119379527896832E35149E9285B**. 모든 ZIP entry SHA와 폴더 일치·SourceCommit/버전·x64 CLR/WPF/SQLite/DPAPI·실제 전용 PE/BAML·Mock/자가검증 UI/사용자 DB/Auth/진단/PDB 제외·SSIM LICENSE/NOTICE 전문 포함 및 원본 해시 일치 PASS.

기능367/367·비표시WPF70/70·배포19/19 PASS. 안전 정리fixture8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 새 앱 실행·설치·물리 GUI·실계정·휴대폰/실제 게시0. runtime/ui-tests/guard-handoff-125.png, guard-handoff-200.png, guard-posted.png는 격리된 자체 RTB 합성 이미지로만 검토했다. 최신본0.20.11/롤백0.20.10을 구분했고 이전 배포·Auth·설정·SQLite·저널·진단 삭제0. GitHub Release 바이너리 업로드0. 최초75개 판정47/16/0/3/9 유지.

사용자 최소 절차: 새 ZIP→로그인 복원→실제 보관함/수수료/시세 갱신→대상·가격·수량 최종 검토→전체 판매등록1회→모바일/이메일/실패/불명 결과 확인→공식 Steam 앱/이메일에서 직접 확인→‘승인 후 게시 확인’ 클릭. 알림 도착이나 실제 서버 호환성을 보장하는 배포가 아니며, Unknown 또는 필수 계약 미확인 시 후속 다중 등록을 중단하고 진단을 확인한다. 남은 실계정 증거는 사용자 직접 검증 단계다.


## 0.20.11 — 실제 오픈소스 기준 승인 연결

SSIM MarketModel.ts/AccountTrader.ts/MarketService.ts(28a503fa79869f3233814d0deaa906a8b3f66621), SteamMarketHelper sellItem/removeListing/fetchInventory(e186d51efb742ddfb527d2ccb248c64fcee9c976), 기존 채택한 MIT steam-session LoginSession.ts(3ac0f34fd964b3f886ba18ef4824ac43c942e030)를 실제 소스로 재확인했다. SSIM의 인증 비밀키 자동 확인/프록시·브라우저 지문 위장/재전송을 가져오는 방식은 채택하지 않았다.

SSIM의 canonical market membership 로직을 C# SteamMarketMembership.cs로 부분 이식했다(Apache-2.0·저작권·원본 커밋·LICENSE/NOTICE 보존). pending_listings/listings_to_confirm을 함께 판독하며 동일 중복은 통합하고 충돌은 거부한다. metadata/가격/통화/등록 ID가 빠진 자산도 presence로 보존한다. 이 경우 완전한 금액 행이나 취소 가능한 가짜 등록 ID를 만들지 않는다. active/hold/pending 버킷 자체가 빠진 계약은 여전히 실패 처리하여 부재를 확정 증거로 사용하지 않는다. raw assets map의 추가 자산은 Unknown membership이며 게시 완료로 판정하지 않는다.

Orders의 canonical Presence는 새 등록 전 검사·기존 등록 ID 재사용 검사·취소/반환·재가격 보호가 공유한다. 상세 금액이 없는 승인 대기 자산 때문에 서버 보관함이 새 판매 가능한 상태로 오인되지 않는다. 취소 ID가 사라져도 다른 스택 등록이 남아 있는 상태와 같은 원 취소가 아직 관측되는 상태를 분리하며, 반환 증거는 완화하지 않는다. 실제 수수료/통화/자산/호가/선택 수량·불변 스냅샷·전송 전 intent·직렬 처리·Unknown 보호를 유지한다.

판매 결과에 공식 Steam 앱의 Steam Guard→확인 경로·푸시가 없을 때 확인 목록 직접 열기·이메일 분기·중복 재등록 금지를 안내하고, ‘승인 후 게시 확인’을 기존 Recover 명령에 연결했다. 한 번의 승인→가격별 전체 요청→각 승인 대기→사용자가 공식 모바일 앱에서 직접 확인→새 서버 게시 조회의 경로를 합성 검증한다. sellitem 응답의 needs_mobile_confirmation 등이 필요하며 QR 로그인 승인과 장터 확인은 별개다. 앱이 휴대폰 푸시나 승인 요청 생성을 강제할 수 없고 이메일/승인 불필요 상태도 구분한다. ID 없는 원요청 귀속/상세 금액이 불충분하면 현재 관측과 미확정을 구분하며 성공으로 꾸미지 않는다.

실제 계정/Steam 로그인/판매·취소/Guard/실제 창/전역 입력 실행0. Auth·설정·SQLite·저널·진단·이전 배포 삭제0. 최초75개 판정47/16/0/3/9 유지. IMPLEMENTED와 합성 검증 범위만 확대하며 새 REAL_FEE_VERIFIED/LIVE_SELL_VERIFIED/전 제품 RELEASE_READY는 부여하지 않는다.


## 이전 배포 — 0.20.10

소스 b6d5f58a9a90f917f02663d16ddf5e693506d08f. `artifacts/releases/MarketCrate-0.20.10-server-state-b6d5f58-win-x64.zip` **66,529,683바이트 /415파일**. SHA256 **2DC8291C26D2DD1D549EDD200E3BB8DA03B89F45DBCA68A1AB3B1DBF40C3D418**. ZIP 전체 entry와 출하 폴더 SHA 일치, SourceCommit·버전·x64 CLR/WPF/SQLite·DPAPI·Apache LICENSE/NOTICE·실제 전용 PE/BAML·Mock/자가검증/사용자 DB/Auth/진단/PDB 제외 검사 PASS. 실행파일은 실행하지 않았다.

latest0.20.10, rollback0.20.9(f9da6c6, SHA2561812E5BF7A072828E6292A896D7176699154FF8104C2EE2C5943F1A6EB15C9D1)를 보존했다. 이전0.20.8을 포함한 배포/증거/운영 데이터 삭제0. 원본 진단은 Git/ZIP에 포함하지 않았다. 기능363/363·WPF69/69·배포19/19 PASS, 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 최종 오류 원인 연결 관련29계약 추가 PASS. 새 실제 앱/실계정/판매·취소·물리 UI·Guard는 미검증이다.

사용자 최소 확인: 새 ZIP을 별도 폴더에 풀기→로그인 복원→보관함/판매목록 갱신→대상 선택/시세 갱신→최종 검토→전체 판매등록 한 번 승인→항목별 접수/승인대기/실패/불명 확인. 오래된 개별 자산 불명 기록은 새 서버 확인과 별도 승인으로 진행 가능하며 최근 요청은2분 대기, 불명 스택은 별도 제한된다. 불명 발생 시 연속으로 승인하지 말고 해당 작업 진단을 확인한다. 서버 접수·공식 모바일 수동 승인·실제 게시 재조회는 서로 다른 단계다.


## 0.20.10 — 현재 서버 상태 기반 새 배치와 이력 보존

사용자 0.20.9 진단 253324b5의 작업 67fcb948-c733-42a9-8b25-43eae6531e5e / 배치 0ae502cc-c388-4f07-900d-1f7ccb12f3ef는 SubmitAsync→BatchCreated 뒤 SaleBatchCoordinator.ExecuteAsync에서 Validation/InvalidOperationException으로 중단됐다. 판매 POST나 Guard 접수의 증거가 아니다. 기존 단일 active 배치가 새 배치 ID를 무조건 거부하는 코드 경로를 합성으로 재현했다.

SteamStateStore는 IBatchHistoryJournal로 승인 배치 ID를 조회한다. 검증을 통과한 새 배치의 첫 저장에서 이전 결과를 archive에 보존하고 새 active/result를 같은 SQLite 트랜잭션으로 저장한다. 기존 batch/result schema3를 유지한다. 실패한 사전 검증은 이전 active를 바꾸지 않는다. 같은 배치 ID 재호출/재시작은 보존 결과를 반환하며 전송하지 않는다. 실행기 재생성뿐 아니라 배치 코디네이터도 저장소 범위의 실행 게이트를 공유한다.

일반 UI는 과거 로컬 수량을 차감해 최종 승인 버튼을 영구 비활성화하지 않는다. 최종 승인 시 인증된 최신 보관함·판매/승인대기 목록·수수료·호가·계정·가격을 검증한다. 현재 서버에 소유/marketable인 개별 자산이 판매·승인대기 목록에 없으면 오래된 Unknown 기록이 있어도 **사용자가 새로 승인한 별도 요청**을 허용한다. 원 Unknown/ID/감사 기록은 변경하지 않으며 과거 요청을 재실행하지 않는다. 직전 불명 요청은 로컬 2분 안정화 대기(서버 전파 시간 보장이 아님)를 적용한다. 불명 스택 수량·현재 서버 게시/승인대기·미지원 수수료/통화·접근 제한은 계속 차단한다. 보관함과 판매목록은 원자 스냅샷이 아니므로 이 검사를 통과해도 중복/제재 위험이 0이 되지 않는다. 최종 검토에 해당 위험을 안내한다.

실제 SSIM28a503fa79869f3233814d0deaa906a8b3f66621의 MoneyOps.ts/MarketService.ts를 다시 확인했다. 이미 도입한 Apache-2.0 C# 자산 잠금과 LICENSE/NOTICE를 유지하며, 서버 listed/unconfirmed 사전 검사·작업 종료 잠금 해제 원칙을 적용했다. SSIM의 자동 Guard/재시도/통화 추정 폴백은 채택하지 않았다. 다른 프로젝트의 동작은 Valve 허가나 MarketCrate 실계정 성공의 증거가 아니다.

실제 앱·Steam 계정·판매·취소·Guard 실행0, 사용자 Auth/설정/SQLite/저널/진단/이전 배포 삭제0. 최초75개 판정47 COMPLETE/16 PARTIAL/0 MISSING/3 BLOCKED/9 UNVERIFIED 유지. 이번 서버 증거 없이 LIVE_SELL_VERIFIED/REAL_FEE_VERIFIED를 새로 부여하지 않는다.


## 0.20.9 — 실제 자산 검사와 로컬 중복 전송 보호 분리

사용자0.20.8 진단 작업90910a45-fe77-4c97-8d7f-981ab3b99c3a(54events/1error)는 PrepareTradingAsync의 Wallet HTTP200·KRW 필드/ClassicInventoryContext VerifiedLegacy·Inventory HTTP200/자산1/description1을 통과한 뒤 SaleableAssets에서 Restricted로 멈췄다. 판매 전송·Steam Guard 요청 증거는 없으며 PricePlan Count0은 부족 수량0이지 계획0건이 아니다. 따라서 이번 로그를 수수료 조회 실패나 판매 요청 실패로 판정하지 않는다.

코드에서는 활성화의 자산 조건에 store.Available을 넣어 과거 Unknown intent까지 차감하고 있었다. 이를 합성 재시작 시나리오로 수정 전21개 관련 계약 중1건 실패로 재현했다. 단, 해당 실제 ZIP에는 marketable/locked/로컬 보호 수량이 분리 기록되지 않아 실제 실패가 이 조건 때문인지, 서버 판매 불가 플래그 때문인지는 단정할 수 없다.

활성화는 서버 수량·marketable·locked·지원된 스택 계약으로 판단하도록 수정했다. 과거 로컬 intent는 활성화를 막지 않고 별도 건수/안내로 제공한다. PendingSubmissionAssets는 메모리의 준비 결과이며 저장/거래 권한의 대체가 아니다. 최종 검토·Executor·SQLite intent의 중복 전송 보호는 그대로 유지한다. 기존 Unknown을 Failed로 변경하거나 해제/재전송하지 않는다. 실제 서버 판매 불가 자산은 계속 활성화 차단하며 건수와 이유를 진단한다.

새 안전 진단은 ServerSaleableAssets, InventoryUnmarketable, InventoryLocked, StackQuantityUnsupported, LocalSubmissionPending이다. 원문·자산ID·계정·쿠키·토큰을 추가하지 않는다. 앱/계정/거래/실제 GUI 실행0, 사용자 Auth·DB·설정·저널·진단·이전 배포 삭제0. 실계정 원인/성공·LIVE_SELL_VERIFIED를 확정하지 않는다.75개 판정47/16/0/3/9 유지.

### 이전 배포 — 0.20.8

소스 **96a5d0e6272bc226388b9647935b7b50fc3bc8e2**. `artifacts/releases/MarketCrate-0.20.8-immediate-stack-96a5d0e-win-x64.zip` **66,520,994바이트 /413파일**, SHA256 **21CFD8578BE1948A76694735E8199B421290F6B7C2834C5D8BBD1F2F0BC8746A**. 모든 ZIP entry SHA/폴더/버전·SourceCommit/x64 CLR/WPF/SQLite/DPAPI·라이선스/실제 전용 PE/BAML/평균·기간 선택 UI/사용자 DB/Auth/로그/PDB/Mock 제외 검사 PASS. 기능354/354·비표시WPF69/69·배포19/19·정리fixture9/9 PASS, 빌드 경고/오류0, 스냅샷 총액 등 관련20계약 추가 확인. BAML 출하 검사도 실제 새 출력에서 PASS했다.

latest=0.20.8 /rollback=보존된0.20.7. 기존 Auth/설정/SQLite/저널/진단/이전 배포 삭제0, 실제 앱/계정/판매·취소 실행0. 단일 가격 스택의 전체/부분 수량 계약은 공개 공식 소스+합성 검증이며 실계정 검증이 아니다. 다른 가격 분할 스택·new-mode 수량·ID 변경/합산 반환·ID 없는 원요청 귀속은 제한을 유지한다. 실제 거래 제재 위험과 과거400 원인 미확정은 남아 있다. 로컬 ZIP이며 GitHub Release 게시를 하지 않았다.

## 0.20.8 — 즉시거래가능가 단일 옵션·수량 계약·ID 없는 관측

사용자 요청으로 보관함/판매 중 재등록의 평균·중앙값 가격 선택기와 기간/이력 버튼을 제거하고 **즉시거래가능가만** 제공한다. 단일 옵션을 불필요한 드롭다운 대신 고정 레이블로 표시하며 최종 검토의 직접 가격 수정은 유지한다. Release의 과거 가격 인덱스는0으로 정규화한다. 기존 enum/저장 계약·개발 회귀용 계산/이력 테스트는 보존하지만 일반 UI에서 선택할 수 없다. 최초75개 ID도 삭제하지 않으며 평균 관련 최초 요구는 이번 사용자 요청으로 제품 범위에서 철회됐음을 기록한다.

SteamMarketHelper e186d51의 sellItem(amount=item.amount)와 SSIM28a503f의 sellOnMarket(amount=1)/getListedAssets를 실제 소스로 대조했다. SSIM은 스택 판매의 근거가 아니며, 자동 Guard·비밀키·proxy/fingerprint는 도입하지 않았다. 공식 economy_v2.js를 인증 없이 다시 받아2026-10-29 SHA256 A3BBDF0A135524102CD055FD89D37EB3FD672FAE20BFBA0ABC700648DCB52512와 SellItemDialog의 amount=m_nConfirmedQuantity / price=m_nConfirmedPrice를 확인했다. 코드를 복사하거나 새 런타임/패키지를 추가하지 않고 현재 C# 어댑터에 해당 요청 계약을 적용했다.

인증된 소유 classic 페이지·동일 지갑·앱 수수료 계약으로 검증한 AppID에만 SingleRequestStackApps를 부여한다. 해당 앱의 스택은 **단일 가격·단일 요청**으로 서버 보유 수량 이하의 전체/부분 수량을 전송할 수 있다. 실제 계정 동작은 미검증이다. 여러 가격으로 쪼갠 동일 스택의 연속 전송, 미확인 React/new 수량 계약, 취소 후 ID 변경/합산 반환은 계속 차단한다. 최종 제출 직전 classic 계약이 바뀌면 intent/전송 전 중단한다. 기존 StackAmountsSupported 개발 플래그를 전체 실계정에서 켜지 않는다.

ID 없는 요청은 재조회한 **동일 asset/app/context·상품·수량·통화·가격·수수료**가 하나만 일치할 때 ObservedAssetState/ObservedListingId를 별도 증거로 남긴다. 현재 서버의 판매 중/승인 대기 관측은 표시하지만 이 요청이 그 등록을 만들었다고 확정하지 않는다. 원 command.ListingId를 후보로 채우거나 Unknown을 재전송/해제하지 않는다. 다른 자산/앱/금액·중복 후보는 관측을 확정하지 않는다. 이전400의 상세 원인과 실제 게시 성공은 여전히 미확정이다.

보관함·장터 표시와 로컬 전송 저널 분리, 단일 최종 승인 전체 배치·계정/통화/수수료·중복 전송 보호·DPAPI·기존 schema3/사용자 자료를 유지한다. 실제 GUI/계정/거래 실행0.75개 판정47/16/0/3/9를 실제 검증 없이 승격하지 않는다.

### 이전 배포 — 0.20.7

소스 **139fb933f73a924036d5c8072d4e0d2466d9340d**. `artifacts/releases/MarketCrate-0.20.7-server-sync-139fb93-win-x64.zip` **66,518,925바이트 /413파일**, SHA256 **0E4A336850CCB00F3F069D9C8EECADF21450DAC391970E5CF0B2738CB75212F9**. 전체 ZIP entry SHA·폴더 일치/버전·SourceCommit/x64 CLR/WPF/SQLite/DPAPI/라이선스/실제 전용 PE/BAML·Mock/사용자DB/Auth/로그/PDB 제외 정적 검사 PASS. 기능350/350·비표시WPF68/68·배포19/19·정리fixture9/9 PASS, 빌드 경고/오류0. 마지막 검토 대상 보호 조회를 해시 방식으로 정리한 뒤 관련16계약을 추가 확인했다.

latest=0.20.7 /rollback=보존된0.20.6. 사용자 Auth/SQLite/저널/설정/진단·이전 배포 삭제0. 로컬 ZIP 생성이며 외부 Release 업로드0. 실제 앱/계정/판매·물리 GUI 실행0. TBH는 **보관함 갱신 → 서버1개 표시/선택 → 시세 갱신 → 최종 검토**를 직접 확인하면 된다. 이전 미확정 전송이 있으면 선택/가격 조회는 가능하지만 새 전송 전 근거 확인은 계속 필요하다. 실판매 성공이나 원인 미상400 해결로 판정하지 않는다.

## 0.20.7 — Steam 수량과 로컬 전송 이력 분리

사용자가 지적한 대로 Steam에는 MarketCrate의 '예약' 기능이 없다.0.20.6은 과거 로컬 판매 요청을 실제 보관함 수량에서 차감해 TBH1개를 가용0/선택 불가로 표시했고 안내만 추가해 근본 UX 문제를 남겼다. SteamMarketHelper fetchInventory와 SSIM InventoryService/MarketService·불명 처리 코드를 다시 대조해 이를 수정했다.

이제 Steam이 반환한 수량/판매 가능 플래그로 표시·선택·수량·가격 검토한다. 과거 미확정 전송은 별도 경고로 표시하고 중복 전송 보호는 최종 전송 시에만 유지한다. 정상 판매목록 갱신에서 알려진 등록 ID·상품·금액·수량이 일치하면 저장 상태와 UI 결과를 동기화한다. ID 없는 우연한 일치·목록 부재·단순 스택 증가로 Unknown/반환을 확정하지 않는다. 전송 후 보관함은 오래됨으로 표시해 다음 명시적 갱신 전 값을 최신처럼 표시하지 않는다. 판매목록 안내도 현재 거래/취소 권한과 일치시켰다.

두 프로젝트 기준 전체 기능·UI 호출 경로의 채택/제외/미완성 표는 OPEN-SOURCE-FLOW-AUDIT.md 최신 절에 있다. 실제 코드 수정/합성 검증을 실계정 성공으로 확대하지 않는다. 기능350/350·WPF68/68·배포19/19·정리fixture9/9 PASS. 초기 회귀2건은 기존 로컬 차감 표시를 기대한 것이어서 서버 수량과 독립 전송 보호를 모두 검사하도록 갱신했다. 기존 사용자 Auth/설정/진단/SQLite/저널·Unknown 삭제0, 실제 GUI/Steam 계정 실행0.75개 판정47/16/0/3/9 유지.

### 이전 배포 — 0.20.6

소스 **2aff72d5c480b132c851367abfcace770d6456d2**. `artifacts/releases/MarketCrate-0.20.6-inventory-reservation-2aff72d-win-x64.zip` **66,517,412바이트 /413파일**, SHA256 **84175BE31A9ADD5E52AD6FAA141C83983A99B114D2F9893FB2B9393AFAE8A247**. 모든 ZIP entry SHA/배포 폴더/버전·SourceCommit·CLR/WPF/SQLite x64·DPAPI·라이선스·실제 전용 PE/BAML·사용자DB/Auth/로그/PDB/Mock 제외 정적 검사 PASS. 기능348/348·비표시WPF68/68·배포19/19·정리fixture9/9 PASS, 빌드 경고/오류0. 실제 앱/계정/거래·물리 GUI는 미검증이다.

latest=0.20.6 /rollback=보존된0.20.5. 삭제0·사용자 Auth/설정/진단/SQLite/저널 초기화0. 새 버전은 실제 TBH 예약의 확정 해제나 실판매 성공을 의미하지 않는다. 사용자는 보관함의 **예약된 항목 확인 → 선택 요청 상태 확인**으로 실제 요청을 대조하고, Unknown이면 재전송하지 말고 그 작업의 진단 ZIP을 확인해야 한다. 원본 진단은 보존하고 Git/배포 ZIP에 포함하지 않았다.

## 0.20.6 — TBH 보관함 예약 안내와 이전 요청 연결

사용자0.20.5 진단 작업3c40b785-c97e-4880-87bc-91301fdd7c6f는 HTTP200/JSON 자산1·description1·전체1, 이미지 메타데이터/캐시, VerifiedLegacy 수수료 및 판매목록8건을 정상 확인했다. 해당 ZIP에는 판매 전송/오류가 없고 LoadAsync 약4.71초가 기록됐다. 사용자 화면 '보유1/예약1/가용0'로 선택 제한이 확인됐다. 따라서 이번 현상은 빈 보관함/파서 실패로 판정하지 않는다. 어떤 과거 요청 상태가 TBH 예약을 만들었는지는 해당 ZIP만으로 확정할 수 없고, 이전400과 같은 자산인지도 추측하지 않는다.

보관함의 예약 사유/비활성 툴팁과 '예약된 항목 확인'을 추가했다. UI→현재 계정·표시 자산의 미해제 ReservedSells→기존 요청 자동 선택→판매 결과/상태 대조로 연결한다. 이동만으로 HTTP 전송·예약 해제·저널 삭제를 하지 않는다. 최종 취소 반환 증거가 있는 요청은 예약 목록에서 제외하며 기존 수량 계산·DB schema3는 유지한다. 일부만 예약된 스택의 가용분은 선택/수량 상한을 보존한다. 기존 Auth·설정·저널·진단·배포본 보존, 삭제0. 실계정·실제 GUI 실행0,75개 판정47/16/0/3/9 유지.

### 이전 배포 — 0.20.5

소스 **18e115fb769eae12b15278d48e67336dfa6e396e**. `artifacts/releases/MarketCrate-0.20.5-flow-audit-18e115f-win-x64.zip` **66,515,643바이트 /413파일**, SHA256 **45F14D1B5CB1D0E73DBD8954D00B6799B02753379D30D77DA02ED849E996C698**. 모든 ZIP entry SHA와 폴더 일치·SourceCommit·x64 CLR/WPF/SQLite·DPAPI·라이선스·실제 전용 PE/BAML·개발/Mock/사용자DB/Auth/로그/PDB 제외 검사 PASS. 기능346/346·WPF67/67·배포계약19/19·정리fixture9/9 PASS, 빌드 경고/오류0. 앱 실행·물리 GUI·설치·실계정·실제 속도는 미검증이다.

latest=0.20.5 /rollback=보존된 직전0.20.4. 사용자 Auth·DB·거래 저널·진단 및 이전 배포 삭제0. 로컬 portable ZIP이며 외부 Release 게시를 하지 않았다. 최종 승인 한 번의 전체 배치·Unknown 예약·수동 승인·서버 계약 보호는 유지한다. 새 요청 전 기존 Unknown을 공식 장터/보관함과 대조하고 재전송하지 않는다.

## 0.20.5 — 전체 경로 오픈소스 대조 및 실제 결함 수정

[전체 경로 감사](OPEN-SOURCE-FLOW-AUDIT.md)에 로그인/복원→보관함/목록→호가/이력/수수료→다중 게임 최종 검토→등록→수동 승인→게시 대조→취소/재가격→SQLite 복구를 담당 코드와 연결해 기록했다. 다른 게임 추가 선택 시 기존 권한만 보고 준비를 건너뛰던 결함을 수정했다. helper의 정상 HTTP200 빈 취소 응답은 접수로 지원하며 실제 취소·반환 확인은 별도로 유지한다. 모바일/이메일/불명 승인 채널을 저널과 UI에 구분해 보존한다. 빈 저장 요청 선택기는 숨기고 안내를 표시한다.

기능346/346·비표시WPF67/67·배포계약19/19·정리fixture9/9 PASS. 다중 게임 회귀의 수정 전 실패도 확보했다. 자체 렌더는 모바일/이메일/HTTP400 불명 결과를 분리하며, 물리 입력·실제 팝업·실계정은 미검증이다. SSIM의 비밀키 자동 승인·fingerprint/proxy 모사·반복 재등록은 도입하지 않았다. 이전400의 구체 원인은 미확정이며 Unknown 예약·중복 방지·서버 제한·사용자 자료를 보존한다.75개 판정47/16/0/3/9 유지, 실판매/수수료 실계약 검증 완료 미부여. 아래0.20.4는 이전 배포 근거다.


### 이전 배포 — 0.20.4

최종 소스 **2680afb187b61baac1cd69e1efc1df06769e5dc9**(본수정3f4b74e + 캐시 실패 복구2680afb). **artifacts/releases/MarketCrate-0.20.4-sell-response-2680afb-win-x64.zip**, **66,514,084바이트/413파일**, SHA256 **AD88BC994CF68EF732D65571D74317F2A7195405AF7783B23273D2BF7ECAF1B2**. 전체 entry SHA/폴더/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData·라이선스·실제 전용 PE/BAML·사용자DB/Auth/로그/PDB·Mock/개발 UI 제외 검사 PASS. 기능341/341·비표시WPF66/66·배포계약19/19·정리fixture9/9 PASS, 빌드 경고/오류0.

latest=0.20.4/rollback=직전0.20.3 등록, 모든 사용자 자료와 이전 산출물 삭제0. 실제 앱 실행/설치/실계정 판매/새 버전 속도는 미검증이다. 실거래·이미지 기본 선택 ON은 유지하며 서버 계약 미검증을 강제로 성공 처리하지 않는다. 기존 Unknown은 예약을 보존하므로 재등록하지 말고 공식 장터/보관함과 서버 대조를 먼저 확인한다. 새 요청도 실패하면 해당 실패 작업의 정제 진단을 확인하고 자동/반복 재전송하지 않는다. ZIP은 로컬 생성이고 외부 Release 업로드는 하지 않았다.

## 0.20.4 — 판매 HTTP400·승인 대기 구분·속도

사용자0.20.3 실제 SubmitAsync(작업3724aadb-b310-4535-8d0a-653f732ff32c)는 사전검증 후 sellitem HTTP400을 받았고 Unknown을 영속 저장했다. Steam Guard 필요/접수/게시 성공 응답은 확인되지 않았다. 기존 오류 본문이 없어400의 구체 원인은 미확정이다. 따라서 기존 Unknown 예약을 해제하거나 재전송하지 않았다.

현재 자체 쿠키에서 sessionid를 읽고 정상 inventory Referer/AJAX 헤더를 적용했다. 400을 단순 Transport로 잃지 않고 HTTP 상태와 최대16KiB 본문의 고정 분류만 남긴다. 원문·쿠키·토큰은 로그/진단 ZIP에 없다. 비200의 성공/승인 플래그도 Unknown으로 유지하며 전체 UI 안내에서 승인 대기 미확인을 명시한다.

해당 실제 작업의 첫 조회→400은 약21초였고3초 pacing이 대부분이었다. 읽기 간격은1.5초로 줄이고 판매/취소 간 최소3초는 유지한다. 지갑 계정/수수료 검증의 중복 GET1개를 제거하고, 검증된 canonical URL만 재사용해 반복302 한 번을 줄인다. 본문·가격·지갑/자산·매도 직전 판매목록은 새로 확인한다. 실제 새 버전 속도는 미측정이고 Valve 호출 한도 보장이 아니다. 직렬 큐/403·429·Challenge 중단은 유지한다.

기능341/341·비표시WPF66/66·배포계약19/19·정리fixture9/9 PASS, 빌드 경고/오류0. 수정 전 쿠키 회전/400 진단/헤더3건 FAIL을 재현했다. 기존 실거래·이미지 기본 선택 ON과 최종 배치 승인, Auth/DB/저널·예약/Unknown 보호를 유지한다. 실제 계정/거래/호스트 GUI/타 프로세스 작업0. REAL_FEE_VERIFIED/LIVE_SELL_VERIFIED/RELEASE_READY 미부여, 최초75개 판정47/16/0/3/9 유지. 아래0.20.3은 이전 배포 근거다.

### 이전 배포 — 0.20.3

소스 **4d158896c90be55a5129d263bcd7a22229fbfb9f**. **artifacts/releases/MarketCrate-0.20.3-live-default-4d15889-win-x64.zip**, **66,511,118바이트/413파일**, SHA256 **23E0C2C5E3F413F5ED0292FEC3C59CECCB6CDF9858D7E67EBF4ACE9C4D8673A5**. 전체 ZIP entry SHA/파일 수·SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData·라이선스·실제 전용 PE/BAML·개발/인증/사용자DB/로그 제외 검사 PASS. 기능333/333·비표시WPF66/66·배포계약19/19·정리fixture9/9 PASS, 빌드 경고/오류0. 새 기본 설정과 검증 경로의 실계정/물리 GUI·실판매 성공은 미검증이다.

ignored releases.json의 latest=0.20.3, rollback=실제 존재하는 직전0.20.2로 등록했다. 과거 인덱스가 가리키던0.17 ZIP은 현재 지정 경로에 없어 첫 등록 시도는 중단됐고, 이후0.20.2 롤백으로 전체 무결성 검사를 통과했다. 이전 파일의 부재 원인은 조사/추정하지 않으며 이번 작업에서 운영 자료나 이전 산출물을 삭제하지 않았다. 새 ZIP은 로컬 생성이고 GitHub Release 업로드는 하지 않았다.

## 0.20.3 — 실거래·이미지 기본 선택 및 활성화 실패 진단

최종 자동 검증: 기능333/333·비표시WPF66/66·배포계약19/19·안전정리fixture9/9 PASS, 빌드 경고/오류0. 기본 체크/상태 자체 렌더를 직접 검토했으며 물리 GUI·실계정 활성화·수수료·판매 성공은 미검증이다. 판매 활성화를 무조건 허용한 배포본이 아니다.

실제 소스 대조로 선택적 market_fee 필드 부재를 무조건 차단하던 결함도 수정했다. 최신 classic 앱/실제 지갑 계약을 확인한 경우에만 해당 AppID의 수수료를 허용한다. 명시적 상충·다른 앱·단순 schedule은 계속 차단한다. 수정 전 회귀 FAIL→수정 후 동일 실제 어댑터 합성 승인 대기까지 PASS, 사용자 실계정 원인으로 단정하지 않는다.

사용자 요청으로 일반 제품의 실거래 모드·이미지 표시를 기본 ON으로 변경했다. 이는 서버 검증을 통과한 전송 권한의 자동 저장이 아니다. 최종 가격 검토를 누를 때 아직 권한이 없으면 선택 scope(최대10)의 계정/지갑/수수료/보관함/기존 등록을 검증한다. 최종 전체 판매등록 승인 전 판매·취소 전송은 없다. 모드 OFF와 취소 전용은 자동 승격하지 않는다. 로그인 직후 백그라운드 보관함 조회·거래는 하지 않는다. 이미지 OFF/로그아웃 시 로딩 중단, 새 실행/인증 시 기본 표시 ON이다.

첨부0.20.2 작업07c9c64b-355a-4a02-8de0-77b8abe30542는 호가 HTTP200/JSON/KRW/3단계와 가격 계획 성공뿐이다. PricePlan Count0은 주문 잔량 부족0개이며 판매 계획0건이라는 뜻이 아니다. 활성화 실패 원인은 해당 작업 증거가 없어 미확정이다. 일반 조회 실패 문구로 덮던 UI를 고쳐 단계별 한글 안내를 제공하고, 이후 성공 조회가 있어도 문제 해결은 최근 실패 작업을 우선 선택한다. 서버 검증 보호 조건은 완화하지 않았다.

새 버전은 실제 거래 성공·수수료 실계약 검증으로 판정하지 않는다. 최초75개 판정47/16/0/3/9 유지. Auth/DB/거래 저널·기존 진단/배포본 보존. 최종 검사·새 ZIP은 배포 확정 절에 기록하며 아래0.20.2는 과거 배포 정보다.

### 이전 배포 — 0.20.2

소스 **6c311ec0dde402b96e6ce9d09bbab30d3834ce45**. **artifacts/releases/MarketCrate-0.20.2-classic-fees-6c311ec-win-x64.zip**, **66,508,533바이트/413파일**, SHA256 **C88086A0D1B247187B96952E0B9237457CE0D3B4123103BE453F7F757397B25C**. 전체 ZIP entry SHA/폴더/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData·라이선스·실제 전용 PE/BAML·개발/인증/사용자 DB/로그 제외 PASS. 기능329/329·비표시WPF66/66·배포계약19/19·정리fixture9/9 PASS, 빌드 경고/오류0. 앱/설치/물리입력·실계정 수수료/거래는 미검증이다.

latest 등록, 직전0.20.1/0.20.0과 롤백0.17 및 모든 사용자 자료 보존. 사용자 최소 확인은 새 폴더 배포본의 저장 로그인 복원→지갑·수수료 한 번 조회다. 성공 여부가 확인되기 전 실수수료 해결/실판매 준비 완료로 판정하지 않는다. 바이너리는 로컬 생성으로 GitHub Release에 업로드하지 않았다.

## 0.20.2 — classic 판매창 수수료 경로 지원

검증: 기능329/329·비표시WPF66/66·배포계약19/19·정리fixture9/9 PASS, 빌드 경고/오류0. 실제 앱 실행/설치/물리입력/실서버 수수료는 NOT VERIFIED. 배포 확정 정보는 위 최신 배포 절을 따른다.

실제0.20.1 진단(사용자 작업f67d8e55-5171-4c25-8c53-01a2524fecb8)은 KRW 지갑200/구형 필드5개→ModeMissing→소유 보관함302 vanity 이동→200→다시 ModeMissing이다. 소유/계정 검증 실패나 CAPTCHA·수수료 API 응답 오류가 아니다. 기존 수정으로302 이동 통과가 실제 관측됐다. 페이지의 실제 React 플래그 값은 로그에 없어 최종 원인은 아직 미확정이다.

공식 economy_v2에서 g_bReactSellDialog=false일 때 기존 SellItemDialog를 여는 경로를 확인했다. React data-props만 요구하던 파서의 누락을 수정해 명시적 유일한 false·소유 페이지·현재 인증 계정·Market과 같은 지갑·요청 앱 설정이 확인되는 경우에만 현재 economy_common 정수/가격증분 계산을 사용한다. 플래그 누락/주석·문자열 위장/모드 충돌/지갑·앱 증거 부족은 차단하며 신형 API 실패 때문에 classic을 선택하지 않는다. 토큰·지갑 원문을 남기지 않는 FeeUiObserved 진단을 추가했다.

REAL_FEE_VERIFIED/LIVE_SELL_VERIFIED/RELEASE_READY는 미부여, 최초75개 판정47 COMPLETE/16 PARTIAL/0 MISSING/3 BLOCKED/9 UNVERIFIED 유지. 새 계정 조회·인증·거래·호스트 GUI/타 프로세스 접근은 실행하지 않았고 기존 Auth/DB/저널/설정/진단·배포본을 보존한다. 아래0.20.1 정보는 이전 배포 기록이다.

### 이전 배포 — 0.20.1 (2026-10-29 KST)

소스 **7dfe8282ce44fff8d3ce1c9768310b342428764c**. Windows x64 self-contained 실제 Steam 전용 ZIP: **artifacts/releases/MarketCrate-0.20.1-fee-context-7dfe828-win-x64.zip** (66,507,519바이트 /413파일), SHA256 **014EA96DF1AE13D55F7292220A1291DCD549FD9619BB96EAB8AF693757F4FCB1**. 모든 ZIP entry와 원본 폴더 SHA/파일 수·SourceCommit 검증 PASS. CLR/WPF10.0.9·SQLite x64·ProtectedData·라이선스·인증/사용자DB/로그/디버그/Mock/테스트 UI 출하 제외 검사 PASS. 기능324/324·비표시WPF66/66·배포계약19/19 PASS, 빌드 경고/오류0. 실제 앱 실행·설치·물리 입력·실계정 수수료/거래는 미검증이다.

latest는 ignored artifacts/releases.json에 등록했다. 사용자 관측 롤백0.17.0과 직전0.20.0을 포함한 기존 배포본·모든 사용자 자료는 보존했다. ZIP은 로컬 산출물이며 GitHub Release로 업로드하지 않았다. 최소 확인: 새 폴더에 압축 해제→로그인 복원→TBH 선택/보관함 한 번 갱신→수수료 한 번 갱신. 실제302 목적지가 기존 로그에 없으므로 실패가 남으면 반복하지 말고 해당 작업 진단 ZIP을 내보낸다.

## 0.20.1 — 수수료 조회 이동·빈 선택창·TBH 보완 (2026-10-29 KST)

첨부 진단 ZIP은 원위치에서 필요한 정제 필드만 읽었다. 사용자 작업 87ff3254-3ccf-4185-a35b-2f427b0d0cad에서 Wallet HTTP200/WalletLegacyComplete, ModeMissing, FeeContext HTTP302/OtherRedirect/Unsupported를 확인했다. 이 작업은 수수료 API JSON 실패나 CAPTCHA 증거가 아니다. 기존 로그에 Location 목적지는 없어 실제 이동 원인은 미확정이다.

소유 보관함의 동일 Steam 도메인 정규화 이동을 최대 한 번만 지원한다. vanity 경로는 서버의 g_bViewingOwnProfile=true와 현재 인증 계정 일치까지 확인해야 한다. 다른 계정·외부 호스트·임의 쿼리·로그인/보안 경로·추가 이동은 차단한다. 모드/게임 수수료 증거 부족은 계속 차단하며 구형 수수료로 추측하지 않는다. 실패는 수수료 조회 상태와 판매 중 가격 검토 안내에 반영한다.

재등록 대기 계획이 없을 때 빈 드롭다운 전체 영역을 숨긴다. 실제 계획이 생기면 표시하고 사라지면 다시 숨긴다. TBH: Task Bar Hero(AppID3678970)를 계정 카탈로그에 한 번 추가한다. context2는 설정 가능한 조회 후보이며 실계정 지원은 미검증이다. 직접 제거한 게임을 다음 로그인에 되살리지 않고 다른 게임/context와 사용자 자료를 보존한다. 게임 내부 보유품/파일을 읽지 않는다.

검증: 기능324/324, 비표시WPF66/66, 배포계약19/19 PASS, 빌드 경고/오류0. 빈 선택창 제거와 TBH 자체 렌더 이미지를 직접 검토했다. 실제 계정 수수료 해결·TBH 보관함/context·물리 GUI는 미검증이며 최초75개 판정(47/16/0/3/9)은 유지한다. 새 배포 정보는 아래에 확정한다. 과거0.20.0 기록은 이전 배포 근거로 보존한다.

### 0.20.0 배포 확정

구현 소스 **13a339108d082f04fac81fb98ab193e27d7e9108**(제품 감사3b3f925 + 대량 알림 보완13a3391), Windows x64 self-contained LiveOnly **artifacts/releases/MarketCrate-0.20.0-product-audit-13a3391-win-x64.zip**, **66,504,701바이트 /413파일**. SHA256 **461D4FD228A9D0EE5025F74C0B0B4AF6F2870A67BF07D0966CBD8B229679CC4E**. 원본 `artifacts/releases/product-audit-13a3391-windows-x64`, BuildVersion0.20.0+13a3391…. 전체 ZIP entry SHA/폴더 파일 수/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData·라이선스 검사 PASS. 사용자 DB/Auth/로그/PDB·Mock/자가검증·테스트 모델은 출하 제외이며 실제 진행/작업 선택 타입은 존재한다. 앱/설치/물리 입력·실계정·서명은 실행·검증하지 않았다.

최종 **기능315/315·비표시WPF65/65·배포계약19/19·정리 fixture9/9 PASS**, Debug/Release 경고·오류0. 최초75개47 COMPLETE/16 PARTIAL/0 MISSING/3 BLOCKED/9 UNVERIFIED. 실수수료/스택/실판매·물리GUI의 부족한 근거를 COMPLETE로 바꾸지 않았다. 실제 Steam 로그인·조회·판매·취소/Guard 실행0·호스트 GUI/타 프로세스 접근0이다.

latest/rollback은 ignored artifacts/releases.json에 기록했다. 롤백0.17.0(session-1700bfd ZIP/SHA A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC)과 직전0.19.0·0.18.0 및 모든 운영 자료를 보존했다. 바이너리는 로컬 생성이며 GitHub Release로 업로드하지 않았다. 사용자 최소 확인은 로그인 복원→보관함/판매목록 갱신→카드·목록/게임 전환 선택 유지→가격 검토, 취소 필요 시 '실제 취소' 전용 활성화→전체 취소 검토→선택 요청 상태 확인이다. 실제 계정 실행은 사용자가 위험과 최신 서버 계약을 확인한 후 직접 수행한다.


0.20 마지막 대량 갱신 보완: 동일 상품10,000개에 SetPrice를 적용할 때 각 행의 Changed가 전체 선택 요약을 다시 계산하던 경로를 기존 Bulk로 묶었다. PriceNotifications 합성 회귀는10,000개 가격 전부 반영·요약 알림10회 미만·거래 전송0을 확인한다. 이는 실행 시간/CPU 개선 비율을 측정한 벤치마크는 아니다. 최종 기준315개 기능/65개 비표시WPF이며 아래 중간312·314 기록과 구분한다.

### 0.20 최종 자동 검증

**기능315/315·비표시WPF65/65·배포계약19/19·안전 정리 fixture9/9 PASS**, Debug/Release 경고·오류0. 기준0.19의305/61과 전 회귀를 유지했다. 신규10기능/4WPF에는 실시간 진행, 취소 대조·취소 전용, 원자 저장 오류 주입, 이전 계획 선택, 로그아웃 늦은 결과, 캐시 선택 보존, 계정 보관 보호와50개 Command Binding 검사가 포함된다. 배포 PE/BAML에서 실제 서비스·진행/작업 선택 타입 존재와 Mock/자가검증/테스트 타입 부재를 검사한다. 자체 RTB의 새 내비게이션·카드/목록·compact 검토·진단 복귀를 직접 열람했다. 개발 구성의 자가검증 탭은 Release에서 제외되며 렌더와 실제 출하 검사를 구분한다.

제한 실행에서 개발 전용 JSON 저널 파일 교체1건은 FAIL이었다. 동일 바이너리 정상 권한312/312 이후, 마지막 선택·보관 가드를 포함한 최종315/315 정상 권한 검사를 확인했다. 실제 GUI/물리 입력/Popup/모니터DPI·장시간 리소스·실서버 수수료/거래는 미검증이다. 실제 창/로그인·인증/조회·판매·취소·Guard 실행0, 타 프로세스/호스트 입력·화면/설정 접근0, 운영 데이터·이전 배포본 삭제0이다. ZIP/SourceCommit/해시는 배포 확정 절에 추가한다.


## 0.20.0 — 전체 요구사항·UI 연결 감사

75개 기존 ID를 유지해 현재 제품 경로를 다시 대조했다. 판정은 **47 COMPLETE /16 PARTIAL /0 MISSING /3 BLOCKED /9 UNVERIFIED**다. B08은 수수료·보관함과 독립된 취소 전용 활성화/전체 검토/실제 어댑터 경로를 구현해 BLOCKED→PARTIAL로 변경했다. 실계정 취소 성공으로 승격한 것은 아니다. 정확 평균 원자료·게임별 실수수료·스택/반환 인과·실제 거래·물리 GUI/배포 실행 검증은 남았다.

수정: 8게임/20,000행 화면 캐시에서 빠진 게임의 선택 대상 누락, 배치 종료 전 UI 결과 미갱신, 사전검증 실패 시 재등록 계획 조기 소비, 최신 재등록 계획만 선택 가능, 취소 후 서버 대조 UI 단절, 다른 계정 배치 보관 방지, 응답 계약 차단인데 재시도 가능으로 표시하던 상태를 보완했다. 새 배치와 재등록 계획은 SQLite 동일 트랜잭션이며 기존 스키마3/Auth/설정·저널을 초기화하지 않는다.

공개 SteamHelper/CSlytics 실제 화면과 SSIM 진행 모델을 참고해 중첩 최상위 탭을 제거하고 보관함→판매 중→판매 결과→계정→고급 설정 순서로 정리했다. 실제 제품 보관함에 카드/목록 전환·10,000항목 가상화, 게임 관리 직접 진입, 작업별 취소 재검토 선택, 간결한 가격 행, 건수별 실시간 결과를 연결했다. 장터와 진단 전환은 기존 뷰/선택을 유지한다. 제품 XAML3개의 Command Binding50개를 실제 ICommand 객체와 대조한다. 이는 물리 입력·실계정 검증과 다르다.

새 자동·Release/ZIP 결과는 아래 배포 확정 기록에 추가한다. 실제 계정 로그인/읽기·판매·취소·Guard와 최상위 창/호스트 입력은 Codex 미실행이다. 공개 페이지 PNG3개는 비교용 runtime에만 보존하고 배포/소스에 복사하지 않았다.


### 0.19.0 배포 확정 — 사용자 결함·새 자산 재등록

구현 소스 **cb2c32685cef5c6acb9c3d5faf87f39acd48d65c**, Windows x64 self-contained LiveOnly portable **artifacts/releases/MarketCrate-0.19.0-relist-cb2c326-win-x64.zip**, **66,495,539바이트 /413파일**. SHA256 **42D92ECDDF23BE4789150B52C1585A802C71BBBD31E9AED8155E3A1AE86528D6**. 원본 `artifacts/releases/relist-cb2c326-windows-x64`, BuildVersion0.19.0+cb2c326…. 전체 ZIP entry SHA/폴더 파일 수/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData·라이선스 일치, 사용자 DB/Auth/로그/PDB 제외·실제 UI/SteamRelistPlan 존재·Mock/자가검증 모델/BAML 부재 검사 PASS. 앱/설치/서명·실제 계정/거래/물리 입력은 Codex 미실행이다.

최종 자동 **기능305/305·비표시WPF61/61·배포계약19/19 PASS**, Debug/Release 경고·오류0. 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 환경제약)를 유지했다. 새 재등록 경로6기능/1WPF를 포함해 기존 전 회귀를 유지하며 OperationId/저널·Unknown/새 승인 전 판매0·민감정보 미노출을 검사했다. detached RTB의 loading/combo/product-empty/whole-management/새 자산 ID 검토125%·200%를 직접 열람했다. 최초75개 판정 **47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED** 유지, 실거래 전체 완료/REAL_FEE_VERIFIED/LIVE_SELL_VERIFIED/RELEASE_READY를 부여하지 않았다.

최신본/롤백은 ignored artifacts/releases.json에 기록했다. 롤백은 사용자 로그인 복원이 관측된0.17.0(session-1700bfd ZIP/SHA A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC)이며, 직전0.18.0 및 나머지 이전본도 모두 보존했다. 사용자 Auth/설정/SQLite/의도·예약/진단 증거 삭제0. 바이너리는 로컬 생성이며 GitHub Release 업로드가 아니다.

사용자 최소 확인: 새 폴더에서 직접 실행→저장 로그인 복원→미확인3기본 게임 없음/Steam 커뮤니티 이름·드롭다운 정렬·작업 중 아이콘/문구 확인→지갑/수수료 수동 갱신→판매목록 선택/즉시거래가능가 일괄 검토. 실제 취소/새 등록은 세션 동의와 현재 서버 계약을 통과한 사용자가 직접 실행한다. 취소 후 새 대상 재검토 시 기존 보유분일 수 있는 실제 자산 ID·새 가격·수량을 다시 확인해야 하며, 취소 불명·수수료/통화·자산/호가 불일치는 전송하지 않는다. 지정 경로에 없었던 진단 ZIP은 다시 첨부해야 실제 오류를 분석할 수 있다.


## 0.19.0 추가 — 외부 등록의 새 자산 재판매

SteamMarketHelper의 취소→보관함 재조회→같은 상품의 새 자산 ID 선택 방식을 실제 서비스에 연결했다. 전체 취소 승인 후 서버 접수·목록 부재를 확인하고, 실제 보관함·가용 수량·수수료·공유 호가로 새 대상을 제안한다. 원래 아이템의 반환 확정은 아니며 다른 기존 보유분일 수 있음을 표시한다. 새 실제 자산 ID·수량·가격을 전체 검토해 판매등록 한 번 승인하면 기존 Coordinator가 배치 처리한다. 항목별 추가 판매 클릭은 없다. SQLite 취소 계획/새 배치 연결을 보존하고 불명 취소·원래 예약은 추측으로 해제하지 않는다. 최근 미제출 계획은 사용자 요청 시 재조회하며 자동 거래하지 않는다. 개별 수량1, 최대100등록/10상품만 지원한다.

추가 합성6개/비표시WPF1개: 변경ID·재시작 후 새 승인, 취소만으로 판매0, Unknown 재전송0, 전체 수량 부족·호가 변경 차단, 계획 불변·OperationId/비밀정보 보호. 최종 기능305/305·WPF61/61·배포19/19 PASS, 경고/오류0. 취소/새 대상 검토125%·200% RTB를 직접 검토했다. 실제 계정/수수료·판매/물리 입력은 미검증이다. 상세 호출 경로·보존/재시도 제약은 [판매 실행 계약](SALE-EXECUTION-SPEC.md)의 0.19 추가 절을 따른다.


0.19 최종 자동 검증: **기능305/305·비표시 WPF61/61·배포계약19/19 PASS**, 경고·오류0. 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 제약). 기존286/56 전 회귀 유지. 신규19기능/5WPF에 재등록 Guard→서버 Listed 읽기 대조(추가 전송0), 타 계정 저널 불변 검사를 포함했다. 재등록 재조회는 현재 계정 최근 최대10건이며 다른 의도는 선택 대조한다. RTB usability-loading/combo-alignment/product-empty/bulk-review-small/200을 직접 검토했다. 물리 입력/팝업/모니터 DPI·장시간 안정성/실계정은 미검증. 최종 로컬 ZIP/해시는 배포 확정 절 참조.

## 0.19.0 — 사용자 발견 결함과 일괄 재등록 검토 (2026-10-29)

사용자가 원하지 않는 게임은 CS2·Dota2·TF2 기본 항목임을 명확히 확인했다. 일반 앱에서 미확인3추천을 제거하고 기본 Steam 커뮤니티(753/context6) 경로만 제공한다. 기존 추천 저장 자료는 숨김 보존하고 직접 추가/판매목록 관측은 Pinned 메타데이터로 구분한다. 게임 구매 여부를 증명하는 목록이 아니다. App753 이름을 Steam 커뮤니티로 바로잡고 조회 후 상태/판매 가능 수 알림 누락을 수정했다.

드롭다운 텍스트·벡터 화살표를 수직 가운데 정렬했다. 작업 중 전역/검토창의 Busy 아이콘·작업명·조회/처리 건수·중단 명령, 로딩/빈 목록/조회 실패 안내를 분리했다. 회전은 보이는 활성 컨트롤에만20fps로 동작하고 숨김/완료 시 중단한다. 명시적 움직임 축소 설정은 보존하며 그때는 정적 아이콘과 작업 문구/건수로 표시한다. 미설정 사용자의 기본은 회전 허용이다. 승인 후 가격 편집은 잠근다.

| 요구/상태 | 실제 지원과 미완성 |
|---|---|
| 판매 중 기준별 일괄 검토 IMPLEMENTED | 선택→즉시거래가능가→상품별 호가1회/공유 잔량→가격별 단가·Steam/게임 수수료/실수령→전체 검토1회. 최대100등록/10상품, 현재 개별 수량1만 지원 |
| 일괄 재등록 SYNTHETIC_VERIFIED / PARTIAL | MarketCrate 원등록 저널이 있는 단일 자산만, 전체 서버/수수료/호가 사전검증→순차 취소/인과 반환→새 등록. Guard 상태를 원래 명령에서 표시한다. 불명 발생 시 나머지는 미전송으로 보존 |
| 외부 기존 등록·반환ID 변경 PARTIAL | 전체 취소 후 새 실제 자산·새 가격을 다시 전체 승인하는 경로 지원. 원래 자산의 반환 인과관계 자동 확정은 BLOCKED. ID/이름·수량 증가만으로 원래 예약을 해제하지 않음 |
| 최근 평균거래가 BLOCKED | 실제 체결 원자료가 없어 정확 평균 기준 자동 재등록을 제공하지 못한다. 선택 시 이유를 명시하며 중앙값/최저 호가로 대체하지 않는다 |
| 실제 오류 진단 NOT VERIFIED | 첨부0c9de…ZIP은 지정 경로에 없어 읽지 못했다. 서버 오류 원인은 추측하지 않으며 재첨부가 필요하다 |
| 실거래/수수료/물리GUI NOT VERIFIED | Codex 실제 로그인·판매·취소·창 실행0. 기존 실사용 성공 범위와 구분. 새 버전 전체 운영 준비 완료 판정 없음 |

최초75개는 로컬 검증 범위47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED 유지하며 '75개 제품 완성'의 의미가 아니다. 알려진 실사용 차단은 평균 원자료/실제 수수료 모드·공식 금액 대조/외부·변경ID 반환/스택/실제 거래 검증이다. 운영 Auth/설정/저널/진단 삭제0, 실거래 매 실행 OFF·Unknown 재전송0 유지. 최종 테스트/배포 해시를 확정 절에 기록한다.


### 0.18.0 배포 확정 — 수수료 모드/제품 화면

구현 커밋 **e6ee865861adbfaf46d91ae8a6680b2a0a7c2cb7**, origin/main push 완료. 새 로컬 portable ZIP **artifacts/releases/MarketCrate-0.18.0-fee-mode-e6ee865-win-x64.zip**, **66,478,248바이트 /413파일**. SHA256 **552CF3704C5A0B1A6C176351A314B7F2BE5922E09F8ABD8346C6F36B2556F44B**. 원본 `artifacts/releases/fee-mode-e6ee865-windows-x64`, BuildVersion0.18.0+e6ee865…. CLR/WPF10.0.9·SQLite x64·ProtectedData·기존 MIT 고지 포함, 경고·오류0. 전체 ZIP entry SHA와 소스 폴더 일치, 버전/commit/구성/라이선스·사용자 DB/로그/Auth/PDB 제외 검사 PASS. PE/BAML에서 Mock/자가검증 화면·모델 부재와 실제 resolver/Auth/거래 서비스 존재를 확인했다. 실제 EXE/설치·서명/계정·거래는 Codex 미실행이다.

최종 기능 **286/286**, 비표시 WPF **56/56**, 배포 계약 **19/19 PASS**. 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 제약). 사용자 운영 파일/배포본 정리0. 소스 변경 경로/키·JWT literal 검사에서 민감/산출물 경로0·실제 비밀값0을 확인했다. 기록은 ignored runtime/product17-*.txt/자체 RTB에 있고 개인 자료를 Git/배포로 전송하지 않았다.

롤백은 사용자 로그인 복원이 관측된 **0.17.0** ZIP, SHA256 **A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC**이며 이전0.16.1·사용자 진단/저널/Auth/설정도 보존했다. latest/rollback은 ignored artifacts/releases.json에 기록한다. 바이너리는 로컬 생성이며 GitHub Release로 업로드한 것은 아니다.

IMPLEMENTED/SYNTHETIC_VERIFIED는 위 범위이며 REAL_READ_VERIFIED는 사용자0.17 관측에 한정한다. 새 실제 수수료 모드/공식3구간 금액/등록·게시·취소·스택·새 버전 복원/물리 GUI는 NOT VERIFIED. **REAL_FEE_VERIFIED·LIVE_SELL_VERIFIED·RELEASE_READY 미달**, 최초75개47/15/0/4/9 유지. 사용자 최소 재확인은 새 폴더에서 직접 실행→로그인 복원→지갑·수수료1회→공식 화면3구간 대조이며 실패 시 거래/반복 조회 없이 문제 해결 ZIP을 내보낸다.

## 17차 / 0.18.0 — 수수료 모드와 실사용 화면

**구현:** FeeContractResolver(VerifiedLegacy/VerifiedNew/Unsupported/Unverified/Restricted), 공식 sell props와 현재 지갑·앱 context 대조, 조건부 fee API 호출. FeesAsync·실거래 사전검증·배치 제출·재가격3곳을 같은 resolver로 연결했다. Wallet 통화 관측과 수수료 실패를 분리해 지갑 단독 조회를 유지한다. 자산 publisher 비율 누락/충돌은 검토 전에 차단한다. 기존 등록이 없더라도 최신 공식 모드/서버 지갑·앱·품목 계약을 충족하는 경우만 사전검증 가능하며, 기존 목록의 수수료 충돌 veto는 유지한다.

정식 UI에서 전용 자가검증 페이지·범위/동의·임시 실행 명령/모델을 컴파일 제외했다. 일반 계정/보관함/목록/가격/수수료/최종 검토/등록·취소·재가격/결과/로그는 보존했다. 상시 내부 코드·OperationId를 제거하고 문제 해결→진단 ZIP으로 모았다. 공식 수수료 비교는3구간×4금액, 게임·통화·시각·최소 가격/수수료 구간·증분별 일치를 표시하며 입력은 거래 활성화 근거가 아니다.

**새 사용자 실환경 증거(0.17.0):** 종료/재실행 후 저장 로그인 복원 성공, QR/Community 성공, 판매목록8건, CS2 730/context2 명시적 빈 보관함, KRW wallet 객체/기존 수수료5필드 관측, fee API200 HtmlMarket. 호가/이력/이미지는 선택 품목 없음으로 미검증이다. 과거 아래 '복원 미검증'은 당시 개발자 검증 범위이며 이번 사용자 보고로 0.17 복원 한 경로를 보완한다. 새0.18 복원·장기 만료/회전·타 Windows 사용자까지 확대한 것은 아니다.

| 판정 | 현재 증거 |
|---|---|
| IMPLEMENTED | 모드 resolver/정수 견적/단일 승인 배치·저널·Guard 대기·대조·취소/재가격·로그인 유지·실제 전용 UI 코드 |
| SYNTHETIC_VERIFIED | 기능286/286, 비표시WPF56/56, 배포계약19/19 PASS. 경고/오류0. 새 수수료 모드16개 및 기존 전 회귀 포함 |
| REAL_READ_VERIFIED | 사용자의0.17 로그인 복원·목록 첫8건·명시적 빈CS2·KRW wallet 관측에 한함. 모든 읽기 지원 완료 아님 |
| REAL_FEE_VERIFIED | **NOT VERIFIED**. 실제 sell props/newFeeRates·공식 UI3구간·API JSON 계약 미확보 |
| LIVE_SELL_VERIFIED | **NOT VERIFIED**. Codex 실계정/등록·취소0 |
| RELEASE_READY | **미달**. 실제 수수료/실판매·새 버전 물리 GUI/실행 검증이 남음. 정적 배포 성공과 구별 |

75개 판정47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED 유지. 운영 Auth/설정/SQLite/Unknown 저널/진단·과거 배포본 삭제0. 실거래 매 실행 OFF, 자동 승인/재전송0. 상세 근거는 STEAM-INTEGRATION-FEASIBILITY와 QA-MATRIX. 새 ZIP/해시는 배포 확정 절에 추가한다.


### 0.17.0 배포 확정 — 로그인 유지

구현 소스 **1700bfdc6cb3cf0df984e7c76b0a14fff8558372**, origin/main push. Windows x64 self-contained LiveOnly portable **artifacts/releases/MarketCrate-0.17.0-session-1700bfd-win-x64.zip**, **66,486,397바이트 /413파일**, SHA256 **A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC**. 원본 `artifacts/releases/session-1700bfd-windows-x64`, BuildVersion0.17.0+1700bfdc6cb3cf0df984e7c76b0a14fff8558372. 전체 entry SHA/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData DLL/steam-session MIT 전문/고지·인증/DB/로그/개발파일 제외 검사 PASS. PE/BAML은 Mock 부재와 실제 Shell/Steam/AuthStore/refresh-token 검증 코드 존재를 확인했다. 경고·오류0, 실제 EXE/설치/서명은 미실행이다.

최종 자동: 기능270/270·자체WPF55/55·배포계약19/19 PASS, 안전 fixture 정리8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 환경제약). 실제 Windows 자체 CurrentUser DPAPI/ACL은 합성 비밀로 검증했다. 실제 Steam 저장 토큰 재사용·서버 회전/철회·수수료/판매·물리GUI·다른 Windows 사용자 복호화·전원 차단 시험은 미실행/미검증이다. LIVE_VERIFIED를 부여하지 않는다. 최초75개 판정47/15/0/4/9 유지, 실거래는 매 실행 OFF다.

롤백0.16.1 ZIP/SHA **94F7E423D49AA0F0AF6C4651A50486F95548A93B45EAE850B488480C70012C2F** 보존(완전 실사용/실거래 검증이라는 의미 아님), 사용자 관측0.16.0 및 그 이전 배포본/설정/SQLite/저널/진단/운영 인증 자료도 보존했다. 최신/롤백은 ignored artifacts/releases.json에 기록했다. 사용자 자료 삭제0·운영 Auth 접근0·실제 계정/GUI/타 프로세스/전역 입력/화면/시스템 설정 접근0. 최소 사용자 확인은 SESSION-PERSISTENCE의 'QR 전 유지ON→인증/저장 확인→로그아웃 없이 종료/재실행→직접 읽기 확인→명시적 로그아웃' 순서다. 토큰/쿠키/Auth 폴더는 진단 전달 대상이 아니다.

### 0.17.0 최종 자동 검증

기능 **270/270 PASS**, 자체 비표시 WPF **55/55 PASS**, 배포 파일계약 **19/19 PASS**, 안전 fixture 정리 **8 PASS/0 FAIL/1 NOT VERIFIED**(실제 junction 생성 환경제약). 최종 빌드 경고·오류0. 신규 세션20/자체UI3개와 기존 전 회귀 포함. 직접 Windows CurrentUser DPAPI·ACL/암호문만 저장, 로그·검토ZIP canary 미노출, 인증 복원 시 Unknown 저널/예약 보존·거래0을 확인했다. 마지막 저장 예외 분류/계정 검증을 반영한 최종 source 증거는 runtime/session-build.txt·session-core.txt·session-ui.txt·session-distribution.txt·session-cleanup.txt에 있다.

RTB session-optin/session-restored-small/large/session-paused를 직접 확인했다. 실제 Windows 사용자 간 복호화·실제 GUI/물리입력·전원 차단·Steam 저장 토큰 재사용/회전·수수료·판매/취소는 NOT VERIFIED. NuGet 취약성 메타데이터를 끄거나 SDK/Windows 설정을 설치/변경하지 않았다. 최초75개 판정47/15/0/4/9 유지. 기존 운영 데이터/진단/릴리즈 삭제0. Release·ZIP 해시와 커밋은 확정 기록에 추가한다.

## 16차 / 0.17.0 — 동의 기반 Steam 로그인 유지

현재0.16.1의 실제 전용 구조를 유지하며 MIT steam-session의 WebBrowser token 검증/getWebCookies 경로를 C#으로 제한 이식했다. 로그인 유지 기본OFF, 사용자가 QR 전 선택하면 DPAPI CurrentUser·사용자 전용 ACL로 Auth에 암호화 저장하고 다음 시작에서 한 번만 새 Community 세션을 발급받는다. 앱 종료는 저장을 보존하고 명시적 로그아웃은 로컬 저장/메모리/거래 권한을 정리한다. 로컬 삭제가 서버 철회라는 주장은 하지 않는다.

실패/만료/거절/손상/중단은 자동 반복하지 않으며 미완료 파일/구 토큰 fallback도 없다. 자동 보관함/목록 갱신·거래0, Unknown 저널/예약 보존, 실거래는 매 실행 OFF다. 계정·쿠키 일치 확인 전 인증 완료가 아니다. 저장 실패는 유지OFF로 안내하며 현재 QR 인증 성공을 지우지 않는다.

실제 서버의 저장 토큰 재사용/자동 회전/철회·수수료/KRW·스택/판매·취소와 물리GUI는 아직 미검증이다. WebBrowser에 맞지 않는 GenerateAccessTokenForApp를 강행하지 않으며 자동 Refresh Token 회전은 미지원, 만료/철회 시 새 QR이 필요하다. 수수료 HTML 문제를 새 로그인 기능의 성공으로 덮어쓰지 않는다. 최초75개47/15/0/4/9 유지. SESSION-PERSISTENCE의 최소 사용자 확인 절차를 따른다. 최종 테스트·배포는 확정 기록으로 추가한다.

### 0.16.1 배포 확정 — 수수료 HTML 오분류 수정

소스 **f5ed123aff841b85fa9a6865fa5a1f4a886004a9**, origin/main push 완료. 최신 Windows x64 self-contained LiveOnly portable: `artifacts/releases/MarketCrate-0.16.1-fees-f5ed123-win-x64.zip` (**66,475,258바이트 /412파일**), SHA256 **94F7E423D49AA0F0AF6C4651A50486F95548A93B45EAE850B488480C70012C2F**. 원본 폴더 `artifacts/releases/fee-f5ed123-windows-x64`, BuildVersion0.16.1+f5ed123aff841b85fa9a6865fa5a1f4a886004a9. Release 빌드·실제 전용 metadata/BAML·CLR/WPF10.0.9/SQLite x64·라이선스·민감/개발파일 제외·전체ZIP entry SHA 일치 검사 PASS. 실제 EXE/설치/서명·실계정/거래 미실행. 초기 NuGet 감사 메타데이터의 sandbox 네트워크 실패는 정상 접근으로 재빌드하여 해소했으며 감사/경고 검사를 끄지 않았다.

최종 기능250/250·WPF52/52·배포계약14/14 PASS, 경고·오류0. 안전정리7 PASS/1 NOT VERIFIED 유지(실제 junction 환경제약), 사용자 산출물 삭제0. 롤백은0.16.0 ZIP/SHA1E758055EB23B5060EEE57BA6E819AEC323E2B698A5AC8518EF307FE48E9704A이며0.15.1 이전본·진단/DB/저널/설정/인증자료도 보존했다. 최신/롤백은 ignored artifacts/releases.json에 기록했다. 바이너리/진단/개인 DB는 Git에 넣지 않는다.

**이 배포는 실제 수수료 성공을 확인한 버전이 아니다.** 확인된 HTML=Challenge 오분류/안전 처리/원인 진단을 수정했다. 실제 사용자 HTML 종류와 계정의 newFeeRates 적용/수수료 계약은 새 버전에서 지갑·수수료만1회 사용자 승인 조회한 정제 ZIP으로 확인해야 한다. 미확인 금액·통화·수수료의 거래는 계속 차단, 제재 위험/LIVE_VERIFIED 미확정 유지. 최초75개 판정47/15/0/4/9 변경없음. 앱·타 프로세스/화면/전역 입력 접근0.

### 0.16.1 최종 자동 검증

기능 **250/250 PASS**, 자체 비표시WPF **52/52 PASS**, 배포 파일계약 **14/14 PASS**, 최종FAIL0·빌드 경고/오류0. 안전정리7 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 환경제약), 이번 사용자 배포/데이터 정리0. 신규 수수료/복구 오류 주입·JSONL 원인 연결/비밀 canary 미노출 및 기존 거래 어댑터·가격·SQLite/Unknown 회귀 포함. runtime/fee-final-core.txt·fee-final-ui.txt·fee-final-build.txt·fee-distribution-contracts.txt·fee-cleanup-contracts.txt에 로컬 증거를 남겼다. RTB fee-response-error-row/fee-comparison-small/large·오류 화면 직접검토, 물리GUI/모니터DPI/설치·실계정 수수료/거래·서명 미실행. 최초75개 판정47/15/0/4/9 유지. 중간1회 기존 복구 검사 실패/미확정 원인과 결정적 게이트 보강은 WORKLOG에 보존했다. Release 정적 검사와 실제 실행 검증을 혼동하지 않는다.

## 0.16.1 — 수수료 응답 계약 분리와 비교 도구

사용자0.16.0 진단으로 eligibility 왕복 후 Wallet HTTP200/객체 파싱 성공이 관측됐다. 목록 첫8건·CS2 서버총0건은 기존 범위만 인정한다. Fees200 HTML의 실제 내용/Content-Type은 구 로그에 없어 서버 쪽 원인은 미확정이다. 확인된 코드 결함은 'JSON 기대 경로의 모든 HTML=Challenge'라는 오분류이며 이를 수정했다.

- SteamHttp→SteamResponseContract는 선언 media/본문 형식/고정 HTML 표식을 별도3005 이벤트로 기록. 장터 HTML은 수수료 endpoint 계약오류, 로그인 폼은 해당 조회 권한, 보안 위젯/접근거부는 안전중단, 분류불가 HTML은 계약미확정 안전중단. HTML/200만으로 성공·CAPTCHA·세션 만료를 추측하지 않는다. 같은 endpoint는 재전송하지 않는다.
- 공식 sell dialog는 newFeeRates 조건에서만 현재 fee GET을 사용한다. 기존 지갑 fee 필드는 관측 정보로 분리하며, 기본 비율/단위·구형 모드를 추측해 자동 판매용 계약으로 대체하지 않는다. 유사 프로그램의 고정 CS2/USD floor 계산을 KRW에 이식하지 않았다.
- 고급 설정의 공식 장터 단가 비교는 현재 게임의 조회된 정확한 fee schedule이 있을 때만 가능. 수동 입력은 판매계획/권한에 반영하지 않고 불일치/입력오류를 표시한다. 수수료 미확인 상태에서는 버튼이 비활성화된다.
- 인증/조회 계약 오류의 상단 안내도 분리했다. 실제 전용 Release/Mock 컴파일 제외·기본 거래OFF·저널·Unknown/예약/중복 보호 유지. 실제 fee/읽기 전체/거래/KRW/스택의 LIVE_VERIFIED는 없음. 실계정 수수료 계약이 확인되지 않으면 전송을 계속 차단한다.

최종 검증·ZIP/커밋은 아래 확정 기록에 기재한다. 최초75개는47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED 유지한다. 사용자0.16.0 Wallet 관측은 0.16.1의 실계정 수수료/판매 성공을 뜻하지 않는다.

## 0.16.0 — 실제 Steam 전용 배포 및 자격 확인 처리

### 0.16.0 LiveOnly 배포 확정

소스 **85756b4f541175cbd388d51c3d3d2fb1349e84e7** origin/main push. 새 x64 self-contained Release: `artifacts/releases/MarketCrate-0.16.0-live-85756b4-win-x64.zip` (66,469,582바이트/412파일), SHA256 **1E758055EB23B5060EEE57BA6E819AEC323E2B698A5AC8518EF307FE48E9704A**. 전체 entry SHA/SourceCommit·CLR/WPF10.0.9·SQLite/라이선스/민감·개발파일 제외 검사 PASS. RuntimeMode=LiveOnly, 필수 PE/BAML 검사로 Mock 구현/모델/구 화면·재시도/구 USD fixture 파서가 없는지 확인했다. 경고·오류0. 실제 EXE/설치·실계정/거래·서명은 미실행이다.

최신/롤백은 ignored artifacts/releases.json에 기록, rollback0.15.1의 SHA106E89E7C801250C1C264F92DDC2BE87710A70CD3591BD09B38ADDFD9BE820F2 재확인. 0.15.0/0.14/0.13과 기존 추출본·사용자 진단 ZIP/DB/저널/설정/인증·NuGet 캐시는 삭제하지 않았다. 실제 지갑의 성공/금액 계약은 사용자 확인이 필요하며 실패를 합성 성공으로 바꾸지 않는다. 최종 기능235/235·자체WPF50/50·배포계약14/14 PASS(개발 구성 회귀), Release 별도 빌드/메타데이터/전체ZIP 무결성 검사 PASS. 마지막 Shell 페이지 진단 이벤트 연결은 Release 빌드에서 확인했다.

새 사용자 진단은 Wallet302의 목적지가 RedirectCommunity / RedirectMarketOther / RedirectOtherQuery임을 보여준다. 공식 자격 확인 경로는 해당 분류에 부합한다. 원문 경로가 없으므로 동일 경로였다고 확정하지 않는다. 공식 공개 eligibilitycheck/goto=Market를 무인증 GET하여302→고정 Market 복귀를 확인했고, 이 정상 프로토콜을 기존 코드가 모두 차단하던 누락을 수정했다.

Wallet 흐름은 canonical 이동1회·자격 확인1회·정확한 goto 복귀1회(최대3 hop)만 처리한다. 서버 Set-Cookie를 앱 자체 메모리 세션에서 수신하며 쿠키 위조/타 앱 추출은 없다. goto는 고정 Market root/기존 허용언어만, 중첩·외부·추가쿼리는 거부한다. 자격 확인에서200으로 멈추면 권한 미확인으로 Wallet만 차단한다. 401/403/429·로그인/보안/알 수 없는 이동·순환은 중단하고 데이터 생성/자동 재시도하지 않는다.

일반 시작 경로를 ProductionShellViewModel/ProductionShellView로 변경했다. Demo 저장소를 열거나 Fake 데이터를 만들지 않는다. 실제 Steam 공급자/검토/저널/복구와 진단만 연결한다. Release에서 MainView/MainViewModel·DemoDataProvider/SqliteDemoStore·Mock 실행/관리/모델/재시도·구 USD fixture 파서를 컴파일 제외한다. 테스트 구성만 개발용 Mock 소스를 유지한다. Release의 격리 Mock 인자 및 합성 handler/모드 주입도 거부한다.

비밀정보 없는 설정은 새 사용자별 Preferences/preferences.sqlite3에 저장한다. 기존 Demo DB는 열거나 삭제하지 않는다. SteamExperimental의 기존 실거래 저널 경로는 유지한다. 새 설정의 손상/미지원 버전은 초기화하지 않고 오류로 중단한다.

회귀: 기능235/235·자체 WPF50/50·배포계약14/14 PASS, 경고·오류0. 정리7 PASS/1 NOT VERIFIED(실제junction 생성불가) 유지. Release 메타데이터/BAML에서 Mock 구현 부재와 실제 서비스 존재를 확인했고, 개발 DLL을 넣으면 검사 거부됨을 확인했다. 자체 렌더에서 연결/진단/작은 창 배치를 직접 검토했다. 실계정 지갑 성공·거래·물리 GUI·설치·DPI·서명은 미검증이며 Codex가 실행하지 않았다. ‘실제 전용 구성’은 LIVE_VERIFIED/제품 전체 완료를 뜻하지 않는다. 최초75개 판정47/15/0/4/9 유지.

## 0.15.1 — 사용자 진단 ZIP의 Wallet302 후속 수정

0.15.1 배포: 소스 d2ee9a4679e24854102f09355ff2d5f125f8c05a, `artifacts/releases/MarketCrate-0.15.1-wallet-d2ee9a4-win-x64.zip` (66,507,841바이트/412파일). SHA256 `106E89E7C801250C1C264F92DDC2BE87710A70CD3591BD09B38ADDFD9BE820F2`. Release/전체entry SHA/SourceCommit·CLR/WPF/SQLite·민감/개발파일 제외검사 PASS, 경고오류0. 실제EXE/지갑/계정은 미실행이며 해결완료 판정 아님. 0.15.0을rollback으로등록하고0.14/0.13·기존추출본/진단ZIP·DB/저널/인증/설정은보존했다.

검증: 기능227/227·비표시WPF48/48·배포계약14/14 PASS, FAIL0·경고오류0. 정리검사7 PASS/1 NOT VERIFIED(실제junction 환경제약) 유지. runtime/wallet-patch-verify.txt 및 최종core 재검사 증거. 실제Steam지갑 성공은 미검증이며 Codex 실계정/앱/거래 실행0. 최초75개 판정과 거래보호 변경없음.

제공된 두 ZIP은 각각0.14.0/0.15.0의 동일경로를 보여준다: 세션기록 성공→판매목록8→CS2 명시적자산0→Wallet HTTP302/OtherRedirect/Unsupported. WalletContract/ResponseBody 단계에 도달하지 않았으므로 지갑파싱·지갑미설정·인증만료로 단정할 수 없다. 실제 Location은 구버전 로그에서 제거되어 목적지 미확정이다. 사용자 ZIP은 제자리에서 읽기만 했고 원문·계정자료를 저장소/외부로 전송하지 않았다.

공식 공개 /market 및 /market/를 쿠키없이 확인했으며 둘다200·익명wallet없음이다. /market도유효한데 기존단일이동허용목록은 /market/만받던 누락을 수정했다. 같은origin/HTTPS·고정언어query·한번만 허용, 임의query/인증/보안/다른host/반복이동과401/403/429/CAPTCHA 제한은 유지한다. **실제 사용자목적지가 /market이었는지는 미확정이며 실지갑 해결완료를 주장하지 않는다.**

새 진단은 Host역할/Path유형/Query유형과hop0/1만화이트리스트로 남긴다. 원본Location/host/path/query값/fragment·인증정보는 저장하지 않는다. 미지원302 결과는 ‘다른페이지로 이동해 중단’으로 설명하고 로그인실패나 지갑없음으로 오인하지 않게 한다. 기존세션·독립조회·거래OFF·저널/Unknown 보호 유지. 사용자 직접 지갑범위만1회확인이 다음단계다.

## 15차 — 조건부 비공식 실거래 경로·최종 승인 보호 (2026-10-29 KST)

### 0.15.0 배포 확정

소스 **eb9ebd7ff79585dfbcd20ba17374eb4c4e155031** origin/main push. portable **artifacts/releases/MarketCrate-0.15.0-stage15-eb9ebd7-win-x64.zip** (66,506,796바이트/412파일), SHA256 **EF520920413636DC1456C30A3C94F6ADAD03C08D0082156ED0544983520F43D5**. 전체 ZIP entry SHA와 원본/SourceCommit 일치, x64 CLR/WPF10.0.9·SQLite 네이티브·라이선스 포함, DB/로그/인증정보/개발파일 제외, Release 경고·오류0. 실제 EXE·설치·실계정은 미실행, 디지털서명 미적용.

최신/롤백은 ignored artifacts/releases.json에 기록. 롤백은0.14 ZIP(SHA E1A001291F4AE0F6BE0C5410FA51421BD95E52612472BD9C187890CBED034680), 기존0.13 ZIP·모든추출본/진단/SQLite/저널/설정/인증/캐시는 그대로 보존했다. 이번 차수의 기존산출물 삭제0(성공한 신규합성fixture 자체정리만). 마지막 생성자 권한검사를 DB열기 앞으로 옮겨 차단시 lease잔류를 막고 활성화재요청은 먼저 권한철회하도록 한 뒤 전체기능223/223 재검사(runtime/stage15-last-core.txt), 기존48WPF/14배포검사와 새Release검사 유지. 동의해제·재로그인/계정변경 활성화보존 없음.

### 15차 최종 자동 검증

기능 **223/223 PASS**, 자체비표시WPF **48/48 PASS**, 배포파일계약 **14/14 PASS**, FAIL0, Debug 경고/오류0. 안전정리 **7 PASS/0 FAIL/1 NOT VERIFIED**(실제junction 생성불가, 기존경계/잠금/변조검사유지). 신규16기능/2WPF는 실제어댑터의 합성HTTP 주입이며 사용자모드의 조건부활성화→Guard대기→수동게시대조→취소반환→재가격·감사저널도 검증했다. runtime/stage15-release-verify.txt가 최종전체회귀 증거이고 runtime/stage15-focused-final.txt는 추가user-mode saga 검사 증거다. RTB stage15-trading-controls.png·stage15-multi-game-review.png 직접검토. Codex 실계정/거래·실제창/물리입력/Popup/DPI/설치·서명 미실행. 최초75개판정47/15/0/4/9 유지. 실제 Wallet302/등록취소/KRW/스택 계약·LIVE_VERIFIED는 미확정.

0.15.0. 사용자0.14 관측: 세션 존재/목록 첫8건, CS2 HTTP200 명시0건, Wallet302 OtherRedirect/Unsupported. 목록·보관함 관측만 인정한다. Wallet 실제 Location/인증된 HTML 증거는 없으므로 원인과 전체읽기 계약은 미확정이다.

- Wallet은 단일 same-origin HTTPS /market/ 정규/공개언어 이동만 지원(빈query 또는 l=english/koreana). POST/로그인/보안/지역·다른host/임의query는 따라가지 않고 두번째redirect도 중단한다. 401/403/429/CAPTCHA 우회 없음. 공개 Market은 익명200, 실제302 목적지라고 단정하지 않는다.
- 사용자 요청으로 제품 정책을 변경: 실제 거래 기본OFF 유지, 고급설정의 위험동의+‘실제 거래’ 입력+사용자 시작 읽기 preflight를 거쳐 이 실행 세션만 비공식 거래모드를 선택 가능. Valve 허용 근거와 동일시하지 않는다. 기존 AGENTS의 허가확인 필수 규칙에 대한 이번 명시적 사용자 요청의 예외를 AGENTS에 기록했다.
- 활성화 필수조건: QR Community 연결, Market g_steamID와 QR계정 일치/로그인플래그, 통화·명시적 minimum/increment, strict 앱fee, marketable/잠금없는 비스택자산, 같은게임 기존 실제등록의 수수료 비교. 증거 없으면 무전송 차단. 기존 등록이 없는 계정의 첫bootstrap은 아직 제한된다. 스택/중앙값 제출은 기본차단. 활성화 게임은 세션별 allowlist, 새게임은 별도 preflight. 개발용 합성모드/초기policy와 분리, 동의해제·로그아웃·새인증은 거래권한 철회. 실제서버 계약이 충분히 검증됐다는 판정은 아님.
- 여러게임/context를 scope별 최신보관함·게임fee·통화/자산으로 전체검증하고 한 배치에서 직렬실행. UI는 방문한게임의 선택/수량을 보존해 전체 검토에 포함, 상품(App/hash)별 호가잔량 공유. 모든 품목의 호가지문을 승인snapshot에 고정하고 변경 시 첫intent 전 StaleReview·새검토 요구. 통화·fee·보유량/marketable·시간 불일치는 무전송. 낮은가격 자동대체 없음.
- 취소배치 전체 대상 사전대조 후 개별전송. 응답이 요청전 기존listingID를 재사용하면 Unknown. 취소/반환 인과증거 없는 스택/ID변경은 Unknown·재등록 차단. 재가격의 승인호가지문을 intent에 저장하고 취소 전/반환 후 비교. 기존 journal v3에 optional필드만 추가, 구 요청/예약/증거 보존. 미확정/ID없는 요청 자동재전송 금지.
- IMPLEMENTED: 조건부 활성화→검토→단일승인 배치·Guard대기→수동조회 대조·취소/조건부 재가격 코드와 UI. CONTRACT_VERIFIED: 합성계약만, 실제서버 금액/등록·취소·스택은 미검증. LIVE_VERIFIED: 없음. 실사용 제품 전체완료 선언 안 함. 기본OFF/실계정시험·GUI 실행0.
- 검증 최종집계/배포결과는 아래 확정기록으로 갱신한다. 최초75개47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED 유지. 물리GUI/DPI/설치·계정제재 위험/first-bootstrap·스택반환 인과증거·실서버302 및 KRW계약은 다음 차단요인이다.

### 14차 최종 배포·정리 결과

구현커밋 **3e5201cd5980a4bf36fb25e7dbb6f7cb5e81960f**, origin/main push 완료. 새 **0.14.0 win-x64 self-contained**: `artifacts/releases/MarketCrate-0.14.0-stage14-3e5201c-win-x64.zip` (66,498,491바이트,412파일). SHA256 **E1A001291F4AE0F6BE0C5410FA51421BD95E52612472BD9C187890CBED034680**. 원본폴더 `artifacts/releases/stage14-3e5201c-windows-x64`의 모든파일과 ZIP entry hash 일치, ProductVersion/SourceCommit·CLR/WPF10.0.9·SQLite x64·라이선스·DB/로그/인증/개발파일 제외 PASS. Release 경고·오류0. EXE/설치/물리GUI·실계정은 Codex 미실행, 서명 미적용.

검증된 release index 생성 후 dry-run11폴더/242파일을 재검증해 **45,650바이트(44.58KiB)** 제거, skipped0. 프로젝트생성22파일짜리 exact 비실행 합성 fixture만 대상. 큰 배포폴더는 실행중 여부/사용자내용 불확실성 때문에 보존했으며 수GB 정리를 주장하지 않는다. 롤백0.13 ZIP `artifacts/MarketCrate-0.13.0-stage13-dd7e276-win-x64.zip` SHA256 **2B76B0ACB3440CCBA5F2C2364AAD46BA458D10E89E329A8456400A0ED83BD7C8** 재확인, 사용자추출본·이전실행폴더·진단·SQLite/저널/설정/인증·캐시·LOCALAPPDATA 보존. dry-run/적용증거는 ignored `artifacts/cleanup-plan-f8ca226ee39e4e5aa6149f38d3fb6690.json`, `artifacts/cleanup-result-755a1b5fc521421bb440139a33344cb5.json`. 최신/롤백경로는 `artifacts/releases.json`.

## 14차 — 개별 읽기 권한·빈 응답 검증·안전 정리 (2026-10-29 KST)

0.14.0. 사용자0.13 관측: QR 인증기록 성공, 판매목록 첫8건/활성8건, CS2 730/2 첫0건·marketable0, 지갑 인증확인 차단 및 후속차단. 부분 실계정 증거이며 전체 계약 검증이 아니다. Codex는 실제 인증·계정조회·거래·앱 실행을 하지 않았다.

- 확인된 코드 결함: GET의 모든 redirect/API오류를 Authentication으로 묶던 처리, 불완전한 inventory 응답을 정상0건으로 받아들이던 처리. 알려진 Steam 로그인redirect/API15는 endpoint Authorization으로 제한하고 세션을 보존한다. 다른 공식redirect는 Unsupported·무추적, 위험redirect는 안전중단. 401/만료27·403/429/CAPTCHA·불명 서버제한은 기존 보호 유지. 재전송/우회 없음.
- 사용자 지갑실패 원인은 미확정(실패응답/ZIP 미제공). 공식 공개JS는 wallet_currency와 선택적 minimum/increment 및 fee비율을 소비한다. 익명 Market200의 wallet marker 부재는 사용자 인증실패의 증거가 아니다. 새 WalletObservation은 필드존재/로그인플래그를 구분하며 통화·가격단위를 생성하지 않는다. 통화만 확인되면 통화관측만 성공, 수수료금액/판매준비는 미검증. 엄격한 실제 money contract 유지.
- 정상 빈 inventory는 명시적 success/total0·추가페이지 없음·모순 없음 필요. 필드누락/타입/총계모순은 ContractMismatch/Parsing, 페이지일부와 marketable0/보유0 구분. 사용자 CS2 실제0건의 원인은 기존 요약만으로 판정 불가.
- 자가검증 상단 성공/실패/차단/미검증 요약·다음행동, 행별 접힌 작업상세(분류/시각/OperationId). 전역제한과 개별권한 분리로 독립조회 보존. 한 실행의 Authorization 이후는 차단·무전송, 독립조회는 사용자가 별도로 선택한다.
- 기능207/207·비표시WPF46/46·배포계약14/14 PASS, FAIL0, 경고/오류0. 안전정리7 PASS/0 FAIL/1 NOT VERIFIED(실제junction 생성불가). RTB 요약/상세 직접검토, 실제창/물리입력/Popup/DPI/설치 대체 아님. 최초75개47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED 유지.
- 새 release검증 후 exact 합성fixture만 정리. 실행폴더·0.13 ZIP/추출본·진단·SQLite/저널·설정/계정자료·캐시·LOCALAPPDATA 보존. 최신/롤백은 artifacts/releases.json에 명시. 실거래 IntegrationBlocked, LIVE/READ_ONLY_READY 승격 없음. 비공식 자동화 제재위험 유지.

## 13차 — 기존 인증 활용 검토와 읽기 전용 자가검증 (2026-10-29 KST)

13차 배포: 구현 `dd7e27635104c02fe53c0e282242e9d598c04a4b` origin/main push. **0.13.0 x64 self-contained Release/정적 파일검사 PASS**, 경고·오류0, CLR/WPF10.0.9·SQLite 네이티브·라이선스 포함. `artifacts/MarketCrate-0.13.0-stage13-dd7e276-win-x64.zip` **78.31MiB/412파일**, ZIP 전체 원본 hash 및 ProductVersion/SourceCommit 일치. SHA256 `2B76B0ACB3440CCBA5F2C2364AAD46BA458D10E89E329A8456400A0ED83BD7C8`. DB·로그·인증·PDB/개발파일 제외, 이전 ZIP·사용자 데이터 보존. 실제 EXE/서버 로그인·읽기·거래·설치·서명 미실행. 최소 사용자 절차는 READ-ONLY-VALIDATION의0.13.0절차를 따른다.

기준 main/origin3684d07 clean/fetch 일치. AGENTS/정의/상태/작업기록·인증/읽기/진단 호출 경로 확인. 공식 문서·공개 QR 정의만 GET, 실제 앱/계정 인증·조회·거래·타 프로그램 접근0.

- 공식 OpenID는 브라우저에서 SteamID를 확인하는 수단이고 Community 장터 인증 권한을 주는 경로로 확인되지 않았다. OAuth는 Valve Client ID/권한 협의가 필요한 제한된 서비스다. 기존 세션을 데스크톱 앱으로 전달하는 공식 공개 경로를 찾지 못했으므로 쿠키·클라이언트 파일·자격 증명·프로세스를 읽는 재사용은 구현하지 않았다. 공식 장터 직접 관리 버튼은 사용자 브라우저 안에서만 기존 로그인을 활용한다. 앱 OpenID/OAuth 로그인은 미구현이며 기존 QR 실험 어댑터만 유지한다.
- 새 연결 자가검증 페이지: 7개 선택 범위 기본OFF, 실행별 명시 승인, 범위·게임·품목 변경 시 승인 해제, 실행 시 불변 plan·승인 소비, 실행 중 범위 잠금/중단. 세션 기록·목록·게임 보관함·marketable·지갑·fee·호가·이력·이미지 9결과. 성공은 관측 범위, 실패/차단/미지원/미검증/중단 구분. 각 행 UTC/KST·다음조치·OperationId.
- 목록/보관함은 각각1페이지만 검사, 전체 목록 캐시·선택/수량·저널을 대체하지 않는다. 가격·이미지 캐시는 기존 어댑터대로 채울 수 있다. 선택한 한 품목이 없으면 호가/이력/이미지는 미검증·무요청. fee는 허용된 지갑 조회 성공에만 의존. 403/429/CAPTCHA/인증 요구·안전 latch 시 나머지 미전송. 단순 parse/404/503은 독립 결과만 실패하고 다른 허용된 읽기는 진행, 자동 재시도0.
- 실제 코드 결함 수정: g_rgWalletInfo 변수 부재만으로 Authentication을 추측하던 Wallet 파서를 ContractChanged로 분리. QR Community transfer 결과1에 더해 비어 있지 않고 Secure/미만료인 쿠키를 요구. 지갑·호가 통화/보관함 publisher fee·서버 fee 불일치를 별도 원인으로 보고한다. 새 필드/로그인 경로를 추측해 생성하지 않았다.
- 전체 기능 **199/199**, 비표시 WPF **45/45**, 배포 파일계약 **14/14 PASS**, FAIL0/Debug 경고0·오류0 (runtime/stage13-verified.txt). 기존187/42/14 유지. 실제 QR 발급/승인/최종 세션·읽기 응답/이미지 CDN/물리 GUI·DPI/설치·서명은 미검증. 실제 KRW/스택/판매·취소 계약, 허용 근거는 미확보, 실거래 IntegrationBlocked 및 최초75개 **47/15/0/4/9** 유지.

## 12차 — 로그인/조회 복구 및 Steam UI 재설계 (2026-10-28 KST)

12차 배포: 구현 `49a3ffad9a8b37aa4085d6ace6191ddbd035430e` origin/main push. **0.12.0 Windows x64 self-contained Release PASS**, 경고·오류0, CLR/WPF10.0.9·SQLite 네이티브/고지 포함, DB·로그·세션·PDB·개발파일 제외. `artifacts/MarketCrate-0.12.0-stage12-49a3ffa-win-x64.zip` **78.30MiB/412파일**, ZIP 전체 파일 SHA256가 원본과 일치. SHA256 `EAE969B83190F2EB9A29F7299AD2B879DC7D699502D631E960A6F10308986408`. ProductVersion/SourceCommit 일치. 앱/인증/실거래/설치·서명은 실행하지 않았고 기존 배포본·사용자 데이터는 보존했다.

사용자 최소 확인:① 보관함/판매/진단 탭의 콘텐츠 버튼·체크·입력·드롭다운 위를 이동해 Header 깜빡임이 없는지, Tab/Shift+Tab 점선과 선택 밑줄이 구분되는지 확인.② 직접 QR 인증을 선택한 경우 인증 완료 카드와 개별 조회 상태를 확인.③ 보관함 파싱/404 실패 시 인증 완료가 유지되고 독립 목록/fee 버튼이 동작하는지 확인(401이면 재로그인,403/429/CAPTCHA면 차단이 정상).④ 게임 전환·선택/수량·스크롤·가격 검토를 확인하되 실등록·취소·재가격은 실행하지 않는다. 실패가 남으면 고급 설정의 작업 ID→진단 검토 ZIP으로 원인을 좁히고 QR/원문 응답은 공유하지 않는다.

12차 최종 회귀: **기능187/187 PASS · 비표시 WPF42/42 PASS · 배포 파일 계약14/14 PASS · FAIL0 · Debug 경고0/오류0** (`scripts/Verify.ps1`, ignored runtime/stage12-verified.txt). 기존177/39/14 검사와 거래/Unknown/SQLite 회귀 유지. 사용자 제보의 실제 Steam 보관함 실패 응답은 미확보이므로 해당 실서버 파서 계약이 해결됐다고 보장하지 않는다. 수정한 것은 실패의 범위·인증 소실·UI 동작이며 실계정 파서/응답 형식 검증은 별도다. 실제 앱/입력/Popup/물리 DPI/Steam 인증·거래·설치 미실행.

main/origin `4210269` clean/fetch 일치에서 시작. 인증 네트워크·실거래·실제 GUI/Popup·전역 입력·타 프로세스 접근 없이 진행. 사용자 0.11.0 문제 제보를 바탕으로 재현 검사를 먼저 작성했다.

- 확인된 결함: 일반 read 파서가 공용 SteamHttp.Stop을 호출하고 VM.Error가 LoginState를 덮어씀. Authentication.Connected도 모든 Halted에 묶여 독립 조회까지 비활성화. 이제 서버 결과/Community 세션에 근거한 HasSession/Connected, 공용 요청 제한, 개별 ReadState를 분리했다. 단순 파싱/404/GET 통신 실패는 해당 조회만 실패; 401/인증 만료는 세션 정리·재로그인, 403/429/CAPTCHA/중복 페이지 등 안전 latch는 유지. POST 응답 불명/Unknown/거래 코디네이터 정지 보호 유지, 자동 재시도 없음.
- Inventory/Listings/Fees/Orderbook/History 5개 상태. 로딩 중 기존 목록 보존, fee 실패 시 기존 fee를 검토에 재사용하지 않음. 한 번의 Load 내부에서도 fee 실패 뒤 독립 listings 계속 가능(서버 latch면 중단). 현재 Inventory/Prices/Fees 조건이 불완전하면 최종 검토 명령 비활성. 인증 완료 카드/일부 조회 실패/세션 만료/요청 제한을 분리 표시한다.
- TabItem 전체 IsMouseOver 대신 템플릿 Header Border B만 Hover를 소유한다. 콘텐츠·하위 버튼/체크/드롭다운/입력 Hover와 Header를 분리. Selected 밑줄·키보드 점선은 유지. 실제 물리 깜빡임을 캡처하거나 재현한 것이 아니라 코드 구조와 제어된 DP 검사로 결함을 확인·수정했다.
- Steam 5개 화면 재구성: 계정·조회 상태 / 3열 보관함 / 독립 판매 관리 / 결과 / 고급 설정. 긴 안내·AppID/context·OperationId는 고급으로 이동. 가격 기준·즉시거래가능가/수수료·실수령 예상·부족 수량·최종 검토를 고정 패널로 분리. 기본 컨트롤·가상화 유지, 이미지 안내 한 줄 기호/접근성 툴팁, 선택/게임별 자료/스크롤 보존. view cache 최대8게임/20,000행, 초과 시 오래된 뷰만 제거하며 선택·수량 기억과 저널은 보존.
- ReadState4203/고정 endpoint/OperationId와 기존 HTTP·파서·SQLite 진단 연결. 원문·토큰·QR·계정 정보 제외. 최초75개 ID/판정 **47 COMPLETE / 15 PARTIAL / 0 MISSING / 4 BLOCKED / 9 UNVERIFIED** 유지. 실계정/현재 물리 GUI/DPI·Popup·설치·서명·KRW·스택 실서버 계약은 계속 미검증, 거래 IntegrationBlocked.

## 11차 — 사용자 발견 QR 계약·상태 강조 결함 수정 (2026-10-28 KST)

11차 배포: 구현 커밋 `0705dc2874b64b521e86dd92b3847c1a1b63219c` origin/main push 완료. **0.11.0** self-contained Windows x64 Release 빌드·정적 검사 PASS, 출력 경고/오류0. CLR/WPF10.0.9, x64 apphost/SQLite 네이티브 DLL/라이선스 고지 포함, DB·로그·인증·PDB·개발파일 제외. `artifacts/MarketCrate-0.11.0-stage11-0705dc2-win-x64.zip` **78.29MiB/412파일**, ZIP 전 항목과 원본 SHA256 일치. SHA256 `C3A5AEC49ACB4B8FEA93D4496E49E4D7AB92F2FB7F409F5EA22F1E43A60AEE28`. distribution.json 및 ProductVersion=0.11.0+0705dc2…의 SourceCommit 확인. 실제 EXE/설치/서명/실서버 QR 발급은 미실행. 이전 배포본·사용자별 저장소를 덮어쓰거나 초기화하지 않았다.

기준 main/origin `62663c5` clean/fetch 일치. 실제 인증·앱·Popup·포커스·입력·타 프로세스 접근 없이 공개 정의 GET 및 합성/비표시 검사만 수행했다. IntegrationBlocked/기존 SQLite·저널·수량 예약 계약 변경 없음.

- QR 요청: 공개 HTTP API 메타데이터가 required로 선언한 최상위 device_friendly_name(1,string), platform_type(2,enum WebBrowser=2)을 device_details(3,message)의 내부1/2와 함께 명시. input_protobuf_encoded 표준 base64→UTF8 form 인코딩, 별도 format=json/Accept를 독립 decoder로 검증. uint64 client_id는 문자열·정확한 정수 JSON 모두 처리하고 소수 interval 허용. 누락·범위·URL 이상은 차단한다.
- 확인된 코드 결함: 최상위1/2 누락, 숫자 JSON client_id 거부. 사용자 ContractFailure가 어느 원인인지는 사용자 진단/인증 응답 없이 확정 불가. 실제 BeginAuth 호출/QR 인증은 실행하지 않았으므로 서버에서 해결됐다고 판정하지 않는다.
- AuthContract2005: AuthBegin/HTTP/ApiResult/ResponseParse/ResponseFields/ChallengeValidation 실패 구분, 고정 endpoint/OperationId/안전한 x-eresult만 기록. HTTP200 API 거절을 성공으로 기록하던 문제 수정. QR·토큰·쿠키·원문 본문 제외.
- UI: Action VSM과 Trigger의 중복 속성 제어 제거. Hover/Pressed 배경, Selected 밑줄·선택면, Checked 체크마크, KeyboardFocus 점선 분리. 자체 subtree 입력 방식만 관찰해 마우스 클릭 포커스와 키보드 탐색 구분. Disabled 포인터 효과 차단, ComboBox 이중 opacity 제거. 기본 스타일·키보드 접근성 유지.
- 기존 DP 상태 왕복에서는 사용자 물리 Hover 잔류를 재현하지 못했다. 중복 제어와 상태 시각 혼동은 확인했으며, 실제 IsMouseOver 오작동/브러시 공유 오염을 확인된 원인으로 주장하지 않는다. 비표시 검사는 제어된 DP Trigger와 명시적 VSM 전이이며 물리 입력 검증이 아니다.
- 최초75개 **47 COMPLETE / 15 PARTIAL / 0 MISSING / 4 BLOCKED / 9 UNVERIFIED** 유지. 실계정·현재 GUI·물리 DPI·Popup·실판매는 미검증/차단. 사용자 기존7개 수동 PASS를 이번 빌드 전체 PASS로 재사용하지 않는다.

## 10차 — Steam 사용자 경로 연결 / 즉시거래가능가 (2026-10-28 KST)

기준 main/origin `a0e7c95` clean/fetch. 사용자 보고: **9차 기본 GUI7항목 PASS**(앱실행, hover/click, 선택·수량, 가격전환·검토, Mock배치, 취소·재가격·진단, 재시작 보존). 정확한 테스트 빌드/OS/DPI·24개 전체 결과는 제공되지 않았고 10차 현재 빌드 GUI 성공으로 승격하지 않는다. Codex는 실제 앱/창/Steam 인증·API·거래·Guard를 실행하지 않았다.

- PriceDisplay 공통 표시/설명과 PriceBasis 저장키: 주 모드 `즉시거래가능가`, 기존 한국어 setting alias는 읽고 다음 변경 시 Immediate/ExactAverage 저장. enum/Steam 인덱스/배분/기존 저널 변경 없음. 정확평균 원자료 없음/중앙값 추정 분리. 현재 매수주문 기준 예상이며 실제 즉시 체결 보장 없음.
- 정상 portable 앱 시작은 Steam 탭을 보여주되 인증/조회는 자동 실행하지 않는다. 격리QA는 계속 offline Mock. QR 서버 회전 시 UI matrix 갱신, 빈 기본값 유지/만료 후 재로그인, 연결 중 실수로 새 로그인 시작 차단. logout/인증만료의 이전 데이터 보존·오래된 검토 차단.
- 판매목록/지갑·fee를 보관함과 독립 조회 가능. 404 보관함이 account 판매목록 조회를 막지 않는다(403/429중단은 유지). fee 없어도 취소 **검토만** 가능, 실제 제출은 원래 policy/preflight. 판매목록 시세는 현재 모드로 조회, 최고 구매주문/잔량·현재 단가 차이/중앙값 기간 구분.
- ReadContract4202의 고정 endpoint/count/currency/time/OperationId만 UI/JSONL에 연결. 원문·필드덤프/계정·asset·QR·쿠키·token 없음. 실제 응답이 아니라면 Synthetic으로 표시. 관측 성공을 READ_ONLY_READY로 자동 판정하지 않는다.
- 공식 Steam 장터 버튼은 사용자 클릭 때 기본 브라우저의 고정 공식 URL만 연다. Codex/테스트는 대체 delegate만 사용. 해당 수동 흐름이 단일 승인 앱 일괄 등록 완료를 뜻하지 않는다.
- 최종 기능172/172·detached WPF33/33·파일계약14/14 PASS, FAIL0/Debug 경고0·오류0. 구현 `e5db266` origin/main push 완료. 해당 소스 self-contained x64 Release PASS(경고0/오류0), CLR/WPF10.0.9·SQLite/고지/데이터·PDB 제외/Core commit 확인. `artifacts/MarketCrate-stage10-e5db266-win-x64.zip` 78.3MiB/412개 원본·ZIP hash 일치, 앱 미실행. SHA256 E14B0BA88D314276F485AEB2D8E97A64604B02F7EA7D5A77207C4DC122EC765F. 75개 **47 COMPLETE / 15 PARTIAL / 0 MISSING / 4 BLOCKED / 9 UNVERIFIED** 유지. 실서버·KRW fee·stack/반환 인과·실제 쓰기·현재 GUI·서명/실환경 출시 기준은 여전히 미검증/차단.

## 9차 — 배포·진단 보완, 실제 GUI QA 차단 유지 (2026-10-28 KST)

기준 main/origin `9cc73a4` clean/fetch 일치. 제공된 게스트 없음. 허용된 조회에서 EditionID=Core(Home계열), build26200.9711, X64, Sandbox 실행파일 없음. CIM 가상화 capability 조회는 권한 제한(CimException)으로 미확인; 다른 경로로 반복·권한 상승·VM/프로세스 탐색하지 않음. 공식 지원표상 Home은 Sandbox 미지원. **환경 제공 전 실제 GUI QA BLOCKED, GUI-01~24 모두 NOT VERIFIED(PASS0/FAIL0/NV24).** 호스트 대체 실행/설정 변경/VM 실행 없음.

- 기존 FDD 유지 + `Publish-SelfContained.ps1`: win-x64, CLR/WPF10.0.9·SQLite 포함, 비단일파일/trim·R2R OFF, app PDB/개발소스 제외. 런타임 팩은 공식 NuGet에서 ignored 프로젝트 cache에 복원했고 Windows SDK/Runtime 설치는 안 함. 원본 .NET MIT/제3자 고지 포함. 실제 OS/시작 호환성은 미검증.
- 기존 guest 하네스 재사용: SCD는 시스템 dotnet 검사 안 함, 동일 hash build 재시작 허용/다른 build 덮어쓰기 거부, 결과표는 읽기전용 share 대신 사용자 QAResults에 GUID별 저장. 시작·종료/exit code가 case PASS를 만들지 않음. 이 guest 경로는 AST/정적 검토만, 미실행.
- 실제 진단 결함 재현: 파일 mtime 역전 시 operation ZIP 이벤트 시간 역전(수정 전165/166, 신규1 FAIL) → Utc 정렬, 동일 operation·원인 분류·로컬 ZIP 경로·무DB 단언으로 수정 확인. 거래 저널/Unknown/예약/제출 정책 변경 없음.
- 전체 회귀 **166/166 기능 PASS**, detached WPF **31/31 PASS**, 합성 파일 계약 **14/14 PASS**, FAIL0/Debug 경고0·오류0. 165+31 기존 검사 유지. 75개 판정47/15/0/4/9 유지. 구현 `d19dcc9` origin/main push 완료. 해당 소스의 SCD/FDD Release 모두 PASS(경고0/오류0), x64 apphost/CLR·WPF·SQLite(모드별)·runtimeconfig·고지·DB/로그 제외 정적 확인. SCD CLR/WPF10.0.9·PDB0, guest bundle414개 해시/142.02MiB; FDD40개 해시/33.51MiB. 두 manifest SourceCommit/PackagingCommit/Core metadata 일치, GUI0/0/24. 앱/guest/설치·서명 미실행.
- Steam 인증/이미지/지갑·fee/스택/판매/취소/Guard는 실계정 미검증, IntegrationBlocked. 실제 입력·Popup·물리 DPI·설치·서명·장시간 안정성 미검증. 다음: 별도 Windows11 x64 오프라인 guest 제공/확인 후 기존24개 수동 QA.

## 8차 — 격리 GUI 실행 준비 / 제품 결함 수정 (2026-10-28 KST)

기준 main/origin `4a18077` clean/fetch 일치. 제공된 전용 VM/게스트 접속·실행 환경은 없으며 다른 프로세스/VM/창을 검색하지 않았다. **독립 Windows 확보 및 실제 GUI·설치 실행은 NOT VERIFIED**. 호스트 대체 실행 없음. 최초 75개 ID/판정 **47 COMPLETE / 15 PARTIAL / 0 MISSING / 4 BLOCKED / 9 UNVERIFIED** 유지.

- 게스트 전용 수동 실행 하네스 + 해시 manifest 패키저 + 24개 NOT VERIFIED 시나리오. 패키저는 VM/앱을 실행하지 않는다. `--isolated-qa`는 명시적 게스트 운영자 확인이 없으면 종료4, 별도 사용자 데이터/로그 루트와 Steam factory/로그인 명령 차단을 적용한다. 확인 문자열은 VM 탐지·격리 보증이 아니며 호스트에서 실행하면 안 된다.
- 실제 결함: Steam AnyReview 알림 누락으로 배경 IsEnabled 바인딩 미갱신 → 알림 복구. Mock/Steam 모달의 자체 포커스 scope/Tab·Ctrl+Tab 경계·기존 자체 요소 복귀, 활성 자기 Window에서만 처리. 실제 focus/Popup/키 입력은 여전히 미검증. ComboBox focus/open 테두리, disabled stepper peer 표준 예외 보완.
- 취소된 HTTP/QR을 Transport/Schema 오류로 변환하던 경로 수정. 보관함 조회 취소 후 인증 세션 유지. QR 새 challenge 빈 기본값은 기존 challenge 유지, 비정상 URL/알 수 없는 필드는 차단. 승인 플래그는 bool/정수0·1만 허용, 모호한 응답은 추측하지 않는다.
- 공식 판매 JS 재조회 SHA256은 기존5차와 같음. 공개 proto/maintainer 소스로 QR 선택 필드 근거 대조. 인증된 실제 응답은 미확보. KRW·스택·반환 인과/ID-less/외부 동시 등록은 이전 차단·Unknown 보존.
- 최종 검증: 전체 기능/합성 HTTP/SQLite/진단 165/165 PASS, 비표시 WPF 31/31 PASS, FAIL0, Debug 경고0/오류0. 구현 `f672120` origin/main push 완료. 해당 커밋 Release publish PASS(경고0/오류0), apphost/SQLite x64 PE·Desktop Runtime10 의존성·데이터 제외·라이선스 고지·Core Git metadata 확인. 격리 QA package39개 SHA256 전부 일치, 24개 결과는 NOT VERIFIED. 실제 앱/guest/설치·서명 미실행. 실제 계정/로그인/거래/Guard, 호스트 입력·포커스·화면·타 프로세스 조사는 수행하지 않았다. 디지털 서명/장시간·실제 DPI/설치·업데이트·제거 미검증.

7차 추가 반환 제한: 원래 단일 수량이었어도 취소 후 returned.Stackable/수량 증가·기존 가용 수량이 있으면 proof로 확정하지 않는다. 동일 ID라도 현재 정확히1개/기존0개인 제한 경로만 합성 대조하며 나머지는 Unknown/예약 유지. 실제 nonstack/스택·ID 의미와 인과 증거는 여전히 미검증이다.

### 7차 자동 검증 마감

- 전체 기능/합성 HTTP/SQLite/진단 **158/158 PASS, FAIL0** (기존139+신규19), 비표시 WPF **28/28 PASS, FAIL0** (기존23+신규5), Debug 빌드 경고0/오류0. 마지막 기능 보강 후 전체 회귀 실행. 예상치 못한 binding 오류0/Window·PresentationSource0.
- 실제 PNG 디코딩·재활용60회·전송 중복/취소·재시도/실패 상태와 보관함/판매/검토/추정/진단 이동 이미지를 직접 검사했다. NumericStepper 36px/텍스트 줄바꿈/출처/기준별 부족 수량 안내를 수정·재확인했다.
- 단기 합성 예: 이미지100회 약2ms wall/31ms 자체 process CPU/managed allocation 약1.36MB, LRU4개/280B; 썸네일60회 약1835ms wall/1406ms 자체 CPU/약0.89MB managed allocation, cache210B. 자체 process만 읽었고 CPU 계수 해상도/다른 자체 스레드가 포함된다. 실이미지/네이티브 메모리/장시간 누수/FPS/다른 앱 영향 측정으로 확대하지 않는다.
- 실제 Steam 이미지/인증/보관함/가격/fee/판매/취소/Guard 및 실제 창/물리 입력/모니터 DPI/installer/VM 실행은 NOT VERIFIED. 소스/배포 검사 수정 커밋 `6828294` Release publish PASS(경고0/오류0): PE·로컬 데이터 제외·라이선스 고지 확인, Core DLL의 해당 Git commit metadata 정적 확인. 실행파일·설치 미실행. 구현 `e40492d`, 배포 검사 수정 `6828294`는 origin/main push 완료.

## 7차 최신 상태 — 이미지 / 계정별 context 카탈로그 / 구분된 가격 지표

시작 HEAD/origin/main `158a606`, clean/fetch 확인. 75개 ID 유지/전 행 재평가: **44/16/1/5/9 → COMPLETE47 / PARTIAL15 / MISSING0 / BLOCKED4 / UNVERIFIED9**. 새 COMPLETE **B04/C02/D02**는 로컬 구현·합성 계약·비표시 WPF 범위. B05는 대체 추정치를 연결해 BLOCKED→PARTIAL. 실제 서버/계정/GUI 검증은 승격하지 않았다.

- 이미지: 검증한 classid/instanceid description join과 icon_url_large/icon_url → 메타데이터 별도 maps → 보관함/판매/검토 및 게임 대표 이미지. opt-in 전 네트워크 없음. 인증과 분리한 단일 Steam CDN/고정 128px 경로, URL 토큰 제한, redirects/cookies/proxy OFF. PNG/JPEG 512KiB·512px 이하, 128px off-thread decode/freeze, LRU 128개/16MiB·single-flight·동시 전송1·대기32·15초 timeout·제한 응답 latch. 가상화 unload/재활용/끄기/로그아웃 취소, 늦은 결과 폐기, 없음/꺼짐/미지원/로딩/실패/재시도 UI. 실제 CDN 이미지 미요청/미검증.
- 카탈로그: 3개 seed는 보유 확인이 아니다. 계정별 AppID/context 수동 추가/제거, 판매목록 관측 scope 추가, 기존 Steam v3 SQLite에 원자 저장/복원. context 2 고정 가정 제거. 이름 미확인 App 임시 이름, 판매 가능 종류 수/대표 보관함 이미지/미조회·빈 목록·404·접근 거부·부분 실패 구분. 403만으로 비공개라고 확정하지 않음. 무차별 조회/전체 계정 자동 발견/GetOwnedGames 호출 없음.
- 가격: 즉시 예상가 / 정확한 평균(원자료 없어 이용 불가) / 기간 중앙값의 서버 거래량 가중 중앙값 추정. 24h/7d/30d·수집/기간/구간 수/서버 거래량/방법/한계 표시. 추정은 VWAP/전체 체결 중앙값/즉시 체결가가 아니다. 최저 판매호가는 자료 없음을 표시. 판매목록 선택 최대10상품의 대체 지표도 명시적 조회. 기준/기간 변경은 수동 가격 초기화, 늦은 응답 폐기.
- 보강: Evidence 불변 snapshot/digest, 최종 history 재조회에서 표본 변동(중앙값/합계 같아도)·부족/통화/오래됨 거부. 상품별 preflight 중복 조회 제거/완료 후 승인 freshness 재검사. publisher fee 누락도 차단. 기존 Unknown/스택 반환 인과 증거/취소·재가격 보호 유지. 로그아웃 뒤 늦은 응답이 새 catalog 상태를 덮지 않음.
- 진단: 새 metadata/image/catalog/indicator 이벤트와 operation/request 연결. 원본/URL/토큰/이미지 bytes 로그 없음. 현재 Steam 작업→진단 탭 바로가기. 정책/보존/ZIP 그대로 유지.
- 최종 자동·Release 결과는 7차 QA-MATRIX/WORKLOG 마감 기록 참조. 실제 앱/Window/Popup/전역 입력/다른 앱/실계정/거래/Guard 미실행. [격리 GUI 후속 절차](ISOLATED-GUI-QA.md)는 구성만 준비하고 실행하지 않음.

남은 28개 판정은 추적표에 유지. 핵심 차단: 실제 인증/응답·KRW/스택 계약/정확한 원자료·ID-less/외부 동시 등록/스택 반환의 인과 증거, live 재시도/실환경 QA. **IntegrationBlocked 기본과 최종 단일 승인 전체 실등록 목표 모두 유지.**

## 6차 최신 상태 — 전체 감사 / 구조화 진단 / 안전한 새 재가격 승인

감사 기준 `origin/main=33cd152`(fetch 확인), 기존 사용자 변경 없음. 최초 A–G **75개**: **COMPLETE 44 / PARTIAL 16 / MISSING 1 / BLOCKED 5 / UNVERIFIED 9**. COMPLETE는 로컬/합성/내부 UI 범위이며 실계정 검증은 전부 N. [세부 대응표](REQUIREMENTS-TRACEABILITY.md), [QA 매트릭스](QA-MATRIX.md), [진단 운영](DIAGNOSTICS.md).

- 중앙 DiagnosticHub + whitelist JSONL, 실행/operation/request/batch/internal item 연결, 버전/commit/UTC, 상태/소요 시간/오류 분류/정제 코드 위치. HTTP·인증·데이터·가격·배치·관리·SQLite·UI에 연결. 별도 진단 탭에서 상세 모드/오류·작업 검색/UTC-KST/보존 정책/로컬 검토 ZIP. 토큰·QR·원문·DB export/자동 외부 전송 없음.
- INFO 기본, 상세 모드 설정 복원, async bounded 256 큐/최근512, 2 MiB·20 MiB·20파일·14일 회전·정리. 기록 실패·포화는 health로 표시하고 거래 결과/재전송에 영향 없음. 종료 tail은 최선 노력이며 모든 오류 재현/원인 확정을 보장하지 않음.
- 실험 보관함 검색/정렬/marketable 필터/선택 해제, 실제 판매 게임 필터/해제, 실행 중 갱신/게임 전환 선택·수량 복원/상한 축소. 오래된 가격은 재조회하며 선택을 실제 데이터로 가장하지 않음. UI 가격 이상 경고/신선도 재확인 추가.
- 오래된 재가격은 취소 요청 **전** 차단. 확정 반환 proof가 있는 미전송 의도는 새 가격 검토/승인으로만 새 immutable intent를 만들며 이전 의도를 원자적 Superseded로 보존. 취소/반환/후속 요청 키를 유지해 재시작 중복 방지. 기존 앱이 Superseded를 오해하지 못하도록 Steam schema **v3**로 올리고 v1/v2 내용을 보존한다. 하위 버전 앱으로 DB를 열지 말 것(기존 v2 코드는 v3 거부).
- 창 닫기 검사에 누락된 Steam.Busy 추가. 실제 Window 이벤트는 실행하지 않음. 실행 중 JSONL export 공유 모드 오류를 오류 주입으로 발견·수정. 로그 재정제/부분 ZIP 원자적 완료/검색 중 승인 범위 변경도 방어.
- 검증: 전체 Debug 빌드 경고0/오류0, **139/139** 기능·합성 HTTP·저장소·진단 및 **23/23** 비표시 WPF PASS, FAIL0. Steam v3 migration 후 전체 회귀 통과. 최종 구현 `fab286a` Release도 경고0/오류0, Windows PE·로컬 데이터 제외·MIT 고지 검사 PASS. 실행파일은 생성만 했고 실행하지 않았다. 구현 `5b44e0d`, 진단 보완 `fab286a` origin/main push 완료. 문서 마감 커밋은 최종 보고 참조. 실제 계정 인증/판매/취소/Guard, 실제 앱 실행/Popup/전역 입력/물리 DPI/설치 미실행. 자체 STA 렌더만 허용 범위에서 수행.

**실사용 차단 요소:** 실제 이미지 미구현, 고정 3게임/context 외 계정 게임 발견 미구현, 정확한 실거래 평균 원자료 미확보, 실제 KRW 수수료·스택/반환·인증/응답 계약 미검증, ID 없는 요청/외부 동시 등록 인과 증거 미확보, live 확정 실패 재시도/실환경 QA 미완성. **실제 전체 일괄 판매등록이 최종 목표지만 현재 실판매 완료 아님**, SteamTradingPolicy.Blocked 기본. [최신 허용 근거 검토](STEAM-INTEGRATION-FEASIBILITY.md).

### 이전 5차 기록 (당시 검증)
## 5차 최신 상태: 사용자 시작 연결 UI / 실거래 기본 차단

구현/검증 커밋 `f7bd8aa` origin/main push 완료 (`73ad542..f7bd8aa`). 최종 기록 동기화 커밋은 완료 보고 참조.

- `Steam 실험 연동`의 QR 연결·읽기·선택·가격 검토·판매 관리·수동 대조를 실제 어댑터와 연결했다. Mock의 보관함/합계/상태와 섞지 않으며 앱 시작·탭 생성은 네트워크나 실저널을 열지 않는다. 실제 거래는 `SteamTradingPolicy.Blocked`, 체크만으로 활성화되지 않는다.
- KRW/USD `Money`와 서버 지갑 최소/증분·앱별 fee rate/cap 계산, 수령/수수료/구매자 금액 분리·정확한 역산·모순 중단. 공식 최신 판매 번들의 구조에 근거하며 **인증된 서버 응답/실계정 계산과 비교한 것은 아니다**.
- 동일 상품의 호가를 asset/context 간 공유, 스택 분할·부족 수량 검토·예약/반환 증거·재시작을 구현했다. 스택 제출 capability는 별도다. 외부 수량 증가/ID 변경 불확실성은 보수적 가용량/차단으로 처리한다.
- listingid 없는 후보의 이전 ID/시각/가격/수량/판정 및 감사 이력을 수동 도구로 제공한다. 후보 1개여도 자동 소유 요청 확정 없음. 취소 접수+목록 부재+원래 비스택 단일 자산의 동일 ID 반환을 확인한 경우만 재가격한다. target/후속키 저장·명시적 재개·중복 차단, 5분 stale은 거절한다.
- Steam DB v2는 v1 불명 의도를 보존한다. command/reprice audit 원자 저장, 확정 batch archive 후에도 의도·증거·예약을 보존하고 다음 배치를 처리한다. 실저널 삭제 UX는 제공하지 않는다.
- 최종 `scripts/Verify.ps1`: 전체 Debug 빌드 경고0/오류0, **112/112** 도메인·합성 HTTP와 **20/20** 비표시 WPF PASS, FAIL0. 새 adapter-backed 10,000개 목록/알림 batching과 이전 회귀도 통과했다. [QA-CHECKLIST](QA-CHECKLIST.md).
- 최신 Release publish·PE·로컬 데이터 제외·QR MIT LICENSE/고지 포함 검사 PASS. 자체 QR/가격 검토/보관함/통합 화면 PNG를 생성·검토했다. 실행파일 실행/설치·실제 입력·모니터 DPI·실계정 검증은 수행하지 않았다.
- 남은 제한: 실계정 QR 스캔/인증/보관함/지갑/fee/등록/취소/Guard, 정책 허용 근거, 게임 scope 자동 발견, 같은 ID로 반환되지 않는 취소/스택, ID 없는 인과관계 확정, stale 재가격 target revision UI·live 확정 실패 retry, 실제 입력/Popup/모니터 DPI/설치·보안 감사.
- **실제 단일 승인 전체 판매등록의 제품 목표는 미완료**다. 합성 성공은 실판매 성공이 아니며 LIVE_READY/RELEASE_READY가 아니다. [최신 근거·지원 범위](STEAM-INTEGRATION-FEASIBILITY.md).

## 4차 상태 기록 (당시 기준)

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
11차 최종 회귀: **기능177/177 PASS · 비표시 WPF39/39 PASS · 배포 파일 계약14/14 PASS · FAIL0 · Debug 경고0/오류0**. 기존172/33/14 전부 유지. `scripts/Verify.ps1`, ignored `runtime/stage11-verify.txt` 근거. 실제 서버/물리입력/Popup/설치·서명 미실행.

사용자 최소 재확인: (1) 새 ZIP 버전·커밋 확인 후 Steam 탭에서 명시적 QR 시작으로 QR 표시 여부만 확인(실거래 금지), 실패하면 표시된 단계와 검토한 진단ZIP 확인. QR 원문은 공유하지 않는다. (2) 진단 탭/최근 오류 체크를 클릭한 뒤 포인터를 밖으로 이동: 선택 밑줄·체크 표시만 유지되는지 확인. (3) Tab/Shift+Tab의 점선 포커스, 빠른 탭 전환, 비활성→활성, 드롭다운 열기·닫기를 직접 확인. Codex는 이 작업을 실행하지 않았다.

0.20.6 검증: 기능348/348·비표시WPF68/68·배포계약19/19·정리fixture9/9 PASS, 빌드 경고/오류0. ReservedInventory는 HTTP400→Unknown→앱 세션 종료/복원→갱신 후 보유1/예약1/가용0을 재현하고, 요청 직접 열기·읽기 대조 뒤에도 판매 POST1회와 예약 보존을 확인했다. PartialReservation은 보유2/예약1/가용1의 수량 상한을 확인했다. 확정 취소 반환 후 ReservedSells 제외도 검사했다. 예약·선택 가능 자산 수와 이전 요청 열기 진단의 이유/건수 및 요청 키 비노출을 검사했다. 자체 렌더는 카드·목록·가용 복귀 시 안내 해제를 확인했다. 최초 WPF 검사1건은 최상위 창 없는 RTB에서 IsVisible을 검사한 테스트 가정 문제로 실패했고, Visibility/실제 배치 크기를 검사하도록 바로잡은 뒤68/68 통과했다. 실제 GUI·실계정 대조·거래는 미실행이다.
0.20.8 검증: 전체 기능354/354·비표시WPF69/69·배포계약19/19·정리fixture9/9 PASS, 빌드 경고/오류0. SingleStack은 전체/부분 수량1·2 및10개를 같은 단가로 맞춘3개 검토 행의 단일 POST/스냅샷1행·원래 총액115×수량 보존을 검사했다. StackBoundaries는 서로 다른 단가 스택·미검증 new mode 및 직접 Executor 우회를 차단하고, StackModeChanged는 검토 후모드 변경 시 intent/전송0을 확인했다. IdlessObservation은 정확한 자산 관측·다른 앱·중복 후보 거부와 원요청 ID 미할당/재전송0을 검사했다. 평균/기간 UI 제거를 보관함·판매목록 자체 렌더로 직접 확인했다. 새 출하 검사는 옛0.20.7 BAML을 'Retired pricing UI leaked: PricingModes'로 거부하는 음성 검사를 확인했다. 최종 스냅샷 총액 보존과 관련20개 계약도 재확인했다. 처음 추가한 스택2건은 React legacy fixture를 classic으로 잘못 구성해 차단됐으며 테스트를 실제 명시적 classic 소유 페이지로 수정했다. production 보호 조건을 완화해 테스트를 통과시키지 않았다. 실제 앱/계정/판매·스택 반환·공식 UI 금액 대조는 미실행이다.

검토에서 여러 행의 동일 스택을 직접 같은 단가로 맞춘 경우 자산·통화·수수료·시각·호가 증거가 전부 일치할 때만 합산한다. 다른 가격을 자동 대체하지 않는다. 여러 가격이 남아 있으면 전송을 차단한다.
0.20.9 최종 검증: 기능356/356·비표시WPF69/69·배포계약19/19·정리fixture9/9 PASS, 빌드 경고/오류0. ReadinessWithPendingIntent는400→Unknown→세션 재시작→준비 허용/현재 자산 선택 가능→최종 제출 거부/추가 POST0/원 저널 보존을 검사했다. UnmarketableReadiness는 실제 marketable=false 응답을 준비 단계에서 거부하고 원인 건수를 확인했다. 최초 재현은 관련21건 중1 FAIL→수정 후 전체 PASS. 자동 승인 검토 시간 초과로 최초 전체 검증 명령이 시작되지 않았고, 허용된1회 재시도로 정상 실행했다. 실제 Steam/GUI 실행이나 사용자 데이터 변경은 없었다.
## 0.20.9 후속 — 실제 SSIM 코드 이식과 실행기 공유

사용자의 '실제 기능하는 프로젝트 코드 활용' 요청으로 SSIM28a503f의 MoneyOps.ts / MarketService.ts와 SteamMarketHelper e186d51의 sellItem/fetchInventory를 실제 소스로 재확인했다. SSIM은 현재 진행 중인 자산 잠금과 영구 거래 저널을 분리하며, MoneyOps 잠금은 작업이 끝나면 해제한다. 이 알고리즘을 C# SteamAssetOperationGuard로 부분 이식했다. Apache-2.0 LICENSE/NOTICE와 Copyright2026 Santer.xyz, 원본 커밋·경로·변경 사실을 보존했다. 브랜딩/비밀키/Node 런타임/자동 승인/네트워크 우회는 도입하지 않았다.

MarketCrate의 SetGame은 Executor를 재생성하므로 인스턴스별 semaphore만으로는 이전/새 Executor의 동시 작업을 보호하지 못했다. 수정 전 실행기로 동시 제출을 재현했을 때 SQLite SQL logic error가 발생했다(추가 중복 POST 성공을 확인했다는 뜻은 아님). 자산 잠금을 저장소 범위로 공유하고 SQLite 쓰기 큐도 공유해 같은 자산의 동시 제출/취소를 미전송 결과로 거절하고, 다른 자산은 직렬 처리한다. 잠금 소유 lease만 해제하며 대기 중 취소도 정리한다. 영속 Unknown은 해제/재전송하지 않고 별도 유지한다.

기존 f3b7594는 활성화 수정 중간 소스이며 그 publish 폴더는 검사만 했고 최신 ZIP/latest로 등록하지 않았다. 최종 SSIM 포함 배포는 후속 소스 커밋과 ZIP 해시 절을 기준으로 한다. 이전 후보·사용자 데이터·진단·롤백본 삭제0.
SSIM 이식 최종 검증: 기능359/359·비표시WPF69/69·배포계약19/19·정리fixture9/9 PASS, 빌드 경고/오류0. 관련25개 합성 계약도 최종 재확인했다. 원본 MoneyOps 기반 atomic claimAll/계정 구분/중복 키/소유자 중복 Dispose, 재생성 Executor의 동일 자산 제출·취소 동시 진입 거부, 서로 다른 자산 직렬화, queue 대기 취소 후 lease 해제를 확인했다. 동시 충돌은 전송 전 실패로 반환하고 새로운 intent/POST 없이 원 작업만 유지한다. 수정 전 f3b7594 실행기를 사용한 음성 재현은 SQLite 동시 접근 오류로 실패했고 새 실행기는 같은 경로를 통과했다. 실계정/실제 GUI/백그라운드 요청은 실행하지 않았다. Source LICENSE/NOTICE를 보존하고 실제 출하 타입이 있으면 해당 고지 파일 부재를 검사하도록 강화했다.
검증: 기능363/363 PASS, 비표시 WPF69/69 PASS, 배포계약19/19 PASS. 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가, 시스템 변경/승격 없이 미검증 유지). 빌드 경고/오류0. 수정 전 연속 배치 재현1 FAIL 후 수정 통과. 서버 목록 검사 추가로 기존 두 fixture가 개별 자산을 보유와 외부 판매에 동시에 두던 모순이 드러나 외부 등록을 다른 자산으로 고치고 기존 ID 재사용 Unknown 검사를 보존했다. 공유 배치 게이트까지 전체 회귀 후, UI 오류 원인 화이트리스트 연결을 관련29개 합성 계약으로 재확인했다. 자체 최종 검토 RTB 이미지를 직접 확인했고 실제 창/물리 입력/Steam 계정은 실행하지 않았다. 원본 진단은 로컬에서만 읽고 배포/Git에 넣지 않았다.

0.20.11 최종 검증: 기능367/367 PASS, 비표시WPF70/70 PASS, 배포계약19/19 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 빌드 경고/오류0. 최종 자체 렌더에서 선택된 요청이 내부 record 문자열(Key 포함)로 보이는 공통 ComboBox 템플릿 누락을 발견했다. SelectionContent의 ItemTemplateSelector 연결을 복구하고 최근 요청 기본 선택/기존 선택 유지 및 DisplayMemberPath의 실제 렌더 텍스트 검사를 추가했다. 마지막 템플릿 변경 후 전체 비표시 WPF70개를 다시 통과했고125/200% 승인 인계 이미지와 게시 후 이미지를 직접 확인했다. 실제 창·물리 입력·모니터 DPI·실계정·휴대폰은 미검증이다.


0.20.12 검증: 전체 기능372/372 PASS, 비표시 WPF71/71 PASS, 배포계약19/19 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 빌드 경고/오류0. 전송 전 권한 해제/queue 취소2건과 비장터 시장명 누락1건을 수정 전 실패로 재현했다. 마지막 요청 기록/현재 관측 문구 분리 후 관련 판매 계약 전체와 비표시 WPF를 재확인했다. ID 없는 게시 관측 이미지와 승인 인계 이미지는 자체 RTB로 검토하며 실제 창/휴대폰/계정은 실행하지 않았다. 추가 테스트 작성 중 nullable 경고1건은 fixture의 확인된 workspace 참조로 수정했다. 관측은 원요청 상태/ID를 바꾸지 않고 다음 명시적 대조에서 초기화한다.

## 0.20.13 — 최종 릴리즈 신뢰도 감사 (2026-10-31 KST)

같은 BatchId에 변경된 계정/가격을 전달해 이전 승인을 재사용하는 경로, 취소 후 과거 로컬 전송 기록으로 새 실제 보유 자산 검토가 막히는 경로, 타 계정 AssetKey의 pending 수량 혼합을 수정했다. 서버 inventory와 active/pending membership을 기준으로 새 대상·새 가격·새 최종 승인을 요구하며 기존 Unknown/원요청 ID/감사 저널은 보존한다. 관련 코드·수정 전/후 증거·출하 기준은 [RELEASE-READINESS](RELEASE-READINESS.md)를 따른다.

번들 Core/WPF10.0.14와 Microsoft.Data.Sqlite/ProtectedData10.0.14·SQLitePCLRaw2.1.14(native3.54.0)를 사용한다. 호스트 SDK/런타임 설치·설정 변경0. 구 provider 생성→신 provider 읽기/쓰기→구 provider 재읽기 합성 DB 호환 PASS. NuGet advisory는 명시적 공개 source/프로젝트 coverage를 검사하며 빈 source를 PASS로 판정하지 않는다. 패키지 감사와 런타임 보안 최소 버전 검사는 별개다.

최초75개47 COMPLETE/16 PARTIAL/0 MISSING/3 BLOCKED/9 UNVERIFIED를 유지한다. 새 실제 GUI·실계정·판매/취소/Guard0. 이전 사용자 관측을 새 릴리즈 실증으로 확대하지 않는다. Auth/Preferences/거래 저널/진단/롤백 삭제0. 실제 전용 출하와 단일 최종 승인 전체 배치, DPAPI, 정제 JSONL/ZIP, Unknown 비재전송, 타 프로세스 비간섭을 유지한다.

## 2026-10-31 — 사용자 실제 게시/미체결 관측

사용자가0.20.14 정상 동작과 TBH Contaminated Soulstone - Torment의 실제 판매등록을 보고했고, 제공 화면에 구매자70원/판매자50원 및 같은 표시 최고 구매주문70원이 관측됐다. 이는 해당 범위의 사용자 등록/게시 증거이며 전체 거래/스택/통화/Guard·LIVE_READY로 확대하지 않는다. 공식 FAQ/최신 공개 수수료 JS/고정 SteamMarketHelper 실제 소스와 가격 경로를 대조했다. 교차 통화 환산·가격 증분으로 표시가격이 같아도 체결 조건이 다를 가능성이 있으나 원통화/서버 적격 증거가 없어 이번 원인을 확정하지 않았다. 세부 근거와 가격 유지 또는 명시적 한 유효 단계 하향 방법은 MARKET-MATCHING-INVESTIGATION.md 참조. 자동 인하/취소/재등록·코드/Release 변경·실계정 요청0. 조사 문서만 변경했으므로 기존 성공 회귀를 반복 실행하지 않았다.
