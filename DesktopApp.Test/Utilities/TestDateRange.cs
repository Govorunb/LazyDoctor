using DesktopApp.Utilities;

namespace DesktopApp.Test.Utilities;

using DateRangeData = (DateTime from, DateTime to, TimeSpan interval, bool endBoundIsInclusive);

[TestClass]
public sealed class TestDateRange
{
    private static readonly DateTime _epoch = DateTime.UnixEpoch;
    private static readonly TimeSpan _day = TimeSpan.FromDays(1);
    private static readonly TimeSpan _minute = TimeSpan.FromMinutes(1);
    public static readonly TheoryData<DateRangeData, int> CountCases =
    [
        ((_epoch, _epoch.Add(_minute), _day, false), 1),
        ((_epoch, _epoch.Add(_day), _day, false), 1),
        ((_epoch, _epoch.Add(_day), _day, true), 2),
        ((_epoch, _epoch.Add(_day + _minute), _day, false), 2),
        ((_epoch, _epoch.Add(_day + _minute), _day, true), 2),
    ];

    [Theory]
    [MemberData(nameof(CountCases))]
    public void TestDateRangeCount(DateRangeData range, int expected)
    {
        Assert.Equal(((DateRange)range).Count, expected);
    }
}
