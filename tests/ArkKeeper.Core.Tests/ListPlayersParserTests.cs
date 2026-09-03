using ArkKeeper.Core.Players;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class ListPlayersParserTests
{
    [Fact]
    public void Parse_ReadsMultiplePlayers()
    {
        const string response = "0. Some Player, 76561198000000000\n1. Another One, 76561198000000001\n";

        var players = ListPlayersParser.Parse(response);

        Assert.Equal(2, players.Count);
        Assert.Equal(new ConnectedPlayer(0, "Some Player", "76561198000000000"), players[0]);
        Assert.Equal(new ConnectedPlayer(1, "Another One", "76561198000000001"), players[1]);
    }

    [Fact]
    public void Parse_OnEmptyServer_ReturnsEmptyList()
    {
        var players = ListPlayersParser.Parse("No Players Connected");

        Assert.Empty(players);
    }

    [Fact]
    public void Parse_IgnoresBlankLines()
    {
        const string response = "0. Solo Player, 76561198000000000\n\n\n";

        var players = ListPlayersParser.Parse(response);

        Assert.Single(players);
    }
}
