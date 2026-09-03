using ArkKeeper.Core.Scheduling;
using ArkKeeper.Networking.Rcon;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArkKeeper.Orchestration;

/// <summary>
/// Actually executes <see cref="ScheduledTask"/>s — <see cref="TaskSchedule"/> only knew how to
/// answer "is it due yet?"; nothing polled that and ran the command until this.
/// </summary>
public sealed class SchedulerRunner
{
    private readonly List<TaskSchedule> _schedules = new();
    private readonly ILogger _logger;

    public SchedulerRunner(ILogger<SchedulerRunner>? logger = null)
    {
        _logger = logger ?? NullLogger<SchedulerRunner>.Instance;
    }

    public IReadOnlyList<TaskSchedule> Schedules => _schedules;

    public TaskSchedule Add(ScheduledTask task, DateTimeOffset? now = null)
    {
        var schedule = new TaskSchedule(task, now ?? DateTimeOffset.UtcNow);
        _schedules.Add(schedule);
        return schedule;
    }

    public void Remove(TaskSchedule schedule) => _schedules.Remove(schedule);

    /// <summary>Runs every due task's command over RCON and advances its schedule. Returns the
    /// tasks that ran, in case a caller wants to notify.</summary>
    public async Task<IReadOnlyList<ScheduledTask>> RunDueTasksAsync(
        RconClient rcon, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var ran = new List<ScheduledTask>();

        foreach (var schedule in _schedules)
        {
            if (!schedule.IsDue(now))
            {
                continue;
            }

            _logger.LogInformation("Running scheduled task {TaskName}: {Command}", schedule.Task.Name, schedule.Task.Command);

            try
            {
                await rcon.ExecuteCommandAsync(schedule.Task.Command, cancellationToken);
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

    /// <summary>Polls for due tasks every <paramref name="pollInterval"/> until cancelled.</summary>
    public async Task RunLoopAsync(RconClient rcon, TimeSpan pollInterval, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunDueTasksAsync(rcon, DateTimeOffset.UtcNow, cancellationToken);
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
}
