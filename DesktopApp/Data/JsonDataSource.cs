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
            string json;
            // temp for local dev (todo LocalHttpCache)
            var localPath = Path.Join(Constants.DataCacheBaseDir, _path);
            if (File.Exists(localPath))
            {
                this.Log().Debug($"Using cached {_path} @ {localPath}");
                json = await File.ReadAllTextAsync(localPath);
            }
            else
            {
                var uriString = Path.Join(Constants.GameDataBaseUrl, _path);
                this.Log().Debug($"Downloading {_path} from {uriString}");
                json = await LOCATOR.GetService<HttpClient>()!.GetStringAsync(new Uri(uriString));
            }
            this.Log().Debug($"Loaded {_path}");
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
