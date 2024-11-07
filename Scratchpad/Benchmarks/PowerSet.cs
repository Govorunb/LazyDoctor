using System.Collections;
using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;

namespace Scratchpad.Benchmarks;

[ShortRunJob, MemoryDiagnoser, PublicAPI]
public class PowerSet
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
