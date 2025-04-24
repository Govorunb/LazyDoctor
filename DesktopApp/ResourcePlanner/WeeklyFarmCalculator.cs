using System.Collections.ObjectModel;
using System.Diagnostics;
using DesktopApp.Data;
using DesktopApp.Data.Stages;
using JetBrains.Annotations;

namespace DesktopApp.ResourcePlanner;

[PublicAPI]
public class WeeklyFarmCalculator(WeeklyStages sched, GameConstants gameConst) : ServiceBase
{
    public ResourcePlannerSettings Settings { get; } = new();

    // for autocomplete
    public ReadOnlyObservableCollection<string> StageCodes => sched.StageCodes;

    public PlannerSimulation? Simulate(ResourcePlannerSettings? setup)
        => setup is null ? null
            : new(setup, sched, gameConst);
}
