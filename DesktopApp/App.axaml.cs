using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace DesktopApp;

[PublicAPI]
public sealed class App : Application
{
    public static readonly Version Version = typeof(App).Assembly.GetName().Version ?? new();

    public new static App? Current => (App?)Application.Current;
    public IClassicDesktopStyleApplicationLifetime? DesktopLifetime => ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
    public MainWindow? MainWindow => DesktopLifetime?.MainWindow as MainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (DesktopLifetime is { } desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = LOCATOR.GetService<MainWindowViewModel>() };
            UiWorkarounds.CalendarDatePickerScrollShouldResetTimeComponent();
        }
#pragma warning disable IL2026 // Own assembly is rooted
        ViewLocator.RegisterViews();
#pragma warning restore IL2026 // RequiresUnreferencedCode

        base.OnFrameworkInitializationCompleted();
    }
}
