using System.Reactive.Linq;
using DesktopApp.Common;
using DesktopApp.Data.Stages;
using Splat;

namespace DesktopApp.Test.Data;
[TestClass]
public class TestStageRewardCalculations
{
    public static readonly TheoryData<string, int, int, int, int> Cases =
    [
        ("CE-1", 100,120,1410,1700),
        ("CE-2", 150,180,2325,2800),
        ("CE-3", 200,240,3410,4100),
        ("CE-4", 250,300,4740,5700),
        ("CE-5", 300,360,6240,7500),
        ("CE-6", 360,432,8325,10000),
    ];

    private static readonly StageRepository _stages = SplatHelpers.LOCATOR.GetService<StageRepository>()
                                                        ?? throw new InvalidOperationException();

    [Theory]
    [MemberData(nameof(Cases))]
    private async Task Test(string stageCode, int passExp, int clearExp, int passLmd, int clearLmd)
    {
        await _stages.Values.FirstAsync();

        var stage = _stages.GetByCode(stageCode);
        Assert.NotNull(stage);

        Assert.Equal(stage.TwoStarClearExpReward, passExp);
        Assert.Equal(stage.ExpReward, clearExp);
        Assert.Equal(stage.TwoStarClearLmdReward, passLmd);
        Assert.Equal(stage.LmdReward, clearLmd);
    }
}
