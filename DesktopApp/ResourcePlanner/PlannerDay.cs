using DesktopApp.Data.Player;

namespace DesktopApp.ResourcePlanner;

[JsonClass]
public class PlannerDay : ViewModelBase
{
    // each day lasts until the next reset
    public DateTime Start { get; set; } = DateTime.Now;
    public DateTime End { get; set; } = DateTime.Today.AddDays(1);

    public PlayerExpData StartingExpData { get; set; } = new();
    public int StartingSanityValue { get; set; }
    public bool IsTargetStageOpen { get; set; }

    public SanityLog SanityLog { get; set; } = new();

    public PlayerExpData FinishExpData { get; set; } = new();
    public int FinishSanityValue { get; set; }
    public int TargetStageCompletions { get; set; }

    public string DateRangeString => GetDateRangeString();
    public bool ShouldShowExpData => FinishExpData != StartingExpData;

    public string GetDateRangeString()
    {
        var isSameDay = Start.Date == End.Date;
        return isSameDay ? $"{Start:MMM dd} {Start:t} - {End:t}"
            : $"{Start:MMM dd} {Start:t} - {End:MMM dd} {End:t}";
    }
}

internal sealed class DesignPlannerDay : PlannerDay
{
    public DesignPlannerDay()
    {
        Start = DateTime.Now;
        End = Start.Date.AddHours(4);
        if (End < Start)
            End = End.AddDays(1);
        StartingExpData = new(100, 5000);
        StartingSanityValue = 60;
        IsTargetStageOpen = true;

        SanityLog.Log(240, "Natural regen");
        SanityLog.Log(-300, "Run AP-5 9 times");
        SanityLog.Log(0, "Get weekly potions", "+240 banked sanity");
        SanityLog.Log(240, "Banked sanity");
        SanityLog.Log(-180, "Saved for tomorrow");
        SanityLog.Log(10000, "Fraud");
        SanityLog.Log(10000, "Fraud");
        SanityLog.Log(10000, "Fraud");
        SanityLog.Log(10000, "Fraud");
        SanityLog.Log(10000, "Fraud");
        SanityLog.Log(10000, "Fraud");
        SanityLog.Log(10000, "Fraud");
        SanityLog.Log(10000, "Fraud");
        SanityLog.Log(10000, "Fraud");

        FinishExpData = new(101, 10000);
        FinishSanityValue = 180;
        TargetStageCompletions = 9;
    }
}
