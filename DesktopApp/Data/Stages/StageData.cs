using System.Text.Json.Serialization;
using DesktopApp.Utilities.Attributes;

namespace DesktopApp.Data.Stages;

[JsonClass]
public sealed class StageData
{
    public required string StageType { get; set; }
    public required string Difficulty { get; set; }
    public required string StageId { get; set; }
    public required string ZoneId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    [JsonPropertyName("apCost")]
    public int SanityCost { get; set; }
    [JsonInclude, JsonPropertyName("expGain")]
    private int TwoStarClearExpReward { get; set; }
    [JsonPropertyName("goldGain")]
    public int CreditsReward { get; set; }

    public int ExpReward => (int)(TwoStarClearExpReward * 1.2);
}
