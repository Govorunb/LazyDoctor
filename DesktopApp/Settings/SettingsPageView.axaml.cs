using Avalonia.ReactiveUI;
using DesktopApp.Utilities.Helpers;

namespace DesktopApp.Settings;

public partial class SettingsPageView : ReactiveUserControl<SettingsPage>
{
    public SettingsPageView()
    {
        InitializeComponent();
        this.WhenAnyValue(t => t.ViewModel)
            .WhereNotNull()
            .Subscribe(vm =>
            {
                vm.PlatformOpenFolder
                    .RegisterHandler(AvaloniaHelpers.OpenInDefaultEditor(this))
                    .DisposeWith(vm);
            });
    }
}
