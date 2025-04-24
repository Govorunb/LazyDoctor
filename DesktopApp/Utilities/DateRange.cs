using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace DesktopApp.Utilities;

[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", // ___Collection
    Justification = "Pretends to be a collection because it's countable.\n" +
                    "(future language designers, please name this kind of interface Countable instead of ReadOnlyCollection...)")]
internal sealed class DateRange(DateTime from, DateTime to, TimeSpan interval) : IReadOnlyCollection<DateTime>, ICollection<DateTime>
{
    public IEnumerator<DateTime> GetEnumerator()
    {
        for (var date = from; date < to; date += interval)
            yield return date;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => (int)((to - from) / interval);
    public bool IsReadOnly => true;
    // enumerable size check fast path only checks for ICollection... and not IReadOnlyCollection
    // the "IThing doesn't extend IReadOnlyThing" design mistake strikes again
    public void Add(DateTime item) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public bool Contains(DateTime item) => throw new NotSupportedException();
    public void CopyTo(DateTime[] array, int arrayIndex) => throw new NotSupportedException();
    public bool Remove(DateTime item) => throw new NotSupportedException();
}

internal static class DateRangeExtensions
{
    public static DateRange Range(this DateTime from, DateTime to, TimeSpan interval)
        => new(from, to, interval);
}
