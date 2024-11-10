using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DesktopApp.Data.Operators;
using DesktopApp.Data.Recruitment;
using DesktopApp.Utilities.Helpers;
using DynamicData;
using DynamicData.Binding;

namespace DesktopApp.Recruitment;

public sealed class RecruitmentFilter : ReactiveObjectBase
{
    private static readonly Dictionary<string, List<Operator>> _resultsCache = [];

    private readonly RecruitableOperators _recruitPool;
    private readonly TagsDataSource _tags;
    private readonly SourceList<ResultRow> _allResultsList = new();

    private readonly ObservableCollectionExtended<Tag> _selectedTags = [];
    private readonly ObservableCollectionExtended<ResultRow> _allResults = [];
    private readonly ObservableCollectionExtended<ResultRow> _filteredResults = [];
    public ReadOnlyObservableCollection<Tag> SelectedTags { get; }
    public ReadOnlyObservableCollection<ResultRow> AllResults { get; }
    public ReadOnlyObservableCollection<ResultRow> Results { get; }

    // todo: user prefs (these are sensible defaults)
    // asssuming >3h50m, which removes 1&2stars from the pool
    private static readonly FilterType[] _defaultFilters = [FilterType.Hide, FilterType.Hide, FilterType.Exclude];
    public IReadOnlyList<RarityFilter> RarityFilters { get; }

    public RecruitmentFilter(RecruitableOperators ops, TagsDataSource tags)
    {
        _recruitPool = ops;
        _tags = tags;

        RarityFilters = [.._defaultFilters.Select((ft, i) => new RarityFilter { Stars = i + 1, Filter = ft })];
        SelectedTags = new(_selectedTags);
        Results = new(_filteredResults);
        AllResults = new(_allResults);

        var filtersChanged = RarityFilters
            .Select(f => f.WhenPropertyChanged(t => t.Filter))
            .Merge();

        _allResultsList.Connect()
            .Bind(_allResults)
            .AutoRefreshOnObservable(_ => filtersChanged)
            .Filter(FilterRow)
            .SortBy(r => (-r.MinimumRarity, r.Operators.Count)) // highest rarity first; then, fewer operators
            .OnMainThread()
            .Bind(_filteredResults)
            .Subscribe();

        tags.Tags.Connect()
            .AutoRefresh(t => t.IsSelected, changeSetBuffer: TimeSpan.FromMilliseconds(50))
            .Filter(t => t.IsSelected)
            .SortBy(t => t.Name) // tags are sorted to make their order consistent for cache keying
            .Bind(_selectedTags)
            .OnMainThread()
            .Subscribe();

        _selectedTags.ObserveCollectionChanges()
            .OnMainThread()
            .Subscribe(_ => UpdateFilter(_selectedTags));
    }

    private void UpdateFilter(IReadOnlyCollection<Tag> tags)
    {
        foreach (var tag in _tags.Tags.Items)
        {
            // max 5 selected
            tag.IsAvailable = tags.Count < 5 || tag.IsSelected;
        }

        _allResultsList.EditDiff(Update([..tags]));
    }

    private bool FilterRow(ResultRow row)
    {
        // todo: show/hide should be on ResultRow
        // when switching filter between the two, result row itself (and thus row.Operators) doesn't (and shouldn't) change
        // alternatively, duplicate each ResultRow
        Span<FilterType> filter = stackalloc FilterType[RarityFilters.Count];
        foreach (var (f, i) in RarityFilters.Select((rf, i) => (rf.Filter, i)))
        {
            filter[i] = f;
        }
        List<Operator> hide = [];

        // if you explicitly pick Robot/Starter they should always show up
        foreach (var tag in row.Tags)
        {
            if (filter[0] is FilterType.Hide or FilterType.Exclude && tag.Name == "Robot")
                filter[0] = FilterType.Show;
            if (filter[1] is FilterType.Hide or FilterType.Exclude && tag.Name == "Starter")
                filter[1] = FilterType.Show;
        }

        foreach (var op in row.Operators)
        {
            if (op.RarityStars > filter.Length)
                continue;

            switch (filter[op.RarityStars - 1])
            {
                case FilterType.Exclude:
                    return false;
                case FilterType.Hide:
                    hide.Add(op);
                    break;
                case FilterType.Require:
                    filter[op.RarityStars - 1] = FilterType.Show;
                    break;
                case FilterType.Show:
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

        var key = string.Join(',', tagList.Select(t => t.Name));

        var operators = _resultsCache.GetOrAdd(key, ValueFactory);
        if (operators.Count == 0)
            return null;

        return new ResultRow { Tags = tagList, Operators = operators.ToList() }; // copy so hiding operators doesn't affect the cache

        List<Operator> ValueFactory()
            => _recruitPool.Values.MostRecent(default).First()
                .Where(op => Match(op, tagList))
                .ToList();
    }

    private static bool Match(Operator op, List<Tag> tags)
    {
        if (op.RarityStars == 6 && tags.All(t => t.Name != "Top Operator"))
            return false;
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        // lambda alloc
        foreach (var t in tags)
        {
            if (!t.Match(op)) return false;
        }

        return true;
    }
}
