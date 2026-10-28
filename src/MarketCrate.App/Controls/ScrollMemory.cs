using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace MarketCrate.App.Controls;
// Per control, per game; event driven and bounded. No external windows/input or persisted account data.
public static class ScrollMemory
{
    public static readonly DependencyProperty KeyProperty=DependencyProperty.RegisterAttached("Key",typeof(string),typeof(ScrollMemory),new PropertyMetadata(null,Changed));
    private static readonly DependencyProperty PositionsProperty=DependencyProperty.RegisterAttached("Positions",typeof(Dictionary<string,double>),typeof(ScrollMemory));
    public static string? GetKey(DependencyObject target)=>(string?)target.GetValue(KeyProperty);
    public static void SetKey(DependencyObject target,string value)=>target.SetValue(KeyProperty,value);
    private static void Changed(DependencyObject target,DependencyPropertyChangedEventArgs args)
    {
        if(target is not FrameworkElement root)return;
        if(root.GetValue(PositionsProperty)is not Dictionary<string,double> positions){positions=[];root.SetValue(PositionsProperty,positions);root.Loaded+=Loaded;root.Unloaded+=Unloaded;}
        if(args.OldValue is string old&&Find(root)is {} previous)positions[old]=previous.VerticalOffset;
        if(positions.Count>100)positions.Remove(positions.Keys.First());Restore(root);
    }
    private static void Loaded(object sender,RoutedEventArgs e)=>Restore((FrameworkElement)sender);
    private static void Unloaded(object sender,RoutedEventArgs e){var root=(FrameworkElement)sender;if(GetKey(root)is {} key&&Find(root)is {} viewer&&root.GetValue(PositionsProperty)is Dictionary<string,double> positions)positions[key]=viewer.VerticalOffset;}
    private static void Restore(FrameworkElement root)=>root.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,new Action(()=>{if(Find(root)is {} viewer&&GetKey(root)is {} key&&root.GetValue(PositionsProperty)is Dictionary<string,double> positions)viewer.ScrollToVerticalOffset(positions.GetValueOrDefault(key));}));
    private static ScrollViewer? Find(DependencyObject parent){if(parent is ScrollViewer viewer)return viewer;for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var result=Find(VisualTreeHelper.GetChild(parent,i));if(result is not null)return result;}return null;}
}
