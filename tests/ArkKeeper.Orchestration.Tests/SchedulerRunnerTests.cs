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

    [Fact]
    public async Task RunDueTasksAsync_WithConcurrentAddDuringIteration_DoesNotThrow()
    {
        // Regression test: _schedules used to be a plain, unsynchronized List<T>. Adding to it
        // while RunDueTasksAsync was mid-foreach (e.g. a UI thread editing tasks while the
        // background scheduler loop is running) threw "Collection was modified".
        await using var rconServer = new FakeRconServer { ResponseDelay = TimeSpan.FromMilliseconds(200) };
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", rconServer.Port, "password");

        var runner = new SchedulerRunner();
        var now = DateTimeOffset.UtcNow;
        // Several due tasks, so the foreach is still in progress (awaiting a delayed RCON
        // response) when the concurrent Add below fires.
        for (var i = 0; i < 5; i++)
        {
            runner.Add(new ScheduledTask($"Task{i}", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(1)), now.AddHours(-2));
        }

        var runTask = runner.RunDueTasksAsync(rcon, now);
        await Task.Delay(50);
        runner.Add(new ScheduledTask("AddedDuringIteration", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(1)), now.AddHours(-2));

        var ran = await runTask;

        Assert.Equal(5, ran.Count);
        Assert.Equal(6, runner.Schedules.Count);
    }
}
