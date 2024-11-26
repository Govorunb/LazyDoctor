using System.Collections.Frozen;
using System.Reactive.Linq;
using DesktopApp.Data.GitHub;
using DesktopApp.Utilities.Helpers;
using JetBrains.Annotations;

namespace DesktopApp.Data.Operators;

[PublicAPI]
public sealed class OperatorRepository : DataSource<IReadOnlyCollection<Operator>>
{
    private readonly DataSource<Dictionary<string, Operator>> _source;
    private FrozenDictionary<string, Operator>? _byId;
    private FrozenDictionary<string, Operator>? _byName;

    public OperatorRepository(GithubDataAdapter adapter)
    {
        _source = adapter.GetDataSource<Dictionary<string, Operator>>("excel/character_table.json");
        _source.Values
            .Select(Process)
            .Subscribe(Subject)
            .DisposeWith(this);
    }

    public Operator? GetById(string id) => _byId?[id];
    public Operator? GetByName(string name) => _byName?[name];
    public override Task Reload() => _source.Reload();

    private IReadOnlyCollection<Operator> Process(Dictionary<string, Operator> ops)
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
}
