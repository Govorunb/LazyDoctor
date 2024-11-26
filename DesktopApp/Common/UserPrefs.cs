using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using DesktopApp.Data;
using DesktopApp.Recruitment;
using DesktopApp.Settings;
using DesktopApp.Utilities.Attributes;
using DesktopApp.Utilities.Helpers;
using JetBrains.Annotations;

namespace DesktopApp.Common;

[PublicAPI]
public sealed class UserPrefs : DataSource<UserPrefs.UserPrefsData>
{
    private const string PrefsFileName = "prefs.json";
    public static readonly string AppVersion = typeof(UserPrefs).Assembly.GetName().Version?.ToString() ?? "0.0.0.0"; // todo: move

    [JsonClass, EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class UserPrefsData : ReactiveObjectBase
    {
        public string Version { get; set; } = null!; // populated on save
        public string Language { get; set; } = "en_US";

        public RecruitmentPrefsData Recruitment { get; set => SetIfNotNull(ref field, value); } = new();

        public GeneralPrefsData General { get; set => SetIfNotNull(ref field, value); } = new();
    }

    private readonly IAppData _appData;
    [Reactive]
    public UserPrefsData? Data { get; set; }
    public string? Version => Data?.Version;
    public RecruitmentPrefsData? Recruitment => Data?.Recruitment;
    public GeneralPrefsData? General => Data?.General;
    public IObservable<Unit> Loaded { get; }

    public UserPrefs(IAppData appData)
    {
        AssertDI(appData);
        _appData = appData;

        Loaded = this.ObservableForProperty(t => t.Data)
            .ToUnit()
            .ReplayCold(1);

        var prefsChanged = this.WhenAnyValue(t => t.Data)
            .Do(_ =>
            {
                this.RaisePropertyChanged(nameof(Recruitment));
                this.RaisePropertyChanged(nameof(General));
            })
            .WhereNotNull()
            .Switch(d => d.Recruitment.Changed
                .Merge(d.General.Changed)
                .ToUnit()
            );

        prefsChanged
            .ThrottleLast(TimeSpan.FromMilliseconds(1000))
            .SubscribeAsync(_ => Save());

        Task.Run(Reload);
    }

    public async Task Save()
    {
        Debug.Assert(Data is { });
        Data.Version = AppVersion;
        var json = JsonSerializer.Serialize(Data, JsonSourceGenContext.Default.UserPrefsData);
        this.Log().Info($"Saving preferences {json}");
        await _appData.WriteFile(PrefsFileName, json);
    }

    public override async Task<UserPrefsData> Reload()
        => Data = await ReloadData();

    private async Task<UserPrefsData> ReloadData()
    {
        var appData = LOCATOR.GetService<IAppData>();
        AssertDI(appData);

        var json = await appData.ReadFile(PrefsFileName);
        if (json is null)
        {
            this.Log().Warn("No saved preferences, using default");
            return new();
        }

        try
        {
            if (JsonSourceGenContext.Default.Deserialize<UserPrefsData>(json) is not { } loaded)
                throw new InvalidOperationException("Deserialization failed");
            this.Log().Info($"Loaded preferences {json}");
            // ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            // fixups for explicit nulls
            loaded.Recruitment ??= new();
            loaded.General ??= new();
            // ReSharper restore NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            return UserPrefsMigrations.RunMigrations(loaded);
        }
        catch (Exception e)
        {
            this.Log().Error(e, "Failed to load preferences, reverting to default");
            return new();
        }
    }

    private static void SetIfNotNull<T>(ref T field, T? value)
    {
        if (value is null)
            return;
        field = value;
    }
}

file static class UserPrefsMigrations
{
    private sealed record Migration(string Version, Func<UserPrefs.UserPrefsData, UserPrefs.UserPrefsData> Migrate);

    private static readonly List<Migration> _migrations = new Migration[]
    {
        new("0.1.2.0", d =>
        {
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            // json
            d.General ??= new();
            return d;
        }),
        new("9.9.9.9", d =>
        {
            d.Log().Error("Should not be here");
            return d;
        }),
    }.OrderBy(m => m.Version).ToList();

    public static UserPrefs.UserPrefsData RunMigrations(UserPrefs.UserPrefsData data)
    {
        var oldVersion = data.Version;
        if (oldVersion == UserPrefs.AppVersion)
        {
            data.Log().Info($"No migrations to perform, prefs data version matches app version ({UserPrefs.AppVersion})");
            return data;
        }

        foreach (var migration in _migrations)
        {
            if (string.CompareOrdinal(migration.Version, data.Version) <= 0)
            {
                data.Log().Debug($"Skipping migration for {migration.Version} as data is same or newer ({data.Version})");
                continue;
            }

            if (string.CompareOrdinal(migration.Version, UserPrefs.AppVersion) > 0)
            {
                data.Log().Warn($"Ending migrations, migration v{migration.Version} is newer than current app version ({UserPrefs.AppVersion})");
                break;
            }
            data.Log().Info($"Applying migration for {migration.Version}");
            try
            {
                data = migration.Migrate(data);
                data.Version = migration.Version;
            }
            catch (Exception e)
            {
                data.Log().Error(e, $"Failed to apply migration for {migration.Version}");
            }
        }
        data.Log().Info($"Finished migrations from {oldVersion} to {data.Version}");
        return data;
    }
}
