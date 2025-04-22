using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace DesktopApp.Utilities.Converters;

public sealed class EnumDisplayNameConverter : ValueConverterBase
{
    private static readonly Dictionary<Type, string?[]> _names = [];

    static EnumDisplayNameConverter()
    {
        HotReloadHandler.OnClear().Subscribe(_ => _names.Clear());
    }

    private static IEnumerable<string?> GetEnumDisplayNames([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type enumType)
    {
        return enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => f.GetCustomAttribute<DisplayNameAttribute>()?.Name);
    }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return null;
        var type = value.GetType();
        if (type is not { IsEnum: true })
            return Error($"Value must be an enum - was of type {type.FullName}", nameof(value));

        if (!_names.TryGetValue(type, out var descriptions))
            _names[type] = descriptions = GetEnumDisplayNames(type).ToArray();

        return descriptions[(int)value] ?? value;
    }
}
