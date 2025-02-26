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

    public static IEnumerable<(T First, T Second)> CrossJoin<T>(this IEnumerable<T> first, IEnumerable<T> second)
        => first.SelectMany(f => second.Select(s => (f, s)));

    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sources)
    {
        // with inputs [ABCDE,123,xyzw], this aggregates as follows:
        // ([], ABCDE) --SelectMany--> [[],[],[],[],[]] --Append--> [A,B,C,D,E]
        //     collectionSelector^            resultSelector^
        // ([A,B,C,D,E], 123) --> [A,A,A, B,B,B, ..., E,E,E] --> [A1,A2,A3, B1,B2, ..., E2,E3]
        // (..., xyzw) --> [A1x, A1y, ..., E3z, E3w]
        var empty = Enumerable.Empty<IEnumerable<T>>();
        return sources.Aggregate(empty, (acc, curr) =>
            // allocates N closures (where N is the number of sources)
            curr.SelectMany(
                collectionSelector: _ => acc,
                resultSelector: (item, seq) => seq.Append(item)
            )
            // alternative (worse because it allocates M^N closures, where M is the number of items in a source)
            // curr.SelectMany(item => acc.Select(seq => seq.Append(item))

            // the one single case where query syntax is the clearest way
            // from item in curr
            // from seq in acc
            // select seq.Append(item)
        );
    }
}
