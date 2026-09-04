using ArkKeeper.Core.Scheduling;

namespace ArkKeeper.App.ViewModels;

/// <summary>Presentation wrapper around one <see cref="TaskSchedule"/> — formats its kind/value
/// and run history into display strings once, at listing time.</summary>
public sealed class ScheduleRowViewModel
{
    public ScheduleRowViewModel(TaskSchedule schedule)
    {
        Schedule = schedule;

        var kindText = schedule.Task.Kind == ScheduleKind.Interval
            ? $"every {FormatInterval(schedule.Task.Value)}"
            : $"daily at {schedule.Task.Value:hh\\:mm}";
        Description = $"{schedule.Task.Command} — {kindText}";

        NextRunDisplay = schedule.NextRunAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        LastRunDisplay = schedule.LastRunAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "Never";
    }

    public TaskSchedule Schedule { get; }

    public string Name => Schedule.Task.Name;

    public string Description { get; }

    public string NextRunDisplay { get; }

    public string LastRunDisplay { get; }

    private static string FormatInterval(TimeSpan value) =>
        value.TotalHours >= 1 ? $"{value.TotalHours:0.##}h" : $"{value.TotalMinutes:0.##}m";
}
