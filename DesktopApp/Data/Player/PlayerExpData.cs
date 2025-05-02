namespace DesktopApp.Data.Player;

[JsonClass]
public class PlayerExpData(int level = 1, int exp = 0)
{
    [Reactive] public int Level { get; set; } = level;
    [Reactive] public int Exp { get; set; } = exp;

    public PlayerExpData(PlayerExpData other) : this(other.Level, other.Exp) { }

    public void Deconstruct(out int level, out int exp)
    {
        level = Level;
        exp = Exp;
    }
}

// xaml needs an actually parameterless ctor for some reason
internal sealed class DesignPlayerExpData : PlayerExpData;
