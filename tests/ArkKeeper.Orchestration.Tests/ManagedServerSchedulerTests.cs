using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Scheduling;
using ArkKeeper.Core.Servers;
using Xunit;

namespace ArkKeeper.Orchestration.Tests;

public class ManagedServerSchedulerTests
{
    private static readonly string CmdExe = Environment.GetEnvironmentVariable("ComSpec") ?? @"C:\Windows\System32\cmd.exe";

    [Fact]
    public async Task RunDueScheduledTasksAsync_RunsDueTasksOverItsOwnManagedRconConnection()
    {
        await using var rconServer = new FakeRconServer();
        var profile = new ServerProfile { RconPort = rconServer.Port, AdminPassword = "admin-pw" };
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        await using var server = new ManagedServer(profile, process);
        server.Start();

        var scheduler = new SchedulerRunner();
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        scheduler.Add(new ScheduledTask("Backup", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(1)), createdAt);
        scheduler.Add(new ScheduledTask("Restart", "DoExit", ScheduleKind.Interval, TimeSpan.FromHours(6)), createdAt);

        var ran = await server.RunDueScheduledTasksAsync(scheduler, createdAt.AddHours(1));

        Assert.Single(ran);
        Assert.Equal("Backup", ran[0].Name);
        Assert.Equal(new[] { "SaveWorld" }, rconServer.ReceivedCommands);

        server.Kill();
    }
}
