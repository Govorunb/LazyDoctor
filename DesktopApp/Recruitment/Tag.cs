using System.Text.Json.Serialization;
using DesktopApp.Common.Operators;
using DesktopApp.ViewModels;

namespace DesktopApp.Recruitment;

public class Tag(string name, string category) : ViewModelBase
{
    public string Id { get; } = name.Replace(' ', '-');
    public string Name { get; } = name;
    public string Category { get; } = category;

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
                return (Name switch
                {
                    "Senior Operator" => "TIER_5",
                    "Top Operator" => "TIER_6",
                    _ => throw new InvalidOperationException($"Unknown rarity tag: {Name}"),
                }).Equals(op.Rarity, StringComparison.OrdinalIgnoreCase);
            case "Position":
                return Name.Equals(op.Position, StringComparison.OrdinalIgnoreCase);
            case "Class":
                return (Name switch
                {
                    "Vanguard" => "PIONEER",
                    "Guard" => "WARRIOR",
                    "Defender" => "TANK",
                    "Medic" => "MEDIC",
                    "Specialist" => "SPECIAL",
                    "Sniper" => "SNIPER",
                    "Caster" => "CASTER",
                    "Supporter" => "SUPPORT",
                    _ => throw new InvalidOperationException($"Unknown class tag: {Name}"),
                }).Equals(op.Class, StringComparison.OrdinalIgnoreCase);
            case "Affix":
                return op.TagList?.Contains(Id) ?? false;
            default:
                throw new InvalidOperationException($"Unknown special tag category: {Category}");
        }
    }

    public override string ToString() => Name;
}

public sealed class DesignTag() : Tag("Robot", "Rarity");
