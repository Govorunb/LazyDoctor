namespace DesktopApp.ResourcePlanner;

public sealed class PlannerDay : ViewModelBase
{
    public DateTime Date { get; init; }

    public int StartingPlayerLevel { get; init; }
    public int StartingPlayerExp { get; init; }
    public int BankedSanityValue { get; init; }
    public bool IsTargetStageOpen { get; init; }

    public int FinishPlayerLevel { get; }
    public int FinishPlayerExp { get; }
    public int SurplusSanity { get; }
}
