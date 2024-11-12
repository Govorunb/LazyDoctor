using DesktopApp.Recruitment;

namespace DesktopApp;

public sealed class MainWindowViewModel(RecruitTab recruitTab) : ViewModelBase
{
    public RecruitTab RecruitTab { get; } = recruitTab;
    public int SelectedTabIndex { get; set; }
}
