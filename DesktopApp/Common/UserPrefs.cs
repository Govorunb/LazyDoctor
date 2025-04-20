using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DesktopApp.Data;
using DesktopApp.Recruitment;
using DesktopApp.ResourcePlanner;
using DesktopApp.Settings;
using DesktopApp.Utilities.Attributes;
using DesktopApp.Utilities.Helpers;
using JetBrains.Annotations;

namespace DesktopApp.Common;

[PublicAPI]
public sealed class UserPrefs : DataSource<UserPrefs.UserPrefsData>
{
    private const string PrefsFileName = "prefs.json";

    [JsonClass, EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class UserPrefsData : ReactiveObjectBase
    {
        public Version Version { get; set; } = null!; // populated on save
        public string Language { get; set; } = "en_US";

        public RecruitmentPrefsData Recruitment { get; set => SetIfNotNull(ref field, value); } = new();
        public ResourcePlannerPrefsData Planner { get; set => SetIfNotNull(ref field, value); } = new();

        public GeneralPrefsData General { get; set => SetIfNotNull(ref field, value); } = new();

        // preserve newer configs on older versions
        [JsonExtensionData] internal Dictionary<string, object> UnknownProperties { get; set; } = new();

        internal static JsonSerializerOptions SerializerOptions { get; } = new(JsonSourceGenContext.Default.Options)
        {
            WriteIndented = true, // more human-readable (and editable) (but still not yaml, all my homies hate yaml)
        };
    }

    private readonly IAppData _appData;
    [Reactive]
    public UserPrefsData? Data { get; set; }
    public Version? Version => Data?.Version;
    public RecruitmentPrefsData? Recruitment => Data?.Recruitment;
    public ResourcePlannerPrefsData? Planner => Data?.Planner;
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
                this.RaisePropertyChanged(nameof(Planner));
            })
            .WhereNotNull()
            .Switch(d => d.Recruitment.Changed
                .Merge(d.General.Changed)
                .Merge(d.Planner.Changed)
            );

        prefsChanged
            .Debounce(TimeSpan.FromMilliseconds(1000))
            .SubscribeAsync(_ => Save());

        Task.Run(Reload);
    }

    public async Task Save()
    {
        Debug.Assert(Data is { });
        Data.Version = App.Version;
#pragma warning disable IL2026 // Type is used (whole assembly is rooted, too)
#pragma warning disable IL3050 // type resolver will use source generated code
        var json = JsonSerializer.Serialize(Data, UserPrefsData.SerializerOptions);
#pragma warning restore IL2026 // RequiresUnreferencedCode
#pragma warning restore IL3050 // RequiresDynamicCode
        this.Log().Info("Saving preferences");
        await _appData.WriteFile(PrefsFileName, json);
    }

    public override async Task<UserPrefsData> Reload()
    {
        UserPrefsData? data;
        // TODO: set log levels from data
        try
        {
            data = await ReloadData();
            if (data is null)
            {
                this.Log().Warn("No prefs found, using default");
                data = new();
            }
        }
        catch (Exception e)
        {
            this.Log().Error(e, "Failed to load preferences, reverting to default");
            data = new();
        }
        this.Log().Info("Loaded preferences");
        data = UserPrefsMigrations.RunMigrations(data);
        return Data = data;
    }

    private async Task<UserPrefsData?> ReloadData()
    {
        var appData = LOCATOR.GetService<IAppData>();
        AssertDI(appData);

        var json = await appData.ReadFile(PrefsFileName);
        if (json is null)
            return null;

        // ReSharper disable once RedundantAlwaysMatchSubpattern
        // Version can't be null... except for right here (and only here)
        if (JsonSourceGenContext.Default.Deserialize<UserPrefsData>(json) is not { Version: { } } loaded)
            throw new InvalidOperationException("Data is malformed");
        // ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        // fixups for explicit nulls
        loaded.Recruitment ??= new();
        loaded.General ??= new();
        loaded.Planner ??= new();
        // ReSharper restore NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        return loaded;
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
    private sealed record Migration(Version Version, Func<UserPrefs.UserPrefsData, UserPrefs.UserPrefsData> Migrate);

    private static readonly List<Migration> _migrations = new Migration[]
    {
        new(new("0.1.2.0"), d =>
        {
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            // json
            d.General ??= new();
            return d;
        }),
    }.OrderBy(m => m.Version).ToList();

    public static UserPrefs.UserPrefsData RunMigrations(UserPrefs.UserPrefsData data)
    {
        var oldVersion = data.Version;
        if (oldVersion == App.Version)
        {
            data.Log().Info($"No migrations to perform, prefs data version matches app version ({App.Version})");
            return data;
        }

        foreach (var migration in _migrations)
        {
            if (migration.Version <= data.Version)
            {
                data.Log().Debug($"Skipping migration for {migration.Version} as data is same or newer ({data.Version})");
                continue;
            }

            if (migration.Version > App.Version)
            {
                data.Log().Warn($"Ending migrations, migration v{migration.Version} is newer than current app version ({App.Version})");
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
