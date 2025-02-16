using System.Net;
using System.Reactive.Linq;
using System.Reflection;
using DesktopApp.Utilities.Helpers;
using ReactiveMarbles.CacheDatabase.Core;

namespace DesktopApp.Data.GitHub;

public sealed class GithubDataAdapter : ReactiveObjectBase
{
    private readonly GithubAkavache _cache;
    private readonly UserPrefs _prefs;
    private const string RepoCN = "ArknightsGameData";
    private const string RepoGlobal = "ArknightsGameData_YoStar";
    private const string RepoOwner = "Kengxxiao";

    private static readonly string _userAgent;

    private readonly IBlobCache _blobCache;

    static GithubDataAdapter()
    {
        var thisAssembly = typeof(GithubDataAdapter).Assembly;
        var author = thisAssembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
        var product = thisAssembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        _userAgent = $"{author}-{product}";
    }

    private readonly HttpClient _client;
    private readonly Dictionary<string, IDataSource> _dataSources = [];

    public GithubDataAdapter(GithubAkavache cache, UserPrefs prefs)
    {
        AssertDI(cache);
        AssertDI(prefs);
        _cache = cache;
        _prefs = prefs;

        _blobCache = cache.BlobCache;

        _client = new HttpClient();
        Disposables.Add(_client);
        _client.DefaultRequestHeaders.UserAgent.ParseAdd($"{_userAgent}/{App.Version}");
        _client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        // 304s are supposed to not count against your ratelimit but they actually do :)
        // unless we add an Authorization header with seemingly literally anything in it :) :)
        _client.DefaultRequestHeaders.Authorization = new("None");
    }

    public async Task<byte[]?> GetFileContents(string lang, string file)
    {
        var repo = lang == "zh_CN" ? RepoCN : RepoGlobal;
        var path = $"{lang}/gamedata/{file}";

        var url = $"https://api.github.com/repos/{RepoOwner}/{repo}/contents/{path}";
        var res = await StampedeLock<string, byte[]?>.CombineConcurrent(url, async () =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var cached = await _cache.GetAsync(url);
            if (cached?.Headers.GetValueOrDefault("ETag") is { } etag)
            {
                this.Log().Debug($"Have etag {etag} for {url}");
                request.Headers.Add("If-None-Match", etag);
            }

            var response = await _client.SendAsync(request);
            this.Log().Debug($"{response.StatusCode} for {url}");

            var (modified, json) = response.StatusCode switch
            {
                HttpStatusCode.NotModified => (false, cached!.Body),
                HttpStatusCode.OK => (true, await response.Content.ReadAsStringAsync()),
                _ => throw new InvalidOperationException($"Failed to {request.Method} {request.RequestUri}: {response.StatusCode} {response.ReasonPhrase}")
            };

            var githubFile = JsonSourceGenContext.Default.Deserialize<GithubFileStub>(json)!;
            if (githubFile.Type is not "file")
                throw new InvalidOperationException($"{path} was a {githubFile.Type} instead of a file");

            if (modified)
            {
                await _blobCache.Invalidate(githubFile.DownloadUrl!);
                var headers = response.Headers.ToDictionary(kvp => kvp.Key, kvp => string.Join(',', kvp.Value));
                await _cache.SetAsync(url, new(json, headers));
            }

            // if the file is small (under 1MB?), github just returns the contents in the first response
            if (githubFile.Encoding == "base64")
            {
                this.Log().Debug($"Using inlined contents for {file}");
                return Convert.FromBase64String(githubFile.Content!);
            }

            // otherwise, we need to do a second fetch for the actual contents
            this.Log().Debug($"Fetching raw contents for {file}");
            // this has a rate limit but it's pretty undocumented/opaque, allegedly 5k req/hr per IP
            var rawContents = await _blobCache.DownloadUrl(githubFile.DownloadUrl!, HttpMethod.Get, GithubAkavache.DefaultExpiration);

            return rawContents;
        });

        return res.Result;
    }

    public GithubDataSource<T> GetDataSource<T>(string path)
    {
        return (GithubDataSource<T>)_dataSources.GetOrAdd(path, () => new GithubDataSource<T>(this, _prefs, path));
    }

    public Task ReloadAll() => Task.WhenAll(_dataSources.Values.Select(ds => ds.Reload()));
}
