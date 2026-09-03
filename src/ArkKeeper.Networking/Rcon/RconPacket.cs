using System.Buffers.Binary;
using System.Text;

namespace ArkKeeper.Networking.Rcon;

/// <summary>
/// A single Source RCON protocol packet. Wire format (all integers little-endian):
/// Int32 Size | Int32 Id | Int32 Type | Body (UTF8, null-terminated) | empty-string null terminator.
/// `Size` counts everything after itself.
/// </summary>
public readonly record struct RconPacket(int Id, RconPacketType Type, string Body)
{
    public byte[] Encode()
    {
        var bodyBytes = Encoding.UTF8.GetBytes(Body);
        var payloadSize = 4 + 4 + bodyBytes.Length + 1 + 1;
        var buffer = new byte[4 + payloadSize];

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(0, 4), payloadSize);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4, 4), Id);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(8, 4), (int)Type);
        bodyBytes.CopyTo(buffer, 12);
        // The two trailing bytes are already zero-initialized (null terminators).

        return buffer;
    }

    /// <summary>Attempts to decode one packet from the start of <paramref name="buffer"/>.
    /// Returns false if the buffer doesn't yet contain a full packet.</summary>
    public static bool TryDecode(ReadOnlySpan<byte> buffer, out RconPacket packet, out int bytesConsumed)
    {
        packet = default;
        bytesConsumed = 0;

        if (buffer.Length < 4)
        {
            return false;
        }

        var size = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        var totalLength = 4 + size;
        if (size < 8 || buffer.Length < totalLength)
        {
            return false;
        }

        var id = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(4, 4));
        var type = (RconPacketType)BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(8, 4));
        var bodyLength = size - 4 - 4 - 2;
        var body = bodyLength > 0
            ? Encoding.UTF8.GetString(buffer.Slice(12, bodyLength))
            : string.Empty;

        packet = new RconPacket(id, type, body);
        bytesConsumed = totalLength;
        return true;
    }
}
