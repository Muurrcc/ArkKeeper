namespace ArkKeeper.Core.Players;

/// <summary>A player as reported by the server's RCON "ListPlayers" command.</summary>
public sealed record ConnectedPlayer(int Index, string Name, string SteamId);
