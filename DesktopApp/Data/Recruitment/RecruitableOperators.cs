using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using DesktopApp.Data.Operators;
using DesktopApp.Utilities.Helpers;

namespace DesktopApp.Data.Recruitment;

public sealed class RecruitableOperators : DataSource<ImmutableArray<Operator>>
{
    private readonly OperatorRepository _allOps;
    private readonly IDataSource<GachaTable> _gachaTable;

    public RecruitableOperators(OperatorRepository allOps, IDataSource<GachaTable> gachaTable)
    {
        AssertDI(allOps);
        AssertDI(gachaTable);
        _allOps = allOps;
        _gachaTable = gachaTable;

        gachaTable.Values
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

    public override Task Reload() => Task.WhenAll(_allOps.Reload(), _gachaTable.Reload());
}
