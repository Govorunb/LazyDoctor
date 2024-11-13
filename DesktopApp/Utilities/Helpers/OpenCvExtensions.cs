using JetBrains.Annotations;
using OpenCvSharp;

namespace DesktopApp.Utilities.Helpers;

[PublicAPI]
internal static class OpenCvExtensions
{
    public static Window ShowWindow(this Mat image, [CallerArgumentExpression(nameof(image))] string? title = null)
    {
        return new Window(title ?? $"Unnamed ({image.Size()})", image);
    }
}
