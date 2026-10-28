using System.Windows;

namespace MarketCrate.App.Controls;

public static class Motion
{
    public static readonly DependencyProperty ReducedProperty = DependencyProperty.RegisterAttached("Reduced", typeof(bool), typeof(Motion),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, null, (_, value) => (bool)value || !SystemParameters.ClientAreaAnimation));
    public static bool GetReduced(DependencyObject target) => (bool)target.GetValue(ReducedProperty);
    public static void SetReduced(DependencyObject target, bool value) => target.SetValue(ReducedProperty, value);
}
