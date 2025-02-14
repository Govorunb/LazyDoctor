using JetBrains.Annotations;

namespace DesktopApp.Common;

// americans...
[PublicAPI]
public enum DayOfWeek
{
    Monday = 1,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday,
}

[PublicAPI]
public static class DayOfWeekExtensions
{
    public static DayOfWeek ToEuropean(this System.DayOfWeek dow)
        => dow is System.DayOfWeek.Sunday ? DayOfWeek.Sunday
            : (DayOfWeek)dow;
    public static System.DayOfWeek ToSystem(this DayOfWeek day)
        => day is DayOfWeek.Sunday ? System.DayOfWeek.Sunday
            : (System.DayOfWeek)day;
}
