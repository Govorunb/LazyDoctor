using DesktopApp.Utilities.Helpers;

namespace DesktopApp.Test.Utilities;

public sealed class TestCartesianProduct
{
    public static readonly TheoryData<IEnumerable<IEnumerable<int>>, IEnumerable<IEnumerable<int>>> Cases =
    [
        ([ [1,2,3], [4,5,6] ],
        [
            [1,4],[1,5],[1,6],
            [2,4],[2,5],[2,6],
            [3,4],[3,5],[3,6],
        ]),
        (
            [ [1,2,3], [4], [] ],
            [[1,4],[2,4],[3,4]]
        ),
        (
            [ [1], [2], [3], [4] ],
            [[1,2,3,4]]
        ),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public void Test(IEnumerable<IEnumerable<int>> input, IEnumerable<IEnumerable<int>> expected)
    {
        Assert.Equal(expected, input.CartesianProduct());
    }
}
