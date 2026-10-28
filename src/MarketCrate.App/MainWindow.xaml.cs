using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using MarketCrate.App.ViewModels;

namespace MarketCrate.App;

public partial class MainWindow : Window
{
    private readonly HashSet<object> invalidInputs = [];
    public MainWindow()
    {
        InitializeComponent();
        ReviewOverlay.MouseDown += (_, e) =>
        {
            if (e.OriginalSource == ReviewOverlay && DataContext is MainViewModel vm && vm.CloseReview.CanExecute(null)) vm.CloseReview.Execute(null);
        };
        AddHandler(Validation.ErrorEvent, new EventHandler<ValidationErrorEventArgs>(InputError));
        Loaded += async (_, _) =>
        {
            if (DataContext is not MainViewModel vm) return;
            vm.PropertyChanged += ReviewChanged;
            await vm.InitializeAsync();
        };
        Closing += (_, e) => { if (DataContext is MainViewModel { Busy: true }) e.Cancel = true; };
    }
    private void InputError(object? sender, ValidationErrorEventArgs e)
    {
        if (e.Action == ValidationErrorEventAction.Added) invalidInputs.Add(e.OriginalSource); else invalidInputs.Remove(e.OriginalSource);
        if (DataContext is MainViewModel vm) vm.HasInputErrors = invalidInputs.Count > 0;
    }
    private void ReviewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.IsReviewOpen) || sender is not MainViewModel vm) return;
        WorkspaceTabs.IsEnabled = !vm.IsReviewOpen;
        // Focus is moved only inside an already active app window, never to another app.
        if (IsActive) { if (vm.IsReviewOpen) ReviewList.Focus(); else ReviewButton.Focus(); }
    }
    private void ReviewKeyDown(object sender, KeyEventArgs e)
    { if (e.Key == Key.Escape && DataContext is MainViewModel vm && vm.CloseReview.CanExecute(null)) { vm.CloseReview.Execute(null); e.Handled = true; } }
    private void SubmitClicked(object sender, RoutedEventArgs e) => WorkspaceTabs.SelectedIndex = 2;
}
