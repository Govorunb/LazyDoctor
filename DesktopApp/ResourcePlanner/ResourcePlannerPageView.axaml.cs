using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace DesktopApp.ResourcePlanner;

public partial class ResourcePlannerPageView : ReactiveUserControl<ResourcePlannerPage>
{
    public ResourcePlannerPageView()
    {
        InitializeComponent();
    }
}
