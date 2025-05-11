using DesktopApp.Utilities;
using DesktopApp.Utilities.Helpers;

namespace DesktopApp.Test.Utilities;

[TestClass]
public sealed class TestRangeInterpolatedStringHandler
{
    public static readonly TheoryData<RangeInterpolatedStringHandler, string[]> Cases =
    [
        ($"TN-{1..4}", ["TN-1", "TN-2", "TN-3", "TN-4"]),
        ($"A{1..3}B{4..6}",
        [
            "A1B4", "A1B5", "A1B6",
            "A2B4", "A2B5", "A2B6",
            "A3B4", "A3B5", "A3B6",
        ]),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public void Test(RangeInterpolatedStringHandler handler, string[] expected)
    {
        Assert.Equal(expected, handler.Enumerate());
    }
}
