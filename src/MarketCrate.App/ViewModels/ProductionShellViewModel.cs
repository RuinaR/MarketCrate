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
    public int Page{get=>page;set{if(Set(ref page,value))DiagnosticHub.Emit(DiagnosticEvent.UiPage,DiagnosticArea.UI,requirement:"G08",reason:"LiveSession",count:value);}}
    public bool ReducedMotion{get=>reducedMotion;set{if(Set(ref reducedMotion,value))preferences.Save("reduced-motion",value);}}
    public Command ShowDiagnostics{get;}
    public ProductionShellViewModel(string preferencesDirectory,JsonDiagnosticLog? log=null,string? authDirectory=null)
    {
        preferences=new(preferencesDirectory);
        try
        {
            Steam=new(authDirectory:authDirectory);Diagnostics=new(log,saveMode:value=>preferences.Save("diagnostic-mode",value));
            reducedMotion=preferences.Read("reduced-motion",false);if(preferences.Read("diagnostic-mode"))Diagnostics.DiagnosticMode=true;
            ShowDiagnostics=new(()=>{Diagnostics.OperationFilter=Steam.LastOperationId?.ToString()??"";Diagnostics.Refresh.Execute(null);Page=1;});
        }
        catch{preferences.Dispose();throw;}
    }
    public void Dispose(){Steam.Dispose();preferences.Dispose();}
}
