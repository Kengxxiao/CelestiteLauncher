namespace FluentAvalonia.UI.Controls;
internal static class SymbolConversion
{
    private static FilledSymbol ToFilledSymbol(this Symbol symbol)
    {
        return FilledSymbolExtensions.TryParse(symbol.ToStringFast(), out var pSymbol) ? pSymbol : FilledSymbol.ErrorCircle;
    }

    internal static char ToChar(this Symbol symbol, bool isFilled)
        => char.ConvertFromUtf32(isFilled ? (int)symbol.ToFilledSymbol() : (int)symbol).Single();

    internal static string ToString(this Symbol symbol, bool isFilled)
        => char.ConvertFromUtf32(isFilled ? (int)symbol.ToFilledSymbol() : (int)symbol);
}
