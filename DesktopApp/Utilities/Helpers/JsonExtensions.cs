using System.Text.Json;

namespace DesktopApp.Utilities.Helpers;

public static class JsonExtensions
{
    public static T? Deserialize<T>(this JsonSerializerContext context, string json)
        => (T?)JsonSerializer.Deserialize(json, typeof(T), context);
}
