using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
// ReSharper disable InconsistentNaming

namespace Scratchpad.Benchmarks;

[ShortRunJob, MemoryDiagnoser, PublicAPI]
public class ListVsDictionaryLookup
{
    private record struct ComplexValueType(int x, string? y, double z);
    [Params(1, 5, 10, 25, 50, 100, 500, 10000)] public int N { get; set; }
    [Params(false, true)] public bool Hit { get; set; }
    public int LookupValue => Hit ? N / 2 : -1;

    private List<int> _listInts = [];
    private Dictionary<int, int> _dictInts = [];
    private List<ComplexValueType> _listComplex = [];
    private Dictionary<int, ComplexValueType> _dictComplex = [];

    [GlobalSetup]
    public void Setup()
    {
        _listInts = Enumerable.Range(0, N).ToList();
        _dictInts = _listInts.ToDictionary(i => i);
        _listComplex = Enumerable.Range(0, N)
            .Select(i => new ComplexValueType(i, null, Random.Shared.NextDouble()))
            .ToList();
        _dictComplex = _listComplex.ToDictionary(i => i.x);
    }

    // ReSharper disable ReturnValueOfPureMethodIsNotUsed
    [Benchmark, BenchmarkCategory("Ints")]
    public int ListIntIndexOf() => _listInts.IndexOf(LookupValue);
    [Benchmark, BenchmarkCategory("Ints")]
    public int ListIntLoop()
    {
        for (var i = 0; i < N; i++)
        {
            if (_listInts[i] == LookupValue)
                return i;
        }

        return -1;
    }
    [Benchmark, BenchmarkCategory("Ints")]
    public void DictionaryInt() => _dictInts.GetValueOrDefault(LookupValue);

    [Benchmark, BenchmarkCategory("Complex")]
    public int ListComplexFindIndex() => _listComplex.FindIndex(c => c.x == LookupValue);
    [Benchmark, BenchmarkCategory("Complex")]
    public void ListComplexLoop()
    {
        // List.Find allocates a delegate
        for (var i = 0; i < N; i++)
        {
            if (_listComplex[i].x == LookupValue)
                return;
        }
    }

    [Benchmark, BenchmarkCategory("Complex")]
    public void DictionaryComplex() => _dictComplex.GetValueOrDefault(LookupValue);
}
