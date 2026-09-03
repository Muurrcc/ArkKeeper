using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Scheduling;
using ArkKeeper.Core.Servers;
using ArkKeeper.Networking.Rcon;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ArkKeeper.Orchestration.Tests;

public class LoggingTests
{
    private static readonly string CmdExe = Environment.GetEnvironmentVariable("ComSpec") ?? @"C:\Windows\System32\cmd.exe";

    [Fact]
    public async Task ManagedServer_Start_LogsInformation()
    {
        var logger = new TestLogger<ManagedServer>();
        var profile = new ServerProfile { SessionName = "Logged Server" };
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        await using var server = new ManagedServer(profile, process, notifier: null, logger: logger);

        server.Start();

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Logged Server"));

        server.Kill();
    }

    [Fact]
    public void ManagedServer_Kill_LogsWarning()
    {
        var logger = new TestLogger<ManagedServer>();
        var profile = new ServerProfile();
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        var server = new ManagedServer(profile, process, notifier: null, logger: logger);
        server.Start();

        server.Kill();

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task SchedulerRunner_RunDueTasksAsync_OnRconFailure_LogsErrorAndRethrows()
    {
        var logger = new TestLogger<SchedulerRunner>();
        var runner = new SchedulerRunner(logger);
        var createdAt = DateTimeOffset.UtcNow.AddHours(-2);
        runner.Add(new ScheduledTask("Backup", "SaveWorld", ScheduleKind.Interval, TimeSpan.FromHours(1)), createdAt);

        // Not connected -> ExecuteCommandAsync throws, which RunDueTasksAsync should log then propagate.
        await using var rcon = new RconClient();

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunDueTasksAsync(rcon, DateTimeOffset.UtcNow));

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }
}
