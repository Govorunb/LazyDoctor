using System.Reactive;
using Avalonia;
using Avalonia.ReactiveUI;
using DesktopApp.Data;
using ReactiveMarbles.CacheDatabase.Core;

namespace DesktopApp;

internal static class Program
{
    [ModuleInitializer]
    internal static void Init()
    {
        CoreRegistrations.Serializer = new JsonContextSerializer(JsonSourceGenContext.Default);
        // yes please do just swallow my exception and throw a generic one instead. it's a great thing to do as a library
        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex => LogHost.Default.Error(ex, "Unhandled exception in observable"));
    }
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
