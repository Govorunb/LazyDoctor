using DesktopApp.Utilities.Attributes;

namespace DesktopApp.ResourcePlanner;

[JsonClass]
public class ResourcePlannerPrefsData
{
    public ResourcePlannerSettings Setup { get; set; } = new();
    public List<PlannerDay> Results { get; set; } = [];

    // TODO: ui state
}
