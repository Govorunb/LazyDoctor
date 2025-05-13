using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace DesktopApp;

[PublicAPI]
public sealed class App : Application
{
    public static Version Version => AssemblyInfo.Version;
    public static string? Author => AssemblyInfo.Author;
    public static string? Product => AssemblyInfo.Product;

    public new static App? Current => (App?)Application.Current;
    public IClassicDesktopStyleApplicationLifetime? DesktopLifetime => ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
    public ISingleViewApplicationLifetime? SingleLifetime => ApplicationLifetime as ISingleViewApplicationLifetime;
    public MainWindow? MainWindow => DesktopLifetime?.MainWindow as MainWindow;
    public Control? MainView => SingleLifetime?.MainView;
    public TopLevel? Toplevel => MainWindow ?? MainView as TopLevel;

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
