namespace DesktopApp.Utilities.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public sealed class DisplayNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
