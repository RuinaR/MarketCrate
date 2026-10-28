# Build/inspect only. No runtime/SDK installation, app/VM execution, or machine configuration changes.
param([string]$OutputDirectory = 'artifacts/windows-self-contained')
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
. (Join-Path $PSScriptRoot 'Distribution.ps1')
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) 'runtime/tooling'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
$env:MSBuildEnableWorkloadResolver = 'false'
# Missing runtime PACKS are build inputs restored into the ignored project cache, not installed into Windows.
$env:NUGET_PACKAGES = Join-Path (Get-Location) 'runtime/self-contained-packages'
$fallback = Join-Path $env:USERPROFILE '.nuget/packages'
$output = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputDirectory))
$root = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) 'artifacts')) + [System.IO.Path]::DirectorySeparatorChar
if (-not $output.StartsWith($root,[System.StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $output)) { throw 'Choose a NEW project artifacts output directory; no overwrite or cleanup is performed.' }
dotnet publish src/MarketCrate.App/MarketCrate.App.csproj -c Release -r win-x64 --self-contained true --source https://api.nuget.org/v3/index.json -m:1 -p:UseSharedCompilation=false -p:DebugType=None -p:DebugSymbols=false -p:NuGetLockFilePath=obj/self-contained.packages.lock.json -p:PublishSingleFile=false -p:PublishTrimmed=false -p:PublishReadyToRun=false "-p:RestoreFallbackFolders=$fallback" -o $output --verbosity minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$runtime = (Get-Content -LiteralPath (Join-Path $output 'MarketCrate.App.runtimeconfig.json') -Raw | ConvertFrom-Json).runtimeOptions
$coreVersion = ($runtime.includedFrameworks | Where-Object name -eq 'Microsoft.NETCore.App').version
$desktopVersion = ($runtime.includedFrameworks | Where-Object name -eq 'Microsoft.WindowsDesktop.App').version
$corePack = Join-Path $env:NUGET_PACKAGES "microsoft.netcore.app.runtime.win-x64/$coreVersion"
$desktopPack = Join-Path $env:NUGET_PACKAGES "microsoft.windowsdesktop.app.runtime.win-x64/$desktopVersion"
if (-not (Test-Path -LiteralPath $corePack)) { $corePack = Join-Path $fallback "microsoft.netcore.app.runtime.win-x64/$coreVersion" }
if (-not (Test-Path -LiteralPath $desktopPack)) { $desktopPack = Join-Path $fallback "microsoft.windowsdesktop.app.runtime.win-x64/$desktopVersion" }
$licenseDirectory = Join-Path $output 'licenses'
New-Item -ItemType Directory -Path $licenseDirectory -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $corePack 'LICENSE.TXT') -Destination (Join-Path $licenseDirectory 'NETCore-LICENSE.txt')
Copy-Item -LiteralPath (Join-Path $corePack 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $licenseDirectory 'NETCore-THIRD-PARTY-NOTICES.txt')
Copy-Item -LiteralPath (Join-Path $desktopPack 'LICENSE') -Destination (Join-Path $licenseDirectory 'WindowsDesktop-LICENSE.txt')
$metadata = Assert-Distribution $output SelfContained
pwsh -NoProfile -File scripts/Test-ProductionAssembly.ps1 -Directory $output
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$metadata | Add-Member -NotePropertyName RuntimeMode -NotePropertyValue LiveOnly
$metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $output 'distribution.json') -Encoding utf8
Write-Output 'PASS self-contained win-x64 static inspection: bundled CLR/WPF/SQLite, runtime config, licenses and data/debug exclusion. App/guest/installation NOT executed.'
