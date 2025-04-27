using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace DesktopApp.Utilities;

[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", // ___Collection
    Justification = "Pretends to be a collection because it's countable.\n" +
                    "(future language designers, please name this kind of interface Countable instead of ReadOnlyCollection...)")]
internal sealed class DateRange(DateTime from, DateTime to, TimeSpan interval, bool endBoundIsInclusive = true) : IReadOnlyCollection<DateTime>, ICollection<DateTime>
{
    public IEnumerator<DateTime> GetEnumerator()
    {
        var actualTo = endBoundIsInclusive
            ? to + TimeSpan.FromTicks(1)
            : to;
        for (var date = from; date < actualTo; date += interval)
            yield return date;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count { get; } = CalculateCount(from, to, interval, endBoundIsInclusive);

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

    internal static int CalculateCount(DateTime from, DateTime to, TimeSpan interval, bool endBoundIsInclusive)
    {
        if (endBoundIsInclusive)
            to += TimeSpan.FromTicks(1); // small epsilon so e.g. exactly 1 day turns to 2

        return to < from ? 0 : (int)Math.Ceiling((to - from).TotalDays / interval.TotalDays);
    }
}

internal static class DateRangeExtensions
{
    public static DateRange Range(this DateTime from, DateTime to, TimeSpan interval)
        => new(from, to, interval);
}
