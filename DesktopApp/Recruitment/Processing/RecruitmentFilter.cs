using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using DesktopApp.Data.Operators;
using DesktopApp.Data.Recruitment;
using DesktopApp.Utilities.Helpers;
using DynamicData;
using DynamicData.Binding;

namespace DesktopApp.Recruitment.Processing;

public sealed class RecruitmentFilter : ReactiveObjectBase
{
    private static readonly Dictionary<string, List<Operator>> _resultsCache = [];

    private readonly TagsDataSource _tags;
    private readonly SourceList<ResultRow> _allResultsList = new();
    private readonly UserPrefs _prefs;
    private ImmutableArray<Operator>? _recruitableOps;

    private readonly ObservableCollectionExtended<Tag> _selectedTags = [];
    private readonly ObservableCollectionExtended<ResultRow> _allResults = [];
    private readonly ObservableCollectionExtended<ResultRow> _filteredResults = [];
    public ReadOnlyObservableCollection<Tag> SelectedTags { get; }
    public ReadOnlyObservableCollection<ResultRow> AllResults { get; }
    public ReadOnlyObservableCollection<ResultRow> Results { get; }

    [Reactive]
    public IReadOnlyList<RarityFilter> RarityFilters { get; private set; }

    public RecruitmentFilter(RecruitableOperators ops, TagsDataSource tags, UserPrefs prefs)
    {
        AssertDI(ops);
        AssertDI(tags);
        AssertDI(prefs);
        _tags = tags;
        _prefs = prefs;

        RarityFilters = [];
        _prefs.Loaded.Subscribe(_ => SetFilters());

        ops.Values.Subscribe(v => _recruitableOps = v);

        SelectedTags = new(_selectedTags);
        Results = new(_filteredResults);
        AllResults = new(_allResults);

        var filtersChanged = this.WhenAnyValue(t => t.RarityFilters)
            .Switch(rf => rf
                .Select(f => f.WhenPropertyChanged(t => t.Filter))
                .Merge()
            );
        var dataChanged = ops.Values.ToUnit()
            .Merge(tags.Values.ToUnit());

        filtersChanged.SubscribeAsync(async f =>
        {
            await _prefs.Loaded.FirstAsync();

            var i = f.Sender.Stars - 1;
            _prefs.Recruitment!.RarityFilters[i] = f.Value;
            await _prefs.Save();
        });

        dataChanged
            .Debounce(TimeSpan.FromMilliseconds(100))
            .OnMainThread()
            .Subscribe(_ =>
            {
                // it's not worth the effort to reselect/refilter everything (at least right now)
                foreach (var selectedTag in _selectedTags)
                {
                    selectedTag.IsSelected = false;
                }

                _resultsCache.Clear();
                _allResultsList.Clear();
            });

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
            .SkipWhile(_ => _recruitableOps is null)
            .OnMainThread()
            .Subscribe(_ => UpdateFilter(_selectedTags));
    }

    private void SetFilters()
    {
        RarityFilters = _prefs.Recruitment!.RarityFilters.Select((ft, i) => new RarityFilter { Stars = i + 1, Filter = ft }).ToList();
    }

    public const int MaxTagsSelected = 5;

    private void UpdateFilter(ObservableCollectionExtended<Tag> tags)
    {
        foreach (var tag in _tags.Tags.Items)
        {
            tag.IsAvailable = tags.Count < MaxTagsSelected || tag.IsSelected;
        }

        _allResultsList.EditDiff(Update([..tags]));
    }

    private bool FilterRow(ResultRow row)
    {
        Debug.Assert(RarityFilters.Count > 0, "empty filters");

        Span<FilterType> filter = stackalloc FilterType[RarityFilters.Count];
        for (var i = 0; i < RarityFilters.Count; i++)
        {
            filter[i] = RarityFilters[i].Filter;
        }
        List<Operator> hide = [];

        // if you explicitly pick Robot/Starter they should always show up
        foreach (var tag in row.Tags)
        {
            if (tag.IsAutoSelected)
                continue;
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

        row.ShownOperators = row.Operators.ToList();

        foreach (var op in hide)
            row.ShownOperators.Remove(op);

        // remaining Requires must be unfulfilled
        return row.ShownOperators.Count > 0 && !filter.Any(f => f is FilterType.Require);
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
        {
            Debug.Assert(_recruitableOps is { }, "recruitable ops pool not loaded yet");

            return _recruitableOps.Value
                .Where(op => Match(op, tagList))
                .ToList();
        }
    }

    private static bool Match(Operator op, List<Tag> tags)
    {
        if (op.RarityStars == 6 && tags.All(t => t.Name != "Top Operator"))
            return false;

        foreach (var t in tags) // hot method, not using .All because of lambda alloc
        {
            if (!t.Match(op))
                return false;
        }

        return true;
    }

    public void ClearSelectedTags()
    {
        foreach (var tag in SelectedTags.ToArray())
            tag.IsSelected = false;
    }

    public void SetSelectedTags(IEnumerable<Tag> tags, bool autoSelected = true)
    {
        ClearSelectedTags();
        foreach (var tag in tags.Take(MaxTagsSelected))
        {
            tag.IsAutoSelected = autoSelected;
            tag.IsSelected = true;
        }
    }
}
