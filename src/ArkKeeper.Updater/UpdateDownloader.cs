namespace ArkKeeper.Updater;

/// <summary>Downloads the installer/package an <see cref="UpdateCheckResult"/> points to.
/// Actually applying it (running the installer, restarting the app) is left to the caller —
/// safely replacing a running .exe on Windows needs a separate small updater process, which is
/// its own follow-up rather than something to improvise here.</summary>
public sealed class UpdateDownloader
{
    private readonly HttpClient _httpClient;

    public UpdateDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>Downloads the update into <paramref name="destinationDirectory"/> and returns the
    /// full path to the downloaded file.</summary>
    public async Task<string> DownloadAsync(UpdateCheckResult update, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destinationDirectory);

        var fileName = GetFileName(update);
        var destinationPath = Path.Combine(destinationDirectory, fileName);

        await using var responseStream = await _httpClient.GetStreamAsync(update.DownloadUrl, cancellationToken);
        await using var fileStream = File.Create(destinationPath);
        await responseStream.CopyToAsync(fileStream, cancellationToken);

        return destinationPath;
    }

    private static string GetFileName(UpdateCheckResult update)
    {
        var fromUrl = Uri.TryCreate(update.DownloadUrl, UriKind.Absolute, out var uri)
            ? Path.GetFileName(uri.LocalPath)
            : null;

        return string.IsNullOrEmpty(fromUrl) ? $"ArkKeeper-{update.LatestVersion}.exe" : fromUrl;
    }
}
