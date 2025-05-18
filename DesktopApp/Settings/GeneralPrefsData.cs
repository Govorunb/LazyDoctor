using Serilog.Core;
using Constants = DesktopApp.Data.Constants;

namespace DesktopApp.Settings;

[JsonClass]
public sealed class GeneralPrefsData : ModelBase
{
    [Reactive]
    public bool ManualRefreshAcknowledged { get; set; }

    // TODO: profiles so you can quickly switch between servers
    [Reactive]
    public TimeOnly ServerReset { get; set; } = Constants.EnServerReset;
    [Reactive]
    public TimeSpan ServerTimezone { get; set; } = Constants.EnServerTimezone;

    private ReactiveLogLevel FileLogLevelSwitch { get; }
    public SeriLogLevel FileLogLevel
    {
        get => FileLogLevelSwitch.Level;
        set => FileLogLevelSwitch.Level = value;
    }

    public GeneralPrefsData()
    {
        FileLogLevelSwitch = new(LOCATOR.GetService<LoggingLevelSwitch>("file"));
        this.NotifyProperty(nameof(FileLogLevel), this.WhenAnyValue(t => t.FileLogLevelSwitch.Level));
    }
}
