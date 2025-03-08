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

    public void Log(int delta, string comment)
    {
        if (delta == 0)
        {
            this.Log().Debug($"Sanity change of 0 ignored: {comment}");
            return;
        }
        _changes.Add(new(delta, comment));
        CurrentValue += delta;
    }
}

[JsonClass]
public sealed class SanityChange(int delta, string comment) : ViewModelBase
{
    public int Delta { get; } = delta;
    public string Comment { get; } = comment;

    public bool IsGain => Delta > 0;
    public override string ToString() => $"{(IsGain ? "+" : "")}{Delta} ({Comment})";
}
