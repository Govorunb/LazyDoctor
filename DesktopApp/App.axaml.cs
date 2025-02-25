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
