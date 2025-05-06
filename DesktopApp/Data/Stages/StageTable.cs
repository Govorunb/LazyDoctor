namespace DesktopApp.Data.Stages;

[JsonClass]
public sealed class StageTable
{
    public required Dictionary<string, StageData> Stages { get; set; }
    public required Dictionary<string, ForceOpenPeriod> ForceOpenTable { get; set; }

    [JsonClass]
    public sealed class ForceOpenPeriod
    {
        public required string Id { get; set; }
        [JsonInclude]
        internal int StartTime { get; set; }
        [JsonInclude]
        internal int EndTime { get; set; }
        [JsonPropertyName("forceOpenList")]
        public required List<string> ZoneList { get; set; }

        // utc; not necessarily on reset
        public DateTime StartsAt => StartTime.AsUnixTimestamp();
        public DateTime EndsAt => EndTime.AsUnixTimestamp();
    }
}
