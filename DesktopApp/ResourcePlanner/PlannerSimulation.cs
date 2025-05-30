using System.Diagnostics;
using DesktopApp.Data;
using DesktopApp.Data.Player;
using DesktopApp.Data.Stages;

namespace DesktopApp.ResourcePlanner;

public sealed class PlannerSimulation : ServiceBase
{
    public List<PlannerDay> Results { get; }

    private readonly ResourcePlannerSettings _setup;
    private readonly GameConstants _gameConst;
    private readonly TimeUtilsService _timeUtils;

    private readonly StageData _targetStage;
    private readonly List<DateTime> _days;
    private int _anniSanityLeft;
    private int _bankedSanity;
    private readonly DateTime _simStart;
    private readonly DateTime _simEnd;

    public int TotalTargetStageRuns => Results.Select(d => d.TargetStageCompletions).Sum();
    private int AnniStageCost => _setup.AnnihilationMap is AnnihilationMap.Chernobog ? 20 : 25;

    public PlannerSimulation(UserPrefs prefs, TimeUtilsService timeUtils, WeeklyStages sched, GameConstants gameConst)
    {
        AssertDI(prefs);
        AssertDI(prefs.General);
        AssertDI(prefs.Planner);
        AssertDI(prefs.Planner.Setup);
        AssertDI(sched);
        AssertDI(gameConst);
        _setup = prefs.Planner.Setup;
        _gameConst = gameConst;
        _timeUtils = timeUtils;

        _targetStage = sched.StagesRepo.GetByCode(_setup.TargetStageCode)
            ?? throw new InvalidOperationException($"Invalid target stage code {_setup.TargetStageCode}");

        _simStart = _setup.InitialDate.WithKind(DateTimeKind.Local);
        _simEnd = _setup.TargetDate.WithKind(DateTimeKind.Local);
        if (_simEnd < _simStart)
        {
            Results = [];
            _days = [];
            return;
        }


        // each day lasts until the next reset
        // if initial datetime is before today's reset, second day will overlap with first
        var secondDayStart = timeUtils.NextReset(_simStart);
        _bankedSanity = _setup.SmallPots * 10
                        + _setup.MediumPots * 80
                        + _setup.LargePots * 120
                        + _setup.OpBudget * 135
                        + _setup.ExtraSanity;

        _days = secondDayStart.Range(_simEnd, TimeSpan.FromDays(1))
            .Prepend(_simStart).ToList();
        Results = new List<PlannerDay>(_days.Count);
        for (var i = 0; i < _days.Count-1; i++)
        {
            var start = _days[i];
            Results.Add(new()
            {
                Start = start,
                End = start.AddDays(1).AddSeconds(-1),
                IsTargetStageOpen = sched.IsOpen(_setup.TargetStageCode, start),
            });
        }

        var initialDay = Results[0];
        initialDay.StartingSanityValue = _setup.CurrentSanity;
        initialDay.StartingExpData = _setup.InitialExpData;
        initialDay.End = secondDayStart.AddSeconds(-1);
        SimulateFrom(0);
    }

    public PlannerDay? GetDay(DateTime date)
    {
        var i = GetDayIndex(date);
        if (i < 0 || i >= Results.Count)
            return null;
        return Results[i];
    }

    private int GetDayIndex(DateTime date) => (date - _simStart).Days;

    public void SimulateFrom(PlannerDay fromDay)
        => SimulateFrom(fromDay.Start);

    public void SimulateFrom(DateTime fromDate)
        => SimulateFrom(GetDayIndex(fromDate));

    private void SimulateFrom(int dayIdx)
    {
        // starting from a given day so that if some day is manually modified we can rerun the calculation for the following days
        for (var i = dayIdx; i < Results.Count; i++)
        {
            var day = Results[i];
            day.TargetStageCompletions = 0;
            day.FinishSanityValue = 0;
            SimulateDay(i);
        }
    }

    private void SimulateDay(int day)
    {
        if (day >= Results.Count)
            return;
        var today = Results[day];
        var tomorrow = day + 1 < Results.Count ? Results[day+1] : null;

        if (day > 0)
        {
            var yesterday = Results[day - 1];
            today.StartingExpData = yesterday.FinishExpData;
            today.StartingSanityValue = yesterday.FinishSanityValue;
        }

        var sanLog = today.SanityLog;
        var natRegen = _setup.DailySanityRegenEfficiency;
        if (day == 0)
        {
            // first day is partial
            var part = (_days.ElementAt(1) - _days.First()).TotalDays;
            natRegen = (int)Math.Floor(natRegen * part);
        }
        if (today.StartingSanityValue > 0)
            sanLog.Log(today.StartingSanityValue, "Starting sanity");
        sanLog.Log(natRegen, "Natural regen");
        var expData = new PlayerExpData(today.StartingExpData);
        if (_setup.UseMonthlyCard)
        {
            sanLog.Log(0, "Daily potion from monthly card (+80 banked)");
            _bankedSanity += 80;
        }

        var serverDayOfWeek = _timeUtils.ToServerTime(today.Start).DayOfWeek;
        if (serverDayOfWeek == System.DayOfWeek.Monday)
        {
            _anniSanityLeft = _setup.WeeklyAnniLoss;
            if (_setup.UseWeeklyPots)
            {
                sanLog.Log(0, "Weekly potions (+240 banked)", "Weekly missions can be completed on Monday using zero-cost stages like OF-1");
                _bankedSanity += 240;
            }
        }

        var loops = 0;
        var repeat = false;
        do
        {
            if (loops++ > 5)
            {
                this.Log().Error("Something's wrong...");
                Debugger.Break();
            }
            var repeating = repeat;
            repeat = false;
            SimForcedAnni();

            if (today.IsTargetStageOpen)
            {
                SimOpenDay(repeating);
            }
            else
            {
                SimClosedDay(repeating);
            }

            var surplus = sanLog.CurrentValue;
            if (surplus > 0 && _anniSanityLeft > 0)
            {
                int used;
                if (sanLog.CurrentValue >= _anniSanityLeft)
                {
                    used = _anniSanityLeft; // last run with partial refund
                }
                else
                {
                    var maxUsed = Math.Min(_anniSanityLeft, sanLog.CurrentValue);
                    CalculateRuns(maxUsed, AnniStageCost, out used);
                }
                sanLog.Log(-used, "Annihilation");
                _anniSanityLeft -= used;
                GainExp(used * 10); // anni is always 10x
            }

            surplus = sanLog.CurrentValue;
            if (surplus > 0)
            {
                sanLog.Log(-surplus, "Surplus", "Spend this on any stage to get EXP");
                if (GainExp(surplus * 12))
                    repeat = true;
            }
        } while (repeat);

        today.FinishExpData = expData;
        return;

        void SimForcedAnni()
        {
            // prefer to do anni on closed days; saving sanity for tomorrow generally takes prio
            // anni is forced:
            // - on Sundays (for obvious reasons)
            // - when the rest of the week is all open (e.g. if target stage is open all week due to CC)
            //   - except if it's the last week and the target date is before Sunday, meaning anni can be done after farming ends
            if (_anniSanityLeft <= 0)
                return;

            var isSunday = serverDayOfWeek == System.DayOfWeek.Sunday;
            var noMoreClosedDays = false;

            if (!isSunday)
            {
                var restOfWeek = Results.Skip(day + 1)
                    .TakeWhile(d => _timeUtils.ToServerTime(d.Start).DayOfWeek != System.DayOfWeek.Monday)
                    .ToList();
                // if not, we also know it's the last week (not-last weeks always end on sunday because there's at least one more week)
                var lastDayIsSunday = restOfWeek.Count > 0 && _timeUtils.ToServerTime(restOfWeek[^1].Start).DayOfWeek == System.DayOfWeek.Sunday;
                noMoreClosedDays = lastDayIsSunday && restOfWeek.All(d => d.IsTargetStageOpen);
            }

            var forceAnni = isSunday || noMoreClosedDays;
            if (!forceAnni)
                return;

            sanLog.Log(-_anniSanityLeft,
                "Annihilation (forced)",
                isSunday ? "Last day before anni resets"
                    : $"Farming {_targetStage.Code} on all remaining days of the week");
            GainExp(_anniSanityLeft * 10); // anni is always 10x
            _anniSanityLeft = 0;
        }

        void SimOpenDay(bool repeating)
        {
            // banked sanity is used aggressively as early as possible
            // the earlier you level up, the more days you have with any possible extra sanity cap
            // e.g. leveling up from 149 cap to 150 is massive for 30san stage runs (from max of 4 extra runs with saved sanity to 5)
            // (obviously using banked sanity on closed days is a complete waste)
            if (_bankedSanity > 0)
            {
                // there might be small opts to be had here by fully simulating an inventory
                // e.g. op is +135, can't cleanly consume 120 without tacking on the extra "wasted" 15; medium pots also suffer a bit with 30san stages
                // i'm choosing to leave these on the table due to complexity
                // at least we don't have to worry about expiration :)
                sanLog.Log(_bankedSanity, "Banked sanity", "Potions, OP, cakes, etc");
                _bankedSanity = 0;
            }

            SpendSanityOnTargetStage();

            Debug.Assert(sanLog.CurrentValue < _targetStage.SanityCost, "spent sanity on target stage but still have enough for more runs");

            // it's better to have leftovers maybe go towards tomorrow's runs than surplus
            // (only if gains don't cleanly divide into target stage cost;
            // otherwise the surplus would be the same every day, meaning it's not actually "saved")
            var prevSaved = repeating ? today.FinishSanityValue : 0;
            var saved = sanLog.CurrentValue;
            if (tomorrow?.IsTargetStageOpen == true && prevSaved + saved > 0)
            {
                today.FinishSanityValue = prevSaved + saved;
                sanLog.Log(prevSaved-saved, prevSaved == 0 ? "Saved for tomorrow" : "Adjust saved sanity");
            }
        }

        void SimClosedDay(bool repeating)
        {
            if (tomorrow?.IsTargetStageOpen != true)
                return;

            // save some sanity for tomorrow (unless today is the last day)
            // subtracting it first makes things a lot easier
            // technically, there's an edge case where you could:
            // - spend sanity as surplus
            // - level up from it (where, if saved, you otherwise wouldn't)
            // - still have enough to save the full amount, but now you might have 1 higher sanity cap or something
            // but in this case you still get more value (runs) out of leveling up tomorrow instead of today

            // grrrr the edge cases around saving sanity are really annoying
            // TODO: it might actually be more sanity efficient to overcap if it saves a levelup for tomorrow's open stage

            // deals with this edge case:
            // - cap 149, save 120
            // - *unavoidable* level up (e.g. from anni)
            // - cap now 150, can save 150 instead of 120
            //   -> saved amount needs to be adjusted
            var prevSaved = repeating ? today.FinishSanityValue : 0;

            // multiple of target stage cost, up to sanity cap
            var maxSaved = Math.Min(_gameConst.GetMaxSanity(expData.Level), sanLog.CurrentValue + prevSaved);
            CalculateRuns(maxSaved, _targetStage.SanityCost, out var saved);
            if (saved != prevSaved)
            {
                today.FinishSanityValue = saved;
                sanLog.Log(prevSaved-saved, prevSaved == 0 ? "Saved for tomorrow" : "Adjust saved sanity");
            }
        }

        // ReSharper disable once UnusedLocalFunctionReturnValue // idk might use it
        bool SpendSanityOnTargetStage()
        {
            var hadLevelups = false;
            while (true)
            {
                var runs = CalculateRuns(sanLog.CurrentValue, _targetStage.SanityCost, out var spent);
                if (runs == 0)
                    return hadLevelups;
                sanLog.Log(-spent, $"Run {_setup.TargetStageCode} {runs} times");
                today.TargetStageCompletions += runs;

                hadLevelups |= GainExp(spent * 12);
            }
        }

        /// <returns>Whether the EXP gain resulted in any levelups.</returns>
        bool GainExp(int exp)
        {
            var newExp = _gameConst.AddExp(expData, exp);
            var levelups = newExp.Level - expData.Level;
            expData = newExp;
            if (levelups == 0)
                return false;
            LevelUp(levelups);
            return true;
        }

        void LevelUp(int levelups)
        {
            if (levelups <= 0)
                return;
            var sanFromLevels = 0;
            var level = expData.Level - levelups;
            for (var i = 0; i < levelups; i++)
            {
                var newCap = _gameConst.GetMaxSanity(++level);
                // every levelup is (new cap − 45) sanity
                sanFromLevels += newCap - 45;
            }
            sanLog.Log(sanFromLevels,
                $"Level up{(levelups == 1 ? "" : $" {levelups} times")}",
                "Each levelup restores (new cap − 45) sanity");
        }
    }

    private static int CalculateRuns(int san, int cost, out int sanitySpent)
    {
        var (runs, rem) = Math.DivRem(san, cost);
        sanitySpent = san - rem;
        return runs;
    }
}
