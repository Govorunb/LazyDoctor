using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DesktopApp.ViewModels;
using DesktopApp.Views;
using HotAvalonia;

namespace DesktopApp;

public sealed class App : Application
{
    public override void Initialize()
    {
        this.EnableHotReload();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mwvm = LOCATOR.GetService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mwvm,
            };
        }
        ViewLocator.RegisterViews();

        base.OnFrameworkInitializationCompleted();
    }
}
