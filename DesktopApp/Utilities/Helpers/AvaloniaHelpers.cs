using System.Reactive;

namespace DesktopApp.Utilities.Helpers;

public static class AvaloniaHelpers
{
    public static ReactiveCommand<string, Unit> OpenFileCommand { get; }
        = ReactiveCommand.CreateFromTask<string>(OpenInDefaultEditor);
    public static ReactiveCommand<string, Unit> OpenUrlCommand { get; }
        = ReactiveCommand.CreateFromTask<string>(OpenInDefaultBrowser);

    public static Task OpenInDefaultEditor(string path) => Open($"file://{path}");
    public static Task OpenInDefaultBrowser(string url) => Open(url);

    private static async Task Open(string uri)
    {
        var topLevel = App.Current?.Toplevel
            ?? throw new InvalidOperationException("Could not find top level");
        var success = await topLevel.Launcher.LaunchUriAsync(new(uri));
        if (!success)
            throw new InvalidOperationException($"Could not open {uri} - operation failed or not supported");
    }
}
