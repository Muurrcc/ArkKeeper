namespace ArkKeeper.Core.Saves;

/// <summary>Data read from a server's .arktribe save file.</summary>
public sealed record TribeInfo(
    int Id,
    string Name,
    uint? OwnerId,
    string FilePath,
    DateTime FileCreatedUtc,
    DateTime FileUpdatedUtc);
