using System.Reactive.Linq;
using DynamicData;
using JetBrains.Annotations;

namespace DesktopApp.Data.Stages;

[PublicAPI]
public sealed class WeeklyStages : ServiceBase
{
    private readonly StageRepository _stages;
    private ZoneTable? _zt;
    public SourceList<StageData> Stages { get; set; }

    public WeeklyStages(IDataSource<ZoneTable> zoneTable, StageRepository stages)
    {
        _stages = stages;
        Stages = new();
        zoneTable.Values.Subscribe(zt => _zt = zt);
        zoneTable.Values.CombineLatest(stages.Values)
            .Select(t => t.Item2.Where(s => t.Item1.WeeklySchedule.ContainsKey(s.ZoneId)))
            .Subscribe(l => Stages.EditDiff(l));
    }

    public bool IsOpen(string stageCode, DayOfWeek day)
    {
        var stage = _stages.GetByCode(stageCode);
        if (stage is null || _zt is null) return false;

        return _zt.WeeklySchedule.TryGetValue(stage.ZoneId, out var schedule)
            && schedule.Days.Contains(day);
    }

    public bool IsOpen(string stage, DateTime date)
        => IsOpen(stage, date.DayOfWeek.ToEuropean());

    public IEnumerable<StageData> GetOpen(DayOfWeek day)
        => Stages.Items.Where(s => IsOpen(s.Code, day));
    public IEnumerable<StageData> GetOpen(DateTime date)
        => GetOpen(date.DayOfWeek.ToEuropean());
}
