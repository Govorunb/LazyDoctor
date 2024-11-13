using System.Text.Json;
using JetBrains.Annotations;

namespace DesktopApp.Data;

public sealed class JsonDataSource<T> : DataSource<T>
{
    private readonly string _path;

    public JsonDataSource(string path)
    {
        _path = path;
        _ = Reload();
    }

    [PublicAPI]
    public async Task<T> Reload()
    {
        try
        {
            // do not inject the cache in ctor
            var cache = LOCATOR.GetService<LocalHttpCache>()!;
            var json = await cache.GetJson(_path);
            this.Log().Info($"Loaded {_path}");
            var value = (T)JsonSerializer.Deserialize(json, typeof(T), JsonSourceGenContext.Default)!;
            Subject.OnNext(value);
            return value;
        }
        catch (Exception e)
        {
            this.Log().Error(e, $"Failed to load {_path}");
            // an error here isn't fatal and caller can freely ignore/retry
            // this is why we don't call subject.OnError(e)
            throw;
        }
    }
}
