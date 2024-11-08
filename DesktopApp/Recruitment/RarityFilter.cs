namespace DesktopApp.Recruitment;

public sealed class RarityFilter : ViewModelBase
{
    public int Stars { get; init; } // for UI
    [Reactive]
    public FilterType Filter { get; set; }
}
