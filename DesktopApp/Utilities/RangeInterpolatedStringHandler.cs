using System.Collections;
using System.Text;

namespace DesktopApp.Utilities;

[InterpolatedStringHandler]
public readonly struct RangeInterpolatedStringHandler : IEnumerable<string>
{
    private record Part;
    private sealed record LiteralPart(string? Literal) : Part;
    private sealed record RangePart(Range Range) : Part;

    private readonly List<Part> _parts = [];

    // ctor shape for duck typing
    // ReSharper disable twice UnusedParameter.Local
    public RangeInterpolatedStringHandler(int literalLength, int formattedCount) { }

    public void AppendLiteral(string literal) => _parts.Add(new LiteralPart(literal));

    public void AppendFormatted<T>(T t)
    {
        if (t is Range range)
        {
            if (range.End.IsFromEnd || range.Start.IsFromEnd)
                throw new ArgumentException("Range start and end must be literals", nameof(t));
            if (range.End.Value <= range.Start.Value)
                throw new ArgumentException("Range end must be greater than start", nameof(t));

            _parts.Add(new RangePart(range));
        }
        else
        {
            _parts.Add(new LiteralPart(t?.ToString()));
        }
    }

    public IEnumerator<string> GetEnumerator()
    {
        var ranges = _parts.Index()
            .Where(p => p.Item is RangePart)
            .Select(p => (p.Index, ((RangePart)p.Item).Range))
            .ToList();
        if (ranges.Count == 0)
        {
            yield return string.Concat(_parts.Cast<LiteralPart>());
            yield break;
        }

        var substitutions = ranges switch
        {
            [var (_, r)] => EnumerateRangeInclusive(r).Select(i => new[] { i }),
            _ => ranges
                .Select(p => p.Range)
                .Select(EnumerateRangeInclusive)
                .CartesianProduct(),
        };

        StringBuilder sb = new();
        foreach (var sub in substitutions)
        {
            var subItems = sub.ToList();
            var rangeIdx = 0;
            foreach (var part in _parts)
            {
                if (part is LiteralPart literalPart)
                    sb.Append(literalPart.Literal);
                else
                    sb.Append(subItems[rangeIdx++]);
            }
            yield return sb.ToString();
            sb.Clear();
        }
    }

    private static IEnumerable<int> EnumerateRangeInclusive(Range range)
        => Enumerable.Range(range.Start.Value, 1 + range.End.Value - range.Start.Value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
