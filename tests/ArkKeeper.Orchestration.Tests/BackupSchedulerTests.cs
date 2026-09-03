using ArkKeeper.Core.Scheduling;
using ArkKeeper.Core.Snapshots;
using ArkKeeper.Networking.Rcon;
using Xunit;

namespace ArkKeeper.Orchestration.Tests;

public class BackupSchedulerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ArkKeeperBackupSchedulerTests_" + Guid.NewGuid());
    private readonly string _saveDirectory;
    private readonly string _backupRoot;

    public BackupSchedulerTests()
    {
        _saveDirectory = Path.Combine(_root, "Saved");
        _backupRoot = Path.Combine(_root, "Backups");
        Directory.CreateDirectory(_saveDirectory);
        File.WriteAllText(Path.Combine(_saveDirectory, "TheIsland.ark"), "save-data");
    }

    [Fact]
    public async Task RunIfDueAsync_WhenDue_SavesWorldOverRconThenCreatesABackup()
    {
        await using var server = new FakeRconServer();
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", server.Port, "password");

        var backupService = new WorldBackupService(_backupRoot);
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var scheduler = new BackupScheduler(backupService, ScheduleKind.Interval, TimeSpan.FromHours(6), now: createdAt);

        var backupPath = await scheduler.RunIfDueAsync(rcon, _saveDirectory, createdAt.AddHours(6));

        Assert.NotNull(backupPath);
        Assert.True(Directory.Exists(backupPath));
        Assert.Equal(new[] { "SaveWorld" }, server.ReceivedCommands);
    }

    [Fact]
    public async Task RunIfDueAsync_WhenNotDue_ReturnsNullAndCreatesNothing()
    {
        await using var server = new FakeRconServer();
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", server.Port, "password");

        var backupService = new WorldBackupService(_backupRoot);
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var scheduler = new BackupScheduler(backupService, ScheduleKind.Interval, TimeSpan.FromHours(6), now: createdAt);

        var backupPath = await scheduler.RunIfDueAsync(rcon, _saveDirectory, createdAt.AddHours(1));

        Assert.Null(backupPath);
        Assert.Empty(server.ReceivedCommands);
        Assert.Empty(backupService.ListBackups());
    }

    [Fact]
    public async Task RunIfDueAsync_WithKeepCount_PrunesOldBackups()
    {
        await using var server = new FakeRconServer();
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", server.Port, "password");

        var backupService = new WorldBackupService(_backupRoot);
        backupService.CreateBackup(_saveDirectory, new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        backupService.CreateBackup(_saveDirectory, new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero));

        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var scheduler = new BackupScheduler(backupService, ScheduleKind.Interval, TimeSpan.FromHours(1), keepCount: 1, now: createdAt);

        await scheduler.RunIfDueAsync(rcon, _saveDirectory, createdAt.AddHours(1));

        Assert.Single(backupService.ListBackups());
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
