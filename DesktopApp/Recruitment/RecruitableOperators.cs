using System.Collections.Immutable;
using System.Reactive.Linq;
using DesktopApp.Common.Operators;
using DesktopApp.Data;

namespace DesktopApp.Recruitment;
public sealed class RecruitableOperators : DataSource<ImmutableArray<Operator>>
{
    private static readonly string Url = Path.GetFullPath(@".\data\gacha_table.json"); // temp for local dev (todo LocalHttpCache)
    //private const string Url = "https://raw.githubusercontent.com/Kengxxiao/ArknightsGameData_YoStar/refs/heads/main/en_US/gamedata/excel/gacha_table.json";

    private readonly JsonDataSource<GachaTable> _source = new(new Uri(Url));

    public RecruitableOperators(OperatorRepository allOps)
    {
        _source.Values
            .CombineLatest(allOps.Values, (table, ops) => (table, ops))
            .Select(Process)
            .Subscribe(Subject)
            .DisposeWith(this);
    }

    private ImmutableArray<Operator> Process((GachaTable table, ImmutableArray<Operator> ops) pair)
    {
        var (table, ops) = pair;
        var names = table.ParseRecruitDetails().ToHashSet();
        return ops.Where(op => names.Contains(op.Name ?? "")).ToImmutableArray();
    }
}
