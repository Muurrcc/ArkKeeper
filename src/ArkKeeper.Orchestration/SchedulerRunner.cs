using ArkKeeper.Core.Scheduling;
using ArkKeeper.Networking.Rcon;

namespace ArkKeeper.Orchestration;

/// <summary>
/// Actually executes <see cref="ScheduledTask"/>s — <see cref="TaskSchedule"/> only knew how to
/// answer "is it due yet?"; nothing polled that and ran the command until this.
/// </summary>
public sealed class SchedulerRunner
{
    private readonly List<TaskSchedule> _schedules = new();

    public IReadOnlyList<TaskSchedule> Schedules => _schedules;

    public TaskSchedule Add(ScheduledTask task, DateTimeOffset? now = null)
    {
        var schedule = new TaskSchedule(task, now ?? DateTimeOffset.UtcNow);
        _schedules.Add(schedule);
        return schedule;
    }

    public void Remove(TaskSchedule schedule) => _schedules.Remove(schedule);

    /// <summary>Runs every due task's command over RCON and advances its schedule. Returns the
    /// tasks that ran, in case a caller wants to log/notify.</summary>
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

            await rcon.ExecuteCommandAsync(schedule.Task.Command, cancellationToken);
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
            await RunDueTasksAsync(rcon, DateTimeOffset.UtcNow, cancellationToken);

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
