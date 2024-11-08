using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DesktopApp.Data.Recruitment;
using DesktopApp.Utilities.Helpers;

namespace DesktopApp.Recruitment;

public class RecruitTabViewModel : ViewModelBase
{
    private readonly TagsDataSource _tagSource;
    private readonly RecruitmentFilter _filter;
    public ReadOnlyObservableCollection<TagCategory> Categories => _tagSource.Categories;
    public ReadOnlyObservableCollection<Tag> SelectedTags => _filter.SelectedTags;
    public ReadOnlyObservableCollection<ResultRow> Results => _filter.Results;
    public IReadOnlyList<RarityFilter> RarityFilters => _filter.RarityFilters;

    [Reactive] public int RowsHidden { get; private set; }

    public ReactiveCommand<Unit, Unit> ClearSelectedTags { get; }

    public RecruitTabViewModel(TagsDataSource tagSource, RecruitmentFilter filter)
    {
        AssertDI(tagSource);
        AssertDI(filter);
        _tagSource = tagSource;
        _filter = filter;

        ClearSelectedTags = ReactiveCommand.Create(() =>
        {
            foreach (var tag in SelectedTags.ToArray())
                tag.IsSelected = false;
        });

        filter.AllResults.WhenCountChanged()
            .CombineLatest(Results.WhenCountChanged(), (a, b) => a - b)
            .Subscribe(v => RowsHidden = v);
    }
}

public sealed class DesignRecruitTabViewModel()
    : RecruitTabViewModel(LOCATOR.GetService<TagsDataSource>()!, LOCATOR.GetService<RecruitmentFilter>()!);
