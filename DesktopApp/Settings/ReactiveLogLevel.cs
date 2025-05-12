using Serilog.Core;

namespace DesktopApp.Settings;

public sealed class ReactiveLogLevel : ReactiveObjectBase
{
    private LoggingLevelSwitch Switch { get; }

    public SeriLogLevel Level
    {
        get => Switch.MinimumLevel;
        set => Switch.MinimumLevel = value;
    }

    public ReactiveLogLevel(LoggingLevelSwitch? switcher = null)
    {
        switcher ??= new();
        Switch = switcher;
        this.NotifyProperty(nameof(Level),
            Observable.FromEventPattern<LoggingLevelSwitchChangedEventArgs>(
                eh => switcher.MinimumLevelChanged += eh,
                eh => switcher.MinimumLevelChanged -= eh)
        );
    }
}
