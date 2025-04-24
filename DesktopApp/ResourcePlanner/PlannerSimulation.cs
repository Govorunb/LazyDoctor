using System.Diagnostics;
using System.Text.Json.Serialization;
using DesktopApp.Data;
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
    private readonly DateRange _days;
    private int _anniSanityLeft;
    private int _bankedSanity;
    [JsonIgnore]
    public int TotalTargetStageRuns => Results.Select(d => d.TargetStageCompletions).Sum();
    private int AnniStageCost => _setup.AnnihilationMap is AnnihilationMap.Chernobog ? 20 : 25;

    public PlannerSimulation(ResourcePlannerSettings setup, WeeklyStages sched, GameConstants gameConst)
    {
        AssertDI(sched);
        AssertDI(_gameConst = gameConst);
        _gameConst = gameConst;
        _setup = setup;
        _targetStage = sched.StagesRepo.GetByCode(setup.TargetStageCode)
            ?? throw new InvalidOperationException($"Invalid target stage code {setup.TargetStageCode}");

        _days = setup.InitialDate.Range(setup.TargetDate, TimeSpan.FromDays(1));
        Results = new List<PlannerDay>(_days.Select(date => new PlannerDay
        {
            Date = date,
            IsTargetStageOpen = sched.IsOpen(setup.TargetStageCode, date),
        }));
        SimulateFrom(0);
    }

    private PlannerDay GetDay(DateTime date)
        => Results[GetDayIndex(date)];

    private int GetDayIndex(DateTime date) => (date - _setup.InitialDate).Days;

    public void SimulateFrom(PlannerDay fromDay)
        => SimulateFrom(fromDay.Date);

    public void SimulateFrom(DateTime fromDate)
        => SimulateFrom(GetDayIndex(fromDate));

    private void SimulateFrom(int day)
    {
        for (var i = day; i < Results.Count; i++)
            SimulateDay(i);
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

        if (today.TargetStageCompletions > 0 || today.FinishSanityValue > 0)
        {
            // we're re-simulating, clear current values
            today.TargetStageCompletions = 0;
            today.FinishSanityValue = 0;
        }

        var sanLog = today.SanityLog;
        var natRegen = _setup.DailySanityRegenEfficiency;
        if (today.StartingSanityValue > 0)
            sanLog.Log(today.StartingSanityValue, "Starting sanity");
        sanLog.Log(natRegen, "Natural regen");
        var (level, exp) = today.StartingExpData;
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
                sanLog.Log(0, "Weekly potions (+240 banked)", "Weekly missions can all be completed on Monday using 0san stages like OF-1");
                _bankedSanity += 240;
            }
        }

        bool repeat = false;
        do
        {
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
                var sunday = today.Date.DayOfWeek == System.DayOfWeek.Sunday;
                // normally we do it on closed days, but if there are no closed days left this week then anni takes priority over the target stage
                if (sunday || Results.Skip(day + 1).TakeWhile(d => d.Date.DayOfWeek != System.DayOfWeek.Monday).All(d => d.IsTargetStageOpen))
                {
                    sanLog.Log(-_anniSanityLeft,
                        $"Annihilation (forced - {(sunday ? "today is Sunday" : $"{_targetStage.Code} is open on all remaining days")})");
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

            if (sanLog.CurrentValue > 0)
            {
                (var newLevel, exp) = _gameConst.AddExp(today.StartingExpData, sanLog.CurrentValue * 12);
                sanLog.Log(-sanLog.CurrentValue, "Surplus", "Spend this on any stage to get EXP");
                var levelups = newLevel - level;
                if (levelups > 0)
                {
                    repeat = true;
                    LevelUp(levelups);
                }
            }
        } while (!repeat);

        today.FinishExpData = new(level, exp);
        return;

        void SimOpenDay(bool repeating)
        {
            // banked sanity is used aggressively as early as possible
            // the earlier you level up, the more days you have with any possible extra sanity cap
            // e.g. leveling up from 149 cap to 150 is massive for 30san stage runs
            // (from max of 4 extra runs with saved sanity to 5)
            if (_bankedSanity > 0)
            {
                // there might be small opts to be had here by fully simulating an inventory
                // e.g. op is +135, can't cleanly consume 120 without tacking on the extra "wasted" 15; medium pots also suffer a bit with 30san stages
                // i'm choosing to leave these on the table due to complexity
                sanLog.Log(_bankedSanity, "Banked sanity (potions, OP, etc)");
                _bankedSanity = 0;
            }

            SpendSanityOnTargetStage();

            Debug.Assert(sanLog.CurrentValue < _targetStage.SanityCost, "spent sanity on target stage but still have enough for more runs");

            // better to have leftovers maybe go towards tomorrow's runs than surplus
            // (no effect if daily gains cleanly divide into target stage cost - the surplus would be the same every day, meaning it's not actually "saved")
            // TODO investigate saving between open days
            // TODO repeating (see closed day saved calc)
            if (sanLog.CurrentValue > 0 && tomorrow?.IsTargetStageOpen == true)
            {
                today.FinishSanityValue = sanLog.CurrentValue;
                sanLog.Log(-sanLog.CurrentValue, "Saved for tomorrow");
            }
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
                var maxSaved = Math.Min(_gameConst.GetMaxSanity(level), sanLog.CurrentValue + prevSaved);
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
                if (sanLog.CurrentValue == _anniSanityLeft && _anniSanityLeft < AnniStageCost)
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
            }
        }

        // ReSharper disable once UnusedLocalFunctionReturnValue // idk might use it
        bool SpendSanityOnTargetStage()
        {
            var hadLevelups = false;
            while (true)
            {
                var runs = CalculateRuns(sanLog.CurrentValue, _targetStage.SanityCost, out var spent);
                sanLog.Log(-spent, $"Run {_setup.TargetStageCode} {runs} times");
                today.TargetStageCompletions += runs;

                (var newLevel, exp) = _gameConst.AddExp(today.StartingExpData, spent * 12);
                var levelups = newLevel - level;
                if (levelups == 0)
                    return hadLevelups;

                hadLevelups = true;
                LevelUp(levelups);
            }
        }

        void LevelUp(int levelups)
        {
            if (levelups <= 0)
                return;
            var sanFromLevels = 0;
            for (var i = 0; i < levelups; i++)
            {
                var newCap = _gameConst.GetMaxSanity(++level);
                // every levelup is (new cap - 1) sanity
                // due to momentary loss of natural regen from overcap
                sanFromLevels += newCap - 1;
            }
            sanLog.Log(sanFromLevels,
                $"Level up{(levelups == 1 ? "" : $" {levelups} times ({level-levelups}->{level})")}",
                "Each levelup restores (new cap)-1 sanity due to the momentary loss of regen from overcap");
        }
    }

    private static int CalculateRuns(int san, int cost, out int sanitySpent)
    {
        var (runs, rem) = Math.DivRem(san, cost);
        sanitySpent = san - rem;
        return runs;
    }
}
