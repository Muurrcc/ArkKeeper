using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using ArkKeeper.Networking.Rcon;

namespace ArkKeeper.Orchestration.Tests;

/// <summary>A minimal in-process Source RCON server for testing ManagedServer/SchedulerRunner
/// without a real ARK server: authenticates any password and echoes back every command it
/// receives (recording them) so tests can assert on what was actually sent over the wire.</summary>
internal sealed class FakeRconServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;
    private readonly List<string> _receivedCommands = new();

    public FakeRconServer()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = AcceptLoopAsync(_cts.Token);
    }

    public int Port { get; }

    /// <summary>If set, the server closes the connection after this many commands on it —
    /// simulating a mid-session drop, so tests can verify a client's reconnect/retry logic.</summary>
    public int? CloseConnectionAfterCommands { get; set; }

    /// <summary>Artificial delay before responding to each command — widens the window for tests
    /// that need to land a concurrent mutation (e.g. SchedulerRunner.Add) while a command is
    /// still in flight.</summary>
    public TimeSpan ResponseDelay { get; set; } = TimeSpan.Zero;

    public IReadOnlyList<string> ReceivedCommands
    {
        get { lock (_receivedCommands) { return _receivedCommands.ToArray(); } }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                await HandleClientAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();

        var authPacket = await ReadPacketAsync(stream, cancellationToken);
        // Real servers send an empty ResponseValue packet before the auth response.
        await WritePacketAsync(stream, new RconPacket(0, RconPacketType.ResponseValue, string.Empty), cancellationToken);
        await WritePacketAsync(stream, new RconPacket(authPacket.Id, RconPacketType.ExecCommandOrAuthResponse, string.Empty), cancellationToken);

        var commandsOnThisConnection = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            RconPacket command;
            try
            {
                command = await ReadPacketAsync(stream, cancellationToken);
            }
            catch
            {
                return;
            }

            lock (_receivedCommands)
            {
                _receivedCommands.Add(command.Body);
            }

            if (ResponseDelay > TimeSpan.Zero)
            {
                await Task.Delay(ResponseDelay, cancellationToken);
            }

            await WritePacketAsync(stream, new RconPacket(command.Id, RconPacketType.ResponseValue, "OK"), cancellationToken);

            commandsOnThisConnection++;
            if (CloseConnectionAfterCommands is { } limit && commandsOnThisConnection >= limit)
            {
                return;
            }
        }
    }

    private static async Task<RconPacket> ReadPacketAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var header = new byte[4];
        await ReadExactAsync(stream, header, cancellationToken);
        var size = BinaryPrimitives.ReadInt32LittleEndian(header);

        var full = new byte[4 + size];
        header.CopyTo(full, 0);
        await ReadExactAsync(stream, full.AsMemory(4, size), cancellationToken);

        RconPacket.TryDecode(full, out var packet, out _);
        return packet;
    }

    private static async Task ReadExactAsync(NetworkStream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[totalRead..], cancellationToken);
            if (read == 0)
            {
                throw new IOException("Connection closed.");
            }
            totalRead += read;
        }
    }

    private static Task WritePacketAsync(NetworkStream stream, RconPacket packet, CancellationToken cancellationToken) =>
        stream.WriteAsync(packet.Encode(), cancellationToken).AsTask();

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        try
        {
            await _acceptLoop;
        }
        catch
        {
            // Best-effort shutdown.
        }
    }
}
