namespace ArkKeeper.Core.Snapshots;

/// <summary>Copies a server's save directory to/from timestamped backup folders.
/// This complements the RCON "SaveWorld" command (which flushes the current save to
/// disk) with ArkKeeper's own point-in-time snapshots.</summary>
public sealed class WorldBackupService
{
    public WorldBackupService(string backupRootDirectory)
    {
        BackupRootDirectory = backupRootDirectory;
    }

    public string BackupRootDirectory { get; }

    /// <summary>Copies <paramref name="saveDirectory"/> into a new timestamped folder under
    /// <see cref="BackupRootDirectory"/> and returns its path.</summary>
    public string CreateBackup(string saveDirectory, DateTimeOffset? timestamp = null)
    {
        if (!Directory.Exists(saveDirectory))
        {
            throw new DirectoryNotFoundException($"Save directory not found: {saveDirectory}");
        }

        var stamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("yyyyMMdd-HHmmss");
        var destination = Path.Combine(BackupRootDirectory, stamp);

        CopyDirectory(saveDirectory, destination);

        return destination;
    }

    /// <summary>Lists available backups, newest first.</summary>
    public IReadOnlyList<string> ListBackups()
    {
        if (!Directory.Exists(BackupRootDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory.GetDirectories(BackupRootDirectory)
            .OrderByDescending(path => path)
            .ToList();
    }

    /// <summary>Overwrites <paramref name="saveDirectory"/> with the contents of <paramref name="backupDirectory"/>.</summary>
    public void RestoreBackup(string backupDirectory, string saveDirectory)
    {
        if (!Directory.Exists(backupDirectory))
        {
            throw new DirectoryNotFoundException($"Backup not found: {backupDirectory}");
        }

        if (Directory.Exists(saveDirectory))
        {
            Directory.Delete(saveDirectory, recursive: true);
        }

        CopyDirectory(backupDirectory, saveDirectory);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
}
