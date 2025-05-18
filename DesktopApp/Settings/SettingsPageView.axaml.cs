using System.Reactive.Disposables;
using Avalonia.ReactiveUI;

namespace DesktopApp.Settings;

public partial class SettingsPageView : ReactiveUserControl<SettingsPage>
{
    public SettingsPageView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            ViewModel!.PlatformOpenFolder
                .RegisterHandler(AvaloniaHelpers.OpenInDefaultEditor)
                .DisposeWith(d);
        });
    }
}
