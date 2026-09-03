using ArkKeeper.Networking.Rcon;
using Xunit;

namespace ArkKeeper.Networking.Tests;

public class RconPacketTests
{
    [Fact]
    public void Encode_ThenTryDecode_RoundTrips()
    {
        var original = new RconPacket(42, RconPacketType.ExecCommandOrAuthResponse, "ListPlayers");

        var encoded = original.Encode();
        var decoded = RconPacket.TryDecode(encoded, out var packet, out var consumed);

        Assert.True(decoded);
        Assert.Equal(encoded.Length, consumed);
        Assert.Equal(original, packet);
    }

    [Fact]
    public void Encode_ProducesCorrectSizeHeader()
    {
        // size = id(4) + type(4) + body("hi" = 2 bytes) + 2 null terminators = 12
        var packet = new RconPacket(1, RconPacketType.Auth, "hi");

        var encoded = packet.Encode();

        var size = BitConverter.ToInt32(encoded, 0);
        Assert.Equal(12, size);
        Assert.Equal(4 + 12, encoded.Length);
    }

    [Fact]
    public void TryDecode_OnIncompleteBuffer_ReturnsFalse()
    {
        var full = new RconPacket(1, RconPacketType.Auth, "password").Encode();
        var partial = full.AsSpan(0, full.Length - 3).ToArray();

        var decoded = RconPacket.TryDecode(partial, out _, out var consumed);

        Assert.False(decoded);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void Encode_HandlesEmptyBody()
    {
        var packet = new RconPacket(7, RconPacketType.ResponseValue, string.Empty);

        var encoded = packet.Encode();
        RconPacket.TryDecode(encoded, out var decoded, out _);

        Assert.Equal(string.Empty, decoded.Body);
    }
}
