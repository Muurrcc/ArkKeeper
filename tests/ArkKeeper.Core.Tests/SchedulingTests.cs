using ArkKeeper.Core.Scheduling;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class ScheduledTaskTests
{
    [Fact]
    public void GetNextOccurrence_Interval_AddsTheInterval()
    {
        var task = new ScheduledTask("Backup", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(6));
        var now = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        var next = task.GetNextOccurrence(now);

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 16, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void GetNextOccurrence_DailyAt_BeforeTimeToday_ReturnsToday()
    {
        var task = new ScheduledTask("Restart", "DoExit", ScheduleKind.DailyAt, TimeSpan.FromHours(4));
        var now = new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero);

        var next = task.GetNextOccurrence(now);

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 4, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void GetNextOccurrence_DailyAt_AfterTimeToday_ReturnsTomorrow()
    {
        var task = new ScheduledTask("Restart", "DoExit", ScheduleKind.DailyAt, TimeSpan.FromHours(4));
        var now = new DateTimeOffset(2026, 1, 1, 5, 0, 0, TimeSpan.Zero);

        var next = task.GetNextOccurrence(now);

        Assert.Equal(new DateTimeOffset(2026, 1, 2, 4, 0, 0, TimeSpan.Zero), next);
    }
}

public class TaskScheduleTests
{
    [Fact]
    public void IsDue_BeforeNextRun_ReturnsFalse()
    {
        var task = new ScheduledTask("Backup", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(1));
        var created = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var schedule = new TaskSchedule(task, created);

        Assert.False(schedule.IsDue(created.AddMinutes(30)));
    }

    [Fact]
    public void IsDue_AtOrAfterNextRun_ReturnsTrue()
    {
        var task = new ScheduledTask("Backup", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(1));
        var created = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var schedule = new TaskSchedule(task, created);

        Assert.True(schedule.IsDue(created.AddHours(1)));
    }

    [Fact]
    public void MarkRan_AdvancesNextRunTime()
    {
        var task = new ScheduledTask("Backup", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(1));
        var created = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var schedule = new TaskSchedule(task, created);
        var ranAt = created.AddHours(1);

        schedule.MarkRan(ranAt);

        Assert.Equal(ranAt, schedule.LastRunAt);
        Assert.Equal(ranAt.AddHours(1), schedule.NextRunAt);
        Assert.False(schedule.IsDue(ranAt.AddMinutes(1)));
    }
}
