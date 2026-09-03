using ArkKeeper.Core.Scheduling;
using ArkKeeper.Core.Snapshots;
using ArkKeeper.Networking.Rcon;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArkKeeper.Orchestration;

/// <summary>
/// Runs a <see cref="WorldBackupService"/> backup on a schedule: SaveWorld over RCON first (so
/// the backup reflects a fresh flush, not just whatever ARK's own autosave last wrote), then
/// copies/zips the save directory. Nothing connected WorldBackupService and the scheduler before
/// this — each worked on its own but a scheduled task couldn't actually trigger a backup.
/// </summary>
public sealed class BackupScheduler
{
    private readonly WorldBackupService _backupService;
    private readonly TaskSchedule _schedule;
    private readonly bool _compress;
    private readonly int? _keepCount;
    private readonly ILogger _logger;

    public BackupScheduler(
        WorldBackupService backupService,
        ScheduleKind kind,
        TimeSpan interval,
        bool compress = false,
        int? keepCount = null,
        DateTimeOffset? now = null,
        ILogger<BackupScheduler>? logger = null)
    {
        _backupService = backupService;
        _schedule = new TaskSchedule(new ScheduledTask("World Backup", "SaveWorld", kind, interval), now ?? DateTimeOffset.UtcNow);
        _compress = compress;
        _keepCount = keepCount;
        _logger = logger ?? NullLogger<BackupScheduler>.Instance;
    }

    public TaskSchedule Schedule => _schedule;

    /// <summary>If due, saves the world over RCON and creates a backup, pruning old ones if a
    /// keep count was configured. Returns the new backup's path, or null if it wasn't due yet.</summary>
    public async Task<string?> RunIfDueAsync(RconClient rcon, string saveDirectory, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (!_schedule.IsDue(now))
        {
            return null;
        }

        _logger.LogInformation("Running scheduled backup of {SaveDirectory}", saveDirectory);
        await rcon.SaveWorldAsync(cancellationToken);

        var backupPath = _backupService.CreateBackup(saveDirectory, now, _compress);
        _schedule.MarkRan(now);
        _logger.LogInformation("Created scheduled backup at {BackupPath}", backupPath);

        if (_keepCount is { } keepCount)
        {
            var pruned = _backupService.PruneBackups(keepCount);
            if (pruned.Count > 0)
            {
                _logger.LogInformation("Pruned {Count} old backup(s), keeping the {KeepCount} most recent", pruned.Count, keepCount);
            }
        }

        return backupPath;
    }

    /// <summary>Polls for the backup schedule every <paramref name="pollInterval"/> until cancelled
    /// — mirrors <see cref="SchedulerRunner.RunLoopAsync"/> for callers that want the same pattern.</summary>
    public async Task RunLoopAsync(RconClient rcon, string saveDirectory, TimeSpan pollInterval, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunIfDueAsync(rcon, saveDirectory, DateTimeOffset.UtcNow, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Scheduled backup attempt failed, will retry after the next interval");
            }

            try
            {
                await Task.Delay(pollInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
