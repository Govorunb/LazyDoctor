using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace DesktopApp.Utilities.Helpers;

public static class AvaloniaHelpers
{
    public static Func<IInteractionContext<string, Unit>, Task> OpenInDefaultEditor(Visual? visual)
    {
        return ctx => GetTopLevel(visual)?.Launcher.LaunchUriAsync(new($"file://{ctx.Input}"))
                        ?? Task.FromException(new InvalidOperationException("Could not find UI top level"));
    }

    internal static TopLevel? GetTopLevel(Visual? visual)
        => visual is { }
            ? TopLevel.GetTopLevel(visual)
            : (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
