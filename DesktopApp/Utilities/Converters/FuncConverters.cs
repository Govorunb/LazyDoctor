using Avalonia.Data.Converters;
using DesktopApp.Data;
using DesktopApp.Utilities.Helpers;

namespace DesktopApp.Utilities.Converters;

internal static class FuncConverters
{
    public static FuncValueConverter<int, string> RarityStars { get; }
        = new(stars => '★'.Repeat(stars) + '☆'.Repeat(6 - stars));

    public static FuncValueConverter<int, int> PlayerLevelToExpRequirement { get; }
        = new(lvl => LOCATOR.GetService<GameConstants>()!.GetExpRequirementForNextLevel(lvl));

    public static FuncValueConverter<double, string> TimezoneConverter { get; }
        = new (utcOffset => (utcOffset >= 0 ? "+" : "") + utcOffset.ToString("N0"));
}
