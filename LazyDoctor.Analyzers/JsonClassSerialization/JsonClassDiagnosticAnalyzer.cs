using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LazyDoctor.Analyzers.JsonClassSerialization;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class JsonClassDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
    }

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

        var jsonSerializableAttribute = jsgcClass.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "JsonSerializableAttribute")
            .FirstOrDefault(a => (a.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol)?.Name == symbol!.Name);
        if (jsonSerializableAttribute is null)
        {
            var diagnostic = Diagnostic.Create(Diagnostics.JsonSerializableMissingDescriptor, declaration.Identifier.GetLocation(),
                additionalLocations: [jsgcClass.Locations.First()],
                properties: ImmutableDictionary<string, string?>.Empty.Add("JsonClass", symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                symbol.Name
            );
            obj.ReportDiagnostic(diagnostic);
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Diagnostics.JsonSerializableMissingDescriptor];
}
