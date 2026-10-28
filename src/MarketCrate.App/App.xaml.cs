using System.IO;
using System.Windows;
using System.Windows.Media;
using MarketCrate.App.ViewModels;
using MarketCrate.Infrastructure;

namespace MarketCrate.App;

public partial class App : Application
{
    private SqliteDemoStore? journal;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
        // No forced activation. A second instance exits without touching the first instance.
        var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarketCrate", "Demo");
        try { journal = new SqliteDemoStore(dataDirectory); }
        catch (IOException) { Shutdown(2); return; }
        catch (Exception error) when (error is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or InvalidDataException) { SafeStartupDiagnostics.StorageFailure(dataDirectory); Shutdown(3); return; }
        var vm = new MainViewModel(new DemoDataProvider(), journal);
        var window = new MainWindow { DataContext = vm };
        MainWindow = window;
        window.Closed += (_, _) => vm.Dispose();
        window.Show();
    }
    protected override void OnExit(ExitEventArgs e) { journal?.Dispose(); base.OnExit(e); }
}
