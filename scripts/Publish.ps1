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
dotnet publish src/MarketCrate.App/MarketCrate.App.csproj -c Release --no-self-contained --no-restore -m:1 -p:UseSharedCompilation=false -o $publishDirectory --verbosity minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$executable = Join-Path $publishDirectory 'MarketCrate.App.exe'
$stream = [System.IO.File]::OpenRead($executable)
try { if ($stream.ReadByte() -ne 77 -or $stream.ReadByte() -ne 90) { throw 'Invalid PE header' } } finally { $stream.Dispose() }
$unwantedData = Get-ChildItem -LiteralPath $publishDirectory -Recurse -File | Where-Object { $_.Extension -in @('.sqlite','.db','.log','.jsonl','.zip','.dmp','.mdmp','.pfx','.pem','.key','.p12') -or $_.Name -match '(?i)(\.sqlite|\.db-)|^\.env|(?:session|cookie|credential|token).*\.json$' }
if ($unwantedData) { throw 'Unexpected local data in publish output' }
foreach ($notice in @('THIRD-PARTY-NOTICES.md','licenses/QrCodeGenerator-LICENSE.txt')) { if (-not (Test-Path -LiteralPath (Join-Path $publishDirectory $notice))) { throw 'Required license notice missing' } }
Write-Output 'PASS Windows executable generated; PE header/data exclusion/license notices checked. App NOT executed. Requires .NET 10 Windows Desktop Runtime.'
