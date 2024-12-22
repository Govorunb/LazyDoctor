using Avalonia;
using Avalonia.ReactiveUI;

namespace DesktopApp;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // ReSharper disable once MemberCanBePrivate.Global
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseSkia()
            .UseWin32()
            .WithInterFont()
            // .LogToTrace(Avalonia.Logging.LogEventLevel.Information)
            .LogToTrace()
            .UseReactiveUI();
}
