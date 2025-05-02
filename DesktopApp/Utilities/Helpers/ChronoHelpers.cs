namespace DesktopApp.Utilities.Helpers;

[PublicAPI]
internal static class ChronoHelpers
{
    public static DateTime Clamp(this DateTime value, DateTime min, DateTime max)
    {
        return value < min ? min
            : value > max ? max
            : value;
    }

    /// <inheritdoc cref="Normalize" />
    public static TimeOnly ToTimeOnly(this TimeSpan time)
        => TimeOnly.FromTimeSpan(Normalize(time));

    /// <summary>
    /// Converts a TimeSpan to a time of day between 00:00 and 23:59.<br/>
    /// A negative time will be interpreted as time before midnight, e.g. -2h -> 22:00.
    /// </summary>
    public static TimeSpan Normalize(this TimeSpan time)
    {
        while (time < TimeSpan.Zero) time += TimeSpan.FromDays(1);
        while (time >= TimeSpan.FromDays(1)) time -= TimeSpan.FromDays(1);
        return time;
    }

    /// <summary>
    /// Returns a copy of the given <see cref="DateTime" /> with the specified <see cref="DateTimeKind" /> set.
    /// </summary>
    public static DateTime WithKind(this DateTime source, DateTimeKind kind)
    {
        return source.Kind == kind ? source
            : new DateTime(DateOnly.FromDateTime(source), TimeOnly.FromDateTime(source), kind);
    }
}
