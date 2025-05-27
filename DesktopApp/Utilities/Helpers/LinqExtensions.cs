using System.Runtime.InteropServices;
using ZLinq;

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

    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> valueFactory)
        where TKey : notnull
    {
        ref var reference = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out var exists);
        if (!exists)
            reference = valueFactory(key);
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

    public static IEnumerable<(T First, T Second)> CrossJoin<T>(this IEnumerable<T> first, IEnumerable<T> second)
        => first.SelectMany(f => second.Select(s => (f, s)));

    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sources)
    {
        // formally - transforms N sequences of lengths (a,b,c,...) into a×b×c×... sequences each of length N
        // e.g. with inputs [ABCDE,123,xyzw], this aggregates as follows:
        // ([], ABCDE) --> [ , , , , ] --> [A,B,C,D,E]
        // ([A,B,C,D,E], 123) --> [A,A,A, B,B,B, ..., E,E,E] --> [A1,A2,A3, B1,B2, ..., E2,E3]
        // (..., xyzw) --> [A1x, A1y, ..., E3z, E3w]
        List<List<T>> acc = [[]];
        foreach (var source in sources)
        {
#pragma warning disable CA1859 // analyzer misfire, could be any collection coming in
            if (source is not ICollection<T> itemsColl)
                itemsColl = source.ToList();
#pragma warning restore CA1859 // Use concrete collection type for improved performance
            if (itemsColl.Count == 0)
                continue;
            var newAcc = new List<List<T>>(acc.Count * itemsColl.Count);
            foreach (var accItem in acc)
            {
                newAcc.AddRange(itemsColl.Select(t => accItem.AsValueEnumerable().Append(t).ToList()));
            }

            acc = newAcc;
        }

        return acc;
    }

    public static TValue? GetValueOrDefault<TKey, TAltKey, TValue>(this Dictionary<TKey, TValue>.AlternateLookup<TAltKey> lookup, TAltKey key)
        where TKey : notnull
        where TAltKey : notnull, allows ref struct
    {
        return lookup.TryGetValue(key, out var value) ? value : default;
    }
}
