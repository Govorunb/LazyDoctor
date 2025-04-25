using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using DesktopApp.Utilities.Attributes;

namespace DesktopApp.ResourcePlanner;

[JsonClass]
public sealed class SanityLog : ViewModelBase
{
    // internal for deserialization
    // ReSharper disable once InconsistentNaming
    [JsonInclude, JsonPropertyName("changes")]
    internal List<SanityChange> _changes = [];
    [JsonIgnore]
    public ReadOnlyCollection<SanityChange> Changes => new(_changes);
    [Reactive]
    public int CurrentValue { get; private set; }

    public void Log(int delta, string comment, string? details = null)
    {
        // changes of 0 act as markers/comments
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

    [JsonIgnore]
    public bool IsGain => Delta > 0;
    [JsonIgnore]
    public bool IsLoss => Delta < 0;
    public override string ToString() => Comment;
}
