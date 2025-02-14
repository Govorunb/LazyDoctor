using System.Text.Json.Serialization;
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
        => MaxSanity[level - 1];

    public int AddExp(int level, int currExp, int addExp, out bool leveledUp)
    {
        leveledUp = false;
        var req = GetExpRequirementForNextLevel(level);
        var nextExp = currExp + addExp;
        if (nextExp >= req)
        {
            nextExp -= req;
            leveledUp = true;
        }
        return nextExp;
    }
}
