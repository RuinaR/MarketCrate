# 외부 의존성 고지

이 파일은 의존성 고지이며 MarketCrate 자체의 배포 라이선스를 선정하지 않습니다. 패키지 바이너리는 커밋하지 않습니다. 아래 QR 순수 계산 소스는 MIT 허가와 고지를 유지해 저장소에 포함합니다. 배포 시 각 의존성의 라이선스/NOTICE 원문을 함께 제공해야 합니다.

| 구성요소 | 버전 | 라이선스 / 출처 |
|---|---|---|
| Microsoft.Data.Sqlite, Microsoft.Data.Sqlite.Core | 8.0.31 | MIT · © Microsoft Corporation · https://github.com/dotnet/efcore |
| SQLitePCLRaw.bundle_e_sqlite3, core, provider.e_sqlite3, lib.e_sqlite3 | 2.1.12 | Apache-2.0 · Copyright 2014-2024 SourceGear, LLC · https://github.com/ericsink/SQLitePCL.raw |
| QR Code Generator for .NET (순수 계산 소스 6파일) | v2.0.0, commit 11cf58fe6cec5bc44f70aa360e84b8d37570e37d | MIT · Manuel Bleichenbacher / Project Nayuki · https://github.com/manuelbl/QrCodeGenerator/tree/v2.0.0 |

QR 소스: `src/MarketCrate.Infrastructure/ThirdParty/QrCodeGenerator`. 각 파일의 원 저작권/MIT 전문을 보존했고 `#nullable disable`만 추가했다. 원 LICENSE도 함께 보존하며 출력/배포에 `licenses/QrCodeGenerator-LICENSE.txt`와 이 고지를 복사한다. QR 생성에는 네트워크·외부 QR 이미지 서비스·시스템 화면 접근이 없다. 바이너리 패키지/새 SDK 설치는 하지 않았다. 이전 배포에서는 Steam 비교 제품 코드를 포함하지 않았다. 0.17.0부터 아래 MIT steam-session 흐름의 제한된 C# 이식을 포함한다.

판정 근거: 실제 로컬 패키지의 .nuspec license/copyright 메타데이터 및 packages.lock.json. 라이선스 본문: https://licenses.nuget.org/MIT , https://licenses.nuget.org/Apache-2.0 . native lib.e_sqlite3 패키지 역시 Apache-2.0으로 표기되어 있으며 내장 SQLite 상위 프로젝트 정보는 https://sqlite.org/copyright.html 에서 확인할 수 있습니다.

로컬 캐시 패키지의 고정 버전을 사용했으며 최신 버전/전체 취약점·배포 라이선스 감사 완료를 주장하지 않습니다. CSlytics/SSIM/SteamMarketHelper의 코드는 포함하지 않았습니다. 아래 MIT 이식과는 구분합니다.

## 선택적 self-contained 배포

Windows x64 self-contained 출력은 Microsoft.NETCore.App 및 Microsoft.WindowsDesktop.App 런타임을 포함합니다. 실제 버전은 출력의 distribution.json/runtimeconfig와 격리 QA manifest에 기록합니다. NuGet 런타임 팩 원본의 MIT LICENSE와 .NET THIRD-PARTY-NOTICES를 각각 `licenses/NETCore-LICENSE.txt`, `licenses/NETCore-THIRD-PARTY-NOTICES.txt`, `licenses/WindowsDesktop-LICENSE.txt`에 복사합니다. SDK/Windows 런타임 설치 없이 프로젝트 전용 build cache에 팩을 복원하며, 바이너리·팩·로컬 로그는 Git에 포함하지 않습니다. 런타임 보안 수정은 자동 roll-forward로 보장되지 않으며 새 버전으로 재게시해야 합니다. 이는 모든 제3자/지원 OS/실행 호환성 감사를 완료했다는 뜻이 아닙니다.

## 0.17.0 — MIT steam-session의 C# 이식

- 원본: DoctorMcKay/node-steam-session, commit **3ac0f34fd964b3f886ba18ef4824ac43c942e030**, `src/LoginSession.ts`의 refreshToken setter 및 WebBrowser getWebCookies 경로.
- Copyright (c) 2022 Alex Corn, **MIT**. 전문은 `src/MarketCrate.Infrastructure/ThirdParty/steam-session/LICENSE`, 배포 `licenses/steam-session-LICENSE.txt`에 보존한다.
- 이식 위치: `SteamWebRefreshToken.cs`, `SteamQrAuthentication.cs`. C#으로 audience/계정 검증과 finalizelogin→Community settoken 흐름을 적용하고, 만료·상한·소유 계정·쿠키 일치 검사를 추가했다. 상위 프로젝트의 debug 원문 출력, 재시도, 임의 도메인 전송, SteamClient/Mobile 쿠키 합성, Guard 기능은 가져오지 않았다. Node.js/NPM 런타임·프로젝트 전체를 포함하지 않는다.
- 원본 URL: https://github.com/DoctorMcKay/node-steam-session/blob/3ac0f34fd964b3f886ba18ef4824ac43c942e030/src/LoginSession.ts
- 추가 패키지: **System.Security.Cryptography.ProtectedData 10.0.9**, MIT · © Microsoft Corporation · https://github.com/dotnet/runtime . 고정 패키지/contentHash는 packages.lock.json 참조. 공식 NuGet 복원/감사 결과와 합성 검증은 전체 보안 감사나 실제 Steam 호환성 보증이 아니다.
- SSIM(Apache-2.0), node-steam-user/community(MIT), SteamMarketHelper(MIT), CSlytics(AGPL-3.0)는 조사만 했다. 서버 계약·기본값·프로토콜 목적이 다른 부분을 구분했고 코드는 포함하지 않았다. Valve 자동화 허가를 확보했다는 의미가 아니다.
ProtectedData10.0.9 패키지의 THIRD-PARTY-NOTICES.TXT(78,041바이트)는 포함된 NETCore10.0.9의 고지와 SHA256이 동일함을 확인했다. self-contained 배포의 licenses/NETCore-THIRD-PARTY-NOTICES.txt 및 NETCore-LICENSE.txt에 이 고지를 보존한다.
