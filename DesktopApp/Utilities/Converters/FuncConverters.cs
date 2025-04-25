using Avalonia.Data.Converters;
using DesktopApp.Data;
using DesktopApp.Utilities.Helpers;
using JetBrains.Annotations;

namespace DesktopApp.Utilities.Converters;

[PublicAPI]
internal static class FuncConverters
{
    public static FuncValueConverter<int, string> RarityStars { get; }
        = new(stars => '★'.Repeat(stars) + '☆'.Repeat(6 - stars));

    public static FuncValueConverter<int, int> PlayerLevelToExpRequirement { get; }
        = new(lvl => LOCATOR.GetService<GameConstants>()!.GetExpRequirementForNextLevel(lvl));

    public static FuncValueConverter<double, string> DoubleWithPlusPrefix { get; }
        = new (utcOffset => (utcOffset >= 0 ? "+" : "") + utcOffset.ToString("N0"));

    public static FuncValueConverter<int, string> IntWithPlusPrefix { get; }
        = new (utcOffset => (utcOffset >= 0 ? "+" : "") + utcOffset);
}
