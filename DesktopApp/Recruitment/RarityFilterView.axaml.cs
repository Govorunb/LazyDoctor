using System.Reactive.Linq;
using Avalonia;
using Avalonia.ReactiveUI;
using DesktopApp.Utilities.Helpers;

namespace DesktopApp.Recruitment;

public sealed partial class RarityFilterView : ReactiveUserControl<RarityFilter>
{
    public RarityFilterView()
    {
        InitializeComponent();
        Label[!ContentProperty] = this.WhenAnyValue(t => t.ViewModel)
            .WhereNotNull()
            .Select(vm =>
            {
                var stars = vm.Stars;
                return "★".Repeat(stars) + "☆".Repeat(6-stars);
            }).ToBinding();
    }
}
