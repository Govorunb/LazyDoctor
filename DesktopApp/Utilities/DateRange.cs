using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace DesktopApp.Utilities;

[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", // ___Collection
    Justification = "Pretends to be a collection because it's countable.\n" +
                    "(future language designers, please name this kind of interface Countable instead of ReadOnlyCollection...)")]
internal struct DateRange(DateTime from, DateTime to, TimeSpan interval, bool endBoundIsInclusive = true) : IReadOnlyCollection<DateTime>, ICollection<DateTime>, IEnumerator<DateTime>
{
    private DateTime To { get; } = AsBound(to, endBoundIsInclusive);

    #region Collection
    public int Count => CalculateCount(from, To, interval);
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
    #endregion Collection

    #region Enumerator
    public IEnumerator<DateTime> GetEnumerator() => _fresh ? this : new DateRange(from, to, interval, endBoundIsInclusive);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    private bool _fresh = true;
    public DateTime Current { get; private set; }
    object IEnumerator.Current => Current;

    public void Reset()
    {
        _fresh = true;
        Current = default;
    }
    public void Dispose() => Reset();
    public bool MoveNext()
    {
        if (_fresh)
        {
            _fresh = false;
            Current = from;
            return true;
        }
        Current += interval;
        return Current < To;
    }
    #endregion Enumerator
    // NOTE: ignoring overflows for perf (reasonable inputs won't have a problem)
    private static int CalculateCount(DateTime from, DateTime to, TimeSpan interval) => to < from ? 0
        : (int)Math.Ceiling((to - from).Ticks / (double)interval.Ticks);
    // NOTE: intentionally breaks at DateTime.MaxValue (perf)
    private static DateTime AsBound(DateTime time, bool inclusive) => inclusive ? time.AddTicks(1) : time;

    public static implicit operator DateRange((DateTime from, DateTime to, TimeSpan interval, bool endBoundIsInclusive) tuple)
        => new(tuple.from, tuple.to, tuple.interval, tuple.endBoundIsInclusive);
}

internal static class DateRangeExtensions
{
    public static DateRange Range(this DateTime from, DateTime to, TimeSpan interval)
        => new(from, to, interval);
}
