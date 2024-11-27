using System.Reactive.Linq;
using DesktopApp.Utilities.Attributes;
using DesktopApp.Utilities.Helpers;
using ReactiveMarbles.CacheDatabase.Core;

namespace DesktopApp.Data.GitHub;

public sealed class GithubAkavache : ReactiveObjectBase
{
    private readonly TimeProvider _timeProvider;
    public IBlobCache BlobCache { get; }

    public GithubAkavache(IAppData appData, TimeProvider timeProvider)
    {
        AssertDI(appData);
        AssertDI(timeProvider);

        _timeProvider = timeProvider;

        BlobCache = appData.GetBlobCache("gamedata_cache.sqlite3");
        BlobCache.Vacuum().Subscribe();
    }

    [JsonClass]
    public sealed record HttpResponse(string Body, Dictionary<string, string> Headers);

    public async Task<HttpResponse?> GetAsync(string url)
    {
        var res = await StampedeLock<string, HttpResponse?>.CombineConcurrent(url, async () =>
        {
            var resp = await BlobCache.GetObject<HttpResponse>(url)
                .Catch((KeyNotFoundException _) => Observable.Return<HttpResponse?>(null));

            this.Log().Info($"Cache {(resp is { } ? "hit" : "miss")} for {url}");
            return resp;
        });

        return res.Result;
    }

    /// <summary>
    /// The default expiration is 2 weeks + anywhere from 0 to 2 days (chosen randomly to stagger refreshes).
    /// </summary>
    public static TimeSpan DefaultExpiration => TimeSpan.FromDays(14) + TimeSpan.FromDays(2 * Random.Shared.NextDouble());

    public async Task SetAsync(string url, HttpResponse cachedResponse)
    {
        var expiration = DefaultExpiration;
        this.Log().Info($"Caching {url} (expiring at {_timeProvider.GetLocalNow().Add(expiration)})");

        await StampedeLock<string, HttpResponse>.OverrideMainRequest(url, async () =>
        {
            await BlobCache.InsertObject(url, cachedResponse, expiration);
            return cachedResponse;
        });
    }
}
