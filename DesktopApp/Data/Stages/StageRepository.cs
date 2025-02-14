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
    private FrozenDictionary<string, StageData>? _byName;
    private FrozenDictionary<string, StageData>? _byCode;

    public StageTable? Stages { get; private set; }

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

    public StageData? GetByName(string name)
    {
        if (_byName is null) return null;
        if (_byName.TryGetValue(name, out var op)) return op;

        this.Log().Error($"Stage name not found: {name}");
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
        _byName = table.Stages.Values.ToFrozenDictionary(s => s.Name);
        _byCode = table.Stages.Values.ToFrozenDictionary(s => s.Code);
        Stages = table;

        return _byName.Values;
    }
}
