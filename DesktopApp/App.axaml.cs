using System.ComponentModel;
using System.Reactive;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DesktopApp.Data;
using HotAvalonia;
using ReactiveMarbles.CacheDatabase.Core;

namespace DesktopApp;

public sealed class App : Application
{
    public static event EventHandler<CancelEventArgs> ShutdownRequested = delegate { };
    public static readonly Version Version = typeof(App).Assembly.GetName().Version ?? new();

    public override void Initialize()
    {
        CoreRegistrations.Serializer = new JsonContextSerializer(JsonSourceGenContext.Default);
        // yes please do just swallow my exception and throw a generic one instead. it's a great thing to do as a library
        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex => LogHost.Default.Error(ex, "Unhandled exception in observable"));

        this.EnableHotReload();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = LOCATOR.GetService<MainWindowViewModel>() };
            desktop.ShutdownRequested += (s, e) => ShutdownRequested.Invoke(s, e);
        }
#pragma warning disable IL2026 // Own assembly is rooted
        ViewLocator.RegisterViews();
#pragma warning restore IL2026 // RequiresUnreferencedCode

        base.OnFrameworkInitializationCompleted();
    }
}
