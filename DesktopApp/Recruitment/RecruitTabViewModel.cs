using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DesktopApp.Common.Operators;
using DesktopApp.Data;
using DesktopApp.ViewModels;
using DynamicData;
using DynamicData.Binding;

namespace DesktopApp.Recruitment;

public class RecruitTabViewModel : ViewModelBase
{
    private SourceList<Tag> Tags { get; } = new();
    private readonly ObservableCollectionExtended<TagCategory> _categories = [];
    public ReadOnlyObservableCollection<TagCategory> Categories { get; }
    private SourceList<ResultRow> AllResults { get; } = new();
    private readonly ObservableCollectionExtended<ResultRow> _filteredResults = [];
    public ReadOnlyObservableCollection<ResultRow> Results { get; }

    [Reactive] public FilterType Filter1Stars { get; set; }
    [Reactive] public FilterType Filter2Stars { get; set; }
    [Reactive] public FilterType Filter3Stars { get; set; }

    private readonly RecruitableOperators _pool;

    private static readonly Dictionary<string, List<Operator>> _resultsCache = [];

    public RecruitTabViewModel(RecruitableOperators operators, JsonDataSource<Tag[]> tagSource)
    {
        _pool = operators;
        Categories = new(_categories);
        Results = new(_filteredResults);
        tagSource.Values.Subscribe(v => Tags.Edit(l =>
            {
                l.Clear();
                l.AddRange(v);
            }))
            .DisposeWith(this);
        Tags.Connect()
            .GroupOnProperty(tag => tag.Category)
            .Transform(group => new TagCategory(group.GroupKey, group.List.Items))
            .Sort(Comparer<TagCategory>.Create((c1, c2) => StringComparer.Ordinal.Compare(c1.Name, c2.Name)))
            .Reverse()
            .OnMainThread()
            .Bind(_categories)
            .Subscribe();
        var starFilterChanged = this.WhenAnyValue(t => t.Filter1Stars, t => t.Filter2Stars, t => t.Filter3Stars);
        Tags.Connect()
            .AutoRefresh(t => t.IsSelected)
            .Filter(t => t.IsSelected)
            .ToCollection()
            .CombineLatest(starFilterChanged, (t, _) => t)
            .Subscribe(t => AllResults.Edit(l =>
            {
                l.Clear();
                l.AddRange(Update([.. t]));
            }));
        AllResults.Connect()
            .Filter(FilterRow)
            .Sort(Comparer<ResultRow>.Create((r1, r2) =>
            {
                // highest rarity first; then, fewer operators
                var minRarityComparison = r1.Operators.Min(op => op.RarityStars).CompareTo(r2.Operators.Min(op => op.RarityStars));
                return minRarityComparison != 0 ? minRarityComparison
                    : r1.Tags.Count.CompareTo(r2.Tags.Count);
            }))
            .Reverse()
            .OnMainThread()
            .Bind(_filteredResults)
            .Subscribe();
        Tags.Connect()
            .AutoRefresh(t => t.IsSelected)
            .Filter(t => t.IsSelected)
            .ToCollection()
            .Subscribe(c =>
            {
                if (c.Count >= 5)
                {
                    foreach (var tag in Tags.Items)
                    {
                        tag.IsAvailable = tag.IsSelected;
                    }
                }
                else
                {
                    foreach (var tag in Tags.Items)
                    {
                        tag.IsAvailable = true;
                    }
                }
            });
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
        var operators = _resultsCache.GetOrAdd(key, () => _pool.Values.MostRecent(default).First().Where(op => Match(op, tagList)).ToList());
        if (operators.Count == 0)
            return null;

        return new ResultRow { Tags = tagList, Operators = operators };
    }

    private bool FilterRow(ResultRow row)
    {
        FilterType[] filter = [Filter1Stars, Filter2Stars, Filter3Stars, FilterType.Ignore, FilterType.Ignore, FilterType.Ignore];
        List<Operator> hide = [];
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
        return filter.All(f => f != FilterType.Require);
    }

    private static bool Match(Operator op, IEnumerable<Tag> tags)
    {
        return tags.All(t => t.Match(op));
    }
}

public sealed class DesignRecruitTabViewModel()
    : RecruitTabViewModel(LOCATOR.GetService<RecruitableOperators>()!, LOCATOR.GetService<JsonDataSource<Tag[]>>()!);
