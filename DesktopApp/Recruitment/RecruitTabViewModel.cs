using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using DesktopApp.Common.Operators;
using DesktopApp.Data;
using DesktopApp.ViewModels;
using DynamicData;
using DynamicData.Binding;
using Operator = DesktopApp.Common.Operators.Operator;

namespace DesktopApp.Recruitment;

public class RecruitTabViewModel : ViewModelBase
{
    private SourceList<Tag> Tags { get; } = new();
    private readonly ObservableCollectionExtended<CategoryViewModel> _categories = [];
    public ReadOnlyObservableCollection<CategoryViewModel> Categories { get; }
    private readonly ObservableCollectionExtended<Operator> _operators = [];
    public ReadOnlyObservableCollection<Operator> AvailableOperators { get; }

    public RecruitTabViewModel(OperatorRepository operators, JsonDataSource<Tag[]> tagSource)
    {
        Tags.AddRange(tagSource.Value);
        Tags.Connect()
            .GroupOnProperty(tag => tag.Category)
            .Transform(group => new CategoryViewModel(group.GroupKey, group.List.Items))
            .Sort(Comparer<CategoryViewModel>.Create((c1, c2) => StringComparer.OrdinalIgnoreCase.Compare(c1.Name, c2.Name)))
            .ObserveOn(AvaloniaScheduler.Instance)
            .Bind(_categories)
            .Subscribe();
        Categories = new(_categories);
        AvailableOperators = new(_operators);
        Tags.Connect()
            .AutoRefresh(t => t.IsSelected)
            .Filter(t => t.IsSelected)
            .ToCollection()
            .ObserveOn(AvaloniaScheduler.Instance)
            .Subscribe(tags =>
            {
                using (_operators.SuspendNotifications())
                {
                    _operators.Clear();
                    if (tags.Count == 0)
                        return;

                    // todo: AND/OR behaviour for different categories
                    // one tag - exclusive, multiple tags in a category are inclusive
                    // e.g. Robot returns 1*, Senior + Top op returns 5* and 6*
                    // todo: "minimum rarity" prefs
                    // tag combo results must start with this rarity
                    // e.g. selecting 4+ will only show guaranteed 4* and above, combos that can give 3* don't show anything
                    _operators.AddRange(operators.Operators
                        .Where(op => op.Rarity != "TIER_3") // temp: ignore 3*s
                        .Where(op => tags.Any(t => t.Match(op)))
                    );
                    Console.WriteLine($"{DateTime.Now} {_operators.Count}");
                    Console.Out.Flush();
                }
            });
    }
}

public sealed class DesignRecruitTabViewModel()
    : RecruitTabViewModel(LOCATOR.GetService<OperatorRepository>()!, LOCATOR.GetService<JsonDataSource<Tag[]>>()!);
