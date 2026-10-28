# Default is a recorded dry-run. Existing executable folders, evidence, caches and user data are never removed.
param([switch]$Apply,[string]$PlanPath)
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ArtifactMaintenance.ps1')
$root=$script:ArtifactProjectRoot;$artifacts=Join-Path $root 'artifacts';$runtime=Join-Path $root 'runtime'
if($Apply){
    if(-not$PlanPath){throw 'Apply requires a previously reviewed plan'};$null=Assert-OwnedPath $PlanPath $artifacts;$plan=Get-Content -LiteralPath $PlanPath -Raw|ConvertFrom-Json
    $indexPath=Join-Path $artifacts 'releases.json';$null=Assert-OwnedPath $indexPath $artifacts;$index=Get-Content -LiteralPath $indexPath -Raw|ConvertFrom-Json
    $version=([xml](Get-Content (Join-Path $root 'Directory.Build.props')-Raw)).Project.PropertyGroup.Version
    if($index.Latest.Version-ne$version-or-not$index.Latest.Verified){throw 'A verified current release must exist before old cleanup'}
    foreach($release in @($index.Latest,$index.Rollback)){$zip=Assert-OwnedPath (Join-Path $artifacts $release.Zip) $artifacts;if((Get-FileHash -LiteralPath $zip).Hash-ne$release.Sha256){throw 'Protected release integrity mismatch'}}
    $removed=@();$skipped=@();foreach($candidate in $plan.Candidates){try{$removed+=Remove-ProvenFixture $candidate.Path}catch{$skipped+=[pscustomobject]@{Path=$candidate.Path;Reason='Changed/locked/unproven; preserved'}}}
    $report=[pscustomobject]@{At=[DateTimeOffset]::UtcNow;RemovedFiles=($removed.Files|Measure-Object).Count;RemovedBytes=($removed|Measure-Object Bytes -Sum).Sum;Removed=@($removed|Select-Object Path,Bytes);Skipped=$skipped;ProtectedReleases=@($index.Latest,$index.Rollback)}
    $out=Join-Path $artifacts ('cleanup-result-'+[Guid]::NewGuid().ToString('N')+'.json');$report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $out -Encoding utf8;$report|ConvertTo-Json -Depth 5;Write-Output "Report: $out";exit
}
$candidates=@();$preserved=@();foreach($folder in Get-ChildItem -LiteralPath $runtime -Directory -Filter 'distribution-tests-*'){
    try{$candidates+=Get-ProvenFixture $folder.FullName}catch{$preserved+=[pscustomobject]@{Path=$folder.FullName;Reason='Unknown/reparse/content not exact; preserved'}}
}
# Metadata-only inventory, never follows directory reparse points.
function Size([string]$Path){$total=0L;$pending=[Collections.Generic.Stack[string]]::new();$pending.Push($Path);while($pending.Count){$next=$pending.Pop();$null=Assert-OwnedPath $next $artifacts;foreach($item in Get-ChildItem -LiteralPath $next -Force){if($item.Attributes-band[IO.FileAttributes]::ReparsePoint){continue};if($item.PSIsContainer){$pending.Push($item.FullName)}else{$total+=$item.Length}}};return $total}
$inventory=@();foreach($item in Get-ChildItem -LiteralPath $artifacts -Force){$refs=@(rg -l -F -g '*.md' -g '*.ps1' -g '*.cs' -g '*.xaml' -g '*.csproj' -g '!**/obj/**' -g '!**/bin/**' -- $item.Name (Join-Path $root 'docs') (Join-Path $root 'scripts') (Join-Path $root 'src') (Join-Path $root 'tests') (Join-Path $root 'README.md'));$size=if($item.Attributes-band[IO.FileAttributes]::ReparsePoint){$null}elseif($item.PSIsContainer){Size $item.FullName}else{$item.Length};$inventory+=[pscustomobject]@{Name=$item.Name;Bytes=$size;References=$refs.Count;Decision='Preserve';Reason=if($item.PSIsContainer){'Executable/staging directory use or user content cannot be disproved; no process inspection'}else{'Current/rollback/evidence or unregistered output'}}}
$plan=[pscustomobject]@{Schema=1;At=[DateTimeOffset]::UtcNow;Mode='DryRun';Candidates=$candidates;Preserved=$preserved;Artifacts=$inventory;Untouched=@('All executable release folders','0.13.0 release and user-extracted copies','All diagnostic evidence and runtime logs','All DB/settings/auth files','NuGet/tooling caches','.git','External and LOCALAPPDATA paths')}
$out=Join-Path $artifacts ('cleanup-plan-'+[Guid]::NewGuid().ToString('N')+'.json');$plan|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $out -Encoding utf8
[pscustomobject]@{Plan=$out;CandidateFolders=$candidates.Count;CandidateFiles=($candidates.Files|Measure-Object).Count;CandidateBytes=($candidates|Measure-Object Bytes -Sum).Sum;PreservedArtifactEntries=$inventory.Count}|ConvertTo-Json
