namespace DesktopApp.Utilities;

internal readonly struct ReverseComparer<T>(IComparer<T> inner) : IComparer<T>
{
    public int Compare(T? x, T? y) => -inner.Compare(x, y);

    public ReverseComparer(Comparison<T> comparison)
        : this(Comparer<T>.Create(comparison))
    {
    }
}

internal static class ReverseComparer
{
    public static IComparer<T> Reverse<T>(this IComparer<T> inner) => new ReverseComparer<T>(inner);
}
