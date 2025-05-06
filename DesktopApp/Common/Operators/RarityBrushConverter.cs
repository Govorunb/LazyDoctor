using System.Globalization;
using DesktopApp.Utilities.Converters;

namespace DesktopApp.Common.Operators;

public sealed class RarityBrushConverter : ValueConverterBase
{
    private static readonly IBrush[] _brushes =
    [
        Brushes.DarkGray,
        Brushes.PaleGoldenrod,
        Brushes.CornflowerBlue,
        Brushes.LightPink,
        Brushes.Bisque,
        Brushes.Gold,
    ];
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int rarity)
            return Error("Value must be an integer");
        return _brushes[rarity - 1];
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IBrush brush)
            return Error("Value must be a brush");
        return Array.IndexOf(_brushes, brush) + 1;
    }
}
