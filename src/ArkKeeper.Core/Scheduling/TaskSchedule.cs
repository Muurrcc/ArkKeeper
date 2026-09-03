namespace ArkKeeper.Core.Scheduling;

/// <summary>Tracks one <see cref="ScheduledTask"/>'s run history and answers "is it due yet?" —
/// pure state, no timers or threads, so the actual polling loop can live wherever the host
/// (the desktop app, later a headless service) wants it.</summary>
public sealed class TaskSchedule
{
    private DateTimeOffset? _lastRunAt;

    public TaskSchedule(ScheduledTask task, DateTimeOffset createdAt)
    {
        Task = task;
        NextRunAt = task.GetNextOccurrence(createdAt);
    }

    public ScheduledTask Task { get; }

    public DateTimeOffset NextRunAt { get; private set; }

    public DateTimeOffset? LastRunAt => _lastRunAt;

    public bool IsDue(DateTimeOffset now) => now >= NextRunAt;

    /// <summary>Marks the task as having run at <paramref name="ranAt"/> and advances the schedule.</summary>
    public void MarkRan(DateTimeOffset ranAt)
    {
        _lastRunAt = ranAt;
        NextRunAt = Task.GetNextOccurrence(ranAt);
    }
}
