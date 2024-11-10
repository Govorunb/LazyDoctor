using DesktopApp.Recruitment;

namespace DesktopApp;

public sealed class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(RecruitTab recruitTab)
    {
        RecruitTab = recruitTab;
    }

    public RecruitTab RecruitTab { get; }
}
