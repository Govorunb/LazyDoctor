using System.IO.IsolatedStorage;
using System.Reactive.Disposables;

namespace DesktopApp.Utilities;

public sealed class AppData : ReactiveObjectBase
{
    private readonly IsolatedStorageFile _store;

    public AppData()
    {
        _store = IsolatedStorageFile.GetUserStoreForApplication()
            .DisposeWith(Disposables);
    }

    public bool FileExists(string localPath)
    {
        return _store.FileExists(localPath);
    }

    public async Task<string?> ReadFile(string localPath)
    {
        if (!FileExists(localPath))
            return null;

        await using var stream = _store.OpenFile(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task WriteFile(string localPath, string content)
    {
        if (string.IsNullOrEmpty(localPath))
            return;

        var dir = Path.GetDirectoryName(localPath)!;
        var file = Path.GetFileName(localPath);
        _store.CreateDirectory(dir);

        await using var stream = _store.OpenFile(localPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
    }
}
