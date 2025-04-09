using System.Reflection;
using Microsoft.CodeAnalysis;

namespace LazyDoctor.Analyzers.Utilities;

internal static class FullMetadataSymbolDisplayFormat
{
    // metadata names use + for nested (inner) types but SymbolDisplayFormat uses .
    // and for some weirdo reason the + behaviour isn't public (see SymbolDisplayCompilerInternalOptions.UsePlusForNestedTypes)
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

    public static SymbolDisplayFormat Instance => _fmt.Value;
}
