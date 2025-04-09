using Microsoft.CodeAnalysis;

namespace LazyDoctor.Analyzers;

public static class Diagnostics
{
    private const string JsonClassId = "LD001";
    private const string JsonClassTitle = "[JsonClass] without a matching [JsonSerializable] on JsonSourceGenContext will not generate serialization code";
    private const string JsonClassMessage = "[JsonClass] cannot (de)serialize without matching [JsonSerializable]";
    private const string JsonClassDescription =
        "This type is marked with a [JsonClass] attribute, but is missing a matching [JsonSerializable] attribute declaration. Without this declaration, no serialization code will be generated for this type.";

    public static DiagnosticDescriptor JsonSerializableMissing { get; } = new(
        id: JsonClassId,
        title: JsonClassTitle,
        messageFormat: JsonClassMessage,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: JsonClassDescription
    );

    private const string JsonClassGenericId = "LD002";
    private const string JsonClassGenericTitle = "Generic [JsonClass] without a matching [JsonSerializable] on JsonSourceGenContext will not generate serialization code";
    private const string JsonClassGenericMessage = "Generic [JsonClass] cannot (de)serialize without [JsonSerializable]";
    private const string JsonClassGenericDescription =
        """
        This type is marked with a [JsonClass] attribute, but has no matching [JsonSerializable] attribute declarations. Without this declaration, no serialization code will be generated for this type.
        No code fix can be provided for this case.
        Please manually add [JsonSerializable] attributes for all generic arguments you expect to (de)serialize.
        """;

    public static DiagnosticDescriptor JsonSerializableMissingGeneric { get; } = new(
        id: JsonClassGenericId,
        title: JsonClassGenericTitle,
        messageFormat: JsonClassGenericMessage,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: JsonClassGenericDescription
    );

    // TODO error on file-local (currently codefixer will just produce invalid code)
}
