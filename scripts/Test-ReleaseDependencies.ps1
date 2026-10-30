param([string]$ReportPath='runtime/release-dependency-audit.json')
$ErrorActionPreference='Stop'
Set-Location (Join-Path $PSScriptRoot '..')
. (Join-Path $PSScriptRoot 'DependencyAudit.ps1')
$projects = @('MarketCrate.Core.csproj','MarketCrate.Infrastructure.csproj','MarketCrate.App.csproj','MarketCrate.Tests.csproj','MarketCrate.UiTests.csproj')
$raw = dotnet list MarketCrate.sln package --vulnerable --include-transitive --no-restore --source https://api.nuget.org/v3/index.json --format json 2>&1
if ($LASTEXITCODE -ne 0) { throw 'Advisory lookup failed; audit NOT VERIFIED' }
$json = $raw -join [Environment]::NewLine
$report = $json | ConvertFrom-Json
Assert-DependencyAudit $report $projects
$output = [IO.Path]::GetFullPath((Join-Path (Get-Location) $ReportPath))
$root = [IO.Path]::GetFullPath((Join-Path (Get-Location) 'runtime')) + [IO.Path]::DirectorySeparatorChar
if (-not $output.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) { throw 'Audit output must remain in project runtime' }
$json | Set-Content -LiteralPath $output -Encoding utf8
Write-Output 'PASS public NuGet advisory audit / five projects / direct and transitive packages. Not a runtime advisory or live-service compatibility guarantee.'
