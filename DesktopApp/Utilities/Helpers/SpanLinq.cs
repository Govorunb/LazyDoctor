using JetBrains.Annotations;

namespace DesktopApp.Utilities.Helpers;

[PublicAPI]
internal static class SpanLinq
{
    public static bool All<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        foreach (var item in span)
        {
            if (!predicate(item))
                return false;
        }
        return true;
    }

    public static bool Any<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        foreach (var item in span)
        {
            if (predicate(item))
                return true;
        }
        return false;
    }

    public static bool All<T>(this Span<T> span, Func<T, bool> predicate)
        => All((ReadOnlySpan<T>)span, predicate);

    public static bool Any<T>(this Span<T> span, Func<T, bool> predicate)
        => Any((ReadOnlySpan<T>)span, predicate);
}
