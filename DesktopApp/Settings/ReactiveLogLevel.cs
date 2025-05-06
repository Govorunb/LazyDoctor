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

    public ReactiveLogLevel(LoggingLevelSwitch switcher)
    {
        DefaultLevel = switcher.MinimumLevel;
        Switch = switcher;
        IDisposable? notifySubscription = null;
        this.WhenAnyValue(t => t.Switch)
            .Subscribe(sw =>
            {
                notifySubscription?.Dispose();
                notifySubscription = this.NotifyProperty(nameof(Level),
                    Observable.FromEventPattern<LoggingLevelSwitchChangedEventArgs>(
                        eh => sw.MinimumLevelChanged += eh,
                        eh => sw.MinimumLevelChanged -= eh)
                );
                this.RaisePropertyChanged(nameof(Level));
            });
    }

    [JsonConstructor]
    public ReactiveLogLevel(SeriLogLevel level = SeriLogLevel.Information)
        : this(new LoggingLevelSwitch(level))
    {
    }
}
