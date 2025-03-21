using System.Diagnostics;
using DesktopApp.Data;
using DesktopApp.Data.Stages;
using JetBrains.Annotations;

namespace DesktopApp.ResourcePlanner;

[PublicAPI]
public class WeeklyFarmCalculator(WeeklyStages sched, GameConstants gameConst) : ServiceBase
{
    public ResourcePlannerSettings Settings { get; } = new();

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
        var targetStage = sched.Stages.GetByCode(Settings.TargetStageCode);
        Debug.Assert(targetStage is {}, $"No target stage found with code {Settings.TargetStageCode}");
        if (Settings.UseMonthlyCard)
            sanLog.Log(80, "Daily potion from monthly card");
        if (Settings.UseWeeklyPots && today.DayOfWeek == System.DayOfWeek.Monday)
            sanLog.Log(240, "Weekly potions");

        if (day.IsTargetStageOpen)
        {
            while (true)
            {
                var runs = RunTargetStage(sanLog.CurrentValue, out var spent);
                sanLog.Log(-spent, $"Run {Settings.TargetStageCode} {runs} times");
                day.TargetStageCompletions += runs;
                exp = gameConst.AddExp(level, exp, spent * 12, out var levelups);
                if (levelups == 0)
                    break;

                for (var i = 0; i < levelups; i++)
                {
                    level += 1;
                    var newCap = gameConst.GetMaxSanity(level);
                    // every levelup (and OP refresh) is (cap - 1) sanity
                    // due to momentary loss of natural regen from overcap
                    sanLog.Log(newCap - 1, "Level up");
                }
            }
            // OP is spent on the last day the target stage is available
            // this is so that, if we level up/get more sanity cap prior, we get more sanity out of our OP
            // though, you have to level up quite a lot to get even one extra run out of this
            // theoretically, there are certain breakpoints where it's better to push and level
            // e.g. 119->120 sanity cap lets you bank 4 runs of AP-5 instead of 3
            // (which compounds more if done early)
            // TODO simulate OP refresh strategies
            // it's simulations all the way down

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

            if (isLast)
            {
                sanLog.Log(Settings.RefreshOpBudget, "Refreshing sanity with OP");
            }
        }
        else
        {
            // TODO: anni
            // done on first closed day of the week


            // save some sanity for tomorrow (unless today is the last day)
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
