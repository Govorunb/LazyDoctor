using DesktopApp.Recruitment;
using DesktopApp.ResourcePlanner;
using DesktopApp.Settings;

namespace DesktopApp;

public sealed class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(RecruitPage recruitPage, ResourcePlannerPage resPlannerPage, SettingsPage settingsPage, UserPrefs prefs)
    {
        RecruitPage = recruitPage;
        ResourcePlannerPage = resPlannerPage;
        SettingsPage = settingsPage;

        PageBase[] pages =
        [
            recruitPage,
            resPlannerPage,
            settingsPage,
        ];

        prefs.Loaded.Subscribe(_ =>
        {
            var pageId = prefs.Data.SelectedPage;
            if (pageId == SelectedPage?.PageId)
                return;
            var idx = pages.Index()
                .FirstOrDefault(pair => pair.Item.PageId == pageId)
                .Index; // defaults to 0
            SelectedPage = pages[idx];
        });
        this.WhenAnyValue(t => t.SelectedPage)
            .WhereNotNull()
            .Subscribe(p => prefs.Data.SelectedPage = p.PageId);
    }

    public RecruitPage RecruitPage { get; }
    public ResourcePlannerPage ResourcePlannerPage { get; }
    public SettingsPage SettingsPage { get; }
    [Reactive]
    public PageBase? SelectedPage { get; set; }
}
