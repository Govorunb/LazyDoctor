using DesktopApp.Utilities.Attributes;

namespace DesktopApp.Settings;

[JsonClass]
public sealed class GeneralPrefsData : ReactiveObjectBase
{
    [Reactive]
    public bool ManualRefreshAcknowledged { get; set; }
}
