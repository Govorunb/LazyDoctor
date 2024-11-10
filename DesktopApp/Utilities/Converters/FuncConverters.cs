using Avalonia.Data.Converters;
using DesktopApp.Utilities.Helpers;

namespace DesktopApp.Utilities.Converters;

internal static class FuncConverters
{
    public static FuncValueConverter<int, string> RarityStars { get; }
        = new(stars => '★'.Repeat(stars) + '☆'.Repeat(6 - stars));
}
