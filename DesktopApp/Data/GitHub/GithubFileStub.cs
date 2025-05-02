namespace DesktopApp.Data.GitHub;

[JsonClass]
public sealed class GithubFileStub : ReactiveObjectBase
{
    public string? Encoding { get; set; }
    public string? Type { get; set; }
    public string? Content { get; set; }
    public string? Sha { get; set; }
    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }
}
