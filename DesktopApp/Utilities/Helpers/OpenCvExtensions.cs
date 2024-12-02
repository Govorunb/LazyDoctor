using JetBrains.Annotations;
using OpenCvSharp;
using AvaRect = Avalonia.Rect;

namespace DesktopApp.Utilities.Helpers;

[PublicAPI]
internal static class OpenCvExtensions
{
    public static Window ShowWindow(this Mat image, [CallerArgumentExpression(nameof(image))] string? title = null)
    {
        return new Window(title ?? $"Unnamed ({image.Size()})", image);
    }

    public static AvaRect ToAvaRect(this Rect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
    public static Rect ToOpenCvRect(this AvaRect rect) => new((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
}
