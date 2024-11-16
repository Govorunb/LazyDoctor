using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using DesktopApp.Data;
using DesktopApp.Recruitment;
using DesktopApp.Utilities.Helpers;
using JetBrains.Annotations;

namespace DesktopApp.Common;

public sealed class UserPrefs : DataSource<UserPrefs.UserPrefsData>
{
    private readonly IAppData _appData;

    private const string PrefsFileName = "prefs.json";

    public sealed class UserPrefsData : ReactiveObjectBase
    {
        public string Version { get; set; } = typeof(UserPrefs).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
        public RecruitmentPrefsData Recruitment { get; set; } = new();
    }

    [Reactive]
    private UserPrefsData? Data { get; set; }
    [PublicAPI]
    public string? Version => Data?.Version;
    public RecruitmentPrefsData? Recruitment => Data?.Recruitment;
    public IObservable<Unit> Loaded => this.ObservableForProperty(t => t.Data).ToUnit();

    public UserPrefs(IAppData appData)
    {
        AssertDI(appData);
        _appData = appData;
        var recruitDataChanged = this.WhenAnyValue(t => t.Data!.Recruitment)
            .Do(_ => this.RaisePropertyChanged(nameof(Recruitment)))
            .Switch(r => r.Changed.ToUnit());
        recruitDataChanged
            .Sample(TimeSpan.FromMilliseconds(500))
            .SubscribeAsync(_ => Save());

        Task.Run(Reload);
    }

    public async Task Save()
    {
        var json = JsonSerializer.Serialize(Data, JsonSourceGenContext.Default.UserPrefsData!);
        this.Log().Info($"Saving preferences {json}");
        await _appData.WriteFile(PrefsFileName, json);
    }

    [PublicAPI]
    public async Task Reload()
    {
        Data = await ReloadData();
    }

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
            return loaded;
        }
        catch (Exception e)
        {
            this.Log().Error(e, "Failed to load preferences, reverting to default");
            return new();
        }
    }
}
