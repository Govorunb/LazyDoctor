using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LazyDoctor.Analyzers.JsonClassSerialization;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class JsonClassDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public const string SymbolMetadataNameProperty = "JsonClass.MetadataName";
    public const string SymbolReferenceableNameProperty = "JsonClass.ReferenceableName";
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeNode,
            SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);
    }

    // lookups with metadata names use + for nested (inner) types but SymbolDisplayFormat uses .
    // and for some weirdo reason the + behaviour isn't public
    private static readonly Lazy<SymbolDisplayFormat> _fmt = new(() =>
    {
        var fmt = SymbolDisplayFormat.FullyQualifiedFormat
            .RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
        var addInternalOptions = typeof(SymbolDisplayFormat)
            .GetMethod("AddCompilerInternalOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        if (addInternalOptions is { })
            fmt = (SymbolDisplayFormat)addInternalOptions.Invoke(fmt, [1 << 7]); // UsePlusForNestedTypes
        return fmt;
    });

    private static SymbolDisplayFormat FullMetadataNameFormat => _fmt.Value;

    private static void AnalyzeNode(SyntaxNodeAnalysisContext obj)
    {
        // filter to declarations with [JsonClass] attribute
        var declaration = (BaseTypeDeclarationSyntax)obj.Node;
        var symbol = obj.SemanticModel.GetDeclaredSymbol(declaration);

        var markerAttr = symbol?.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "JsonClassAttribute");
        if (markerAttr is null)
            return;

        // find matching [JsonSerializable] attribute
        var jsgcClass = obj.SemanticModel.Compilation.GetTypeByMetadataName("DesktopApp.Data.JsonSourceGenContext");
        if (jsgcClass is null)
            throw new InvalidOperationException("DesktopApp.Data.JsonSourceGenContext not found");

        var jsgcHasMatchingJsonSerializableAttribute = jsgcClass.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "JsonSerializableAttribute"
                      && (a.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol)?.Name == symbol!.Name);
        if (jsgcHasMatchingJsonSerializableAttribute)
            return;

        // all of this is just so the generated attributes look 'normal' (e.g. autoimport namespace into usings instead of spamming "global::")
        // [JsonSerializable] attributes can't be source-generated since they're markers for the System.Text.Json sourcegen
        // (and generators can't see each other's output because their order is not specified)
        var symbolName = symbol!.ToDisplayString(FullMetadataNameFormat);

        // name to use in JsonSerializable attribute (e.g. OutermostParent.Inner.Target)
        // for completeness's sake, there's theoretically some weirdness with top level statements - but we'll ignore that
        var lastPeriod = symbolName.LastIndexOf('.');
        var refName = lastPeriod == -1 ? symbolName : symbolName.Substring(lastPeriod + 1);
        refName = refName.Replace('+', '.');

        var diagnostic = Diagnostic.Create(Diagnostics.JsonSerializableMissingDescriptor, declaration.Identifier.GetLocation(),
            additionalLocations: [jsgcClass.Locations.First()],
            properties: ImmutableDictionary<string, string?>.Empty
                .Add(SymbolMetadataNameProperty, symbolName)
                .Add(SymbolReferenceableNameProperty, refName),
            symbolName
        );
        obj.ReportDiagnostic(diagnostic);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Diagnostics.JsonSerializableMissingDescriptor];
}
