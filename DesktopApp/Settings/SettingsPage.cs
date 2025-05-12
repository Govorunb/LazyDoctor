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
            LastRefresh = time.GetLocalNow();
            RefreshCooldownLeft = _refreshCooldown.ToString("mm\\:ss", CultureInfo.InvariantCulture);
        }, this.WhenAnyValue(t => t.RefreshCooldownLeft).Select(cd => cd is null));
        RefreshDataSource.Execute().Subscribe();
        OpenLogsFolder = ReactiveCommand.CreateFromObservable(() => PlatformOpenFolder.Handle(AppData.GetFullPath("logs/")));
    }
}

internal sealed class DesignSettingsPage()
    : SettingsPage(
        LOCATOR.GetService<UserPrefs>()!,
        LOCATOR.GetService<TimeProvider>()!,
        LOCATOR.GetService<GithubDataAdapter>()!
    );

