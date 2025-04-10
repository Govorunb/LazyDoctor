using DesktopApp.Utilities.Attributes;

namespace DesktopApp.Data.Player;

[JsonClass]
public sealed class PlayerExpData(int level = 1, int exp = 0)
{
    [Reactive] public int Level { get; set; } = level;
    [Reactive] public int Exp { get; set; } = exp;

    public void Deconstruct(out int level, out int exp)
    {
        level = Level;
        exp = Exp;
    }
}
