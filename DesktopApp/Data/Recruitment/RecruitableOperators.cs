using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using DesktopApp.Data.Operators;

namespace DesktopApp.Data.Recruitment;

public sealed class RecruitableOperators : DataSource<ImmutableArray<Operator>>
{
    private readonly OperatorRepository _allOps;

    public RecruitableOperators(OperatorRepository allOps, IDataSource<GachaTable> source)
    {
        _allOps = allOps;
        source.Values
            .CombineLatest(allOps.Values, (table, _) => table)
            .Select(Process)
            .Subscribe(Subject)
            .DisposeWith(this);
    }

    private ImmutableArray<Operator> Process(GachaTable table)
    {
        var names = table.ParseRecruitDetails();
        var ops = new Operator[names.Count];
        for (var i = 0; i < names.Count; i++)
        {
            ops[i] = _allOps.GetByName(names[i])
                ?? throw new InvalidOperationException($"Operator '{names[i]}' does not exist");
        }

        // return ops.ToImmutableArray(); // copies
        // return [..ops]; // also copies
        return ImmutableCollectionsMarshal.AsImmutableArray(ops); // wraps directly
    }
}
