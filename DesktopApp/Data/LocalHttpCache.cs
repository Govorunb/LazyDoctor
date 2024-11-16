using System.Collections.Concurrent;

namespace DesktopApp.Data;

public sealed class LocalHttpCache : ReactiveObjectBase
{
    private readonly IAppData _appData;
    private readonly HttpClient _client;

    private static readonly ConcurrentDictionary<string, Task<string>> _openRequests = [];

    public LocalHttpCache(IAppData appData, HttpClient client)
    {
        AssertDI(appData);
        AssertDI(client);
        _appData = appData;
        _client = client;
    }

    private static string GetLocalPath(string file)
        => Path.Join("data_cache", file);

    public async Task<string> GetJson(string file)
    {
        var localPath = GetLocalPath(file);
        if (_appData.FileExists(localPath))
        {
            this.Log().Info($"Using cached {file} @ {localPath}");
            return (await _appData.ReadFile(localPath))!;
        }

        var uriString = Path.Join(Constants.GameDataBaseUrl, file);
        var mainRequest = false;
        if (_openRequests.TryGetValue(uriString, out var task))
        {
            this.Log().Info($"Waiting for {file} from {uriString}");
        }
        else
        {
            this.Log().Info($"Downloading {file} from {uriString}");
            mainRequest = true;
            task = _client.GetStringAsync(new Uri(uriString));
            _openRequests.TryAdd(uriString, task);
        }

        var json = await task;
        _openRequests.TryRemove(uriString, out _);

        if (mainRequest)
        {
            this.Log().Info($"Caching {file} to {localPath}");
            await _appData.WriteFile(localPath, json);
        }

        return json;
    }
}
