using ArkKeeper.Core.Snapshots;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class WorldBackupServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ArkKeeperBackupTests_" + Guid.NewGuid());
    private readonly string _saveDirectory;
    private readonly string _backupRoot;

    public WorldBackupServiceTests()
    {
        _saveDirectory = Path.Combine(_root, "Saved");
        _backupRoot = Path.Combine(_root, "Backups");
        Directory.CreateDirectory(_saveDirectory);
        Directory.CreateDirectory(Path.Combine(_saveDirectory, "SavedArksLocal"));
        File.WriteAllText(Path.Combine(_saveDirectory, "TheIsland.ark"), "save-data");
        File.WriteAllText(Path.Combine(_saveDirectory, "SavedArksLocal", "Profile.arkprofile"), "profile-data");
    }

    [Fact]
    public void CreateBackup_CopiesFilesAndSubdirectories()
    {
        var service = new WorldBackupService(_backupRoot);

        var backupPath = service.CreateBackup(_saveDirectory, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));

        Assert.True(Directory.Exists(backupPath));
        Assert.Equal("save-data", File.ReadAllText(Path.Combine(backupPath, "TheIsland.ark")));
        Assert.Equal("profile-data", File.ReadAllText(Path.Combine(backupPath, "SavedArksLocal", "Profile.arkprofile")));
    }

    [Fact]
    public void ListBackups_ReturnsNewestFirst()
    {
        var service = new WorldBackupService(_backupRoot);
        service.CreateBackup(_saveDirectory, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        service.CreateBackup(_saveDirectory, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));

        var backups = service.ListBackups();

        Assert.Equal(2, backups.Count);
        Assert.Contains("20260102", backups[0]);
        Assert.Contains("20260101", backups[1]);
    }

    [Fact]
    public void RestoreBackup_OverwritesSaveDirectoryWithBackupContents()
    {
        var service = new WorldBackupService(_backupRoot);
        var backupPath = service.CreateBackup(_saveDirectory, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        File.WriteAllText(Path.Combine(_saveDirectory, "TheIsland.ark"), "corrupted");

        service.RestoreBackup(backupPath, _saveDirectory);

        Assert.Equal("save-data", File.ReadAllText(Path.Combine(_saveDirectory, "TheIsland.ark")));
    }

    [Fact]
    public void CreateBackup_Compressed_ProducesAZipFile()
    {
        var service = new WorldBackupService(_backupRoot);

        var backupPath = service.CreateBackup(_saveDirectory, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), compress: true);

        Assert.True(File.Exists(backupPath));
        Assert.EndsWith(".zip", backupPath);
    }

    [Fact]
    public void RestoreBackup_FromAZip_RestoresContents()
    {
        var service = new WorldBackupService(_backupRoot);
        var backupPath = service.CreateBackup(_saveDirectory, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), compress: true);
        File.WriteAllText(Path.Combine(_saveDirectory, "TheIsland.ark"), "corrupted");

        service.RestoreBackup(backupPath, _saveDirectory);

        Assert.Equal("save-data", File.ReadAllText(Path.Combine(_saveDirectory, "TheIsland.ark")));
        Assert.Equal("profile-data", File.ReadAllText(Path.Combine(_saveDirectory, "SavedArksLocal", "Profile.arkprofile")));
    }

    [Fact]
    public void ListBackups_IncludesBothFoldersAndZips_NewestFirst()
    {
        var service = new WorldBackupService(_backupRoot);
        service.CreateBackup(_saveDirectory, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        service.CreateBackup(_saveDirectory, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), compress: true);

        var backups = service.ListBackups();

        Assert.Equal(2, backups.Count);
        Assert.Contains("20260102", backups[0]);
        Assert.Contains("20260101", backups[1]);
    }

    [Fact]
    public void PruneBackups_DeletesOldestBeyondKeepCount()
    {
        var service = new WorldBackupService(_backupRoot);
        service.CreateBackup(_saveDirectory, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        service.CreateBackup(_saveDirectory, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));
        service.CreateBackup(_saveDirectory, new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero));

        var deleted = service.PruneBackups(keepCount: 1);

        Assert.Equal(2, deleted.Count);
        var remaining = service.ListBackups();
        Assert.Single(remaining);
        Assert.Contains("20260103", remaining[0]);
    }

    [Fact]
    public void PruneBackups_WithFewerBackupsThanKeepCount_DeletesNothing()
    {
        var service = new WorldBackupService(_backupRoot);
        service.CreateBackup(_saveDirectory, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var deleted = service.PruneBackups(keepCount: 5);

        Assert.Empty(deleted);
        Assert.Single(service.ListBackups());
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
