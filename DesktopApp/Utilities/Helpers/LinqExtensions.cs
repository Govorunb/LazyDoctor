using System.Runtime.InteropServices;

namespace DesktopApp.Utilities.Helpers;

public static class LinqExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> valueFactory)
        where TKey : notnull
    {
        ref var reference = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out var exists);
        if (!exists)
            reference = valueFactory();
        return reference!;
    }

    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        where TKey : notnull
    {
        ref var reference = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out var exists);
        if (!exists)
            reference = defaultValue;
        return reference!;
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        => source.Where(t => t is {})!;
}
