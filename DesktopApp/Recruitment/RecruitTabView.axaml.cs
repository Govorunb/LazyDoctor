using Avalonia.ReactiveUI;

namespace DesktopApp.Recruitment;

public sealed partial class RecruitTabView : ReactiveUserControl<RecruitTab>
{
    public static readonly FilterType[] FilterTypes = Enum.GetValues<FilterType>();

    public RecruitTabView()
    {
        InitializeComponent();
    }
}
