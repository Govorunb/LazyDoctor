using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using DesktopApp.Data.Operators;
using DesktopApp.Data.Recruitment;
using DesktopApp.ViewModels;
using DynamicData;
using DynamicData.Binding;

namespace DesktopApp.Recruitment;

public class RecruitTabViewModel : ViewModelBase
{
    private readonly RecruitableOperators _recruitPool;
    private static readonly Dictionary<string, List<Operator>> _resultsCache = [];

    private SourceList<Tag> Tags { get; } = new();
    private readonly ObservableCollectionExtended<TagCategory> _categories = [];
    public ReadOnlyObservableCollection<TagCategory> Categories { get; }

    private readonly ObservableCollectionExtended<Tag> _selectedTags = [];
    public ReadOnlyObservableCollection<Tag> SelectedTags { get; }
    public ReactiveCommand<Unit, Unit> ClearSelectedTags { get; }

    private SourceList<ResultRow> AllResults { get; } = new();
    private readonly ObservableCollectionExtended<ResultRow> _filteredResults = [];
    public ReadOnlyObservableCollection<ResultRow> Results { get; }

    [Reactive] public int RowsHidden { get; private set; }

    // todo: user prefs (these are sensible defaults)
    // asssuming >3h50m, which removes 1&2stars from the pool
    [Reactive] public FilterType Filter1Stars { get; set; } = FilterType.Hide;
    [Reactive] public FilterType Filter2Stars { get; set; } = FilterType.Hide;
    [Reactive] public FilterType Filter3Stars { get; set; } = FilterType.Exclude;


    public RecruitTabViewModel(RecruitableOperators operators, TagsDataSource tagSource)
    {
        Debug.Assert(operators is { } && tagSource is { }, "DI failure");
        _recruitPool = operators;

        Categories = new(_categories);
        SelectedTags = new(_selectedTags);
        Results = new(_filteredResults);

        ClearSelectedTags = ReactiveCommand.Create(() =>
        {
            foreach (var tag in _selectedTags.ToArray())
                tag.IsSelected = false;
        });

        // todo: move to TagDataSource
        tagSource.Values.Subscribe(v => Tags.Edit(l =>
            {
                l.Clear();
                l.AddRange(v);
            }))
            .DisposeWith(this);
        Tags.Connect()
            .GroupOnProperty(tag => tag.Category)
            .Transform(group => new TagCategory(group.GroupKey, group.List.Items))
            .SortByDescending(c => c.Name)
            .OnMainThread()
            .Bind(_categories)
            .Subscribe();
        Tags.Connect()
            .AutoRefresh(t => t.IsSelected, changeSetBuffer: TimeSpan.FromMilliseconds(50))
            .Filter(t => t.IsSelected)
            .SortBy(t => t.Name) // tags are sorted to make their order consistent for cache keying
            .Bind(_selectedTags)
            .ToCollection()
            .OnMainThread()
            .Subscribe(SelectedTagsUpdated);
        var starFilterChanged = this.WhenAnyValue(t => t.Filter1Stars, t => t.Filter2Stars, t => t.Filter3Stars);
        AllResults.Connect()
            .CombineLatest(starFilterChanged, (t, _) => t)
            .Filter(FilterRow)
            .SortBy(r => (-r.MinimumRarity, r.Operators.Count)) // highest rarity first; then, fewer operators
            .OnMainThread()
            .Bind(_filteredResults)
            .Subscribe();
    }

    private void SelectedTagsUpdated(IReadOnlyCollection<Tag> tags)
    {
        foreach (var tag in Tags.Items)
        {
            // max 5 selected
            tag.IsAvailable = tags.Count < 5 || tag.IsSelected;
        }

        AllResults.Edit(l =>
        {
            l.Clear();
            l.AddRange(Update([..tags]));
        });
        RowsHidden = AllResults.Count - Results.Count;
    }

    private IEnumerable<ResultRow> Update(IReadOnlyList<Tag> tags)
    {
        var powerSet = tags.GetPowerSet().ToList();
        return powerSet
            .Select(GetFilterResult)
            .Where(r => r is { })!;
    }

    private ResultRow? GetFilterResult(IEnumerable<Tag> tags)
    {
        var tagList = tags.ToList();
        if (tagList.Count == 0)
            return null;

        var key = string.Join(',', tagList.Select(t => t.Id));

        var operators = _resultsCache.GetOrAdd(key, ValueFactory);
        if (operators.Count == 0)
            return null;

        return new ResultRow { Tags = tagList, Operators = operators.ToList() }; // copy so hiding operators doesn't affect the cache

        List<Operator> ValueFactory()
            => _recruitPool.Values.MostRecent(default).First()
                .Where(op => Match(op, tagList))
                .ToList();
    }

    private bool FilterRow(ResultRow row)
    {
        Span<FilterType> filter = [Filter1Stars, Filter2Stars, Filter3Stars, FilterType.Ignore, FilterType.Ignore, FilterType.Ignore];
        List<Operator> hide = [];

        // if you explicitly pick Robot/Starter they should always show up
        foreach (var tag in row.Tags)
        {
            if (filter[0] is FilterType.Hide or FilterType.Exclude && tag.Name == "Robot")
                filter[0] = FilterType.Ignore;
            if (filter[1] is FilterType.Hide or FilterType.Exclude && tag.Name == "Starter")
                filter[1] = FilterType.Ignore;
        }

        foreach (var op in row.Operators)
        {
            switch (filter[op.RarityStars - 1])
            {
                case FilterType.Exclude:
                    return false;
                case FilterType.Hide:
                    hide.Add(op);
                    break;
                case FilterType.Require:
                    filter[op.RarityStars - 1] = FilterType.Ignore;
                    break;
                case FilterType.Ignore:
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        foreach (var op in hide)
            row.Operators.Remove(op);

        // any Requires left were not fulfilled
        foreach (var filterType in filter)
        {
            if (filterType == FilterType.Require)
                return false;
        }

        return true;
    }

    private static bool Match(Operator op, List<Tag> tags)
    {
        if (op.RarityStars == 6 && tags.All(t => t.Name != "Top Operator"))
            return false;
        return tags.All(t => t.Match(op));
    }
}

public sealed class DesignRecruitTabViewModel()
    : RecruitTabViewModel(LOCATOR.GetService<RecruitableOperators>()!, LOCATOR.GetService<TagsDataSource>()!);
