using DesktopApp.Recruitment;
using DesktopApp.ResourcePlanner;
using DesktopApp.Settings;

namespace DesktopApp;

public sealed class MainWindowViewModel(
    RecruitPage recruitPage,
    ResourcePlannerPage resPlannerPage,
    SettingsPage settingsPage
) : ViewModelBase
{
    public RecruitPage RecruitPage { get; } = recruitPage;
    public ResourcePlannerPage ResourcePlannerPage { get; } = resPlannerPage;
    public SettingsPage SettingsPage { get; } = settingsPage;
    public PageBase? SelectedPage { get; set; }
}
