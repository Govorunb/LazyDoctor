using System.Text.Json;

namespace DesktopApp.Data;

public sealed class JsonDataSource<T> : DataSource<T>
{
    private readonly JsonSerializerOptions? _jsonSerializerOptions;
    private Uri Uri { get; }

    public JsonDataSource(Uri uri, JsonSerializerOptions? options = null)
    {
        _jsonSerializerOptions = options ?? Constants.DefaultJsonSerializerOptions;
        Uri = uri;
        _ = Reload();
    }

    public async Task<T> Reload()
    {
        try
        {
            var json = await (Uri.Scheme switch
            {
                // todo: cache on client (many tables are several MB each)
                // "http" or "https" => LOCATOR.GetService<LocalHttpCache>().GetString(Uri),
                "http" or "https" => LOCATOR.GetService<HttpClient>()!.GetStringAsync(Uri),
                "file" => File.ReadAllTextAsync(Uri.AbsolutePath),
                _ => throw new InvalidOperationException($"Unsupported URI scheme {Uri.Scheme}"),
            });
            this.Log().Debug($"Loaded {Uri}");
            var value = JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions)!;
            Subject.OnNext(value);
            return value;
        }
        catch (Exception e)
        {
            this.Log().Error(e, $"Failed to load {Uri}");
            // an error here isn't fatal and caller can freely ignore/retry
            // this is why we don't call subject.OnError(e)
            throw;
        }
    }
}
