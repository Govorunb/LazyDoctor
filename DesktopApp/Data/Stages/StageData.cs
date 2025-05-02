namespace DesktopApp.Data.Stages;

[JsonClass]
public sealed class StageData
{
    public required string StageType { get; set; }
    public required string Difficulty { get; set; }
    [JsonPropertyName("diffGroup")]
    public required string DifficultyGroup { get; set; }
    public required string StageId { get; set; }
    public required string LevelId { get; set; }
    public required string ZoneId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    [JsonPropertyName("apCost")]
    public int SanityCost { get; set; }
    // TODO: decide whether to keep using these or just calc from sanity cost like a normal human being
    // SanityCost * 10
    [JsonInclude, JsonPropertyName("expGain")]
    public int TwoStarClearExpReward { get; set; }
    // generally (sanity cost * 10) just like exp
    // CE- stages override this; others have this set to 0 (and give LMD as a regular drop)
    // some stages forgor to set regular reward to 0 so you get funny numbers like 1216 on 6-1 (180*1.2 + 1000 drop)
    [JsonPropertyName("goldGain")]
    public int TwoStarClearLmdReward { get; set; }

    public int ExpReward => (int)(TwoStarClearExpReward * 1.2);
    private int? _lmdReward;
    public int LmdReward => _lmdReward ??= CalcLmdReward();

    private int CalcLmdReward()
    {
        if (TwoStarClearLmdReward == 0)
            return 0;

        var reward = TwoStarClearLmdReward * 1.2;
        if (Code.StartsWith("CE-", StringComparison.Ordinal))
        {
            // round to nearest 100
            return (int)Math.Round(reward / 100) * 100;
        }

        return (int)reward;
    }
}
