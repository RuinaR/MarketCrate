# Windows 배포 준비 — 3차 구현

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
