# Project-owned artifacts only. Never enumerates processes or user-profile data.
$script:ArtifactProjectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
function Assert-OwnedPath([string]$Path,[string]$Base) {
    $baseFull=[IO.Path]::GetFullPath($Base).TrimEnd('\','/');$full=[IO.Path]::GetFullPath($Path)
    if(-not $full.StartsWith($baseFull+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Path outside owned boundary'}
    if((Get-Item -LiteralPath $baseFull -Force).Attributes-band[IO.FileAttributes]::ReparsePoint){throw 'Base reparse point refused'}
    $relative=$full.Substring($baseFull.Length+1);if($relative.Contains(':')){throw 'Alternate stream refused'};$cursor=$baseFull
    foreach($part in $relative.Split([IO.Path]::DirectorySeparatorChar)){$cursor=Join-Path $cursor $part;if((Get-Item -LiteralPath $cursor -Force).Attributes-band[IO.FileAttributes]::ReparsePoint){throw 'Reparse point refused'}}
    return $full
}
function Get-ProvenFixture([string]$Path) {
    $base=Join-Path $script:ArtifactProjectRoot 'runtime';$full=Assert-OwnedPath $Path $base
    if((Split-Path $full -Leaf)-notmatch'^distribution-tests-[a-f0-9]{32}$'-or(Split-Path $full -Parent)-ne$base){throw 'Not a distribution fixture'}
    $pe=[byte[]]::new(256);$pe[0]=77;$pe[1]=90;$pe[60]=64;$pe[64]=80;$pe[65]=69;[BitConverter]::GetBytes([uint16]0x8664).CopyTo($pe,68);$peHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($pe))
    $names=@('MarketCrate.App.exe','MarketCrate.App.dll','MarketCrate.Core.dll','MarketCrate.Infrastructure.dll','PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll','System.Private.CoreLib.dll','coreclr.dll','clrjit.dll','hostfxr.dll','hostpolicy.dll','PresentationNative_cor3.dll','wpfgfx_cor3.dll','e_sqlite3.dll')
    $text=@{'THIRD-PARTY-NOTICES.md'='synthetic notice';'licenses/QrCodeGenerator-LICENSE.txt'='synthetic notice';'licenses/NETCore-LICENSE.txt'='synthetic notice';'licenses/NETCore-THIRD-PARTY-NOTICES.txt'='synthetic notice';'licenses/WindowsDesktop-LICENSE.txt'='synthetic notice';'MarketCrate.App.runtimeconfig.json'='{"runtimeOptions":{"includedFrameworks":[{"name":"Microsoft.NETCore.App","version":"10.0.9"},{"name":"Microsoft.WindowsDesktop.App","version":"10.0.9"}]}}';'MarketCrate.App.deps.json'='{"runtimeTarget":{"name":".NETCoreApp,Version=v10.0/win-x64"}}'}
    $hasCrypto=Test-Path -LiteralPath (Join-Path $full 'System.Security.Cryptography.ProtectedData.dll');$hasAuthNotice=Test-Path -LiteralPath (Join-Path $full 'licenses/steam-session-LICENSE.txt')
    if($hasCrypto-ne$hasAuthNotice){throw 'Incomplete modern fixture'}
    if($hasCrypto){$names+='System.Security.Cryptography.ProtectedData.dll';$text['licenses/steam-session-LICENSE.txt']='synthetic notice'}
    $items=@(Get-ChildItem -LiteralPath $full -Force);foreach($item in $items){$null=Assert-OwnedPath $item.FullName $base;if($item.PSIsContainer-and$item.Name-ne'licenses'){throw 'Unknown fixture subdirectory'}}
    $license=Assert-OwnedPath (Join-Path $full 'licenses') $base;$all=@($items|Where-Object{-not$_.PSIsContainer})+@(Get-ChildItem -LiteralPath $license -Force)
    if($all.Count-ne($names.Count+$text.Count)){throw 'Unexpected fixture file count'}
    $files=@();foreach($file in $all){$null=Assert-OwnedPath $file.FullName $base;if($file.PSIsContainer){throw 'Nested fixture directory'};$relative=[IO.Path]::GetRelativePath($full,$file.FullName).Replace('\','/');$hash=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        if($relative-in$names){if($hash-ne$peHash){throw 'Not an exact synthetic PE stub'}}elseif($text.ContainsKey($relative)){if([IO.File]::ReadAllText($file.FullName).Trim()-ne$text[$relative]){throw 'Not exact synthetic text'}}else{throw 'Unknown fixture content'}
        $files+= [pscustomobject]@{Path=$file.FullName;Sha256=$hash;Bytes=$file.Length}
    }
    return [pscustomobject]@{Path=$full;Kind='SyntheticDistributionFixture';Bytes=($files|Measure-Object Bytes -Sum).Sum;Files=$files;Reason='Exact script-produced non-executable PE256/text fixtures; no DB/log/auth files'}
}
function Remove-ProvenFixture([string]$Path) {
    $fixture=Get-ProvenFixture $Path;$streams=@();try{
        # Hold every file against new readers/writers and re-hash under the lock. No PID inspection.
        foreach($file in $fixture.Files){$stream=[IO.File]::Open($file.Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Delete);$streams+=$stream;$hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream));if($hash-ne$file.Sha256){throw 'Fixture changed during cleanup'}}
        foreach($file in $fixture.Files){$null=Assert-OwnedPath $file.Path (Join-Path $script:ArtifactProjectRoot 'runtime');Remove-Item -LiteralPath $file.Path -Force -ErrorAction Stop}
    }finally{foreach($stream in $streams){$stream.Dispose()}}
    # Non-recursive: new/unknown files cause preservation, not broad deletion.
    $license=Join-Path $fixture.Path 'licenses';if(@(Get-ChildItem -LiteralPath $license -Force).Count-eq0){Remove-Item -LiteralPath $license -ErrorAction Stop}
    if(@(Get-ChildItem -LiteralPath $fixture.Path -Force).Count-eq0){Remove-Item -LiteralPath $fixture.Path -ErrorAction Stop}
    return $fixture
}
