using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DesktopApp.Common.Operators;

public sealed class RarityBrushConverter : IValueConverter
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
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int rarity)
            return new BindingNotification(new ArgumentException("Value must be an integer"), BindingErrorType.DataValidationError);
        return _brushes[rarity - 1];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IBrush brush)
            return new BindingNotification(new ArgumentException("Value must be a brush"), BindingErrorType.DataValidationError);
        return Array.IndexOf(_brushes, brush) + 1;
    }
}
