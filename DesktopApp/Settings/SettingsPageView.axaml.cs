using System.Reactive.Disposables;
using Avalonia.ReactiveUI;

namespace DesktopApp.Settings;

public partial class SettingsPageView : ReactiveUserControl<SettingsPage>
{
    // ReSharper disable once CollectionNeverQueried.Global // used in UI
    public static readonly SeriLogLevel[] LogLevels = Enum.GetValues<SeriLogLevel>();

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
