using System.Diagnostics.CodeAnalysis;
using DesktopApp.Data.Player;
using DesktopApp.Utilities.Attributes;

namespace DesktopApp.ResourcePlanner;

[JsonClass]
public class PlannerDay : ViewModelBase
{
    public DateTime Date { get; set; } = DateTime.Today;

    public PlayerExpData StartingExpData { get; set; } = new();
    public int StartingSanityValue { get; set; }
    public bool IsTargetStageOpen { get; set; }

    public SanityLog SanityLog { get; set; } = new();

    public PlayerExpData FinishExpData { get; set; } = new();
    public int FinishSanityValue { get; set; }
    public int TargetStageCompletions { get; set; }
}

internal sealed class DesignPlannerDay : PlannerDay
{
    [SetsRequiredMembers]
    public DesignPlannerDay()
    {
        Date = DateTime.Today;
        StartingExpData = new(100, 5000);
        StartingSanityValue = 60;
        IsTargetStageOpen = true;

        SanityLog.Log(240, "Natural regen");
        SanityLog.Log(-300, "Run AP-5 9 times");
        SanityLog.Log(120, "Use weekly potion");
        SanityLog.Log(-30, "Run AP-5 once");

        FinishExpData = new(101, 10000);
        FinishSanityValue = 90;
        TargetStageCompletions = 9;
    }
}
