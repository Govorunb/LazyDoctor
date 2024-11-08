using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace DesktopApp.Utilities.Converters;

public abstract class ValueConverterBase : IValueConverter
{
    public abstract object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);
    public abstract object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);

    protected static BindingNotification Error(string message, string? paramName = null)
        => new(new ArgumentException(message, paramName), BindingErrorType.DataValidationError);
}
