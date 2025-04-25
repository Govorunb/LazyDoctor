using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using JetBrains.Annotations;

namespace DesktopApp.Data.Stages;

[PublicAPI]
public sealed class WeeklyStages : ServiceBase
{
    private ZoneTable? _zt;
    private SourceList<StageData> StagesList { get; set; }
    private ReadOnlyObservableCollection<string> _stageCodes;
    public ReadOnlyObservableCollection<string> StageCodes => _stageCodes;
    public StageRepository StagesRepo { get; }

    public WeeklyStages(IDataSource<ZoneTable> zoneTable, StageRepository stagesRepo)
    {
        AssertDI(zoneTable);
        AssertDI(stagesRepo);
        StagesRepo = stagesRepo;
        StagesList = new();
        zoneTable.Values.Subscribe(zt => _zt = zt);
        zoneTable.Values.CombineLatest(stagesRepo.Values)
            .Select(t => t.Item2.Where(s => t.Item1.WeeklySchedule.ContainsKey(s.ZoneId)))
            .Subscribe(l => StagesList.EditDiff(l));

        StagesList.Connect()
            .Transform(s => s.Code)
            .Sort(StringComparer.Ordinal)
            .Bind(out _stageCodes)
            .Subscribe();
    }

    public bool IsOpen(StageData? stage, DayOfWeek day)
    {
        if (stage is null || _zt is null) return false;

        return !_zt.WeeklySchedule.TryGetValue(stage.ZoneId, out var schedule)
            || schedule.Days.Contains(day);
    }
    public bool IsOpen(StageData? stage, DateTime date)
        => IsOpen(stage, date.DayOfWeek.ToEuropean());
    public bool IsOpen(string stageCode, DayOfWeek day)
        => IsOpen(StagesRepo.GetByCode(stageCode), day);
    public bool IsOpen(string stage, DateTime date)
        => IsOpen(stage, date.DayOfWeek.ToEuropean());

    public IEnumerable<StageData> GetOpen(DayOfWeek day)
        => StagesList.Items.Where(s => IsOpen(s.Code, day));
    public IEnumerable<StageData> GetOpen(DateTime date)
        => GetOpen(date.DayOfWeek.ToEuropean());
}
