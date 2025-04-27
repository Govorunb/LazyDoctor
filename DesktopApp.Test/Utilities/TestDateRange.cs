using DesktopApp.Utilities;

namespace DesktopApp.Test.Utilities;

public sealed class TestDateRange
{
    private static readonly DateTime _epoch = DateTime.UnixEpoch;
    private static readonly TimeSpan _day = TimeSpan.FromDays(1);
    private static readonly TimeSpan _minute = TimeSpan.FromMinutes(1);
    public static TheoryData<DateTime, DateTime, TimeSpan, bool, int> CalculateCountData => new()
    {
        // from, to, interval, endBoundIsInclusive, expectedCount
        { _epoch, _epoch.Add(_minute), _day, false, 1 },
        { _epoch, _epoch.Add(_day), _day, false, 1 },
        { _epoch, _epoch.Add(_day), _day, true, 2 },
        { _epoch, _epoch.Add(_day).Add(_minute), _day, false, 2 },
        { _epoch, _epoch.Add(_day).Add(_minute), _day, true, 2 },
    };

    [Theory]
    [MemberData(nameof(CalculateCountData))]
    public void TestCalculateCount(DateTime from, DateTime to, TimeSpan interval, bool endBoundIsInclusive, int expected)
    {
        var got = DateRange.CalculateCount(from, to, interval, endBoundIsInclusive);
        Assert.Equal(expected, got);
    }
}
