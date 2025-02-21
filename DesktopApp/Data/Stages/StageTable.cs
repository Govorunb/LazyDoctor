using System.Text.Json.Serialization;
using DesktopApp.Utilities.Attributes;

namespace DesktopApp.Data.Stages;

[JsonClass]
public sealed class StageTable
{
    public required Dictionary<string, StageData> Stages { get; set; }
    [JsonPropertyName("forceOpenTable")]
    public required object CcOpenPeriods { get; set; }

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
        public DateTime StartsAt => DateTimeOffset.FromUnixTimeSeconds(StartTime).DateTime;
        public DateTime EndsAt => DateTimeOffset.FromUnixTimeSeconds(EndTime).DateTime;
    }
}
