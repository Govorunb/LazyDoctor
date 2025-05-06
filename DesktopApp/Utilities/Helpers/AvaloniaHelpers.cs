using System.Reactive;
using Avalonia;
using Avalonia.Controls;

namespace DesktopApp.Utilities.Helpers;

public static class AvaloniaHelpers
{
    public static Func<IInteractionContext<string, Unit>, Task> OpenInDefaultEditor(Visual? visual)
    {
        return async ctx =>
        {
            var topLevel = GetTopLevel(visual)
                   ?? throw new InvalidOperationException("Could not find UI top level");
            var success = await topLevel.Launcher.LaunchUriAsync(new($"file://{ctx.Input}"));
            if (!success)
                throw new InvalidOperationException($"Could not open {ctx.Input} - operation failed or not supported");
        };
    }

    internal static TopLevel? GetTopLevel(Visual? visual)
        => visual is { }
            ? TopLevel.GetTopLevel(visual)
            : App.Current.MainWindow;
}
