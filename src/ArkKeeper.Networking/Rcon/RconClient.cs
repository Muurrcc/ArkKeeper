using System.Buffers.Binary;
using System.Net.Sockets;

namespace ArkKeeper.Networking.Rcon;

/// <summary>A Source RCON protocol client for sending admin commands to an ARK dedicated server.
/// Thread-safe: <see cref="ConnectAsync"/> and <see cref="ExecuteCommandAsync"/> serialize
/// against each other internally, since the protocol has no way to disambiguate interleaved
/// writes/reads on one connection — two concurrent ExecuteCommandAsync calls without this would
/// corrupt the stream (one could read the other's response, or the two writes could interleave
/// mid-packet), discovered via a genuinely concurrent test rather than by inspection.</summary>
public sealed class RconClient : IAsyncDisposable
{
    private readonly TcpClient _tcpClient = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private NetworkStream? _stream;
    private int _nextPacketId = 1;

    public bool IsConnected => _stream is not null;

    public async Task ConnectAsync(string host, int port, string password, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await _tcpClient.ConnectAsync(host, port, cancellationToken);
            _stream = _tcpClient.GetStream();

            var authId = NextId();
            await SendPacketAsync(new RconPacket(authId, RconPacketType.Auth, password), cancellationToken);

            // The server sends an empty SERVERDATA_RESPONSE_VALUE packet before the real
            // SERVERDATA_AUTH_RESPONSE packet — drain it first.
            await ReceivePacketAsync(cancellationToken);
            var authResponse = await ReceivePacketAsync(cancellationToken);

            if (authResponse.Id != authId)
            {
                throw new RconAuthenticationException();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            EnsureConnected();

            var id = NextId();
            await SendPacketAsync(new RconPacket(id, RconPacketType.ExecCommandOrAuthResponse, command), cancellationToken);
            var response = await ReceivePacketAsync(cancellationToken);
            return response.Body;
        }
        finally
        {
            _lock.Release();
        }
    }

    private int NextId() => _nextPacketId++;

    private void EnsureConnected()
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Call ConnectAsync before executing commands.");
        }
    }

    private async Task SendPacketAsync(RconPacket packet, CancellationToken cancellationToken)
    {
        EnsureConnected();
        await _stream!.WriteAsync(packet.Encode(), cancellationToken);
    }

    private async Task<RconPacket> ReceivePacketAsync(CancellationToken cancellationToken)
    {
        EnsureConnected();

        var header = new byte[4];
        await ReadExactAsync(header, cancellationToken);
        var size = BinaryPrimitives.ReadInt32LittleEndian(header);

        var full = new byte[4 + size];
        header.CopyTo(full, 0);
        await ReadExactAsync(full.AsMemory(4, size), cancellationToken);

        RconPacket.TryDecode(full, out var packet, out _);
        return packet;
    }

    private async Task ReadExactAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await _stream!.ReadAsync(buffer[totalRead..], cancellationToken);
            if (read == 0)
            {
                throw new IOException("RCON connection closed by the server.");
            }
            totalRead += read;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_stream is not null)
        {
            await _stream.DisposeAsync();
        }
        _tcpClient.Dispose();
        _lock.Dispose();
    }
}
