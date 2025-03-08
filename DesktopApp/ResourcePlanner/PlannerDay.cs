using DesktopApp.Utilities.Attributes;

namespace DesktopApp.ResourcePlanner;

[JsonClass]
public class PlannerDay : ViewModelBase
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

public sealed class DesignPlannerDay : PlannerDay
{
    public DesignPlannerDay()
    {
        Date = DateTime.Today;
        StartingPlayerLevel = 100;
        StartingPlayerExp = 5000;
        StartingSanityValue = 60;
        IsTargetStageOpen = true;

        SanityLog.Log(240, "Natural regen");
        SanityLog.Log(-300, "Run AP-5 9 times");
        SanityLog.Log(120, "Use weekly potion");
        SanityLog.Log(-30, "Run AP-5 once");

        FinishPlayerLevel = 101;
        FinishPlayerExp = 10000;
        FinishSanityValue = 90;
        TargetStageCompletions = 9;
    }
}
