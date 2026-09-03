namespace ArkKeeper.Networking.Workshop;

/// <summary>Steam Workshop metadata for one mod, as returned by
/// ISteamRemoteStorage/GetPublishedFileDetails.</summary>
public sealed record WorkshopModDetails(
    string PublishedFileId,
    bool Found,
    string? Title,
    long? FileSizeBytes,
    DateTimeOffset? TimeUpdatedUtc,
    string? PreviewUrl,
    bool IsBanned);
