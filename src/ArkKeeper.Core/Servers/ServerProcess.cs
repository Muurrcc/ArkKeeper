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

    public ServerProcess(string executablePath, string arguments, ProcessPriorityLevel priority = ProcessPriorityLevel.Normal, int cpuCoreLimit = 0)
    {
        ExecutablePath = executablePath;
        Arguments = arguments;
        Priority = priority;
        CpuCoreLimit = cpuCoreLimit;
    }

    public string ExecutablePath { get; }

    public string Arguments { get; }

    public ProcessPriorityLevel Priority { get; }

    public int CpuCoreLimit { get; }

    public ServerStatus Status => _process is { HasExited: false } ? ServerStatus.Running : ServerStatus.Stopped;

    public int? ProcessId => Status == ServerStatus.Running ? _process!.Id : null;

    /// <summary>Raised when the process exits, however that happened (crash, DoExit via RCON, Kill()).</summary>
    public event EventHandler? Exited;

    public static ServerProcess ForProfile(ServerProfile profile) =>
        new(profile.GetServerExecutablePath(), LaunchArgumentsBuilder.Build(profile), profile.ProcessPriority, profile.CpuCoreLimit);

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
        ApplyPerformanceSettings(newProcess);
    }

    /// <summary>Best-effort: a priority/affinity change failing (e.g. the process already exited,
    /// or the OS denies it) shouldn't take down a server that otherwise started fine.</summary>
    private void ApplyPerformanceSettings(Process process)
    {
        try
        {
            process.PriorityClass = Priority switch
            {
                ProcessPriorityLevel.Idle => ProcessPriorityClass.Idle,
                ProcessPriorityLevel.BelowNormal => ProcessPriorityClass.BelowNormal,
                ProcessPriorityLevel.AboveNormal => ProcessPriorityClass.AboveNormal,
                ProcessPriorityLevel.High => ProcessPriorityClass.High,
                _ => ProcessPriorityClass.Normal,
            };

            if (CpuCoreLimit > 0 && CpuCoreLimit < Environment.ProcessorCount &&
                (OperatingSystem.IsWindows() || OperatingSystem.IsLinux()))
            {
                // The lowest CpuCoreLimit bits — e.g. a limit of 4 restricts the process to
                // cores 0-3, mirroring what setting affinity by hand in Task Manager looks like.
                // ProcessorAffinity isn't supported on macOS, hence the platform check.
                process.ProcessorAffinity = (nint)((1L << CpuCoreLimit) - 1);
            }
        }
        catch
        {
        }
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
