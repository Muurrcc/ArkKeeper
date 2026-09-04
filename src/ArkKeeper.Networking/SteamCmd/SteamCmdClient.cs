using System.Diagnostics;
using ArkKeeper.Networking.Processes;

namespace ArkKeeper.Networking.SteamCmd;

/// <summary>Invokes steamcmd.exe to install/update the ARK dedicated server, and to download
/// Steam Workshop mod content for it.</summary>
public sealed class SteamCmdClient
{
    /// <summary>Steam app id for "ARK: Survival Evolved Dedicated Server" — verified against
    /// Steam's own app manifest (api.steamcmd.net), not guessed.</summary>
    public const int ArkDedicatedServerAppId = 376030;

    private readonly string _steamCmdExecutablePath;

    public SteamCmdClient(string steamCmdExecutablePath)
    {
        _steamCmdExecutablePath = steamCmdExecutablePath;
    }

    /// <summary>
    /// Installs or updates the ARK dedicated server into <paramref name="installDirectory"/> via
    /// anonymous SteamCMD login. <paramref name="onOutput"/> receives steamcmd's console output
    /// line by line, useful for showing install progress.
    ///
    /// Note: SteamCMD is known to sometimes exit non-zero on its first-ever run (it self-updates
    /// before doing anything else) even though the requested install/update still completed on a
    /// later internal retry — callers shouldn't treat a non-zero exit code alone as proof of
    /// failure without also checking whether the server executable actually landed on disk.
    /// </summary>
    public Task<int> InstallOrUpdateAsync(
        string installDirectory,
        Action<string>? onOutput = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(installDirectory);
        return RunAsync(BuildArguments(installDirectory), onOutput, cancellationToken);
    }

    /// <summary>Downloads a single Steam Workshop mod's content into <paramref name="installDirectory"/>'s
    /// steamapps/workshop folder — see <see cref="GetWorkshopItemPath"/> for where it lands.
    /// Anonymous login works for public ARK mods (no Steam account/API key needed).</summary>
    public Task<int> DownloadWorkshopItemAsync(
        string installDirectory,
        string publishedFileId,
        Action<string>? onOutput = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(installDirectory);
        return RunAsync(BuildWorkshopDownloadArguments(installDirectory, publishedFileId), onOutput, cancellationToken);
    }

    /// <summary>Builds the steamcmd.exe argument line for an anonymous install/update of the ARK
    /// dedicated server. Kept separate from process invocation so it's testable without spawning
    /// anything.</summary>
    public static string BuildArguments(string installDirectory) => string.Join(' ',
        "+force_install_dir", Quote(installDirectory),
        "+login anonymous",
        $"+app_update {ArkDedicatedServerAppId} validate",
        "+quit");

    /// <summary>Builds the steamcmd.exe argument line for downloading one Workshop item.</summary>
    public static string BuildWorkshopDownloadArguments(string installDirectory, string publishedFileId) => string.Join(' ',
        "+force_install_dir", Quote(installDirectory),
        "+login anonymous",
        $"+workshop_download_item {ArkDedicatedServerAppId} {publishedFileId}",
        "+quit");

    /// <summary>Where SteamCMD puts a downloaded Workshop item's files, per its own fixed
    /// steamapps/workshop/content/&lt;appid&gt;/&lt;publishedfileid&gt; convention.</summary>
    public static string GetWorkshopItemPath(string installDirectory, string publishedFileId) =>
        Path.Combine(installDirectory, "steamapps", "workshop", "content", ArkDedicatedServerAppId.ToString(), publishedFileId);

    /// <summary>Internal (not private) so tests can exercise cancellation timing with an arbitrary
    /// executable/argument pair — the public methods above always pass steamcmd's own fixed flags,
    /// which exit too fast against a stand-in executable to test cancelling mid-run.</summary>
    internal async Task<int> RunAsync(string arguments, Action<string>? onOutput, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _steamCmdExecutablePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.Start();

        // WaitForExitAsync's own cancellation only stops *awaiting* — it doesn't touch the
        // process itself, which would otherwise keep running (and downloading) in the background
        // even after the caller thinks a Cancel/Quit click actually stopped it. Kill it for real.
        await using var killOnCancel = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best-effort — the process may have exited in the tiny window between the
                // HasExited check and Kill().
            }
        });

        // Process.OutputDataReceived/BeginOutputReadLine only split on '\n' — SteamCMD's download
        // progress uses '\r'-only updates, so that reader buffers them silently until the next
        // real newline. ProcessOutputPump treats '\r' as a line terminator too. Not passing
        // cancellationToken through here: once Kill() above closes the process's output pipes,
        // these end on their own (ReadAsync returns 0) without needing their own cancellation.
        var stdOutTask = ProcessOutputPump.PumpAsync(process.StandardOutput, onOutput, CancellationToken.None);
        var stdErrTask = ProcessOutputPump.PumpAsync(process.StandardError, onOutput, CancellationToken.None);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        finally
        {
            await Task.WhenAll(stdOutTask, stdErrTask);
        }

        return process.ExitCode;
    }

    private static string Quote(string value) => $"\"{value}\"";
}
