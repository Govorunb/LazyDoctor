using System.Globalization;
using System.Reactive;
using DesktopApp.Data;
using DesktopApp.Data.GitHub;

namespace DesktopApp.Settings;

public class SettingsPage : PageBase
{
    public override string PageId => Constants.SettingsPageId;

    private static readonly TimeSpan _refreshCooldown = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan _refreshCooldownTextUpdateInterval = TimeSpan.FromSeconds(1);

    private readonly TimeProvider _time;
    public UserPrefs Prefs { get; }

    [Reactive]
    public string? RefreshCooldownLeft { get; private set; }
    [Reactive]
    public DateTimeOffset LastRefresh { get; set; }

    public ReactiveCommand<Unit, Unit> RefreshDataSource { get; }
    public ReactiveCommand<Unit, Unit> OpenLogsFolder { get; }
    public Interaction<string, Unit> PlatformOpenFolder { get; } = new();

    public SettingsPage(UserPrefs prefs, TimeProvider time, GithubDataAdapter data)
    {
        AssertDI(prefs);
        AssertDI(time);
        AssertDI(data);
        Prefs = prefs;
        _time = time;

        Observable.Interval(_refreshCooldownTextUpdateInterval)
            .ToUnit()
            .Where(_ => !string.IsNullOrEmpty(RefreshCooldownLeft))
            .Select(_ => _refreshCooldown - (time.GetLocalNow() - LastRefresh))
            .Select(remaining => remaining > TimeSpan.Zero ? remaining.ToString("mm\\:ss", CultureInfo.InvariantCulture) : null)
            .OnMainThread()
            .Subscribe(t => RefreshCooldownLeft = t);

        RefreshDataSource = ReactiveCommand.CreateFromTask(async () =>
        {
            await data.ReloadAll();
            OnRefresh();
        }, this.WhenAnyValue(t => t.RefreshCooldownLeft).Select(cd => cd is null));
        OnRefresh();
        OpenLogsFolder = ReactiveCommand.CreateFromObservable(() => PlatformOpenFolder.Handle(AppData.GetFullPath(Constants.LogsAppDataPath)));
        // TODO: open prefs file, reload prefs
    }

    private void OnRefresh()
    {
        LastRefresh = _time.GetLocalNow();
        RefreshCooldownLeft = _refreshCooldown.ToString("mm\\:ss", CultureInfo.InvariantCulture);
    }
}

internal sealed class DesignSettingsPage()
    : SettingsPage(
        LOCATOR.GetService<UserPrefs>()!,
        LOCATOR.GetService<TimeProvider>()!,
        LOCATOR.GetService<GithubDataAdapter>()!
    );
