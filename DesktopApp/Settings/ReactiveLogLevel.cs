using System.Reactive.Linq;
using System.Text.Json.Serialization;
using DesktopApp.Utilities.Attributes;
using DesktopApp.Utilities.Helpers;
using Serilog.Core;

namespace DesktopApp.Settings;

[JsonClass]
public class ReactiveLogLevel : ReactiveObjectBase
{
    // ReSharper disable once CollectionNeverQueried.Global // used in UI
    public static readonly SeriLogLevel[] LogLevels = Enum.GetValues<SeriLogLevel>();

    [JsonIgnore]
    public LoggingLevelSwitch Switch { get; set; }

    public SeriLogLevel Level
    {
        get => Switch.MinimumLevel;
        set => Switch.MinimumLevel = value;
    }

    public SeriLogLevel DefaultLevel { get; }

    public ReactiveLogLevel(SeriLogLevel level = SeriLogLevel.Information)
    {
        DefaultLevel = level;
        Switch = new LoggingLevelSwitch(level);
        this.NotifyProperty(nameof(Level),
            Observable.FromEventPattern<LoggingLevelSwitchChangedEventArgs>(
                eh => Switch.MinimumLevelChanged += eh,
                eh => Switch.MinimumLevelChanged -= eh)
        );
    }
}
