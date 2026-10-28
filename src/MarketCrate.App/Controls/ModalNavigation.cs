using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MarketCrate.Core;

namespace MarketCrate.App.Controls;

// App-local focus only, never activates a Window. Detached tests never receive focus.
public static class ModalNavigation
{
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.RegisterAttached(
        "IsOpen", typeof(bool), typeof(ModalNavigation), new PropertyMetadata(false, Changed));
    private static readonly DependencyProperty ReturnTargetProperty = DependencyProperty.RegisterAttached(
        "ReturnTarget", typeof(WeakReference<UIElement>), typeof(ModalNavigation));
    public static bool GetIsOpen(DependencyObject target) => (bool)target.GetValue(IsOpenProperty);
    public static void SetIsOpen(DependencyObject target, bool value) => target.SetValue(IsOpenProperty, value);
    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not FrameworkElement modal) return;
        var open = (bool)args.NewValue;
        using var operation = DiagnosticHub.Operation(DiagnosticEvent.Modal, DiagnosticArea.UI, "G08");
        DiagnosticHub.Emit(DiagnosticEvent.Modal, DiagnosticArea.UI, reason: open ? "Opened" : "Closed");
        var operationId = DiagnosticHub.Context?.OperationId;
        var window = Window.GetWindow(modal);
        if (open && window?.IsActive == true && Keyboard.FocusedElement is UIElement prior && window.IsAncestorOf(prior))
            modal.SetValue(ReturnTargetProperty, new WeakReference<UIElement>(prior));
        modal.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            using var scope = DiagnosticHub.Scope(operation: operationId);
            if (GetIsOpen(modal) != open) return;
            // A detached, inactive or unloaded view must never change desktop focus.
            if (window?.IsActive != true || !modal.IsLoaded)
            { DiagnosticHub.Emit(DiagnosticEvent.Modal, DiagnosticArea.UI, level: LogLevel.Debug, reason: "FocusDeferred"); return; }
            if (open && !modal.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)))
                DiagnosticHub.Emit(DiagnosticEvent.Modal, DiagnosticArea.UI, DiagnosticOutcome.Failed, LogLevel.Warning, DiagnosticError.Validation, reason: "FocusRejected");
            if (!open && modal.GetValue(ReturnTargetProperty) is WeakReference<UIElement> reference && reference.TryGetTarget(out var prior)
                && prior.IsEnabled && prior.IsVisible && window.IsAncestorOf(prior)) prior.Focus();
            if (!open) modal.ClearValue(ReturnTargetProperty);
        }));
    }
}
