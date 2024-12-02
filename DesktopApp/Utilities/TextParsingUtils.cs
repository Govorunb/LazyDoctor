using DesktopApp.Data.Recruitment;
using DesktopApp.Recruitment;
using DesktopApp.Recruitment.Processing;

namespace DesktopApp.Utilities;

public sealed class TextParsingUtils(TagsDataSource tagSource)
{
    public List<Tag> TryParseTags(string text, string lang = "en")
    {
        const string Delimiters = ".,;| \t\n";
        text = text.Replace('—', '-');
        var span = text.AsSpan();
        var allowed = (GetAllowedCharactersInTags(lang) + Delimiters).ToHashSet();
        Span<char> filteredSpan = stackalloc char[span.Length];
        var j = 0;
        foreach (var c in span)
        {
            if (allowed.Contains(c))
                filteredSpan[j++] = c;
        }

        var filtered = filteredSpan[..j].ToString();
        var possibleTags = filtered.Split(Delimiters.ToCharArray());
        for (var i = 0; i < possibleTags.Length; i++)
        {
            possibleTags[i] = possibleTags[i].Trim();
        }

        var tags = new List<Tag>(Math.Min(RecruitmentFilter.MaxTagsSelected, possibleTags.Length));
        for (var i = 0; i < possibleTags.Length; i++)
        {
            if (tags.Count >= RecruitmentFilter.MaxTagsSelected)
                break;

            var tagName = possibleTags[i];
            var next = i >= possibleTags.Length - 1 ? null
                : possibleTags[i + 1];
            if (next is "Operator" && tagName is "Top" or "Senior")
            {
                tagName = $"{tagName} Operator";
                i++;
            }
            if (tagSource.GetByName(tagName) is { } tag)
                tags.Add(tag);
        }

        return tags;
    }

    private static readonly Dictionary<string, string> _allowedCharactersInTags = new()
    {
        ["en"] = new string(EmbeddedTagsData.GetKnownTags().SelectMany(t => t.Name).Distinct().Order().ToArray()),
    };
    public static string? GetAllowedCharactersInTags(string lang)
    {
        if (lang.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            lang = "en";
        return _allowedCharactersInTags.GetValueOrDefault(lang);
    }
}
