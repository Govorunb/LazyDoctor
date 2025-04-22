using System.Text.Json.Serialization;
using DesktopApp.Utilities.Attributes;

namespace DesktopApp.Data.Stages;

[JsonClass]
public sealed class Zone
{
    [JsonPropertyName("zoneID")]
    public required string ZoneId { get; set; }
    public int ZoneIndex { get; set; }
    [JsonPropertyName("zoneNameFirst")]
    public string? Title { get; set; }
    [JsonPropertyName("zoneNameSecond")]
    public string? Subtitle { get; set; }
}
