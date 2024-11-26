namespace DesktopApp.Data.GitHub;

public sealed class GithubDataFile(string lang, string path) : ViewModelBase
{
    public string Language { get; set; } = lang;
    public string Path { get; set; } = path;
    public DateTime LastUpdated { get; set; }
    public DateTime LastChecked { get; set; }
}
