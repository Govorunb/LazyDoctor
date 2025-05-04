namespace DesktopApp.Data.Player;

[JsonClass]
public class PlayerExpData(int level = 1, int exp = 0) : IEquatable<PlayerExpData>
{
    [Reactive] public int Level { get; set; } = level;
    [Reactive] public int Exp { get; set; } = exp;

    public PlayerExpData(PlayerExpData other) : this(other.Level, other.Exp) { }

    public void Deconstruct(out int level, out int exp)
    {
        level = Level;
        exp = Exp;
    }

    public bool Equals(PlayerExpData? other)
    {
        if (other is null)
            return false;
        return ReferenceEquals(this, other)
               || Level == other.Level && Exp == other.Exp;
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj)
           || obj is PlayerExpData other && Equals(other);

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(Level, Exp);

    public static bool operator ==(PlayerExpData? left, PlayerExpData? right) => Equals(left, right);
    public static bool operator !=(PlayerExpData? left, PlayerExpData? right) => !Equals(left, right);
}

// xaml needs an actually parameterless ctor for some reason
internal sealed class DesignPlayerExpData : PlayerExpData;
