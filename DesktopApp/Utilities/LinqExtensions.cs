namespace DesktopApp.Utilities;

public static class LinqExtensions
{
    public static IEnumerable<(T Item, int Index)> WithIndex<T>(this IEnumerable<T> source)
        => source.Select((item, index) => (item, index));

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> valueFactory)
    {
        return dict.TryGetValue(key, out var value) ? value
            : dict[key] = valueFactory();
    }

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        => GetOrAdd(dict, key, () => defaultValue);
}
