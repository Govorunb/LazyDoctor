using System.Text.Json;
using System.Text.Json.Serialization;
using DesktopApp.Data;
using ReactiveMarbles.CacheDatabase.Core;

namespace DesktopApp.Utilities;

public sealed class JsonContextSerializer(JsonSerializerContext context) : ISerializer
{
    public T? Deserialize<T>(byte[] bytes)
        => (T?)JsonSerializer.Deserialize(bytes, typeof(T), context);

    public byte[] Serialize<T>(T item)
    {
        var typeInfo = context.GetTypeInfo(typeof(T));
        if (typeInfo is null)
            throw new InvalidOperationException($"Don't know how to serialize {typeof(T).Name} - add it to {nameof(JsonSourceGenContext)}");

        return JsonSerializer.SerializeToUtf8Bytes(item, typeInfo);
    }
}
