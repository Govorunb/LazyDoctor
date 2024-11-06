using System.Collections.Immutable;
using System.Reactive.Linq;
using DesktopApp.Data;

namespace DesktopApp.Common.Operators;

public sealed class OperatorRepository : DataSource<ImmutableArray<Operator>>
{
    private static readonly string Url = Path.GetFullPath(@".\data\character_table.json"); // temp for local dev (todo LocalHttpCache)
    // private const string Url = "https://raw.githubusercontent.com/Kengxxiao/ArknightsGameData_YoStar/refs/heads/main/en_US/gamedata/excel/character_table.json";

    private readonly JsonDataSource<Dictionary<string, Operator>> _source = new(new Uri(Url));

    public OperatorRepository()
    {
        _source.Values
            .Select(Process)
            .Subscribe(Subject)
            .DisposeWith(this);
    }

    private static ImmutableArray<Operator> Process(Dictionary<string, Operator> ops)
    {
        foreach (var (id, op) in ops)
        {
            op.Id = id;
        }

        return [.. ops.Values];
    }
}
