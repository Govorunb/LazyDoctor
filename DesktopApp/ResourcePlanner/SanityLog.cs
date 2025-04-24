using System.Collections.ObjectModel;
using DesktopApp.Utilities.Attributes;

namespace DesktopApp.ResourcePlanner;

[JsonClass]
public sealed class SanityLog : ViewModelBase
{
    private readonly List<SanityChange> _changes;
    public ReadOnlyCollection<SanityChange> Changes => new(_changes);
    [Reactive]
    public int CurrentValue { get; private set; }

    public SanityLog(params IEnumerable<SanityChange> changes)
    {
        _changes = changes.ToList();
        CurrentValue = _changes.Sum(c => c.Delta);
    }

    public void Log(int delta, string comment, string? details = null)
    {
        // changes of 0 used to be ignored, now they just act as markers/comments
        _changes.Add(new(delta, comment, details));
        CurrentValue += delta;
    }
}

[JsonClass]
public sealed class SanityChange(int delta, string comment, string? details = null) : ViewModelBase
{
    public int Delta { get; } = delta;
    public string Comment { get; } = comment;
    public string? Details { get; } = details; // for tooltip

    public bool IsGain => Delta > 0;
    public override string ToString() => $"{(IsGain ? "+" : "")}{Delta} ({Comment})";
}
