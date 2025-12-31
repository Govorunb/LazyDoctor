using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text.Json;
using DesktopApp.Data;
using DesktopApp.Recruitment;
using DesktopApp.ResourcePlanner;
using DesktopApp.Settings;

namespace DesktopApp.Common;

[PublicAPI]
public sealed class UserPrefs : DataSource<UserPrefs.UserPrefsData>
{
    [JsonClass, EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class UserPrefsData : ModelBase
    {
        public Version Version { get; set; } = App.Version;
        public string Language { get; set; } = "en_US";

        [Reactive]
        public string SelectedPage { get; set; } = Constants.RecruitPageId;

        public RecruitmentPrefsData Recruitment { get; set => SetIfNotNull(ref field, value); } = new();
        public ResourcePlannerPrefsData Planner { get; set => SetIfNotNull(ref field, value); } = new();

        public GeneralPrefsData General { get; set => SetIfNotNull(ref field, value); } = new();

        // preserve newer configs on older versions
        [JsonExtensionData] internal Dictionary<string, object> UnknownProperties { get; set; } = new();

        internal static JsonSerializerOptions SerializerOptions { get; } = new(JsonSourceGenContext.Default.Options)
        {
            WriteIndented = true, // more human-editable
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    private readonly IAppData _appData;
    private readonly ReplaySubject<PrefsLoadIssue> _issueSubj = new(1);

    [Reactive]
    public UserPrefsData Data { get; private set; } = new();
    public Version Version => Data.Version;
    public RecruitmentPrefsData Recruitment => Data.Recruitment;
    public ResourcePlannerPrefsData Planner => Data.Planner;
    public GeneralPrefsData General => Data.General;
    public IObservable<Unit> Loaded { get; }
    public IObservable<PrefsLoadIssue> LoadIssues => _issueSubj.AsObservable().OnMainThread();
    public bool ReadOnly { get; set; } = true;

    public UserPrefs(IAppData appData)
    {
        AssertDI(appData);
        _appData = appData;

        Loaded = this.ObservableForProperty(t => t.Data)
            .ToUnit()
            .ReplayCold(1)
            .OnMainThread();
        var dataChanged = this.WhenAnyValue(t => t.Data).Publish().AutoConnect();
        this.NotifyProperty(nameof(Recruitment), dataChanged);
        this.NotifyProperty(nameof(General), dataChanged);
        this.NotifyProperty(nameof(Planner), dataChanged);

        var prefsChanged = dataChanged
            .WhereNotNull()
            .Switch(d => d.Changed
                .Merge(d.Recruitment.Changed)
                .Merge(d.General.Changed)
                .Merge(d.Planner.Changed)
            );

        prefsChanged
            .Debounce(TimeSpan.FromMilliseconds(1000))
            .SubscribeAsync(_ => Save());

        Task.Run(Reload)
            // raise for 1st load (whether successful or not) so ui can do its thing
            .ContinueWith(_ => this.RaisePropertyChanged(nameof(Data)));
    }

    public async Task Save()
    {
        if (ReadOnly)
        {
            this.Log().Debug("Skipped saving prefs (in read-only mode)");
            return;
        }

        Debug.Assert(Data is { });
        Data.Version = App.Version;
        this.Log().Debug("Saving preferences");
#pragma warning disable IL2026 // Type is used (whole assembly is rooted, too)
#pragma warning disable IL3050 // type resolver will use source generated code
        var json = JsonSerializer.Serialize(Data, UserPrefsData.SerializerOptions);
#pragma warning restore IL3050 // RequiresDynamicCode
#pragma warning restore IL2026 // RequiresUnreferencedCode
        await _appData.WriteFile(Constants.PrefsAppDataPath, json);
        this.Log().Info("Wrote preferences");
    }

    public override async Task<UserPrefsData> Reload()
    {
        UserPrefsData? data = null;
        try
        {
            data = await ReloadData();
            ReadOnly = false; // loaded safely (whether prefs were there or not)
            if (data is null)
            {
                this.Log().Info("No prefs found");
            }
            else if (data.Version > App.Version)
            {
                this.Log().Warn($"Preferences version {data.Version} is newer than current app version ({App.Version})");
                ReadOnly = true;
                _issueSubj.OnNext(new(PrefsLoadIssueKind.Warning, $"""
                  The prefs file is for a newer version ({data.Version}) than the app ({App.Version}).
                  To avoid breaking the prefs, it's recommended to load in read-only mode.
                  """));
            }
            else if (data.UnknownProperties.Count > 0)
            {
                this.Log().Warn("Prefs contain unknown properties (possibly from a future version)");
                ReadOnly = true;
                _issueSubj.OnNext(new(PrefsLoadIssueKind.Warning, """
                  The prefs file contains unknown properties.
                  This is generally weird and shouldn't happen without manual editing.
                  To limit the impact of any incompatibilities, it's recommended to load in read-only mode.'
                  """, $"Unknown properties: [{string.Join(", ", Data.UnknownProperties.Keys)}]"));
            }
        }
        catch (Exception e)
        {
            this.Log().Error(e, "Failed to load preferences");
            _issueSubj.OnNext(new(PrefsLoadIssueKind.Error, "The preferences failed to load.", $"{e}\n{e.StackTrace}"));
        }

        if (data is null)
            return Data;

        ReadOnly |= Constants.IsDev;

        data = UserPrefsMigrations.RunMigrations(data);
        Data = data;
        this.Log().Info("Loaded preferences");
        return data;
    }

    private async Task<UserPrefsData?> ReloadData()
    {
        var json = await _appData.ReadFile(Constants.PrefsAppDataPath);
        if (json is null)
            return null;

        // ReSharper disable once RedundantAlwaysMatchSubpattern
        // Version can't be null... except for right here (and only here)
        if (JsonSourceGenContext.Default.Deserialize<UserPrefsData>(json) is not { Version: { } } loaded)
            throw new InvalidOperationException("Data is malformed");
        // ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        // fixups for explicit nulls (malformed/in-dev)
        loaded.Recruitment ??= new();
        loaded.General ??= new();
        loaded.Planner ??= new();
        // ReSharper restore NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        return loaded;
    }

    public void ResetToDefaults()
    {
        Data = new();
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
        new(new("0.2.0"), d =>
        {
            d.SelectedPage = Constants.RecruitPageId;
            return d;
        }),
    }.OrderBy(m => m.Version).ToList();

    public static UserPrefs.UserPrefsData RunMigrations(UserPrefs.UserPrefsData data)
    {
        var initialVersion = data.Version;
        if (initialVersion == App.Version)
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
                // dirty backport?
                data.Log().Info($"Ending migrations, migration v{migration.Version} is newer than current app version ({App.Version})");
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
        data.Log().Info($"Finished migrations from {initialVersion} to {data.Version}");
        return data;
    }
}

public enum PrefsLoadIssueKind { Warning, Error }

/// <summary>
/// Represents a problem that occurred while loading the prefs.
/// </summary>
/// <param name="Kind">
/// Whether the problem is a warning (i.e. the user can choose to continue with the loaded prefs)
/// or an error (i.e. prefs could not be loaded, and the user can only revert to defaults).
/// </param>
/// <param name="Message">Message to display to the user.</param>
/// <param name="Details">Additional details, e.g. an exception stack trace.</param>
[PublicAPI]
public sealed record PrefsLoadIssue(PrefsLoadIssueKind Kind, string Message, string? Details = null)
{
    public bool IsWarning => Kind == PrefsLoadIssueKind.Warning;
    public bool IsError => Kind == PrefsLoadIssueKind.Error;
}
