using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reactive.Linq;

namespace DesktopApp.Data.Operators;

public sealed class OperatorRepository : DataSource<ImmutableArray<Operator>>
{
    private readonly JsonDataSource<Dictionary<string, Operator>> _source = new("character_table.json");
    private FrozenDictionary<string, Operator>? _byId;
    private FrozenDictionary<string, Operator>? _byName;

    public OperatorRepository()
    {
        _source.Values
            .Select(Process)
            .Subscribe(Subject)
            .DisposeWith(this);
    }

    private ImmutableArray<Operator> Process(Dictionary<string, Operator> ops)
    {
        // remove all scripted/utility entities
        var actualOps = ops.Where(pair => pair.Value.MaxPotentialLevel > 0).ToDictionary();
        foreach (var (id, op) in actualOps)
        {
            op.Id = id;
        }
        _byId = actualOps.ToFrozenDictionary();
        _byName = actualOps.Values.ToFrozenDictionary(op => op.Name!, op => op);

        return [.. ops.Values];
    }

    public Operator? GetById(string id) => _byId?[id];
    public Operator? GetByName(string name) => _byName?[name];
}
