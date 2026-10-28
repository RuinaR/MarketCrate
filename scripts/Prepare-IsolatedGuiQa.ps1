# Packaging ONLY. Does not launch a VM, app, shell window, browser, or guest script.
param([string]$OutputDirectory = 'artifacts/isolated-gui-qa',
    [ValidateSet('FrameworkDependent','SelfContained')][string]$Deployment = 'FrameworkDependent',
    [string]$SourceDirectory = '')
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
. (Join-Path $PSScriptRoot 'Distribution.ps1')
if (-not $SourceDirectory) { $SourceDirectory = if ($Deployment -eq 'SelfContained') { 'artifacts/windows-self-contained' } else { 'artifacts/windows-framework-dependent' } }
$source = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $SourceDirectory))
if (-not (Test-Path -LiteralPath (Join-Path $source 'MarketCrate.App.exe'))) { throw 'Run Publish.ps1 first; packaging never builds or launches the app.' }
$output = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputDirectory))
$artifactRoot = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) 'artifacts')) + [System.IO.Path]::DirectorySeparatorChar
if (-not $output.StartsWith($artifactRoot, [System.StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $output)) { throw 'Choose a NEW output folder inside project artifacts; existing contents are preserved.' }
if (-not $source.StartsWith($artifactRoot,[System.StringComparison]::OrdinalIgnoreCase) -or $output.StartsWith($source + [System.IO.Path]::DirectorySeparatorChar,[System.StringComparison]::OrdinalIgnoreCase)) { throw 'Source must be a separate project artifacts directory' }
$distribution = Assert-Distribution $source $Deployment
New-Item -ItemType Directory -Path $output | Out-Null
Copy-Item -LiteralPath $source -Destination (Join-Path $output 'app') -Recurse
Copy-Item -LiteralPath 'qa/Start-IsolatedGuiQa.ps1','qa/scenarios.json' -Destination $output
$files = @(Get-ChildItem -LiteralPath $output -Recurse -File | ForEach-Object {
    if ($_.Extension -in @('.db','.sqlite','.log','.jsonl','.zip','.dmp','.key','.pem') -or $_.Name -match '^\.env|(?:session|cookie|credential|token).*\.json$') { throw 'Local data is forbidden in QA package' }
    [ordered]@{ Path = [System.IO.Path]::GetRelativePath($output,$_.FullName); Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$identity = (git rev-parse HEAD)
[ordered]@{Schema=1;Mode='OfflineMockOnly';PackagingCommit=$identity;SourceCommit=$distribution.SourceCommit;BuildVersion=$distribution.BuildVersion;Deployment=$Deployment;Runtime=$distribution.Runtime;Files=$files} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $output 'manifest.json') -Encoding utf8
Write-Output 'PASS offline GUI QA bundle prepared with hashes. Guest NOT provisioned or executed. Signing/installation NOT performed.'
