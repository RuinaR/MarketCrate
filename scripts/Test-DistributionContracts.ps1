# Deterministic synthetic FILE tests only. Fake PE headers are never loaded/executed.
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
. (Join-Path $PSScriptRoot 'Distribution.ps1')
$folder = Join-Path (Get-Location) ('runtime/distribution-tests-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $folder | Out-Null
$passed = 0; $failed = 0
function Test-Case([string]$Name,[scriptblock]$Action) {
    try { & $Action; $script:passed++; Write-Output ('PASS '+$Name) } catch { $script:failed++; Write-Output ('FAIL '+$Name+': '+$_.Exception.Message) }
}
function Reject([scriptblock]$Action) { $rejected=$false; try { & $Action | Out-Null } catch { $rejected=$true }; if (-not $rejected) { throw 'Invalid distribution accepted' } }
function Pe([string]$Path,[uint16]$Machine=0x8664) {
    $bytes=[byte[]]::new(256);$bytes[0]=77;$bytes[1]=90;$bytes[60]=64;$bytes[64]=80;$bytes[65]=69
    [BitConverter]::GetBytes($Machine).CopyTo($bytes,68);[System.IO.File]::WriteAllBytes($Path,$bytes)
}
New-Item -ItemType Directory -Path (Join-Path $folder 'licenses') | Out-Null
foreach ($name in @('MarketCrate.App.exe','MarketCrate.App.dll','MarketCrate.Core.dll','MarketCrate.Infrastructure.dll','PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll','System.Private.CoreLib.dll','coreclr.dll','clrjit.dll','hostfxr.dll','hostpolicy.dll','PresentationNative_cor3.dll','wpfgfx_cor3.dll','e_sqlite3.dll','System.Security.Cryptography.ProtectedData.dll')) { Pe (Join-Path $folder $name) }
foreach ($name in @('THIRD-PARTY-NOTICES.md','licenses/EFCore-MIT-LICENSE.txt','licenses/SQLitePCLRaw-LICENSE.txt','licenses/SQLitePCLRaw-NOTICE.txt','licenses/QrCodeGenerator-LICENSE.txt','licenses/steam-session-LICENSE.txt','licenses/NETCore-LICENSE.txt','licenses/NETCore-THIRD-PARTY-NOTICES.txt','licenses/WindowsDesktop-LICENSE.txt')) { Set-Content -LiteralPath (Join-Path $folder $name) -Value 'synthetic notice' }
$config=Join-Path $folder 'MarketCrate.App.runtimeconfig.json'
$selfConfig='{"runtimeOptions":{"includedFrameworks":[{"name":"Microsoft.NETCore.App","version":"10.0.14"},{"name":"Microsoft.WindowsDesktop.App","version":"10.0.14"}]}}'
Set-Content -LiteralPath $config -Value $selfConfig
Set-Content -LiteralPath (Join-Path $folder 'MarketCrate.App.deps.json') -Value '{"runtimeTarget":{"name":".NETCoreApp,Version=v10.0/win-x64"}}'
Test-Case 'Self-contained file contract (not an executable compatibility test)' { $m=Assert-Distribution $folder SelfContained; if($m.Deployment -ne 'SelfContained'-or $m.AppExecuted) { throw 'Bad metadata' } }
Test-Case 'Old bundled runtime below reviewed security minimum rejected' {
    Set-Content -LiteralPath $config -Value $selfConfig.Replace('10.0.14','10.0.9')
    try { Reject { Assert-Distribution $folder SelfContained } } finally { Set-Content -LiteralPath $config -Value $selfConfig }
}
Test-Case 'Wrong apphost architecture rejected' { Pe (Join-Path $folder 'MarketCrate.App.exe') 0x14c; Reject { Assert-Distribution $folder SelfContained }; Pe (Join-Path $folder 'MarketCrate.App.exe') }
Test-Case 'Wrong WPF native architecture rejected' { Pe (Join-Path $folder 'wpfgfx_cor3.dll') 0x14c; Reject { Assert-Distribution $folder SelfContained }; Pe (Join-Path $folder 'wpfgfx_cor3.dll') }
Test-Case 'Wrong SQLite architecture rejected' { Pe (Join-Path $folder 'e_sqlite3.dll') 0x14c; Reject { Assert-Distribution $folder SelfContained }; Pe (Join-Path $folder 'e_sqlite3.dll') }
Test-Case 'FDD config cannot masquerade as bundled runtime' {
    Set-Content -LiteralPath $config -Value '{"runtimeOptions":{"frameworks":[{"name":"Microsoft.WindowsDesktop.App","version":"10.0.0"}]}}'
    Reject { Assert-Distribution $folder SelfContained }; Set-Content -LiteralPath $config -Value $selfConfig
}
Test-Case 'Missing runtime license rejected' {
    $license=Join-Path $folder 'licenses/NETCore-LICENSE.txt'; [System.IO.File]::Delete($license); Reject { Assert-Distribution $folder SelfContained };Set-Content -LiteralPath $license -Value 'synthetic notice'
}
Test-Case 'Auth directory cannot be shipped even when empty' { $auth = Join-Path $folder 'Auth'; New-Item -ItemType Directory -Path $auth | Out-Null; try { Reject { Assert-Distribution $folder SelfContained } } finally { Remove-Item -LiteralPath $auth } }
foreach($forbiddenFile in @('sample.db','sample.sqlite3-wal','sample.db-shm','auth-session.json','trace.jsonl','.env.local','private.key','MarketCrate.App.pdb','refresh.authbin','store.authlock','refresh.pending','restore.blocked')) {
    Test-Case ('Forbidden file '+$forbiddenFile) { $path=Join-Path $folder $forbiddenFile;Set-Content -LiteralPath $path -Value 'synthetic';Reject { Assert-Distribution $folder SelfContained };[System.IO.File]::Delete($path) }
}
Write-Output "DISTRIBUTION RESULT $passed PASS / $failed FAIL; file inspection only; app/guest NOT executed"
if($failed){exit 1}
. (Join-Path $PSScriptRoot 'ArtifactMaintenance.ps1')
$null=Remove-ProvenFixture $folder
