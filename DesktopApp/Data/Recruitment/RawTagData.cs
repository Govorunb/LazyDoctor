using DesktopApp.Utilities.Attributes;

namespace DesktopApp.Data.Recruitment;

/// <summary>
/// Recruitment tag data as structured in the gamedata.<br/>
/// For a more useful format, see <see cref="Tag"/>.
/// </summary>
[JsonClass]
public sealed class RawTagData
{
    public int TagId { get; set; }
    public string? TagName { get; set; }
    // unused? we ignore this
    public int TagGroup { get; set; }
}
