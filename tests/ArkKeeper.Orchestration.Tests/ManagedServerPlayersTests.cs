using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Servers;
using Xunit;

namespace ArkKeeper.Orchestration.Tests;

public class ManagedServerPlayersTests
{
    private static readonly string CmdExe = Environment.GetEnvironmentVariable("ComSpec") ?? @"C:\Windows\System32\cmd.exe";

    [Fact]
    public async Task GetPlayersAsync_ParsesListPlayersResponse()
    {
        await using var rconServer = new FakeRconServer
        {
            ResponseProvider = cmd => cmd == "ListPlayers"
                ? "0. Some Player, 76561198000000000\n1. Another One, 76561198000000001\n"
                : "OK",
        };
        var profile = new ServerProfile { RconPort = rconServer.Port, AdminPassword = "admin-pw" };
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        await using var server = new ManagedServer(profile, process);
        server.Start();

        var players = await server.GetPlayersAsync();

        Assert.Equal(2, players.Count);
        Assert.Equal("Some Player", players[0].Name);
        Assert.Equal("76561198000000000", players[0].SteamId);

        server.Kill();
    }

    [Fact]
    public async Task KickBanUnban_SendTheCorrectCommandsWithSteamId()
    {
        await using var rconServer = new FakeRconServer();
        var profile = new ServerProfile { RconPort = rconServer.Port, AdminPassword = "admin-pw" };
        using var process = new ServerProcess(CmdExe, "/c ping -n 30 127.0.0.1 >nul");
        await using var server = new ManagedServer(profile, process);
        server.Start();

        await server.KickPlayerAsync("76561198000000000");
        await server.BanPlayerAsync("76561198000000000");
        await server.UnbanPlayerAsync("76561198000000000");

        Assert.Equal(
            new[]
            {
                "KickPlayer 76561198000000000",
                "BanPlayer 76561198000000000",
                "UnbanPlayer 76561198000000000",
            },
            rconServer.ReceivedCommands);

        server.Kill();
    }
}
