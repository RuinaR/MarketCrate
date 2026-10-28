# Windows 배포 준비 — 3차 구현

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
