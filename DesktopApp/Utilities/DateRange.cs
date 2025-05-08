using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace DesktopApp.Utilities;

[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", // ___Collection
    Justification = "Pretends to be a collection because it's countable.\n" +
                    "(future language designers, please name this kind of interface Countable instead of ReadOnlyCollection...)")]
internal sealed class DateRange(DateTime from, DateTime to, TimeSpan interval, bool endBoundIsInclusive = true) : IReadOnlyCollection<DateTime>, ICollection<DateTime>
{
    private DateTime To { get; } = endBoundIsInclusive ? to.AddTicks(1) : to;
    public IEnumerator<DateTime> GetEnumerator()
    {
        for (var date = from; date < To; date += interval)
            yield return date;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => CalculateCount();

    public bool IsReadOnly => true;
    // enumerable size check fast path only checks for ICollection... and not IReadOnlyCollection
    // the "IThing doesn't extend IReadOnlyThing" design mistake strikes again
    public void Add(DateTime item) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public bool Contains(DateTime item) => throw new NotSupportedException();

    public void CopyTo(DateTime[] array, int arrayIndex)
    {
        foreach (var item in this)
            array[arrayIndex++] = item;
    }
    public bool Remove(DateTime item) => throw new NotSupportedException();

    private int CalculateCount() => To < from ? 0
        : (int)Math.Ceiling((To - from).TotalDays / interval.TotalDays);

    public static implicit operator DateRange((DateTime from, DateTime to, TimeSpan interval, bool endBoundIsInclusive) tuple)
        => new(tuple.from, tuple.to, tuple.interval, tuple.endBoundIsInclusive);
}

internal static class DateRangeExtensions
{
    public static DateRange Range(this DateTime from, DateTime to, TimeSpan interval)
        => new(from, to, interval);
}
