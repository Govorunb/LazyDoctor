using System.Diagnostics;
using DesktopApp.Recruitment;
using DesktopApp.ResourcePlanner;
using DesktopApp.Settings;

namespace DesktopApp;

public sealed class MainWindowViewModel : ViewModelBase, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();

    private readonly UserPrefs _prefs;
    public RecruitPage RecruitPage { get; }
    public ResourcePlannerPage ResourcePlannerPage { get; }
    public SettingsPage SettingsPage { get; }
    [Reactive]
    public PageBase? SelectedPage { get; set; }


    public MainWindowViewModel(RecruitPage recruitPage, ResourcePlannerPage resPlannerPage, SettingsPage settingsPage, UserPrefs prefs)
    {
        AssertDI(recruitPage);
        AssertDI(resPlannerPage);
        AssertDI(settingsPage);
        AssertDI(prefs);
        RecruitPage = recruitPage;
        ResourcePlannerPage = resPlannerPage;
        SettingsPage = settingsPage;
        _prefs = prefs;

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
        }).DisposeWith(this);
        this.WhenAnyValue(t => t.SelectedPage)
            .WhereNotNull()
            .Subscribe(p => prefs.Data.SelectedPage = p.PageId);
    }

    public Interaction<PrefsLoadIssue, PrefsLoadIssueResponse> ShowPrefsLoadIssueDialog { get; } = new();

    public void OnPrefsLoadIssueDialogHandlerRegistered()
    {
        // ew... unfortunately can't put this in the ctor since it runs way before the view has a chance to register the handler
        _prefs.LoadIssues
            .Select(err => ShowPrefsLoadIssueDialog.Handle(err).Select(resp => (err, resp)))
            .Concat()
            .Subscribe(pair =>
            {
                var (err, resp) = pair;
                _prefs.ReadOnly = resp.ReadOnly;
                switch (resp.ChosenAction)
                {
                    case PrefsLoadIssueAction.TryAgain: _ = Task.Run(_prefs.Reload); break;
                    case PrefsLoadIssueAction.Continue: Debug.Assert(!err.IsError); break;
                    case PrefsLoadIssueAction.RevertToDefaults: _prefs.ResetToDefaults(); break;
                    default: throw new UnreachableException();
                }
            }).DisposeWith(this);
    }
}

public enum PrefsLoadIssueAction { TryAgain, Continue, RevertToDefaults }
public record struct PrefsLoadIssueResponse(PrefsLoadIssueAction ChosenAction, bool ReadOnly);
