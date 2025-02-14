using JetBrains.Annotations;

namespace DesktopApp.Data.Stages;

[PublicAPI]
public sealed class WeeklyStages : ServiceBase
{
    private readonly StageRepository _stages;
    private ZoneTable? _zt;
    public WeeklyStages(IDataSource<ZoneTable> zoneTable, StageRepository stages)
    {
        _stages = stages;
        zoneTable.Values.Subscribe(zt => _zt = zt);
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
}
