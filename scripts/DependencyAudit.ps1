# Validate the public advisory result, not merely the CLI exit code.
function Assert-DependencyAudit($Report, [string[]]$ExpectedProjects) {
    if ($Report.version -ne 1 -or -not (@($Report.sources) -contains 'https://api.nuget.org/v3/index.json')) { throw 'Public advisory source missing; audit NOT VERIFIED' }
    if ($Report.problems -or $Report.errors) { throw 'Advisory lookup failed; audit NOT VERIFIED' }
    foreach ($project in $ExpectedProjects) {
        $matches = @($Report.projects | Where-Object { [IO.Path]::GetFileName($_.path) -eq $project })
        if ($matches.Count -ne 1) { throw 'Incomplete project coverage; audit NOT VERIFIED' }
    }
    foreach ($project in $Report.projects) {
        foreach ($framework in $project.frameworks) {
            foreach ($package in @($framework.topLevelPackages) + @($framework.transitivePackages)) {
                if ($package.vulnerabilities) { throw 'Known vulnerable dependency; release blocked' }
            }
        }
    }
}
