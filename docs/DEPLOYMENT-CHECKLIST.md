## 최신 배포 — 0.20.20 최저 유효 가격 유지

소스 **426b6ee0146b0ff37611790265f76f7df9f2a370**. `artifacts/releases/MarketCrate-0.20.20-minimum-price-426b6ee-win-x64.zip` **66,751,408바이트 /418파일**. SHA256 **918C46D9D4CCB597DF74E5721AE11D33AB0C3FCEF4E179ED5DEF3808E1EEF04B**. ZIP 전체 entry/출하 파일 해시·SourceCommit/버전·x64 CLR/WPF10.0.14·SQLite/DPAPI·라이선스·실제 전용 PE/BAML·Mock/개발UI/Auth/DB/로그/PDB 제외 정적 검사 PASS. Release 경고/오류0. 앱/설치/실계정은 미실행이다.

기능430/430·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS, 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 최저 유효가 유지/감소0 검토125/200% 자체 RTB 직접 확인. 새 코드의 실제 사용자 거래/모바일 승인은 NOT VERIFIED이며 75개47/16/0/3/9 유지.

롤백0.20.19(a3ef735, SHA256 2470686121811D9A4BD5A1F1FF09C5BD085996C74DDB41526E2E81B6E8869C91)과 기존 Auth/설정/SQLite/저널/진단·과거 ZIP 보존, 삭제0. 사용자는 새 폴더에 해제→보관함 시세 갱신→최저가 유지 표시/수수료/실수령 확인→새 최종 검토의 전체 판매등록 승인 순으로 확인한다. 호가 변경 안내 시 이전 승인을 반복하지 말고 다시 시세를 갱신해 검토해야 한다. 첨부 작업은 전송 전 호가 변경으로 Sell0이며 Steam 확인 요청이 생성되지 않았다는 근거가 확인됐다. 자동 재전송/가격 변경·실제 Steam 실행0.

## 최신 배포 — 0.20.19 승인 후 현재 목록 중심

소스 **a3ef735f1d516c22de6d7424423e80cb62a7af19**. `artifacts/releases/MarketCrate-0.20.19-current-listings-a3ef735-win-x64.zip` **66,750,871바이트 /418파일**. SHA256 **2470686121811D9A4BD5A1F1FF09C5BD085996C74DDB41526E2E81B6E8869C91**. 전체 ZIP entry/출하 파일 해시·SourceCommit/버전·x64 CLR/WPF10.0.14·SQLite/DPAPI·라이선스·실제 전용 PE/BAML·Mock/개발UI/Auth/DB/로그/PDB 제외 검사 PASS. Release build 경고/오류0, 앱/설치/실계정 실행은 NOT VERIFIED.

전체 기능428/428·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS. 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가).125/200% 승인 인계와 현재 판매목록 자체 RTB를 직접 검토했다. 75개 판정47/16/0/3/9 유지. 정상 기본 흐름에서 history 호출/후속 체결 추적0, 원래 요청/중복 보호 기록은 보존한다.

롤백0.20.18(d3eb49e, SHA256 15645A9B553A9775EB8DD92A724E53E06A9CBCC253EE4F938A16CF6C92ED2C25)과 모든 사용자 Auth/설정/SQLite/저널/진단·이전 ZIP 보존, 삭제0. 사용자는 새 폴더에 해제→로그인 복원→필요 시 공식 Steam 앱 수동 확인→승인 후 판매 중 보기로 현재 남은 항목을 확인한다. 이미 팔린 항목의 정밀 이력 대조는 일반 사용에 필요하지 않다. 응답 불명 요청을 새로 전송하거나 원기록을 성공으로 조작하지 않는다.

## 최신 배포 — 0.20.18 KRW 거래내역 원본 단위 대조

소스 **d3eb49e282cba23ebf26c418c5cfd048ee633de2**. `artifacts/releases/MarketCrate-0.20.18-history-wire-d3eb49e-win-x64.zip` **66,750,288바이트 /418파일**. SHA256 **15645A9B553A9775EB8DD92A724E53E06A9CBCC253EE4F938A16CF6C92ED2C25**. 전체 ZIP entry/출하 파일 SHA 일치·SourceCommit/버전·x64 CLR/WPF10.0.14·SQLite/DPAPI·라이선스·실제 전용 PE/BAML·Mock/개발UI/DB/Auth/로그/PDB 제외 정적 검사 PASS. Release 경고/오류0. 앱/설치/실계정은 미실행이다.

전체 기능424/424·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS. 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 원화 이력1건의 비정수 표시단위 때문에 페이지 전체가 중단되는 합성 재현을 수정했고 신규5개 회귀를 포함한다. 사용자 실제3건의 새 버전 대조는 아직 NOT VERIFIED다. 75개 판정47/16/0/3/9 유지.

롤백0.20.17(72b7b70, SHA256 9139E51CCD5C4C378E04C9D448DCA428D2DBD6D808C1C35C42BE5D77910D502D)과 모든 기존 배포·운영 로그·Auth·설정·SQLite·저널을 보존했다. 삭제/초기화0. 사용자는 새 폴더에 ZIP을 해제해 로그인 복원 후 기존 판매 결과의 ‘승인 후 게시 확인’ 또는 ‘선택 요청 상태 확인’을 실행하면 된다. 새 판매는 필요하지 않다. 원래 등록 ID/자산/통화/수량/원본 단위 금액/시각이 정확히 일치해야 판매 완료이며, 불일치는 새 이유별 진단과 Unknown을 유지한다. Codex의 실제 계정/GUI 요청0.

## 최신 배포 — 0.20.17 판매목록 빈 자산 맵

소스 **72b7b703caa5133c4719198fe80297b2d39d75c7**. `artifacts/releases/MarketCrate-0.20.17-empty-listings-72b7b70-win-x64.zip` **66,749,751바이트 /418파일**. SHA256 **9139E51CCD5C4C378E04C9D448DCA428D2DBD6D808C1C35C42BE5D77910D502D**. ZIP 모든 entry와 출하 파일 SHA 일치, 버전/커밋·x64 CLR/WPF10.0.14·SQLite/DPAPI·라이선스·실제 전용 PE/BAML·Mock/개발UI/개인DB/Auth/로그/PDB 제외 검사 PASS. Release 경고/오류0, 앱/설치/실계정 실행은 NOT VERIFIED.

기능419/419·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 최초75개47/16/0/3/9 유지. 원본 로그를 저장하지 않아 사용자의 root assets 정확한 JSON 종류는 미확정이며 출하 IL로 비객체 거부 분기만 확정했다. 빈 [] 호환은 합성 계약/활성화 경로로 검증했다. 잘못된 맵/금액/total_count/403·429 보호는 유지한다.

최신0.20.17/롤백0.20.16(209d0a7, SHA256 FAEE88178DE08A507A5852924017CB834CE38F79B68D2CBD307ECB6C1DE68693) 보존. 기존 사용자 Auth·설정·SQLite·저널·진단·이전 ZIP 삭제/초기화0. 사용자는 새 폴더에 ZIP 해제→기존 로그인 복원→보관함/판매목록 갱신→실제 거래 준비 또는 최종 가격 검토로 기존 실패 지점을 재확인할 수 있다. 이 확인만으로 판매 요청은 전송하지 않으며 최종 전체 판매등록은 별도 사용자 승인이다. 실패 시 새 작업 진단을 내보내고 원본문/인증정보를 별도 수집하지 않는다.

## 최신 배포 — 0.20.16 즉시 체결 상태 대조

소스 **209d0a7f958b623c9be490374e0294f371fa40bb**. `artifacts/releases/MarketCrate-0.20.16-instant-sale-209d0a7-win-x64.zip` **66,748,818바이트 /418파일**. SHA256 **FAEE88178DE08A507A5852924017CB834CE38F79B68D2CBD307ECB6C1DE68693**. ZIP 전체 entry/출하 파일 해시 일치·버전/커밋·x64 CLR/WPF10.0.14·SQLite/DPAPI·라이선스·실제 전용 PE/BAML·Auth/개인DB/로그/PDB/Mock/자가검증 제외 정적 검사 PASS. Release build 경고/오류0. 앱/설치/실계정은 미실행이다.

기능411/411·비표시WPF79/79·배포20/20·advisory 검사기6/6 PASS. 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). Sold/보류/Unknown125/200% 자체 RTB 시각 검토 PASS. 최초75개47/16/0/3/9 유지. 새 체결 이력 파서는 실계정 NOT VERIFIED다.

최신0.20.16/롤백0.20.15(59e3191, SHA256 3E55B1AECECE9728B381D0BC36DE9E2F155838FDE492EADC53F1BD5BE895A21D) 보존. Auth·설정·SQLite·저널·진단·이전 배포 삭제0. 기존 enum 숫자를 유지하고 Sold를 뒤에 추가했으므로 새 Sold 저널을 구버전이 읽지 못할 수 있다. 이 경우 DB를 삭제/초기화하지 말고 새 앱을 사용한다.

사용자 확인: 새 ZIP을 별도 폴더에 해제 → 로그인 복원 → 판매 결과의 해당 요청 선택 후 ‘선택 요청 상태 확인’ 또는 ‘승인 후 게시 확인’. 등록 ID가 있고 실제 거래내역이 일치하면 판매 완료, ID가 없으면 체결 관측/원요청 귀속 미확인, 범위 밖/반영 지연/스택 부분 체결/모순은 미확인 안내다. 알려진 ID도 판매목록 부재만으로 완료 확정하지 않는다. 추가 판매를 먼저 할 필요는 없으며 기존 요청 상태를 새로 확인할 수 있다. 원본 증거는 보존하며 자동 재전송하지 않는다.

# Windows 배포 준비 —0.20.4

## 최신 0.20.4

소스2680afb187b61baac1cd69e1efc1df06769e5dc9, artifacts/releases/MarketCrate-0.20.4-sell-response-2680afb-win-x64.zip, 66,514,084바이트/413파일, SHA256 AD88BC994CF68EF732D65571D74317F2A7195405AF7783B23273D2BF7ECAF1B2. 전체 ZIP entry SHA/폴더·메타데이터·SourceCommit·CLR/WPF10.0.9/SQLite x64/ProtectedData·라이선스·실제전용 PE/BAML·개발/Mock/사용자DB/Auth/로그/PDB 제외 검사 PASS. 기능341/WPF66/배포19/정리fixture9 PASS, 경고/오류0. 실제 앱/설치·실계정/신규 속도는 NOT VERIFIED. latest 등록, 직전0.20.3 롤백 보존, 운영자료/이전 산출물 삭제0. 아래0.20.3은 이전 배포 기록이다.

## 최신 0.20.3 출하

소스4d158896c90be55a5129d263bcd7a22229fbfb9f, artifacts/releases/MarketCrate-0.20.3-live-default-4d15889-win-x64.zip, 66,511,118바이트/413파일, SHA256 23E0C2C5E3F413F5ED0292FEC3C59CECCB6CDF9858D7E67EBF4ACE9C4D8673A5. 전체 entry SHA·폴더·SourceCommit·CLR/WPF10.0.9/SQLite x64/ProtectedData·라이선스·실제전용 PE/BAML·Mock/테스트 UI/사용자DB/Auth/로그/PDB 제외 검사 PASS. 기능333/WPF66/배포계약19/정리fixture9 PASS, 빌드 경고/오류0. 실거래·이미지 기본 선택 ON은 검증된 전송 권한 저장이 아니며 최종 승인 전 전송0. 실제 앱/설치/물리입력·실계정 계약/거래는 NOT VERIFIED.

기존 인덱스의0.17 ZIP이 현재 경로에 없어 첫 등록은 중단, 실제 존재하는0.20.2 ZIP을 롤백으로 지정해 전체 해시 검증 후 등록했다. 누락 원인 추정/외부 파일 조사/이전자료 삭제는 하지 않았다. 아래0.20.2는 이전 배포 기록이다.

## 0.20.2 최신 배포 확정

SourceCommit6c311ec0dde402b96e6ce9d09bbab30d3834ce45. artifacts/releases/MarketCrate-0.20.2-classic-fees-6c311ec-win-x64.zip, 66,508,533바이트/413파일, SHA256 C88086A0D1B247187B96952E0B9237457CE0D3B4123103BE453F7F757397B25C. 전체 entry SHA·소스/폴더 메타데이터·CLR/WPF10.0.9/SQLite x64/ProtectedData·라이선스·실제 전용 PE/BAML·사용자 DB/Auth/로그/개발 파일 제외 검사 PASS. 기능329/WPF66/배포계약19/정리fixture9 PASS, 경고/오류0. 실제 앱 실행/설치/실계정 계약 미검증. 기존0.20.1/0.20/롤백0.17·운영 자료 보존. 아래0.20.1은 과거 배포 검사다.

## 최신 0.20.1 배포 검사

소스7dfe8282ce44fff8d3ce1c9768310b342428764c, artifacts/releases/MarketCrate-0.20.1-fee-context-7dfe828-win-x64.zip, 66,507,519바이트/413파일. SHA256 014EA96DF1AE13D55F7292220A1291DCD549FD9619BB96EAB8AF693757F4FCB1. 전체 entry SHA와 폴더/메타데이터/SourceCommit 일치, CLR/WPF10.0.9·SQLite x64·ProtectedData·라이선스, PE/BAML 실제 전용·Mock/자가검증/테스트 타입 제외, 사용자 DB/Auth/로그/PDB 제외 검사 PASS. 실제 실행·설치·실계정은 NOT VERIFIED. latest 등록, 롤백0.17 및 직전0.20/기존 자료 삭제0.

### 0.20.0 배포 확정

구현 소스 **13a339108d082f04fac81fb98ab193e27d7e9108**(제품 감사3b3f925 + 대량 알림 보완13a3391), Windows x64 self-contained LiveOnly **artifacts/releases/MarketCrate-0.20.0-product-audit-13a3391-win-x64.zip**, **66,504,701바이트 /413파일**. SHA256 **461D4FD228A9D0EE5025F74C0B0B4AF6F2870A67BF07D0966CBD8B229679CC4E**. 원본 `artifacts/releases/product-audit-13a3391-windows-x64`, BuildVersion0.20.0+13a3391…. 전체 ZIP entry SHA/폴더 파일 수/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData·라이선스 검사 PASS. 사용자 DB/Auth/로그/PDB·Mock/자가검증·테스트 모델은 출하 제외이며 실제 진행/작업 선택 타입은 존재한다. 앱/설치/물리 입력·실계정·서명은 실행·검증하지 않았다.

최종 **기능315/315·비표시WPF65/65·배포계약19/19·정리 fixture9/9 PASS**, Debug/Release 경고·오류0. 최초75개47 COMPLETE/16 PARTIAL/0 MISSING/3 BLOCKED/9 UNVERIFIED. 실수수료/스택/실판매·물리GUI의 부족한 근거를 COMPLETE로 바꾸지 않았다. 실제 Steam 로그인·조회·판매·취소/Guard 실행0·호스트 GUI/타 프로세스 접근0이다.

latest/rollback은 ignored artifacts/releases.json에 기록했다. 롤백0.17.0(session-1700bfd ZIP/SHA A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC)과 직전0.19.0·0.18.0 및 모든 운영 자료를 보존했다. 바이너리는 로컬 생성이며 GitHub Release로 업로드하지 않았다. 사용자 최소 확인은 로그인 복원→보관함/판매목록 갱신→카드·목록/게임 전환 선택 유지→가격 검토, 취소 필요 시 '실제 취소' 전용 활성화→전체 취소 검토→선택 요청 상태 확인이다. 실제 계정 실행은 사용자가 위험과 최신 서버 계약을 확인한 후 직접 수행한다.


0.20 마지막 대량 갱신 보완: 동일 상품10,000개에 SetPrice를 적용할 때 각 행의 Changed가 전체 선택 요약을 다시 계산하던 경로를 기존 Bulk로 묶었다. PriceNotifications 합성 회귀는10,000개 가격 전부 반영·요약 알림10회 미만·거래 전송0을 확인한다. 이는 실행 시간/CPU 개선 비율을 측정한 벤치마크는 아니다. 최종 기준315개 기능/65개 비표시WPF이며 아래 중간312·314 기록과 구분한다.

### 0.20 최종 자동 검증

**기능315/315·비표시WPF65/65·배포계약19/19·안전 정리 fixture9/9 PASS**, Debug/Release 경고·오류0. 기준0.19의305/61과 전 회귀를 유지했다. 신규10기능/4WPF에는 실시간 진행, 취소 대조·취소 전용, 원자 저장 오류 주입, 이전 계획 선택, 로그아웃 늦은 결과, 캐시 선택 보존, 계정 보관 보호와50개 Command Binding 검사가 포함된다. 배포 PE/BAML에서 실제 서비스·진행/작업 선택 타입 존재와 Mock/자가검증/테스트 타입 부재를 검사한다. 자체 RTB의 새 내비게이션·카드/목록·compact 검토·진단 복귀를 직접 열람했다. 개발 구성의 자가검증 탭은 Release에서 제외되며 렌더와 실제 출하 검사를 구분한다.

제한 실행에서 개발 전용 JSON 저널 파일 교체1건은 FAIL이었다. 동일 바이너리 정상 권한312/312 이후, 마지막 선택·보관 가드를 포함한 최종315/315 정상 권한 검사를 확인했다. 실제 GUI/물리 입력/Popup/모니터DPI·장시간 리소스·실서버 수수료/거래는 미검증이다. 실제 창/로그인·인증/조회·판매·취소·Guard 실행0, 타 프로세스/호스트 입력·화면/설정 접근0, 운영 데이터·이전 배포본 삭제0이다. ZIP/SourceCommit/해시는 배포 확정 절에 추가한다.


### 0.19.0 배포 확정 — 사용자 결함·새 자산 재등록

구현 소스 **cb2c32685cef5c6acb9c3d5faf87f39acd48d65c**, Windows x64 self-contained LiveOnly portable **artifacts/releases/MarketCrate-0.19.0-relist-cb2c326-win-x64.zip**, **66,495,539바이트 /413파일**. SHA256 **42D92ECDDF23BE4789150B52C1585A802C71BBBD31E9AED8155E3A1AE86528D6**. 원본 `artifacts/releases/relist-cb2c326-windows-x64`, BuildVersion0.19.0+cb2c326…. 전체 ZIP entry SHA/폴더 파일 수/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData·라이선스 일치, 사용자 DB/Auth/로그/PDB 제외·실제 UI/SteamRelistPlan 존재·Mock/자가검증 모델/BAML 부재 검사 PASS. 앱/설치/서명·실제 계정/거래/물리 입력은 Codex 미실행이다.

최종 자동 **기능305/305·비표시WPF61/61·배포계약19/19 PASS**, Debug/Release 경고·오류0. 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 환경제약)를 유지했다. 새 재등록 경로6기능/1WPF를 포함해 기존 전 회귀를 유지하며 OperationId/저널·Unknown/새 승인 전 판매0·민감정보 미노출을 검사했다. detached RTB의 loading/combo/product-empty/whole-management/새 자산 ID 검토125%·200%를 직접 열람했다. 최초75개 판정 **47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED** 유지, 실거래 전체 완료/REAL_FEE_VERIFIED/LIVE_SELL_VERIFIED/RELEASE_READY를 부여하지 않았다.

최신본/롤백은 ignored artifacts/releases.json에 기록했다. 롤백은 사용자 로그인 복원이 관측된0.17.0(session-1700bfd ZIP/SHA A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC)이며, 직전0.18.0 및 나머지 이전본도 모두 보존했다. 사용자 Auth/설정/SQLite/의도·예약/진단 증거 삭제0. 바이너리는 로컬 생성이며 GitHub Release 업로드가 아니다.

사용자 최소 확인: 새 폴더에서 직접 실행→저장 로그인 복원→미확인3기본 게임 없음/Steam 커뮤니티 이름·드롭다운 정렬·작업 중 아이콘/문구 확인→지갑/수수료 수동 갱신→판매목록 선택/즉시거래가능가 일괄 검토. 실제 취소/새 등록은 세션 동의와 현재 서버 계약을 통과한 사용자가 직접 실행한다. 취소 후 새 대상 재검토 시 기존 보유분일 수 있는 실제 자산 ID·새 가격·수량을 다시 확인해야 하며, 취소 불명·수수료/통화·자산/호가 불일치는 전송하지 않는다. 지정 경로에 없었던 진단 ZIP은 다시 첨부해야 실제 오류를 분석할 수 있다.


## 0.19.0 후속 배포 조건

실제 전용 self-contained x64·BusyIndicator/실제 재가격 서비스 포함과 Mock/자가검증/BulkRepriceContractServer/Usability 테스트 타입 제외를 PE/BAML로 확인한다. 추가 Pinned JSON은 기존 카탈로그를 읽고 저장 정보를 보존하며 DB/Auth 초기화하지 않는다. 운영 자료·0.18/0.17 ZIP/진단은 삭제하지 않는다.

사용자 최소 확인: 기본3게임 비노출→Steam 커뮤니티 이름/조회 후 상태·수량 갱신→작업 아이콘/문구/중단→판매목록 선택/즉시거래가능가 일괄 가격 검토→평균가 자료 부족/외부 반환 실행 불가 안내→로그 내보내기. 실제 거래는 이번 검증 범위가 아니다. 새 ZIP의 실행·물리 입력·DPI·실서버 수수료/재등록은 미검증으로 유지한다. 새 배포 경로/해시/전체 테스트는 확정 기록 참조.


### 0.18.0 배포 확정 — 수수료 모드/제품 화면

구현 커밋 **e6ee865861adbfaf46d91ae8a6680b2a0a7c2cb7**, origin/main push 완료. 새 로컬 portable ZIP **artifacts/releases/MarketCrate-0.18.0-fee-mode-e6ee865-win-x64.zip**, **66,478,248바이트 /413파일**. SHA256 **552CF3704C5A0B1A6C176351A314B7F2BE5922E09F8ABD8346C6F36B2556F44B**. 원본 `artifacts/releases/fee-mode-e6ee865-windows-x64`, BuildVersion0.18.0+e6ee865…. CLR/WPF10.0.9·SQLite x64·ProtectedData·기존 MIT 고지 포함, 경고·오류0. 전체 ZIP entry SHA와 소스 폴더 일치, 버전/commit/구성/라이선스·사용자 DB/로그/Auth/PDB 제외 검사 PASS. PE/BAML에서 Mock/자가검증 화면·모델 부재와 실제 resolver/Auth/거래 서비스 존재를 확인했다. 실제 EXE/설치·서명/계정·거래는 Codex 미실행이다.

최종 기능 **286/286**, 비표시 WPF **56/56**, 배포 계약 **19/19 PASS**. 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 제약). 사용자 운영 파일/배포본 정리0. 소스 변경 경로/키·JWT literal 검사에서 민감/산출물 경로0·실제 비밀값0을 확인했다. 기록은 ignored runtime/product17-*.txt/자체 RTB에 있고 개인 자료를 Git/배포로 전송하지 않았다.

롤백은 사용자 로그인 복원이 관측된 **0.17.0** ZIP, SHA256 **A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC**이며 이전0.16.1·사용자 진단/저널/Auth/설정도 보존했다. latest/rollback은 ignored artifacts/releases.json에 기록한다. 바이너리는 로컬 생성이며 GitHub Release로 업로드한 것은 아니다.

IMPLEMENTED/SYNTHETIC_VERIFIED는 위 범위이며 REAL_READ_VERIFIED는 사용자0.17 관측에 한정한다. 새 실제 수수료 모드/공식3구간 금액/등록·게시·취소·스택·새 버전 복원/물리 GUI는 NOT VERIFIED. **REAL_FEE_VERIFIED·LIVE_SELL_VERIFIED·RELEASE_READY 미달**, 최초75개47/15/0/4/9 유지. 사용자 최소 재확인은 새 폴더에서 직접 실행→로그인 복원→지갑·수수료1회→공식 화면3구간 대조이며 실패 시 거래/반복 조회 없이 문제 해결 ZIP을 내보낸다.

## 0.18 제품 후보

- Windows x64 self-contained LiveOnly·현재CLR/WPF·SQLite·DPAPI·라이선스/암호화 인증 제외·전체 ZIP entry SHA·버전/commit을 검사한다. 바이너리 실행/설치/서명 성공으로 표시하지 않는다.
- Test-ProductionAssembly에서 Mock/자가검증 VM·infra 모델·DevelopmentReadCheckView BAML 부재, 실제 FeeContractResolver/AuthStore/ListingExecutor 존재를 필수 검사한다. 테스트 소스는 개발 구성에 보존한다.
- 기존 Auth/Preferences/SteamExperimental/저널/설정/진단0건 삭제·초기화. 새 ZIP 성공 후 latest를 등록하고 사용자 복원이 관측된0.17 ZIP을 rollback으로 보존한다.0.16.1 이전본도 삭제하지 않는다.
- 사용자 첫 확인: 동일 Windows 계정에서 새 폴더에 압축 해제→실행/로그인 복원→정상 조회/지갑·수수료1회→실패 시 거래 중단·문제 해결 ZIP. 파일들을 일부만 덮어쓰거나 Auth를 패키지에 복사하지 않는다.
- 실제 새 버전 실행/복원/공식 수수료·실판매/물리 DPI는 사용자 단계다. 배포 산출물 정적 PASS는 RELEASE_READY/REAL_FEE_VERIFIED/LIVE_SELL_VERIFIED가 아니다.


### 0.17.0 배포 확정 — 로그인 유지

구현 소스 **1700bfdc6cb3cf0df984e7c76b0a14fff8558372**, origin/main push. Windows x64 self-contained LiveOnly portable **artifacts/releases/MarketCrate-0.17.0-session-1700bfd-win-x64.zip**, **66,486,397바이트 /413파일**, SHA256 **A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC**. 원본 `artifacts/releases/session-1700bfd-windows-x64`, BuildVersion0.17.0+1700bfdc6cb3cf0df984e7c76b0a14fff8558372. 전체 entry SHA/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData DLL/steam-session MIT 전문/고지·인증/DB/로그/개발파일 제외 검사 PASS. PE/BAML은 Mock 부재와 실제 Shell/Steam/AuthStore/refresh-token 검증 코드 존재를 확인했다. 경고·오류0, 실제 EXE/설치/서명은 미실행이다.

최종 자동: 기능270/270·자체WPF55/55·배포계약19/19 PASS, 안전 fixture 정리8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 환경제약). 실제 Windows 자체 CurrentUser DPAPI/ACL은 합성 비밀로 검증했다. 실제 Steam 저장 토큰 재사용·서버 회전/철회·수수료/판매·물리GUI·다른 Windows 사용자 복호화·전원 차단 시험은 미실행/미검증이다. LIVE_VERIFIED를 부여하지 않는다. 최초75개 판정47/15/0/4/9 유지, 실거래는 매 실행 OFF다.

롤백0.16.1 ZIP/SHA **94F7E423D49AA0F0AF6C4651A50486F95548A93B45EAE850B488480C70012C2F** 보존(완전 실사용/실거래 검증이라는 의미 아님), 사용자 관측0.16.0 및 그 이전 배포본/설정/SQLite/저널/진단/운영 인증 자료도 보존했다. 최신/롤백은 ignored artifacts/releases.json에 기록했다. 사용자 자료 삭제0·운영 Auth 접근0·실제 계정/GUI/타 프로세스/전역 입력/화면/시스템 설정 접근0. 최소 사용자 확인은 SESSION-PERSISTENCE의 'QR 전 유지ON→인증/저장 확인→로그아웃 없이 종료/재실행→직접 읽기 확인→명시적 로그아웃' 순서다. 토큰/쿠키/Auth 폴더는 진단 전달 대상이 아니다.

## 0.17.0 로그인 유지 배포 조건

실제 전용 x64 self-contained 유지, Mock/BAML 테스트 주입 제외 검사 필수. ProtectedData10.0.9 DLL과 steam-session MIT LICENSE·원본 커밋/변경 고지를 포함한다. Auth 디렉터리/refresh.authbin/store.authlock/refresh.pending/restore.blocked가 있으면 패키징을 거부한다. 개인 DB/로그/암호문/토큰/QR/개발 파일은 포함하지 않는다. Auth는 사용자 LocalAppData 전용이며 앱 설치/업데이트 디렉터리에 쓰지 않는다.

SQLite 저널/설정 마이그레이션 변경 없음. 최신 후보 생성·전체 entry/소스 커밋 해시 검증 뒤0.16.1을 롤백으로 등록하고 이전 배포본/사용자 자료는 보존한다. 실제 실행·설치/실계정 복원·다른 Windows 사용자·서명/전원 차단을 정적 PASS로 대신하지 않는다. 최종 ZIP/해시는 현재 상태·작업 기록에 확정한다.

### 0.16.1 배포 확정 — 수수료 HTML 오분류 수정

소스 **f5ed123aff841b85fa9a6865fa5a1f4a886004a9**, origin/main push 완료. 최신 Windows x64 self-contained LiveOnly portable: `artifacts/releases/MarketCrate-0.16.1-fees-f5ed123-win-x64.zip` (**66,475,258바이트 /412파일**), SHA256 **94F7E423D49AA0F0AF6C4651A50486F95548A93B45EAE850B488480C70012C2F**. 원본 폴더 `artifacts/releases/fee-f5ed123-windows-x64`, BuildVersion0.16.1+f5ed123aff841b85fa9a6865fa5a1f4a886004a9. Release 빌드·실제 전용 metadata/BAML·CLR/WPF10.0.9/SQLite x64·라이선스·민감/개발파일 제외·전체ZIP entry SHA 일치 검사 PASS. 실제 EXE/설치/서명·실계정/거래 미실행. 초기 NuGet 감사 메타데이터의 sandbox 네트워크 실패는 정상 접근으로 재빌드하여 해소했으며 감사/경고 검사를 끄지 않았다.

최종 기능250/250·WPF52/52·배포계약14/14 PASS, 경고·오류0. 안전정리7 PASS/1 NOT VERIFIED 유지(실제 junction 환경제약), 사용자 산출물 삭제0. 롤백은0.16.0 ZIP/SHA1E758055EB23B5060EEE57BA6E819AEC323E2B698A5AC8518EF307FE48E9704A이며0.15.1 이전본·진단/DB/저널/설정/인증자료도 보존했다. 최신/롤백은 ignored artifacts/releases.json에 기록했다. 바이너리/진단/개인 DB는 Git에 넣지 않는다.

**이 배포는 실제 수수료 성공을 확인한 버전이 아니다.** 확인된 HTML=Challenge 오분류/안전 처리/원인 진단을 수정했다. 실제 사용자 HTML 종류와 계정의 newFeeRates 적용/수수료 계약은 새 버전에서 지갑·수수료만1회 사용자 승인 조회한 정제 ZIP으로 확인해야 한다. 미확인 금액·통화·수수료의 거래는 계속 차단, 제재 위험/LIVE_VERIFIED 미확정 유지. 최초75개 판정47/15/0/4/9 변경없음. 앱·타 프로세스/화면/전역 입력 접근0.

## 0.16.1 수수료 진단 수정 후보

실제 전용 x64 self-contained 구성 유지, Mock 서비스/모델/구화면/BAML·테스트 주입을 제외하는 필수 PE/BAML 검사를 수행한다. 가격·수수료 미검증일 때 전송 차단을 유지한다. 새 ZIP은 버전0.16.1·소스 커밋을 이름/메타데이터에 고정하고 새 artifacts 폴더에만 생성한다. 기존0.16.0을 롤백으로 보존하며 그 이전 ZIP/폴더·모든 사용자 진단/DB/인증/설정/저널은 삭제하지 않는다.

정적 검사와 실제 EXE 실행·설치/서명/Steam 실계정 성공은 구분한다. 이번 작업에서 앱/계정/거래 실행0. 최종 경로·SHA256·전체entry 검사는 배포 확정 기록 참조.

## 0.16.0 LiveOnly 배포

### 0.16.0 LiveOnly 배포 확정

소스 **85756b4f541175cbd388d51c3d3d2fb1349e84e7** origin/main push. 새 x64 self-contained Release: `artifacts/releases/MarketCrate-0.16.0-live-85756b4-win-x64.zip` (66,469,582바이트/412파일), SHA256 **1E758055EB23B5060EEE57BA6E819AEC323E2B698A5AC8518EF307FE48E9704A**. 전체 entry SHA/SourceCommit·CLR/WPF10.0.9·SQLite/라이선스/민감·개발파일 제외 검사 PASS. RuntimeMode=LiveOnly, 필수 PE/BAML 검사로 Mock 구현/모델/구 화면·재시도/구 USD fixture 파서가 없는지 확인했다. 경고·오류0. 실제 EXE/설치·실계정/거래·서명은 미실행이다.

최신/롤백은 ignored artifacts/releases.json에 기록, rollback0.15.1의 SHA106E89E7C801250C1C264F92DDC2BE87710A70CD3591BD09B38ADDFD9BE820F2 재확인. 0.15.0/0.14/0.13과 기존 추출본·사용자 진단 ZIP/DB/저널/설정/인증·NuGet 캐시는 삭제하지 않았다. 실제 지갑의 성공/금액 계약은 사용자 확인이 필요하며 실패를 합성 성공으로 바꾸지 않는다. 최종 기능235/235·자체WPF50/50·배포계약14/14 PASS(개발 구성 회귀), Release 별도 빌드/메타데이터/전체ZIP 무결성 검사 PASS. 마지막 Shell 페이지 진단 이벤트 연결은 Release 빌드에서 확인했다.

Release 기본 MarketCrateProduction=true, 개발/테스트는 false이다. Directory.Build.targets의 조건부 심볼로 Core/Infrastructure/App의 Mock 구현과 legacy MainView BAML을 제외한다. 테스트 프로젝트의 ProjectReference는 명시적 false여서 기존 회귀를 유지한다.

Publish 및 Publish-SelfContained는 배포검사에 더해 Test-ProductionAssembly를 필수 실행한다. PEReader로 타입·메서드·BAML 리소스만 읽으며 앱/네이티브 DLL을 실행하지 않는다. Mock 구현/모델/저장소/재시도·구 USD fixture 파서가 있거나 실제 Shell/서비스가 없으면 실패한다. distribution.json RuntimeMode=LiveOnly를 기록한다. LiveOnly 패키지를 오프라인 Mock QA 패키지로 감싸는 작업도 거부한다.

기존 Demo 데이터는 읽거나 제거하지 않는다. 실제 SteamExperimental 저널 위치는 유지하고 새 비밀정보 없는 설정만 LOCALAPPDATA/MarketCrate/Preferences/preferences.sqlite3에 저장한다. 업데이트/롤백 시 파일을 초기화하지 않는다. 소스 테스트·로그·DB·인증·개발파일은 ZIP에서 제외한다.

## 0.15.0 배포 정책

0.15.1 배포: 소스 d2ee9a4679e24854102f09355ff2d5f125f8c05a, `artifacts/releases/MarketCrate-0.15.1-wallet-d2ee9a4-win-x64.zip` (66,507,841바이트/412파일). SHA256 `106E89E7C801250C1C264F92DDC2BE87710A70CD3591BD09B38ADDFD9BE820F2`. Release/전체entry SHA/SourceCommit·CLR/WPF/SQLite·민감/개발파일 제외검사 PASS, 경고오류0. 실제EXE/지갑/계정은 미실행이며 해결완료 판정 아님. 0.15.0을rollback으로등록하고0.14/0.13·기존추출본/진단ZIP·DB/저널/인증/설정은보존했다.

### 0.15.0 배포 확정

소스 **eb9ebd7ff79585dfbcd20ba17374eb4c4e155031** origin/main push. portable **artifacts/releases/MarketCrate-0.15.0-stage15-eb9ebd7-win-x64.zip** (66,506,796바이트/412파일), SHA256 **EF520920413636DC1456C30A3C94F6ADAD03C08D0082156ED0544983520F43D5**. 전체 ZIP entry SHA와 원본/SourceCommit 일치, x64 CLR/WPF10.0.9·SQLite 네이티브·라이선스 포함, DB/로그/인증정보/개발파일 제외, Release 경고·오류0. 실제 EXE·설치·실계정은 미실행, 디지털서명 미적용.

최신/롤백은 ignored artifacts/releases.json에 기록. 롤백은0.14 ZIP(SHA E1A001291F4AE0F6BE0C5410FA51421BD95E52612472BD9C187890CBED034680), 기존0.13 ZIP·모든추출본/진단/SQLite/저널/설정/인증/캐시는 그대로 보존했다. 이번 차수의 기존산출물 삭제0(성공한 신규합성fixture 자체정리만). 마지막 생성자 권한검사를 DB열기 앞으로 옮겨 차단시 lease잔류를 막고 활성화재요청은 먼저 권한철회하도록 한 뒤 전체기능223/223 재검사(runtime/stage15-last-core.txt), 기존48WPF/14배포검사와 새Release검사 유지. 동의해제·재로그인/계정변경 활성화보존 없음.

새고유버전/커밋폴더에 x64 self-contained publish, 전체ZIP entry hash/SourceCommit/아키텍처/CLR·WPF·SQLite·고지/개발·민감파일 제외검사. 실제창·설치·서명·실계정은 미실행. 0.14 최신본과0.13 이전사용자관측본/추출폴더·진단·저널·설정·계정파일 보존. 새latest index의rollback은0.14이며 구0.13도삭제하지 않는다. 정리는 기존 exact합성fixture dry-run 규칙외 추가없음. optional snapshot/reprice digest는 기존SQLite v3 자료를 초기화하지 않으며 구불명요청을 재실행하지 않는다.

## 0.14.0 배포·정리·롤백

14차 배포 구현커밋 3e5201cd5980a4bf36fb25e7dbb6f7cb5e81960f origin/main push. 새 `artifacts/releases/MarketCrate-0.14.0-stage14-3e5201c-win-x64.zip`:66,498,491바이트/412파일, SHA256 E1A001291F4AE0F6BE0C5410FA51421BD95E52612472BD9C187890CBED034680. 원본전체entry SHA·SourceCommit·CLR/WPF10.0.9/SQLite x64/라이선스/민감·개발파일 제외 PASS, Release경고오류0. 실제EXE/설치·실계정 미실행/서명미적용. 최신·롤백은 artifacts/releases.json.

검증된 새release 등록 후 dry-run11폴더/242파일(45,650바이트) 재검증·제거, skipped0. exact256byte 합성PE/고지fixture만 대상. 큰 실행폴더는 사용중/사용자내용 불확실하여 보존. 원본0.13 ZIP SHA256 2B76B0ACB3440CCBA5F2C2364AAD46BA458D10E89E329A8456400A0ED83BD7C8 재확인. 추출본/진단/SQLite/저널/설정/인증/캐시/LOCALAPPDATA 보존. ignored 증거 artifacts/cleanup-plan-f8ca226ee39e4e5aa6149f38d3fb6690.json 및 cleanup-result-755a1b5fc521421bb440139a33344cb5.json.

새 x64 self-contained는 고유버전/커밋 경로에 생성한다. 기존 CLR/WPF/SQLite 아키텍처·라이선스·민감/개발파일 제외 검사 유지. ZIP 전체entry SHA를 원본과 대조한 후 Register-Release.ps1로 artifacts/releases.json Latest/rollback 등록. 사용자자료는 기존 사용자별 저장소 보존. EXE/설치실행·디지털서명은 별도 미검증이다.

Clean-Artifacts.ps1 기본dry-run은 후보·크기·참조수·보존이유를 ignored artifacts/cleanup-plan-*.json으로 남긴다. -Apply -PlanPath <절대경로>는 현재버전 검증Latest·rollback SHA 일치 이후에만 실행한다. 자동삭제는 exact hash256byte 가짜PE/합성고지22파일의 runtime/distribution-tests-<guid>만 허용. 경로정규화/reparse경계/파일잠금/변조 재검증, 파일별삭제 후 빈폴더만 비재귀제거. unknown/locked는 보존. 실제junction 생성은 환경제약으로 미검증.

실행가능폴더·진단/DB/계정자료·NuGet캐시·프로젝트외부/LOCALAPPDATA는 대상이 아니다. 0.13 ZIP/추출본은 부분실사용 관측 롤백본이며 기능전체 정상인증본이라는 뜻은 아니다. 과거문서 배포경로는 이력으로 보존한다. 성공한 신규합성fixture는 자체정리하고 실패출력은 증거로 보존한다.

## 5차 추가 범위 (최신)

- 일반 앱에 지연 연결 UI를 포함한다. startup/탭 생성은 Steam HTTP나 실저널을 열지 않으며 실제 거래 policy는 기본 Blocked다. 합성 테스트 이외의 인증/계정 시험을 하지 않는다.
- Steam 전용 DB v1→v2 보존, audit/반환/재가격/배치 archive를 지원한다. Demo DB v2와 별개이며 실저널을 Demo 데이터 삭제에 연결하지 않는다. 실행파일/업데이트 버전과 데이터 폴더를 분리한다.
- MIT QR 소스를 고정 버전으로 포함하고 원 LICENSE/저작권 고지를 보존한다. publish에 `licenses/QrCodeGenerator-LICENSE.txt`와 THIRD-PARTY-NOTICES를 복사하고 누락 검사를 추가했다. 신규 패키지/SDK 설치 없음.
- Release 산출물 생성/정적 검사는 실제 설치/계정 연결/라이브 거래 검증이 아니다. 새 기기/모바일 QR 스캔/모니터 DPI/서명·설치·보안 감사는 미검증이다.

## 4차 추가 범위

- 실험적 Steam 소스가 포함되지만 App startup은 계속 Demo이며 연결/토큰/실저널 생성이 자동 실행되지 않는다. 새 패키지/외부 소스 편입 없음.
- 별도 실저널은 실행파일/버전과 분리된 전용 디렉터리에 두도록 composition 계약을 제공하며 Demo 삭제 UI에 연결하지 않았다. 인증정보 persistence 없음. 일반 사용자용 실계정 활성화 UI/업데이트/설치·보안 감사를 완료한 것이 아니다.

현재는 Mock 빌드 산출물 생성/정적 검사이며 정식 배포나 실판매 지원 완료가 아니다.

## 확인한 구조

- `scripts/Publish.ps1`: 설치된 SDK/캐시를 이용해 Release framework-dependent WinExe 생성, 단일 MSBuild 작업, 추가 다운로드 없음.
- 출력: `artifacts/windows-framework-dependent/MarketCrate.App.exe`, 앱/의존 DLL·런타임 설정·SQLite native 자산. Git 제외. PE MZ 헤더와 DB/로그/인증서 파일 제외 검사.
- 실행에는 .NET 10 Windows Desktop Runtime이 필요하다. self-contained/설치 프로그램/자동 업데이트는 구현하지 않았다.
- 데이터 경로 `%LOCALAPPDATA%/MarketCrate/Demo`는 실행파일 위치/버전과 분리되어 있다. 배포 스크립트는 이 경로를 읽거나 삭제하지 않는다. 테스트는 workspace/runtime의 격리 DB만 사용한다.
- SQLite v1→v2에서 설정/레코드를 보존한다. 실패 시 기존 데이터/버전을 유지하며 더 높은 버전은 자동 초기화하지 않는다. 구형 앱으로 DB를 다운그레이드하면 안 된다.
- 실제 Window/App 실행·포커스·입력·Steam 연결을 하지 않는다. 테스트 프로세스 종료 검증은 이 저장소의 headless 테스트 DLL만 CreateNoWindow로 실행하고 자기 프로세스에서 Exit한다. 타 앱을 찾거나 종료하지 않는다.

## 수행한 검증

- [x] Release publish·PE 헤더·출력의 로컬 DB/로그/키 파일 제외.
- [x] 실제 SQLite 파일 손상 거절·v1/v2 이관·실패 rollback·설정 보존.
- [x] 소유 headless 자식이 intent 저장 후 Dispose 없이 종료했을 때 Unknown 복구·재전송 0.
- [x] 소유 headless 자식이 미커밋 SQLite transaction 상태로 종료했을 때 이전 값 복구.
- [x] 고정 스키마 진단·유한 보존. 예외 메시지/인증정보/아이템 이름을 진단 로그 API가 받지 않음.
- [ ] 다른 Windows PC/클린 계정의 설치·실제 실행·삭제·업데이트.
- [ ] 코드 서명/SmartScreen/백신 호환성·종속성 취약점·배포 라이선스 원문 감사.
- [ ] 실제 모니터 DPI·입력·팝업·접근성/스크린리더.
- [ ] 실제 전원 차단·하드웨어/파일시스템 장애·장시간 안정성.

테스트 자식의 의도적인 Exit는 OS 강제 전원 차단이나 실제 GUI 프로세스의 장기 안정성을 재현한 것이 아니다. 산출물 생성/테스트 성공을 RELEASE_READY로 확대 해석하지 않는다.


## 0.20.1 후속 보완 (2026-10-29 KST)

0.20.1 후보: 실제 Steam 전용 Release와 기존 스키마3/사용자 Auth·Preferences·저널 유지. 소유 보관함 이동·빈 선택창·TBH 보완. 기능324/WPF66/배포계약19 PASS. 새 self-contained win-x64 ZIP은 소스 커밋 후 생성하고 전체 파일 해시/PE/BAML/CLR·WPF·SQLite·ProtectedData·라이선스/비밀·테스트 제외 검사 결과를 최신 배포 절에 확정한다. 실제 앱 실행/설치/서명/Steam 인증·조회·거래는 실행하지 않는다. 이전0.20와 사용자 관측 롤백0.17 및 진단 증거는 보존.

## 0.20.5 배포 확정

### 최신 배포 — 0.20.5

소스 **18e115fb769eae12b15278d48e67336dfa6e396e**. `artifacts/releases/MarketCrate-0.20.5-flow-audit-18e115f-win-x64.zip` **66,515,643바이트 /413파일**, SHA256 **45F14D1B5CB1D0E73DBD8954D00B6799B02753379D30D77DA02ED849E996C698**. 모든 ZIP entry SHA와 폴더 일치·SourceCommit·x64 CLR/WPF/SQLite·DPAPI·라이선스·실제 전용 PE/BAML·개발/Mock/사용자DB/Auth/로그/PDB 제외 검사 PASS. 기능346/346·WPF67/67·배포계약19/19·정리fixture9/9 PASS, 빌드 경고/오류0. 앱 실행·물리 GUI·설치·실계정·실제 속도는 미검증이다.

latest=0.20.5 /rollback=보존된 직전0.20.4. 사용자 Auth·DB·거래 저널·진단 및 이전 배포 삭제0. 로컬 portable ZIP이며 외부 Release 게시를 하지 않았다. 최종 승인 한 번의 전체 배치·Unknown 예약·수동 승인·서버 계약 보호는 유지한다. 새 요청 전 기존 Unknown을 공식 장터/보관함과 대조하고 재전송하지 않는다.

## 0.20.6 배포 확정

### 최신 배포 — 0.20.6

소스 **2aff72d5c480b132c851367abfcace770d6456d2**. `artifacts/releases/MarketCrate-0.20.6-inventory-reservation-2aff72d-win-x64.zip` **66,517,412바이트 /413파일**, SHA256 **84175BE31A9ADD5E52AD6FAA141C83983A99B114D2F9893FB2B9393AFAE8A247**. 모든 ZIP entry SHA/배포 폴더/버전·SourceCommit·CLR/WPF/SQLite x64·DPAPI·라이선스·실제 전용 PE/BAML·사용자DB/Auth/로그/PDB/Mock 제외 정적 검사 PASS. 기능348/348·비표시WPF68/68·배포19/19·정리fixture9/9 PASS, 빌드 경고/오류0. 실제 앱/계정/거래·물리 GUI는 미검증이다.

latest=0.20.6 /rollback=보존된0.20.5. 삭제0·사용자 Auth/설정/진단/SQLite/저널 초기화0. 새 버전은 실제 TBH 예약의 확정 해제나 실판매 성공을 의미하지 않는다. 사용자는 보관함의 **예약된 항목 확인 → 선택 요청 상태 확인**으로 실제 요청을 대조하고, Unknown이면 재전송하지 말고 그 작업의 진단 ZIP을 확인해야 한다. 원본 진단은 보존하고 Git/배포 ZIP에 포함하지 않았다.

## 0.20.7 배포 확정

### 최신 배포 — 0.20.7

소스 **139fb933f73a924036d5c8072d4e0d2466d9340d**. `artifacts/releases/MarketCrate-0.20.7-server-sync-139fb93-win-x64.zip` **66,518,925바이트 /413파일**, SHA256 **0E4A336850CCB00F3F069D9C8EECADF21450DAC391970E5CF0B2738CB75212F9**. 전체 ZIP entry SHA·폴더 일치/버전·SourceCommit/x64 CLR/WPF/SQLite/DPAPI/라이선스/실제 전용 PE/BAML·Mock/사용자DB/Auth/로그/PDB 제외 정적 검사 PASS. 기능350/350·비표시WPF68/68·배포19/19·정리fixture9/9 PASS, 빌드 경고/오류0. 마지막 검토 대상 보호 조회를 해시 방식으로 정리한 뒤 관련16계약을 추가 확인했다.

latest=0.20.7 /rollback=보존된0.20.6. 사용자 Auth/SQLite/저널/설정/진단·이전 배포 삭제0. 로컬 ZIP 생성이며 외부 Release 업로드0. 실제 앱/계정/판매·물리 GUI 실행0. TBH는 **보관함 갱신 → 서버1개 표시/선택 → 시세 갱신 → 최종 검토**를 직접 확인하면 된다. 이전 미확정 전송이 있으면 선택/가격 조회는 가능하지만 새 전송 전 근거 확인은 계속 필요하다. 실판매 성공이나 원인 미상400 해결로 판정하지 않는다.

## 0.20.8 배포 확정

### 최신 배포 — 0.20.8

소스 **96a5d0e6272bc226388b9647935b7b50fc3bc8e2**. `artifacts/releases/MarketCrate-0.20.8-immediate-stack-96a5d0e-win-x64.zip` **66,520,994바이트 /413파일**, SHA256 **21CFD8578BE1948A76694735E8199B421290F6B7C2834C5D8BBD1F2F0BC8746A**. 모든 ZIP entry SHA/폴더/버전·SourceCommit/x64 CLR/WPF/SQLite/DPAPI·라이선스/실제 전용 PE/BAML/평균·기간 선택 UI/사용자 DB/Auth/로그/PDB/Mock 제외 검사 PASS. 기능354/354·비표시WPF69/69·배포19/19·정리fixture9/9 PASS, 빌드 경고/오류0, 스냅샷 총액 등 관련20계약 추가 확인. BAML 출하 검사도 실제 새 출력에서 PASS했다.

latest=0.20.8 /rollback=보존된0.20.7. 기존 Auth/설정/SQLite/저널/진단/이전 배포 삭제0, 실제 앱/계정/판매·취소 실행0. 단일 가격 스택의 전체/부분 수량 계약은 공개 공식 소스+합성 검증이며 실계정 검증이 아니다. 다른 가격 분할 스택·new-mode 수량·ID 변경/합산 반환·ID 없는 원요청 귀속은 제한을 유지한다. 실제 거래 제재 위험과 과거400 원인 미확정은 남아 있다. 로컬 ZIP이며 GitHub Release 게시를 하지 않았다.

## 최신 배포 — 0.20.10

소스 b6d5f58a9a90f917f02663d16ddf5e693506d08f. `artifacts/releases/MarketCrate-0.20.10-server-state-b6d5f58-win-x64.zip` **66,529,683바이트 /415파일**. SHA256 **2DC8291C26D2DD1D549EDD200E3BB8DA03B89F45DBCA68A1AB3B1DBF40C3D418**. ZIP 전체 entry와 출하 폴더 SHA 일치, SourceCommit·버전·x64 CLR/WPF/SQLite·DPAPI·Apache LICENSE/NOTICE·실제 전용 PE/BAML·Mock/자가검증/사용자 DB/Auth/진단/PDB 제외 검사 PASS. 실행파일은 실행하지 않았다.

latest0.20.10, rollback0.20.9(f9da6c6, SHA2561812E5BF7A072828E6292A896D7176699154FF8104C2EE2C5943F1A6EB15C9D1)를 보존했다. 이전0.20.8을 포함한 배포/증거/운영 데이터 삭제0. 원본 진단은 Git/ZIP에 포함하지 않았다. 기능363/363·WPF69/69·배포19/19 PASS, 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 최종 오류 원인 연결 관련29계약 추가 PASS. 새 실제 앱/실계정/판매·취소·물리 UI·Guard는 미검증이다.

사용자 최소 확인: 새 ZIP을 별도 폴더에 풀기→로그인 복원→보관함/판매목록 갱신→대상 선택/시세 갱신→최종 검토→전체 판매등록 한 번 승인→항목별 접수/승인대기/실패/불명 확인. 오래된 개별 자산 불명 기록은 새 서버 확인과 별도 승인으로 진행 가능하며 최근 요청은2분 대기, 불명 스택은 별도 제한된다. 불명 발생 시 연속으로 승인하지 말고 해당 작업 진단을 확인한다. 서버 접수·공식 모바일 수동 승인·실제 게시 재조회는 서로 다른 단계다.

## 최신 배포 — 0.20.11

소스 **b487acc4df8f19c322fe6d2959fe19849c4b3c10**. `artifacts/releases/MarketCrate-0.20.11-guard-handoff-b487acc-win-x64.zip` **66,534,680바이트 /415파일**. SHA256 **CC83BA7F779935006FF0437794E7E80213B10119379527896832E35149E9285B**. 모든 ZIP entry SHA와 폴더 일치·SourceCommit/버전·x64 CLR/WPF/SQLite/DPAPI·실제 전용 PE/BAML·Mock/자가검증 UI/사용자 DB/Auth/진단/PDB 제외·SSIM LICENSE/NOTICE 전문 포함 및 원본 해시 일치 PASS.

기능367/367·비표시WPF70/70·배포19/19 PASS. 안전 정리fixture8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 새 앱 실행·설치·물리 GUI·실계정·휴대폰/실제 게시0. runtime/ui-tests/guard-handoff-125.png, guard-handoff-200.png, guard-posted.png는 격리된 자체 RTB 합성 이미지로만 검토했다. 최신본0.20.11/롤백0.20.10을 구분했고 이전 배포·Auth·설정·SQLite·저널·진단 삭제0. GitHub Release 바이너리 업로드0. 최초75개 판정47/16/0/3/9 유지.

사용자 최소 절차: 새 ZIP→로그인 복원→실제 보관함/수수료/시세 갱신→대상·가격·수량 최종 검토→전체 판매등록1회→모바일/이메일/실패/불명 결과 확인→공식 Steam 앱/이메일에서 직접 확인→‘승인 후 게시 확인’ 클릭. 알림 도착이나 실제 서버 호환성을 보장하는 배포가 아니며, Unknown 또는 필수 계약 미확인 시 후속 다중 등록을 중단하고 진단을 확인한다. 남은 실계정 증거는 사용자 직접 검증 단계다.


## 최신 배포 — 0.20.12

소스 c75ceff18c0a353e0cd2c8f47afd5836760c875d. artifacts/releases/MarketCrate-0.20.12-parity-c75ceff-win-x64.zip **66,536,415바이트 /415파일**. SHA256 **2C0AF766F53FCED230F29FA86A6AC7035E08D51B1876897D60BC64653D6E8534**. 전체 ZIP entry/출하 폴더 SHA 일치, SourceCommit/버전·x64 CLR/WPF/SQLite/DPAPI·라이선스/실제 전용 PE/BAML·Mock/자가검증/사용자DB/Auth/로그/PDB 제외 정적 검사 PASS.

기능372/372·비표시WPF71/71·배포19/19 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 마지막 문구 구분 후 관련 판매 계약 전체와 WPF71개 재검증. ID 없는 현재 게시 관측/요청 기록 분리 이미지를 직접 검토했다. 최신본0.20.12/롤백0.20.11(b487acc, SHA256 CC83BA7F779935006FF0437794E7E80213B10119379527896832E35149E9285B)을 보존했다. 이전 배포·Auth·설정·SQLite·저널·진단 삭제0, 원본 진단/공개 JS 다운로드 Git·배포 포함0, 외부 Release 업로드0.

검증 범위는 코드·공식 공개 소스·합성 계약·자체 렌더·배포 정적 검사다. 새 실제 앱·설치·물리 GUI·실계정·등록 접수·휴대폰 승인·실제 게시 확인은 미검증이다. 최초75개47/16/0/3/9 유지,100% 동등/LIVE_SELL_VERIFIED/전체 RELEASE_READY를 선언하지 않는다.

사용자 최소 재검증: 새 ZIP을 별도 폴더에 풀기→로그인 복원→실제 보관함/수수료/호가 갱신→단일 품목의 금액·수량 최종 검토→전체 판매등록1회→개별 결과의 미전송/접수/모바일·이메일 요구 확인→필요한 경우 공식 앱에서 수동 확인→승인 후 게시 확인. 현재 게시 관측과 원요청 귀속 미확인은 별도로 표시한다. Unknown/계약 불일치가 있으면 후속 다중 등록을 중단하고 해당 작업 진단을 확인한다.

## 0.20.13 — 최종 릴리즈 신뢰도 감사 (2026-10-31 KST)

같은 BatchId에 변경된 계정/가격을 전달해 이전 승인을 재사용하는 경로, 취소 후 과거 로컬 전송 기록으로 새 실제 보유 자산 검토가 막히는 경로, 타 계정 AssetKey의 pending 수량 혼합을 수정했다. 서버 inventory와 active/pending membership을 기준으로 새 대상·새 가격·새 최종 승인을 요구하며 기존 Unknown/원요청 ID/감사 저널은 보존한다. 관련 코드·수정 전/후 증거·출하 기준은 [RELEASE-READINESS](RELEASE-READINESS.md)를 따른다.

번들 Core/WPF10.0.14와 Microsoft.Data.Sqlite/ProtectedData10.0.14·SQLitePCLRaw2.1.14(native3.54.0)를 사용한다. 호스트 SDK/런타임 설치·설정 변경0. 구 provider 생성→신 provider 읽기/쓰기→구 provider 재읽기 합성 DB 호환 PASS. NuGet advisory는 명시적 공개 source/프로젝트 coverage를 검사하며 빈 source를 PASS로 판정하지 않는다. 패키지 감사와 런타임 보안 최소 버전 검사는 별개다.

최초75개47 COMPLETE/16 PARTIAL/0 MISSING/3 BLOCKED/9 UNVERIFIED를 유지한다. 새 실제 GUI·실계정·판매/취소/Guard0. 이전 사용자 관측을 새 릴리즈 실증으로 확대하지 않는다. Auth/Preferences/거래 저널/진단/롤백 삭제0. 실제 전용 출하와 단일 최종 승인 전체 배치, DPAPI, 정제 JSONL/ZIP, Unknown 비재전송, 타 프로세스 비간섭을 유지한다.

## 최신 배포 — 0.20.13

소스 **f1c86318e1f49a1b9f46fe69e5e56b6d25970248**, Windows x64 self-contained portable. `artifacts/releases/MarketCrate-0.20.13-stability-f1c8631-win-x64.zip` **66,738,853바이트 /418파일**. SHA256 **2F45D2EFD6C21D54AB30271D31F9448C3F15FE5F3E300B49B6AD10CF27B528CE**. 전체 ZIP entry/출하 파일 SHA 일치, SourceCommit/버전·x64 Core/WPF10.0.14/SQLite/DPAPI·원본 라이선스 byte 일치·실제 전용 PE/BAML·Mock/개발자가검증/사용자DB/Auth/로그/PDB 제외 검사 PASS. Release build 경고/오류0. 실제 앱/설치 실행은 미실행이다.

기능375/375·비표시WPF71/71·배포20/20·advisory 검사기6/6 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 공개 NuGet advisory 5개 프로젝트/직접·전이 검사 PASS. 번들10.0.14 콘솔 기능375/375, 구/신/구 provider의 active/archive/Unknown/Preferences 보존 PASS. 중간 개발 파일 저널 접근 거부1회와 정리 fixture 기대 파일 수 수정은 QA-MATRIX에 숨기지 않고 기록했다.

최신본0.20.13 /롤백0.20.12(c75ceff, SHA256 2C0AF766F53FCED230F29FA86A6AC7035E08D51B1876897D60BC64653D6E8534). 과거 배포·사용자 설정·Auth·SQLite·거래 저널·진단 삭제0, 외부 바이너리 업로드0. 최초75개47/16/0/3/9 유지. 새 실계정/휴대폰 승인/실제 게시·물리GUI 미검증이며 전체 RELEASE_READY/LIVE_SELL_VERIFIED/100%를 선언하지 않는다.

최소 사용자 확인: ZIP을 새 폴더에 풀기 → 로그인 복원 → 실제 보관함·판매목록·수수료·호가 직접 갱신 → 단일 항목의 새 가격/수량 최종 검토. 사용자가 거래 위험을 수락해 시험하는 경우에만 전체 판매등록1회 → 필요한 공식 모바일/이메일 확인 → 승인 후 게시 확인. Unknown/계약 불일치/금액 이상 시 추가 제출하지 않고 해당 작업 진단을 내보낸다. 취소 후 새 대상 검토는 원래 반환 확정과 다르며 실제 자산 ID·새 가격을 다시 확인한다.

## 최신 배포 — 0.20.14 사용자 경로 검증

소스 **1f3d95eaceab979197807a3ef4035ca8941cb251**. `artifacts/releases/MarketCrate-0.20.14-user-paths-1f3d95e-win-x64.zip` **66,740,827바이트 /418파일**. SHA256 **FDC735FF70F9CC1264A0EAFB296AE1B0EF80577678C0E3AFF65E4AA102F0238A**. Core/WPF10.0.14 x64 자체 포함·SQLite/DPAPI·실제 전용 PE/BAML·라이선스·사용자 DB/Auth/로그/테스트 UI/Mock/PDB 제외 및 ZIP 모든 파일 해시 일치 PASS. 실제 앱 실행/설치는 수행하지 않았다.

기능389/389·비표시WPF75/75는 기본 및 번들10.0.14 환경에서 각각 PASS다. 배포20/20·advisory 검사기6/6 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가), Release build 경고/오류0. 사용자 입력/실행 경로와 한계는 USER-JOURNEY-QA 참조. 무한 조작 조합100%·새 실계정/물리 GUI·전체 RELEASE_READY를 선언하지 않는다. 최초75개47/16/0/3/9 유지.

최신0.20.14 /롤백0.20.13(f1c8631, SHA256 2F45D2EFD6C21D54AB30271D31F9448C3F15FE5F3E300B49B6AD10CF27B528CE). 기존 Auth·설정·SQLite·거래 저널·진단·과거 배포 삭제0, 원본 운영 자료/테스트 산출물 외부 전송0. 사용자는 수량을 직접 입력한 직후 검토 수량 확인 → 잘못된 입력에서 검토 비활성·게임 전환 후 오류 보존 → 정상 수정/갱신 → 실제 금액 최종 확인 순으로 물리 GUI를 점검할 수 있다. 실제 거래 시험은 사용자 직접 범위이며 Unknown이면 추가 등록을 중단한다.

## 최신 배포 — 0.20.15 한 단계 하향 옵션

소스 **59e3191afe21c3d11a86e53d343c2f96c3e7b779**. `artifacts/releases/MarketCrate-0.20.15-lower-step-59e3191-win-x64.zip` **66,743,543바이트 /418파일**. SHA256 **3E55B1AECECE9728B381D0BC36DE9E2F155838FDE492EADC53F1BD5BE895A21D**. ZIP 모든 entry SHA/출하 파일 일치, SourceCommit/버전·x64 Core/WPF10.0.14/SQLite/DPAPI·라이선스·실제 전용 PE/BAML·Mock/테스트UI/Auth/DB/로그/PDB 제외 정적 검사 PASS. Release build 경고·오류0. 실제 앱/설치는 미실행이다.

전체 기능399/399·비표시WPF77/77·배포20/20·advisory 검사기6/6 PASS, 안전 정리8 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 불가). 최종 문구/관리 승인조건 및 수동 증가 비교를 포함한 신규10기능/2WPF는 관련 재검증과 번들 .NET/WPF10.0.14에서도 PASS.125/200% 자체 RTB의 비교/스크롤/버튼을 직접 검토했다. 옵션의 실제 서버 체결/물리 GUI는 미검증이다. 실제 앱·계정·판매·취소·Guard 실행0, 최초75개47/16/0/3/9 유지.

최신0.20.15/롤백0.20.14(1f3d95e, SHA256 FDC735FF70F9CC1264A0EAFB296AE1B0EF80577678C0E3AFF65E4AA102F0238A). 이전 배포·설정·DPAPI Auth·SQLite·저널·진단 삭제0, 외부 바이너리 업로드0. 사용자 확인: 새 폴더에 ZIP 해제 → 로그인/직접 갱신 → ‘즉시거래가능가보다 한 단계 낮게’ 직접 선택 → 최종 검토의 기본/변경 가격·수수료·실수령 감소액/비율 확인 → 원하는 경우 전체 판매등록 한 번 승인 → 공식 앱 수동 확인/서버 게시 대조. 최저 하향 불가 또는 계약/시세 변경은 옵션/대상/자료를 바꿔 새로 검토해야 한다. 자동 반복 인하나 체결 보장은 없다.
