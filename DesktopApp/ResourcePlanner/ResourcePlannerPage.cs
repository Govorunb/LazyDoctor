using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using DesktopApp.Data;
using DesktopApp.Data.Stages;
using DynamicData;
using DynamicData.Aggregation;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;

namespace DesktopApp.ResourcePlanner;

public class ResourcePlannerPage : PageBase, IValidatableViewModel
{
    public override string PageId => Constants.PlannerPageId;

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
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    // start/end for results calendar
    [Reactive]
    public DateTime StartDate { get; private set; }
    [Reactive]
    public DateTime EndDate { get; private set; }
    [Reactive]
    public DateTime SelectedDate { get; set; } = DateTime.Today;
    [Reactive]
    public PlannerDay? SelectedDay { get; private set; }
    [Reactive]
    public int TotalTargetStageRuns { get; private set; }

    [Reactive]
    private bool DayDateIsEnd { get; set; }

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

        this.WhenAnyValue(t => t.Setup!.InitialDate)
            .Subscribe(d =>
            {
                d = d.WithKind(DateTimeKind.Local);
                // FIXME: scrolling on the form field doesn't reset the time of day (eugh)
                // workaround in the ui layer (literally on scroll event)
                if (d.TimeOfDay == TimeSpan.Zero) // picked from the calendar
                    d = timeUtils.NextReset(d);
                Setup!.InitialDate = d;
            })
            .DisposeWith(this);

        CalculateCommand = ReactiveCommand.Create(Simulate, prefsLoaded);
        SetInitialDateToTodayCommand = ReactiveCommand.Create(void () => Setup!.InitialDate = DateTime.Now, prefsLoaded);

        _resultsList.Connect()
            .Bind(out _results)
            .Do(_ =>
            {
                // the dates have two patterns depending on where the initial day falls relative to the reset
                // the premise is that intuitively:
                // - the initial date ("now") should be "today"
                //   (i.e. if the form field shows May 1st, the results calendar should start from May 1st as well)
                // - each day on the calendar has one and only one planner "day"
                // usually "the date" of a planner day is when the day starts - but if "now" is before today's reset, the second day would have the same date
                // so "the date" is made to be the end instead (you do end up with the same number of days in both cases)
                if (_results is not [var firstDay, ..])
                    return;
                DayDateIsEnd = firstDay.Start.Date == firstDay.End.Date;
                StartDate = firstDay.Start.Date;
                var endDay = _results.Last();
                EndDate = (DayDateIsEnd ? endDay.End : endDay.Start).Date;
                SelectedDate = firstDay.Start.Date;
            })
            .Sum(d => d.TargetStageCompletions)
            .Subscribe(runs => TotalTargetStageRuns = runs);

        this.WhenAnyValue(t => t.SelectedDate)
            .Select(d => DayDateIsEnd
                ? _results.FirstOrDefault(d2 => d2.End.Date == d.Date)
                : _results.FirstOrDefault(d2 => d2.Start.Date == d.Date))
            .Subscribe(d => SelectedDay = d);

        this.ValidationRule(t => t.Setup!.TargetStageCode,
            this.WhenAnyValue(t => t.Setup!.TargetStageCode)
                .ToUnit()
                .Merge(sched.StagesRepo.Values.ToUnit())
                .Select(_ => _sched.StagesRepo.GetByCode(Setup?.TargetStageCode) is { })
                .Prepend(false),
            v => v,
            _ => $"Stage '{Setup?.TargetStageCode}' not found");

        this.ValidationRule(t => t.Setup!.TargetDate,
            this.WhenAnyValue(t => t.Setup!.TargetDate, t => t.Setup!.InitialDate)
                .Select(p => p.Item1 > p.Item2),
            "Target date must be after initial date"
        );
        ValidationContext.ValidationStatusChange
            .Select(s => s.IsValid ? null : s.Text.ToSingleLine("; "))
            .Subscribe(t => Errors = t);
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
