using System.IO.Compression;

namespace ArkKeeper.Networking.SteamCmd;

/// <summary>Downloads and extracts steamcmd.exe if it isn't already present — SteamCMD is what
/// actually installs/updates the ARK dedicated server files (see <see cref="SteamCmdClient"/>).</summary>
public sealed class SteamCmdInstaller
{
    private const string DownloadUrl = "https://media.steampowered.com/installer/steamcmd.zip";

    private readonly HttpClient _httpClient;

    public SteamCmdInstaller(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>Ensures steamcmd.exe exists under <paramref name="installDirectory"/>, downloading
    /// and extracting Valve's official zip if it doesn't. Returns the full path to steamcmd.exe.</summary>
    public async Task<string> EnsureInstalledAsync(string installDirectory, CancellationToken cancellationToken = default)
    {
        // Tolerate a caller passing the full path to an existing steamcmd.exe itself rather than
        // its containing directory — an easy mistake given the setting this usually comes from is
        // just labeled "SteamCMD directory". Directory.CreateDirectory would otherwise throw here
        // ("... because a file or directory with the same name already exists"), since a file
        // sits exactly where it wants to create a directory.
        if (File.Exists(installDirectory) && Path.GetFileName(installDirectory).Equals("steamcmd.exe", StringComparison.OrdinalIgnoreCase))
        {
            return installDirectory;
        }

        Directory.CreateDirectory(installDirectory);
        var executablePath = Path.Combine(installDirectory, "steamcmd.exe");

        if (File.Exists(executablePath))
        {
            return executablePath;
        }

        var zipPath = Path.Combine(installDirectory, "steamcmd.zip");
        await using (var responseStream = await _httpClient.GetStreamAsync(DownloadUrl, cancellationToken))
        await using (var fileStream = File.Create(zipPath))
        {
            await responseStream.CopyToAsync(fileStream, cancellationToken);
        }

        ZipFile.ExtractToDirectory(zipPath, installDirectory, overwriteFiles: true);
        File.Delete(zipPath);

        if (!File.Exists(executablePath))
        {
            throw new InvalidOperationException("steamcmd.exe was not found after extracting the SteamCMD download.");
        }

        return executablePath;
    }
}
