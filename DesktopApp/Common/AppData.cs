using System.Reflection;

namespace DesktopApp.Common;

public sealed class AppData : ReactiveObjectBase, IAppData
{
    private static readonly string _basePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        typeof(AppData).Assembly.GetCustomAttribute<AssemblyProductAttribute>()!.Product
    );

    private static string GetFullPath(string localPath)
        => Path.Join(_basePath, localPath);

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
}
