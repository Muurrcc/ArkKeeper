namespace ArkKeeper.Updater;

/// <summary>The JSON document ArkKeeper polls to check for a newer release.</summary>
public sealed record UpdateManifest(string LatestVersion, string DownloadUrl);

public sealed record UpdateCheckResult(bool IsUpdateAvailable, Version LatestVersion, string DownloadUrl);
