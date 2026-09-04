using System.Globalization;

namespace ArkKeeper.App.ViewModels;

/// <summary>Presentation wrapper around one backup path from <c>WorldBackupService.ListBackups()</c>
/// — the service itself deals only in raw paths (folder or .zip), so this is where the
/// filename's <c>yyyyMMdd-HHmmss</c> timestamp gets parsed into something readable and the size
/// gets computed, once, at listing time.</summary>
public sealed class BackupRowViewModel
{
    public BackupRowViewModel(string path)
    {
        Path = path;
        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        Timestamp = DateTime.TryParseExact(name, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToString("yyyy-MM-dd HH:mm:ss")
            : name;
        IsCompressed = path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        SizeDisplay = FormatSize(ComputeSize(path));
    }

    public string Path { get; }

    public string Timestamp { get; }

    public bool IsCompressed { get; }

    public string SizeDisplay { get; }

    private static long ComputeSize(string path)
    {
        if (File.Exists(path))
        {
            return new FileInfo(path).Length;
        }

        if (Directory.Exists(path))
        {
            return new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        }

        return 0;
    }

    private static string FormatSize(long bytes)
    {
        var mb = bytes / 1024.0 / 1024.0;
        return mb >= 1 ? $"{mb:F1} MB" : $"{bytes / 1024.0:F0} KB";
    }
}
