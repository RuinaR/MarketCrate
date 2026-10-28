# 실제 GUI 후속 검사 구성 — 준비만 완료, 실행 NOT VERIFIED

## 8차 게스트 전용 하네스 — 호스트에서는 실행 금지

구현 `f672120` Release 정적 검사 PASS, `artifacts/isolated-gui-qa` 39개 파일 SHA256·빌드 Git metadata 확인. 패키징만 실행했으며 게스트 launcher는 AST 구문 확인만 수행했다. GUI-01~24는 전부 NOT VERIFIED.

현재 전용 Windows VM/접속 정보는 제공되지 않았다. VM 목록/프로세스 탐색, Windows 기능 설치·활성화, Sandbox/VM 시작은 수행하지 않았다. **격리 환경 미확보/실제 GUI·런타임 실행·설치 전부 NOT VERIFIED.** 아래 구성은 대체 호스트 실행을 허용하지 않는다.

1. 호스트의 `scripts/Publish.ps1`로 실행 없이 정적 배포 검사 후 `pwsh -File scripts/Prepare-IsolatedGuiQa.ps1`로 **새** artifacts 폴더에 package를 만든다. app/guest-local launcher/24 case 결과표/전체 SHA256 manifest만 포함한다. manifest PackagingCommit은 패키징 기준이며 실제 DLL source commit metadata를 함께 기록한다. 이 스크립트는 guest/app을 시작하지 않는다.
2. 사람이 별도 전용 Windows x64 VM의 오프라인 snapshot을 준비한다. .NET10 **Windows Desktop x64** Runtime, 일반 사용자, 운영 DB/계정/Steam 클라이언트 없음. 네트워크·vGPU·클립보드·장치 전달 OFF, VM RAM/시간 제한을 검토한다. 고정 DPI별 snapshot을 따로 사용하고 호스트 설정은 바꾸지 않는다. 제공된 Sandbox 예제는 준비 자료이지 설치·격리 보증이 아니다.
3. 검토한 package만 읽기 전용 share 또는 offline media로 게스트에 전달한다. 사용자 홈/저장소/운영 로그를 공유하지 않는다. **게스트 내부에서만** `C:\MarketCrateQa\guest-only.ack`에 `dedicated-offline-windows-guest` 한 줄을 사람이 작성한다. 이 파일은 VM 탐지가 아닌 운영자 확인이므로 호스트에 만들지 않는다.
4. 게스트 내부 PowerShell에서 package의 `Start-IsolatedGuiQa.ps1`을 수동 실행한다. ack/manifest hash/Runtime을 검사하고 새 guest app 폴더로 복사한다(기존 폴더 덮어쓰기 거부). 눈에 보이는 자체 앱만 실행, 입력/캡처 자동화 없음. 사전 검사를 통과한 것이 GUI PASS는 아니다.
5. App은 `--isolated-qa`와 해당 process env 확인이 모두 필요하다. 미확인/오타 args는 종료4, 일반 실행으로 조용히 fallback하지 않는다. QA Steam factory/로그인 UI와 직접 BeginAsync도 차단하며 정상 Mock 서비스만 사용한다. 데이터/로그는 `%LOCALAPPDATA%/MarketCrate/IsolatedGuiQa/{Demo,Logs}`로 분리, 운영 Demo/SteamExperimental과 격리. env 확인은 보안 sandbox가 아니며 실제 격리는 VM 운영에 달려 있다.
6. `scenarios.json`의 GUI-01~24를 게스트 내부에서 관찰해 결과/OperationId/빌드/OS/Runtime/DPI/증거를 기록한다. 기본값은 전부 NOT VERIFIED. 실제 Tab/ShiftTab/Enter/Esc/휠/Popup/스크린리더/최소화·복원·실행/종료·24개 경로를 수동 검증한다. Steam/실이미지 네트워크는 이 offline 프로필에서 시험하지 않는다.
7. 이전 **합성 DB 복사본만** guest QA 루트에 두고 앱 종료→기존 publish 보존→새 폴더 publish 교체→재실행으로 schema/예약/Unknown 보존을 검사한다. 강제 종료는 게스트의 대상 MarketCrate 자체에 한정해 별도 수동 시나리오로만 수행한다. 호스트 PID/다른 앱 조사 없음. 설치/제거 프로그램은 현재 없으므로 해당 검사는 NOT VERIFIED, 서명 미적용.
8. 진단 ZIP은 앱 내부에서 범위 검토 후 로컬 내보낸다. package share로 자동 복사/외부 전송하지 않는다. 검증 후 snapshot 복원/자료 반출은 사람이 검토한다. 장시간 검사는 별도 시간/자원 예산과 자체 프로세스 측정만 허용하며 이번 짧은 자동 검사로 대체하지 않는다.

이번 개발은 실제 Window/Popup/입력/DPI/VM을 실행하지 않았다. 이 문서는 **추후 사용자가 별도 승인한 격리 Windows 환경**에서 사람이 수행할 절차다. 현재 호스트의 포커스·배율·해상도·Windows 기능/설정을 바꾸거나 Sandbox/VM을 자동 실행하는 스크립트는 없다.

## 환경 준비 (미실행)

- 전용 Windows VM과 고정 DPI별 snapshot(100/125/150/200%)을 권장한다. 사용자 동의와 자원 여유 확인 후 별도 시간에 준비한다. 실제 계정/Steam 클라이언트/게임/운영 DB를 넣지 않는다. 전체 프로젝트/사용자 홈을 공유하지 않는다.
- 대안 `qa/windows-sandbox.wsb.example`는 **실행 파일 확장자가 아닌 예제 XML**이다. [Microsoft 구성 문서](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-configure-using-wsb-file)에 근거해 네트워크/vGPU/클립보드/장치 전달 OFF, publish 폴더 읽기 전용, 자동 LogonCommand 없음으로 작성했다. 호스트 경로를 검토한 publish 폴더 하나로만 바꾼다. 지금 실행하거나 호스트에 Sandbox 기능을 설치/활성화하지 않는다.
- .NET 10 Windows Desktop Runtime이 준비된 오프라인 환경을 사용한다. Sandbox의 기본 이미지에 런타임이 없으면 이 템플릿만으로 실행되지 않는다. 런타임 설치/관리자 권한은 별도 승인 범위이며 여기서 수행하지 않았다. Sandbox는 장시간/영구 데이터 시험 환경 대체가 아니다.

## 실행 체크리스트 (전부 NOT VERIFIED)

1. `scripts/Publish.ps1` 산출물의 commit/PE/고지/데이터 제외 결과를 기록하고 격리 내부 폴더에 복사한다. 사람이 해당 환경 안에서 앱을 시작한다. 자동 전역 입력·스크린샷 도구는 사용하지 않는다.
2. Mock 인벤토리→게임 필터→검색/수량/전체 선택→가격 기준→검토/수동 단가→한 번 Mock 배치→Guard 모의 대기→상태 대조→판매 취소/재가격을 수동 확인한다. 실제 Steam 로그인/거래는 이 QA 범위에서도 제외한다.
3. 실제 마우스 hover/pressed/focus, Tab/Shift+Tab/Space/Enter/Esc, ComboBox popup 열기·외부 클릭 닫기, 스크롤/숫자 스테퍼 wheel, 툴팁/컨텍스트 메뉴/긴 문자열/모달 크기를 점검한다. 물리 입력과 detached VSM 테스트를 혼동하지 않는다.
4. 고정 환경별 1100×700/1100×800/1440×900, DPI snapshot별 배치를 확인한다. 호스트 모니터/설정을 변경하지 않는다. 타 앱 픽셀을 포함하는 캡처는 하지 않는다. 증거가 필요하면 별도 허용된 자체 RenderTargetBitmap만 사용한다.
5. 격리 사용자 LocalAppData의 Demo/SteamExperimental/Logs 위치를 확인하고 **합성 DB 복사본만**으로 재시작/새 publish 교체/구 버전 schema 보존을 시험한다. publish 폴더/읽기 전용 host share에 DB/로그가 생기면 FAIL이다.
6. 진단 모드/작업 ID/오류 검색/ZIP 검토/내보내기를 수동 확인한다. 운영 자료/QR/토큰을 넣거나 자동 전송하지 않는다. 격리 환경 외부로 자료를 반출하는 동작도 별도 검토한다.
7. 긴 반복 스크롤/이미지·메모리/CPU 및 앱 종료는 별도 자원·시간 제한을 정한 후 수행한다. 현재의 60회 썸네일 재활용은 장시간 누수 시험으로 간주하지 않는다.

기록 필드: 실행자/UTC/빌드 commit/Windows 및 Runtime 버전/고정 DPI/논리 창 크기/시나리오/예상 상태/관찰 상태/PASS·FAIL·BLOCKED·NOT VERIFIED/OperationId/합성 증거 경로. 실제 계정 연결은 별도의 허용 근거/사용자 명시 승인/서버 계약 계획이 먼저 필요하다.
