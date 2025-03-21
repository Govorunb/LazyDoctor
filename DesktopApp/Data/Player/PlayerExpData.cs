using DesktopApp.Utilities.Attributes;

namespace DesktopApp.Data.Player;

[JsonClass]
public sealed record PlayerExpData(
    [property: Reactive] int Level = 1,
    [property: Reactive] int Exp = 0
);
