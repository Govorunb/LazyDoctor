using DesktopApp.Data;
using DesktopApp.Utilities.Attributes;

namespace DesktopApp.ResourcePlanner;

[JsonClass]
public sealed class ResourcePlannerSettings : ViewModelBase
{
    [Reactive]
    public PlannerDay InitialState { get; set; } = new()
    {
        Date = DateTime.Now,
        // TODO: player stats saved prefs?
        StartingExpData = new(),
    };

    #region Target
    [Reactive] public string TargetStageCode { get; set; } = "AP-5";
    // TODO: target mode switch (target date/target amount)
    [Reactive] public DateTime TargetDate { get; set; } = DateTime.Now.AddDays(7);
    #endregion Target

    #region Time

    [Reactive] public TimeOnly ServerReset { get; set; } = Constants.EnServerReset;
    [Reactive] public TimeSpan ServerTimezone { get; set; } = Constants.EnServerTimezone;
    #endregion Time

    #region Potion/sanity settings
    [Reactive] public int CurrentSanity { get; set; }

    // reoccurring
    [Reactive] public bool UseMonthlyCard { get; set; } // daily, 80 each
    [Reactive] public bool UseWeeklyPots { get; set; } = true; // 2x120 each
    [Reactive] public int DailySanityRegenEfficiency { get; set; } = 240; // can be manually set lower if you login once a day or something

    // banked potions
    [Reactive] public int SmallPots { get; set; } // 10 each
    [Reactive] public int MediumPots { get; set; } // 80 each
    [Reactive] public int LargePots { get; set; } // 120 each
    [Reactive] public int ExtraSanity { get; set; }

    // extra gain/loss
    [Reactive] public int RefreshOpBudget { get; set; }
    [Reactive] public int WeeklyAnniLoss { get; set; } = 124;
    #endregion Potion/sanity settings
}
