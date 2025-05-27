using System.Collections.ObjectModel;
using DynamicData;
using ZLinq;

namespace DesktopApp.Data.Stages;

[PublicAPI]
public sealed class WeeklyStages : ServiceBase
{
    private readonly TimeUtilsService _timeUtils;
    private ZoneTable? _zt;
    private SourceList<StageData> StagesList { get; set; }
    private ReadOnlyObservableCollection<string> _stageCodes;
    public ReadOnlyObservableCollection<string> StageCodes => _stageCodes;
    public StageRepository StagesRepo { get; }

    public WeeklyStages(IDataSource<ZoneTable> zoneTable, StageRepository stagesRepo, TimeUtilsService timeUtils)
    {
        AssertDI(zoneTable);
        AssertDI(stagesRepo);
        AssertDI(timeUtils);
        StagesRepo = stagesRepo;
        _timeUtils = timeUtils;
        StagesList = new();
        zoneTable.Values.Subscribe(zt => _zt = zt)
            .DisposeWith(this);
        zoneTable.Values.CombineLatest(stagesRepo.Values)
            .Select(t => t.Second.Where(s => t.First.WeeklySchedule.ContainsKey(s.ZoneId)))
            .Subscribe(l => StagesList.EditDiff(l))
            .DisposeWith(this);

        StagesList.Connect()
            .Transform(s => s.Code)
            .Sort(StringComparer.Ordinal)
            .Bind(out _stageCodes)
            .Subscribe();
    }

    public bool IsStageOpen(string stageCode, DateTime date)
        => IsStageOpen(StagesRepo.GetByCode(stageCode), date);
    public bool IsStageOpen(StageData? stage, DateTime date)
        => IsZoneOpen(stage?.ZoneId, date);
    public bool IsZoneOpen(string? zoneId, DateTime date)
    {
        if (zoneId is null || _zt is null) return false;
        if (!_zt.WeeklySchedule.TryGetValue(zoneId, out var schedule))
            return true;

        var day = _timeUtils.ToServerTime(date).DayOfWeek.ToEuropean();
        if (schedule.Days.Contains(day))
            return true;

        var utcTime = _timeUtils.ToUtc(date);
        return StagesRepo.ForceOpenSchedule.AsValueEnumerable()
            // they're already ordered by date asc, so it's faster to iterate from end
            .Reverse()
            // in fact, we should never ever have to check more than two items
            .TakeWhile(fop => fop.EndsAt >= utcTime)
            .Any(fop => fop.StartsAt <= utcTime && fop.ZoneList.Contains(zoneId));
    }
    public IEnumerable<StageData> GetOpenStages(DateTime dateTime)
        => StagesList.Items.Where(s => IsStageOpen(s.Code, dateTime));
}
