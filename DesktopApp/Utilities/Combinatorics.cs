namespace DesktopApp.Utilities;

public static class Combinatorics
{
    public static IEnumerable<IEnumerable<T>> GetPowerSet<T>(this IReadOnlyList<T> list)
    {
        if (list.Count > 63)
            throw new InvalidOperationException("Too many items");
        var max = 1UL << list.Count;

        for (var mask = 0UL; mask < max; mask++)
        {
            yield return Subset(list, mask);
        }

        static IEnumerable<T> Subset(IReadOnlyList<T> list, ulong mask)
        {
            var i = 0;
            var indexBit = 1UL;
            while (i < list.Count)
            {
                if ((mask & indexBit) != 0)
                {
                    yield return list[i];
                }

                i++;
                indexBit *= 2;
            }
        }
    }
}
