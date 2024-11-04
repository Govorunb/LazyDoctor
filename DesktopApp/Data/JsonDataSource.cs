using System.Text.Json;

namespace DesktopApp.Data;

public sealed class JsonDataSource<T> : DataSource<T>
{
    private JsonSerializerOptions? _jsonSerializerOptions;
    private Uri Uri { get; }
    protected override string DataPath => Uri.AbsolutePath;
    private readonly Lazy<T> _lazyValue;
    public override T Value => _lazyValue.Value;

    public JsonDataSource(Uri uri, JsonSerializerOptions? options = null)
    {
        _jsonSerializerOptions = options;
        Uri = uri;
        _lazyValue = new(Load);
    }

    public T Load()
    {
        _jsonSerializerOptions ??= JsonDataSource.DefaultOptions;
        var json = Uri.Scheme switch
        {
            // todo: cache on client (many tables are several MB each)
            "http" or "https" => LOCATOR.GetService<HttpClient>()!.GetStringAsync(Uri).Result,
            "file" => File.ReadAllText(Uri.AbsolutePath),
            _ => throw new InvalidOperationException($"Unknown URI scheme {Uri.Scheme}"),
        };
        return JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions)!;
    }
}

public static class JsonDataSource
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
