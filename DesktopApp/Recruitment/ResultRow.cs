using DesktopApp.Data.Operators;
using DesktopApp.ViewModels;

namespace DesktopApp.Recruitment;

public class ResultRow : ViewModelBase
{
    public required List<Tag> Tags { get; set; }
    public required List<Operator> Operators { get; set; }

    public int MinimumRarity => Operators.Count == 0 ? 0 : Operators.Min(o => o.RarityStars);
}

public sealed class DesignResultRow : ResultRow
{
    public DesignResultRow()
    {
        string[] tags = ["Robot", "Starter", "Affix", "Rarity", "Example"];
        Tags = tags.Select(t => new Tag(t, "Example")).ToList();
        string[] names = ["Alex", "Barry", "Charles"];
        Operators = names.Select(name => new Operator() {Name = name}).ToList();
    }
};
