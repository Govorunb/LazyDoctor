using System.Reflection;

[assembly: InternalsVisibleTo("DesktopApp.Test")]
[assembly: InternalsVisibleTo("Scratchpad")]
// god bless the .NET teams
[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(HotReloadHandler))]

namespace DesktopApp;

[PublicAPI]
internal static class AssemblyInfo
{
    private static Assembly Assembly { get; } = typeof(AssemblyInfo).Assembly;
    public static string? Author { get; } = Assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
    public static string? Product { get; } = Assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
    public static Version Version { get; } = Assembly.GetName().Version!;
}
