using ArkKeeper.Core.Servers;
using ArkKeeper.Networking.Rcon;

namespace ArkKeeper.Networking.Servers;

/// <summary>
/// Stops a running <see cref="ServerProcess"/> the way ARK server admins are meant to: flush the
/// world to disk and ask the game itself to quit over RCON, rather than killing the process
/// outright (which can lose whatever hasn't auto-saved yet). Falls back to <see cref="ServerProcess.Kill"/>
/// if RCON is unreachable or the process doesn't exit within <paramref name="timeout"/>.
/// </summary>
public static class GracefulShutdown
{
    public static async Task StopAsync(
        ServerProcess process,
        RconClient rcon,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (process.Status != ServerStatus.Running)
        {
            return;
        }

        try
        {
            await rcon.ExecuteCommandAsync("SaveWorld", cancellationToken);
            await rcon.ExecuteCommandAsync("DoExit", cancellationToken);
        }
        catch
        {
            // RCON unreachable or the command failed — the timeout below still applies,
            // and Kill() is the fallback either way.
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill();
        }
    }
}
