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

        static IEnumerable<T> Subset(IReadOnlyList<T> list, int i)
        {
            uint j = 0;
            uint mask = 1;
            while (j < list.Count)
            {
                if ((i & mask) != 0)
                {
                    yield return list[(int)j];
                }
                j++;
                mask *= 2;
            }
        }
    }
}
