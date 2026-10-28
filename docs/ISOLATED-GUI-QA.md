# 실제 GUI 후속 검사 구성 — 준비만 완료, 실행 NOT VERIFIED

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
