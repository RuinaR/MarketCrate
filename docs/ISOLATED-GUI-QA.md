# 실제 GUI 후속 검사 구성 — 준비만 완료, 실행 NOT VERIFIED

## 9차 실행 조건 및 배포 선택 (실제 GUI 미실행)

제공된 guest 없음. 최소 시스템 조회: Core(Home계열)/build26200.9711/X64, WindowsSandbox.exe 없음. CIM capability는 권한으로 미확인. [공식 Sandbox 에디션 표](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/)상 Home 미지원. 가상화 활성/SLAT/사용 RAM을 추정하지 않으며 기능 활성화·BIOS·호스트 재부팅/VM 설치·실행은 별도 승인 항목이다. 현재는 환경 제공 전 실제 QA BLOCKED이며 host 실행을 대안으로 사용하지 않는다.

기존24개 scenario ID/NOT VERIFIED를 그대로 사용한다. **아래 새 동작이 이전8차의 Runtime 필수/기존 App 폴더 거부 문구를 대체한다.** 새 하네스를 만들지 않고 기존 스크립트를 확장했다.

| 배포 | 호스트에서 생성만 | 게스트 요구 |
|---|---|---|
| FDD(기존) | `pwsh -File scripts/Publish.ps1`, Prepare-IsolatedGuiQa 기본 모드 | Windows11 x64 + .NET10 Windows Desktop x64 Runtime 사전 준비 |
| SCD(추가) | `pwsh -File scripts/Publish-SelfContained.ps1 -OutputDirectory artifacts/qa-scd-BUILD`, 이어 Prepare-IsolatedGuiQa에 `-Deployment SelfContained -SourceDirectory artifacts/qa-scd-BUILD -OutputDirectory artifacts/qa-bundle-BUILD` | Windows11 x64/일반 사용자. 시스템 dotnet/SDK 설치 불필요; OS/native 전제조건·실제 실행은 별도 검증 |

self publisher는 새 artifacts 폴더만 허용하며 덮어쓰기/정리를 하지 않는다. 누락 runtime **팩**은 공식 NuGet에서 프로젝트 runtime/self-contained-packages에 복원한다(Windows 설치 아님). 기존 offline NuGet.Config·전역 소스는 변경하지 않는다. runtime lock은 각 obj 아래로 분리해 원래 package lock을 바꾸지 않는다. WPF 때문에 trimming/single-file/R2R를 활성화하지 않았다. 실제 runtime patch는 distribution/runtimeconfig/manifest에 기록; self-contained는 전역 runtime 보안 패치를 자동 사용하는 것으로 가정하지 말고 새 runtime으로 재게시한다.

검증된 bundle만 guest 내부로 전달하고 기존 ack/오프라인 운영자 절차를 따른다. ack/env는 VM 보증이 아니다. SCD 분기는 system dotnet을 호출하지 않고 bundled config/필수 파일을 확인한다. 같은 build는 설치된 app hash 일치 시 복사 없이 재실행한다. 다른 build는 자동 덮어쓰기/제거하지 않으며 guest에서 사람이 구 publish를 보존·폴더 변경 후 새 build를 준비한다. 기존 QA Demo/로그/저널은 사용자 루트에 그대로 남는다.

수동 결과는 **게스트** `%LOCALAPPDATA%/MarketCrate/IsolatedGuiQa/QaResults/gui-GUID.json`. 시작 UTC/끝 UTC/exit code/OS/빌드/배포형태만 기록하고 24개 결과는 자동 PASS로 바꾸지 않는다. Operator/DPI/Observed/OperationId/Evidence는 사람이 관찰해 기록한다. 실제 runtime 값은 앱의 진단 ZIP manifest에서 대조한다. 읽기전용 share의 scenarios.json을 편집할 필요가 없다.

진단 경로: guest 앱 Diagnostics → OperationId 입력/Refresh → package 범위 검토 → 사용자 Reviewed → Export. 로그/ZIP은 `%LOCALAPPDATA%/MarketCrate/IsolatedGuiQa/Logs/{session-*.jsonl,exports/*.zip}`. OS/Runtime/commit·정제 이벤트만 포함하고 case JSON/DB/QR/HTTP 원문은 자동 포함·외부 전송하지 않는다. 수동 결과JSON과 ZIP의 OperationId를 사람이 연결한다. 합성 테스트로 export/local path/order/원인 연결을 검증했으며 실제 guest UI/권한/팝업/설치는 미검증이다.

공식 [.NET publish](https://learn.microsoft.com/en-us/dotnet/core/deploying/)와 [Windows 지원표](https://learn.microsoft.com/en-us/dotnet/core/install/windows)를 2026-10-28 KST 확인했다. self-contained가 native OS 의존성을 모두 제거하거나 지원하지 않는 Windows를 지원한다는 뜻은 아니다. 게스트는 지원되는 Windows11 x64 desktop 버전으로 준비하고 OS/DPI/권한·시간/자원 예산을 기록한다. 운영 Steam/실계정/거래는 제외한다.

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
