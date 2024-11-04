using DesktopApp.ViewModels;

namespace DesktopApp.Recruitment;

public sealed class CategoryViewModel(string name, IEnumerable<Tag> tags) : ViewModelBase
{
    public string Name { get; } = name;
    public IEnumerable<Tag> Tags { get; } = tags;
}
