using System.Collections.Immutable;
using System.Diagnostics;
using DesktopApp.Data;

namespace DesktopApp.Common.Operators;

public sealed class OperatorRepository : ReactiveObjectBase
{
    public ImmutableArray<Operator> Operators { get; private set; } = [];

    private JsonDataSource<Dictionary<string, Operator>> _source = new(new Uri("https://raw.githubusercontent.com/Kengxxiao/ArknightsGameData_YoStar/refs/heads/main/en_US/gamedata/excel/character_table.json"));

    public OperatorRepository()
    {
        Task.Run(Load);
    }

    private void Load()
    {
        if (Operators.Length > 0) return;
        try
        {
            var ops = _source.Load();
            _source = null!;
            foreach (var (id, op) in ops)
            {
                op.Id = id;
            }

            Operators = [..ops.Values];
        }
        catch (Exception e)
        {
            Console.WriteLine($"erm {e}");
            Debug.WriteLine($"erm2 {e}");
            throw;
        }
    }
}
