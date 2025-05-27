using BenchmarkDotNet.Attributes;
using DesktopApp.Utilities;
using JetBrains.Annotations;
using ZLinq;

namespace Scratchpad.Benchmarks;

[ShortRunJob, MemoryDiagnoser, PublicAPI]
public class DateRangeVsZLinqRange
{
    private DateTime Start { get; } = DateTime.UnixEpoch;
    private DateTime End { get; } = DateTime.UnixEpoch.AddDays(1000);
    private TimeSpan Interval { get; } = TimeSpan.FromDays(1);

    [Benchmark]
    public void DateRangeConsume()
    {
        Consume(Start.Range(End, Interval));
    }
    [Benchmark]
    public void ZLinqRangeConsume()
    {
        Consume(ValueEnumerable.Range(Start, End, Interval, RightBound.Inclusive));
    }
    [Benchmark]
    public void DateRangeToList()
    {
        _ = Start.Range(End, Interval).ToList();
    }
    [Benchmark]
    public void ZLinqRangeToList()
    {
        _ = ValueEnumerable.Range(Start, End, Interval, RightBound.Inclusive).ToList();
    }
    private static void Consume<T>(IEnumerable<T> items)
    {
        foreach (var _ in items);
    }

    private static void Consume<TE, T>(ValueEnumerable<TE, T> items)
        where TE : struct, IValueEnumerator<T>
    {
        foreach (var _ in items);
    }
}
