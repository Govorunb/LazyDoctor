using System.Runtime.InteropServices;

namespace DesktopApp.Utilities.Helpers;

public static class LinqExtensions
{
    // net9: .NET 9 has this
    public static IEnumerable<(T Item, int Index)> WithIndex<T>(this IEnumerable<T> source)
        => source.Select((item, index) => (item, index));

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
}
