using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation.Peers;
namespace MarketCrate.App.Controls;
public sealed class BusyIndicator:Control
{
    static BusyIndicator()=>DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyIndicator),new FrameworkPropertyMetadata(typeof(BusyIndicator)));
    public static readonly DependencyProperty IsActiveProperty=DependencyProperty.Register(nameof(IsActive),typeof(bool),typeof(BusyIndicator),new PropertyMetadata(false));
    public bool IsActive{get=>(bool)GetValue(IsActiveProperty);set=>SetValue(IsActiveProperty,value);}
    protected override AutomationPeer OnCreateAutomationPeer()=>new BusyPeer(this);
    private sealed class BusyPeer(BusyIndicator owner):FrameworkElementAutomationPeer(owner)
    {protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.ProgressBar;protected override string GetClassNameCore()=>nameof(BusyIndicator);}
}
