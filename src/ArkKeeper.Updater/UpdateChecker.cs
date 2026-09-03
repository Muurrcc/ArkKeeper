using System.Text.Json;

namespace ArkKeeper.Updater;

/// <summary>Checks a JSON manifest URL for a newer ArkKeeper release than the one currently running.</summary>
public sealed class UpdateChecker
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly string _manifestUrl;

    public UpdateChecker(HttpClient httpClient, string manifestUrl)
    {
        _httpClient = httpClient;
        _manifestUrl = manifestUrl;
    }

    public async Task<UpdateCheckResult> CheckAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        var json = await _httpClient.GetStringAsync(_manifestUrl, cancellationToken);
        var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Update manifest was empty or malformed.");

        var latestVersion = Version.Parse(manifest.LatestVersion);
        return new UpdateCheckResult(latestVersion > currentVersion, latestVersion, manifest.DownloadUrl);
    }
}
