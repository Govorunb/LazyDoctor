using Avalonia.ReactiveUI;

namespace DesktopApp.Recruitment;

public sealed partial class RecruitTabView : ReactiveUserControl<RecruitTabViewModel>
{
    public static readonly FilterType[] FilterTypes = Enum.GetValues<FilterType>();

    public RecruitTabView()
    {
        InitializeComponent();
    }
}
