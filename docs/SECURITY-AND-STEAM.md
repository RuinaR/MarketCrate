# MarketCrate — 보안·Steam 계정·메이플 동시 실행

## 7차 추가 경계

ImageCache는 Steam 인증 client와 완전히 분리된 무쿠키/무권한·referrer·redirect/proxy OFF, 단일 확인 CDN/고정 경로다. opt-in과 실현된 컨트롤 구독만 내려받고 unload/재활용/로그아웃 취소, 범위·용량·시간·403/429 latch 제한을 적용했다. URL/토큰/bytes는 로그·journal에 저장하지 않는다. 실제 이미지서버 확인/위험0 보장 아님.

카탈로그는 app/context/name만 계정별 최소 저장하고 진단 ZIP에서 DB를 제외한다. 원자료 없는 정확 평균과 이력 지표의 결제액/통화 의미는 추정으로 활성화하지 않는다. MedianPriceContractSupported도 기본 false, 기존 정책/stack/fee/Unknown proof 차단을 유지한다. 신규 CPU 측정은 자체 테스트 process 하나뿐이며 타 프로세스를 탐색하지 않았다. Sandbox/VM 구성은 파일/절차만 준비하고 실행/Windows 기능 변경은 하지 않았다.

## 6차 진단 보호 경계

- 진단은 자체 앱의 typed 이벤트만 처리한다. 다른 프로세스/화면/입력/패킷/클립보드 감시·메모리 덤프 없음. 폴더 열기는 사용자 직접 명령이며 테스트는 callback 대체다.
- 비밀정보/URL/QR/헤더/원문/실제 계정·자산 ID를 생성 지점에서 제외, 예외 메시지/경로 제거, sink/export whitelist 재정제. 앱 내부 WPF default binding listener도 자유 텍스트 출력 대신 코드만 기록한다. JSONL은 사용자별 평문이며 보안 감사/암호화 저장소로 가장하지 않는다.
- 로그/내보내기/큐 실패는 거래 재전송·예약 해제·실패 확정 사유가 아니다. 거래 SQLite와 별도 운영한다. ZIP은 범위 검토와 새 사용자 확인 후 로컬 저장, 자동 업로드 없음.
- 실제 연동/허용 근거·KRW·스택 계약은 미검증/차단 유지. 자세한 제한과 오류 주입 근거는 DIAGNOSTICS/QA-MATRIX/REQUIREMENTS-TRACEABILITY 참조.

## 5차 연결 UI 및 검증 경계 (최신)

- 실제 사용자 기능의 읽기 UI를 연결했지만 앱 시작/탭 열기는 HTTP/로그인/실저널 생성 없음. 현 작업은 factory 주입 합성 HTTP만 사용했다. 실제 계정/쿠키/클라이언트/외부 창·입력·화면은 접근하지 않았다.
- 실제 거래는 기본 `SteamTradingPolicy.Blocked`; risk/trading 체크는 정책 허가의 대체가 아니다. source capability와 account opt-in을 모두 요구한다. 무허가 자동화의 제재 위험과 인증 탈취 위험은 별도이며 0으로 보장하지 않는다.
- 로컬 QR 순수 계산·공식 모바일 수동 확인·메모리 cookie session만 사용한다. 외부 QR 서비스/브라우저 쿠키 추출/비밀번호/maFile/Guard secret 수집/DPAPI token persistence/덤프/telemetry 없음. 메모리/외부 OS 침해까지 방어한다고 주장하지 않는다.
- 로그아웃/동의 철회가 작업을 취소하고 화면·QR을 비우며 늦은 데이터 응답을 버린다. 미전송 중단은 이미 서버가 받은 요청의 취소 보장이 아니므로 불명 의도는 보존·대조한다. 401/403/429/CAPTCHA/형식 변경 우회나 자동 retry 없음.
- audit는 계정 reference·자산/확정 가격/수량·request key·시각·enum state만 보존한다. raw 응답/쿠키/QR URI/token/자유 예외 메시지는 저장하지 않는다. 실저널은 Demo 삭제 UI와 분리, 배치 archive는 거래 감사 자료를 삭제하지 않는다.
- QA는 자체 STA controls/RenderTargetBitmap 및 owned headless 테스트만. 실제 QR 스캔/사용자 입력/모니터 DPI/Popup/Steam 계정/장시간 장애·설치·보안 감사 미검증. MIT QR 소스와 고지는 함께 배포한다.

## 4차 구현·검증 경계 (최신)

- 이번 사용자 지시로 비공식 구현 후보를 별도 소스로 작성했다. 이것은 Valve 허가나 실계정 테스트 승인이 아니다. 일반 App 구성/실거래는 IntegrationBlocked이며 로그인·거래·취소·Guard 세션 요청을 실행하지 않았다.
- 공개 GitHub 소스 및 공식 Market HTML/JS·공개 orderbook 1회만 네트워크로 읽었다. 테스트의 모든 인증/판매/취소 요청은 in-memory handler가 처리하며 외부로 나가지 않는다. 테스트 소유 headless 프로세스/비표시 STA 경계는 이전과 동일하다.
- 세션은 새 QR 인증에서만 자체 메모리 CookieContainer로 만든다. 기존 브라우저 파일·쿠키 가져오기·Steam 클라이언트/타 프로세스 접근·자체 비밀번호 입력·shared_secret/identity_secret/maFile 없음. token 보존은 구현하지 않았으며 DPAPI가 되었다고 주장하지 않는다. 메모리 문자열 완전 삭제 보장은 하지 않는다.
- host/path/HTTPS/redirect 제한, 외부 QR 서비스 없음, proxy OFF, 비밀값 ToString/응답/예외 로그 없음. SQLite에는 계정 reference/자산/확정 가격/의도·상태만 저장한다. 일반 앱에서 실저널 삭제/자동 초기화 기능 없음.
- 정책 제재 위험과 인증 탈취 위험을 별도로 설명한다. 3초 지연/사람 승인/공식 앱 수동 확인은 안전 보증이나 허가가 아니다. 탐지 회피/사람 행위 위장/자동승인 없음. 401/403/429/challenge/응답 변경/불명 시 세션 중단.
- 현재 어댑터는 실사용 준비 완료가 아니다. QR/실계정 UI, USD 외 통화, 스택 분할, 확정 취소/재가격, 등록ID 없는 결과 대조, 실제 계정/설치/보안 감사는 미검증·미완성이다. 향후 명시적 옵트인 절차는 조사 보고서에 기록했다.

## 3차 검증 경계

- 실제 Steam 로그인/키 발급/API/거래/모바일 승인은 하지 않는다. 공개 문서/약관·오픈소스 설명 조사만 수행했다. publisher 경로의 권한·보안 서버 제한은 개인용 장터 권한과 다르며 조사 보고서의 IntegrationBlocked 결론을 유지한다.
- WPF는 계속 비표시 MainView·RenderTargetBitmap·내부 상태/명령만 검사한다. 실제 창 활성화·Focus/OS 입력·전체 화면 캡처·타 프로세스 조사는 없다.
- 강제 종료/복구 재현을 위한 유일한 Process.Start는 **프로젝트 테스트 DLL의 headless 자식**이다. CreateNoWindow/UseShellExecute=false로 생성한 자식이 자기 프로세스에서 Environment.Exit한다. 다른 프로세스의 PID를 찾거나 메모리/입력/창을 조사·종료하지 않는다. 두 자식 검사도 순차 실행한다.
- 진단 API는 timestamp/DiagnosticCode/count만 받는다. 임의 예외 문자열·계정·아이템명·토큰·쿠키를 로그로 넘기지 않는다. SQLite 진단 최대500개, 시작 DB 오류는 앱 전용 bounded 고정 코드 파일이다. 이 파일/DB/이미지/바이너리는 Git에서 제외한다.
- Release publish/PE 검사만 했으며 생성 앱을 실행·설치·자동 시작·공개 배포하지 않았다. 실제 다른 프로그램 비간섭/제재 위험 0을 인증하는 검사는 아니다.

## 2차 작업의 명시적 승인과 실행 경계

사용자는 MarketCrate 컨트롤을 별도 STA 환경에서 생성하고 RenderTargetBitmap으로 자체 렌더링하는 검증을 승인했다. 이 승인은 실제 창 표시/활성화·전역 입력·타 앱 접근·화면 캡처·Steam 연결로 확장하지 않는다.

테스트는 plain Application 리소스/MainView/UserControl만 생성한다. Application.Run/Window/HwndSource/Popup 열기·Focus 호출 없이 자체 Measure/Arrange, ViewModel 명령, 컨트롤 속성/내부 routed event, VSM 상태, managed automation peer를 검사한다. 타 앱 UI Automation은 사용하지 않는다. Application.Windows.Count=0 및 detached PresentationSource=null을 검사한다. 상태별 Focus 렌더는 실제 포커스를 옮기지 않은 시각 상태 모의다.

SQLite 네이티브 라이브러리는 앱의 합성 DB만 처리한다. 테스트 DB는 workspace/runtime 아래의 격리 경로를 사용하며 일반 앱의 LocalAppData DB를 열지 않는다. 실제 계정/토큰/쿠키/개인 로그는 저장·전송하지 않는다. 합성 증거의 fixture 주입은 자동 Steam Guard 승인이 아니다.

금지 API/민감정보 정적 검색과 테스트 실행 경로를 검토한다. 일반 App.OnStartup의 자체 창 Show/활성 앱 내부 Focus는 정상 앱 코드에 남아 있지만 이번 테스트에서 실행하지 않는다. 다른 프로그램의 실행 여부를 확인하지 않는다.

## 최초 구현 허용 매트릭스 (2026-10-28 KST)

| 기능 | 정책/권한 판정 | 앱 상태 |
|---|---|---|
| 로컬 합성 데이터·순수 계산 | Allowed | Mock / Available |
| 실제 보관함·시세·판매 중 조회 | Limited: 개별 경로·권한 확인 미완료 | Unavailable, 네트워크 어댑터 없음 |
| Steam OpenID 신원 | 거래 권한과 별개, 연결 미구현 | Unavailable |
| 실제 일괄 등록·취소·재등록 | Blocked: 허용된 쓰기 경로 미확보 | IntegrationBlocked |
| Steam Guard 수동 승인 | 실제 연동 시 공식 앱에서 사용자 직접 | Mock 대기 표시만, 승인 자동화 없음 |
| 타 앱 조사·입력·주입·캡처 | 금지 | 관련 코드/호출 없음 |

약관의 자동화 제한을 재확인했다. 비교 제품이나 공개 스크립트의 존재를 Valve 허가로 해석하지 않는다. Support FAQ URL은 열렸지만 본문이 도구에 제공되지 않아 세부 내용을 이번 작업에서 독립 재확인했다고 주장하지 않는다. 실제 연동 전 본문/최신 공식 정책·권한 확인이 남아 있다. 자세한 출처는 REFERENCES.md.

개발 검증은 빌드·합성 콘솔 테스트·소스 정적 검색만 수행한다. 타 프로세스 목록/게임 상태·화면·입력·메모리를 확인하지 않는다. 앱에도 네트워크/계정 입력·자동 시작·전역 후킹 의존성을 추가하지 않았다. 이 소스 검토는 모든 외부 환경에서의 무간섭 인증 또는 제재 위험 0 보장이 아니다.

## 실패하면 안 되는 최상위 규칙
- 이 앱 및 개발 도구는 메이플스토리와 **완전히 독립**해야 한다. 메이플 실행 여부 검사 자체를 하지 않는다.
- 다른 프로세스 열거·핸들 획득·메모리 접근·DLL 주입·후킹·디버거 연결·트래픽 스니핑·게임 파일/로그 조작·안티치트 회피/진단 금지.
- `SendInput`, 전역 후킹, UI Automation으로 다른 앱 조작, 마우스 자동 이동/클릭, 화면 전체 캡처, 클립보드 감시, 포커스 강탈/Alt+Tab, 관리자 권한·드라이버 설치 금지.
- 앱은 자신의 창에서만 입력을 처리하며 창 비활성화 시 글로벌 단축키 작동 금지. 자동 시작·상주 서비스·작업 스케줄러·백그라운드 폴링 기본 OFF.
- Codex가 작업하는 동안 메이플이 켜져 있을 수 있으므로 전체 GUI 자동실행/광범위 테스트/화면 녹화는 사용자 별도 승인 전까지 하지 않음.

## Steam 계정 보호
- 비밀번호·Steam Guard 복구 코드/공유 비밀·세션 쿠키·access token·기밀 헤더를 코드/디스크/SQLite/로그/GitHub에 저장하거나 출력하지 않는다.
- Steam OpenID는 신원만 인증. 장터 쓰기 권한의 증거로 사용하지 않는다.
- 로그인 화면 위조, 세션 탈취, 브라우저 쿠키 자동 수집, Steam 클라이언트 주입/변조, 앱에 Steam Guard 공유 비밀 입력 유도 금지.
- Steam Guard 승인 자동화 없이 공식 모바일 앱에서 직접 승인. 대기 상태인 아이템을 `판매 등록 완료`로 표시하지 않는다.
- 데이터 조회는 허용된 방식만 사용하고 요청 캐시·보수적 속도제한·동시성 제한·중단/백오프. 오류 403/429, 차단·캡차는 우회하지 않음.
- 실등록/취소·재등록은 **Valve가 허용한 방식과 권한을 확인한 뒤에만** 활성화한다. UI에서 전체 판매등록 요청 하나로 전체 항목을 처리한다는 요구는 고정하되, 승인되지 않은 비공식 POST를 안전하다고 속이지 않는다.
- 탐지 회피(랜덤 행동·사람 흉내·프록시 회전·계정 분산·요청 위장) 금지. 정책과 별개로 제재 위험 0 보장 금지.

## 일괄 거래 안정성
- 실전 모드의 버튼은 최종 검토에서 승인할 때만 실제 제출. 자동 실행/자동 재등록/자동 시세에 따라 거래 결정 금지.
- 스냅샷 고정, 입력값 검증, 이중 클릭·재시도 중복 등록 방지, 부분 성공/타임아웃/불명 상태를 저널에 기록하고 재조회 후 조정.
- 서비스가 명시적으로 멱등성을 보장하지 않는다면 HTTP 타임아웃 후 자동 재전송 금지.
- `배치 요청을 보냈다` vs `Steam에서 게시되었다` vs `판매 체결되었다`를 서로 다른 상태로 표시.
- 긴 로그나 Crash dump에 민감정보 남지 않게 필터링. 로컬 데이터 삭제/로그 지우기 UX 제공.

## 정책 출처·판단 기준
- https://store.steampowered.com/subscriber_agreement/ — 무허가 스크립트/봇/매크로 자동화 및 클라이언트·장터 프로세스 변조 제한. 최종 승인 클릭만으로 제재 위험이 없어지는 것은 아님.
- https://help.steampowered.com/en/faqs/view/2E6E-A02C-5581-8904 — 거래/장터 보안 확인 절차.
- https://steamcommunity.com/dev — 공개 Steam Web API 문서; 일반적인 개인 장터 대량 등록 공개 API를 전제하지 말 것.
- 이 문서는 Valve의 서면 허가가 아님. 실제 기능은 현재 정책을 재검토해 `docs/CURRENT-STATUS.md`에 근거/날짜/제한을 보고.
