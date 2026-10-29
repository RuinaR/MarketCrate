7차 Release 검사 수정: SQLite 파일명 제외 정규식이 정상 의존성 Microsoft.Data.Sqlite.dll까지 거부했다. DB/sidecar 확장자 끝으로 한정해 수정하고 DLL 허용·sqlite/sqlite3-wal/db-shm/.env/session.json/private.key 차단 7개 파일명 회귀를 확인했다. 실제 비밀파일·DB는 생성하지 않았다. 최초 publish는 이 정적 검사에서 FAIL이며 실행파일은 실행하지 않았다.

0.20 최종 배포: source13a339108d082f04fac81fb98ab193e27d7e9108, artifacts/releases/MarketCrate-0.20.0-product-audit-13a3391-win-x64.zip(66,504,701바이트/413파일), SHA256 461D4FD228A9D0EE5025F74C0B0B4AF6F2870A67BF07D0966CBD8B229679CC4E. 전체 entry 해시/metadata/민감·개발파일 제외 PASS, 기능315/WPF65/배포19/정리fixture9 PASS. 실제 실행/서명/Steam 계정·거래/물리GUI는 미검증. 이전 배포/운영자료 보존, 상세CURRENT-STATUS 배포 확정 절 참조.


0.20 마지막 대량 갱신 보완: 동일 상품10,000개에 SetPrice를 적용할 때 각 행의 Changed가 전체 선택 요약을 다시 계산하던 경로를 기존 Bulk로 묶었다. PriceNotifications 합성 회귀는10,000개 가격 전부 반영·요약 알림10회 미만·거래 전송0을 확인한다. 이는 실행 시간/CPU 개선 비율을 측정한 벤치마크는 아니다. 최종 기준315개 기능/65개 비표시WPF이며 아래 중간312·314 기록과 구분한다.

### 0.20 최종 자동 검증

**기능315/315·비표시WPF65/65·배포계약19/19·안전 정리 fixture9/9 PASS**, Debug/Release 경고·오류0. 기준0.19의305/61과 전 회귀를 유지했다. 신규10기능/4WPF에는 실시간 진행, 취소 대조·취소 전용, 원자 저장 오류 주입, 이전 계획 선택, 로그아웃 늦은 결과, 캐시 선택 보존, 계정 보관 보호와50개 Command Binding 검사가 포함된다. 배포 PE/BAML에서 실제 서비스·진행/작업 선택 타입 존재와 Mock/자가검증/테스트 타입 부재를 검사한다. 자체 RTB의 새 내비게이션·카드/목록·compact 검토·진단 복귀를 직접 열람했다. 개발 구성의 자가검증 탭은 Release에서 제외되며 렌더와 실제 출하 검사를 구분한다.

제한 실행에서 개발 전용 JSON 저널 파일 교체1건은 FAIL이었다. 동일 바이너리 정상 권한312/312 이후, 마지막 선택·보관 가드를 포함한 최종315/315 정상 권한 검사를 확인했다. 실제 GUI/물리 입력/Popup/모니터DPI·장시간 리소스·실서버 수수료/거래는 미검증이다. 실제 창/로그인·인증/조회·판매·취소·Guard 실행0, 타 프로세스/호스트 입력·화면/설정 접근0, 운영 데이터·이전 배포본 삭제0이다. ZIP/SourceCommit/해시는 배포 확정 절에 추가한다.


## 2026-10-29 —0.20 전체 요구사항 및 제품 경로 감사

main23589e7 clean에서 시작. 75개 추적표와 실제 ViewModel/Provider/Executor/SQLite/ControlTemplate·테스트를 대조했다. A06의 '메모리 전용' 등 과거 표 설명을 DPAPI 저장과 사용자0.17 관측 범위로 수정했다. COMPLETE 선결론 없이 B08만 BLOCKED→PARTIAL, 나머지는 기존 근거/제한을 유지한다.

발견·수정: 화면 캐시 제거가 선택 대상도 지움→선택 참조 별도 보존/전략 변경 시 함께 초기화/게임별 해제; Progress 미주입→동일 UI 문맥에서 결과별 갱신/로그아웃 후 늦은 결과 폐기; 계획 선소비→배치 최초 기록과 원자 연결; 최신 계획 고정→작업 선택/유지; 취소 상태 확인 미연결→선택 요청별 실제 대조; 취소가 견적·보관함에 종속→명시적 취소 전용 세션; 계정 결속 보관 가드; read latch의 재시도 안내 오류. 실제 전용 카드 보기도 연결했다.

SteamHelper/CSlytics 공개 PNG 화면을 직접 열람했다. 선택 요약·검색/목록·가격 검토/일괄 승인 배치를 참고했으며 이미지·AGPL 소스는 제품에 복사하지 않았다. SSIM28a503f의 MarketService MassSellJob(단계·queued/done/failed/deferred)을 비교해 기존 IProgress 경로를 활용했다. 자동 Guard·병렬/맹목 재시도는 채택하지 않았다.

중간 검증에서 테스트용 FileBatchJournal의 파일 교체 권한 오류1건이 제한 실행에서 발생했다. 동일 바이너리를 정상 권한으로 실행하자312/312 PASS였다. 해당 JSON 저널은 Release 컴파일 제외이며 원인을 임의 Steam/SQLite 결함으로 단정하지 않았다. 초기 추가 회귀의 SQLite 외부 테스트 연결 Pooling 기본값 때문에 정리가 실패한 것은 Pooling=False로 수정했고, 계획 선택 테스트는 기존 선택 보존을 검증하도록 바로잡았다. WPF 내비게이션 테스트는 새 표시 순서와 기존 논리 WorkPage 매핑을 구분했다. 물리 GUI/실계정은 실행하지 않는다.


### 0.19.0 배포 확정 — 사용자 결함·새 자산 재등록

구현 소스 **cb2c32685cef5c6acb9c3d5faf87f39acd48d65c**, Windows x64 self-contained LiveOnly portable **artifacts/releases/MarketCrate-0.19.0-relist-cb2c326-win-x64.zip**, **66,495,539바이트 /413파일**. SHA256 **42D92ECDDF23BE4789150B52C1585A802C71BBBD31E9AED8155E3A1AE86528D6**. 원본 `artifacts/releases/relist-cb2c326-windows-x64`, BuildVersion0.19.0+cb2c326…. 전체 ZIP entry SHA/폴더 파일 수/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData·라이선스 일치, 사용자 DB/Auth/로그/PDB 제외·실제 UI/SteamRelistPlan 존재·Mock/자가검증 모델/BAML 부재 검사 PASS. 앱/설치/서명·실제 계정/거래/물리 입력은 Codex 미실행이다.

최종 자동 **기능305/305·비표시WPF61/61·배포계약19/19 PASS**, Debug/Release 경고·오류0. 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 환경제약)를 유지했다. 새 재등록 경로6기능/1WPF를 포함해 기존 전 회귀를 유지하며 OperationId/저널·Unknown/새 승인 전 판매0·민감정보 미노출을 검사했다. detached RTB의 loading/combo/product-empty/whole-management/새 자산 ID 검토125%·200%를 직접 열람했다. 최초75개 판정 **47 COMPLETE/15 PARTIAL/0 MISSING/4 BLOCKED/9 UNVERIFIED** 유지, 실거래 전체 완료/REAL_FEE_VERIFIED/LIVE_SELL_VERIFIED/RELEASE_READY를 부여하지 않았다.

최신본/롤백은 ignored artifacts/releases.json에 기록했다. 롤백은 사용자 로그인 복원이 관측된0.17.0(session-1700bfd ZIP/SHA A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC)이며, 직전0.18.0 및 나머지 이전본도 모두 보존했다. 사용자 Auth/설정/SQLite/의도·예약/진단 증거 삭제0. 바이너리는 로컬 생성이며 GitHub Release 업로드가 아니다.

사용자 최소 확인: 새 폴더에서 직접 실행→저장 로그인 복원→미확인3기본 게임 없음/Steam 커뮤니티 이름·드롭다운 정렬·작업 중 아이콘/문구 확인→지갑/수수료 수동 갱신→판매목록 선택/즉시거래가능가 일괄 검토. 실제 취소/새 등록은 세션 동의와 현재 서버 계약을 통과한 사용자가 직접 실행한다. 취소 후 새 대상 재검토 시 기존 보유분일 수 있는 실제 자산 ID·새 가격·수량을 다시 확인해야 하며, 취소 불명·수수료/통화·자산/호가 불일치는 전송하지 않는다. 지정 경로에 없었던 진단 ZIP은 다시 첨부해야 실제 오류를 분석할 수 있다.


## 0.19.0 추가 — 외부 등록의 새 자산 재판매

SteamMarketHelper의 취소→보관함 재조회→같은 상품의 새 자산 ID 선택 방식을 실제 서비스에 연결했다. 전체 취소 승인 후 서버 접수·목록 부재를 확인하고, 실제 보관함·가용 수량·수수료·공유 호가로 새 대상을 제안한다. 원래 아이템의 반환 확정은 아니며 다른 기존 보유분일 수 있음을 표시한다. 새 실제 자산 ID·수량·가격을 전체 검토해 판매등록 한 번 승인하면 기존 Coordinator가 배치 처리한다. 항목별 추가 판매 클릭은 없다. SQLite 취소 계획/새 배치 연결을 보존하고 불명 취소·원래 예약은 추측으로 해제하지 않는다. 최근 미제출 계획은 사용자 요청 시 재조회하며 자동 거래하지 않는다. 개별 수량1, 최대100등록/10상품만 지원한다.

추가 합성6개/비표시WPF1개: 변경ID·재시작 후 새 승인, 취소만으로 판매0, Unknown 재전송0, 전체 수량 부족·호가 변경 차단, 계획 불변·OperationId/비밀정보 보호. 최종 기능305/305·WPF61/61·배포19/19 PASS, 경고/오류0. 취소/새 대상 검토125%·200% RTB를 직접 검토했다. 실제 계정/수수료·판매/물리 입력은 미검증이다. 상세 호출 경로·보존/재시도 제약은 [판매 실행 계약](SALE-EXECUTION-SPEC.md)의 0.19 추가 절을 따른다.


0.19 최종 자동 검증: **기능305/305·비표시 WPF61/61·배포계약19/19 PASS**, 경고·오류0. 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 제약). 기존286/56 전 회귀 유지. 신규19기능/5WPF에 재등록 Guard→서버 Listed 읽기 대조(추가 전송0), 타 계정 저널 불변 검사를 포함했다. 재등록 재조회는 현재 계정 최근 최대10건이며 다른 의도는 선택 대조한다. RTB usability-loading/combo-alignment/product-empty/bulk-review-small/200을 직접 검토했다. 물리 입력/팝업/모니터 DPI·장시간 안정성/실계정은 미검증. 최종 로컬 ZIP/해시는 배포 확정 절 참조.

0.19 추가 감사: 재등록 후 상태 재조회가 원 배치만 읽던 경로를 보완하여 현재 계정의 최근 재등록 최대10건을 실제 명령/서버와 대조합니다. Guard→Listed 합성 경로를 확인했으며, 타 계정 배치를 복구해 변경하지 않도록 계정 필터와 저널 불변 회귀를 추가했습니다. 과거 기록은 보존합니다.

## 2026-10-29 —0.19.0 실사용 결함 후속

main8fc445d clean·origin/main0/0 기준. 첨부 화면은 보았으나 지정 ZIP0c9de…는 FileNotFound여서 열지 못했다. 타 진단 폴더/프로세스/창/인증 저장소를 탐색하지 않았다. 사용자 답변으로 미보유 게임은 기본3추천임을 확인하고 수정했다.

발견/수정: 일반 카탈로그의 하드코딩3추천(개발 회귀에만 보존), App753 임시명, SteamGameScope.Update의 StateText/MarketableItems 알림 누락, Combo 선택 텍스트 상단 정렬·문자 화살표, Busy 중 이전 상태 문구만 표시, 조회 중 빈 목록을 연결 필요로 오인시키는 안내, 판매 중 가격 기준별 일괄 계획 미연결, 재가격 전체 사전검증 누락, 재가격 Guard를 Submitted로 뭉뚱그리는 UI, 일부 중단 후 남은 미전송 결과 누락을 보완했다. 승인 중 가격 편집을 잠갔다.

기존 SalePlanner/SteamFees/SteamListingExecutor/SQLite/AsyncCommand를 재사용했다. 신규 일괄 계획은 상품 단위 공유 호가를 배분하고 전송 전 전체 검토·스냅샷 가격/5분 만료/서버/수수료/계정을 대조한다. 외부 등록은 반환 근거가 없어 사전에 차단하며 같은 이름을 임의로 고르지 않는다. 사용자 입력 단가 검토도 유지한다. Catalog Pinned는 JSON 부가 필드로 이전 자료를 읽으며 SQLite 스키마/거래 저널/DPAPI 포맷 변경·초기화0이다. 숨긴 추천도 저장하며 직접 추가한 기본 게임의 재시작 보존을 검사했다.

SteamMarketHelper e186d51 MIT 실제 act(relist) 소스를 확인했다. 상품별 조회/큐/진행/취소 후 재조회 구조는 참고했지만, 새 자산 ID를 market_hash_name으로 선택해 재판매하는 부분은 기존 인과/Unknown 보호와 충돌하여 미채택했다. 소스 직접 복사/새 라이브러리0. 평균 자료·외부 반환의 미완성을 숨기지 않고 UI/Current/Trace에 기록했다.

신규 UsabilityTests13개/BulkRepriceContractServer, WPF4개. 정상 복수 재등록→Guard/3·4·3/호가 부족/외부 선차단/시세 변경/응답 유실·재시작/ID 변경/Busy 취소·노출/저장 카탈로그·바인딩을 검증한다. 중간 WPF1실패는 독립 ComboBox 루트의 암시적 스타일 미적용 테스트 구성으로, 대상 커스텀 스타일을 명시해 실제 템플릿 정렬을 검사했다. 실계정/물리 입력/Popup/모니터DPI/장기 부하·메모리 인증은 미실행이다.


### 0.18.0 배포 확정 — 수수료 모드/제품 화면

구현 커밋 **e6ee865861adbfaf46d91ae8a6680b2a0a7c2cb7**, origin/main push 완료. 새 로컬 portable ZIP **artifacts/releases/MarketCrate-0.18.0-fee-mode-e6ee865-win-x64.zip**, **66,478,248바이트 /413파일**. SHA256 **552CF3704C5A0B1A6C176351A314B7F2BE5922E09F8ABD8346C6F36B2556F44B**. 원본 `artifacts/releases/fee-mode-e6ee865-windows-x64`, BuildVersion0.18.0+e6ee865…. CLR/WPF10.0.9·SQLite x64·ProtectedData·기존 MIT 고지 포함, 경고·오류0. 전체 ZIP entry SHA와 소스 폴더 일치, 버전/commit/구성/라이선스·사용자 DB/로그/Auth/PDB 제외 검사 PASS. PE/BAML에서 Mock/자가검증 화면·모델 부재와 실제 resolver/Auth/거래 서비스 존재를 확인했다. 실제 EXE/설치·서명/계정·거래는 Codex 미실행이다.

최종 기능 **286/286**, 비표시 WPF **56/56**, 배포 계약 **19/19 PASS**. 안전 정리 fixture8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 제약). 사용자 운영 파일/배포본 정리0. 소스 변경 경로/키·JWT literal 검사에서 민감/산출물 경로0·실제 비밀값0을 확인했다. 기록은 ignored runtime/product17-*.txt/자체 RTB에 있고 개인 자료를 Git/배포로 전송하지 않았다.

롤백은 사용자 로그인 복원이 관측된 **0.17.0** ZIP, SHA256 **A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC**이며 이전0.16.1·사용자 진단/저널/Auth/설정도 보존했다. latest/rollback은 ignored artifacts/releases.json에 기록한다. 바이너리는 로컬 생성이며 GitHub Release로 업로드한 것은 아니다.

IMPLEMENTED/SYNTHETIC_VERIFIED는 위 범위이며 REAL_READ_VERIFIED는 사용자0.17 관측에 한정한다. 새 실제 수수료 모드/공식3구간 금액/등록·게시·취소·스택·새 버전 복원/물리 GUI는 NOT VERIFIED. **REAL_FEE_VERIFIED·LIVE_SELL_VERIFIED·RELEASE_READY 미달**, 최초75개47/15/0/4/9 유지. 사용자 최소 재확인은 새 폴더에서 직접 실행→로그인 복원→지갑·수수료1회→공식 화면3구간 대조이며 실패 시 거래/반복 조회 없이 문제 해결 ZIP을 내보낸다.

## 2026-10-29 — 17차 / 0.18.0

main07056d8 clean·origin/main0/0에서 시작. AGENTS/제품 목표/상태/기록/관련 QA·설계를 읽었다. 공식 공개 JS를 압축 해제해 재검토하고 main feature props 생산/현대 dialog !newFeeRates 분기를 확인했다. 사용자 계정의 실제 mode HTML은 읽지 않았으며 운영 Auth/DB/로그/진단을 변경하지 않았다.

FeeContractResolver와 동일 계정 sell-context 읽기 allowlist를 추가하고, 무조건 새 API 호출을 제거했다. 앱 조회·활성화·최종 배치·재가격3곳을 모두 연결했다. legacy 5필드의 enabled도 보존/검증하고 현재 false 분기와 충돌하는 historical base/minimum은 지원하지 않는다고 명시했다. KRW 원/wire·USD·최소/증분/역산은 기존 정수 엔진을 재사용했다. 자산 publisher 비율 누락/충돌의 UI 검토 차단을 추가했다. 과거 등록을 필수로 요구하던 순환 조건을 현재 서버 모드/앱/품목 증거로 대체하고 기존 등록의 충돌 veto를 보존했다.

최초 모드 구현 후 전체 기능284/284 PASS를 확인한 뒤 UI 제품화를 시작했다. 전용 자가검증 UI/VM/infra 모델은 개발 구성에 보존하되 Release 컴파일 제외. 정상 직접 조회·저장 로그인 관리·실제 거래/저널·진단은 유지했다. 지갑 단독 조회 및 마지막 KRW 관측 표시,3구간×4금액 비교, 내부 코드 상시 노출 제거, 문제 해결 내비게이션을 연결했다. 역사 고정 날짜로 만료되던 합성 이력을 상대 시각으로 수정했으며 실제 신선도 정책은 유지했다.

최종 기능286/286·비표시 WPF56/56·배포 계약19/19 PASS, 정리 fixture8/0/1미검증. 새UI RTB를 직접 확인했다. 사용자 제공0.17 재시작 복원 성공은 실제 인간 관측으로 기록하며 새 버전/장기 회전·수수료/거래 검증으로 확대하지 않았다. 실제 계정/앱 창/타 프로세스/전역 입력/화면·설정 조작0. RELEASE_READY·REAL_FEE_VERIFIED·LIVE_SELL_VERIFIED 부여0. 새 릴리즈/롤백 파일과 최종 커밋은 배포 확정 절 참조.


### 0.17.0 배포 확정 — 로그인 유지

구현 소스 **1700bfdc6cb3cf0df984e7c76b0a14fff8558372**, origin/main push. Windows x64 self-contained LiveOnly portable **artifacts/releases/MarketCrate-0.17.0-session-1700bfd-win-x64.zip**, **66,486,397바이트 /413파일**, SHA256 **A025E48C5D2FEBBA1C19D99F6F3B1F0DD3A947DBD77F67850910D9FD3363E1CC**. 원본 `artifacts/releases/session-1700bfd-windows-x64`, BuildVersion0.17.0+1700bfdc6cb3cf0df984e7c76b0a14fff8558372. 전체 entry SHA/SourceCommit·CLR/WPF10.0.9·SQLite x64·ProtectedData DLL/steam-session MIT 전문/고지·인증/DB/로그/개발파일 제외 검사 PASS. PE/BAML은 Mock 부재와 실제 Shell/Steam/AuthStore/refresh-token 검증 코드 존재를 확인했다. 경고·오류0, 실제 EXE/설치/서명은 미실행이다.

최종 자동: 기능270/270·자체WPF55/55·배포계약19/19 PASS, 안전 fixture 정리8 PASS/0 FAIL/1 NOT VERIFIED(실제 junction 환경제약). 실제 Windows 자체 CurrentUser DPAPI/ACL은 합성 비밀로 검증했다. 실제 Steam 저장 토큰 재사용·서버 회전/철회·수수료/판매·물리GUI·다른 Windows 사용자 복호화·전원 차단 시험은 미실행/미검증이다. LIVE_VERIFIED를 부여하지 않는다. 최초75개 판정47/15/0/4/9 유지, 실거래는 매 실행 OFF다.

롤백0.16.1 ZIP/SHA **94F7E423D49AA0F0AF6C4651A50486F95548A93B45EAE850B488480C70012C2F** 보존(완전 실사용/실거래 검증이라는 의미 아님), 사용자 관측0.16.0 및 그 이전 배포본/설정/SQLite/저널/진단/운영 인증 자료도 보존했다. 최신/롤백은 ignored artifacts/releases.json에 기록했다. 사용자 자료 삭제0·운영 Auth 접근0·실제 계정/GUI/타 프로세스/전역 입력/화면/시스템 설정 접근0. 최소 사용자 확인은 SESSION-PERSISTENCE의 'QR 전 유지ON→인증/저장 확인→로그아웃 없이 종료/재실행→직접 읽기 확인→명시적 로그아웃' 순서다. 토큰/쿠키/Auth 폴더는 진단 전달 대상이 아니다.

### 0.17.0 최종 자동 검증

기능 **270/270 PASS**, 자체 비표시 WPF **55/55 PASS**, 배포 파일계약 **19/19 PASS**, 안전 fixture 정리 **8 PASS/0 FAIL/1 NOT VERIFIED**(실제 junction 생성 환경제약). 최종 빌드 경고·오류0. 신규 세션20/자체UI3개와 기존 전 회귀 포함. 직접 Windows CurrentUser DPAPI·ACL/암호문만 저장, 로그·검토ZIP canary 미노출, 인증 복원 시 Unknown 저널/예약 보존·거래0을 확인했다. 마지막 저장 예외 분류/계정 검증을 반영한 최종 source 증거는 runtime/session-build.txt·session-core.txt·session-ui.txt·session-distribution.txt·session-cleanup.txt에 있다.

RTB session-optin/session-restored-small/large/session-paused를 직접 확인했다. 실제 Windows 사용자 간 복호화·실제 GUI/물리입력·전원 차단·Steam 저장 토큰 재사용/회전·수수료·판매/취소는 NOT VERIFIED. NuGet 취약성 메타데이터를 끄거나 SDK/Windows 설정을 설치/변경하지 않았다. 최초75개 판정47/15/0/4/9 유지. 기존 운영 데이터/진단/릴리즈 삭제0. Release·ZIP 해시와 커밋은 확정 기록에 추가한다.

## 2026-10-29 — 16차 로그인 유지와 실제 소스 이식

main9de5197 clean에서 시작, origin/main0/0 확인. AGENTS에 사용자 명시 동의·DPAPI CurrentUser·자체 세션만 저장하는 예외를 추가했다. 실제 Steam 계정/읽기/거래·GUI/타 프로세스/화면·시스템 설정은 실행하지 않았다. 테스트는 프로젝트 격리 Auth 폴더의 합성 토큰만 사용했고 운영 Auth/DB/저널/진단을 열거나 초기화하지 않았다.

지정6개 프로젝트의 실제 auth/session/market/pricing 소스와 존재하는 관련 테스트·LICENSE를 대조했다. node-steam-session3ac0f34 MIT의 audience/계정 검증·WebBrowser cookie 발급을 제한 이식하고 전문/커밋/변경 고지를 보존했다. Node runtime과 AGPL 코드는 추가하지 않았다. GenerateAccessTokenForApp의 WebBrowser 제한, SSIM의 손상 저장소·통화 floor, SteamMarketHelper의 KRW round/floor·gap과 현재 보호를 비교했고 기본율/프록시·회피·Guard/평문·자동재시도를 도입하지 않았다.

구현: 별도 Auth store·사설 ACL·버전/SID/계정·만료 검사·전송 전 disarm·암호화 pending/원자 교체·원 파일 지문 대조·삭제경쟁 보호. App.Loaded에서 동의가 있는 경우만 단일 복원, QR/restore 후 저장·일반종료/명시로그아웃 분리·OFF/삭제·상태 UI/진단 연결. settoken 결과 JSON 생략은 실제 계정 쿠키가 있는 경우만 허용하도록 호환성을 보완했다. SQLite 금융/Unknown 경로는 유지했다.

중간 검증: 처음 DPAPI package restore가 로컬 전용 source에서 실패하여 공식 NuGet source를 명시해 복원했다. 신규 UI 파일의 System.IO using 누락은 빌드에서 수정했다. 잠금 실패 테스트는 Windows가 UnauthorizedAccessException을 반환하는 것도 정당한 실패로 확인했다. 배포 검사에 인증파일/pending/blocked 차단을 추가했고 테스트 fixture 정리의 기존22파일·신규24파일 정확한 집합/해시 구분을 보완했다. 모든 실패를 기록하고 수정 후 관련 검증을 수행한다. 실제 크로스 사용자 DPAPI/전원차단/실계정 성공은 테스트하지 않았으며 해당 미검증을 유지한다.

### 0.16.1 배포 확정 — 수수료 HTML 오분류 수정

소스 **f5ed123aff841b85fa9a6865fa5a1f4a886004a9**, origin/main push 완료. 최신 Windows x64 self-contained LiveOnly portable: `artifacts/releases/MarketCrate-0.16.1-fees-f5ed123-win-x64.zip` (**66,475,258바이트 /412파일**), SHA256 **94F7E423D49AA0F0AF6C4651A50486F95548A93B45EAE850B488480C70012C2F**. 원본 폴더 `artifacts/releases/fee-f5ed123-windows-x64`, BuildVersion0.16.1+f5ed123aff841b85fa9a6865fa5a1f4a886004a9. Release 빌드·실제 전용 metadata/BAML·CLR/WPF10.0.9/SQLite x64·라이선스·민감/개발파일 제외·전체ZIP entry SHA 일치 검사 PASS. 실제 EXE/설치/서명·실계정/거래 미실행. 초기 NuGet 감사 메타데이터의 sandbox 네트워크 실패는 정상 접근으로 재빌드하여 해소했으며 감사/경고 검사를 끄지 않았다.

최종 기능250/250·WPF52/52·배포계약14/14 PASS, 경고·오류0. 안전정리7 PASS/1 NOT VERIFIED 유지(실제 junction 환경제약), 사용자 산출물 삭제0. 롤백은0.16.0 ZIP/SHA1E758055EB23B5060EEE57BA6E819AEC323E2B698A5AC8518EF307FE48E9704A이며0.15.1 이전본·진단/DB/저널/설정/인증자료도 보존했다. 최신/롤백은 ignored artifacts/releases.json에 기록했다. 바이너리/진단/개인 DB는 Git에 넣지 않는다.

**이 배포는 실제 수수료 성공을 확인한 버전이 아니다.** 확인된 HTML=Challenge 오분류/안전 처리/원인 진단을 수정했다. 실제 사용자 HTML 종류와 계정의 newFeeRates 적용/수수료 계약은 새 버전에서 지갑·수수료만1회 사용자 승인 조회한 정제 ZIP으로 확인해야 한다. 미확인 금액·통화·수수료의 거래는 계속 차단, 제재 위험/LIVE_VERIFIED 미확정 유지. 최초75개 판정47/15/0/4/9 변경없음. 앱·타 프로세스/화면/전역 입력 접근0.

### 0.16.1 최종 자동 검증

기능 **250/250 PASS**, 자체 비표시WPF **52/52 PASS**, 배포 파일계약 **14/14 PASS**, 최종FAIL0·빌드 경고/오류0. 안전정리7 PASS/0 FAIL/1 NOT VERIFIED(junction 생성 환경제약), 이번 사용자 배포/데이터 정리0. 신규 수수료/복구 오류 주입·JSONL 원인 연결/비밀 canary 미노출 및 기존 거래 어댑터·가격·SQLite/Unknown 회귀 포함. runtime/fee-final-core.txt·fee-final-ui.txt·fee-final-build.txt·fee-distribution-contracts.txt·fee-cleanup-contracts.txt에 로컬 증거를 남겼다. RTB fee-response-error-row/fee-comparison-small/large·오류 화면 직접검토, 물리GUI/모니터DPI/설치·실계정 수수료/거래·서명 미실행. 최초75개 판정47/15/0/4/9 유지. 중간1회 기존 복구 검사 실패/미확정 원인과 결정적 게이트 보강은 WORKLOG에 보존했다. Release 정적 검사와 실제 실행 검증을 혼동하지 않는다.

### 최종 회귀 중 확인한 개발용 복구 게이트

최종 회귀 중 기존 개발용 Mock 재시작 검사가1회 실패(248/249), 파일 읽기→VM복구를 정제 이벤트로 분리한 단독 검사에서는 재현되지 않았다. 최초 실패 원인은 미확정이며 수수료 HTTP 원인으로 돌리지 않는다. 별도 코드 감사에서 복구 실패를 catch한 뒤 record=null이면 검토/저널해제 명령이 열리는 개발 경로를 확인했다. 복구 미확인 게이트를 추가하고 결정적 IOException 주입으로 수정 전 Review 조건에서 실패, 수정 후 차단·명시적 복구 후에만 허용됨을 검증했다. 이는 기존 개발 회귀 경로의 안전 수정이며 MainViewModel 자체는 Release 컴파일 제외를 유지한다. 최초 실패가 이 원인이었다고 확정하지 않는다. 재현 증거 runtime/fee-recovery-baseline.txt·fee-recovery-fixed.txt, 최종 전체 집계는 아래 기록.

## 2026-10-29 — 0.16.1 수수료 응답 계약 후속

main f2535e1 clean에서 시작, origin fetch 후0/0 확인. 사용자0.16.0 ZIP의 이전 분석: Wallet eligibility 왕복/200 성공, Fees200 HTML 후 Challenge·후속차단. 새 실계정 요청/인증·거래는 하지 않았다. 원문 진단·DB·설정·저널·이전 실행폴더/ZIP 삭제·복사·업로드0.

공식 sell dialog 압축을 해제해 SHA CB84BB8A9B2970F71D277626A0B4E922B47CCE90BF437F704CF8F5100FA66726와 newFeeRates 조건부 GET/legacy 비율 경로를 확인. 무인증 공개 fee URL200/text-html/장터 화면도 관측했으나 사용자 원인과 동일시하지 않음. 사용자 추가 요청에 따라 CSlytics/SteamMarketHelper/steam-bulk-seller/steampy 소스의 wallet·정수fee·round/floor·KRW 지원 한계를 비교했다. AGPL/불명LICENSE 코드 도입0. 외부 자료의 명령/AGENTS 지시를 실행하지 않았다.

구현: 고정 응답 분류/3005 whitelist, HTML=Challenge 오분류 제거, 실제 challenge/권한/unknown 중단 구분, endpoint별 계약 latch·독립 조회 보존, JSON Accept/유효AppID 검증, optional legacy fee 관측(판매용 대체0). UI 상단의 계약불일치/권한 구분, 고급 공식 단가 비교(판매 계획/권한 불변). 새14기능/2UI 테스트와 직접 RTB 검토.

중간 검증에서 신규 테스트의 nullable 참조와 잘못된 WorkPage 이름을 빌드가 검출했고 수정했다. 첫 UI 검사는 테스트가 범위 변경 전 승인해 승인이 무효화된 준비 순서 때문에1FAIL이었으며, 실제 사용자 동작 순서대로 범위→승인으로 고친 뒤 재검증했다. 최종 결과·Release ZIP 무결성/커밋은 확정 기록에 누적한다. 실제 서버 수수료 성공은 아직 미검증이고, 이를 해결 완료나 LIVE_VERIFIED로 보고하지 않는다.

## 2026-10-29 — 0.16.0 실제 전용 Release

### 0.16.0 LiveOnly 배포 확정

소스 **85756b4f541175cbd388d51c3d3d2fb1349e84e7** origin/main push. 새 x64 self-contained Release: `artifacts/releases/MarketCrate-0.16.0-live-85756b4-win-x64.zip` (66,469,582바이트/412파일), SHA256 **1E758055EB23B5060EEE57BA6E819AEC323E2B698A5AC8518EF307FE48E9704A**. 전체 entry SHA/SourceCommit·CLR/WPF10.0.9·SQLite/라이선스/민감·개발파일 제외 검사 PASS. RuntimeMode=LiveOnly, 필수 PE/BAML 검사로 Mock 구현/모델/구 화면·재시도/구 USD fixture 파서가 없는지 확인했다. 경고·오류0. 실제 EXE/설치·실계정/거래·서명은 미실행이다.

최신/롤백은 ignored artifacts/releases.json에 기록, rollback0.15.1의 SHA106E89E7C801250C1C264F92DDC2BE87710A70CD3591BD09B38ADDFD9BE820F2 재확인. 0.15.0/0.14/0.13과 기존 추출본·사용자 진단 ZIP/DB/저널/설정/인증·NuGet 캐시는 삭제하지 않았다. 실제 지갑의 성공/금액 계약은 사용자 확인이 필요하며 실패를 합성 성공으로 바꾸지 않는다. 최종 기능235/235·자체WPF50/50·배포계약14/14 PASS(개발 구성 회귀), Release 별도 빌드/메타데이터/전체ZIP 무결성 검사 PASS. 마지막 Shell 페이지 진단 이벤트 연결은 Release 빌드에서 확인했다.

main7c7ba05 clean에서 시작. 사용자 지정 ZIP 하나만 OpenRead하여 정제된 분류를 확인했다. 원문 ZIP/로그·계정 정보는 복사/커밋하지 않았다. 새 증거는 Community/Market하위/추가query 이동이다. 공식 공개 자격 확인 URL의302→Market 복귀와 공개 구현 소스를 대조해 좁은 상태 머신으로 구현했다. 사용자 원문 목적지까지 확정한 것은 아니다.

일반 App의 Demo 초기화·Mock UI 합성을 제거하고 실제 Shell/진단/별도 SQLite 설정으로 연결했다. Release는 Mock 서비스/관리 코드/구 화면/BAML/데이터모델·구 USD fixture 파서를 제외한다. 명시적 개발 테스트는 기존 소스를 유지한다. 소스 삭제/사용자 DB 초기화 없이 배포 경계를 분리했다. Publisher에 필수 메타데이터 검사와 LiveOnly 표기를 추가하고 LiveOnly 패키지의 Mock QA 래핑을 거부한다.

LiveOnlyTests8개(자격 왕복/goto공격/401403429/미완료/루프/알 수 없는 이동/설정 보존)·자체UI2개 추가. 234/50/14 PASS. 초기 전처리 분기 편집 오류는 빌드에서 발견해 수정 후 Debug/Release 경고·오류0을 확인했다. RTB 연결/진단 직접 검토, 실제 창/계정/거래/다른 앱 접근0. metadata 양성 검사 및 개발 DLL 음성 검사(구 fixture 파서 검출) 확인. 최종 배포 hash/커밋은 확정 기록에 추가한다.

## 2026-10-29 — 0.15.1 Wallet302 진단 후속

0.15.1 배포: 소스 d2ee9a4679e24854102f09355ff2d5f125f8c05a, `artifacts/releases/MarketCrate-0.15.1-wallet-d2ee9a4-win-x64.zip` (66,507,841바이트/412파일). SHA256 `106E89E7C801250C1C264F92DDC2BE87710A70CD3591BD09B38ADDFD9BE820F2`. Release/전체entry SHA/SourceCommit·CLR/WPF/SQLite·민감/개발파일 제외검사 PASS, 경고오류0. 실제EXE/지갑/계정은 미실행이며 해결완료 판정 아님. 0.15.0을rollback으로등록하고0.14/0.13·기존추출본/진단ZIP·DB/저널/인증/설정은보존했다.

검증: 기능227/227·비표시WPF48/48·배포계약14/14 PASS, FAIL0·경고오류0. 정리검사7 PASS/1 NOT VERIFIED(실제junction 환경제약) 유지. runtime/wallet-patch-verify.txt 및 최종core 재검사 증거. 실제Steam지갑 성공은 미검증이며 Codex 실계정/앱/거래 실행0. 최초75개 판정과 거래보호 변경없음.

두 사용자 ZIP을 직접OpenRead하여 화이트리스트 이벤트만 분석. 0.14/0.15 모두 Wallet302/OtherRedirect, 파싱전실패이며 실제Location 미기록 확인. raw 로그·ZIP·개인정보는 복사/커밋하지 않았다. 공식공개 두Market주소를 무인증GET했고 둘다200. /market 단일이동지원 누락을 수정하고 목적지유형·hop의 안전진단 및 사용자원인안내를 추가했다. 실제계정원인은 미확정, 임의리다이렉트나통화추정으로우회하지 않았다.

WalletRedirectPatchTests4개: slashless단일이동, 사용자관측8/0/302와독립세션, 안전진단/작업ID/비밀canary차단, 왕복루프2요청중단. 실제계정/브라우저/앱·타프로세스/전역입력·거래 실행0. 사용자파일/SQLite/저널/롤백본 변경0.

## 2026-10-29 — 15차 비공식 조건부 실행과 승인 정합성

### 0.15.0 배포 확정

소스 **eb9ebd7ff79585dfbcd20ba17374eb4c4e155031** origin/main push. portable **artifacts/releases/MarketCrate-0.15.0-stage15-eb9ebd7-win-x64.zip** (66,506,796바이트/412파일), SHA256 **EF520920413636DC1456C30A3C94F6ADAD03C08D0082156ED0544983520F43D5**. 전체 ZIP entry SHA와 원본/SourceCommit 일치, x64 CLR/WPF10.0.9·SQLite 네이티브·라이선스 포함, DB/로그/인증정보/개발파일 제외, Release 경고·오류0. 실제 EXE·설치·실계정은 미실행, 디지털서명 미적용.

최신/롤백은 ignored artifacts/releases.json에 기록. 롤백은0.14 ZIP(SHA E1A001291F4AE0F6BE0C5410FA51421BD95E52612472BD9C187890CBED034680), 기존0.13 ZIP·모든추출본/진단/SQLite/저널/설정/인증/캐시는 그대로 보존했다. 이번 차수의 기존산출물 삭제0(성공한 신규합성fixture 자체정리만). 마지막 생성자 권한검사를 DB열기 앞으로 옮겨 차단시 lease잔류를 막고 활성화재요청은 먼저 권한철회하도록 한 뒤 전체기능223/223 재검사(runtime/stage15-last-core.txt), 기존48WPF/14배포검사와 새Release검사 유지. 동의해제·재로그인/계정변경 활성화보존 없음.

### 15차 최종 자동 검증

기능 **223/223 PASS**, 자체비표시WPF **48/48 PASS**, 배포파일계약 **14/14 PASS**, FAIL0, Debug 경고/오류0. 안전정리 **7 PASS/0 FAIL/1 NOT VERIFIED**(실제junction 생성불가, 기존경계/잠금/변조검사유지). 신규16기능/2WPF는 실제어댑터의 합성HTTP 주입이며 사용자모드의 조건부활성화→Guard대기→수동게시대조→취소반환→재가격·감사저널도 검증했다. runtime/stage15-release-verify.txt가 최종전체회귀 증거이고 runtime/stage15-focused-final.txt는 추가user-mode saga 검사 증거다. RTB stage15-trading-controls.png·stage15-multi-game-review.png 직접검토. Codex 실계정/거래·실제창/물리입력/Popup/DPI/설치·서명 미실행. 최초75개판정47/15/0/4/9 유지. 실제 Wallet302/등록취소/KRW/스택 계약·LIVE_VERIFIED는 미확정.

origin/main445a868 clean/fetch0/0에서 시작. 사용자Wallet302 OtherRedirect·CS2명시0·목록8 관측을 반영, 실제Location/계정응답은 없어 원인미확정. 공식공개HTML/JS만GET, gzip해제후14차와동일hash확인, 오픈소스Market세션구조 참조만(복제없음).

단일same-origin정규/언어Wallet이동, 세션조건부활성화/계정binding/앱allowlist, 다중scope최종검증·게임간선택/호가공유, 승인bookdigest변경거절, 취소대상전체사전대조·기존listingID응답Unknown, 재가격intent지문/취소전·반환후 재검증 구현. 구지침의허가Gate 제품정책 예외를명시하고 위험/미검증은유지. 사용자데이터/저널초기화0·실계정/거래/실제창/타프로세스/전역입력0.

초기통합실패는 인증전합성동의setup과provider Game비교를수정해해결. RTB로신규입력폭/문구를발견·수정. 기존테스트유지, 최종집계와release/hash는 확정기록에추가.

## 2026-10-29 — 14차 읽기 계약 복구 및 안전 정리

14차 배포 구현커밋 3e5201cd5980a4bf36fb25e7dbb6f7cb5e81960f origin/main push. 새 `artifacts/releases/MarketCrate-0.14.0-stage14-3e5201c-win-x64.zip`:66,498,491바이트/412파일, SHA256 E1A001291F4AE0F6BE0C5410FA51421BD95E52612472BD9C187890CBED034680. 원본전체entry SHA·SourceCommit·CLR/WPF10.0.9/SQLite x64/라이선스/민감·개발파일 제외 PASS, Release경고오류0. 실제EXE/설치·실계정 미실행/서명미적용. 최신·롤백은 artifacts/releases.json.

검증된 새release 등록 후 dry-run11폴더/242파일(45,650바이트) 재검증·제거, skipped0. exact256byte 합성PE/고지fixture만 대상. 큰 실행폴더는 사용중/사용자내용 불확실하여 보존. 원본0.13 ZIP SHA256 2B76B0ACB3440CCBA5F2C2364AAD46BA458D10E89E329A8456400A0ED83BD7C8 재확인. 추출본/진단/SQLite/저널/설정/인증/캐시/LOCALAPPDATA 보존. ignored 증거 artifacts/cleanup-plan-f8ca226ee39e4e5aa6149f38d3fb6690.json 및 cleanup-result-755a1b5fc521421bb440139a33344cb5.json.

main/origin6c40500 clean/fetch 일치에서 시작. 0.13 사용자부분관측 반영, wallet실패 실제응답 미확보. 공식공개 market.js/marketselldialog.js·익명Market만 GET. 모든GET redirect/API오류를 인증실패로 묶던 경로와 증거없는빈inventory 허용 수정. wallet관측/strict money분리, endpoint권한 보존, 독립조회 복구, 요약/접힌상세와 고정진단사실 추가.

기능8개·자체WPF1개 추가, 전체207/46/14 PASS/FAIL0·경고오류0. 안전정리7 PASS/1 NOT VERIFIED(실제junction 생성불가). 실행폴더 사용여부를 알아보는 프로세스 조사를 하지 않았고 모두 보존. release성공 이후 검증된 dry-run만 적용한다. 실제앱/계정/Steam/브라우저조작·타프로세스·전역입력·화면접근0. 실거래 차단 유지.

## 2026-10-29 KST — 13차 기존 인증 검토·자가검증

13차 배포: 구현 `dd7e27635104c02fe53c0e282242e9d598c04a4b` origin/main push. **0.13.0 x64 self-contained Release/정적 파일검사 PASS**, 경고·오류0, CLR/WPF10.0.9·SQLite 네이티브·라이선스 포함. `artifacts/MarketCrate-0.13.0-stage13-dd7e276-win-x64.zip` **78.31MiB/412파일**, ZIP 전체 원본 hash 및 ProductVersion/SourceCommit 일치. SHA256 `2B76B0ACB3440CCBA5F2C2364AAD46BA458D10E89E329A8456400A0ED83BD7C8`. DB·로그·인증·PDB/개발파일 제외, 이전 ZIP·사용자 데이터 보존. 실제 EXE/서버 로그인·읽기·거래·설치·서명 미실행. 최소 사용자 절차는 READ-ONLY-VALIDATION의0.13.0절차를 따른다.

- 기준main/origin3684d07 clean/fetch 확인, AGENTS·제품/현재상태/작업기록·인증/읽기/진단 소스 감사. 공식OpenID/OAuth/Steamworks ticket와 공개QR 정의 재확인. 공식 장터 브라우저 내부 로그인 활용과 앱 Market 세션 권한을 구분, 지원 근거 없는 세션 전달/쿠키추출/자격증명 접근·OpenID로 Market권한 가장을 구현하지 않음.
- 기존 provider에 첫 페이지 probe를 공통 page 함수로 추가, fee wallet/structure 단계를 분리. 불변 SteamReadCheckPlan·순차 실행기(거래/저널 명령 없음)와 사용자 범위/실행 동의·9단계 결과/중단·정제 진단4204/UI 탐색을 구현. 전체 목록 캐시·선택/수량·저널 유지. 일반 조회 실패와 인증 상태 분리 유지.
- 감사에서 Wallet HTML 마커 부재를 Authentication으로 추측하던 오류 수정. QR transfer 쿠키 Secure/비어 있지 않음·미만료 검사. publisher rate/지갑·호가 통화 상충을 특정 원인으로 실패 처리. 실제 서버의 새 필드를 추정하지 않았으며 실응답 불일치를 확인한 것으로 과장하지 않는다.
- 신규 합성12개 및 비표시UI3개, 전체199/199·45/45·14/14 PASS, FAIL0·Debug 경고0/오류0. 초기 helper scope 선행순서·UI 초기 탐색 바인딩 문제는 QA-MATRIX에 실패와 수정 근거 기록. 성공/차단/실행중 own RTB 이미지 직접 검토.
- 실제 앱/브라우저 실행·계정 로그인·실계정 읽기·판매/취소/Guard·타 프로세스·입력/화면·기존 사용자 DB 접근/초기화0. SDK/Windows 설정 설치·변경0. 날짜가 자정 경계를 지나 공개 자료 확인기간은28~29KST로 기록. 버전0.13.0,75개 판정47/15/0/4/9 유지.

## 2026-10-28 KST — 12차 사용자 발견 복구·UI 결함

12차 배포: 구현 `49a3ffad9a8b37aa4085d6ace6191ddbd035430e` origin/main push. **0.12.0 Windows x64 self-contained Release PASS**, 경고·오류0, CLR/WPF10.0.9·SQLite 네이티브/고지 포함, DB·로그·세션·PDB·개발파일 제외. `artifacts/MarketCrate-0.12.0-stage12-49a3ffa-win-x64.zip` **78.30MiB/412파일**, ZIP 전체 파일 SHA256가 원본과 일치. SHA256 `EAE969B83190F2EB9A29F7299AD2B879DC7D699502D631E960A6F10308986408`. ProductVersion/SourceCommit 일치. 앱/인증/실거래/설치·서명은 실행하지 않았고 기존 배포본·사용자 데이터는 보존했다.

사용자 최소 확인:① 보관함/판매/진단 탭의 콘텐츠 버튼·체크·입력·드롭다운 위를 이동해 Header 깜빡임이 없는지, Tab/Shift+Tab 점선과 선택 밑줄이 구분되는지 확인.② 직접 QR 인증을 선택한 경우 인증 완료 카드와 개별 조회 상태를 확인.③ 보관함 파싱/404 실패 시 인증 완료가 유지되고 독립 목록/fee 버튼이 동작하는지 확인(401이면 재로그인,403/429/CAPTCHA면 차단이 정상).④ 게임 전환·선택/수량·스크롤·가격 검토를 확인하되 실등록·취소·재가격은 실행하지 않는다. 실패가 남으면 고급 설정의 작업 ID→진단 검토 ZIP으로 원인을 좁히고 QR/원문 응답은 공유하지 않는다.

12차 최종 회귀: **기능187/187 PASS · 비표시 WPF42/42 PASS · 배포 파일 계약14/14 PASS · FAIL0 · Debug 경고0/오류0** (`scripts/Verify.ps1`, ignored runtime/stage12-verified.txt). 기존177/39/14 검사와 거래/Unknown/SQLite 회귀 유지. 사용자 제보의 실제 Steam 보관함 실패 응답은 미확보이므로 해당 실서버 파서 계약이 해결됐다고 보장하지 않는다. 수정한 것은 실패의 범위·인증 소실·UI 동작이며 실계정 파서/응답 형식 검증은 별도다. 실제 앱/입력/Popup/물리 DPI/Steam 인증·거래·설치 미실행.

- main/origin4210269 clean/fetch 확인, AGENTS/현재 상태/QA/설계·실제 호출 경로 감사. 수정 전 read4FAIL 및 Tab header1FAIL 재현 증거를 ignored runtime/stage12-before.txt,stage12-ui-before.txt에 남김. 실제 사용자 인증 응답을 확보·재생하지 않았다.
- read parser→공용 Stop→Authentication.Connected→VM.Error.LoginState 결합 제거. GET transport 수동 재시도와 POST Unknown latch 구분,401/403/429/CAPTCHA 안전 중단 유지. fee 실패 후 listings 독립 진행·검토 freshness gate·새 ReadState4203·원인/operation 추적 구현.
- TabItem.IsMouseOver를 Header B의 상태로 교체. 사용자 물리 깜빡임은 미실행이나 기존 구조의 잘못된 반응을 DP로 재현. Grid 기반 Steam 5페이지/3열 보관함/고급 분리/인증 완료 카드/가격·수수료 예상/한국어 결과/스크롤·선택 보존 구현. 렌더에서 QR 완료 후 대기 오안내, 긴 가격 source 노출, 좁은 칸 이미지 안내, 짧은 창의 계획 패널 clipping을 찾아 수정.
- S6 진단 ZIP 검사에서 기존 async 경쟁 발견: 폴더 생성과 busy는 저장 완료 증거가 아니므로 실제 완료 Status+ZIP을 대기. 무조건 성공/허용 기준 완화 없이 기존 명령·검토·비밀정보 단언을 유지. UI 고급 이동과 템플릿 구조 변경은 같은 기능을 새 진입점에서 검증하도록 기존 테스트만 조정.
- 공개 UX/IsMouseOver 문서만 조회, 실제 앱·인증·판매·취소·Guard·타 프로세스·입력·캡처0. SQLite/실저널/사용자 데이터 초기화0. 버전0.12.0, 기존75개 판정변경0. 실계정/물리 GUI는 별도 사용자 검증 필요.

## 2026-10-28 KST — 11차 실제 사용자 QR·상태 강조 결함

- main/origin62663c5 clean에서 시작. AGENTS/현재 상태/QA/설계·관련 소스 확인. 공개 metadata/proto/공식WebAPI/공개 구현 대조, 인증 네트워크 실행0.
- 수정 전 독립 decoder/숫자JSON 계약2개 실패 증거 저장(ignored runtime/stage11-before-fix.txt). 요청 최상위 device_friendly_name/platform_type 보완, WebBrowser2/중첩구분/명시JSON/uint64·interval 유효성 검증. HTTP200 API 거절 성공 오표시 수정,2005단계 진단/OperationId/정제 유지.
- Action VSM/Trigger 중복 구조 검사 실패를 수정. 물리 ghost-hover 및 shared brush 오염은 재현되지 않았다. FocusCue는 자체 routed subtree만 관찰하며 전역 입력/포커스 변경 없음. 선택·체크·Hover·키보드점선 분리, Disabled 우선순위/Combo 이중 opacity 수정. 단순 테스트 수 증가가 아닌 두 사용자 결함의 계약·시각 원인 정리에 집중.
- detached WPF 입력장치 부재 때문에 DP 주입과 CLR 상태 getter가 달랐다. 명시적 VSM 전이와 DP Trigger 검사를 분리하고 실제 입력으로 주장하지 않음. 자체 PNG 시각 검토에서 Normal/Hover/Pressed/Focus/Disabled와 Checked/Selected의 구분 확인.
- 앱/브라우저/Popup/실계정/거래/Guard/타 프로세스·화면·입력 접근0. SQLite/저널/사용자 데이터 초기화0. 최초75개 판정변경0. 실서버 장애 재현·서버 QR 성공은 미검증.

## 2026-10-28 KST — 10차 실제 사용자 경로 보완

- 시작 main/origin `a0e7c95` clean/fetch. 기존 AGENTS/정의/상태/구조·QA·저널·진단 확인. 사용자보고9차 기본GUI7PASS를별도기록, 상세24개/10차GUI/실계정PASS로변환안함. 타프로세스·OS입력·화면/실앱·계정작업없음.
- 즉시거래가능가 Core PriceDisplay 리소스/정확한비체결보장설명 도입. Main전략은문자열비교대신PriceBasis, 구한국어저장키읽기/새enum키저장. 실제모드인덱스·저널·계산함수변경없음.
- QR Challenge회전이UI에누락되는결함수정. auth중빈갱신유지/만료재시작/성공시QR참조해제, connected중Begin차단. 인증401시이전관측row보존/새검토차단/logout정리검증.
- 실제 판매목록이inventory+fees와묶여불가능한경로를독립명령으로연결. 수수료없는취소검토는허용하되쓰기flag·계약제약유지. 현재모드별시세/잔량·등록가격차이·음수차이는가격타입과분리해표시. 계약관측UI와정제ReadContract이벤트추가, 실제READ_ONLY_READY자동부여없음.
- 정상App진입Steam탭(자동인증없음), 공식장터수동대체버튼, 중복상태Toast숨김. 테스트는URL delegate만사용. userStage9 GUI7스코프와LIVE·RELEASE미검증을화면에표시.
- 공개Steam auth proto/유지보수자소스·공식market/약관재확인, 타코드도입없음. Supportconfirmation페이지본문은도구상재확인불가로현재검증근거로안씀.
- 최종 기능172/172·detached WPF33/33·파일계약14/14 PASS, FAIL0/Debug 경고0·오류0. 구현 `e5db266` origin/main push 완료. 해당 소스 self-contained x64 Release PASS(경고0/오류0), CLR/WPF10.0.9·SQLite/고지/데이터·PDB 제외/Core commit 확인. `artifacts/MarketCrate-stage10-e5db266-win-x64.zip` 78.3MiB/412개 원본·ZIP hash 일치, 앱 미실행. SHA256 E14B0BA88D314276F485AEB2D8E97A64604B02F7EA7D5A77207C4DC122EC765F. 신규UI검사Single가정실패1은실제두전략컨트롤전체단언으로수정. 최초단독buildcache NU1101은기존Verify환경사용으로해소. 초기실패도runtime기록보존. 실제Steam접근/거래/Guard·CodexGUI조작없음.

## 2026-10-28 KST — 9차 실행 조건 판단 / self-contained / 진단 ZIP 수정

- 시작 main/origin `9cc73a4` clean/fetch. AGENTS/상태/QA/격리/정의/로그 검토. 명시 제공 guest 없음. registry Core/build26200.9711/X64·Sandbox exe 없음만 조회, CIM capability는 권한 제한으로 미확인. 다른 프로세스/창·입력·화면/VM 목록 탐색 없음. 시스템 변경/재부팅/가상화 설치·실행 없음. 공식Home 미지원/별도 Windows11 guest 필요를 기록.
- 기존 FDD/하네스 유지, self-contained win-x64/CLR·WPF10.0.9/SQLite 패키징·정적 검사를 추가했다. 첫 offline-source NU1101은 self 명령의 공식 NuGet source로 해소(전역 source/SDK 설치 없음). runtime pack은 ignored project cache. runtime lock은 obj 별도 파일. 새 출력만 허용하고 기존 산출물/데이터 삭제 없음.
- 실제 runtime 고지 누락 FAIL을 검출해 원본 LICENSE/THIRD-PARTY3개를 복사. x64 apphost/CLR·JIT·host/WPF·SQLite·runtimeconfig/RID·앱 자산·고지·로컬자료/PDB 제외 검증. framework-dependent는 별도 유지. 기존 guest launcher의 시스템 dotnet 강제/재시작 거부/RO scenario 기록 문제를 코드에서 수정, 실제guest경로는 미실행.
- 신규 mtime 역전 재현: 기존165PASS+ZIP1FAIL → JsonDiagnosticLog.ReadSafe의 전체/제한 반환을 Utc 순서로 수정 →166/166PASS. 검토 동의/불변snapshot·정제·2-entry/localZIP/operation·원인·DB미포함 유지. Unknown/저널·예약·Steam 코드는 변경 안 함.
- 최종 자동: 기능166/166, detached WPF31/31, 파일계약14/14 PASS, FAIL0/경고0/오류0. .env 파일 검사 최초FAIL은 fixture 동적 변수 scope를 분리해 해결. guest launcher는 AST만 확인. 구현 `d19dcc9` origin/main push 완료. 해당 소스의 SCD/FDD Release 모두 PASS(경고0/오류0), x64 apphost/CLR·WPF·SQLite(모드별)·runtimeconfig·고지·DB/로그 제외 정적 확인. SCD CLR/WPF10.0.9·PDB0, guest bundle414개 해시/142.02MiB; FDD40개 해시/33.51MiB. 두 manifest SourceCommit/PackagingCommit/Core metadata 일치, GUI0/0/24. 앱/guest/설치·서명 미실행. 새 obj lock 검증 중 NuGet 감사 자료의 네트워크 제한 NU1900을 확인했으며 검사를 비활성화하지 않고 공식 접근으로 재검증 PASS했다. FDD publish는 자체 기본 restore를 수행해 SCD용 RID/실패 assets가 남아도 이전 --no-restore 상태에 의존하지 않게 했다.
- 실제 GUI24개NV/실앱·설치·서명·DPI·계정·거래·Guard 미실행. 75개47/15/0/4/9 그대로. 다음은 전용guest 제공 후 기존24개실행이며 환경 구성을 반복하지 않는다.

## 2026-10-28 KST — 8차 GUI·실연동 계약 검증 준비

- 기준 HEAD/origin `4a18077` clean/fetch 확인. AGENTS/정의/상태/75개·QA/진단/Steam/격리 GUI 문서 검토. 전용 guest 제공 정보 없음. 호스트 대체 GUI/VM 탐색/Windows 기능 변경/다른 프로세스·입력·화면 접근 안 함.
- Steam AnyReview 알림 누락을 실제 binding 상태 테스트로 확인·수정. 모달 focus boundary 및 active own-window guard, ComboBox focus/open trigger, disabled NumericStepper peer 표준 예외 보완. UI 테스트의 standalone style 미설정으로 FAIL1을 확인한 뒤 fixture를 수정해 관련31개 PASS; 물리 GUI 문제로 가장하지 않음.
- QR 갱신 빈 선택필드 처리와 취소 Schema 오분류, HTTP 사용자 취소→Transport halt 및 VM 로그인 상태 소실을 수정. 승인 flag 0/1 외 정수는 계약 오류로 제한. AuthState endpoint/원인/operation 연결과 secret sentinel 누출0 단언 추가. Unknown·예약·반환 인과 기준 완화 없음.
- 게스트 전용24 case/manifest/해시/런타임 확인 수동 launcher와 host packaging-only script, offline App profile/분리 데이터·Steam factory 차단 구현. marker/env는 운영자 선언이며 VM 보증 아님. guest launcher/실앱은 미실행.
- 공식 판매 JS 재다운로드 hash는5차와 동일; 공개 auth proto/maintainer source·MIT 확인, 코드/의존성 도입 없음. 실제 인증 응답·계정/API/이미지·거래/Guard 미실행.
- 전체 기능/합성 HTTP/SQLite/진단 165/165 PASS, 비표시 WPF 31/31 PASS, FAIL0, Debug 경고0/오류0. 구현 `f672120` origin/main push 완료. 해당 커밋 Release publish PASS(경고0/오류0), apphost/SQLite x64 PE·Desktop Runtime10 의존성·데이터 제외·라이선스 고지·Core Git metadata 확인. 격리 QA package39개 SHA256 전부 일치, 24개 결과는 NOT VERIFIED. 실제 앱/guest/설치·서명 미실행. 최종 문서 마감 커밋은 완료 보고 참조. 최초75개 판정47/15/0/4/9 유지, 실제 입력·Popup·DPI·스크린리더·장기누수·설치·서명은 미검증.

# MarketCrate — 누적 작업 로그

7차 추가 반환 제한: 원래 단일 수량이었어도 취소 후 returned.Stackable/수량 증가·기존 가용 수량이 있으면 proof로 확정하지 않는다. 동일 ID라도 현재 정확히1개/기존0개인 제한 경로만 합성 대조하며 나머지는 Unknown/예약 유지. 실제 nonstack/스택·ID 의미와 인과 증거는 여전히 미검증이다.

### 7차 자동 검증 마감

- 전체 기능/합성 HTTP/SQLite/진단 **158/158 PASS, FAIL0** (기존139+신규19), 비표시 WPF **28/28 PASS, FAIL0** (기존23+신규5), Debug 빌드 경고0/오류0. 마지막 기능 보강 후 전체 회귀 실행. 예상치 못한 binding 오류0/Window·PresentationSource0.
- 실제 PNG 디코딩·재활용60회·전송 중복/취소·재시도/실패 상태와 보관함/판매/검토/추정/진단 이동 이미지를 직접 검사했다. NumericStepper 36px/텍스트 줄바꿈/출처/기준별 부족 수량 안내를 수정·재확인했다.
- 단기 합성 예: 이미지100회 약2ms wall/31ms 자체 process CPU/managed allocation 약1.36MB, LRU4개/280B; 썸네일60회 약1835ms wall/1406ms 자체 CPU/약0.89MB managed allocation, cache210B. 자체 process만 읽었고 CPU 계수 해상도/다른 자체 스레드가 포함된다. 실이미지/네이티브 메모리/장시간 누수/FPS/다른 앱 영향 측정으로 확대하지 않는다.
- 실제 Steam 이미지/인증/보관함/가격/fee/판매/취소/Guard 및 실제 창/물리 입력/모니터 DPI/installer/VM 실행은 NOT VERIFIED. 소스/배포 검사 수정 커밋 `6828294` Release publish PASS(경고0/오류0): PE·로컬 데이터 제외·라이선스 고지 확인, Core DLL의 해당 Git commit metadata 정적 확인. 실행파일·설치 미실행. 구현 `e40492d`, 배포 검사 수정 `6828294`는 origin/main push 완료.

## 2026-10-28 KST — 7차 미완성 항목 구현

- 시작 main/origin `158a606`, clean/fetch 확인. AGENTS·75개 추적표·QA/진단/설계 조사 후 같은 ID와 6차 판정을 보존했다. B04/C02/D02 로컬 COMPLETE, B05 PARTIAL로 개선(47/15/0/4/9), 실제 응답/계정/DPI 판정은 승격하지 않음.
- 공식 공개 HTML/JS에서 icon fields/CDN 경로/기간 중앙값 의미, 공식 GetOwnedGames 문서와 Sandbox 구성 자료를 확인했다. 새 SDK/패키지/타 제품 코드 도입 없음, source만 읽고 실행하지 않음.
- 메타데이터 join/side maps, opt-in 무인증 단일 CDN/제한 캐시/중복 억제/취소/재활용·worker decode/freeze, inventory/listings/review/대표 이미지와 오류 UI 구현. 초기 합성 PNG fixture 오류를 BCL CRC/ZLib로 직접 생성한 유효 PNG로 수정, 실제 decoder까지 검증했다.
- account별 manual/observed context catalog와 v3 저장/복원/오류 상태를 추가했다. 로컬 입력 실패가 로그인 상태를 만료시키던 문제를 수정. 로그아웃 후 늦은 load의 새 catalog 오염도 차단. 통계 모드/기간 변경 후 늦은 응답 폐기.
- exact average는 계속 미제공. 대체 weighted median estimate의 기간/표본/한계/불변 evidence/digest를 구현하고 같은 합계의 바뀐 표본도 거부한다. 최종 preflight의 동일 상품 반복 조회를 제거하고 마지막 freshness 검사, publisher fee 누락 차단 보완. 실제 stack return/Unknown 인과 증거 기준은 그대로다.
- 정상/실패/취소/중복/403·429/MIME/픽셀·바이트/캐시/변조 이력/설정·저널 재시작/진단 연결에 합성 테스트를 추가했다. 자체 PNG를 육안 검토해 과도하게 늘어난 스테퍼를 수정. 앱/최상위 창/실제 입력/다른 프로세스·화면/실계정 접근은 없음. 최종 결과/커밋은 마감 기록 참조.

## 2026-10-28 KST — 6차 요구사항 감사 및 실사용 진단

- 최종 마감: 구현 `5b44e0d`, 진단 보완 `fab286a` 각각 origin/main push 완료. 보완 후 전체 **139/139 + WPF 23/23 PASS**, FAIL0/경고0/오류0. `fab286a` Release PE/데이터 제외/MIT 고지 PASS, Core DLL의 소스 commit 포함 정적 확인. 최신 진단/최종 검토 PNG 육안 재확인, 실제 Window/앱/계정/타 프로세스 접근 없음. 남은 항목은 추적표 PARTIAL/MISSING/BLOCKED/UNVERIFIED로 유지한다. 최종 문서 커밋은 완료 보고 참조.

- 구현/감사 커밋 `5b44e0d` origin/main push 및 Release PE/데이터 제외/라이선스 검사 PASS. 마감 점검에서 writer 실패의 원인 분류/OperationId가 health에 부족하고 변조 null stack 프레임이 export를 중단할 수 있음을 발견해 후속 보완했다. 원문 없이 typed 실패/OS 숫자 코드/작업 ID를 화면·manifest에 제공하고 null 프레임은 제외한다.

- origin/main `33cd152` fetch/clean 기준, AGENTS/정의/설계/기록/QA/실연동 자료와 관련 코드 경로를 대조했다. 75개 ID 추적표를 작성했고 COMPLETE44/PARTIAL16/MISSING1/BLOCKED5/UNVERIFIED9(로컬/합성 범위)을 분리했다. 실계정/실제 응답 수집은 전부 미실행이다.
- 실제 결함: 실험 화면 검색·필터·정렬/선택 해제·게임별 판매 필터 누락, 갱신/게임 변경 시 선택 소실, MainWindow 닫기에서 Steam.Busy 누락, stale 재가격의 검증 순서가 취소 이후였음. 관련 코드/UI/수량 상한 복원을 수정했다. 자동 발견/이미지/실평균가는 별도 미완성으로 명시했다.
- 고정 코드/건수 진단을 재사용·연결하면서 중앙 구조화 JSONL 시스템을 추가했다. operation/correlation/batch/item, UTC/버전/commit, 고정 EventId/RequirementId, HTTP·저장소 오류/상태·가격/재시도, 정제 stack/inner 위치와 사용자 진단/로컬 ZIP UI. 읽는 중 writer 파일 공유 오류를 첫 주입 검사에서 찾아 ReadWrite 공유로 수정했다.
- 보안 설계: 자유 텍스트/URL/원문/인증/계정·자산 식별자는 생성 지점부터 제외, sink/export 재정제. 예외 message/path/전체 객체 금지. bounded async writer와 누락/실패 health, 보존 한도, 검토 후 원자 ZIP 저장. 로그 실패로 재판매/취소하지 않는 것을 검증했다. 기존 거래 저널은 별도 유지한다.
- 새 재가격 승인은 원 intent를 변경하지 않고 확정 취소 proof를 새 intent에 연결/원자 Superseded 처리. 이전 앱이 새 상태를 오해하지 않도록 Steam schema v3 마이그레이션, 구 내용/요청/예약 보존. 미확정 취소/이미 전송한 replacement는 revision 불가.
- QA는 합성 handler, 격리 SQLite, 자체 STA/RenderTargetBitmap만 사용. 디스크 full은 writer 예외와 in-memory SQLite page 상한으로 주입(실제 사용자 디스크를 채우지 않음). 비밀 marker/위조 파일/ZIP 재정제, 작업 ID/commit, 부분 실패/저널 실패/호출 횟수/재시작을 assert했다. 최종 전체 Debug **139/139** 및 비표시 WPF **23/23 PASS**, FAIL0, 경고0/오류0. Steam v3 변경 후 회귀도 통과. 최종 App startup의 app-local default binding listener 제거는 정적 보안 점검/Release 컴파일로 확인하고 실제 startup 실행은 하지 않는다. Release/커밋 결과는 마감 기록에 추가한다.
- 공개 약관 Automation/Enforcement 본문을 재확인했다. support confirmations 링크는 이번 도구 응답이 이미지 한 줄이라 본문 재확인 실패로 기록했다. 새로운 API live 호환성/정책 허용을 추정하지 않았다. 다른 앱/프로세스/입력/화면/실계정 접근 없음, 새 SDK/패키지/경쟁 코드 도입 없음.

## 2026-10-28 — 5차 사용자 연결·KRW·스택·대조/재가격

- 시작 HEAD/origin/main `73ad542`, 작업 트리 clean. AGENTS·현황·정의·QA·아키텍처·판매 계약·실연동 조사를 읽고 fetch했다. 실제 계정/거래·외부 앱·입력·화면 접근은 하지 않고 현재 저장소를 이어 수정했다.
- 공식 최신 판매 번들에서 새로운 wallet floor/step + ajaxfeestructure rate/cap 및 amount 계약을 확인했다. 공개 소스만 읽었으며 authenticated fee 응답/실계정 계산은 확인하지 않았다. KRW/USD 정수 Money/wire, 정확한 fee와 역산 불가 금액 차단, 서버 응답 모순 중단을 독립 구현했다.
- 새 Steam MVVM/UserControl에 로컬 QR·직접 로그인 확인·세션 종료/취소·실제 provider 갱신/게임/선택/시세·가격 검토·판매 관리·결과/증거/감사 이력·재가격 재개를 연결했다. 기본 policy Blocked 및 stack capability를 별도화, 사용자 동의로 Valve 허가를 가정하지 않는다. Mock과 실제 source/상태/합계를 분리했다.
- MIT QR v2.0.0 소스 6파일을 고정 커밋으로 도입, 원 고지·LICENSE 유지 및 nullable 지시문만 추가. 순수 계산/무네트워크를 확인하고 output/publish에 LICENSE/고지를 함께 넣도록 했다. 패키지/SDK 설치 없음.
- 공유 호가 key에서 context를 제거해 같은 장터 잔량의 중복 사용을 막았다. 보관함과 예약 반영 가용량, Unknown 보존, 동일 자산의 반환 proof로 해당 예약만 해제, 확정 배치 archive 후 새 배치를 처리한다. v1→v2 이관과 command/reprice audit는 별도 실저널에 보존한다.
- 재가격은 승인 target 불변→취소→목록 부재+동일 자산 수량 반환→새 preflight→후속 key 먼저 저장→등록이다. 스택/외부 등록/ID 교체/불명/5분 stale/중복은 차단한다. listingid 없는 후보는 시각/기존 ID/수량·가격까지 보여 주되 인과관계 없이 확정하지 않는다.
- 합성 HTTP 서버(소켓/listener 없음)와 동일 UI/어댑터로 로그인→KRW 스택 3/4/3→승인 대기, 서버 fee 충돌·currency mismatch, 비스택 취소/반환/재가격/재시작 및 스택 반환 추정 차단, logout 늦은 응답, 동의 철회, QR 만료/거부/CAPTCHA/네트워크, archive·부분 예약 해제·legacy 이관을 검사했다.
- UI 검사에서 템플릿 생성 전 컨트롤 검색 실패를 이름 기반 접근으로 수정했다. 렌더에서 standalone 글자색, QR 빈 상태, 실험 화면의 Mock 문구/가로 폭 혼동을 발견해 실제 소스를 수정했다. 실제 창/사용자 입력 검증으로 확대하지 않는다.
- 새 실험 목록에서도 반복 DB 조회/전체 선택 알림을 발견해 일괄 예약 조회·RangeObservableCollection·알림 batching을 적용했다. adapter-backed 10,000개 합성 목록으로 검사하며 장시간 성능/실서버 부하 검증은 하지 않는다.
- 최종 안전 검토: 스택 수량 증가가 취소 반환의 인과 증명이 되지 않음을 발견했다. 원래 snapshot과 직접 연결된 비스택 단일 자산만 자동 반환 proof를 허용하고 스택/외부 등록은 Unknown·예약 유지로 제한했다. 긍정적인 스택 반환 추정 테스트를 안전한 차단/재시작 보존 회귀로 대체했으며 이 제한을 완료 보고에 공개한다.
- 최종 회귀: `scripts/Verify.ps1`, 5프로젝트 Debug 경고0/오류0, 도메인/합성 HTTP **112/112**, detached STA WPF **20/20 PASS**, FAIL0. 마지막 변경 소스 이후 Release publish·PE/데이터 제외·QR MIT LICENSE/고지 포함 검사 PASS. 산출물 실행/설치/서명/공개 배포는 안 했다.
- QR/검토/보관함/통합 PNG를 직접 검토했고 생성물은 ignored runtime/artifacts에만 둔다. 금지 API·민감값 패턴·diff 점검 후 소스/합성 테스트/문서/허가된 QR 소스만 commit/push한다. 미지원·제재 위험은 현황에 누적하며 실판매 완성을 선언하지 않는다.
- 최종 검토: 금지 API 검색의 Process.Start는 이전 승인된 owned headless 테스트 자식뿐이며 새 타 앱 접근 없음. QR vendor 6파일은 내려받은 고정 원본과 nullable 지시문을 제외하고 문자 단위 일치 확인. upstream 주석/EOF 공백은 원본 보존으로 확인했고 자체 소스·테스트·문서 diff check는 PASS다. 새 소스 제한 이후 전체112/112·WPF20/20·Release/라이선스 검사도 다시 PASS했다.
- 구현·조사·검증 커밋 `f7bd8aa`을 origin/main에 정상 push했다 (`73ad542..f7bd8aa`). 실제 로그인/거래/Guard·타 앱/전역 입력/화면 접근 없음. 다음 우선순위는 별도 승인된 실제 QR/읽기 검증, 정책 근거, 통화/게임별 검증 및 스택/외부 등록의 취소 인과 증거 어댑터다. 실제 일괄 등록 지원 완료/릴리즈 준비 완료를 선언하지 않는다.

## 2026-10-28 — 4차 조사·실험적 연동 구현

- 시작 HEAD/origin/main `9455dc9`, 작업 트리 clean. AGENTS/현재 상태/정의/QA/아키텍처/실행 계약을 확인하고 fetch했다. 사용자 승인으로 비공식 **소스 구현** 범위를 확장했으며 계정 로그인/거래 시험은 승인 범위 밖으로 유지했다.
- GitHub 최신 고정 커밋 세 곳의 관련 소스/라이선스와 공식 Market/약관/OpenID/publisher 문서를 읽었다. 공개 orderbook 1회 무인증 응답의 중첩 JSON/eCurrency/가격·잔량 쌍을 직접 확인했다. 연구 소스/응답은 runtime에만 보관하고 코드 복사/실행/의존성 설치 없음. 최신 지원 주장과 직접 관측 한계를 분리했다.
- Infrastructure/Steam에 QR protocol→새 자체 메모리 쿠키 세션·동의·host 제한 HTTP·inventory/orderbook/median history/listings·sell/cancel·별도 SQLite intent/reconcile를 독립 구현. 기존 provider/executor 도메인 계약과 단일 승인 coordinator를 재사용했다. live 단가 USD exact fee만 허용하며 기존 KRW Mock은 유지, unsupported 통화 회귀 케이스는 EUR로 바꾸고 USD 통화/수수료 경로를 추가 검사했다.
- request key는 로컬 중복 방지용이며 서버 idempotency 지원을 가정하지 않는다. Unknown/서버ID 없음/취소 목록 부재는 자동 성공·재등록으로 바꾸지 않는다. 원화/스택/확정 취소 후 실제 재가격/live 수동 retry/로그인 WPF는 미완성으로 명시했다. 일반 앱에는 live 구성 주입 없음.
- 합성 HTTP로 전송 코드의 form/인증 만료/접근 제한/호가·통화·수수료/승인 대기/서버ID 대조/타임아웃·재시작/취소 불명/중복 키/순차성/자격 불일치/미래 스키마/비밀값 비저장을 검사했다. 초기 검사의 timeout 후 중단된 세션 재실행은 기존 결과 반환이 아닌 차단이 맞아 테스트 기대를 수정했다. 실제 안전 검사를 완화하지 않았다.
- 비표시 안전 안내 렌더에서 좁은 viewport 오른쪽 여백을 보완했고 카드 경계 검사를 추가했다. Steam 저널은 불변 배치+변경 result 행 transaction으로 저장하도록 구현했으며 128행/두 번째 행 실패 rollback 재열기 검사를 추가했다.
- 최종 전체 회귀: 빌드 경고0/오류0, 도메인/저장/MVVM/합성 Steam HTTP **87/87 PASS**, detached STA WPF **17/17 PASS**, FAIL0. 기존58/16 회귀 유지. 새 안전 화면 자체 PNG 시각 검토·카드 경계 검사 PASS. 변경 소스 이후 Release publish·PE/로컬 데이터 제외 검사 PASS. 앱 실행/설치/공개 배포는 안 했다.
- 전체 diff/비밀값 패턴/금지 API 검색에서 문제 없음. 기존 Process.Start는 이전 승인된 프로젝트 headless 테스트 자식뿐이다. raw source/응답/이미지/DB/로그/bin/obj/산출물은 Git 제외. 실제 Steam 인증/거래/Guard·다른 앱/화면/입력/프로세스는 건드리지 않았다. 후속은 QR/실계정 UI와 원화/스택 계약·확정 취소 및 불명 대조 근거 보강이다.
- 구현/조사/검증 커밋 `61fbd58`을 origin/main에 정상 push했다 (`9455dc9..61fbd58`). 현재 일반 앱의 실거래 차단은 유지하며 LIVE_READY를 선언하지 않는다. 최종 기록 동기화 커밋은 최종 보고에 기재한다.

## 2026-10-28 — 3차 구현·검증 마일스톤
- 조사 커밋 `db8a55c`를 origin/main에 정상 push한 뒤 기존 WPF/MVVM/Core/SQLite 구조를 이어 구현했다. 새로운 라이브 연동/의존성 설치/원격 변경 없음.
- 검토: 개별 등록 단위 가격 편집, Steam/게임 수수료·실수령/양쪽 합계 갱신, 부족 잔량 빈 단가 행·예상 미체결 안내, 수동 가격 전략/닫기/갱신 초기화, 조회 시각/신선도 및 최종 재검증.
- 관리: 개별/전체 선택, 취소 후 재등록 가격 변경, 확정 실패 배치·관리 작업만 명시적 재시도. 시도 번호별 요청 키·기존 성공 보존·stale/double 승인 차단. 재가격 취소 확인 이후 후속 미전송만 재개하고 Unknown은 재전송하지 않는다. 후속 교체의 취소 전까지 원래 예약을 유지한다.
- 안전 결함 수정: 구형 관리 Unknown(Action=None)을 과거 등록 증거가 덮어쓰지 않게 Registration/Cancel/Reprice 출처를 구분. 모달/배경 작업 동시 조작과 닫힌 모달 입력 오류 잔류를 보완. Mock 재시도 결과 대조도 현재 시도 키를 사용한다.
- SQLite: v2 보존 이관·quick_check·설정 보존·고정 진단. 대량 배치/관리에서 전체 JSON 반복 쓰기를 발견해 불변 헤더+결과/listing 변경 행 transaction 저장으로 보완. 캐시는 단일 writer의 immutable 레코드이며 초기화 시 무효화. 전체 rollback/재열기 검증 유지.
- UI: 신규 검토/재가격/재시도/로딩·조회 실패/대량 재활용 자체 PNG 생성·직접 시각 검토. 로딩 중단 버튼 clipping을 발견해 패널 여유를 늘리고 위치 검사를 추가했다. 실제 창/포커스·전역 입력/다른 화면은 사용하지 않았다.
- 검증: 마지막 전체 회귀 `scripts/Verify.ps1`, 빌드 경고0/오류0, 도메인/SQLite/MVVM **58/58**, WPF **16/16 PASS**. 이후 UI 오류 수집 범위를 새 화면까지 확장한 관련 UI 검사도 16/16 PASS. FAIL 0. 재실행은 새로운 기능/저장 구조/발견 결함/추가 검사 확인에 한정했다.
- 장애 검증: 프로젝트 테스트 DLL만 CreateNoWindow headless 자식으로 순차 실행하고 자식이 자기 Exit. intent 저장 후 재시작 Unknown/재전송0 및 미커밋 SQLite transaction rollback 확인. 타 앱 검색/종료/메모리/화면/입력 접근 없음. 실제 전원 차단/GUI 강제종료 시험은 아니다.
- 부하/배포: 10,000개 뷰 컨테이너 시작9/끝7 및 반복 스크롤 선택 보존, 128행 SQLite 배치와 저장 payload <1MB 검사. 장시간/최대 규모 실행 인증은 아니다. `scripts/Publish.ps1` 최신 Release framework-dependent exe 생성/PE/DB·로그·키 제외 PASS, 산출물 실행·설치 안 함. 데이터 경로는 기존 LocalAppData 유지.
- 진단: timestamp/enum code/count만 DB 최대500개, 시작 DB 오류는 앱 전용 bounded 고정 코드 파일. 임의 예외 메시지·계정/아이템·비밀정보를 기록하지 않는 검사 추가. 이미지/DB/로그/빌드 결과는 Git 제외.
- 미검증/차단: 실제 입력·열린 팝업·모니터 DPI·스크린리더·설치/서명·장시간/전원 차단·실제 Steam 로그인/거래/Guard. 관리 Unknown의 외부 증거 없는 재전송은 차단. 필수 실제 일괄 등록 목표는 IntegrationBlocked 유지.
- 후속: 허용된 실연동 경로/권한 확보 전 실제 어댑터 금지, 다중 배치 탐색·외부 대조 도구·대량 UI 선택 확장·배포 감사. 최종 diff/민감정보 패턴 검사 PASS. 구현 커밋 `1e6c280`을 origin/main에 정상 push했다. 조사 `db8a55c` 및 구현 커밋 모두 원격 반영 완료.

## 2026-10-28 — 3차 조사 마일스톤
- 시작 HEAD `9ce53df`, 작업 트리 clean. fetch한 origin/main과 일치, AGENTS/현황/QA/아키텍처/판매 계약 확인.
- 공식 Steam OpenID·Web API 인증·Inventory/Economy/Market API 및 약관을 조회. publisher/소유 앱/secure server 제한과 개인용 장터 권한을 구분했다. 공식 Support 직접 본문 접근 실패 후 공식 검색 색인의 확인 절차 본문을 확인했다.
- 오픈소스·서비스 공개 문서만 조사, 설치/실행/로그인/API/거래/모바일 승인 없음. 결과와 한계·라이선스·후속 해제 조건을 STEAM-INTEGRATION-FEASIBILITY에 기록. 실제 쓰기는 IntegrationBlocked 유지.

## 2026-10-28 — 최초 개발 지침 패키지 작성
- 사용자 요구사항을 기반으로 제품 목표, UI/UX, 인증·안전, 일괄 판매 실행 계약, QA, 레퍼런스를 문서화.
- **실제 앱 코드, 실행, Steam 로그인, 실거래, 테스트, Git 푸시는 수행되지 않음.**
- 향후 Codex가 매 작업 단위에 `날짜/목표/수정 파일/설계 결정/실제 확인한 검증/미실행 검증/위험/commit hash/push 결과` 추가.
- 기능 구현 진척은 `CURRENT-STATUS.md`를 단일 현재 상태 문서로 사용.

## 2026-10-28 — 최초 Mock 구현
- 시작: HEAD `8fcad5c`, `main`→`origin/main`, origin `RuinaR/MarketCrate`, 작업 트리 clean. AGENTS 및 동봉 설계/QA/계약 확인. 기존 문서와 요구사항 보존.
- 웹 조사: 지정 6개 레퍼런스, 약관·API 문서·Support URL 확인. Support 본문 추출 실패와 raw LICENSE Cache miss는 REFERENCES에 명시. 외부 코드/자산/인증 흐름 미도입.
- 구현: 4개 프로젝트 솔루션, 다크 토큰/커스텀 템플릿, Mock 보관함·수량·가격 전략·단가 편집 검토·단일 최종 배치 명령·개별 결과·대기/불명 표시, 정수 가격/수수료 엔진, Blocked executor, 디스크 저널/복구.
- 설계: 실연동 경로 미확인으로 실제 읽기·쓰기 미구현/차단. SQLite 대신 합성 Demo v1 JSON을 사용하고 live 저장소로 재사용 금지. journal intent를 요청 전에 저장, Unknown 시 후속 중단·재전송 금지. 단일 활성 배치·독점 파일 lease 유지.
- 수정한 결함: 잘못된 가격 입력이 이전 바인딩 값으로 제출되는 경로를 Validation.Error→명령 차단으로 수정. 작업 취소와 앱 수명 토큰 분리. 지연 Progress 큐로 완료 상태가 덮이지 않도록 인라인 진행 통지 사용.
- 검증: `scripts/Verify.ps1`, SDK 10.0.301, 빌드 경고 0/오류 0, 계산/배치/디스크/MVVM **25/25 PASS**. 초기 테스트 22/24에서 TEMP 파일 이동 권한 오류 2건 확인→허용된 workspace runtime 격리 경로로 변경 후 25/25. 재실행은 이 수정과 추가 최종 명령 테스트 확인 목적.
- 개발 도구: `dotnet --info` workload 정보 수집이 서비스 관리자 권한 오류를 보고했으나 SDK 존재 확인 후 workload resolver 없이 빌드 성공. `dotnet new`의 전역 template cache 쓰기 거절은 workspace DOTNET_CLI_HOME으로 해결. SDK 최초 실행이 HTTPS 개발 인증서 설치 메시지를 출력함; 인증서 신뢰/추가 설치/전역 설정 변경 명령은 실행하지 않음. 이후 검증 스크립트는 인증서 생성·telemetry·workload 갱신을 비활성화.
- NOT VERIFIED: GUI 실행/렌더/키보드/호버/DPI/접근성/장시간 부하·실제 Steam 로그인/조회/등록/승인. 대량 카드 가상화·상세 취소/재시도·SQLite·배포는 미완료.
- 안전: 앱/브라우저/캡처/다른 프로세스 목록·창·입력·메모리 조회 없음. Steam 계정·인증정보·실거래 요청 없음. 정적 금지 API/민감정보 검색과 diff 검토 후 명시 파일만 커밋 예정.
- 다음: 승인된 앱 자체 창 QA, 남은 컨트롤/대량 UI, Valve 허용 경로 검증. 실등록 목표는 LIVE_INTEGRATION_BLOCKED 유지. Git 결과는 아래 후속 기록/최종 보고에 추가.
- 마감 검토: C#/XAML 소스 서식 정리. 솔루션 기반 formatter는 격리 환경 named pipe 권한으로 실패해 권한 확대 없이 폴더/파일 formatter로 대체했다. XAML 정리 후 앱 빌드도 PASS. staged diff의 추가 EOF 공백을 제거했고, 금지 API/민감정보 패턴 검색 결과 일치 없음. 빌드 출력/테스트 runtime/비밀 설정은 Git 제외.
- Git 결과: 구현 커밋 `9b904bf` (`feat: implement MarketCrate WPF mock workspace and durable batch flow`), `origin/main` 정상 push 성공 (`8fcad5c..9b904bf`). 로컬 .git 쓰기 제한에는 승인된 Git 명령만 사용했고, 강제 push/원격 변경/타 브랜치 조작 없음. 이 결과 기록은 별도 문서 커밋으로 누적한다.

## 2026-10-28 — 2차: 창 없는 UI 검증·SQLite·취소/복구

- 시작: HEAD `65e3ef4`, 작업 트리 clean. `git fetch origin main` 후 원격과 동일함을 확인했다. 이번 사용자 승인은 별도 STA 자체 컨트롤 렌더이며 실제 창·활성화·전역 입력·타 앱 접근으로 확대하지 않았다.
- 화면: MainWindow에서 MainView/UserControl 분리, detached STA 검사 프로젝트 추가. RenderTargetBitmap으로 크기/출력 밀도·검토 오류·배치 결과·취소·컨트롤 상태 PNG 생성/시각 검토. Application.Windows=0, PresentationSource=null 확인. VSM Focus 상태는 시각 모의이며 실제 포커스 이동 없음.
- UI 개선: 검색 placeholder·원형 Radio·상태 색 차이·Stepper 범위/peer/overflow/템플릿 핸들러·스크롤 thumb 최소 크기, inventory 전용 context menu 범위. 카드 VirtualizingTilePanel/recycling과 목록 가상화, 필터·전체 선택 알림 일괄 처리, 결과 변경 행만 갱신.
- 렌더 재현으로 찾은 문제: WPF generator 초기화 순서 및 재활용된 컨테이너가 created=false로 돌아올 때 시각 트리에 재부착하지 않는 누락을 수정. ToolTip은 부모에 붙일 수 없어 독립 컨트롤로 렌더했고, 상태 clock은 테스트 dispatcher의 렌더 큐만 처리해 색 차이를 확인했다.
- 저장: 로컬 캐시 Microsoft.Data.Sqlite 8.0.31/SQLitePCLRaw 2.1.12 사용. CLI_HOME과 별개인 기존 캐시를 NUGET_PACKAGES로 지정해 오프라인 복원 문제 해결. 설치/다운로드 없음. SQLite v1·transaction/FULL/단일 lease, 설정/합성 inventory/배치/증거/판매 목록·삭제 UX, JSON 일회 이관/원본 보존/재이관 방지.
- 도메인: 공유 호가를 예상 합계와 검토에 공통 적용해 기존 계산 불일치를 해결. 최종 직전 현재 inventory/선택 수량/가격·잔량 재검증. 다른 활성 배치 차단, immutable snapshot/확정 결과 역행 거절, 보관 후에도 예약 수량 중복 차단.
- 연결: Mock 요청 증거로 응답 유실/재시작 대조, 수동 fixture→Listed→판매 목록 반영. 개별 선택→취소 검토→선택 전체 Mock 취소→부분 결과 저장. 취소 불명은 Unknown, 취소 완료된 요청 키만 예약 해제. 실제 Steam Guard/네트워크 동작 없음.
- 최종 검증: `scripts/Verify.ps1`, 5개 프로젝트 단일 빌드 경고0/오류0, 도메인/SQLite/MVVM **41/41 PASS**, detached UI **10/10 PASS**. 실제 SQLite 롤백/스키마99/손상/저널 재열기/JSON 이관·삭제/상태 보존 검사 포함.
- 자원/성능: 무거운 빌드·테스트를 병렬 실행하지 않았다. 10,000개 카드에서 실현된 컨테이너 시작9/끝7. 마지막 fixture+복수 렌더 관측259ms는 전후 속도 향상/장기 부하 인증이 아니다. 소프트웨어 렌더링, 전역 시스템 설정 변경 없음.
- 미검증/차단: 실제 입력·모니터 DPI·열린 팝업·스크린리더·장시간/전원 차단·설치·가격 변경·수동 재시도. 실제 Steam 로그인/조회/판매/Guard·타 앱 조사/조작/데스크톱 캡처 없음. 실등록 필수 목표는 LIVE_INTEGRATION_BLOCKED 유지.
- Git: 소스/테스트/누적 문서/패키지 잠금·고지만 커밋한다. 생성 이미지·DB·로그·빌드 바이너리·인증정보는 제외한다. 구현 및 push 결과는 아래 후속 기록에 추가한다.
- 완료 기록: 구현 커밋 `35ffbb4` (`feat: validate detached WPF UI and persist safe mock workflows in SQLite`), 기존 `origin/main`으로 정상 push 성공 (`65e3ef4..35ffbb4`). staged diff 공백 검사 PASS, 금지 API/민감정보 패턴 검색 일치 없음. 허용된 일반 앱의 자체 Show/Focus 코드는 남아 있으나 detached 테스트에서 호출하지 않았음을 실행 경로/Window 미생성 검사로 확인했다. 결과 기록은 별도 문서 커밋으로 누적한다.
11차 최종 회귀: **기능177/177 PASS · 비표시 WPF39/39 PASS · 배포 파일 계약14/14 PASS · FAIL0 · Debug 경고0/오류0**. 기존172/33/14 전부 유지. `scripts/Verify.ps1`, ignored `runtime/stage11-verify.txt` 근거. 실제 서버/물리입력/Popup/설치·서명 미실행.

사용자 최소 재확인: (1) 새 ZIP 버전·커밋 확인 후 Steam 탭에서 명시적 QR 시작으로 QR 표시 여부만 확인(실거래 금지), 실패하면 표시된 단계와 검토한 진단ZIP 확인. QR 원문은 공유하지 않는다. (2) 진단 탭/최근 오류 체크를 클릭한 뒤 포인터를 밖으로 이동: 선택 밑줄·체크 표시만 유지되는지 확인. (3) Tab/Shift+Tab의 점선 포커스, 빠른 탭 전환, 비활성→활성, 드롭다운 열기·닫기를 직접 확인. Codex는 이 작업을 실행하지 않았다.

11차 배포: 구현 커밋 `0705dc2874b64b521e86dd92b3847c1a1b63219c` origin/main push 완료. **0.11.0** self-contained Windows x64 Release 빌드·정적 검사 PASS, 출력 경고/오류0. CLR/WPF10.0.9, x64 apphost/SQLite 네이티브 DLL/라이선스 고지 포함, DB·로그·인증·PDB·개발파일 제외. `artifacts/MarketCrate-0.11.0-stage11-0705dc2-win-x64.zip` **78.29MiB/412파일**, ZIP 전 항목과 원본 SHA256 일치. SHA256 `C3A5AEC49ACB4B8FEA93D4496E49E4D7AB92F2FB7F409F5EA22F1E43A60AEE28`. distribution.json 및 ProductVersion=0.11.0+0705dc2…의 SourceCommit 확인. 실제 EXE/설치/서명/실서버 QR 발급은 미실행. 이전 배포본·사용자별 저장소를 덮어쓰거나 초기화하지 않았다.
