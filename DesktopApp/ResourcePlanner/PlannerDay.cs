using DesktopApp.Utilities.Attributes;

namespace DesktopApp.ResourcePlanner;

[JsonClass]
public sealed class PlannerDay : ViewModelBase
{
    public DateTime Date { get; init; }

    public int StartingPlayerLevel { get; init; }
    public int StartingPlayerExp { get; init; }
    public int StartingSanityValue { get; set; }
    public bool IsTargetStageOpen { get; init; }

    public SanityLog SanityLog { get; init; } = new();

    public int FinishPlayerLevel { get; set; }
    public int FinishPlayerExp { get; set; }
    public int FinishSanityValue { get; set; }
    public int TargetStageCompletions { get; set; }
}
