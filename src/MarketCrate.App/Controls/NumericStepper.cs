using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace MarketCrate.App.Controls;

[TemplatePart(Name = "PART_Down", Type = typeof(Button))]
[TemplatePart(Name = "PART_Up", Type = typeof(Button))]
public sealed class NumericStepper : Control
{
    private Button? downPart, upPart;
    protected override AutomationPeer OnCreateAutomationPeer() => new StepperPeer(this);
    private sealed class StepperPeer(NumericStepper owner) : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        protected override string GetClassNameCore() => nameof(NumericStepper);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Spinner;
        protected override string GetNameCore() => string.IsNullOrWhiteSpace(base.GetNameCore()) ? "선택 수량" : base.GetNameCore();
        public override object? GetPattern(PatternInterface patternInterface) => patternInterface == PatternInterface.RangeValue ? this : base.GetPattern(patternInterface);
        public bool IsReadOnly => !owner.IsEnabled; public double LargeChange => 10; public double SmallChange => 1;
        public double Minimum => 1; public double Maximum => owner.Maximum; public double Value => owner.Value;
        public void SetValue(double value)
        {
            if (IsReadOnly) throw new ElementNotEnabledException();
            if (!double.IsFinite(value) || value < 1 || value > owner.Maximum || value != Math.Truncate(value)) throw new ArgumentOutOfRangeException(nameof(value));
            owner.SetUserValue((int)value);
        }
    }
    private bool editingText;
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(int), typeof(NumericStepper), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d,e)=>((NumericStepper)d).SyncText(), Coerce));
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(NumericStepper), new PropertyMetadata(1, (d, e) => {d.CoerceValue(ValueProperty);((NumericStepper)d).ValidateText();}), value => (int)value >= 1);
    public static readonly DependencyProperty InputTextProperty = DependencyProperty.Register(nameof(InputText),typeof(string),typeof(NumericStepper),new FrameworkPropertyMetadata("1",FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,(d,e)=>((NumericStepper)d).ValidateText()));
    private static readonly DependencyPropertyKey IsInputValidPropertyKey=DependencyProperty.RegisterReadOnly(nameof(IsInputValid),typeof(bool),typeof(NumericStepper),new PropertyMetadata(true));
    public static readonly DependencyProperty IsInputValidProperty=IsInputValidPropertyKey.DependencyProperty;
    public string InputText {get=>(string)GetValue(InputTextProperty);set=>SetValue(InputTextProperty,value);}
    public bool IsInputValid=>(bool)GetValue(IsInputValidProperty);
    private void SyncText(){if(!editingText&&!System.Windows.Data.BindingOperations.IsDataBound(this,InputTextProperty))SetCurrentValue(InputTextProperty,Value.ToString(System.Globalization.CultureInfo.InvariantCulture));}
    private void ValidateText()
    {
        var valid=int.TryParse(InputText,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var value)&&value>=1&&value<=Maximum;
        SetValue(IsInputValidPropertyKey,valid);
        if(!valid)return;
        editingText=true;try{SetCurrentValue(ValueProperty,value);}finally{editingText=false;}
    }
    public int Value { get => (int)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public int Maximum { get => (int)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    private static object Coerce(DependencyObject d, object value) => Math.Clamp((int)value, 1, Math.Max(1, ((NumericStepper)d).Maximum));
    public override void OnApplyTemplate()
    {
        if (downPart != null) downPart.Click -= Down;
        if (upPart != null) upPart.Click -= Up;
        base.OnApplyTemplate();
        downPart = GetTemplateChild("PART_Down") as Button; upPart = GetTemplateChild("PART_Up") as Button;
        if (downPart != null) downPart.Click += Down;
        if (upPart != null) upPart.Click += Up;
    }
    private void SetUserValue(int value){SetCurrentValue(ValueProperty,value);SetCurrentValue(InputTextProperty,Value.ToString(System.Globalization.CultureInfo.InvariantCulture));}
    private void Step(int delta) {if(IsEnabled)SetUserValue((int)Math.Clamp((long)Value + delta, 1, Maximum));}
    private void Down(object sender, RoutedEventArgs e) => Step(-1);
    private void Up(object sender, RoutedEventArgs e) => Step(1);
    protected override void OnKeyDown(KeyEventArgs e)
    { if (e.Key is Key.Up or Key.Down) { Step(e.Key == Key.Up ? 1 : -1); e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    { if (IsKeyboardFocusWithin) { Step(Math.Sign(e.Delta)); e.Handled = true; } base.OnMouseWheel(e); }
}
