using System.Buffers.Binary;
using System.Text;
using ArkKeeper.Core.Saves;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class PlayerFileReaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ArkKeeperPlayerTests_" + Guid.NewGuid());

    public PlayerFileReaderTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void Read_ParsesIdNameTribeAndLevelFromSyntheticProfile()
    {
        var bytes = BuildUInt64Property("PlayerDataID", 123456789012345)
            .Concat(BuildFixedString("UniqueNetIdRepl", 9, "76561198012345678"))
            .Concat(BuildStrProperty("PlayerName", "SteamNick"))
            .Concat(BuildStrProperty("PlayerCharacterName", "Survivor Joe"))
            .Concat(BuildIntProperty("TribeID", 42))
            .Concat(BuildUInt16Property("CharacterStatusComponent_ExtraCharacterLevel", 104))
            .ToArray();

        var path = Path.Combine(_directory, "123.arkprofile");
        File.WriteAllBytes(path, bytes);

        var player = PlayerFileReader.Read(path);

        Assert.NotNull(player);
        Assert.Equal(123456789012345ul, player!.PlayerDataId);
        Assert.Equal("76561198012345678", player.SteamId);
        Assert.Equal("SteamNick", player.SteamName);
        Assert.Equal("Survivor Joe", player.CharacterName);
        Assert.Equal(42, player.TribeId);
        Assert.Equal(105, player.Level); // 1 + ExtraCharacterLevel
    }

    [Fact]
    public void Read_WithNoTribeId_ReturnsNullTribeId()
    {
        var bytes = BuildStrProperty("PlayerName", "Solo Player");

        var path = Path.Combine(_directory, "solo.arkprofile");
        File.WriteAllBytes(path, bytes);

        var player = PlayerFileReader.Read(path);

        Assert.Null(player!.TribeId);
    }

    [Fact]
    public void Read_MissingFile_ReturnsNull()
    {
        var player = PlayerFileReader.Read(Path.Combine(_directory, "does-not-exist.arkprofile"));

        Assert.Null(player);
    }

    [Fact]
    public void ReadDirectory_ReadsAllArkprofileFilesAndSkipsOthers()
    {
        File.WriteAllBytes(Path.Combine(_directory, "1.arkprofile"), BuildStrProperty("PlayerName", "A"));
        File.WriteAllBytes(Path.Combine(_directory, "2.arkprofile"), BuildStrProperty("PlayerName", "B"));
        File.WriteAllText(Path.Combine(_directory, "notes.txt"), "ignore me");

        var players = PlayerFileReader.ReadDirectory(_directory);

        Assert.Equal(2, players.Count);
        Assert.Equal(new[] { "A", "B" }, players.Select(p => p.SteamName).OrderBy(n => n));
    }

    private static byte[] BuildIntProperty(string name, int value)
    {
        var valueBytes = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(valueBytes, value);
        return Concat(Encoding.ASCII.GetBytes(name), Encoding.ASCII.GetBytes("IntProperty"), new byte[9], valueBytes);
    }

    private static byte[] BuildUInt16Property(string name, ushort value)
    {
        var valueBytes = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(valueBytes, value);
        return Concat(Encoding.ASCII.GetBytes(name), Encoding.ASCII.GetBytes("UInt16Property"), new byte[9], valueBytes);
    }

    private static byte[] BuildUInt64Property(string name, ulong value)
    {
        var valueBytes = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(valueBytes, value);
        return Concat(Encoding.ASCII.GetBytes(name), Encoding.ASCII.GetBytes("UInt64Property"), new byte[9], valueBytes);
    }

    private static byte[] BuildStrProperty(string name, string value)
    {
        var valueBytes = Encoding.Latin1.GetBytes(value);
        var declaredSize = (byte)(valueBytes.Length + 5);

        return Concat(
            Encoding.ASCII.GetBytes(name),
            Encoding.ASCII.GetBytes("StrProperty"),
            new byte[1],
            new[] { declaredSize },
            new byte[10],
            new byte[] { 0 },
            valueBytes);
    }

    private static byte[] BuildFixedString(string name, int offsetAfterName, string value)
    {
        return Concat(Encoding.ASCII.GetBytes(name), new byte[offsetAfterName], Encoding.Latin1.GetBytes(value));
    }

    private static byte[] Concat(params byte[][] segments) => segments.SelectMany(s => s).ToArray();

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
