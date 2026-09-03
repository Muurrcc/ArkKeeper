using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Servers;
using Xunit;

namespace ArkKeeper.Orchestration.Tests;

public class ManagedServerResilienceTests
{
    private static readonly string CmdExe = Environment.GetEnvironmentVariable("ComSpec") ?? @"C:\Windows\System32\cmd.exe";

    [Fact]
    public async Task SendRconCommandAsync_WhenConnectionDropsMidSession_ReconnectsAndRetriesOnce()
    {
        await using var rconServer = new FakeRconServer { CloseConnectionAfterCommands = 1 };
        var profile = new ServerProfile { RconPort = rconServer.Port, AdminPassword = "admin-pw" };
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        await using var server = new ManagedServer(profile, process);
        server.Start();

        var first = await server.SendRconCommandAsync("ListPlayers");
        // The server closed the connection after that first command; this one has to
        // reconnect (a fresh TCP connection + auth) before it can succeed.
        var second = await server.SendRconCommandAsync("ListPlayers");

        Assert.Equal("OK", first);
        Assert.Equal("OK", second);

        server.Kill();
    }

    [Fact]
    public async Task AutoRestart_WhenProcessCrashesUnexpectedly_RestartsIt()
    {
        var profile = new ServerProfile();
        using var process = new ServerProcess(CmdExe, "/c exit 1");
        await using var server = new ManagedServer(profile, process)
        {
            AutoRestart = true,
            AutoRestartDelay = TimeSpan.FromMilliseconds(50),
        };

        server.Start();
        // The stand-in process exits almost immediately ("crashing"); wait for the (short,
        // for this test) auto-restart delay to elapse and the exited-event handler to react.
        await WaitUntilAsync(() => server.Status == ServerStatus.Running);

        Assert.Equal(ServerStatus.Running, server.Status);

        server.Kill();
    }

    [Fact]
    public async Task AutoRestart_AfterAnExplicitKill_DoesNotRestart()
    {
        var profile = new ServerProfile();
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        await using var server = new ManagedServer(profile, process)
        {
            AutoRestart = true,
            AutoRestartDelay = TimeSpan.FromMilliseconds(50),
        };
        server.Start();

        server.Kill();
        // Long enough to be sure a (wrongly) pending auto-restart would have fired by now.
        await Task.Delay(500);

        Assert.Equal(ServerStatus.Stopped, server.Status);
    }

    [Fact]
    public async Task Kill_DuringPendingAutoRestartDelay_CancelsTheRestart()
    {
        // Distinct from AutoRestart_AfterAnExplicitKill_DoesNotRestart above: here the crash
        // happens first (unrequested, so a restart gets scheduled), and Kill() lands while that
        // restart is still waiting out its delay — proving the pending restart is cancelled
        // rather than relying on timing to avoid the race.
        var profile = new ServerProfile();
        using var process = new ServerProcess(CmdExe, "/c exit 1");
        await using var server = new ManagedServer(profile, process)
        {
            AutoRestart = true,
            AutoRestartDelay = TimeSpan.FromMilliseconds(300),
        };

        server.Start();
        await Task.Delay(100); // process has crashed and a restart is now pending
        server.Kill();
        await Task.Delay(500); // longer than the original 300ms delay would have needed

        Assert.Equal(ServerStatus.Stopped, server.Status);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
        {
            await Task.Delay(20);
        }
    }

    [Fact]
    public async Task AutoRestart_DefaultsToFalse()
    {
        var profile = new ServerProfile();
        using var process = new ServerProcess(CmdExe, "/c exit 0");
        await using var server = new ManagedServer(profile, process);

        Assert.False(server.AutoRestart);
    }
}
