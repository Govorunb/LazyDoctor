namespace DesktopApp.Recruitment;

public sealed class RecruitmentPrefsData : ReactiveObjectBase
{
    [Reactive]
    // assuming >3h50m, which removes 1&2stars from the pool
    public FilterType[] RarityFilters { get; set; } = [FilterType.Hide, FilterType.Hide, FilterType.Exclude];
    [Reactive]
    public bool TagsExpanded { get; set; } = true;
    [Reactive]
    public bool MonitorClipboard { get; set; }
}
