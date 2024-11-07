namespace DesktopApp.Utilities;

public static class Combinatorics
{
    public static IEnumerable<IEnumerable<T>> GetPowerSet<T>(this IReadOnlyList<T> list)
    {
        var max = 1 << list.Count;

        for (var i = 0; i < max; i++)
        {
            yield return Subset(list, i);
        }

        static IEnumerable<T> Subset(IReadOnlyList<T> list, int mask)
        {
            uint j = 0;
            uint indexBit = 1;
            while (j < list.Count)
            {
                if ((mask & indexBit) != 0)
                {
                    yield return list[(int)j];
                }

                j++;
                indexBit *= 2;
            }
        }
    }
}
