using ArkKeeper.Core.Scheduling;
using ArkKeeper.Networking.Rcon;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArkKeeper.Orchestration;

/// <summary>
/// Actually executes <see cref="ScheduledTask"/>s — <see cref="TaskSchedule"/> only knew how to
/// answer "is it due yet?"; nothing polled that and ran the command until this.
///
/// Thread-safe: <see cref="RunLoopAsync"/> is meant to run continuously in the background while
/// a caller (e.g. a UI) calls <see cref="Add"/>/<see cref="Remove"/> concurrently as the user
/// edits their scheduled tasks — an earlier version used a plain, unsynchronized List here,
/// which would throw "Collection was modified" if a mutation landed mid-iteration.
/// </summary>
public sealed class SchedulerRunner
{
    private readonly List<TaskSchedule> _schedules = new();
    private readonly object _lock = new();
    private readonly ILogger _logger;

    public SchedulerRunner(ILogger<SchedulerRunner>? logger = null)
    {
        _logger = logger ?? NullLogger<SchedulerRunner>.Instance;
    }

    public IReadOnlyList<TaskSchedule> Schedules
    {
        get { lock (_lock) { return _schedules.ToArray(); } }
    }

    /// <summary>The underlying tasks, e.g. to persist via <see cref="SchedulerStore"/>.</summary>
    public IReadOnlyList<ScheduledTask> Tasks
    {
        get { lock (_lock) { return _schedules.Select(s => s.Task).ToList(); } }
    }

    public TaskSchedule Add(ScheduledTask task, DateTimeOffset? now = null)
    {
        var schedule = new TaskSchedule(task, now ?? DateTimeOffset.UtcNow);
        lock (_lock)
        {
            _schedules.Add(schedule);
        }
        return schedule;
    }

    /// <summary>Adds every task from a previous <see cref="SchedulerStore.LoadAsync"/> call.</summary>
    public void AddRange(IEnumerable<ScheduledTask> tasks, DateTimeOffset? now = null)
    {
        foreach (var task in tasks)
        {
            Add(task, now);
        }
    }

    public void Remove(TaskSchedule schedule)
    {
        lock (_lock)
        {
            _schedules.Remove(schedule);
        }
    }

    /// <summary>Runs every due task's command and advances its schedule, sending each command
    /// through <paramref name="sendCommand"/> — a plain delegate rather than a concrete
    /// <see cref="RconClient"/>, so a caller that already owns its own connect/lock/retry
    /// discipline around RCON (like <c>ManagedServer.SendRconCommandAsync</c>, which keeps its
    /// <see cref="RconClient"/> private on purpose) can plug straight in instead of the runner
    /// needing a second, independent RCON connection. Returns the tasks that ran, in case a
    /// caller wants to notify.</summary>
    public async Task<IReadOnlyList<ScheduledTask>> RunDueTasksAsync(
        Func<string, CancellationToken, Task<string>> sendCommand, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        TaskSchedule[] snapshot;
        lock (_lock)
        {
            snapshot = _schedules.ToArray();
        }

        var ran = new List<ScheduledTask>();

        foreach (var schedule in snapshot)
        {
            if (!schedule.IsDue(now))
            {
                continue;
            }

            _logger.LogInformation("Running scheduled task {TaskName}: {Command}", schedule.Task.Name, schedule.Task.Command);

            try
            {
                await sendCommand(schedule.Task.Command, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled task {TaskName} failed", schedule.Task.Name);
                throw;
            }

            schedule.MarkRan(now);
            ran.Add(schedule.Task);
        }

        return ran;
    }

    /// <summary>Convenience overload for callers that just have a raw <see cref="RconClient"/>.</summary>
    public Task<IReadOnlyList<ScheduledTask>> RunDueTasksAsync(
        RconClient rcon, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        RunDueTasksAsync(rcon.ExecuteCommandAsync, now, cancellationToken);

    /// <summary>Polls for due tasks every <paramref name="pollInterval"/> until cancelled.</summary>
    public async Task RunLoopAsync(Func<string, CancellationToken, Task<string>> sendCommand, TimeSpan pollInterval, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunDueTasksAsync(sendCommand, DateTimeOffset.UtcNow, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Scheduler poll iteration failed, will retry after the next interval");
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

    /// <summary>Convenience overload for callers that just have a raw <see cref="RconClient"/>.</summary>
    public Task RunLoopAsync(RconClient rcon, TimeSpan pollInterval, CancellationToken cancellationToken) =>
        RunLoopAsync(rcon.ExecuteCommandAsync, pollInterval, cancellationToken);
}
