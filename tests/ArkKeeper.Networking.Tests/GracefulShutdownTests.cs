using System.Diagnostics;
using ArkKeeper.Core.Servers;
using ArkKeeper.Networking.Rcon;
using ArkKeeper.Networking.Servers;
using Xunit;

namespace ArkKeeper.Networking.Tests;

public class GracefulShutdownTests
{
    private static readonly string CmdExe = Environment.GetEnvironmentVariable("ComSpec") ?? @"C:\Windows\System32\cmd.exe";

    [Fact]
    public async Task StopAsync_SendsSaveWorldThenDoExit_OverRcon()
    {
        await using var server = new FakeRconServer();
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", server.Port, "password");

        // The stand-in process doesn't actually respond to RCON like a real ARK server would,
        // so it won't self-exit — this also exercises the Kill() fallback below.
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        process.Start();

        await GracefulShutdown.StopAsync(process, rcon, TimeSpan.FromSeconds(2));

        Assert.Equal(new[] { "SaveWorld", "DoExit" }, server.ReceivedCommands);
        Assert.Equal(ServerStatus.Stopped, process.Status);
    }

    [Fact]
    public async Task StopAsync_OnAlreadyStoppedProcess_DoesNothing()
    {
        await using var server = new FakeRconServer();
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", server.Port, "password");

        using var process = new ServerProcess(CmdExe, "/c exit 0");

        await GracefulShutdown.StopAsync(process, rcon, TimeSpan.FromSeconds(5));

        Assert.Empty(server.ReceivedCommands);
    }

    [Fact]
    public async Task StopAsync_WhenProcessExitsOnItsOwn_ReturnsWithoutWaitingForTheFullTimeout()
    {
        await using var server = new FakeRconServer();
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", server.Port, "password");

        // Exits on its own shortly after start, simulating a real server that honored DoExit.
        using var process = new ServerProcess(CmdExe, "/c ping -n 2 127.0.0.1 >nul");
        process.Start();

        var sw = Stopwatch.StartNew();
        await GracefulShutdown.StopAsync(process, rcon, TimeSpan.FromSeconds(30));
        sw.Stop();

        Assert.Equal(ServerStatus.Stopped, process.Status);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(15), $"Expected to return well before the 30s timeout, took {sw.Elapsed}");
    }
}
