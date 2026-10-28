$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) 'runtime/tooling'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
$env:MSBuildEnableWorkloadResolver = 'false'
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages'
dotnet build MarketCrate.sln -m:1 -p:UseSharedCompilation=false --verbosity minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet tests/MarketCrate.Tests/bin/Debug/net10.0/MarketCrate.Tests.dll
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
# Detached STA controls only. This does not run the desktop app or show a Window/Popup.
dotnet tests/MarketCrate.UiTests/bin/Debug/net10.0-windows/MarketCrate.UiTests.dll
exit $LASTEXITCODE
