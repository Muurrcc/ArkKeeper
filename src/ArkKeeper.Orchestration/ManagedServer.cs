using ArkKeeper.Core.Players;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Scheduling;
using ArkKeeper.Core.Servers;
using ArkKeeper.Discord;
using ArkKeeper.Networking.Rcon;
using ArkKeeper.Networking.Servers;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArkKeeper.Orchestration;

/// <summary>
/// Ties one <see cref="ServerProfile"/> together with its actual OS process, its RCON
/// connection, and (optionally) Discord notifications — the thing that was still missing after
/// building each of those pieces separately: nothing connected "the server exited" to "tell
/// Discord", or owned the RCON connection a graceful stop needs.
/// </summary>
public sealed class ManagedServer : IAsyncDisposable
{
    private readonly ServerProcess _process;
    private readonly ILogger _logger;

    // Guards every use of _rcon — not just who gets to (re)connect it, but who gets to send a
    // command on it. RconClient has no per-command framing/id disambiguation of its own, so two
    // ExecuteCommandAsync calls running concurrently on the *same* connection interleave their
    // writes/reads and corrupt the stream (discovered via a genuinely concurrent test — an
    // earlier version of this lock only serialized *connecting*, not *sending*, and hung).
    private readonly SemaphoreSlim _rconLock = new(1, 1);
    private RconClient? _rcon;
    private bool _stopRequested;
    private CancellationTokenSource? _pendingRestartCts;

    public ManagedServer(ServerProfile profile, DiscordWebhookNotifier? notifier = null, ILogger<ManagedServer>? logger = null)
        : this(profile, ServerProcess.ForProfile(profile), notifier, logger)
    {
    }

    /// <summary>Lets a caller supply the <see cref="ServerProcess"/> directly — mainly so tests
    /// can pass a harmless stand-in process instead of the real server executable.</summary>
    public ManagedServer(ServerProfile profile, ServerProcess process, DiscordWebhookNotifier? notifier = null, ILogger<ManagedServer>? logger = null)
    {
        Profile = profile;
        Notifier = notifier;
        _logger = logger ?? NullLogger<ManagedServer>.Instance;
        _process = process;
        _process.Exited += OnProcessExited;
    }

    public ServerProfile Profile { get; }

    public DiscordWebhookNotifier? Notifier { get; set; }

    /// <summary>When true, an unexpected process exit (a crash — not a requested <see cref="StopAsync"/>
    /// or <see cref="Kill"/>) automatically restarts the server.</summary>
    public bool AutoRestart { get; set; }

    /// <summary>Delay before an auto-restart actually happens. Exists so a server that crashes
    /// immediately on every launch (bad config, missing mod, etc.) doesn't spin in a tight
    /// restart loop — without this, a instant-crashing process previously caused exactly that,
    /// hammering the OS with process creation and racing Start()/Dispose() on the same instant.</summary>
    public TimeSpan AutoRestartDelay { get; set; } = TimeSpan.FromSeconds(5);

    public ServerStatus Status => _process.Status;

    public int? ProcessId => _process.ProcessId;

    /// <summary>Writes GameUserSettings.ini/Game.ini before launching — without this, none of
    /// ServerProfile's settings beyond the handful LaunchArgumentsBuilder puts on the command
    /// line (map, ports, session name, mods, ...) would ever actually reach the running server;
    /// everything else the UI exposes would silently do nothing.</summary>
    public void Start()
    {
        _stopRequested = false;
        CancelPendingRestart();
        _logger.LogInformation("Starting server {SessionName}", Profile.SessionName);
        Profile.WriteConfigFiles();
        _process.Start();
        _logger.LogInformation("Server {SessionName} started, PID {ProcessId}", Profile.SessionName, _process.ProcessId);
        FireAndForgetNotify(n => n.NotifyServerStartedAsync(Profile.SessionName));
    }

    /// <summary>Stops the server the safe way: RCON SaveWorld+DoExit, falling back to killing
    /// the process if that doesn't work within <paramref name="timeout"/>. Holds the RCON lock
    /// for the whole operation, so no other RCON command can interleave with (or run against a
    /// connection that's about to go away because of) the shutdown sequence.</summary>
    public async Task StopAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        _stopRequested = true;
        CancelPendingRestart();
        _logger.LogInformation("Stopping server {SessionName} (graceful, timeout {Timeout})", Profile.SessionName, timeout);

        await _rconLock.WaitAsync(cancellationToken);
        try
        {
            RconClient? rcon = null;
            try
            {
                rcon = await EnsureConnectedAsync(forceReconnect: false, cancellationToken);
            }
            catch (Exception ex)
            {
                // RCON refusing the connection (server still loading, RCON disabled, wrong
                // port/password, ...) used to bubble straight out of StopAsync and leave the
                // process running — Stop looked like it silently did nothing. Falling back to
                // Kill() here instead means Stop always actually stops the server, same as when
                // GracefulShutdown's own RCON commands fail once connected.
                _logger.LogWarning(ex, "Could not reach RCON to stop {SessionName} gracefully, killing instead", Profile.SessionName);
            }

            if (rcon is not null)
            {
                await GracefulShutdown.StopAsync(_process, rcon, timeout, cancellationToken);
            }

            if (_process.Status == ServerStatus.Running)
            {
                _process.Kill();
            }
        }
        finally
        {
            _rconLock.Release();
        }

        _logger.LogInformation("Server {SessionName} stopped", Profile.SessionName);
    }

    /// <summary>Immediately terminates the process without attempting a graceful RCON shutdown.</summary>
    public void Kill()
    {
        _stopRequested = true;
        CancelPendingRestart();
        _logger.LogWarning("Killing server {SessionName} without a graceful RCON shutdown", Profile.SessionName);
        _process.Kill();
    }

    /// <summary>Sends an RCON command, retrying once with a fresh connection if it fails — RCON
    /// connections can go stale (e.g. the server restarted, or the TCP connection dropped
    /// silently) without <see cref="RconClient.IsConnected"/> having a way to detect that ahead
    /// of time. Safe to call concurrently: each call holds the RCON lock for its whole
    /// connect+send+read sequence, so overlapping calls queue up on one shared connection
    /// instead of each opening their own or corrupting each other's reads.</summary>
    public async Task<string> SendRconCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        await _rconLock.WaitAsync(cancellationToken);
        try
        {
            var rcon = await EnsureConnectedAsync(forceReconnect: false, cancellationToken);
            try
            {
                return await rcon.ExecuteCommandAsync(command, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or SocketException)
            {
                _logger.LogWarning(ex, "RCON command failed for {SessionName}, reconnecting and retrying once", Profile.SessionName);
                var freshRcon = await EnsureConnectedAsync(forceReconnect: true, cancellationToken);
                return await freshRcon.ExecuteCommandAsync(command, cancellationToken);
            }
        }
        finally
        {
            _rconLock.Release();
        }
    }

    /// <summary>Lists currently-connected players, parsed from RCON's <c>ListPlayers</c> — goes
    /// through <see cref="SendRconCommandAsync"/> like every other command, so it shares the same
    /// connect/lock/retry behavior rather than opening a connection of its own.</summary>
    public async Task<IReadOnlyList<ConnectedPlayer>> GetPlayersAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendRconCommandAsync("ListPlayers", cancellationToken);
        return ListPlayersParser.Parse(response);
    }

    public Task<string> KickPlayerAsync(string steamId, CancellationToken cancellationToken = default) =>
        SendRconCommandAsync($"KickPlayer {steamId}", cancellationToken);

    public Task<string> BanPlayerAsync(string steamId, CancellationToken cancellationToken = default) =>
        SendRconCommandAsync($"BanPlayer {steamId}", cancellationToken);

    public Task<string> UnbanPlayerAsync(string steamId, CancellationToken cancellationToken = default) =>
        SendRconCommandAsync($"UnbanPlayer {steamId}", cancellationToken);

    /// <summary>Runs whichever of <paramref name="scheduler"/>'s tasks are due, over this
    /// server's own managed RCON connection — via <see cref="SendRconCommandAsync"/>, so it
    /// shares the same connect/lock/retry behavior as everything else here, rather than the
    /// scheduler needing a second, independent <see cref="RconClient"/>.</summary>
    public Task<IReadOnlyList<ScheduledTask>> RunDueScheduledTasksAsync(
        SchedulerRunner scheduler, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        scheduler.RunDueTasksAsync(SendRconCommandAsync, now, cancellationToken);

    /// <summary>Returns the current RCON connection, or establishes one. Callers must already
    /// hold <see cref="_rconLock"/> — this does no locking of its own.</summary>
    private async Task<RconClient> EnsureConnectedAsync(bool forceReconnect, CancellationToken cancellationToken)
    {
        if (!forceReconnect && _rcon is { IsConnected: true })
        {
            return _rcon;
        }

        if (_rcon is not null)
        {
            await _rcon.DisposeAsync();
            _rcon = null;
        }

        var rcon = new RconClient();
        await rcon.ConnectAsync("127.0.0.1", Profile.RconPort, Profile.AdminPassword, cancellationToken);
        _rcon = rcon;
        return rcon;
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        _logger.LogWarning("Server {SessionName} process exited", Profile.SessionName);
        FireAndForgetNotify(n => n.NotifyServerStoppedAsync(Profile.SessionName));

        if (AutoRestart && !_stopRequested)
        {
            _logger.LogWarning(
                "Server {SessionName} exited unexpectedly, auto-restarting in {Delay}",
                Profile.SessionName, AutoRestartDelay);

            var cts = new CancellationTokenSource();
            _pendingRestartCts = cts;
            _ = AutoRestartAfterDelayAsync(cts.Token);
        }
    }

    private async Task AutoRestartAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AutoRestartDelay, cancellationToken);
            Start();
        }
        catch (OperationCanceledException)
        {
            // An explicit Start()/StopAsync()/Kill()/DisposeAsync() cancelled this pending
            // restart — nothing to do.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-restart failed for {SessionName}", Profile.SessionName);
        }
    }

    /// <summary>Cancels a pending auto-restart, if one is scheduled — called from anything that
    /// represents an intentional state change (Start/Stop/Kill/Dispose), so a delayed restart
    /// can never fire after one of those already ran.</summary>
    private void CancelPendingRestart()
    {
        if (_pendingRestartCts is { } cts)
        {
            _pendingRestartCts = null;
            cts.Cancel();
            cts.Dispose();
        }
    }

    private void FireAndForgetNotify(Func<DiscordWebhookNotifier, Task> notify)
    {
        if (Notifier is not { } notifier)
        {
            return;
        }

        _ = notify(notifier);
    }

    public async ValueTask DisposeAsync()
    {
        _stopRequested = true;
        CancelPendingRestart();
        _process.Exited -= OnProcessExited;

        await _rconLock.WaitAsync();
        try
        {
            if (_rcon is not null)
            {
                await _rcon.DisposeAsync();
                _rcon = null;
            }
        }
        finally
        {
            _rconLock.Release();
        }

        _rconLock.Dispose();
        _process.Dispose();
    }
}
