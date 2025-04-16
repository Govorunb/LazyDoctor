using DesktopApp.Utilities.Attributes;
using DesktopApp.Utilities.Helpers;

namespace DesktopApp.Settings;

[JsonClass]
public sealed class GeneralPrefsData : ReactiveObjectBase
{
    [Reactive]
    public bool ManualRefreshAcknowledged { get; set; }

    [Reactive]
    public ReactiveLogLevel FileLogLevel { get; internal set; } = new();

    [Reactive]
    public ReactiveLogLevel ConsoleLogLevel { get; internal set; } = new(SeriLogLevel.Debug);

    public GeneralPrefsData()
    {
        this.NotifyProperty(nameof(FileLogLevel), FileLogLevel.Changed);
        this.NotifyProperty(nameof(ConsoleLogLevel), ConsoleLogLevel.Changed);
    }
}
