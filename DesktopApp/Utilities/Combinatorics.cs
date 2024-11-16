namespace DesktopApp.Utilities;

public static class Combinatorics
{
    public static IEnumerable<IEnumerable<T>> GetPowerSet<T>(this IReadOnlyList<T> list)
    {
        var max = 1 << list.Count;

        for (var mask = 0; mask < max; mask++)
        {
            yield return Subset(list, mask);
        }

        static IEnumerable<T> Subset(IReadOnlyList<T> list, int mask)
        {
            uint i = 0;
            uint indexBit = 1;
            while (i < list.Count)
            {
                if ((mask & indexBit) != 0)
                {
                    yield return list[(int)i];
                }

                i++;
                indexBit *= 2;
            }
        }
    }
}
