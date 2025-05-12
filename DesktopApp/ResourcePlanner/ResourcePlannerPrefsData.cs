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

    public bool SetupExpanded { get; set; } = true;
    public bool ResultsExpanded { get; set; }
}
