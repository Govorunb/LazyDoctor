using System.Collections.Frozen;

namespace DesktopApp.Data.Operators;

[PublicAPI]
public sealed class OperatorRepository : DataSource<IReadOnlyCollection<Operator>>
{
    private readonly IDataSource<OperatorTable> _source;
    private FrozenDictionary<string, Operator>? _byId;
    private FrozenDictionary<string, Operator>? _byName;

    public OperatorRepository(IDataSource<OperatorTable> source)
    {
        _source = source;
        source.Values
            .Select(Process)
            .Subscribe(Subject)
            .DisposeWith(this);
    }

    public Operator? GetById(string id)
    {
        if (_byId is null) return null;
        if (_byId.TryGetValue(id, out var result)) return result;

        this.Log().Error($"Operator ID not found: {id}");
        return null;
    }

    public Operator? GetByName(string name)
    {
        if (_byName is null) return null;

        if (_byName.TryGetValue(name, out var op)) return op;

        if (_aliases.TryGetValue(name, out var aliasedName) && _byName.TryGetValue(aliasedName, out op))
            return op;

        this.Log().Error($"Operator name not found: {name}");
        return null;
    }

    public override Task Reload() => _source.Reload();

    private IReadOnlyCollection<Operator> Process(OperatorTable ops)
    {
        _byId = ops
            .Where(pair => pair.Value.MaxPotentialLevel > 0) // filter out scripted/utility entities
            .ToFrozenDictionary();

        foreach (var (id, op) in _byId)
        {
            op.Id = id;
        }
        _byName = _byId.Values.ToFrozenDictionary(op => op.Name!, op => op);

        return _byId.Values;
    }

    private static readonly Dictionary<string, string> _aliases = new()
    {
        ["Justice Knight"] = "'Justice Knight'",
    };
}
