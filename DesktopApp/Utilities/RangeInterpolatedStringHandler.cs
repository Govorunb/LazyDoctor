using System.Collections;
using System.Text;

namespace DesktopApp.Utilities;

[InterpolatedStringHandler]
public readonly struct RangeInterpolatedStringHandler : IEnumerable<string>
{
    private record Part;
    private sealed record LiteralPart(string Literal) : Part;
    private sealed record RangePart(Range Range) : Part;

    private readonly List<Part> _parts = [];

    // ctor shape for duck typing
    // ReSharper disable twice UnusedParameter.Local
    public RangeInterpolatedStringHandler(int literalLength, int formattedCount) { }

    public void AppendLiteral(string literal) => _parts.Add(new LiteralPart(literal));

    public void AppendFormatted<T>(T t)
    {
        if (t is not Range range)
            throw new ArgumentException($"{nameof(t)} must be of type {nameof(Range)}", nameof(t));
        if (range.End.IsFromEnd || range.Start.IsFromEnd)
            throw new ArgumentException("Range start and end must be literals", nameof(t));

        _parts.Add(new RangePart(range));
    }

    public IEnumerator<string> GetEnumerator()
    {
        var ranges = _parts.Index().Where(p => p.Item is RangePart).ToList();
        if (ranges.Count > 1)
            throw new InvalidOperationException("Only one range is supported for now");

        var range = (ranges[0].Item as RangePart)!.Range;
        var start = range.Start.Value;
        var end = range.End.Value;
        var size = end - start;
        StringBuilder sb = new();
        foreach (var sub in Enumerable.Range(start, size))
        {
            foreach (var part in _parts)
            {
                if (part is LiteralPart literalPart)
                    sb.Append(literalPart.Literal);
                else
                    sb.Append(sub);
            }
            yield return sb.ToString();
            sb.Clear();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
