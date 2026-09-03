namespace ArkKeeper.Core.Saves;

/// <summary>Data read from a server's .arkprofile save file.</summary>
public sealed record PlayerInfo(
    ulong PlayerDataId,
    string SteamId,
    string SteamName,
    string CharacterName,
    int? TribeId,
    short Level,
    string FilePath,
    DateTime FileCreatedUtc,
    DateTime FileUpdatedUtc);
