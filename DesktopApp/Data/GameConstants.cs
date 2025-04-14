using System.Text.Json.Serialization;
using DesktopApp.Data.Player;
using DesktopApp.Utilities.Attributes;

namespace DesktopApp.Data;

[JsonClass]
public sealed class GameConstants
{
    public int MaxPlayerLevel { get; init; }
    public required int[] PlayerExpMap { get; init; }
    [JsonPropertyName("playerApMap")]
    public required int[] MaxSanity { get; init; }

    public int GetExpRequirementForNextLevel(int currentLevel)
        => PlayerExpMap[currentLevel - 1];
    public int GetTotalExpRequirement(int level)
        => PlayerExpMap.Take(level).Sum();
    public int GetMaxSanity(int level)
        => MaxSanity[level - 1] + 45; // cap was raised by 45 in early 2025, but they did it in the code instead of the data...

    public PlayerExpData AddExp(PlayerExpData curr, int exp)
    {
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
            }
        } while (rem >= 0);

        return new(level, nextExp);
    }
}
