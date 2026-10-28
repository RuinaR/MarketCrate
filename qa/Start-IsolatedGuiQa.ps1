# GUEST ONLY. Never run on the user's host. No OS input/capture/VM discovery.
param([string]$BundleDirectory = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$ackPath = 'C:\MarketCrateQa\guest-only.ack'
if (-not (Test-Path -LiteralPath $ackPath) -or (Get-Content -LiteralPath $ackPath -Raw).Trim() -ne 'dedicated-offline-windows-guest') {
    throw 'A human must provision a dedicated OFFLINE Windows guest and create its guest-only acknowledgement. No host fallback.'
}
# The marker is an operator assertion, NOT proof that this machine is a VM.
$bundle = (Resolve-Path -LiteralPath $BundleDirectory).Path
$manifest = Get-Content -LiteralPath (Join-Path $bundle 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.Schema -ne 1 -or $manifest.Mode -ne 'OfflineMockOnly') { throw 'Unsupported QA bundle' }
foreach ($file in $manifest.Files) {
    if ($file.Path -match '(^[\\/]|:|(^|[\\/])\.\.([\\/]|$))') { throw 'Unsafe manifest path' }
    $path = Join-Path $bundle $file.Path
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.Sha256) { throw 'Bundle hash mismatch' }
}
$deployment = if ($manifest.Deployment) { $manifest.Deployment } else { 'FrameworkDependent' } # stage-eight bundles
if ($deployment -eq 'FrameworkDependent') {
    if (-not ((& dotnet --list-runtimes) -match 'Microsoft.WindowsDesktop.App 10\.')) { throw '.NET 10 Windows Desktop Runtime must be prepared separately in this guest, or use the self-contained bundle.' }
} elseif ($deployment -eq 'SelfContained') {
    # No system dotnet invocation or installation for a bundled-runtime package.
    $runtime = (Get-Content -LiteralPath (Join-Path $bundle 'app/MarketCrate.App.runtimeconfig.json') -Raw | ConvertFrom-Json).runtimeOptions
    if ($runtime.frameworks -or -not ($runtime.includedFrameworks | Where-Object { $_.name -eq 'Microsoft.WindowsDesktop.App' -and $_.version -like '10.*' })) { throw 'Missing bundled Desktop Runtime configuration' }
    foreach ($name in @('coreclr.dll','hostfxr.dll','PresentationFramework.dll','wpfgfx_cor3.dll','e_sqlite3.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $bundle ('app/'+$name)) -PathType Leaf)) { throw 'Incomplete self-contained package' }
    }
} else { throw 'Unsupported deployment mode' }
$guestApp = 'C:\MarketCrateQa\App'
if (Test-Path -LiteralPath $guestApp) {
    # Restart the SAME reviewed build without copying/overwriting anything.
    foreach ($file in @($manifest.Files | Where-Object { $_.Path -match '^app[\\/]' })) {
        $installed = Join-Path $guestApp $file.Path
        if (-not (Test-Path -LiteralPath $installed -PathType Leaf) -or (Get-FileHash -LiteralPath $installed -Algorithm SHA256).Hash -ne $file.Sha256) { throw 'Existing guest app differs. Preserve/archive that folder manually inside the guest before an update.' }
    }
} else {
    New-Item -ItemType Directory -Path $guestApp | Out-Null
    Copy-Item -LiteralPath (Join-Path $bundle 'app') -Destination $guestApp -Recurse
}
$resultsDirectory = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'MarketCrate/IsolatedGuiQa/QaResults'
New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null
$result = Get-Content -LiteralPath (Join-Path $bundle 'scenarios.json') -Raw | ConvertFrom-Json
if ($result.Cases.Count -ne 24 -or @($result.Cases | Where-Object Result -ne 'NOT VERIFIED').Count) { throw 'Expected 24 unexecuted GUI scenarios' }
$result.Environment.SourceCommit = if ($manifest.SourceCommit) { $manifest.SourceCommit } else { $null }
$result.Environment.Runtime = 'Package '+$deployment+'; actual runtime is recorded in the diagnostic ZIP'
$result.Environment.Windows = [System.Environment]::OSVersion.VersionString
$resultPath = Join-Path $resultsDirectory ('gui-'+[Guid]::NewGuid().ToString('N')+'.json')
$result | Add-Member -NotePropertyName StartedUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('O'))
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $resultPath -Encoding utf8
$env:MARKETCRATE_ISOLATED_QA = 'dedicated-offline-windows-guest'
try {
    # Visible interactive app explicitly requested INSIDE the offline guest only. No automation.
    $run = Start-Process -FilePath (Join-Path $guestApp 'app/MarketCrate.App.exe') -ArgumentList '--isolated-qa' -WindowStyle Normal -Wait -PassThru
    $result | Add-Member -NotePropertyName EndedUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('O'))
    $result | Add-Member -NotePropertyName ExitCode -NotePropertyValue $run.ExitCode
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $resultPath -Encoding utf8
    Write-Output "Guest app exit code: $($run.ExitCode). Manual results: $resultPath. App exit does NOT mark scenarios PASS."
} finally { Remove-Item Env:\MARKETCRATE_ISOLATED_QA -ErrorAction SilentlyContinue }
