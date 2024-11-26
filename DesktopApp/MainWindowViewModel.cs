using DesktopApp.Recruitment;
using DesktopApp.Settings;

namespace DesktopApp;

public sealed class MainWindowViewModel(RecruitPage recruitPage, SettingsPage settingsPage) : ViewModelBase
{
    public RecruitPage RecruitPage { get; } = recruitPage;
    public SettingsPage SettingsPage { get; } = settingsPage;
    public PageBase? SelectedPage { get; set; }
}
