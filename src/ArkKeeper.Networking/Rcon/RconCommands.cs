using ArkKeeper.Core.Players;

namespace ArkKeeper.Networking.Rcon;

/// <summary>Typed wrappers for the standard ARK RCON admin commands, instead of every caller
/// building raw command strings by hand. <see cref="GetPlayersAsync"/> also parses the response
/// via <see cref="ListPlayersParser"/> instead of returning raw text.</summary>
public static class RconCommands
{
    public static async Task<IReadOnlyList<ConnectedPlayer>> GetPlayersAsync(this RconClient rcon, CancellationToken cancellationToken = default)
    {
        var response = await rcon.ExecuteCommandAsync("ListPlayers", cancellationToken);
        return ListPlayersParser.Parse(response);
    }

    public static Task<string> SaveWorldAsync(this RconClient rcon, CancellationToken cancellationToken = default) =>
        rcon.ExecuteCommandAsync("SaveWorld", cancellationToken);

    public static Task<string> BroadcastAsync(this RconClient rcon, string message, CancellationToken cancellationToken = default) =>
        rcon.ExecuteCommandAsync($"ServerChat {message}", cancellationToken);

    public static Task<string> KickPlayerAsync(this RconClient rcon, string steamId, CancellationToken cancellationToken = default) =>
        rcon.ExecuteCommandAsync($"KickPlayer {steamId}", cancellationToken);

    public static Task<string> BanPlayerAsync(this RconClient rcon, string steamId, CancellationToken cancellationToken = default) =>
        rcon.ExecuteCommandAsync($"BanPlayer {steamId}", cancellationToken);

    public static Task<string> UnbanPlayerAsync(this RconClient rcon, string steamId, CancellationToken cancellationToken = default) =>
        rcon.ExecuteCommandAsync($"UnbanPlayer {steamId}", cancellationToken);
}
