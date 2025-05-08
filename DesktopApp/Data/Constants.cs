namespace DesktopApp.Data;

public static class Constants
{
    public static readonly TimeOnly EnServerReset = new(4, 0);
    public static readonly TimeSpan EnServerTimezone = TimeSpan.FromHours(-7);
    public const string RecruitPageId = "recruitment";
    public const string PlannerPageId = "planner";
    public const string SettingsPageId = "settings";
}
