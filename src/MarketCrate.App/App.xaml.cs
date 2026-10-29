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
    private IDisposable? owner;
    private JsonDiagnosticLog? diagnostics;
    private readonly BindingDiagnostics bindingDiagnostics=new();
    protected override void OnStartup(StartupEventArgs e)
    {
        AppRunProfile profile;
        try{profile=AppRunProfile.Create(e.Args,Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Environment.GetEnvironmentVariable("MARKETCRATE_ISOLATED_QA"));}
        catch(ArgumentException){Shutdown(4);return;}
        diagnostics=new(Path.Combine(profile.DataRoot,"Logs"));DiagnosticHub.SetDefault(diagnostics);
        DiagnosticHub.Emit(DiagnosticEvent.AppStart,DiagnosticArea.Lifecycle,requirement:"LOG-01",reason:profile.OfflineQa?"IsolatedQa":"LiveSession");
        DispatcherUnhandledException+=(_,args)=>Fault(args.Exception);
        AppDomain.CurrentDomain.UnhandledException+=(_,args)=>{if(args.ExceptionObject is Exception error)Fault(error);};
        TaskScheduler.UnobservedTaskException+=(_,args)=>DiagnosticHub.Emit(DiagnosticEvent.AppFault,DiagnosticArea.Lifecycle,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticError.Unhandled,exception:args.Exception);
        PresentationTraceSources.DataBindingSource.Listeners.Clear();PresentationTraceSources.DataBindingSource.Listeners.Add(bindingDiagnostics);
        base.OnStartup(e);RenderOptions.ProcessRenderMode=System.Windows.Interop.RenderMode.SoftwareOnly;
        try
        {
            var window=new MainWindow();
#if MARKETCRATE_DEMO
            if(profile.OfflineQa)
            {
                var journal=new SqliteDemoStore(Path.Combine(profile.DataRoot,"Demo"));
                try{var vm=new MainViewModel(new DemoDataProvider(),journal,offlineOnly:true);window.Content=new MainView();window.DataContext=vm;owner=new OfflineOwner(vm,journal);}
                catch{journal.Dispose();throw;}
                window.Title="MarketCrate · ISOLATED GUI QA · Mock only / Steam disabled";
            }
            else
#endif
            {
                var vm=new ProductionShellViewModel(Path.Combine(profile.DataRoot,"Preferences"),diagnostics,Path.Combine(profile.DataRoot,"Auth"));owner=vm;window.DataContext=vm;
                window.Loaded+=async(_,_)=>{try{await vm.Steam.RestoreRememberedAsync();}catch(Exception error){Fault(error);}};
            }
            MainWindow=window;window.Closed+=(_,_)=>{owner?.Dispose();owner=null;};window.Show();
        }
        catch(IOException error){Fault(error);SafeStartupDiagnostics.StorageFailure(profile.DataRoot);Shutdown(2);}
        catch(Exception error)when(error is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or InvalidDataException){Fault(error);SafeStartupDiagnostics.StorageFailure(profile.DataRoot);Shutdown(3);}
    }
#if MARKETCRATE_DEMO
    private sealed class OfflineOwner(MainViewModel vm,SqliteDemoStore store):IDisposable{public void Dispose(){vm.Dispose();store.Dispose();}}
#endif
    private void Fault(Exception error)
    {
        DiagnosticHub.Emit(DiagnosticEvent.AppFault,DiagnosticArea.Lifecycle,DiagnosticOutcome.Failed,LogLevel.Critical,DiagnosticError.Unhandled,exception:error);
        try{diagnostics?.FlushAsync().Wait(TimeSpan.FromMilliseconds(500));}catch{ }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        try{owner?.Dispose();DiagnosticHub.Emit(DiagnosticEvent.ResourceCleanup,DiagnosticArea.Lifecycle);}
        finally{DiagnosticHub.Emit(DiagnosticEvent.AppStop,DiagnosticArea.Lifecycle);PresentationTraceSources.DataBindingSource.Listeners.Remove(bindingDiagnostics);diagnostics?.Dispose();DiagnosticHub.SetDefault(null);base.OnExit(e);}
    }
}
