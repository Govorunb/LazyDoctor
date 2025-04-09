using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LazyDoctor.Analyzers.JsonClassSerialization;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class JsonClassDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public const string SymbolNamespaceProperty = "Namespace";
    public const string SymbolTypeNameProperty = "Name";
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeNode,
            SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);
    }

    private static SymbolDisplayFormat AccessibleTypeNameDisplayFormat { get; }
        = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes
        );

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

        var jsgcLoc = jsgcClass.Locations.First();
        var loc = markerAttr.ApplicationSyntaxReference!.GetSyntax().GetLocation();
        Diagnostic diagnostic;
        if (symbol!.IsGenericType)
        {
            diagnostic = Diagnostic.Create(Diagnostics.JsonSerializableMissingGeneric, loc, additionalLocations: [jsgcLoc]);
        }
        else
        {
            var namespaceName = symbol.ContainingNamespace.ToDisplayString();
            var typeName = symbol.ToDisplayString(AccessibleTypeNameDisplayFormat);
            diagnostic = Diagnostic.Create(Diagnostics.JsonSerializableMissing, loc,
                additionalLocations: [jsgcLoc],
                properties: ImmutableDictionary<string, string?>.Empty
                    .AddRange([
                        new(SymbolNamespaceProperty, namespaceName),
                        new(SymbolTypeNameProperty, typeName),
                    ])
            );
        }
        obj.ReportDiagnostic(diagnostic);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Diagnostics.JsonSerializableMissing, Diagnostics.JsonSerializableMissingGeneric];
}
