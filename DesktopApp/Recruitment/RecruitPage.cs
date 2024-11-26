using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DesktopApp.Data.Recruitment;
using DesktopApp.Recruitment.Processing;
using DesktopApp.Utilities.Attributes;
using DesktopApp.Utilities.Helpers;

namespace DesktopApp.Recruitment;

public class RecruitPage : PageBase
{
    private readonly TagsDataSource _tagSource;
    private readonly RecruitmentFilter _filter;
    private readonly TagsOCR _ocr;

    public ReadOnlyObservableCollection<TagCategory> Categories => _tagSource.Categories;
    public ReadOnlyObservableCollection<Tag> SelectedTags => _filter.SelectedTags;
    public ReadOnlyObservableCollection<ResultRow> Results => _filter.Results;
    public IReadOnlyList<RarityFilter> RarityFilters => _filter.RarityFilters;

    public UserPrefs Prefs { get; }
    [Reactive]
    public int RowsHidden { get; private set; }
    [Reactive]
    public string? PasteError { get; internal set; }

    public ReactiveCommand<Unit, Unit> ClearSelectedTags { get; }

    public RecruitPage(TagsDataSource tagSource, RecruitmentFilter filter, TagsOCR ocr, UserPrefs prefs)
    {
        AssertDI(tagSource);
        AssertDI(filter);
        _tagSource = tagSource;
        _filter = filter;
        _ocr = ocr;
        Prefs = prefs;

        ClearSelectedTags = ReactiveCommand.Create(filter.ClearSelectedTags);

        filter.AllResults.WhenCountChanged()
            .CombineLatest(Results.WhenCountChanged(), (a, b) => a - b)
            .Subscribe(v => RowsHidden = v);

        // show PasteError only for a few seconds
        this.WhenAnyValue(t => t.PasteError)
            .WhereNotNull()
            .ToUnit()
            .Debounce(TimeSpan.FromSeconds(5))
            .OnMainThread()
            .Subscribe(_ => PasteError = null);
    }

    public void OnPaste(string text)
        => SetTagsFromPaste(TryParseTags(text));
    public void OnPaste(ReadOnlySpan<byte> pngData)
        => SetTagsFromPaste(_ocr.Process(pngData));

    private void SetTagsFromPaste(ICollection<Tag> tags)
    {
        if (tags.Count == 0)
        {
            PasteError = "No tags found, try again.";
            return;
        }

        _filter.SetSelectedTags(tags);
    }

    // if/when there's anything else that does text parsing, pull this out into a service
    private List<Tag> TryParseTags(string text)
    {
        const string Delimiters = ".,;| \t\n";
        var span = text.AsSpan();
        // todo: lang from prefs
        var allowed = (TagsOCR.GetCharacters("eng") + Delimiters).ToHashSet();
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
            if (_tagSource.GetByName(tagName) is { } tag)
                tags.Add(tag);
        }

        return tags;
    }
}

[DesignClass]
public sealed class DesignRecruitPage()
    : RecruitPage(
        LOCATOR.GetService<TagsDataSource>()!,
        LOCATOR.GetService<RecruitmentFilter>()!,
        LOCATOR.GetService<TagsOCR>()!,
        LOCATOR.GetService<UserPrefs>()!
    );