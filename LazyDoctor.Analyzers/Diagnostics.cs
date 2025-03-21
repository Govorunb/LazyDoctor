using Microsoft.CodeAnalysis;

namespace LazyDoctor.Analyzers;

public static class Diagnostics
{
    private const string JsonClassId = "LD001";
    private const string JsonClassMessage = "Classes marked with [JsonClass] must have a matching [JsonSerializable] on JsonSourceGenContext";
    private const string JsonClassDescription =
        "This class is marked with a [JsonClass] attribute, but is missing a matching [JsonSerializable] attribute declaration. Without this declaration, no serialization code will be generated for this class.";

    public static DiagnosticDescriptor JsonSerializableMissingDescriptor { get; } = new(
        id: JsonClassId,
        title: "[JsonClass] without a matching [JsonSerializable] on JsonSourceGenContext will not generate serialization code",
        messageFormat: JsonClassMessage,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: JsonClassDescription
    );
}
