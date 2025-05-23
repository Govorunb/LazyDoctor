using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace DesktopApp.Utilities.Converters;

public abstract class ConverterBase
{
    protected static BindingNotification Error(string message, string? paramName = null)
        => new(new ArgumentException(message, paramName), BindingErrorType.DataValidationError);
}

public abstract class ValueConverterBase : ConverterBase, IValueConverter
{
    public abstract object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    public virtual object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public abstract class MultiValueConverterBase : ConverterBase, IMultiValueConverter
{
    public abstract object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture);
}
