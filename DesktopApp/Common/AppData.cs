using System.Reflection;
using DesktopApp.Utilities.Helpers;
using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.Sqlite3;

namespace DesktopApp.Common;

public sealed class AppData : ReactiveObjectBase, IAppData
{
    private static readonly string _basePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        typeof(AppData).Assembly.GetCustomAttribute<AssemblyProductAttribute>()!.Product
    );
    private readonly Dictionary<string, SqliteBlobCache> _blobCaches = [];

    public AppData()
    {
        App.ShutdownRequested += (_, _) =>
        {
            var task = OnShutdown();
            if (task.Status == TaskStatus.Created)
                task.Start();
            task.Wait(TimeSpan.FromSeconds(2));
        };
    }

    private static string GetFullPath(string localPath)
        => Path.Join(_basePath, localPath);

    public IBlobCache GetBlobCache(string localPath)
        => _blobCaches.GetOrAdd(localPath, () => new SqliteBlobCache(GetFullPath(localPath)));

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

    private async Task OnShutdown()
    {
        // sync Dispose does not close connections...
        await Task.WhenAll(_blobCaches.Values.Select(c => c.DisposeAsync().AsTask()));
    }
}
