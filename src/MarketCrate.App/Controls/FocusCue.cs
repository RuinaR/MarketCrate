using System.Windows;
using System.Windows.Input;

namespace MarketCrate.App.Controls;

// Input modality is observed only on this app's routed subtree. No global hooks or input polling.
public static class FocusCue
{
    public static readonly DependencyProperty EnabledProperty=DependencyProperty.RegisterAttached("Enabled",typeof(bool),typeof(FocusCue),new PropertyMetadata(false,Changed));
    public static readonly DependencyProperty KeyboardModeProperty=DependencyProperty.RegisterAttached("KeyboardMode",typeof(bool),typeof(FocusCue),new FrameworkPropertyMetadata(true,FrameworkPropertyMetadataOptions.Inherits));
    public static bool GetEnabled(DependencyObject target)=>(bool)target.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject target,bool value)=>target.SetValue(EnabledProperty,value);
    public static bool GetKeyboardMode(DependencyObject target)=>(bool)target.GetValue(KeyboardModeProperty);
    public static void SetKeyboardMode(DependencyObject target,bool value)=>target.SetValue(KeyboardModeProperty,value);
    private static void Changed(DependencyObject target,DependencyPropertyChangedEventArgs args)
    {
        if(target is not UIElement root)return;
        if((bool)args.OldValue){root.PreviewKeyDown-=Key;root.PreviewMouseDown-=Pointer;}
        if((bool)args.NewValue){root.PreviewKeyDown+=Key;root.PreviewMouseDown+=Pointer;}
    }
    private static void Key(object sender,KeyEventArgs args)=>SetKeyboardMode((DependencyObject)sender,true);
    private static void Pointer(object sender,MouseButtonEventArgs args)=>SetKeyboardMode((DependencyObject)sender,false);
}
