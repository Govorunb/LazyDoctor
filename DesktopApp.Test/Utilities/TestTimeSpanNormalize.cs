using System.Globalization;
using DesktopApp.Utilities.Helpers;

namespace DesktopApp.Test.Utilities;

[TestClass]
public sealed class TestTimeSpanNormalize
{
    private static readonly TimeSpan _oneDay = TimeSpan.FromDays(1);

    public record struct TimeSpanData(TimeSpan TimeSpan)
    {
        public TimeSpanData(string s) : this(TimeSpan.Parse(s, CultureInfo.InvariantCulture))
        {
        }

        public static implicit operator TimeSpan(TimeSpanData d) => d.TimeSpan;
        public static implicit operator TimeSpanData(TimeSpan t) => new(t);
        public static implicit operator TimeSpanData(string s) => new(s);
    }

    public static readonly TheoryData<TimeSpanData, TimeSpanData> Cases =
    [
        // basic cases
        ("00:00:00", "00:00:00"),
        ("1.00:00:00", "00:00:00"),
        // positive <1d
        ("01:00:00", "01:00:00"),
        ("12:30:15", "12:30:15"),
        ("23:59:59", "23:59:59"),
        // positive >1d
        ("1.01:00:00", "01:00:00"),
        ("2.00:30:15", "00:30:15"),
        ("3.00:00:00", "00:00:00"),
        ("4.04:15:30", "04:15:30"),
        // negative <1d
        ("-01:00:00", "23:00:00"),
        ("-12:00:00", "12:00:00"),
        ("-23:59:59", "00:00:01"),
        // negative >1d
        ("-1.01:00:00", "23:00:00"),
        ("-2.00:30:00", "23:30:00"),
        ("-3.00:15:30", "23:44:30"),
        // extreme cases
        (TimeSpan.MinValue, TimeSpan.FromTicks(763145224192)),
        (TimeSpan.MaxValue, TimeSpan.FromTicks(100854775807)),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public void TestCommonCases(TimeSpanData input, TimeSpanData expected)
    {
        var result = input.TimeSpan.Normalize();

        Assert.True(result.Days is >=0 and <1);
        Assert.Equal(expected.TimeSpan, result);
    }
}
