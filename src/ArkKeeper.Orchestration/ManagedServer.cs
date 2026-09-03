using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Servers;
using ArkKeeper.Discord;
using ArkKeeper.Networking.Rcon;
using ArkKeeper.Networking.Servers;

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
    private RconClient? _rcon;

    public ManagedServer(ServerProfile profile, DiscordWebhookNotifier? notifier = null)
        : this(profile, ServerProcess.ForProfile(profile), notifier)
    {
    }

    /// <summary>Lets a caller supply the <see cref="ServerProcess"/> directly — mainly so tests
    /// can pass a harmless stand-in process instead of the real server executable.</summary>
    public ManagedServer(ServerProfile profile, ServerProcess process, DiscordWebhookNotifier? notifier = null)
    {
        Profile = profile;
        Notifier = notifier;
        _process = process;
        _process.Exited += OnProcessExited;
    }

    public ServerProfile Profile { get; }

    public DiscordWebhookNotifier? Notifier { get; set; }

    public ServerStatus Status => _process.Status;

    public int? ProcessId => _process.ProcessId;

    public void Start()
    {
        _process.Start();
        FireAndForgetNotify(n => n.NotifyServerStartedAsync(Profile.SessionName));
    }

    /// <summary>Stops the server the safe way: RCON SaveWorld+DoExit, falling back to killing
    /// the process if that doesn't work within <paramref name="timeout"/>.</summary>
    public async Task StopAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var rcon = await GetOrConnectRconAsync(cancellationToken);
        await GracefulShutdown.StopAsync(_process, rcon, timeout, cancellationToken);
    }

    /// <summary>Immediately terminates the process without attempting a graceful RCON shutdown.</summary>
    public void Kill() => _process.Kill();

    public async Task<string> SendRconCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        var rcon = await GetOrConnectRconAsync(cancellationToken);
        return await rcon.ExecuteCommandAsync(command, cancellationToken);
    }

    private async Task<RconClient> GetOrConnectRconAsync(CancellationToken cancellationToken)
    {
        if (_rcon is { IsConnected: true })
        {
            return _rcon;
        }

        if (_rcon is not null)
        {
            await _rcon.DisposeAsync();
        }
        _rcon = new RconClient();
        await _rcon.ConnectAsync("127.0.0.1", Profile.RconPort, Profile.AdminPassword, cancellationToken);
        return _rcon;
    }

    private void OnProcessExited(object? sender, EventArgs e) =>
        FireAndForgetNotify(n => n.NotifyServerStoppedAsync(Profile.SessionName));

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
        _process.Exited -= OnProcessExited;
        if (_rcon is not null)
        {
            await _rcon.DisposeAsync();
        }
        _process.Dispose();
    }
}
