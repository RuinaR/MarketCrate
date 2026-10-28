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
function Assert-X64Pe([string]$path) {
    $reader = [System.IO.BinaryReader]::new([System.IO.File]::OpenRead($path))
    try {
        if ($reader.ReadUInt16() -ne 0x5a4d) { throw 'Invalid DOS header' }
        $reader.BaseStream.Position = 0x3c
        $offset = $reader.ReadInt32()
        if ($offset -lt 64 -or $offset -gt $reader.BaseStream.Length - 6) { throw 'Invalid PE offset' }
        $reader.BaseStream.Position = $offset
        if ($reader.ReadUInt32() -ne 0x4550 -or $reader.ReadUInt16() -ne 0x8664) { throw 'Expected x64 PE' }
    } finally { $reader.Dispose() }
}
Assert-X64Pe $executable
Assert-X64Pe (Join-Path $publishDirectory 'runtimes/win-x64/native/e_sqlite3.dll')
$runtime = Get-Content -LiteralPath (Join-Path $publishDirectory 'MarketCrate.App.runtimeconfig.json') -Raw | ConvertFrom-Json
if (-not ($runtime.runtimeOptions.frameworks | Where-Object { $_.name -eq 'Microsoft.WindowsDesktop.App' -and $_.version -like '10.*' })) { throw 'Missing Windows Desktop Runtime 10 dependency' }
$unwantedData = Get-ChildItem -LiteralPath $publishDirectory -Recurse -File | Where-Object { $_.Extension -in @('.sqlite','.db','.log','.jsonl','.zip','.dmp','.mdmp','.pfx','.pem','.key','.p12') -or $_.Name -match '(?i)\.(?:sqlite3?|db)(?:-(?:wal|shm|journal))?$|^\.env|(?:session|cookie|credential|token).*\.json$' }
if ($unwantedData) { throw 'Unexpected local data in publish output' }
foreach ($notice in @('THIRD-PARTY-NOTICES.md','licenses/QrCodeGenerator-LICENSE.txt')) { if (-not (Test-Path -LiteralPath (Join-Path $publishDirectory $notice))) { throw 'Required license notice missing' } }
Write-Output 'PASS Windows executable generated; x64 apphost/SQLite PE, Desktop Runtime 10 dependency/data exclusion/license notices checked. App NOT executed. Installation/signing NOT verified.'
