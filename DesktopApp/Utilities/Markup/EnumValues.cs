using Avalonia.Markup.Xaml;

namespace DesktopApp.Utilities.Markup;

public sealed class EnumValuesExtension : MarkupExtension
{
    private readonly Type _enumType;

    public EnumValuesExtension(Type enumType)
    {
        if (enumType is not { IsEnum: true })
            throw new ArgumentException("Type must be an enum", nameof(enumType));
        _enumType = enumType;
    }


    public override object ProvideValue(IServiceProvider serviceProvider)
        => Enum.GetValues(_enumType);
}
