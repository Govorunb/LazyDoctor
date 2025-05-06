using System.ComponentModel;
using DesktopApp.Data.Player;

namespace DesktopApp.ResourcePlanner;

[JsonClass]
public sealed class ResourcePlannerSettings : ViewModelBase
{
    public static readonly AnnihilationMap[] AnnihilationMaps = Enum.GetValues<AnnihilationMap>();

    [Reactive]
    public DateTime InitialDate { get; set; } = DateTime.Now;
    // TODO: dedicated prefs space for player data (stats, inventory, base rotation or something idk)
    [Reactive] public PlayerExpData InitialExpData { get; set; } = new();

    [Reactive] public string TargetStageCode { get; set; } = "";
    [Reactive] public DateTime TargetDate { get; set; } = DateTime.Today.AddDays(7);

    #region Potion/sanity settings
    [Reactive] public int CurrentSanity { get; set; }

    // reoccurring gain/loss
    [Reactive] public bool UseMonthlyCard { get; set; } // daily, 80 each
    [Reactive] public bool UseWeeklyPots { get; set; } = true; // 2x120 each
    [Reactive] public int DailySanityRegenEfficiency { get; set; } = 240; // can be manually set lower if you login once a day or something
    [Reactive] public AnnihilationMap AnnihilationMap { get; set; }
    [Reactive] public int WeeklyAnniLoss { get; private set; } = 124;

    // banked potions
    [Reactive] public int SmallPots { get; set; } // 10 each
    [Reactive] public int MediumPots { get; set; } // 80 each
    [Reactive] public int LargePots { get; set; } // 120 each
    [Reactive] public int OpBudget { get; set; } // 135 each
    [Reactive] public int ExtraSanity { get; set; }

    #endregion Potion/sanity settings

    public ResourcePlannerSettings()
    {
        this.WhenAnyValue(t => t.AnnihilationMap)
            .Select(GetAnnihilationLoss)
            .BindTo(this, t => t.WeeklyAnniLoss);
    }

    public static int GetAnnihilationLoss(AnnihilationMap map)
    {
        return map switch
        {
            AnnihilationMap.CurrentRotating => 124,
            AnnihilationMap.Chernobog => 139,
            AnnihilationMap.Outskirts => 140,
            AnnihilationMap.Downtown => 133,
            _ => throw new ArgumentOutOfRangeException(nameof(map), map, "Unknown annihilation map"),
        };
    }
}

public enum AnnihilationMap
{
    [DisplayName("Current rotating"), Description("The currently available rotating map")]
    CurrentRotating,
    Chernobog,
    [DisplayName("Lungmen Outskirts")]
    Outskirts,
    [DisplayName("Lungmen Downtown")]
    Downtown,
}
