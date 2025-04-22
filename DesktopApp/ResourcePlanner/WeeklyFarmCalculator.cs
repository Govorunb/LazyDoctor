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

    public IEnumerable<PlannerDay> Simulate(ResourcePlannerSettings? setup)
    {
        if (setup is null)
            yield break;
        yield break;
    }

    public PlannerDay AdvanceDay(PlannerDay yesterday)
    {
        var today = yesterday.Date.AddDays(1);
        var tomorrow = today.AddDays(1);
        var day = new PlannerDay
        {
            Date = today,
            StartingExpData = yesterday.FinishExpData,
            StartingSanityValue = yesterday.FinishSanityValue,
            IsTargetStageOpen = sched.IsOpen(Settings.TargetStageCode, today),
        };
        var sanLog = day.SanityLog;
        var natRegen = Settings.DailySanityRegenEfficiency;
        sanLog.Log(yesterday.SanityLog.CurrentValue, "Banked sanity");
        sanLog.Log(natRegen, "Natural regen");
        var (level, exp) = day.StartingExpData;
        var targetStage = sched.StagesRepo.GetByCode(Settings.TargetStageCode);
        Debug.Assert(targetStage is {}, $"No target stage found with code {Settings.TargetStageCode}");
        // simulate inventory?
        // e.g. op is +135, can't cleanly consume 120 without tacking on the extra "wasted" 15
        // since the optimal strategy is to dump all banked sanity immediately, this is not a problem
        if (Settings.UseMonthlyCard)
            sanLog.Log(80, "Daily potion from monthly card");
        if (Settings.UseWeeklyPots && today.DayOfWeek == System.DayOfWeek.Monday)
            sanLog.Log(240, "Weekly potions");

        if (day.IsTargetStageOpen)
        {
            int levelups;
            do
            {
                var runs = RunTargetStage(sanLog.CurrentValue, out var spent);
                sanLog.Log(-spent, $"Run {Settings.TargetStageCode} {runs} times");
                day.TargetStageCompletions += runs;
                (var newLevel, exp) = gameConst.AddExp(day.StartingExpData, spent * 12);
                levelups = newLevel - level;

                for (var i = 0; i < levelups; i++)
                {
                    level += 1;
                    var newCap = gameConst.GetMaxSanity(level);
                    // every levelup is (new cap - 1) sanity
                    // due to momentary loss of natural regen from overcap
                    sanLog.Log(newCap - 1, "Level up");
                }
            } while (levelups > 0);

            // TODO: precompute
            // loop over following days until target date
            bool isLast = true;
            for (var d = tomorrow; d <= Settings.TargetDate; d = d.AddDays(1))
            {
                if (sched.IsOpen(Settings.TargetStageCode, d))
                {
                    isLast = false;
                    break;
                }
            }
        }
        else
        {
            // save some sanity for tomorrow (unless today is the last day)
            // doing it first makes things a lot easier
            if (tomorrow <= Settings.TargetDate && sched.IsOpen(Settings.TargetStageCode, tomorrow))
            {
                // (multiple of target stage cost, up to player cap)
                // TODO: spending surplus can level up...
                // just calc twice, first with X exp and then with X-(max san gained from levelups)
                // 1 sanity can make a difference in levelups once but not twice so two passes are enough
                var maxSaved = Math.Min(gameConst.GetMaxSanity(level) - 1, sanLog.CurrentValue);
                RunTargetStage(maxSaved, out var saved);
                day.FinishSanityValue = saved;
                sanLog.Log(-saved, "Saved for tomorrow (target stage will be open)");
            }
            // TODO: anni (done on closed days; if open all week, then immediately on Monday)
            // e.g. Monday closed, Tuesday open; -124 total but we have to save 180 for Tuesday -> use 50 and waste 10
            // takes prio over saving in one single case: today closed, rest of week is open, can't save full 180
        }

        day.FinishExpData = new(level, exp);

        return day;

        int RunTargetStage(int san, out int sanitySpent)
        {
            var (runs, rem) = Math.DivRem(san, targetStage.SanityCost);
            sanitySpent = san - rem;
            return runs;
        }
    }
}
