namespace DesktopApp.Data;

public static class Constants
{
    public static readonly TimeOnly EnServerReset = new(4, 0);
    public static readonly TimeSpan EnServerTimezone = TimeSpan.FromHours(-7);
    public static bool IsDev => ModeDetector.InUnitTestRunner() || Avalonia.Controls.Design.IsDesignMode;

    public const string RecruitPageId = "recruitment";
    public const string PlannerPageId = "planner";
    public const string SettingsPageId = "settings";

    public const string PrefsAppDataPath = "prefs.json";
    public const string LogsAppDataPath = "logs";
    public const string GamedataCacheAppDataPath = "gamedata_cache";
}
