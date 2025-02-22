using JetBrains.Annotations;

namespace DesktopApp.ResourcePlanner;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ResourcePlannerSettings : ViewModelBase
{
    [Reactive]
    public PlannerDay InitialState { get; set; } = new()
    {
        Date = DateTime.Now,
        // TODO: pull from saved prefs
        StartingPlayerLevel = 1,
        StartingPlayerExp = 0,
        BankedSanityValue = 30,
    };

    [Reactive] public string TargetStageCode { get; set; } = "AP-5";

    #region Time
    [Reactive] public TimeOnly ServerReset { get; set; } = new(4, 0);
    [Reactive] public int ServerTimezone { get; set; } = -7;
    [Reactive] public DateTime CurrentServerTime { get; private set; }
    [Reactive] public DateTime TargetDate { get; set; } = DateTime.Now.AddDays(7);
    #endregion Time

    #region Potion/sanity settings
    [Reactive] public int CurrentSanity { get; set; }

    // reoccurring
    [Reactive] public bool UseMonthlyCard { get; set; } // daily, 80 each
    [Reactive] public bool UseWeeklyPots { get; set; } = true; // 2x120 each

    // banked potions
    [Reactive] public int SmallPots { get; set; } // 10 each
    [Reactive] public int MediumPots { get; set; } // 80 each
    [Reactive] public int LargePots { get; set; } // 120 each
    [Reactive] public int ExtraSanity { get; set; }

    // extra gain/loss
    [Reactive] public int RefreshOpBudget { get; set; }
    [Reactive] public int WeeklyAnniLoss { get; set; } = 124;
    #endregion Potion/sanity settings

    public ResourcePlannerSettings()
    {
        this.WhenAnyValue(t => t.ServerTimezone)
            .Subscribe(t => CurrentServerTime = DateTime.UtcNow.AddHours(t));
    }
}
