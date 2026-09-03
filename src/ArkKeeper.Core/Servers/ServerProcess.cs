using System.Diagnostics;
using ArkKeeper.Core.Launch;
using ArkKeeper.Core.Profiles;

namespace ArkKeeper.Core.Servers;

/// <summary>
/// Wraps the actual dedicated server OS process — starting it, tracking whether it's alive, and
/// killing it if needed. This is the piece the original tool centers on that ArkKeeper didn't
/// have until now: everything else (RCON, ini files, launch args) existed but nothing actually
/// ran the server.
///
/// Only knows about the OS process, not RCON — a graceful, save-before-quit shutdown that uses
/// both this and an RconClient lives in ArkKeeper.Networking.Servers.GracefulShutdown, since
/// Core doesn't (and shouldn't) depend on Networking.
/// </summary>
public sealed class ServerProcess : IDisposable
{
    private Process? _process;

    public ServerProcess(string executablePath, string arguments)
    {
        ExecutablePath = executablePath;
        Arguments = arguments;
    }

    public string ExecutablePath { get; }

    public string Arguments { get; }

    public ServerStatus Status => _process is { HasExited: false } ? ServerStatus.Running : ServerStatus.Stopped;

    public int? ProcessId => Status == ServerStatus.Running ? _process!.Id : null;

    /// <summary>Raised when the process exits, however that happened (crash, DoExit via RCON, Kill()).</summary>
    public event EventHandler? Exited;

    public static ServerProcess ForProfile(ServerProfile profile) =>
        new(profile.GetServerExecutablePath(), LaunchArgumentsBuilder.Build(profile));

    public void Start()
    {
        if (Status == ServerStatus.Running)
        {
            throw new InvalidOperationException("The server process is already running.");
        }

        if (!File.Exists(ExecutablePath))
        {
            throw new FileNotFoundException("Server executable not found — install/update it first.", ExecutablePath);
        }

        // Release the previous instance's handle before replacing it — Start() can be called
        // again after a prior run exited (e.g. ManagedServer's auto-restart), and leaving the
        // old Process object around unreleased/undisposed is a real resource leak, not just tidiness.
        if (_process is not null)
        {
            _process.Exited -= OnProcessExited;
            _process.Dispose();
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = ExecutablePath,
            Arguments = Arguments,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(ExecutablePath)),
        };

        var newProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        newProcess.Exited += OnProcessExited;
        newProcess.Start();
        _process = newProcess;
    }

    /// <summary>Terminates the process immediately. Prefer
    /// <c>ArkKeeper.Networking.Servers.GracefulShutdown</c> when RCON is reachable — killing
    /// outright can lose whatever the world hasn't auto-saved yet.</summary>
    public void Kill()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
        }
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
        _process?.WaitForExitAsync(cancellationToken) ?? Task.CompletedTask;

    private void OnProcessExited(object? sender, EventArgs e) => Exited?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        if (_process is not null)
        {
            _process.Exited -= OnProcessExited;
            _process.Dispose();
        }
    }
}
