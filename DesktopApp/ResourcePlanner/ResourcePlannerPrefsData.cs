using DesktopApp.Utilities.Attributes;
using DesktopApp.Utilities.Helpers;

namespace DesktopApp.ResourcePlanner;

[JsonClass]
public class ResourcePlannerPrefsData : ReactiveObjectBase
{
    [Reactive]
    public ResourcePlannerSettings Setup { get; set; } = new();
    [Reactive]
    public List<PlannerDay> Results { get; set; } = [];

    public ResourcePlannerPrefsData()
    {
        this.NotifyProperty(nameof(Setup), Setup.Changed);
    }

    // TODO: ui state
    public bool SetupExpanded { get; set; }
    public bool ResultsExpanded { get; set; }
}
