using System.Diagnostics;
using DesktopApp.Data.Recruitment;
using ZLinq;

namespace DesktopApp.Data;

[JsonClass]
public sealed class GachaTable
{
    [JsonInclude, JsonPropertyName("gachaTags")]
    internal RawTagData[]? RecruitmentTags { get; set; }
    [JsonInclude]
    internal string? RecruitDetail { get; set; }
    public List<string> ParseRecruitDetails()
        => ParseRecruitDetailsCore(RecruitDetail);

    private static List<string> ParseRecruitDetailsCore(string? recruitDetails)
    {
        //★★★
        //<@rc.eml>Exclusive</> / Regular / ... / Regular
        //--------------------
        //★★★★
        //<@rc.eml>Exclusive</> / <@rc.eml>Exclusive</> / Regular / Regular / ...
        if (string.IsNullOrEmpty(recruitDetails))
            return [];

        List<string> names = [];
        ReadOnlySpan<char> span = recruitDetails;

        Span<char> buf = stackalloc char[6];
        buf.Fill('★');

        const string OpeningTag = "<@rc.eml>"; // exclusive to recruitment
        const string ClosingTag = "</>";
        const string Separator = " / ";

        for (var stars = 1; stars <= 6; stars++)
        {
            ReadOnlySpan<char> slice = buf[..stars];
            var starsIdx = span.IndexOf(slice, StringComparison.Ordinal);
            Debug.Assert(slice.AsValueEnumerable().All(c => c == '★'), $"Took less than {stars} stars: '{slice}'");
            Debug.Assert(span[starsIdx + stars + 1] != '★', $"Found more than {stars} stars");
            span = span[(starsIdx + stars + 1)..]; // +1 for line break

            while (true)
            {
                if (span.StartsWith(Separator))
                    span = span[Separator.Length..];
                if (span.StartsWith(OpeningTag))
                {
                    // <@rc.eml>THRM-EX</> / ...
                    // <@rc.eml>12F</>\n----...
                    span = span[OpeningTag.Length..];
                    var nameLen = span.IndexOf(ClosingTag, StringComparison.Ordinal);
                    names.Add(span[..nameLen].ToString());
                    span = span[(nameLen + ClosingTag.Length)..];
                }
                else
                {
                    var i = 0;
                    // consume chars for name until separator/end
                    for (; i < span.Length; i++)
                    {
                        // \n (finished)
                        if (span[i] == '\n')
                            break;
                        // end of whole string
                        if (span.Length < i + 2)
                        {
                            i = span.Length;
                            break;
                        }
                        // separator
                        if (span[i] == ' ' && span[i+1] == '/' && span[i + 2] == ' ')
                            break;
                    }
                    names.Add(span[..i].ToString());
                    span = span[i..];
                }

                if (span.Length == 0 || span[0] == '\n') // \n (finished parsing for this rarity)
                    break;
            }
        }
        return names;
    }
}
