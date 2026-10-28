# 외부 의존성 고지

이 파일은 의존성 고지이며 MarketCrate 자체의 배포 라이선스를 선정하지 않습니다. 패키지 바이너리는 커밋하지 않습니다. 아래 QR 순수 계산 소스는 MIT 허가와 고지를 유지해 저장소에 포함합니다. 배포 시 각 의존성의 라이선스/NOTICE 원문을 함께 제공해야 합니다.

| 구성요소 | 버전 | 라이선스 / 출처 |
|---|---|---|
| Microsoft.Data.Sqlite, Microsoft.Data.Sqlite.Core | 8.0.31 | MIT · © Microsoft Corporation · https://github.com/dotnet/efcore |
| SQLitePCLRaw.bundle_e_sqlite3, core, provider.e_sqlite3, lib.e_sqlite3 | 2.1.12 | Apache-2.0 · Copyright 2014-2024 SourceGear, LLC · https://github.com/ericsink/SQLitePCL.raw |
| QR Code Generator for .NET (순수 계산 소스 6파일) | v2.0.0, commit 11cf58fe6cec5bc44f70aa360e84b8d37570e37d | MIT · Manuel Bleichenbacher / Project Nayuki · https://github.com/manuelbl/QrCodeGenerator/tree/v2.0.0 |

QR 소스: `src/MarketCrate.Infrastructure/ThirdParty/QrCodeGenerator`. 각 파일의 원 저작권/MIT 전문을 보존했고 `#nullable disable`만 추가했다. 원 LICENSE도 함께 보존하며 출력/배포에 `licenses/QrCodeGenerator-LICENSE.txt`와 이 고지를 복사한다. QR 생성에는 네트워크·외부 QR 이미지 서비스·시스템 화면 접근이 없다. 바이너리 패키지/새 SDK 설치는 하지 않았다. Steam 비교 제품의 코드는 포함하지 않는다.

판정 근거: 실제 로컬 패키지의 .nuspec license/copyright 메타데이터 및 packages.lock.json. 라이선스 본문: https://licenses.nuget.org/MIT , https://licenses.nuget.org/Apache-2.0 . native lib.e_sqlite3 패키지 역시 Apache-2.0으로 표기되어 있으며 내장 SQLite 상위 프로젝트 정보는 https://sqlite.org/copyright.html 에서 확인할 수 있습니다.

로컬 캐시 패키지의 고정 버전을 사용했으며 최신 버전/전체 취약점·배포 라이선스 감사 완료를 주장하지 않습니다. SteamHelper/CSlytics 등 비교 제품 코드는 재사용하지 않았습니다.
