using System.Globalization;
namespace MarketCrate.Core;

// TryParse alone accepts trailing NUL characters even with NumberStyles.None.
// Preserve the raw UI text; never normalize invalid characters into a sale amount.
public static class UserNumericInput
{
    public static bool TryQuantity(string? text,out int value)
    {
        value=0;
        return text is {Length:>0 and <=10}&&text.All(char.IsAsciiDigit)&&int.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out value);
    }
    public static bool PriceCharactersValid(string? text,bool decimalPoint)=>text is {Length:>0 and <=32}&&text.All(c=>char.IsAsciiDigit(c)||(decimalPoint&&c=='.'));
}
