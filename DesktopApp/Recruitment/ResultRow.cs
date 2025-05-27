using System.Diagnostics.CodeAnalysis;
using DesktopApp.Data.Operators;

namespace DesktopApp.Recruitment;

public class ResultRow : ViewModelBase
{
    public required List<Tag> Tags { get; init; }
    public required List<Operator> Operators { get; init; }

    [field: MaybeNull]
    public List<Operator> ShownOperators
    {
        get => field ?? Operators;
        set;
    }

    public int MinimumRarity => Operators.Count == 0 ? 0 : Operators.Min(o => o.RarityStars);
}

[DesignClass]
internal sealed class DesignResultRow : ResultRow
{
    public DesignResultRow()
    {
        string[] tags = ["Robot", "Starter", "Affix", "Rarity", "Example"];
        Tags = tags.Select(t => new Tag(t, "Example")).ToList();
        string[] names = ["Alex", "Barry", "Charles"];
        Operators = names.Select(name => new Operator { Name = name, Rarity = "TIER_1" }).ToList();
    }
}
