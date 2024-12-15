using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace DesktopApp.Utilities.Converters;

public sealed class EnumDescriptionConverter : ValueConverterBase
{
    private static readonly Dictionary<Type, string?[]> _descriptions = [];

    static EnumDescriptionConverter()
    {
        HotReloadHandler.OnClear().Subscribe(_ => _descriptions.Clear());
    }

    private static IEnumerable<string?> GetEnumDescriptions([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type enumType)
    {
        return enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => f.GetCustomAttribute<DescriptionAttribute>()?.Description);
    }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value?.GetType() is not { IsEnum: true } enumType)
            return Error("Value must be an enum", nameof(value));

        if (!_descriptions.TryGetValue(enumType, out var descriptions))
            _descriptions[enumType] = descriptions = GetEnumDescriptions(enumType).ToArray();

        return descriptions[(int)value];
    }
}
