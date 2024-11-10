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
    }
}
