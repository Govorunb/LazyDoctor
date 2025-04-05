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
                ct => AddJsonSerializableAttributeAsync(context, ct),
                nameof(JsonClassCodeFixer)),
            context.Diagnostics);
        return Task.CompletedTask;
    }

    private static async Task<Solution> AddJsonSerializableAttributeAsync(CodeFixContext ctx, CancellationToken ct = default)
    {
        var doc = ctx.Document;
        var diagnostic = ctx.Diagnostics[0];

        var clsName = diagnostic.Properties["JsonClass"]!; // full metadata name
        var jsonContextLoc = diagnostic.AdditionalLocations[0];
        var jsgcDoc = doc.Project.GetDocument(jsonContextLoc.SourceTree)!;

        return await AddMany([clsName], jsonContextLoc, jsgcDoc, ct);
    }

    private static async Task<Solution> AddMany(
        IEnumerable<string> jsonClassNames,
        Location jsonContextLoc,
        Document jsgcDoc,
        CancellationToken ct = default)
    {
        var jsgcRoot = await jsgcDoc.GetSyntaxRootAsync(ct);
        var jsgcNode = jsgcRoot!.FindNode(jsonContextLoc.SourceSpan);
        var jsgcClass = jsgcNode.FirstAncestorOrSelf<ClassDeclarationSyntax>()!;

        var compilation = (await jsgcDoc.Project.GetCompilationAsync(ct))!;
        var jsonSerializableAttr = SyntaxFactory.Attribute(SyntaxFactory.ParseName("JsonSerializable"));
        var attrCount = jsgcClass.AttributeLists
            .SelectMany(a => a.Attributes)
            .Count(a => a.Name.ToString() == "JsonSerializable");
        ImmutableArray<string> classNames = [..jsonClassNames];

        var jsonClassSymbols = classNames
            .Select(name => name.StartsWith("global::", StringComparison.Ordinal)
                ? name.Substring(8)
                : name)
            .Select(compilation.GetTypesByMetadataName)
            .Select(symbols => symbols.Single())
            .ToArray();
        // add [JsonSerializable(typeof(...))] for each class
        var jsonSerializableAttrs = jsonClassSymbols
            .Select(s => jsonSerializableAttr.AddArgumentListArguments(
                SyntaxFactory.AttributeArgument(
                    SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(s.Name)))
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
        var autoImports = jsonClassSymbols
            .Select(s => s.ContainingNamespace.ToDisplayString())
            .Distinct()
            .Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)))
            .Where(u => usings.All(u2 => u2.Name?.ToString() != u.Name?.ToString()));
        newRoot = lastUsing is null
            ? newRoot.InsertNodesBefore(newRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First(), autoImports)
            : newRoot.InsertNodesAfter(lastUsing, autoImports);

        // check for fix-alls with multiple declarations (ideally tests should cover this and not runtime checks)
        var newAttrCount = newJsonContextClass.AttributeLists
            .SelectMany(a => a.Attributes)
            .Count(a => a.Name.ToString() == "JsonSerializable");
        if (newAttrCount != attrCount + classNames.Length)
            throw new InvalidOperationException($"{attrCount} + {classNames.Length} should have been {attrCount + classNames.Length}, was {newAttrCount}");

        var newSolution = jsgcDoc.Project.Solution.WithDocumentSyntaxRoot(jsgcDoc.Id, newRoot);
        return newSolution;
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [Diagnostics.JsonSerializableMissingDescriptor.Id];

    public sealed override FixAllProvider GetFixAllProvider()
        => JsonClassFixAllProvider.Instance;

    public sealed class JsonClassFixAllProvider : FixAllProvider
    {
        public static JsonClassFixAllProvider Instance { get; } = new();
        private JsonClassFixAllProvider() { }

        public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            var diagnostics = await fixAllContext.GetAllDiagnosticsAsync(fixAllContext.Project);
            var jsonContextLoc = diagnostics.First().AdditionalLocations[0];
            var names = diagnostics.Select(d => d.Properties["JsonClass"]!).Distinct();
            var jsgcDoc = fixAllContext.Project.GetDocument(jsonContextLoc.SourceTree)!;

            return CodeAction.Create(
                "Add [JsonSerializable] attributes on JsonSourceGenContext",
                ct => AddMany(names, jsonContextLoc, jsgcDoc, ct),
                nameof(JsonClassFixAllProvider));
        }
    }
}
