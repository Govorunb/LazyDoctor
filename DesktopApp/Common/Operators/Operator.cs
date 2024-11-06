using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DesktopApp.ViewModels;

namespace DesktopApp.Common.Operators;

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
public sealed partial class Operator : ViewModelBase
{
    #region JSON
    [JsonSerializable(typeof(Operator))]
    public partial class OperatorJsonContext : JsonSerializerContext;

    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Rarity { get; set; }
    public string? Position { get; set; }
    [JsonInclude, JsonPropertyName("profession")]
    private string? JsonClass { get; set; }
    public List<string>? TagList { get; set; }
    #endregion JSON

    [JsonIgnore]
    public int RarityStars => Rarity?[^1] is { } c && char.IsAsciiDigit(c) ? c - '0' : -1;
    [JsonIgnore]
    public OperatorClass Class => (OperatorClass)_classStrings.AsSpan().IndexOf(JsonClass!);

    public override string ToString() => Name ?? "unnamed";

    private static readonly string[] _classStrings =
    [
        "PIONEER",
        "WARRIOR",
        "TANK",
        "SNIPER",
        "CASTER",
        "MEDIC",
        "SUPPORT",
        "SPECIAL",
    ];
}

public enum OperatorClass
{
    Vanguard,
    Guard,
    Defender,
    Sniper,
    Caster,
    Medic,
    Supporter,
    Specialist,
}
