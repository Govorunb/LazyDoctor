using System.Collections.ObjectModel;
using System.Reactive;
using Windows.Storage.Streams;
using DesktopApp.Data;
using DesktopApp.Data.Recruitment;
using DesktopApp.Recruitment.Processing;

namespace DesktopApp.Recruitment;

public class RecruitPage : PageBase
{
    public override string PageId => Constants.RecruitPageId;

    private readonly TagsDataSource _tagSource;
    private readonly RecruitmentFilter _filter;
    private readonly IRecruitTagsOCR _ocr;
    private readonly TagParsingUtils _parser;

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

    public RecruitPage(TagsDataSource tagSource, RecruitmentFilter filter, IRecruitTagsOCR ocr, UserPrefs prefs, TagParsingUtils parser)
    {
        AssertDI(tagSource);
        AssertDI(filter);
        AssertDI(ocr);
        AssertDI(prefs);
        AssertDI(parser);
        _tagSource = tagSource;
        _filter = filter;
        _ocr = ocr;
        Prefs = prefs;
        _parser = parser;

        ClearSelectedTags = ReactiveCommand.Create(filter.ClearSelectedTags);

        filter.AllResults.WhenCountChanged()
            .CombineLatest(Results.WhenCountChanged(), (a, b) => a - b)
            .Subscribe(v => RowsHidden = v);
        this.NotifyProperty(nameof(RarityFilters), filter.WhenAnyValue(t => t.RarityFilters));

        // show PasteError only for a few seconds
        this.WhenAnyValue(t => t.PasteError)
            .WhereNotNull()
            .ToUnit()
            .Debounce(TimeSpan.FromSeconds(5))
            .OnMainThread()
            .Subscribe(_ => PasteError = null);
    }

    public void OnPaste(string text)
        => SetTagsFromPaste(_parser.TryParseTags(text));
    public void OnPaste(IRandomAccessStream stream)
    {
        Task.Run(() =>
            _ocr.Process(stream.AsStreamForRead())
                .ContinueWith(t => OnPaste(string.Join('|', t.Result.Select(r => r.FullText))))
        );
    }

    private void SetTagsFromPaste(IReadOnlyCollection<Tag> tags)
    {
        if (tags.Count == 0)
        {
            PasteError = "No tags found, try again.";
            return;
        }

        _filter.SetSelectedTags(tags);
        PasteError = null;
    }
}

[DesignClass]
internal sealed class DesignRecruitPage()
    : RecruitPage(
        LOCATOR.GetService<TagsDataSource>()!,
        LOCATOR.GetService<RecruitmentFilter>()!,
        LOCATOR.GetService<IRecruitTagsOCR>()!,
        LOCATOR.GetService<UserPrefs>()!,
        LOCATOR.GetService<TagParsingUtils>()!
    );
