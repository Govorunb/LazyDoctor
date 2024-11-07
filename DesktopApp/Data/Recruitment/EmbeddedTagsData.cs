using DesktopApp.Recruitment;

namespace DesktopApp.Data.Recruitment;

public static class EmbeddedTagsData
{
    // more convenient way to declare the data
    // I don't mind the runtime cost of converting these
    private static readonly Dictionary<string, string[]> _knownTagsRaw = new()
    {
        ["Rarity"] = ["Robot", "Starter", "Senior Operator", "Top Operator"],
        ["Position"] = ["Melee", "Ranged"],
        ["Class"] = ["Vanguard", "Guard", "Defender", "Sniper", "Caster", "Medic", "Supporter", "Specialist"],
        ["Affix"] = ["AoE", "Crowd Control", "Debuff", "Defense", "DP Recovery", "DPS", "Elemental", "Fast Redeploy", "Healing", "Nuker", "Shift", "Slow", "Summon", "Support", "Survival"],
    };

    private static readonly Tag[] _knownTags = _knownTagsRaw
        .OrderByDescending(pair => pair.Key)
        .SelectMany(pair => pair.Value.Select(name => new Tag(name, pair.Key)))
        .ToArray();

    public static Tag[] GetKnownTags() => _knownTags;
}
