using DesktopApp.Recruitment;

namespace DesktopApp;

public sealed class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(RecruitTabViewModel recruitTab)
    {
        RecruitTab = recruitTab;
    }

    public RecruitTabViewModel RecruitTab { get; }
}
