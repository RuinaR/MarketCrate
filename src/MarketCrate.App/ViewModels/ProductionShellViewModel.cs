using MarketCrate.Core;
using MarketCrate.Infrastructure;
using MarketCrate.Infrastructure.Diagnostics;
namespace MarketCrate.App.ViewModels;

public sealed class ProductionShellViewModel:Observable,IDisposable
{
    private readonly UserPreferencesStore preferences;
    private bool reducedMotion;
    private int page;
    public SteamWorkspaceViewModel Steam{get;}
    public DiagnosticsViewModel Diagnostics{get;}
    public int Page{get=>page;set{if(value is >=0 and <=1&&Set(ref page,value)){Raise(nameof(ShowSteam));Raise(nameof(DiagnosticsOpen));Raise(nameof(DiagnosticsButtonText));DiagnosticHub.Emit(DiagnosticEvent.UiPage,DiagnosticArea.UI,requirement:"G08",reason:"LiveSession",count:value);}}}
    public bool ShowSteam=>Page==0;
    public bool DiagnosticsOpen=>Page==1;
    public string DiagnosticsButtonText=>DiagnosticsOpen?"장터 관리로 돌아가기":"문제 해결";
    public bool ReducedMotion{get=>reducedMotion;set{if(Set(ref reducedMotion,value))preferences.Save("reduced-motion",value);}}
    public Command ShowDiagnostics{get;}
    public ProductionShellViewModel(string preferencesDirectory,JsonDiagnosticLog? log=null,string? authDirectory=null)
    {
        preferences=new(preferencesDirectory);
        try
        {
            Steam=new(authDirectory:authDirectory);Diagnostics=new(log,saveMode:value=>preferences.Save("diagnostic-mode",value));
            reducedMotion=preferences.Read("reduced-motion",false);if(preferences.Read("diagnostic-mode"))Diagnostics.DiagnosticMode=true;
            ShowDiagnostics=new(()=>{if(Page==1){Page=0;return;}Diagnostics.OperationFilter=Steam.LastOperationId?.ToString()??"";Diagnostics.Refresh.Execute(null);Page=1;});
        }
        catch{preferences.Dispose();throw;}
    }
    public void Dispose(){Steam.Dispose();preferences.Dispose();}
}
