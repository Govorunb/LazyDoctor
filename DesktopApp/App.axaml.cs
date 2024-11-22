using System.ComponentModel;
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
    public override void Initialize()
    {
        CoreRegistrations.Serializer = new JsonContextSerializer(JsonSourceGenContext.Default);

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
        ViewLocator.RegisterViews();

        base.OnFrameworkInitializationCompleted();
    }
}
