using System.Windows;
using MarketCrate.App.ViewModels;
namespace MarketCrate.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => { if (DataContext is MainViewModel vm) await vm.InitializeAsync(); };
        Closing += (_, e) => { if (DataContext is MainViewModel { Busy: true }) e.Cancel = true; };
    }
}
