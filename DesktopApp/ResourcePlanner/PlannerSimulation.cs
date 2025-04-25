using System.Diagnostics;
using System.Text.Json.Serialization;
using DesktopApp.Data;
using DesktopApp.Data.Player;
using DesktopApp.Data.Stages;
using DesktopApp.Utilities.Attributes;

namespace DesktopApp.ResourcePlanner;

[JsonClass]
public sealed class PlannerSimulation : ReactiveObjectBase
{
    public List<PlannerDay> Results { get; }

    private readonly ResourcePlannerSettings _setup;
    private readonly GameConstants _gameConst;

    private readonly StageData _targetStage;
    private readonly IEnumerable<DateTime> _days;
    private int _anniSanityLeft;
    private int _bankedSanity;
    [JsonIgnore]
    public int TotalTargetStageRuns => Results.Select(d => d.TargetStageCompletions).Sum();
    private int AnniStageCost => _setup.AnnihilationMap is AnnihilationMap.Chernobog ? 20 : 25;

    public PlannerSimulation(ResourcePlannerSettings setup, WeeklyStages sched, GameConstants gameConst)
    {
        AssertDI(setup);
        AssertDI(sched);
        AssertDI(gameConst);
        _gameConst = gameConst;
        _setup = setup;
        _targetStage = sched.StagesRepo.GetByCode(setup.TargetStageCode)
            ?? throw new InvalidOperationException($"Invalid target stage code {setup.TargetStageCode}");

        // dates (after initial) should all be at midnight (or maybe local reset time - TODO)
        // this is so the first day will have a proportion of the daily nat regen
        var secondDay = setup.InitialDate.AddDays(1).Date;
        _days = secondDay.Range(setup.TargetDate, TimeSpan.FromDays(1))
            .Prepend(setup.InitialDate);
        Results = new List<PlannerDay>(_days.Select(date => new PlannerDay
        {
            Date = date,
            IsTargetStageOpen = sched.IsOpen(setup.TargetStageCode, date),
        }));
        SimulateFrom(0);
    }

    public PlannerDay? GetDay(DateTime date)
    {
        var i = GetDayIndex(date);
        if (i < 0 || i >= Results.Count)
            return null;
        return Results[i];
    }

    private int GetDayIndex(DateTime date) => (date - _setup.InitialDate).Days;

    public void SimulateFrom(PlannerDay fromDay)
        => SimulateFrom(fromDay.Date);

    public void SimulateFrom(DateTime fromDate)
        => SimulateFrom(GetDayIndex(fromDate));

    private void SimulateFrom(int dayIdx)
    {
        // starting from a given day so that if some day is manually modified we can rerun the calculation for the following days
        for (var i = dayIdx; i < Results.Count; i++)
        {
            var day = Results[dayIdx];
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
        var yesterday = day > 0 ? Results[day - 1] : null;
        var tomorrow = day + 1 < Results.Count ? Results[day+1] : null;

        today.StartingExpData = yesterday?.FinishExpData ?? _setup.InitialExpData;
        today.StartingSanityValue = yesterday?.FinishSanityValue ?? _setup.CurrentSanity;

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

        if (today.Date.DayOfWeek == System.DayOfWeek.Monday)
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
            // anni is done on closed days
            // e.g.:
            // - today is Monday (closed), Tuesday will be open; san cap is 180
            // +240 regen
            // -180 saved (takes prio)
            // -50 used for anni
            // -10 surplus (waste)
            // saving takes prio except in one single case: when the rest of the week is all open
            // e.g. if target stage is open all week due to CC, just do anni immediately on Monday
            if (_anniSanityLeft > 0)
            {
                // anni *must* be done by end of week
                var isSunday = today.Date.DayOfWeek == System.DayOfWeek.Sunday;
                // normally we do it on closed days, but if there are no closed days left this week then anni takes priority over the target stage
                if (isSunday || Results.Skip(day + 1).TakeWhile(d => d.Date.DayOfWeek != System.DayOfWeek.Monday).All(d => d.IsTargetStageOpen))
                {
                    sanLog.Log(-_anniSanityLeft,
                        $"Annihilation (forced - {(isSunday ? "today is Sunday" : $"{_targetStage.Code} is open on all remaining days")})");
                    GainExp(_anniSanityLeft * 10); // anni is always 10x
                    _anniSanityLeft = 0;
                }
            }

            if (today.IsTargetStageOpen)
            {
                SimOpenDay(repeating);
            }
            else
            {
                SimClosedDay(repeating);
            }

            var surplus = sanLog.CurrentValue;
            if (surplus > 0)
            {
                sanLog.Log(-surplus, "Surplus", "Spend this on any stage to get EXP");
                if (GainExp(surplus * 12))
                    repeat = true;
            }
        } while (repeat);

        today.FinishExpData = expData;
        return;

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
                sanLog.Log(_bankedSanity, "Banked sanity (potions, OP, etc)");
                _bankedSanity = 0;
            }

            SpendSanityOnTargetStage();

            Debug.Assert(sanLog.CurrentValue < _targetStage.SanityCost, "spent sanity on target stage but still have enough for more runs");

            // TODO investigate saving between open days
            // might be better to have leftovers maybe go towards tomorrow's runs than surplus
            // (no effect if daily gains cleanly divide into target stage cost - the surplus would be the same every day, meaning it's not actually "saved")

            // var prevSaved = repeating ? today.FinishSanityValue : 0;
            // var saved = sanLog.CurrentValue;
            // if (tomorrow?.IsTargetStageOpen == true && prevSaved + saved > 0)
            // {
            //     today.FinishSanityValue = prevSaved + saved;
            //     sanLog.Log(prevSaved-saved, prevSaved == 0 ? "Saved for tomorrow" : "Adjust saved sanity");
            // }
        }

        void SimClosedDay(bool repeating)
        {
            // save some sanity for tomorrow (unless today is the last day)
            // subtracting it first makes things a lot easier
            // teeeeechnically, there's an edge case where you could:
            // - spend sanity as surplus
            // - level up from it (where, if saved, you otherwise wouldn't)
            // - still have enough to save the full amount, but now you might have 1 higher sanity cap or something
            // but in this case you'll get more value out of leveling up tomorrow instead of today
            if (tomorrow?.IsTargetStageOpen == true)
            {
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
                    sanLog.Log(prevSaved-saved, prevSaved == 0 ? "Saved for tomorrow" : "Adjust saved sanity due to cap increase");
                }
            }

            if (_anniSanityLeft > 0)
            {
                int used;
                if (sanLog.CurrentValue >= _anniSanityLeft)
                {
                    used = _anniSanityLeft; // last run with partial refund
                }
                else
                {
                    var maxUsed = Math.Min(_anniSanityLeft, sanLog.CurrentValue);
                    var anniRuns = CalculateRuns(maxUsed, AnniStageCost, out used);
                    sanLog.Log(0, $"(debug) Anni runs: {anniRuns} (used {used} sanity)");
                }
                sanLog.Log(-used, "Annihilation");
                _anniSanityLeft -= used;
                GainExp(used * 10); // anni is always 10x
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
                // every levelup is (new cap - 45) sanity
                // you also lose at least 1 more due to momentary loss of natural regen from overcap
                // technically... if you're level 1 and you level up more than once in 6 minutes you still only lose 1 sanity...
                sanFromLevels += newCap - 46;
            }
            sanLog.Log(sanFromLevels,
                $"Level up{(levelups == 1 ? "" : $" {levelups} times")}",
                "Each levelup restores (new cap-45) sanity; you also lose 1 more due to the momentary loss of regen from overcap");
        }
    }

    private static int CalculateRuns(int san, int cost, out int sanitySpent)
    {
        var (runs, rem) = Math.DivRem(san, cost);
        sanitySpent = san - rem;
        return runs;
    }
}
