namespace DesktopApp.Data.Stages;

[JsonClass]
public sealed class ZoneTable
{
    public required Dictionary<string, Zone> Zones { get; set; }
    [JsonPropertyName("weeklyAdditionInfo")]
    public required Dictionary<string, WeeklyZoneSchedule> WeeklySchedule { get; set; }

    [JsonClass]
    public sealed class WeeklyZoneSchedule
    {
        [JsonPropertyName("daysOfWeek")]
        public List<DayOfWeek> Days { get; set; } = [];
    }
}
