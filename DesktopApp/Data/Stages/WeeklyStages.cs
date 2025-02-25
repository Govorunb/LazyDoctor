using System.Reactive.Linq;
using DynamicData;
using JetBrains.Annotations;

namespace DesktopApp.Data.Stages;

[PublicAPI]
public sealed class WeeklyStages : ServiceBase
{
    private ZoneTable? _zt;
    private SourceList<StageData> StagesList { get; set; }
    public StageRepository Stages { get; }

    public WeeklyStages(IDataSource<ZoneTable> zoneTable, StageRepository stages)
    {
        Stages = stages;
        StagesList = new();
        zoneTable.Values.Subscribe(zt => _zt = zt);
        zoneTable.Values.CombineLatest(stages.Values)
            .Select(t => t.Item2.Where(s => t.Item1.WeeklySchedule.ContainsKey(s.ZoneId)))
            .Subscribe(l => StagesList.EditDiff(l));
    }

    public bool IsOpen(string stageCode, DayOfWeek day)
    {
        var stage = Stages.GetByCode(stageCode);
        if (stage is null || _zt is null) return false;

        return _zt.WeeklySchedule.TryGetValue(stage.ZoneId, out var schedule)
            && schedule.Days.Contains(day);
    }

    public bool IsOpen(string stage, DateTime date)
        => IsOpen(stage, date.DayOfWeek.ToEuropean());

    public IEnumerable<StageData> GetOpen(DayOfWeek day)
        => StagesList.Items.Where(s => IsOpen(s.Code, day));
    public IEnumerable<StageData> GetOpen(DateTime date)
        => GetOpen(date.DayOfWeek.ToEuropean());
}
