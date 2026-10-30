$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'DependencyAudit.ps1')
$passed=0
function Expect-Rejection([string]$Json) {
    $rejected=$false
    try { Assert-DependencyAudit ($Json|ConvertFrom-Json) @('Example.csproj') } catch { $rejected=$true }
    if(-not $rejected){throw 'Unsafe advisory result accepted'}
    $script:passed++
}
Assert-DependencyAudit ('{"version":1,"sources":["https://api.nuget.org/v3/index.json"],"projects":[{"path":"Example.csproj"}]}'|ConvertFrom-Json) @('Example.csproj');$passed++
Expect-Rejection '{"version":1,"sources":[],"projects":[{"path":"Example.csproj"}]}'
Expect-Rejection '{"version":1,"sources":["https://api.nuget.org/v3/index.json"],"projects":[]}'
Expect-Rejection '{"version":1,"sources":["https://api.nuget.org/v3/index.json"],"projects":[{"path":"Example.csproj"}],"problems":[{"message":"unavailable"}]}'
Expect-Rejection '{"version":1,"sources":["https://api.nuget.org/v3/index.json"],"projects":[{"path":"Example.csproj","frameworks":[{"topLevelPackages":[{"id":"Example","vulnerabilities":[{"severity":"Critical"}]}]}]}]}'
Expect-Rejection '{"version":1,"sources":["https://api.nuget.org/v3/index.json"],"projects":[{"path":"Example.csproj","frameworks":[{"transitivePackages":[{"id":"Example","vulnerabilities":[{"severity":"High"}]}]}]}]}'
Write-Output "DEPENDENCY AUDIT CONTRACT $passed PASS / 0 FAIL"
