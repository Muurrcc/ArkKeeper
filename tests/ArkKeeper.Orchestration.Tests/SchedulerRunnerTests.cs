using ArkKeeper.Core.Scheduling;
using ArkKeeper.Networking.Rcon;
using Xunit;

namespace ArkKeeper.Orchestration.Tests;

public class SchedulerRunnerTests
{
    [Fact]
    public async Task RunDueTasksAsync_RunsOnlyDueTasks()
    {
        await using var rconServer = new FakeRconServer();
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", rconServer.Port, "password");

        var runner = new SchedulerRunner();
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        runner.Add(new ScheduledTask("Backup", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(1)), createdAt);
        runner.Add(new ScheduledTask("Restart", "DoExit", ScheduleKind.Interval, TimeSpan.FromHours(6)), createdAt);

        var ran = await runner.RunDueTasksAsync(rcon, createdAt.AddHours(1));

        Assert.Single(ran);
        Assert.Equal("Backup", ran[0].Name);
        Assert.Equal(new[] { "SaveWorld" }, rconServer.ReceivedCommands);
    }

    [Fact]
    public async Task RunDueTasksAsync_CalledAgainBeforeNextOccurrence_DoesNotRerun()
    {
        await using var rconServer = new FakeRconServer();
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", rconServer.Port, "password");

        var runner = new SchedulerRunner();
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        runner.Add(new ScheduledTask("Backup", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(1)), createdAt);

        await runner.RunDueTasksAsync(rcon, createdAt.AddHours(1));
        var secondRun = await runner.RunDueTasksAsync(rcon, createdAt.AddHours(1).AddMinutes(1));

        Assert.Empty(secondRun);
        Assert.Single(rconServer.ReceivedCommands);
    }

    [Fact]
    public async Task RunDueTasksAsync_AfterMarkRan_BecomesDueAgainAfterTheNextInterval()
    {
        await using var rconServer = new FakeRconServer();
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", rconServer.Port, "password");

        var runner = new SchedulerRunner();
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        runner.Add(new ScheduledTask("Backup", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(1)), createdAt);

        await runner.RunDueTasksAsync(rcon, createdAt.AddHours(1));
        var secondRun = await runner.RunDueTasksAsync(rcon, createdAt.AddHours(2));

        Assert.Single(secondRun);
        Assert.Equal(new[] { "SaveWorld", "SaveWorld" }, rconServer.ReceivedCommands);
    }

    [Fact]
    public void Remove_StopsTrackingTheSchedule()
    {
        var runner = new SchedulerRunner();
        var schedule = runner.Add(new ScheduledTask("Backup", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(1)));

        runner.Remove(schedule);

        Assert.Empty(runner.Schedules);
    }
}
