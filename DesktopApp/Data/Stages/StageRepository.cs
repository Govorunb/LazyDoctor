using System.Collections.Frozen;
using System.Reactive.Linq;
using DesktopApp.Utilities.Helpers;
using JetBrains.Annotations;

namespace DesktopApp.Data.Stages;

[PublicAPI]
public sealed class StageRepository : DataSource<IReadOnlyCollection<StageData>>
{
    private readonly IDataSource<ZoneTable> _zones;
    private readonly IDataSource<StageTable> _stages;

    private FrozenDictionary<string, StageData>? _byId;
    private FrozenDictionary<string, StageData>? _byCode;

    [Reactive]
    public StageTable? StageTable { get; private set; }

    public StageRepository(IDataSource<ZoneTable> zones, IDataSource<StageTable> stages)
    {
        _zones = zones;
        _stages = stages;
        _stages.Values
            .Select(StagesUpdated)
            .Subscribe(Subject)
            .DisposeWith(this);
    }

    public StageData? GetById(string id)
    {
        if (_byId is null) return null;
        if (_byId.TryGetValue(id, out var result)) return result;

        this.Log().Error($"Stage ID not found: {id}");
        return null;
    }

    public StageData? GetByCode(string code)
    {
        if (_byCode is null) return null;
        if (_byCode.TryGetValue(code, out var op)) return op;

        this.Log().Error($"Stage code not found: {code}");
        return null;
    }

    public override async Task Reload()
    {
        await _stages.Reload();
        await _zones.Reload();
    }

    private IReadOnlyCollection<StageData> StagesUpdated(StageTable table)
    {
        _byId = table.Stages.ToFrozenDictionary();
        _byCode = StagesByCode(table.Stages.Values).ToFrozenDictionary(s => s.Code);
        StageTable = table;

        return _byId.Values;
    }

    private IEnumerable<StageData> StagesByCode(IEnumerable<StageData> source)
    {
        return source.GroupBy(s => s.Code)
            .Select(g => (g.Key, g
                    // event and main story CMs (through ch9) have stage IDs end in "#f#"
                    .Where(stage => !stage.StageId.EndsWith("#f#", StringComparison.Ordinal))
                    // from ch10 onwards, the "diffGroup" field is used (H-stages are "TOUGH" but have "stageType": "SUB")
                    // ... except for 11-20, the single other stage that uses "SUB"
                    .Where(stage => !(stage is {DifficultyGroup: "EASY" or "TOUGH"} && stage.LevelId.StartsWith("Obt/Main/", StringComparison.Ordinal)))
                    // Children of Ursus share their SV- prefix with Under Tides (very cool)
                    // keep Under Tides since it's the only one whose stages are actually available
                    .Where(stage => stage.ZoneId != "act10d5_zone1")
                    .ToList()
                ))
            .Select(g =>
            {
                switch (g.Item2.Count)
                {
                    case 1: return g.Item2[0];
                    default:
                    {
                        this.Log().Info($"Stage {g.Key} has {g.Item2.Count} duplicates - did not filter to 1");
                        return null;
                    }
                }
            })
            .WhereNotNull();
    }
}
