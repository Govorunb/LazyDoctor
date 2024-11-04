using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace DesktopApp.Common.Operators;

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
public sealed partial class Operator : ReactiveObjectBase
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Rarity { get; set; }
    public string? Position { get; set; }
    [JsonPropertyName("profession")]
    public string? Class { get; set; }
    public List<string>? TagList { get; set; }

    public override string ToString() => Name ?? "unnamed";


    [JsonSerializable(typeof(Operator))]
    public partial class OperatorJsonContext : JsonSerializerContext;
}
