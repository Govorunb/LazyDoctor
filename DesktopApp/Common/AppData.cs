using System.Reflection;
using DesktopApp.Utilities.Helpers;
using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.Sqlite3;

namespace DesktopApp.Common;

public sealed class AppData : ReactiveObjectBase, IAppData
{
    private static readonly string _basePath = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        typeof(AppData).Assembly.GetCustomAttribute<AssemblyProductAttribute>()!.Product
    );
    private readonly Dictionary<string, SqliteBlobCache> _blobCaches = [];

    public AppData()
    {
        App.ShutdownRequested += async (_, _) => await OnShutdown().ConfigureAwait(false); // waiting is futile
        // ensure we have a folder in AppData/Local before SQLite tries to write there
        Directory.CreateDirectory(_basePath);
    }

    private static string GetFullPath(string localPath)
        => Path.Join(_basePath, localPath);

    public IBlobCache GetBlobCache(string localPath)
        => _blobCaches.GetOrAdd(localPath, () => new SqliteBlobCache(GetFullPath($"{localPath}.sqlite3")));

    public bool FileExists(string localPath)
        => File.Exists(GetFullPath(localPath));

    public async Task<string?> ReadFile(string localPath)
    {
        if (!FileExists(localPath))
            return null;

        await using var stream = File.Open(GetFullPath(localPath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task WriteFile(string localPath, string content)
    {
        if (string.IsNullOrEmpty(localPath))
            return;

        var fullPath = GetFullPath(localPath);
        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);

        await using var stream = File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
    }

    private Task OnShutdown()
    {
        // sync Dispose does not close connections...
        return Task.WhenAll(_blobCaches.Values.Select(c => c.DisposeAsync().AsTask()));
    }
}
