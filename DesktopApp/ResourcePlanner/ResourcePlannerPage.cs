using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using DesktopApp.Data;
using DesktopApp.Data.Stages;
using DesktopApp.Utilities.Helpers;
using DynamicData;

namespace DesktopApp.ResourcePlanner;

public class ResourcePlannerPage : PageBase
{
    private readonly WeeklyStages _sched;
    private readonly IDataSource<GameConstants> _gameConstSrc;
    private readonly UserPrefs _prefs;

    public ResourcePlannerPrefsData? Prefs => _prefs.Planner;
    public ResourcePlannerSettings? Setup => Prefs?.Setup;
    private GameConstants? GameConst { get; set; }

    private readonly SourceList<PlannerDay> _resultsList = new();
    private readonly ReadOnlyObservableCollection<PlannerDay> _results;
    public ReadOnlyObservableCollection<PlannerDay> Results => _results;

    [Reactive]
    public string? Errors { get; private set; }

    [Reactive]
    public DateTime SelectedDate { get; set; } = DateTime.Today; // otherwise, calendar has incorrect bounds (selects 0001-Jan-01 so the minimum extends down to there)
    [Reactive]
    public PlannerDay? SelectedDay { get; private set; }

    internal ReadOnlyObservableCollection<string> StageCodes => _sched.StageCodes;
    public ReactiveCommand<Unit, Unit> CalculateCommand { get; }
    public ReactiveCommand<Unit, Unit> SetInitialDateToTodayCommand { get; }

    public ResourcePlannerPage(WeeklyStages sched, IDataSource<GameConstants> gameConstSrc, UserPrefs prefs)
    {
        AssertDI(sched);
        AssertDI(gameConstSrc);
        AssertDI(prefs);
        _sched = sched;
        _gameConstSrc = gameConstSrc;
        _prefs = prefs;

        _gameConstSrc.Values
            .BindTo(this, t => t.GameConst)
            .DisposeWith(this);

        var prefsLoaded = _prefs.Loaded
            .Select(_ => true).Prepend(false)
            .Replay().AutoConnect();
        this.NotifyProperty(nameof(Prefs), _prefs.Loaded);
        this.NotifyProperty(nameof(Setup), _prefs.Loaded);
        prefsLoaded
            .Do(_ => Setup?.RaisePropertyChanged(nameof(Setup.InitialDate)))
            .Subscribe(_ => _resultsList.Reset(Prefs?.Results ?? []))
            .DisposeWith(this);

        CalculateCommand = ReactiveCommand.Create(Simulate, prefsLoaded);
        SetInitialDateToTodayCommand = ReactiveCommand.Create(void () => Setup!.InitialDate = DateTime.Now, prefsLoaded);

        _resultsList.Connect()
            .Bind(out _results)
            .Subscribe();

        // should only change when recalculated
        // this.WhenAnyValue(t => t.Setup!.InitialDate, t => t.Setup!.TargetDate)
        //     .Select(pair => SelectedDate.Clamp(pair.Item1, pair.Item2))
        //     .BindTo(this, t => t.SelectedDate);

        this.WhenAnyValue(t => t.SelectedDate)
            .Select(d => Prefs?.Results.FirstOrDefault(d2 => d2.Date.Date == d.Date))
            .Subscribe(d => SelectedDay = d);

        this.WhenAnyValue(t => t.Prefs!.Setup.TargetStageCode)
            .ToUnit()
            .Merge(sched.StagesRepo.Values.ToUnit())
            // .Debounce(TimeSpan.FromMilliseconds(100))
            .Select(_ => _sched.StagesRepo.GetByCode(Setup?.TargetStageCode))
            .Subscribe(stage => Errors = stage is { } ? null : "Stage not found");
    }

    private void Simulate()
    {
        if (Prefs is null || GameConst is null)
            return;
        Debug.Assert(Setup is {});

        var sim = new PlannerSimulation(Setup, _sched, GameConst);
        _resultsList.Reset(Prefs.Results = sim.Results);
        SelectedDate = Setup.InitialDate;
    }
}

internal sealed class DesignResourcePlannerPage()
    : ResourcePlannerPage(
        LOCATOR.GetService<WeeklyStages>()!,
        LOCATOR.GetService<IDataSource<GameConstants>>()!,
        LOCATOR.GetService<UserPrefs>()!
);
