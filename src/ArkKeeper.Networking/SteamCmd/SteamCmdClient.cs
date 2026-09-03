using System.Diagnostics;

namespace ArkKeeper.Networking.SteamCmd;

/// <summary>Invokes steamcmd.exe to install or update the ARK dedicated server files.</summary>
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
    public async Task<int> InstallOrUpdateAsync(
        string installDirectory,
        Action<string>? onOutput = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(installDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = _steamCmdExecutablePath,
            Arguments = BuildArguments(installDirectory),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) onOutput?.Invoke(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) onOutput?.Invoke(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }

    /// <summary>Builds the steamcmd.exe argument line for an anonymous install/update of the ARK
    /// dedicated server. Kept separate from process invocation so it's testable without spawning
    /// anything.</summary>
    public static string BuildArguments(string installDirectory) => string.Join(' ',
        "+force_install_dir", Quote(installDirectory),
        "+login anonymous",
        $"+app_update {ArkDedicatedServerAppId} validate",
        "+quit");

    private static string Quote(string value) => $"\"{value}\"";
}
