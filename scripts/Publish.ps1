$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) 'runtime/tooling'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
$env:MSBuildEnableWorkloadResolver = 'false'
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages'
$publishDirectory = Join-Path (Get-Location) 'artifacts/windows-framework-dependent'
dotnet publish src/MarketCrate.App/MarketCrate.App.csproj -c Release --no-self-contained -m:1 -p:UseSharedCompilation=false -o $publishDirectory --verbosity minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
. (Join-Path $PSScriptRoot 'Distribution.ps1')
$metadata = Assert-Distribution $publishDirectory FrameworkDependent
$metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $publishDirectory 'distribution.json') -Encoding utf8
Write-Output 'PASS framework-dependent x64 static inspection: apphost/SQLite PE, Desktop Runtime10 dependency, licenses and data exclusion. App/installation NOT executed.'
