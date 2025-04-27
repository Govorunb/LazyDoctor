using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using DesktopApp.Data;
using DesktopApp.Data.Stages;
using DesktopApp.Utilities.Helpers;
using DynamicData;
using DynamicData.Aggregation;

namespace DesktopApp.ResourcePlanner;

public class ResourcePlannerPage : PageBase
{
    private readonly WeeklyStages _sched;
    private readonly UserPrefs _prefs;
    private readonly TimeUtilsService _timeUtils;

    public ResourcePlannerPrefsData? Prefs => _prefs.Planner;
    public ResourcePlannerSettings? Setup => Prefs?.Setup;
    [Reactive]
    internal GameConstants? GameConst { get; private set; }

    private readonly SourceList<PlannerDay> _resultsList = new();
    private readonly ReadOnlyObservableCollection<PlannerDay> _results;
    public ReadOnlyObservableCollection<PlannerDay> Results => _results;

    [Reactive]
    public string? Errors { get; private set; }

    [Reactive]
    public DateTime SelectedDate { get; set; } = DateTime.Today; // otherwise, calendar has incorrect bounds (selects 0001-Jan-01 so the minimum extends down to there)
    [Reactive]
    public PlannerDay? SelectedDay { get; private set; }
    [Reactive]
    public int TotalTargetStageRuns { get; private set; }

    internal ReadOnlyObservableCollection<string> StageCodes => _sched.StageCodes;
    public ReactiveCommand<Unit, Unit> CalculateCommand { get; }
    public ReactiveCommand<Unit, Unit> SetInitialDateToTodayCommand { get; }

    public ResourcePlannerPage(WeeklyStages sched, IDataSource<GameConstants> gameConstSrc, UserPrefs prefs, TimeUtilsService timeUtils)
    {
        AssertDI(sched);
        AssertDI(gameConstSrc);
        AssertDI(prefs);
        AssertDI(timeUtils);
        _sched = sched;
        _prefs = prefs;
        _timeUtils = timeUtils;

        gameConstSrc.Values
            .Subscribe(c => GameConst = c)
            .DisposeWith(this);

        var prefsLoaded = _prefs.Loaded
            .Select(_ => true).Prepend(false)
            .Replay().AutoConnect();
        this.NotifyProperty(nameof(Prefs), _prefs.Loaded).DisposeWith(this);
        this.NotifyProperty(nameof(Setup), _prefs.Loaded).DisposeWith(this);
        prefsLoaded
            .Do(_ => Setup?.RaisePropertyChanged(nameof(Setup.InitialDate)))
            .Subscribe(_ => _resultsList.Reset(Prefs?.Results ?? []))
            .DisposeWith(this);

        // picked from the calendar
        this.WhenAnyValue(t => t.Setup!.InitialDate)
            .Where(d => d.TimeOfDay == TimeSpan.Zero)
            .Subscribe(d => Setup!.InitialDate = timeUtils.NextReset(d))
            .DisposeWith(this);

        CalculateCommand = ReactiveCommand.Create(Simulate, prefsLoaded);
        SetInitialDateToTodayCommand = ReactiveCommand.Create(void () => Setup!.InitialDate = DateTime.Now, prefsLoaded);

        _resultsList.Connect()
            .Bind(out _results)
            .Sum(d => d.TargetStageCompletions)
            .Subscribe(runs => TotalTargetStageRuns = runs);

        // should only change when recalculated
        // this.WhenAnyValue(t => t.Setup!.InitialDate, t => t.Setup!.TargetDate)
        //     .Select(pair => SelectedDate.Clamp(pair.Item1, pair.Item2))
        //     .BindTo(this, t => t.SelectedDate);

        // the dates have two patterns depending on where the initial day falls relative to the reset
        // the point is that intuitively the initial date ("now") should be "today"
        // (i.e. if the form field shows May 1st, the results calendar should start from May 1st as well)
        // usually "the date" of a planner day is when the day starts
        // but if "now" is before today's reset, the second day would overlap the first - so "the date" is made to be the end instead
        // (you do end up with the same number of days in both cases)
        this.WhenAnyValue(t => t.SelectedDate)
            .Select(d =>
            {
                if (Prefs is null)
                    return null;
                var days = Prefs.Results;
                var firstDay = days.First();
                var firstIsBeforeReset = firstDay.Start.Date == firstDay.End.Date;
                return firstIsBeforeReset
                    ? days.FirstOrDefault(d2 => d2.End.Date == d.Date)
                    : days.FirstOrDefault(d2 => d2.Start.Date == d.Date);
            }).Subscribe(d => SelectedDay = d);

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

        var sim = new PlannerSimulation(_prefs, _timeUtils, _sched, GameConst);
        _resultsList.Reset(Prefs.Results = sim.Results);
        SelectedDate = Setup.InitialDate;
    }
}

internal sealed class DesignResourcePlannerPage()
    : ResourcePlannerPage(
        LOCATOR.GetService<WeeklyStages>()!,
        LOCATOR.GetService<IDataSource<GameConstants>>()!,
        LOCATOR.GetService<UserPrefs>()!,
        LOCATOR.GetService<TimeUtilsService>()!
);
