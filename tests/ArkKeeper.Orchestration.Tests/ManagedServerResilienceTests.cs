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
        await using var server = new ManagedServer(profile, process) { AutoRestart = true };

        server.Start();
        // The stand-in process exits almost immediately ("crashing"); give the exited-event
        // handler a moment to react and call Start() again.
        await Task.Delay(500);

        Assert.Equal(ServerStatus.Running, server.Status);

        server.Kill();
    }

    [Fact]
    public async Task AutoRestart_AfterAnExplicitKill_DoesNotRestart()
    {
        var profile = new ServerProfile();
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        await using var server = new ManagedServer(profile, process) { AutoRestart = true };
        server.Start();

        server.Kill();
        await Task.Delay(500);

        Assert.Equal(ServerStatus.Stopped, server.Status);
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
