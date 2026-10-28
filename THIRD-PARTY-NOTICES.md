# 외부 의존성 고지

이 파일은 의존성 고지이며 MarketCrate 자체의 배포 라이선스를 선정하지 않습니다. 패키지 바이너리/원본 소스는 저장소에 커밋하지 않습니다. 배포 시 각 패키지의 라이선스/NOTICE 원문을 함께 제공해야 합니다.

| 구성요소 | 버전 | 라이선스 / 출처 |
|---|---|---|
| Microsoft.Data.Sqlite, Microsoft.Data.Sqlite.Core | 8.0.31 | MIT · © Microsoft Corporation · https://github.com/dotnet/efcore |
| SQLitePCLRaw.bundle_e_sqlite3, core, provider.e_sqlite3, lib.e_sqlite3 | 2.1.12 | Apache-2.0 · Copyright 2014-2024 SourceGear, LLC · https://github.com/ericsink/SQLitePCL.raw |

판정 근거: 실제 로컬 패키지의 .nuspec license/copyright 메타데이터 및 packages.lock.json. 라이선스 본문: https://licenses.nuget.org/MIT , https://licenses.nuget.org/Apache-2.0 . native lib.e_sqlite3 패키지 역시 Apache-2.0으로 표기되어 있으며 내장 SQLite 상위 프로젝트 정보는 https://sqlite.org/copyright.html 에서 확인할 수 있습니다.

로컬 캐시 패키지의 고정 버전을 사용했으며 최신 버전/전체 취약점·배포 라이선스 감사 완료를 주장하지 않습니다. SteamHelper/CSlytics 등 비교 제품 코드는 재사용하지 않았습니다.
