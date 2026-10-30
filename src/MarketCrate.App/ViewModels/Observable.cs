using System.ComponentModel;
using MarketCrate.Core;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MarketCrate.App.ViewModels;

public abstract class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Raise([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; Raise(name); return true; }
}
public sealed class Command(Action action, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter)
    {
        if(!CanExecute(parameter))return;
        using var scope=DiagnosticHub.Operation(DiagnosticEvent.UiCommand,DiagnosticArea.UI,"G08");
        try{action();}catch(Exception e){DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.UI,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticError.Validation,exception:e);throw;}
    }
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
public sealed class AsyncCommand(Func<Task> action, Action<Exception> onError, Func<bool>? canExecute = null) : ICommand
{
    private bool running;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !running && (canExecute?.Invoke() ?? true);
    public async void Execute(object? parameter) => await ExecuteAsync(parameter);
    public async Task ExecuteAsync(object? parameter = null)
    {
        if (!CanExecute(parameter)) return;
        using var scope=DiagnosticHub.Operation(DiagnosticEvent.UiCommand,DiagnosticArea.UI,"G08");
        running = true; Refresh();
        try { await action(); }
        catch (Exception e) { DiagnosticHub.Emit(DiagnosticEvent.UiFailure,DiagnosticArea.UI,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticError.Unhandled,exception:e); onError(e); }
        finally { running = false; Refresh(); }
    }
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
