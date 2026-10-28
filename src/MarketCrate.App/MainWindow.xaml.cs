using System.Windows;
using MarketCrate.App.ViewModels;
namespace MarketCrate.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if MARKETCRATE_DEMO
        Loaded += async (_, _) => { if (DataContext is MainViewModel vm) await vm.InitializeAsync(); };
#endif
        Closing += (_, e) =>
        {
            if(DataContext is ProductionShellViewModel live&&live.Steam.Busy)e.Cancel=true;
#if MARKETCRATE_DEMO
            if(DataContext is MainViewModel vm&&(vm.Busy||vm.Steam.Busy))e.Cancel=true;
#endif
        };
    }
}
