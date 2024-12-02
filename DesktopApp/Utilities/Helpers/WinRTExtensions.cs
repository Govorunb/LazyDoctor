using Windows.Foundation;
using JetBrains.Annotations;
using AvaRect = Avalonia.Rect;

namespace DesktopApp.Utilities.Helpers;

[PublicAPI]
internal static class WinRtExtensions
{
    public static AvaRect ToAvaRect(this Rect rect)
        => new(rect.X, rect.Y, rect.Width, rect.Height);
    public static Rect ToWinRTRect(this AvaRect rect)
        => new(rect.X, rect.Y, rect.Width, rect.Height);
}
