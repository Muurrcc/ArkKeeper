using ArkKeeper.Networking.Rcon;
using Xunit;

namespace ArkKeeper.Networking.Tests;

public class RconCommandsTests
{
    [Fact]
    public async Task GetPlayersAsync_SendsListPlayersAndParsesTheResponse()
    {
        await using var server = new FakeRconServer
        {
            ResponseProvider = cmd => cmd == "ListPlayers"
                ? "0. Some Player, 76561198000000000\n1. Another One, 76561198000000001\n"
                : "OK",
        };
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", server.Port, "password");

        var players = await rcon.GetPlayersAsync();

        Assert.Equal(2, players.Count);
        Assert.Equal("Some Player", players[0].Name);
    }

    [Fact]
    public async Task SaveWorldAsync_SendsSaveWorldCommand()
    {
        await using var server = new FakeRconServer();
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", server.Port, "password");

        await rcon.SaveWorldAsync();

        Assert.Equal(new[] { "SaveWorld" }, server.ReceivedCommands);
    }

    [Fact]
    public async Task BroadcastAsync_SendsServerChatWithMessage()
    {
        await using var server = new FakeRconServer();
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", server.Port, "password");

        await rcon.BroadcastAsync("Restart in 5 minutes");

        Assert.Equal(new[] { "ServerChat Restart in 5 minutes" }, server.ReceivedCommands);
    }

    [Fact]
    public async Task KickBanUnban_SendTheCorrectCommandsWithSteamId()
    {
        await using var server = new FakeRconServer();
        await using var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", server.Port, "password");

        await rcon.KickPlayerAsync("76561198000000000");
        await rcon.BanPlayerAsync("76561198000000000");
        await rcon.UnbanPlayerAsync("76561198000000000");

        Assert.Equal(
            new[]
            {
                "KickPlayer 76561198000000000",
                "BanPlayer 76561198000000000",
                "UnbanPlayer 76561198000000000",
            },
            server.ReceivedCommands);
    }
}
