# Metadata/resources only. Never loads an application assembly into a running WPF app.
param([Parameter(Mandatory)][string]$Directory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Reflection.Metadata
function Inspect([string]$Name) {
    $stream=[IO.File]::OpenRead((Join-Path $Directory $Name));$pe=[System.Reflection.PortableExecutable.PEReader]::new($stream)
    try {
        $metadata=[System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe);$types=@();$retry=$false;$resources=@()
        foreach($handle in $metadata.TypeDefinitions){$type=$metadata.GetTypeDefinition($handle);$name=$metadata.GetString($type.Name);$types+=$name;if($name-eq'SaleBatchCoordinator'){foreach($method in $type.GetMethods()){if($metadata.GetString($metadata.GetMethodDefinition($method).Name)-eq'RetryFailedAsync'){$retry=$true}}}}
        foreach($handle in $metadata.ManifestResources){$resource=$metadata.GetManifestResource($handle);if($metadata.GetString($resource.Name).EndsWith('.g.resources')){$bytes=[byte[]]$pe.GetSectionData($pe.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress).GetContent();$at=[int]$resource.Offset;$length=[BitConverter]::ToInt32($bytes,$at);$memory=[IO.MemoryStream]::new($bytes,$at+4,$length,$false);$reader=[Resources.ResourceReader]::new($memory);try{$entries=$reader.GetEnumerator();while($entries.MoveNext()){$resources+=[string]$entries.Key}}finally{$reader.Dispose();$memory.Dispose()}}}
        foreach($handle in $metadata.TypeDefinitions){$type=$metadata.GetTypeDefinition($handle);if($metadata.GetString($type.Name)-eq'SteamListingExecutor'){foreach($method in $type.GetMethods()){if($metadata.GetString($metadata.GetMethodDefinition($method).Name)-eq'Wallet'){throw 'Legacy USD fixture wallet parser leaked'}}}}
        return [pscustomobject]@{Types=$types;MockRetry=$retry;Resources=$resources}
    }finally{$pe.Dispose();$stream.Dispose()}
}
$app=Inspect 'MarketCrate.App.dll';$core=Inspect 'MarketCrate.Core.dll';$infrastructure=Inspect 'MarketCrate.Infrastructure.dll'
$forbidden=@('DemoDataProvider','MockListingExecutor','BlockedListingExecutor','SqliteDemoStore','FileBatchJournal','MockCancellation','MockListingManagement','MockOrderProjection','MockListing','IDemoJournal','IDemoDataStore','MainView','MainViewModel','ItemViewModel','ManagementRowViewModel','OfflineOwner','DevelopmentReadCheckView','ReadSelfCheckRow','SteamReadSelfCheck','SteamReadCheckPlan','SteamReadCheckResult','BulkRepriceContractServer','UsabilityTests','UsabilityUiTests')
foreach($name in $forbidden){if($name-in@($app.Types+$core.Types+$infrastructure.Types)){throw "Development implementation leaked: $name"}}
if($core.MockRetry){throw 'Mock-only retry behavior leaked'}
foreach($name in @('ProductionShellView','ProductionShellViewModel','SteamWorkspaceViewModel','DiagnosticsViewModel','BusyIndicator')){if($name-notin$app.Types){throw "Production UI missing: $name"}}
if('developmentreadcheckview.baml'-in$app.Resources-or'mainview.baml'-in$app.Resources-or'productionshellview.baml'-notin$app.Resources){throw 'Production/development WPF resource mismatch'}
if('SteamRelistPlan'-notin$infrastructure.Types-or'FeeContractResolver'-notin$infrastructure.Types-or'SteamAuthStore'-notin$infrastructure.Types-or'SteamListingExecutor'-notin$infrastructure.Types-or'UserPreferencesStore'-notin$infrastructure.Types){throw 'Actual services missing'}
Write-Output 'PASS production metadata: no Demo/Mock implementations, offline owner, legacy/self-check UI/BAML or Mock retry; actual Steam shell/services retained. Binaries NOT executed.'
