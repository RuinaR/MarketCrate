# Pure owned-file tests; never runs an executable or reads user-profile/process data.
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ArtifactMaintenance.ps1')
$passed=0;$failed=0;$notVerified=0
function Case([string]$Name,[scriptblock]$Run){try{&$Run;$script:passed++;Write-Output "PASS $Name"}catch{$script:failed++;Write-Output "FAIL $Name : $($_.Exception.Message)"}}
function Reject([scriptblock]$Run){$denied=$false;try{&$Run|Out-Null}catch{$denied=$true};if(-not$denied){throw 'Unproven target accepted'}}
$folder=Join-Path $script:ArtifactProjectRoot ('runtime/distribution-tests-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path (Join-Path $folder licenses)|Out-Null
$names=@('MarketCrate.App.exe','MarketCrate.App.dll','MarketCrate.Core.dll','MarketCrate.Infrastructure.dll','PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll','System.Private.CoreLib.dll','coreclr.dll','clrjit.dll','hostfxr.dll','hostpolicy.dll','PresentationNative_cor3.dll','wpfgfx_cor3.dll','e_sqlite3.dll')
$pe=[byte[]]::new(256);$pe[0]=77;$pe[1]=90;$pe[60]=64;$pe[64]=80;$pe[65]=69;$pe[68]=100;$pe[69]=134
foreach($name in $names){[IO.File]::WriteAllBytes((Join-Path $folder $name),$pe)}
foreach($name in @('THIRD-PARTY-NOTICES.md','licenses/QrCodeGenerator-LICENSE.txt','licenses/NETCore-LICENSE.txt','licenses/NETCore-THIRD-PARTY-NOTICES.txt','licenses/WindowsDesktop-LICENSE.txt')){Set-Content -LiteralPath (Join-Path $folder $name) -Value 'synthetic notice'}
Set-Content -LiteralPath (Join-Path $folder 'MarketCrate.App.runtimeconfig.json') -Value '{"runtimeOptions":{"includedFrameworks":[{"name":"Microsoft.NETCore.App","version":"10.0.9"},{"name":"Microsoft.WindowsDesktop.App","version":"10.0.9"}]}}'
Set-Content -LiteralPath (Join-Path $folder 'MarketCrate.App.deps.json') -Value '{"runtimeTarget":{"name":".NETCoreApp,Version=v10.0/win-x64"}}'
Case 'Exact non-executable PE fixture proven' {$proof=Get-ProvenFixture $folder;if($proof.Files.Count-ne22){throw 'Wrong file set'}}
Case 'Dry validation never mutates files' {$before=Get-FileHash (Join-Path $folder 'MarketCrate.App.exe');$null=Get-ProvenFixture $folder;if((Get-FileHash $before.Path).Hash-ne$before.Hash){throw 'Dry-run modified file'}}
Case 'Boundary and nonfixture directory rejected' {Reject {Get-ProvenFixture (Join-Path $script:ArtifactProjectRoot 'artifacts')};Reject {Assert-OwnedPath (Join-Path $script:ArtifactProjectRoot 'README.md') (Join-Path $script:ArtifactProjectRoot 'runtime')}}
Case 'Tampered PE content preserved' {$path=Join-Path $folder 'MarketCrate.App.exe';$changed=$pe.Clone();$changed[200]=1;[IO.File]::WriteAllBytes($path,$changed);Reject {Remove-ProvenFixture $folder};if(-not(Test-Path $path)){throw 'Tampered file removed'};[IO.File]::WriteAllBytes($path,$pe)}
Case 'Unexpected DB sentinel preserves entire fixture' {$sentinel=Join-Path $folder 'preserve.db';Set-Content -LiteralPath $sentinel -Value 'owned-test-only';Reject {Remove-ProvenFixture $folder};if((Get-Content $sentinel)-ne'owned-test-only'){throw 'Sentinel changed'};Remove-Item -LiteralPath $sentinel}
Case 'Locked file causes no deletion' {$path=Join-Path $folder 'MarketCrate.App.exe';$lock=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None);try{Reject {Remove-ProvenFixture $folder}}finally{$lock.Dispose()};if(@(Get-ChildItem $folder -File).Count-ne18){throw 'Partially removed locked target'}}
$target=Join-Path $script:ArtifactProjectRoot ('runtime/cleanup-link-test-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $target|Out-Null;Set-Content -LiteralPath (Join-Path $target 'sentinel.txt') -Value 'owned-junction-target'
$link=Join-Path $folder 'foreign'
try{
    try{New-Item -ItemType Junction -Path $link -Target $target -ErrorAction Stop|Out-Null;$made=$true}catch{$made=$false;$notVerified++;Write-Output 'NOT VERIFIED junction creation unavailable; no escalation/settings change'}
    if($made){Case 'Junction boundary refused and target preserved' {Reject {Remove-ProvenFixture $folder};if((Get-Content (Join-Path $target 'sentinel.txt'))-ne'owned-junction-target'){throw 'Target touched'}}}
}finally{if(Test-Path -LiteralPath $link){[IO.Directory]::Delete($link)};Remove-Item -LiteralPath (Join-Path $target 'sentinel.txt');Remove-Item -LiteralPath $target}
Case 'Modern crypto fixture requires both exact stub and synthetic license' {
    $crypto=Join-Path $folder 'System.Security.Cryptography.ProtectedData.dll';$notice=Join-Path $folder 'licenses/steam-session-LICENSE.txt'
    [IO.File]::WriteAllBytes($crypto,$pe);Reject {Get-ProvenFixture $folder};Set-Content -LiteralPath $notice -Value 'synthetic notice'
    if((Get-ProvenFixture $folder).Files.Count-ne24){throw 'Modern exact fixture not recognized'}
    Remove-Item -LiteralPath $crypto;Remove-Item -LiteralPath $notice
}
Case 'Proven owned fixture removed without recursive delete' {$removed=Remove-ProvenFixture $folder;if($removed.Files.Count-ne22-or(Test-Path -LiteralPath $folder)){throw 'Incomplete removal'}}
Write-Output "ARTIFACT CLEANUP RESULT $passed PASS / $failed FAIL / $notVerified NOT VERIFIED; synthetic owned files only"
if($failed){exit 1}
