using System.Diagnostics;
using System.Text.Json.Serialization;
using DesktopApp.Data.Operators;

namespace DesktopApp.Recruitment;

[DebuggerDisplay("{Name,nq}")]
public class Tag(string name, string category) : ViewModelBase
{
    public string Id { get; } = name.Replace(' ', '-');
    public string Name { get; } = name;
    public string Category { get; } = category;

    [Obsolete("Designer only", true)]
    internal Tag() : this("", "") { }

    [Reactive, JsonIgnore]
    public bool IsAvailable { get; set; } = true;
    [Reactive, JsonIgnore]
    public bool IsSelected { get; set; }

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
                return op.TagList?.Contains(Id) ?? false;
            default:
                throw new InvalidOperationException($"Unknown special tag category: {Category}");
        }
    }

    public override string ToString() => Name;
}

public sealed class DesignTag() : Tag("Test", "Rarity");
