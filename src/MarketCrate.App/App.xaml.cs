using System.IO;
using System.Diagnostics;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;
using System.Windows;
using System.Windows.Media;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure;

namespace MarketCrate.App;

public partial class App : Application
{
    private SqliteDemoStore? journal;
    private JsonDiagnosticLog? diagnostics;
    private readonly BindingDiagnostics bindingDiagnostics=new();
    protected override void OnStartup(StartupEventArgs e)
    {
        diagnostics=new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MarketCrate","Logs"));
        DiagnosticHub.SetDefault(diagnostics);
        DiagnosticHub.Emit(DiagnosticEvent.AppStart,DiagnosticArea.Lifecycle,requirement:"LOG-01");
        DispatcherUnhandledException+=(_,args)=>Fault(args.Exception);
        AppDomain.CurrentDomain.UnhandledException+=(_,args)=>{if(args.ExceptionObject is Exception error)Fault(error);};
        TaskScheduler.UnobservedTaskException+=(_,args)=>{DiagnosticHub.Emit(DiagnosticEvent.AppFault,DiagnosticArea.Lifecycle,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticError.Unhandled,exception:args.Exception);};
        // Replace app-local default binding listeners: their free text may contain bound user values.
        PresentationTraceSources.DataBindingSource.Listeners.Clear();
        PresentationTraceSources.DataBindingSource.Listeners.Add(bindingDiagnostics);
        base.OnStartup(e);
        RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
        // No forced activation. A second instance exits without touching the first instance.
        var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarketCrate", "Demo");
        try { journal = new SqliteDemoStore(dataDirectory); }
        catch (IOException error) { Fault(error); Shutdown(2); return; }
        catch (Exception error) when (error is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or InvalidDataException) { Fault(error); SafeStartupDiagnostics.StorageFailure(dataDirectory); Shutdown(3); return; }
        var vm = new MainViewModel(new DemoDataProvider(), journal);
        var window = new MainWindow { DataContext = vm };
        MainWindow = window;
        window.Closed += (_, _) => vm.Dispose();
        window.Show();
    }
    private void Fault(Exception error)
    {
        DiagnosticHub.Emit(DiagnosticEvent.AppFault,DiagnosticArea.Lifecycle,DiagnosticOutcome.Failed,LogLevel.Critical,DiagnosticError.Unhandled,exception:error);
        // No exception message/body/dump. Never mark fatal dispatcher exceptions handled and continue trading.
        try{diagnostics?.FlushAsync().Wait(TimeSpan.FromMilliseconds(500));}catch{ }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        try{journal?.Dispose();DiagnosticHub.Emit(DiagnosticEvent.ResourceCleanup,DiagnosticArea.Lifecycle);}
        finally{DiagnosticHub.Emit(DiagnosticEvent.AppStop,DiagnosticArea.Lifecycle);PresentationTraceSources.DataBindingSource.Listeners.Remove(bindingDiagnostics);diagnostics?.Dispose();DiagnosticHub.SetDefault(null);base.OnExit(e);}
    }
}
