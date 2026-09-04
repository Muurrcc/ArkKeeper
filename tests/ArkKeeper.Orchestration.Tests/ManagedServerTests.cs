using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Servers;
using ArkKeeper.Discord;
using Xunit;

namespace ArkKeeper.Orchestration.Tests;

public class ManagedServerTests
{
    private static readonly string CmdExe = Environment.GetEnvironmentVariable("ComSpec") ?? @"C:\Windows\System32\cmd.exe";

    [Fact]
    public async Task Start_LaunchesProcessAndNotifiesDiscord()
    {
        var handler = new FakeHttpMessageHandler();
        var notifier = new DiscordWebhookNotifier(new HttpClient(handler), "https://discord.com/api/webhooks/1/a");
        var profile = new ServerProfile { SessionName = "Test Server" };
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        await using var server = new ManagedServer(profile, process, notifier);

        server.Start();
        await WaitUntilAsync(() => handler.RequestBodies.Count > 0);

        Assert.Equal(ServerStatus.Running, server.Status);
        Assert.Contains(handler.RequestBodies, b => b.Contains("Server started"));

        server.Kill();
    }

    [Fact]
    public async Task ProcessExitingOnItsOwn_NotifiesDiscordOfStop()
    {
        var handler = new FakeHttpMessageHandler();
        var notifier = new DiscordWebhookNotifier(new HttpClient(handler), "https://discord.com/api/webhooks/1/a");
        var profile = new ServerProfile { SessionName = "Test Server" };
        using var process = new ServerProcess(CmdExe, "/c exit 0");
        await using var server = new ManagedServer(profile, process, notifier);

        server.Start();
        await WaitUntilAsync(() => handler.RequestBodies.Any(b => b.Contains("Server stopped")));

        Assert.Contains(handler.RequestBodies, b => b.Contains("Server stopped"));
    }

    [Fact]
    public async Task StopAsync_SendsSaveWorldAndDoExitOverRcon_ThenProcessIsStopped()
    {
        await using var rconServer = new FakeRconServer();
        var profile = new ServerProfile { RconPort = rconServer.Port, AdminPassword = "admin-pw" };
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        await using var server = new ManagedServer(profile, process);
        server.Start();

        await server.StopAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(new[] { "SaveWorld", "DoExit" }, rconServer.ReceivedCommands);
        Assert.Equal(ServerStatus.Stopped, server.Status);
    }

    [Fact]
    public async Task StopAsync_WhenRconIsUnreachable_FallsBackToKillingTheProcess()
    {
        // No FakeRconServer listening on this port — StopAsync used to let the connection
        // failure bubble out of the method entirely and leave the process running (reported as
        // "Stop doesn't work"); it must fall back to Kill() instead, same as when RCON connects
        // but the graceful shutdown commands themselves fail.
        var profile = new ServerProfile { RconPort = 39123, AdminPassword = "admin-pw" };
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        await using var server = new ManagedServer(profile, process);
        server.Start();

        await server.StopAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(ServerStatus.Stopped, server.Status);
    }

    [Fact]
    public async Task SendRconCommandAsync_SendsCommandAndReturnsResponse()
    {
        await using var rconServer = new FakeRconServer();
        var profile = new ServerProfile { RconPort = rconServer.Port, AdminPassword = "admin-pw" };
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        await using var server = new ManagedServer(profile, process);
        server.Start();

        var response = await server.SendRconCommandAsync("ListPlayers");

        Assert.Equal("OK", response);
        Assert.Contains("ListPlayers", rconServer.ReceivedCommands);

        server.Kill();
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
        {
            await Task.Delay(20);
        }
    }
}
