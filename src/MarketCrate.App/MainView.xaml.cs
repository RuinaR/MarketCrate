using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MarketCrate.App.ViewModels;

namespace MarketCrate.App;

public partial class MainView : UserControl
{
    private readonly HashSet<object> invalidInputs = [];
    public MainView()
    {
        InitializeComponent();
        ReviewOverlay.MouseDown += (_, e) =>
        {
            if (e.OriginalSource == ReviewOverlay && DataContext is MainViewModel vm && vm.CloseReview.CanExecute(null)) vm.CloseReview.Execute(null);
        };
        AddHandler(Validation.ErrorEvent, new EventHandler<ValidationErrorEventArgs>(InputError));
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is MainViewModel old) old.PropertyChanged -= ReviewChanged;
            if (e.NewValue is MainViewModel current) current.PropertyChanged += ReviewChanged;
        };
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
        if (!vm.IsReviewOpen)
        {
            invalidInputs.RemoveWhere(x => x is System.Windows.Media.Visual visual && ReviewOverlay.IsAncestorOf(visual));
            vm.HasInputErrors = invalidInputs.Count > 0;
        }
        // No focus operation in detached tests and no window activation.
        if (Window.GetWindow(this)?.IsActive == true)
        { if (vm.IsReviewOpen) ReviewList.Focus(); else ReviewButton.Focus(); }
    }
    private void ReviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is MainViewModel vm)
        { var command = vm.IsCancelReviewOpen ? vm.CloseCancelReview : vm.CloseReview; if (command.CanExecute(null)) command.Execute(null); e.Handled = true; }
    }
    private void SubmitClicked(object sender, RoutedEventArgs e) => WorkspaceTabs.SelectedIndex = 2;
}
