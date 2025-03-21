using Microsoft.CodeAnalysis;

namespace LazyDoctor.Analyzers;

[Generator]
public class JsonClassSerializationContextGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // turns out the output of one generator does not in fact go into another generator
        // so System.Text.Json can only generate from manual organic [JsonSerializable] attributes
        // womp womp

//         var jsonClassSymbols = context.SyntaxProvider
//             .CreateSyntaxProvider(
//                 (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
//                 (syntaxContext, ct) => (INamedTypeSymbol)syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node, ct)!)
//             .Where(symbol => symbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == "JsonClassAttribute"));
//
//         context.RegisterSourceOutput(jsonClassSymbols, static (spc, symbol) =>
//         {
//             StringBuilder sb = new("""
//             using System.Text.Json.Serialization;
//
//             namespace DesktopApp.Data;
//
//             """);
//             sb.AppendLine();
//             sb.AppendLine($"[JsonSerializable(typeof({symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}))]");
//             sb.AppendLine("public sealed partial class JsonSourceGenContext;");
//
//             spc.AddSource($"{symbol.Name}.JsonClass_JsonSourceGenContext.g.cs", sb.ToString());
//         });
    }
}
