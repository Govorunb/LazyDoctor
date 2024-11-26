using System.Reflection;
using DesktopApp.Utilities.Helpers;
using Octokit;

namespace DesktopApp.Data.GitHub;

public sealed class GithubDataAdapter : ReactiveObjectBase
{
    private readonly GithubAkavache _cache;
    private readonly UserPrefs _prefs;
    private const string RepoCN = "ArknightsGameData";
    private const string RepoGlobal = "ArknightsGameData_YoStar";
    private const string RepoOwner = "Kengxxiao";

    private static readonly string _userAgent;

    static GithubDataAdapter()
    {
        var thisAssembly = typeof(GithubDataAdapter).Assembly;
        var author = thisAssembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
        var product = thisAssembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        _userAgent = $"{author}-{product}";
    }

    private readonly GitHubClient _client;
    private readonly Dictionary<string, IDataSource> _dataSources = [];

    public GithubDataAdapter(GithubAkavache cache, UserPrefs prefs)
    {
        AssertDI(cache);
        AssertDI(prefs);
        _cache = cache;
        _prefs = prefs;

        _client = new(new ProductHeaderValue(_userAgent)) { ResponseCache = cache };
    }

    public async Task<byte[]?> GetFileContents(string lang, string file)
    {
        var repo = lang == "zh_CN" ? RepoCN : RepoGlobal;
        var path = $"{lang}/gamedata/{file}";

        var uriString = $"https://api.github.com/repos/{RepoOwner}/{repo}/contents/{path}";
        var res = await StampedeLock<string, byte[]?>.CombineConcurrent(uriString, async () =>
        {
            var results = await _client.Repository.Content.GetAllContents(RepoOwner, repo, path);
            var githubFile = results.Single();
            if (githubFile.Type.Value != ContentType.File)
                throw new InvalidOperationException($"{file} is a {githubFile.Type.Value}, not a file");

            // if the file is small (under 1MB?), github just returns the contents in the first response
            if (githubFile.Encoding == "base64")
            {
                this.Log().Debug($"Using inlined contents for {file}");
                return Convert.FromBase64String(githubFile.EncodedContent);
            }

            // otherwise, we need to do a second fetch for the actual contents
            this.Log().Debug($"Fetching raw contents for {file}");
            var rawContents = await _client.Repository.Content.GetRawContent(RepoOwner, repo, path);

            // if the previous call freshly cached the result, it means the http stream was consumed
            if (rawContents.Length == 0)
            {
                // realistically, i can either do this garbage (and cry every time i have to look at this code)
                // or use IBlobCache.DownloadUrl (and cry at being unable to invalidate properly)
                // you can clearly see which was chosen
                rawContents = await _client.Repository.Content.GetRawContent(RepoOwner, repo, path);
                if (rawContents.Length > 0)
                    this.Log().Warn($"Fresh cache on raw contents for {file}");
                // otherwise, the file really is empty
            }

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
