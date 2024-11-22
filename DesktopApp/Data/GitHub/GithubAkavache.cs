using System.ComponentModel;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using DesktopApp.Utilities.Attributes;
using Octokit;
using Octokit.Caching;
using Octokit.Internal;
using ReactiveMarbles.CacheDatabase.Core;

namespace DesktopApp.Data.GitHub;

public sealed class GithubAkavache : ReactiveObjectBase, IResponseCache
{
    private readonly TimeProvider _timeProvider;
    private readonly IBlobCache _blobCache;

    #region bad workaround for terrible code
    // OctoKit.ApiInfo's constructor parameters having different types from the properties breaks System.Text.Json
    // (e.g. IList<string> acceptedOauthScopes in ctor vs IReadOnlyList AcceptedOauthScopes { get; set; })
    // because of this, we can't deserialize them without writing this shitty wrapper
    #nullable disable // L + json + i don't care
    [JsonClass, Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class CachedResponseWrapper
    {
        public ApiInfoWrapper ApiInfo { get; set; }
        public string Body { get; set; }
        public string ContentType { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        [JsonClass, Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class ApiInfoWrapper
        {
            public List<string> AcceptedOauthScopes { get; set; }
            public string Etag { get; set; }
            public Dictionary<string, Uri> Links { get; set; }
            public List<string> OauthScopes { get; set; }
            public RateLimit RateLimit { get; set; }
            public TimeSpan ServerTimeDifference { get; set; }

            public ApiInfo ToApiInfo()
            {
                return new(Links, OauthScopes, AcceptedOauthScopes, Etag, RateLimit, ServerTimeDifference);
            }

            public static ApiInfoWrapper FromApiInfo(ApiInfo apiInfo)
            {
                return new()
                {
                    AcceptedOauthScopes = apiInfo.AcceptedOauthScopes.ToList(),
                    Etag = apiInfo.Etag,
                    Links = apiInfo.Links.ToDictionary(),
                    OauthScopes = apiInfo.OauthScopes.ToList(),
                    RateLimit = apiInfo.RateLimit,
                    ServerTimeDifference = apiInfo.ServerTimeDifference
                };
            }
        }

        public CachedResponse.V1 ToCachedResponseV1()
        {
            object body = ContentType.Contains("raw")
                ? Encoding.UTF8.GetBytes(Body) // "raw" calls expect a byte[] (or Stream)
                : Body;
            return new(body, Headers, ApiInfo.ToApiInfo(), StatusCode, ContentType);
        }

        public static CachedResponseWrapper FromCachedResponseV1(CachedResponse.V1 cachedResponse)
        {
            // brother how the hell am i supposed to cache your response body
            // it's an http stream we can't both read it
            // bonus: if you think you're smart and want to copy it to a MemoryStream and replace the previous body
            // you've been outsmarted
            // because the actual IResponse that octokit reads the body from is completely inaccessible to us from here
            // the only way to unscrew this is a Harmony patch and i have sworn off patching non-game code
            var fullBody = cachedResponse.Body switch
            {
                // consumes the (unseekable) http stream - octokit will read an empty byte array
                Stream stream => new StreamReader(stream).ReadToEnd(),
                string s => s,
                null => "",
                _ => throw new InvalidOperationException($"Unknown body type {cachedResponse.Body.GetType()}"),
            };

            return new()
            {
                ApiInfo = ApiInfoWrapper.FromApiInfo(cachedResponse.ApiInfo),
                Body = fullBody,
                Headers = cachedResponse.Headers.ToDictionary(),
                StatusCode = cachedResponse.StatusCode,
                ContentType = cachedResponse.ContentType,
            };
        }
    }
    #nullable restore
    #endregion bad workaround for terrible code

    public GithubAkavache(IAppData appData, TimeProvider timeProvider)
    {
        AssertDI(appData);
        AssertDI(timeProvider);

        _timeProvider = timeProvider;
        _blobCache = appData.GetBlobCache("gamedata_cache.sqlite3");
        _blobCache.Vacuum().Subscribe();
    }

    async Task<CachedResponse.V1?> IResponseCache.GetAsync(IRequest request)
    {
        var key = GetKey(request);
        var resp = await _blobCache.GetObject<CachedResponseWrapper>(key)
            .Catch((KeyNotFoundException _) => Observable.Return<CachedResponseWrapper?>(null));

        this.Log().Info($"Cache {(resp is { } ? "hit" : "miss")} for {request.Method} {request.Endpoint} {request.ContentType}");
        return resp?.ToCachedResponseV1();
    }

    async Task IResponseCache.SetAsync(IRequest request, CachedResponse.V1 cachedResponse)
    {
        var expiration = TimeSpan.FromDays(14) + TimeSpan.FromDays(2 * Random.Shared.NextDouble()); // stagger refreshes
        this.Log().Info($"Caching {request.Method} {request.Endpoint} (expiring at {_timeProvider.GetLocalNow().Add(expiration)})");

        var wrapper = CachedResponseWrapper.FromCachedResponseV1(cachedResponse);

        await _blobCache.InsertObject(GetKey(request), wrapper, expiration);
    }

    private static string GetKey(IRequest request)
    {
        StringBuilder sb = new($"{request.Method} {request.Endpoint}");
        if (request.Parameters.Count > 0)
        {
            sb.Append('?');
            sb.AppendJoin('&', request.Parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }
        sb.AppendLine();
        if (request.Headers.Count > 0)
        {
            sb.AppendJoin('\n', request.Headers.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
