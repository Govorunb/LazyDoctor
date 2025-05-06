using System.Collections.ObjectModel;

namespace DesktopApp.ResourcePlanner;

[JsonClass]
public sealed class SanityLog : ViewModelBase
{
    // internal for deserialization
    // ReSharper disable once InconsistentNaming
    [JsonInclude, JsonPropertyName("changes")]
    internal List<SanityChange> _changes = [];
    [JsonIgnore] // https://github.com/dotnet/runtime/issues/80113#issuecomment-1385756271
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
    // on one hand, it makes sense that properties without a setter aren't written if you set IgnoreReadOnlyProperties
    // on the other hand, however, STJ already matches property names to ctor parameters... so it clearly knows they are *meant* to be (de)serialized
    [JsonInclude] public int Delta { get; } = delta;
    [JsonInclude] public string Comment { get; } = comment;
    [JsonInclude] public string? Details { get; } = details; // for tooltip

    public bool IsGain => Delta > 0;
    public bool IsLoss => Delta < 0;
    public override string ToString() => Comment;
}
