namespace DesktopApp.Common;

[PublicAPI]
public sealed class TimeUtilsService : ServiceBase
{
    private UserPrefs _prefs;

    [Reactive]
    public TimeOnly LocalResetTime { get; private set; }
    [Reactive]
    public TimeOnly UtcResetTime { get; private set; }
    [Reactive]
    public TimeSpan LocalToServerTime { get; private set; }

    public TimeUtilsService(UserPrefs prefs)
    {
        _prefs = prefs;
        prefs.WhenAnyValue(p => p.General.ServerTimezone)
            .Subscribe(tz => LocalToServerTime = -TimeZoneInfo.Local.BaseUtcOffset + tz)
            .DisposeWith(this);
        prefs.WhenAnyValue(p => p.General.ServerReset, p => p.General.ServerTimezone)
            .Subscribe(pair =>
            {
                var (reset, timezone) = pair;
                var utcReset = reset.ToTimeSpan() - timezone;
                var localReset = utcReset + TimeZoneInfo.Local.BaseUtcOffset;
                UtcResetTime = utcReset.ToTimeOnly();
                LocalResetTime = localReset.ToTimeOnly();
            }).DisposeWith(this);
    }

    public DateTime ToLocal(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Local => dateTime,
            DateTimeKind.Utc => dateTime.ToLocalTime(),
            DateTimeKind.Unspecified => dateTime.Subtract(LocalToServerTime).WithKind(DateTimeKind.Local),
            _ => throw new ArgumentException($"Invalid DateTimeKind {dateTime.Kind}", nameof(dateTime)),
        };
    }
    public DateTime ToUtc(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Unspecified => dateTime.Subtract(_prefs.General.ServerTimezone).WithKind(DateTimeKind.Utc),
            _ => throw new ArgumentException($"Invalid DateTimeKind {dateTime.Kind}", nameof(dateTime)),
        };
    }
    public DateTime ToServerTime(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Local => dateTime.Add(LocalToServerTime).WithKind(DateTimeKind.Unspecified),
            DateTimeKind.Utc => dateTime.Add(_prefs.General.ServerTimezone).WithKind(DateTimeKind.Unspecified),
            DateTimeKind.Unspecified => dateTime, // unspecified is used for server
            _ => throw new ArgumentException($"Invalid DateTimeKind {dateTime.Kind}", nameof(dateTime)),
        };
    }

    public TimeSpan TimeSinceReset(DateTime dt)
    {
        var curTime = TimeOnly.FromDateTime(dt);
        return dt.Kind switch
        {
            DateTimeKind.Local => curTime - LocalResetTime,
            DateTimeKind.Utc => curTime - UtcResetTime,
            DateTimeKind.Unspecified => curTime - _prefs.General.ServerReset,
            _ => throw new ArgumentException($"Invalid DateTimeKind {dt.Kind}", nameof(dt)),
        };
    }
    public TimeSpan TimeToNextReset(DateTime dt)
        => TimeSpan.FromDays(1) - TimeSinceReset(dt);

    public DateTime NextReset(DateTime dt)
        => dt + TimeToNextReset(dt);
    public DateTime PrevReset(DateTime dt)
        => dt - TimeSinceReset(dt);
}
