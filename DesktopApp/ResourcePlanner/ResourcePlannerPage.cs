using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using DesktopApp.Utilities.Helpers;
using DynamicData;

namespace DesktopApp.ResourcePlanner;

public class ResourcePlannerPage : PageBase
{
    private readonly WeeklyFarmCalculator _calc;
    private readonly UserPrefs _prefs;

    public ResourcePlannerPrefsData? Prefs => _prefs.Planner;
    public ResourcePlannerSettings? Setup => Prefs?.Setup;
    private readonly SourceList<PlannerDay> _resultsList = new();
    private readonly ReadOnlyObservableCollection<PlannerDay> _results;
    public ReadOnlyObservableCollection<PlannerDay> Results => _results;

    internal ReadOnlyObservableCollection<string> StageCodes => _calc.StageCodes;
    public ReactiveCommand<Unit, Unit> CalculateCommand { get; }
    public ReactiveCommand<Unit, Unit> SetInitialDateToTodayCommand { get; }

    public ResourcePlannerPage(WeeklyFarmCalculator calc, UserPrefs prefs)
    {
        AssertDI(calc);
        AssertDI(prefs);
        _calc = calc;
        _prefs = prefs;

        var prefsLoaded = _prefs.Loaded
            .Select(_ => true).Prepend(false)
            .Replay().AutoConnect();
        this.NotifyProperty(nameof(Prefs), _prefs.Loaded);
        this.NotifyProperty(nameof(Setup), _prefs.Loaded);
        prefsLoaded.Subscribe(_ => _resultsList.Reset(Prefs?.Results ?? []));

        CalculateCommand = ReactiveCommand.Create(Simulate, prefsLoaded);
        SetInitialDateToTodayCommand = ReactiveCommand.Create(void () => Setup!.InitialDate = DateTime.Now, prefsLoaded);

        _resultsList.Connect()
            .Bind(out _results)
            .Subscribe();
    }

    private void Simulate()
    {
        Debug.Assert(Prefs is { });
        _resultsList.Reset(Prefs.Results = _calc.Simulate(Setup).ToList());
    }
}

internal sealed class DesignResourcePlannerPage()
    : ResourcePlannerPage(
        LOCATOR.GetService<WeeklyFarmCalculator>()!,
        LOCATOR.GetService<UserPrefs>()!
);
