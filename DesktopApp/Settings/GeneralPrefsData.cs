using DesktopApp.Data;

namespace DesktopApp.Settings;

[JsonClass]
public sealed class GeneralPrefsData : ReactiveObjectBase
{
    [Reactive]
    public bool ManualRefreshAcknowledged { get; set; }

    // TODO: profiles so you can quickly switch between servers
    #region Server
    [Reactive]
    public TimeOnly ServerReset { get; set; } = Constants.EnServerReset;
    [Reactive]
    public TimeSpan ServerTimezone { get; set; } = Constants.EnServerTimezone;

    #endregion Server

    #region Logging
    [Reactive]
    public ReactiveLogLevel FileLogLevel { get; internal set; } = new();
    [Reactive]
    public ReactiveLogLevel ConsoleLogLevel { get; internal set; } = new(SeriLogLevel.Debug);
    #endregion Logging

    public GeneralPrefsData()
    {
        this.NotifyProperty(nameof(FileLogLevel), FileLogLevel.Changed);
        this.NotifyProperty(nameof(ConsoleLogLevel), ConsoleLogLevel.Changed);
    }
}
