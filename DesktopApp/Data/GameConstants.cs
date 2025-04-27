using System.Diagnostics;
using System.Text.Json.Serialization;
using DesktopApp.Data.Player;
using DesktopApp.Utilities.Attributes;

namespace DesktopApp.Data;

[JsonClass]
public sealed class GameConstants
{
    public int MaxPlayerLevel { get; init; }
    /// <summary>
    /// Table of exp requirements for each level. Starts at the requirement to go from level 1 to 2.
    /// </summary>
    public required int[] PlayerExpMap { get; init; }
    [JsonPropertyName("playerApMap")]
    public required int[] MaxSanity { get; init; }

    public int GetExpRequirementForNextLevel(int currentLevel)
    {
        Debug.Assert(currentLevel <= MaxPlayerLevel);
        return currentLevel >= MaxPlayerLevel ? 0
            : PlayerExpMap[currentLevel - 1];
    }

    public int GetTotalExpRequirement(int level)
        => PlayerExpMap.Take(level-1).Sum();
    public int GetMaxSanity(int level)
        => MaxSanity[level - 1] + 45; // cap was raised by 45 in early 2025, but they did it in the code instead of the data...

    public PlayerExpData AddExp(PlayerExpData curr, int exp)
    {
        if (curr.Level >= MaxPlayerLevel)
            return new(MaxPlayerLevel);
        var (level, currExp) = curr;
        var nextExp = currExp + exp;
        int rem;
        do
        {
            var req = GetExpRequirementForNextLevel(level);
            rem = nextExp - req;
            if (rem >= 0)
            {
                nextExp = rem;
                level++;
                if (level == MaxPlayerLevel)
                    return new(MaxPlayerLevel);
            }
        } while (rem >= 0);

        return new(level, nextExp);
    }
}
