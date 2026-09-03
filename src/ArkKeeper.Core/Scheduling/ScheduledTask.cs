namespace ArkKeeper.Core.Scheduling;

public enum ScheduleKind
{
    /// <summary>Runs repeatedly every <see cref="ScheduledTask.Value"/>.</summary>
    Interval,

    /// <summary>Runs once a day at the time of day given by <see cref="ScheduledTask.Value"/>.</summary>
    DailyAt,
}

/// <summary>A recurring RCON command (e.g. "SaveWorld", "DoExit", a broadcast) — ArkKeeper's own
/// in-process scheduler, replacing the original tool's dependency on the Windows Task Scheduler
/// so scheduling isn't a Windows-only feature.</summary>
public sealed record ScheduledTask(string Name, string Command, ScheduleKind Kind, TimeSpan Value)
{
    /// <summary>Computes when this task should next run, strictly after <paramref name="after"/>.</summary>
    public DateTimeOffset GetNextOccurrence(DateTimeOffset after) => Kind switch
    {
        ScheduleKind.Interval => after + Value,
        ScheduleKind.DailyAt => NextDailyAt(after, Value),
        _ => throw new NotSupportedException($"Unknown schedule kind: {Kind}"),
    };

    private static DateTimeOffset NextDailyAt(DateTimeOffset after, TimeSpan timeOfDay)
    {
        var candidate = new DateTimeOffset(
            after.Year, after.Month, after.Day,
            timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds,
            after.Offset);

        return candidate > after ? candidate : candidate.AddDays(1);
    }
}
