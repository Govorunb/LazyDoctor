using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DesktopApp.Utilities.Attributes;
using JetBrains.Annotations;

namespace DesktopApp.Data.Operators;

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "That's what they're called, I don't make the rules")]
[JsonClass]
public sealed class Operator : ViewModelBase
{
    #region JSON
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Rarity { get; set; }
    public string? Position { get; set; }
    public int MaxPotentialLevel { get; set; }

    [JsonInclude, JsonPropertyName("profession")]
    internal string? JsonClass { get; set; }
    public string[]? TagList { get; set; }
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

[PublicAPI]
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
