using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;

namespace DesktopApp.ResourcePlanner;

public sealed class ResourcePlannerPage : PageBase
{
    public ResourcePlannerSettings? Setup => _prefs.Planner?.Setup;

    private readonly SourceList<PlannerDay> _resultsList = new();
    private readonly ReadOnlyObservableCollection<PlannerDay> _results;
    public ReadOnlyObservableCollection<PlannerDay> Results => _results;
    private readonly WeeklyFarmCalculator _calc;
    private readonly UserPrefs _prefs;

    public ReactiveCommand<Unit, Unit> CalculateCommand { get; }

    public ResourcePlannerPage(WeeklyFarmCalculator calc, UserPrefs prefs)
    {
        _calc = calc;
        _prefs = prefs;

        CalculateCommand = ReactiveCommand.Create(Simulate, _prefs.Loaded.Select(_ => true).Prepend(false));

        _resultsList.Connect()
            .Bind(out _results);
    }

    private void Simulate()
    {
        _resultsList.Edit(list =>
        {
            list.Clear();
            for (var day = Setup!.InitialState; day.Date < Setup.TargetDate; day = _calc.AdvanceDay(day))
            {
                list.Add(day);
            }
        });
    }
}
