using System.ComponentModel;
using MarketCrate.Core;
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
        WorkspaceTabs.SelectionChanged+=(_,args)=>{if(args.OriginalSource==WorkspaceTabs)DiagnosticHub.Emit(DiagnosticEvent.UiPage,DiagnosticArea.UI,requirement:"G08",count:WorkspaceTabs.SelectedIndex);};
        ReviewOverlay.MouseDown += (_, e) =>
        {
            if (e.OriginalSource == ReviewOverlay && DataContext is MainViewModel vm && vm.CloseReview.CanExecute(null)) vm.CloseReview.Execute(null);
        };
        ManagementOverlay.MouseDown += (_, e) => { if (e.OriginalSource == ManagementOverlay && DataContext is MainViewModel vm && vm.CloseCancelReview.CanExecute(null)) vm.CloseCancelReview.Execute(null); };
        AddHandler(Validation.ErrorEvent, new EventHandler<ValidationErrorEventArgs>(InputError));
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is MainViewModel old) old.PropertyChanged -= ReviewChanged;
            if (e.NewValue is MainViewModel current) current.PropertyChanged += ReviewChanged;
        };
    }
    private void InputError(object? sender, ValidationErrorEventArgs e)
    {
        if (e.Action == ValidationErrorEventAction.Added) {DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.UI,DiagnosticOutcome.Failed,LogLevel.Warning,DiagnosticError.Validation,reason:"InputRejected");invalidInputs.Add(e.OriginalSource);} else invalidInputs.Remove(e.OriginalSource);
        if (DataContext is MainViewModel vm) vm.HasInputErrors = invalidInputs.Count > 0;
    }
    private void ReviewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(MainViewModel.IsReviewOpen) or nameof(MainViewModel.IsCancelReviewOpen)) || sender is not MainViewModel vm) return;
        WorkspaceBody.IsEnabled = !vm.AnyDialog;
        invalidInputs.RemoveWhere(x => x is System.Windows.Media.Visual visual && ((!vm.IsReviewOpen && ReviewOverlay.IsAncestorOf(visual)) || (!vm.IsCancelReviewOpen && ManagementOverlay.IsAncestorOf(visual))));
        vm.HasInputErrors = invalidInputs.Count > 0;

    }
    private void ReviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is MainViewModel vm)
        { var command = vm.IsCancelReviewOpen ? vm.CloseCancelReview : vm.CloseReview; if (command.CanExecute(null)) command.Execute(null); e.Handled = true; }
    }
    private void SteamDiagnosticsClicked(object sender,RoutedEventArgs e){if(DataContext is MainViewModel vm){vm.Diagnostics.OperationFilter=vm.Steam.LastOperationId?.ToString()??"";vm.Diagnostics.Refresh.Execute(null);WorkspaceTabs.SelectedIndex=5;}}
    private void SubmitClicked(object sender, RoutedEventArgs e) => WorkspaceTabs.SelectedIndex = 2;
}
