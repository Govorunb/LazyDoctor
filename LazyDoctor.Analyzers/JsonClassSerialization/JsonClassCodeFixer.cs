using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LazyDoctor.Analyzers.JsonClassSerialization;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(JsonClassCodeFixer)), Shared]
public class JsonClassCodeFixer : CodeFixProvider
{
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        context.RegisterCodeFix(
            CodeAction.Create(
                "Add [JsonSerializable] attribute on JsonSourceGenContext",
                ct => FixAll(context.Diagnostics, context.Document.Project, ct),
                nameof(JsonClassCodeFixer)),
            context.Diagnostics);
        return Task.CompletedTask;
    }

    private static async Task<Solution> FixAll(IList<Diagnostic> diagnostics, Project proj, CancellationToken ct = default)
    {
        if (diagnostics.Count == 0)
            return proj.Solution;

        var jsonContextLoc = diagnostics[0].AdditionalLocations[0];
        var jsgcDoc = proj.GetDocument(jsonContextLoc.SourceTree)!;
        var jsgcRoot = await jsgcDoc.GetSyntaxRootAsync(ct);
        var jsgcNode = jsgcRoot!.FindNode(jsonContextLoc.SourceSpan);
        var jsgcClass = jsgcNode.FirstAncestorOrSelf<ClassDeclarationSyntax>()!;

        var jsonSerializableAttr = SyntaxFactory.Attribute(SyntaxFactory.ParseName("JsonSerializable"));

        // add [JsonSerializable(typeof(...))] for each class
        var jsonSerializableAttrs = SelectProperty(diagnostics, JsonClassDiagnosticAnalyzer.SymbolTypeNameProperty)
            .Select(name => jsonSerializableAttr.AddArgumentListArguments(
                SyntaxFactory.AttributeArgument(
                    SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(name)))
                )
            );
        var newJsonContextClass = jsgcClass
            .AddAttributeLists(jsonSerializableAttrs
                .Select(a => SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(a)))
                .ToArray()
            );
        var newRoot = jsgcRoot.ReplaceNode(jsgcClass, newJsonContextClass);

        var usings = newRoot.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
        var lastUsing = usings.LastOrDefault(u => u.Name is { });
        var autoImports = SelectProperty(diagnostics, JsonClassDiagnosticAnalyzer.SymbolNamespaceProperty)
            .Distinct()
            .Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)))
            .Where(u => usings.All(u2 => u2.Name?.ToString() != u.Name!.ToString()));
        newRoot = lastUsing is null
            ? newRoot.InsertNodesBefore(newRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First(), autoImports)
            : newRoot.InsertNodesAfter(lastUsing, autoImports);

        var newSolution = proj.Solution.WithDocumentSyntaxRoot(jsgcDoc.Id, newRoot);
        return newSolution;
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [Diagnostics.JsonSerializableMissing.Id];

    public sealed override FixAllProvider GetFixAllProvider()
        => JsonClassFixAllProvider.Instance;

    public sealed class JsonClassFixAllProvider : FixAllProvider
    {
        public static JsonClassFixAllProvider Instance { get; } = new();
        private JsonClassFixAllProvider() { }

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
            => [FixAllScope.Project];

        public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            var diagnostics = await fixAllContext.GetAllDiagnosticsAsync(fixAllContext.Project);

            return CodeAction.Create(
                "Add [JsonSerializable] attributes on JsonSourceGenContext",
                ct => FixAll(diagnostics, fixAllContext.Project, ct),
                nameof(JsonClassFixAllProvider));
        }
    }

    private static IEnumerable<string> SelectProperty(IEnumerable<Diagnostic> diagnostics, string propName)
        => diagnostics.Select(d => d.Properties[propName]!);
}
