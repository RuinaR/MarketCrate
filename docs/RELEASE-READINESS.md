# 0.20.13 릴리즈 신뢰도 감사 — 2026-10-31 KST

목표는 실패를 감추지 않는 실제 Steam 전용 배포 후보다. 코드/합성 계약/자체 렌더/정적 패키지 검증과 실계정/물리 GUI 검증을 구분한다. 이 문서는 전체 RELEASE_READY 또는 100% 동작 인증서가 아니다.

## 발견하고 수정한 결함

| 결함 | 수정·실제 호출 경로 | 재현/회귀 증거 |
|---|---|---|
| 같은 BatchId의 다른 계정·가격을 이전 승인 결과로 반환 | 최종 승인 → SaleBatchCoordinator.ExecuteAsync → 기존 snapshot 불변성 검사 → journal; 불일치 시 전송 전 거절 | ReleaseSafetyTests.SnapshotIdentity. 변경 가격/계정 거절, 동일 snapshot 재시작은 POST 추가 없음 |
| 취소된 자체 판매의 과거 저널이 새 보유 자산 검토까지 차단 | 판매 중 → 취소 → ReviewCancelledTargetsAsync → 최신 inventory/Orders.Presence → 새 대상·새 가격 검토 → 단일 신규 승인 | ReleaseSafetyTests.OwnedCancelNewReview. 구버전 재주입 FAIL / 수정 PASS. 기존 command 불변, 취소1회/신규 승인 후에만 추가 sell1회 |
| 다른 계정의 같은 AssetKey가 현재 계정의 미확정 수량에 합산 | SteamWorkspaceViewModel.RefreshRequests / readiness → SteamStateStore.AvailableAll(account); 취소 원 sell 검색도 현재 account로 한정 | ReleaseSafetyTests.AccountPendingIsolation. 타 계정 Unknown은 보존하지만 현재 UI 수량/요청에 섞이지 않음 |
| 구버전 런타임이 출하 검사 통과 | Core/WPF 번들10.0.14, Directory.Build.props 보안 최소 버전 → Distribution.Assert-Distribution | 10.0.9 거부 /10.0.14 허용 합성 검사 |
| 빈 advisory source 결과를 보안 검사 성공으로 오해 가능 | Test-ReleaseDependencies → 명시적 NuGet public source, 5개 프로젝트 coverage·오류·직접/전이 취약점 검사 | Test-DependencyAuditContracts 6개: 빈 source/누락 project/조회 오류/직접·전이 취약점 거부 |

기존 Unknown의 소유 요청 ID를 후보 listing ID로 덮어쓰지 않는다. 취소 후 같은 상품의 현재 보유 자산을 새 대상으로 검토하는 것은 원래 반환 인과관계의 확정이 아니다. metadata-less active/pending membership도 후보에서 제외하고 검토 전 재조회한다. 실제 제출 시 기존 계정·자산·가격·수수료·저널 중복 보호를 다시 통과해야 한다.

## 보안·저장소

- Microsoft 공식 2026-10-25 보안 공지에 따라 배포에 포함하는 Core/WPF를10.0.9→10.0.14로 변경했다. 호스트 SDK/런타임·Windows 설정은 설치/변경하지 않았다. 런타임 advisory와 NuGet 패키지 advisory는 별도 검사다.
- Microsoft.Data.Sqlite/ProtectedData10.0.14, SQLitePCLRaw2.1.14로 고정했다. 실제 native SQLite는3.54.0이다. upstream3.54.1보다 최신이라고 주장하지 않는다. 이전 native3.53.3이 취약했다는 판정도 하지 않는다.
- 구 provider8.0.31/native3.53.3으로 합성 schema3 Steam active/archive/Unknown 및 Preferences 생성 → 신 provider10.0.14/native3.54.0 읽기/쓰기 → 구 provider 재읽기 PASS. 새 런타임10.0.14에서도 재읽기 PASS. DB schema 변경/운영 데이터 초기화0.
- 기능 회귀375/375는 기존 호스트 런타임과 번들10.0.14 콘솔 테스트 환경에서 각각 PASS. 기능 결과를 실제 네트워크 성공으로 해석하지 않는다. 자체 WPF 검사는 비표시 STA이며 물리 입력/DPI/Popup 실환경 증거가 아니다.
- 원본 EFCore MIT, SQLitePCLRaw Apache-2.0 LICENSE/NOTICE를 배포에 추가한다. 원본 경로/라이선스는 THIRD-PARTY-NOTICES 참조.

## 출하 판정과 남은 필수 증거

| 영역 | 이번 증거 | 남은 조건 |
|---|---|---|
| 실제 로그인/DPAPI 복원 | 기존 사용자0.17 재시작 성공 관측 + 합성 만료/손상/삭제 검사 유지 | 새 버전 실제 복원, 장기 만료/회전은 사용자 검증 |
| 실제 보관함/판매목록/수수료/호가 | 공개 official/helper/SSIM 계약 대조, 기존 실제 읽기 관측 보존, 합성 회귀 | 대상 계정/앱/통화의 최신 실제 응답이 필요. 불명 모드·수수료·스택 계약은 차단 |
| 단일 승인 전체 판매 | UI→VM→Coordinator→Executor→SQLite→UI, 가격 분할·부분 실패·Guard·Unknown 합성 검사 | 실제 서버 접수/공식 앱 확인/실제 게시의 사용자 증거 필요 |
| 취소/재가격/반환 | 서버 membership+새 자산 검토+새 승인·Unknown 비재전송 | 스택 합산/ID 변경의 반환 인과관계 미확정은 Unknown |
| GUI/배포 실행 | 기존 사용자 수동7개 관측 보존; 새 비표시WPF71개·x64패키지 정적 검사 | 새 실제 창/물리 입력/다중모니터DPI/설치·업데이트는 미실행 |
| 보안 | 정제 JSONL/ZIP·한정 큐·DPAPI·보안 런타임·공개 패키지 advisory | 전체 침투검사/서명 인증서/Valve 허가/제재 위험0은 확보되지 않음 |

최초75개는 COMPLETE47 / PARTIAL16 / MISSING0 / BLOCKED3 / UNVERIFIED9 유지. ID/요구사항을 삭제하지 않았고 합성 PASS만으로 실계정 항목을 승격하지 않았다. 우선순위는 새 배포의 사용자 읽기 확인 → 단일 저가 자산의 수동 승인/게시 대조 → 성공 증거 확보 후 복수 항목·취소 검증이다. Unknown/필수 계약 불일치이면 후속 거래를 중단한다. Codex의 실계정/실제 GUI/타 프로세스 접근은0이다.

## 재현 가능한 검사

1. `pwsh -NoProfile -File scripts/Verify.ps1`: 순차 build/기능/비표시WPF/배포/정리/보안 검사기 합성 계약.
2. `pwsh -NoProfile -File scripts/Test-ReleaseDependencies.ps1`: 공개 advisory 별도 네트워크 조회; source/coverage 없으면 NOT VERIFIED/실패. 현재 lock graph 검사이며 런타임 CVE를 대체하지 않음.
3. `scripts/Publish-SelfContained.ps1`로 새 경로 게시 후 ProductionAssembly/Distribution 검사. GUI는 실행하지 않음.
4. `scripts/Register-Release.ps1`: 전체 ZIP entry SHA와 폴더 일치, 최신본/롤백본 구분. Auth/로그/SQLite·개발 코드 출하 제외.

상세 source comparison은 OPEN-SOURCE-FLOW-AUDIT, UI/VM 연결은 REQUIREMENTS-TRACEABILITY, 사용자 직접 단계는 READ-ONLY-VALIDATION 및 ISOLATED-GUI-QA를 따른다.

중간 회귀에서는 출하 제외인 개발 FileBatchJournal의 atomic File.Move 접근 거부가1회 관측됐다(374/375). 구체 원인은 미확정이며 해당 경로의 독립3회 반복은3/3 PASS다. 타 프로세스 조사·예외 삼키기·위험한 재전송은 하지 않았다. 원기록을 보존하고 최종 후보 결과와 분리한다.


## 0.20.14 추가 입력 경로 검증

수량 입력/재활용·명시적 게임 제거·합계 검증 보완과 사용자 명령14개 그룹/비표시 템플릿4개 그룹을 추가했다. 기본/번들10.0.14 양쪽에서 기능389/389·WPF75/75 PASS다. 자세한 입력 행렬과 실환경 미검증은 [USER-JOURNEY-QA](USER-JOURNEY-QA.md)를 따른다. 위0.20.13 보안·구/신 SQLite 검증은 그대로 유지하며 실계정 성공으로 확대하지 않는다.
