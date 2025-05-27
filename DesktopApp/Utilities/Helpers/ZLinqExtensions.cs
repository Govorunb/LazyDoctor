using ZLinq;

namespace DesktopApp.Utilities.Helpers;

[PublicAPI]
public static class ZLinqExtensions
{
    public static ValueEnumerable<WhereNotNull<TEnumerator, TItem>, TItem> WhereNotNull<TEnumerator, TItem>(this ValueEnumerable<TEnumerator, TItem?> src)
        where TEnumerator : struct, IValueEnumerator<TItem?>, allows ref struct
    {
        return new(new(src.Enumerator));
    }
}

public ref struct WhereNotNull<TEnumerator, TItem>(TEnumerator src)
: IValueEnumerator<TItem>
where TEnumerator : struct, IValueEnumerator<TItem?>, allows ref struct
{
    private TEnumerator _src = src;

    public bool TryGetNext(out TItem current)
    {
        while (_src.TryGetNext(out var item))
        {
            if (item is null)
                continue;
            current = item;
            return true;
        }

        current = default!;
        return false;
    }

    public bool TryGetNonEnumeratedCount(out int count)
    {
        count = 0;
        return false;
    }

    public bool TryGetSpan(out ReadOnlySpan<TItem> span)
    {
        span = default;
        return false;
    }

    public bool TryCopyTo(scoped Span<TItem> destination, Index offset)
    {
        return false;
    }
    public void Dispose() => _src.Dispose();
}
