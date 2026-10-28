using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MarketCrate.App.Controls;

[TemplatePart(Name = "PART_Down", Type = typeof(Button))]
[TemplatePart(Name = "PART_Up", Type = typeof(Button))]
public sealed class NumericStepper : Control
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(int), typeof(NumericStepper), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, Coerce));
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(NumericStepper), new PropertyMetadata(1, (d, e) => d.CoerceValue(ValueProperty)));
    public int Value { get => (int)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public int Maximum { get => (int)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    private static object Coerce(DependencyObject d, object value) => Math.Clamp((int)value, 1, Math.Max(1, ((NumericStepper)d).Maximum));
    public override void OnApplyTemplate()
    {
        if (GetTemplateChild("PART_Down") is Button oldDown) oldDown.Click -= Down;
        if (GetTemplateChild("PART_Up") is Button oldUp) oldUp.Click -= Up;
        base.OnApplyTemplate();
        if (GetTemplateChild("PART_Down") is Button down) down.Click += Down;
        if (GetTemplateChild("PART_Up") is Button up) up.Click += Up;
    }
    private void Down(object sender, RoutedEventArgs e) => SetCurrentValue(ValueProperty, Math.Max(1, Value - 1));
    private void Up(object sender, RoutedEventArgs e) => SetCurrentValue(ValueProperty, Math.Min(Maximum, Value + 1));
    protected override void OnKeyDown(KeyEventArgs e)
    { if (e.Key is Key.Up or Key.Down) { SetCurrentValue(ValueProperty, Math.Clamp(Value + (e.Key == Key.Up ? 1 : -1), 1, Math.Max(1, Maximum))); e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    { if (IsKeyboardFocusWithin) { SetCurrentValue(ValueProperty, Math.Clamp(Value + Math.Sign(e.Delta), 1, Math.Max(1, Maximum))); e.Handled = true; } base.OnMouseWheel(e); }
}
