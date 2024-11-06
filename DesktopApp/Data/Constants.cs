using System.Text.Json;

namespace DesktopApp.Data;

internal static class Constants
{
    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
