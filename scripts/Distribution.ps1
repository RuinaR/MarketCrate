# Pure file inspection shared by publish and offline QA packaging. Never executes app binaries.
function Assert-X64Pe([string]$Path) {
    $reader = [System.IO.BinaryReader]::new([System.IO.File]::OpenRead($Path))
    try {
        if ($reader.ReadUInt16() -ne 0x5a4d) { throw 'Invalid DOS header' }
        $reader.BaseStream.Position = 0x3c; $offset = $reader.ReadInt32()
        if ($offset -lt 64 -or $offset -gt $reader.BaseStream.Length - 6) { throw 'Invalid PE offset' }
        $reader.BaseStream.Position = $offset
        if ($reader.ReadUInt32() -ne 0x4550 -or $reader.ReadUInt16() -ne 0x8664) { throw 'Expected x64 PE' }
    } finally { $reader.Dispose() }
}
function Assert-Distribution([string]$Path, [ValidateSet('FrameworkDependent','SelfContained')][string]$Deployment) {
    if ((Get-Item -LiteralPath $Path).Attributes -band [System.IO.FileAttributes]::ReparsePoint) { throw 'Distribution root links are forbidden' }
    $all = @(Get-ChildItem -LiteralPath $Path -Recurse -Force)
    if ($all | Where-Object { $_.Attributes -band [System.IO.FileAttributes]::ReparsePoint }) { throw 'Distribution links are forbidden' }
    $files = @($all | Where-Object { -not $_.PSIsContainer })
    if ($all | Where-Object { $_.PSIsContainer -and $_.Name -eq 'Auth' }) { throw 'Authentication store directory in distribution' }
    if ($files | Where-Object { $_.Extension -in @('.db','.sqlite','.log','.jsonl','.zip','.dmp','.mdmp','.pfx','.pem','.key','.p12','.authbin','.authlock') -or $_.Name -in @('refresh.pending','restore.blocked') -or $_.Name -match '(?i)\.(?:sqlite3?|db)(?:-(?:wal|shm|journal))?$|^\.env|(?:session|cookie|credential|token).*\.json$' }) { throw 'Unexpected local data in distribution' }
    foreach ($name in @('MarketCrate.App.exe','MarketCrate.App.dll','MarketCrate.Core.dll','MarketCrate.Infrastructure.dll','MarketCrate.App.deps.json','THIRD-PARTY-NOTICES.md','licenses/QrCodeGenerator-LICENSE.txt','licenses/steam-session-LICENSE.txt','System.Security.Cryptography.ProtectedData.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $Path $name) -PathType Leaf)) { throw 'Required application asset missing' }
    }
    Assert-X64Pe (Join-Path $Path 'MarketCrate.App.exe')
    $runtime = (Get-Content -LiteralPath (Join-Path $Path 'MarketCrate.App.runtimeconfig.json') -Raw | ConvertFrom-Json).runtimeOptions
    if ($Deployment -eq 'SelfContained') {
        if ($runtime.frameworks -or $runtime.framework -or -not $runtime.includedFrameworks) { throw 'Expected bundled runtime configuration' }
        foreach ($name in @('Microsoft.NETCore.App','Microsoft.WindowsDesktop.App')) {
            if (-not ($runtime.includedFrameworks | Where-Object { $_.name -eq $name -and $_.version -match '^10\.0\.\d+$' })) { throw 'Bundled runtime framework missing' }
        }
        $deps = Get-Content -LiteralPath (Join-Path $Path 'MarketCrate.App.deps.json') -Raw | ConvertFrom-Json
        if (-not $deps.runtimeTarget.name.EndsWith('/win-x64')) { throw 'Expected win-x64 runtime target' }
        foreach ($name in @('coreclr.dll','clrjit.dll','hostfxr.dll','hostpolicy.dll','PresentationNative_cor3.dll','wpfgfx_cor3.dll','e_sqlite3.dll')) { Assert-X64Pe (Join-Path $Path $name) }
        foreach ($name in @('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll','System.Private.CoreLib.dll','licenses/NETCore-LICENSE.txt','licenses/NETCore-THIRD-PARTY-NOTICES.txt','licenses/WindowsDesktop-LICENSE.txt')) {
            if (-not (Test-Path -LiteralPath (Join-Path $Path $name) -PathType Leaf)) { throw 'Bundled runtime asset or license missing' }
        }
        if ($files | Where-Object { $_.Extension -in @('.pdb','.dbg','.cs','.csproj') }) { throw 'Unexpected debug/development file in self-contained distribution' }
    } else {
        Assert-X64Pe (Join-Path $Path 'runtimes/win-x64/native/e_sqlite3.dll')
        if ($runtime.includedFrameworks -or -not ($runtime.frameworks | Where-Object { $_.name -eq 'Microsoft.WindowsDesktop.App' -and $_.version -like '10.*' })) { throw 'Expected framework-dependent Desktop Runtime 10 configuration' }
    }
    $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $Path 'MarketCrate.Core.dll')).ProductVersion
    $sourceCommit = if ($version -match '\+([0-9a-fA-F]{40})$') { $Matches[1].ToLowerInvariant() } else { $null }
    return [ordered]@{ Schema=1; Deployment=$Deployment; Rid='win-x64'; Runtime=if($Deployment -eq 'SelfContained') { @($runtime.includedFrameworks) } else { @($runtime.frameworks) }; BuildVersion=$version; SourceCommit=$sourceCommit; AppExecuted=$false }
}
