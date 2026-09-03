using System.Text.RegularExpressions;

namespace ArkKeeper.Core.Players;

/// <summary>
/// Parses the plain-text response of the RCON "ListPlayers" command, e.g.:
/// <code>
/// 0. Some Player, 76561198000000000
/// 1. Another One, 76561198000000001
/// </code>
/// or "No Players Connected" when the server is empty.
/// </summary>
public static partial class ListPlayersParser
{
    [GeneratedRegex(@"^(?<index>\d+)\.\s+(?<name>.+),\s*(?<steamId>\d+)$")]
    private static partial Regex LineFormat();

    public static IReadOnlyList<ConnectedPlayer> Parse(string rconResponse)
    {
        var players = new List<ConnectedPlayer>();

        foreach (var rawLine in rconResponse.Split('\n'))
        {
            var line = rawLine.Trim('\r', '\n', ' ');
            if (line.Length == 0)
            {
                continue;
            }

            var match = LineFormat().Match(line);
            if (!match.Success)
            {
                continue;
            }

            players.Add(new ConnectedPlayer(
                int.Parse(match.Groups["index"].Value),
                match.Groups["name"].Value.Trim(),
                match.Groups["steamId"].Value));
        }

        return players;
    }
}
