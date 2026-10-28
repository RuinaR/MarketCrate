using System.Globalization;
using System.Windows.Data;
using MarketCrate.Core;
using MarketCrate.App.ViewModels;
namespace MarketCrate.App.Controls;
public sealed class SaleStateLabelConverter:IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is SaleState state?SteamStatusText.Label(state):"확인 필요";
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
