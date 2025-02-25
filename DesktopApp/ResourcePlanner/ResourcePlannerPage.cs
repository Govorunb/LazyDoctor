using System.Collections.ObjectModel;
using DynamicData;

namespace DesktopApp.ResourcePlanner;

public sealed class ResourcePlannerPage : PageBase
{
    [Reactive]
    public ResourcePlannerSettings Setup { get; set; } = new();

    private readonly SourceList<PlannerDay> _resultsList = new();
    private readonly ReadOnlyObservableCollection<PlannerDay> _results;
    public ReadOnlyObservableCollection<PlannerDay> Results => _results;

    public ResourcePlannerPage(WeeklyFarmCalculator calc)
    {
        _resultsList.Connect()
            .Bind(out _results);
    }
}
