using System.Globalization;
using Avalonia.Data.Converters;
using DesktopApp.Data;

namespace DesktopApp.Utilities.Converters;

[PublicAPI]
internal static class FuncConverters
{
    public static FuncValueConverter<int, string> RarityStars { get; }
        = new(stars => '★'.Repeat(stars) + '☆'.Repeat(6 - stars));

    public static FuncValueConverter<int, int?> PlayerLevelToExpRequirement { get; }
        = new(lvl => LOCATOR.GetService<IDataSource<GameConstants>>()!.Values.MostRecent(null).First()?.GetExpRequirementForNextLevel(lvl));

    public static FuncValueConverter<double, string> DoubleWithPlusPrefix { get; }
        = new (utcOffset => (utcOffset >= 0 ? "+" : "") + utcOffset.ToString("N0", CultureInfo.InvariantCulture));

    public static FuncValueConverter<int, string> IntWithPlusPrefix { get; }
        = new (utcOffset => (utcOffset >= 0 ? "+" : "") + utcOffset);
}
