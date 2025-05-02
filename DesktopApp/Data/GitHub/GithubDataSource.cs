using System.Text.Json;

namespace DesktopApp.Data.GitHub;

/// <summary>
/// A <see cref="DataSource{T}" /> that loads data from a predefined GitHub repository.
/// </summary>
/// <typeparam name="T">Must be JSON serializable.</typeparam>
public sealed class GithubDataSource<T> : DataSource<T>
{
    private readonly GithubDataAdapter _adapter;
    private readonly UserPrefs _prefs;
    [Reactive]
    public string Language { get; set; }
    public string Path { get; }

    public GithubDataSource(GithubDataAdapter adapter, UserPrefs prefs, string path)
    {
        AssertDI(adapter);
        AssertDI(prefs);
        _adapter = adapter;
        _prefs = prefs;
        Language = "en_US";
        Path = path;

        prefs.Values
            .Switch(d => d.WhenAnyValue(t => t.Language))
            .Subscribe(lang => Language = lang)
            .DisposeWith(this);

        _ = Reload();
    }

    public override async Task<T> Reload()
    {
        try
        {
            await _prefs.Loaded.FirstAsync();

            var json = await _adapter.GetFileContents(Language, Path);
            if (json is null)
                throw new InvalidOperationException($"File not found: {Language}/{Path}");

            var value = (T)JsonSerializer.Deserialize(json, typeof(T), JsonSourceGenContext.Default)!;
            Subject.OnNext(value);
            return value;
        }
        catch (Exception e)
        {
            this.Log().Error(e, $"Failed to load data for {Language}/{Path}");
            Subject.OnError(e);
            throw;
        }
    }
}
