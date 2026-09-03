using ArkKeeper.Core.Profiles;
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
    private RconClient? _rcon;
    private bool _stopRequested;

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

    public void Start()
    {
        _stopRequested = false;
        _logger.LogInformation("Starting server {SessionName}", Profile.SessionName);
        _process.Start();
        _logger.LogInformation("Server {SessionName} started, PID {ProcessId}", Profile.SessionName, _process.ProcessId);
        FireAndForgetNotify(n => n.NotifyServerStartedAsync(Profile.SessionName));
    }

    /// <summary>Stops the server the safe way: RCON SaveWorld+DoExit, falling back to killing
    /// the process if that doesn't work within <paramref name="timeout"/>.</summary>
    public async Task StopAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        _stopRequested = true;
        _logger.LogInformation("Stopping server {SessionName} (graceful, timeout {Timeout})", Profile.SessionName, timeout);
        var rcon = await GetOrConnectRconAsync(cancellationToken);
        await GracefulShutdown.StopAsync(_process, rcon, timeout, cancellationToken);
        _logger.LogInformation("Server {SessionName} stopped", Profile.SessionName);
    }

    /// <summary>Immediately terminates the process without attempting a graceful RCON shutdown.</summary>
    public void Kill()
    {
        _stopRequested = true;
        _logger.LogWarning("Killing server {SessionName} without a graceful RCON shutdown", Profile.SessionName);
        _process.Kill();
    }

    /// <summary>Sends an RCON command, retrying once with a fresh connection if it fails — RCON
    /// connections can go stale (e.g. the server restarted, or the TCP connection dropped
    /// silently) without <see cref="RconClient.IsConnected"/> having a way to detect that ahead
    /// of time.</summary>
    public async Task<string> SendRconCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        try
        {
            var rcon = await GetOrConnectRconAsync(cancellationToken);
            return await rcon.ExecuteCommandAsync(command, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            _logger.LogWarning(ex, "RCON command failed for {SessionName}, reconnecting and retrying once", Profile.SessionName);
            await ForceDisconnectRconAsync();
            var rcon = await GetOrConnectRconAsync(cancellationToken);
            return await rcon.ExecuteCommandAsync(command, cancellationToken);
        }
    }

    private async Task<RconClient> GetOrConnectRconAsync(CancellationToken cancellationToken)
    {
        if (_rcon is { IsConnected: true })
        {
            return _rcon;
        }

        await ForceDisconnectRconAsync();
        _rcon = new RconClient();
        await _rcon.ConnectAsync("127.0.0.1", Profile.RconPort, Profile.AdminPassword, cancellationToken);
        return _rcon;
    }

    private async Task ForceDisconnectRconAsync()
    {
        if (_rcon is not null)
        {
            await _rcon.DisposeAsync();
            _rcon = null;
        }
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
            _ = AutoRestartAfterDelayAsync();
        }
    }

    private async Task AutoRestartAfterDelayAsync()
    {
        try
        {
            await Task.Delay(AutoRestartDelay);

            if (_stopRequested)
            {
                return;
            }

            Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-restart failed for {SessionName}", Profile.SessionName);
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
        _process.Exited -= OnProcessExited;
        if (_rcon is not null)
        {
            await _rcon.DisposeAsync();
        }
        _process.Dispose();
    }
}
