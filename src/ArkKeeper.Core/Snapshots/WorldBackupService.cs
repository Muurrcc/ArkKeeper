using System.IO.Compression;

namespace ArkKeeper.Core.Snapshots;

/// <summary>Copies a server's save directory to/from timestamped backups (plain folders or,
/// optionally, zip files). This complements the RCON "SaveWorld" command (which flushes the
/// current save to disk) with ArkKeeper's own point-in-time snapshots.</summary>
public sealed class WorldBackupService
{
    public WorldBackupService(string backupRootDirectory)
    {
        BackupRootDirectory = backupRootDirectory;
    }

    public string BackupRootDirectory { get; }

    /// <summary>Copies <paramref name="saveDirectory"/> into a new timestamped backup under
    /// <see cref="BackupRootDirectory"/> — a folder by default, or a single .zip file when
    /// <paramref name="compress"/> is true — and returns its path.</summary>
    public string CreateBackup(string saveDirectory, DateTimeOffset? timestamp = null, bool compress = false)
    {
        if (!Directory.Exists(saveDirectory))
        {
            throw new DirectoryNotFoundException($"Save directory not found: {saveDirectory}");
        }

        Directory.CreateDirectory(BackupRootDirectory);
        var stamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("yyyyMMdd-HHmmss");

        if (compress)
        {
            var zipPath = Path.Combine(BackupRootDirectory, stamp + ".zip");
            ZipFile.CreateFromDirectory(saveDirectory, zipPath);
            return zipPath;
        }

        var destination = Path.Combine(BackupRootDirectory, stamp);
        CopyDirectory(saveDirectory, destination);
        return destination;
    }

    /// <summary>Lists available backups (folders and .zip files alike), newest first.</summary>
    public IReadOnlyList<string> ListBackups()
    {
        if (!Directory.Exists(BackupRootDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory.GetDirectories(BackupRootDirectory)
            .Concat(Directory.GetFiles(BackupRootDirectory, "*.zip"))
            .OrderByDescending(path => Path.GetFileNameWithoutExtension(path))
            .ToList();
    }

    /// <summary>Deletes the oldest backups beyond <paramref name="keepCount"/> most recent ones.
    /// Returns the paths that were deleted.</summary>
    public IReadOnlyList<string> PruneBackups(int keepCount)
    {
        if (keepCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(keepCount), "keepCount cannot be negative.");
        }

        var toDelete = ListBackups().Skip(keepCount).ToList();

        foreach (var path in toDelete)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        return toDelete;
    }

    /// <summary>Overwrites <paramref name="saveDirectory"/> with the contents of <paramref name="backupPath"/>
    /// — accepts either a backup folder or a .zip backup.</summary>
    public void RestoreBackup(string backupPath, string saveDirectory)
    {
        var isZip = backupPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

        if (isZip ? !File.Exists(backupPath) : !Directory.Exists(backupPath))
        {
            throw new FileNotFoundException($"Backup not found: {backupPath}", backupPath);
        }

        // Extract/copy into a staging directory next to saveDirectory (so the later Directory.Move
        // stays on the same volume) *before* touching the live save — a corrupt/truncated backup
        // (bad download, disk error, ...) throwing partway through used to leave the original
        // saveDirectory already deleted with nothing to replace it: the live world wiped for
        // nothing. Only swap the staged content in once it's fully extracted/copied.
        var stagingPath = saveDirectory + ".restoring-" + Guid.NewGuid().ToString("N");
        try
        {
            if (isZip)
            {
                Directory.CreateDirectory(stagingPath);
                ZipFile.ExtractToDirectory(backupPath, stagingPath);
            }
            else
            {
                CopyDirectory(backupPath, stagingPath);
            }
        }
        catch
        {
            if (Directory.Exists(stagingPath))
            {
                Directory.Delete(stagingPath, recursive: true);
            }

            throw;
        }

        if (Directory.Exists(saveDirectory))
        {
            Directory.Delete(saveDirectory, recursive: true);
        }

        Directory.Move(stagingPath, saveDirectory);
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
