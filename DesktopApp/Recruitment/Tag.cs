using System.Diagnostics;
using System.Reactive.Linq;
using DesktopApp.Data.Operators;
using DesktopApp.Utilities.Attributes;

namespace DesktopApp.Recruitment;

[DebuggerDisplay("{Name,nq}")]
public class Tag : ViewModelBase
{
    public string Name { get; }
    public string Category { get; }

    [Obsolete("Designer only", true)]
    internal Tag() : this("", "") { }

    public Tag(string name, string category)
    {
        Name = name;
        Category = category;
        this.WhenAnyValue(t => t.IsSelected)
            .Where(b => !b)
            .Subscribe(_ => IsAutoSelected = false);
    }

    [Reactive]
    public bool IsAvailable { get; set; } = true;
    [Reactive]
    public bool IsSelected { get; set; }
    [Reactive]
    public bool IsAutoSelected { get; set; }

    public bool Match(Operator op)
    {
        switch (Category)
        {
            case "Rarity":
                if (Name is "Robot" or "Starter")
                    return op.TagList?.Contains(Name) ?? false;
                return Name switch
                {
                    "Senior Operator" => 5,
                    "Top Operator" => 6,
                    _ => throw new InvalidOperationException($"Unknown rarity tag: {Name}"),
                } == op.RarityStars;
            case "Position":
                return Name.Equals(op.Position, StringComparison.OrdinalIgnoreCase);
            case "Class":
                return Enum.Parse<OperatorClass>(Name).Equals(op.Class);
            case "Affix":
                return op.TagList?.Contains(Name) ?? false;
            default:
                throw new InvalidOperationException($"Unknown special tag category: {Category}");
        }
    }

    public override string ToString() => Name;
}

[DesignClass]
public sealed class DesignTag() : Tag("Test", "Example");
