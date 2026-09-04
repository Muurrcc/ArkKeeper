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
    // Guards every read/write of _process (and the two CPU-sample fields below it) — Process.Exited
    // fires on a ThreadPool thread, not necessarily the UI thread, and ManagedServer's auto-restart
    // calls Start() (reassigning _process) directly from that Exited handler's continuation. Without
    // this, that could run concurrently with a UI-thread poll timer's Status/SampleResourceUsage
    // call on the very same instance — an unsynchronized field race, not just a theoretical one
    // given this app already polls every server every 2 seconds.
    private readonly object _lock = new();
    private Process? _process;
    private DateTime? _lastCpuSampleAt;
    private TimeSpan? _lastCpuSampleTotal;

    /// <summary>Set only by <see cref="ForProfile"/> — lets <see cref="Start"/> re-derive
    /// ExecutablePath/Arguments/Priority/CpuCoreLimit from the live profile every time instead of
    /// just once. Null for a <see cref="ServerProcess"/> built directly (tests pass a fixed
    /// executable/arguments with no profile at all), where refreshing has nothing to read from.</summary>
    private readonly ServerProfile? _profile;

    public ServerProcess(string executablePath, string arguments, ProcessPriorityLevel priority = ProcessPriorityLevel.Normal, int cpuCoreLimit = 0)
    {
        ExecutablePath = executablePath;
        Arguments = arguments;
        Priority = priority;
        CpuCoreLimit = cpuCoreLimit;
    }

    private ServerProcess(ServerProfile profile)
        : this(profile.GetServerExecutablePath(), LaunchArgumentsBuilder.Build(profile), profile.ProcessPriority, profile.CpuCoreLimit)
    {
        _profile = profile;
    }

    public string ExecutablePath { get; private set; }

    public string Arguments { get; private set; }

    public ProcessPriorityLevel Priority { get; private set; }

    public int CpuCoreLimit { get; private set; }

    public ServerStatus Status
    {
        get { lock (_lock) { return _process is { HasExited: false } ? ServerStatus.Running : ServerStatus.Stopped; } }
    }

    public int? ProcessId
    {
        get { lock (_lock) { return _process is { HasExited: false } p ? p.Id : null; } }
    }

    /// <summary>Raised when the process exits, however that happened (crash, DoExit via RCON, Kill()).</summary>
    public event EventHandler? Exited;

    public static ServerProcess ForProfile(ServerProfile profile) => new(profile);

    /// <summary>Re-derives ExecutablePath/Arguments/Priority/CpuCoreLimit from the live profile —
    /// a <see cref="ManagedServer"/>-and-so-this-<see cref="ServerProcess"/> is created once per
    /// profile and then kept for the app's lifetime (<c>ServerFleet.GetOrAdd</c>), so without this
    /// every launch-only setting (BattlEye, ports, session name, map, RCON, mods, process
    /// priority/CPU limit — anything <c>LaunchArgumentsBuilder</c> reads) would silently keep
    /// using whatever the profile looked like the first time this server was touched, no matter
    /// how many times it was edited and saved afterward. A real bug found by a user toggling
    /// "Disable BattlEye" and seeing it have no effect on an already-known profile.</summary>
    private void RefreshFromProfile()
    {
        if (_profile is null)
        {
            return;
        }

        ExecutablePath = _profile.GetServerExecutablePath();
        Arguments = LaunchArgumentsBuilder.Build(_profile);
        Priority = _profile.ProcessPriority;
        CpuCoreLimit = _profile.CpuCoreLimit;
    }

    public void Start()
    {
        RefreshFromProfile();

        lock (_lock)
        {
            if (_process is { HasExited: false })
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
            _lastCpuSampleAt = null;
            _lastCpuSampleTotal = null;
            ApplyPerformanceSettings(newProcess);
        }
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

    /// <summary>Samples current CPU/RAM usage, or null if the process isn't running. CPU% is
    /// derived from the delta in <see cref="Process.TotalProcessorTime"/> since the previous
    /// call, divided by wall-clock elapsed time and core count — the same technique Task Manager
    /// uses. The first call after a process starts (or after it wasn't running) has no previous
    /// sample to diff against, so it always reports 0% CPU rather than a fabricated number; RAM is
    /// accurate from the first call since it doesn't need a delta.</summary>
    public ResourceUsageSample? SampleResourceUsage()
    {
        lock (_lock)
        {
            if (_process is not { HasExited: false } process)
            {
                _lastCpuSampleAt = null;
                _lastCpuSampleTotal = null;
                return null;
            }

            try
            {
                process.Refresh();
                var now = DateTime.UtcNow;
                var totalCpuTime = process.TotalProcessorTime;

                var cpuPercent = 0.0;
                if (_lastCpuSampleAt is { } lastAt && _lastCpuSampleTotal is { } lastTotal)
                {
                    var elapsedWallMs = (now - lastAt).TotalMilliseconds;
                    var elapsedCpuMs = (totalCpuTime - lastTotal).TotalMilliseconds;
                    if (elapsedWallMs > 0)
                    {
                        cpuPercent = Math.Clamp(elapsedCpuMs / (elapsedWallMs * Environment.ProcessorCount) * 100.0, 0, 100);
                    }
                }

                _lastCpuSampleAt = now;
                _lastCpuSampleTotal = totalCpuTime;

                return new ResourceUsageSample(cpuPercent, process.WorkingSet64);
            }
            catch (InvalidOperationException)
            {
                // The process exited in the window between the HasExited check above and one of
                // the property reads below — Process.Refresh()/TotalProcessorTime/WorkingSet64
                // all throw this once the OS process handle is gone, even though HasExited said
                // it was still running a moment earlier. A poll-timer caller (every server, every
                // 2s) shouldn't have that genuinely-just-happened race crash the whole tick.
                _lastCpuSampleAt = null;
                _lastCpuSampleTotal = null;
                return null;
            }
        }
    }

    /// <summary>Terminates the process immediately. Prefer
    /// <c>ArkKeeper.Networking.Servers.GracefulShutdown</c> when RCON is reachable — killing
    /// outright can lose whatever the world hasn't auto-saved yet.</summary>
    public void Kill()
    {
        lock (_lock)
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
            }
        }
    }

    /// <summary>Not locked while actually awaiting — this can run for as long as the server is up,
    /// and holding <see cref="_lock"/> across that would block every other member (Status, Kill,
    /// a concurrent Start) for the entire wait. Only the reference read at the start needs the lock.</summary>
    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        Process? process;
        lock (_lock)
        {
            process = _process;
        }

        return process?.WaitForExitAsync(cancellationToken) ?? Task.CompletedTask;
    }

    private void OnProcessExited(object? sender, EventArgs e) => Exited?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        lock (_lock)
        {
            if (_process is not null)
            {
                _process.Exited -= OnProcessExited;
                _process.Dispose();
            }
        }
    }
}
