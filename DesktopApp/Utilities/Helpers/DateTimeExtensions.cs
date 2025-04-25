using JetBrains.Annotations;

namespace DesktopApp.Utilities.Helpers;

[PublicAPI]
internal static class DateTimeExtensions
{
    public static DateTime Clamp(this DateTime value, DateTime min, DateTime max)
    {
        return value < min ? min
            : value > max ? max
            : value;
    }
}
