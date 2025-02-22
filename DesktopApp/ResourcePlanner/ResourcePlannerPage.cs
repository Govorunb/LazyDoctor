namespace DesktopApp.ResourcePlanner;

public class ResourcePlannerPage : PageBase
{
    [Reactive]
    public ResourcePlannerSettings Setup { get; set; } = new();
}
