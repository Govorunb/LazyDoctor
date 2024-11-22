using ReactiveMarbles.CacheDatabase.Core;

namespace DesktopApp.Common;

public interface IAppData
{
    bool FileExists(string localPath);
    Task<string?> ReadFile(string localPath);
    Task WriteFile(string localPath, string content);
    IBlobCache GetBlobCache(string localPath);
}
