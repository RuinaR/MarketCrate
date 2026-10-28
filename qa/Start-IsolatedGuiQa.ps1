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
if (-not ((& dotnet --list-runtimes) -match 'Microsoft.WindowsDesktop.App 10\.')) { throw '.NET 10 Windows Desktop Runtime must be prepared separately in this guest.' }
$guestApp = 'C:\MarketCrateQa\App'
if (Test-Path -LiteralPath $guestApp) { throw 'Use a fresh guest app directory/snapshot; never overwrite data or an existing installation.' }
New-Item -ItemType Directory -Path $guestApp | Out-Null
Copy-Item -LiteralPath (Join-Path $bundle 'app') -Destination $guestApp -Recurse
$env:MARKETCRATE_ISOLATED_QA = 'dedicated-offline-windows-guest'
try {
    # Visible interactive app explicitly requested INSIDE the offline guest only. No automation.
    $run = Start-Process -FilePath (Join-Path $guestApp 'app/MarketCrate.App.exe') -ArgumentList '--isolated-qa' -WindowStyle Normal -Wait -PassThru
    Write-Output "Guest app exit code: $($run.ExitCode). Record observations in scenarios.json; unexecuted cases remain NOT VERIFIED."
} finally { Remove-Item Env:\MARKETCRATE_ISOLATED_QA -ErrorAction SilentlyContinue }
