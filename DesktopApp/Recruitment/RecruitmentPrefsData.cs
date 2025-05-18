namespace DesktopApp.Recruitment;

[JsonClass]
public sealed class RecruitmentPrefsData : ModelBase
{
    [Reactive]
    // assuming >3h50m, which removes 1&2stars from the pool
    public FilterType[] RarityFilters { get; set; } = [FilterType.Hide, FilterType.Hide, FilterType.Exclude];
    [Reactive]
    public bool MonitorClipboard { get; set; }

    #region UI state
    [Reactive]
    public bool TagsExpanded { get; set; } = true;
    #endregion UI state
}
