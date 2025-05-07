using System.Buffers;
using System.Diagnostics;
using DesktopApp.Data.Recruitment;

namespace DesktopApp.Recruitment.Processing;

public sealed class TagParsingUtils : ServiceBase
{
    private readonly TagsDataSource _tags;
    private static readonly SearchValues<char> _delimiters = SearchValues.Create(".,;| \t\n");
    private readonly List<Tag> _resultTags = new(RecruitmentFilter.MaxTagsSelected);

    private int _minTagLength = 3; // AoE
    public TagParsingUtils(TagsDataSource tags)
    {
        AssertDI(tags);
        _tags = tags;
        tags.Values
            .Subscribe(t => _minTagLength = t.Min(tag => tag.Name.Length))
            .DisposeWith(this);
    }

    public IReadOnlyCollection<Tag> TryParseTags(string text, string lang = "en")
    {
        Debug.WriteLine($"Raw OCR text: {text}");
        var time = TimeProvider.System;
        var now = time.GetTimestamp();
        text = text.Replace('—', '-');

        var span = text.AsSpan();

        _resultTags.Clear();
        var possibleTags = span.SplitAny(_delimiters);
        ReadOnlySpan<char> prev = default;
        foreach (var range in possibleTags)
        {
            var tagName = span[range];
            if (tagName.Length < _minTagLength)
                continue;
            // better pray HG don't release a french version
            if (lang is "en")
            {
                if (tagName is "Operator" && prev is "Top" or "Senior")
                {
                    tagName = $"{prev} Operator";
                }
                prev = tagName;
            }

            if (_tags.GetByName(tagName) is { } tag)
            {
                _resultTags.Add(tag);
                if (_resultTags.Count >= RecruitmentFilter.MaxTagsSelected)
                    break;
            }
        }

        var took = time.GetElapsedTime(now);
        this.Log().Debug($"Parsed {_resultTags.Count} tags in {took.TotalMicroseconds}us");
        return _resultTags;
    }
}
