using System.Collections.Frozen;
using ZLinq;

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

    [Reactive]
    public StageTable.ForceOpenPeriod[] ForceOpenSchedule { get; private set; } = [];

    public StageRepository(IDataSource<ZoneTable> zones, IDataSource<StageTable> stages)
    {
        _zones = zones;
        _stages = stages;
        _stages.Values
            .Select(StagesUpdated)
            .Subscribe(Subject)
            .DisposeWith(this);
    }

    public StageData? GetById(string? id)
    {
        if (id is null || _byId is null) return null;
        if (_byId.TryGetValue(id, out var result)) return result;

        this.Log().Error($"Stage ID not found: {id}");
        return null;
    }

    public StageData? GetByCode(string? code)
    {
        if (code is null || _byCode is null) return null;
        if (_byCode.TryGetValue(code, out var op)) return op;

        this.Log().Debug($"Stage code not found: {code}");
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
        _byCode = StagesByCode(table.Stages.Values);
        StageTable = table;
        ForceOpenSchedule = table.ForceOpenTable
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value)
            .ToArray();

        return _byId.Values;
    }

    private FrozenDictionary<string, StageData> StagesByCode(IEnumerable<StageData> source)
    {
        // stage codes were really not meant to be used as keys
        return source
            .AsValueEnumerable()
            .GroupBy(s => s.Code)
            .Select(g => (g.Key, g
                .AsValueEnumerable()
                // event and main story CMs (through ch9) have stage IDs end in "#f#"
                .Where(stage => !stage.StageId.EndsWith("#f#", StringComparison.Ordinal))
                // from ch10 onwards, the "diffGroup" field is used (H-stages are "TOUGH" but have "stageType": "SUB")
                // ... except for 11-20, the single other stage that uses "SUB"
                .Where(stage => !(stage.DifficultyGroup is "EASY" or "TOUGH" && stage.LevelId.StartsWith("Obt/Main/", StringComparison.Ordinal)))
                // Children of Ursus share their SV- prefix with Under Tides (very cool)
                // keep Under Tides since CoU's stages aren't available
                .Where(stage => stage.ZoneId != "act10d5_zone1")
                // the rest are:
                // - TN-x (trials for navigator)
                // - LT-x (SSS)
                // - annihilations (whose codes are country names, e.g. "Ursus" is for Chernobog, Abandoned Mine, and the other one i forgor)
                // - Il Siracusano missions (IS-QT)
                // - two stages with the code ??? (cool)
                // - TR-1 to TR-3 (yep)
                // - "Regular Mode" (Stronghold Protocol aka autochess)
                .ToList()))
            .Select(g => g.Item2 switch
            {
                [var stage] => stage,
                _ when _expectedNonUniqueCodes.Contains(g.Key) => null,
                _ => ((StageData?)null).AndLog(this, LogLevel.Info, $"Stage {g.Key} has {g.Item2.Count} duplicates - did not filter to 1"),
            })
            .WhereNotNull()
            .ToFrozenDictionary(s => s.Code);
    }

    // initializing the FrozenSet directly with a collection expression:
    // - copies the list with a ToArray call (which is then cast to ReadOnlySpan)
    //     (instead of using CollectionsMarshal.AsSpan, which is safe because this is a temporary list)
    // - passes the ROS to FrozenSet.Create, which copies everything (again!) into a HashSet
    // - then, from that set, everything is copied (!!!) into the final frozen set object
    // this does not help the creation cost one bit
    private static readonly FrozenSet<string> _expectedNonUniqueCodes = new HashSet<string>(
    [
        ..StringHelpers.Enumerate($"TN-{1..4}"),
        ..StringHelpers.Enumerate($"LT-{1..8}"),
        "IS-QT",
        "Ursus", "Yan", "Kazimierz", "Siesta", "Iberia", "Sargon", "Columbia", "Victoria",
        "???",
        ..StringHelpers.Enumerate($"TR-{1..3}"),
        "Regular Mode",
    ]).ToFrozenSet();
}
