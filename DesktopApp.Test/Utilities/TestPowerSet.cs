using DesktopApp.Utilities;

namespace DesktopApp.Test.Utilities;

[TestClass]
public sealed class TestPowerSet
{
    private sealed record TestCase(List<int> Input, List<List<int>> Expected);

    private static readonly TestCase[] _cases =
    [
        new([], [[]]),
        new([1], [[], [1]]),
        new([1, 2], [[], [1], [2], [1, 2]]),
        new([1, 2, 3],
        [
            [],
            [1], [2], [3],
            [1, 2], [1, 3], [2, 3],
            [1, 2, 3],
        ]),
        new([1, 2, 3, 4],
        [
            [],
            [1], [2], [3], [4],
            [1, 2], [1, 3], [1, 4], [2, 3], [2, 4], [3, 4],
            [1, 2, 3], [1, 2, 4], [1, 3, 4], [2, 3, 4],
            [1, 2, 3, 4],
        ]),
    ];

    public static IEnumerable<object[]> Cases
        => _cases.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(Cases))]
    private void Test(TestCase testCase)
    {
        var got = testCase.Input.GetPowerSet()
            .Select(subset => subset.ToList())
            .ToList();
        Assert.Equal(SortedOrder(testCase.Expected), SortedOrder(got));
    }

    private static IOrderedEnumerable<List<int>> SortedOrder(IEnumerable<List<int>> items)
    {
        return items.OrderBy(l => l.Prepend(l.Count).Aggregate(0, (a, b) => a * 10 + b));
    }
}
