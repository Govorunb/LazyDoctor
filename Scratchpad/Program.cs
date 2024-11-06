using System.Collections;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DesktopApp.Utilities;

BenchmarkRunner.Run<Benchmarks>();

[ShortRunJob, MemoryDiagnoser]
public class Benchmarks
{
    [Params(1, 5, 10, 15, 20, 25)]
    public int N { get; set; }

    private List<int> Data { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        Data = Enumerable.Range(1, N).ToList();
    }

    // [Benchmark]
    // public void BitmaskAlloc()
    // {
    //     Consume(Combinatorics.PowerSetImplBitMask(Data));
    // }
    //
    // [Benchmark]
    // public void BitmaskIter()
    // {
    //     Consume(Combinatorics.PowerSetImplBitMaskIter(Data));
    // }
    //
    // [Benchmark]
    // public void Iterator()
    // {
    //     Consume(Combinatorics.PowerSetImplIterator(Data));
    // }

    private static void Consume(IEnumerable items)
    {
        foreach (var item in items)
        {
            if (item is IEnumerable sub)
                Consume(sub);
        }
    }
}
