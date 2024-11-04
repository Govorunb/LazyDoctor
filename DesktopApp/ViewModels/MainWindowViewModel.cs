using DesktopApp.Recruitment;

namespace DesktopApp.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(RecruitTabViewModel recruitVm)
    {
        RecruitTab = recruitVm;
    }

    public RecruitTabViewModel RecruitTab { get; }
}
