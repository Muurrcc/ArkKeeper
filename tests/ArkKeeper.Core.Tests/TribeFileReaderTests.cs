using System.Buffers.Binary;
using System.Text;
using ArkKeeper.Core.Saves;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class TribeFileReaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ArkKeeperTribeTests_" + Guid.NewGuid());

    public TribeFileReaderTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void Read_ParsesIdNameAndOwnerFromSyntheticTribeFile()
    {
        var bytes = BuildIntProperty("TribeID", "IntProperty", 4242)
            .Concat(BuildStrProperty("TribeName", "The Islanders"))
            .Concat(BuildUInt32Property("OwnerPlayerDataID", 123456789))
            .ToArray();

        var path = Path.Combine(_directory, "4242.arktribe");
        File.WriteAllBytes(path, bytes);

        var tribe = TribeFileReader.Read(path);

        Assert.NotNull(tribe);
        Assert.Equal(4242, tribe!.Id);
        Assert.Equal("The Islanders", tribe.Name);
        Assert.Equal(123456789u, tribe.OwnerId);
    }

    [Fact]
    public void Read_UnicodeTribeName_DecodesCorrectly()
    {
        var bytes = BuildStrProperty("TribeName", "Nördische Wölfe", unicode: true);

        var path = Path.Combine(_directory, "unicode.arktribe");
        File.WriteAllBytes(path, bytes);

        var tribe = TribeFileReader.Read(path);

        Assert.Equal("Nördische Wölfe", tribe!.Name);
    }

    [Fact]
    public void Read_MissingFile_ReturnsNull()
    {
        var tribe = TribeFileReader.Read(Path.Combine(_directory, "does-not-exist.arktribe"));

        Assert.Null(tribe);
    }

    [Fact]
    public void ReadDirectory_ReadsAllArktribeFilesAndSkipsOthers()
    {
        File.WriteAllBytes(Path.Combine(_directory, "1.arktribe"), BuildIntProperty("TribeID", "IntProperty", 1));
        File.WriteAllBytes(Path.Combine(_directory, "2.arktribe"), BuildIntProperty("TribeID", "IntProperty", 2));
        File.WriteAllText(Path.Combine(_directory, "notes.txt"), "ignore me");

        var tribes = TribeFileReader.ReadDirectory(_directory);

        Assert.Equal(2, tribes.Count);
        Assert.Equal(new[] { 1, 2 }, tribes.Select(t => t.Id).OrderBy(id => id));
    }

    private static byte[] BuildIntProperty(string name, string typeName, int value)
    {
        var filler = new byte[9];
        var valueBytes = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(valueBytes, value);

        return Concat(Encoding.ASCII.GetBytes(name), Encoding.ASCII.GetBytes(typeName), filler, valueBytes);
    }

    private static byte[] BuildUInt32Property(string name, uint value)
    {
        var filler = new byte[9];
        var valueBytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(valueBytes, value);

        return Concat(Encoding.ASCII.GetBytes(name), Encoding.ASCII.GetBytes("UInt32Property"), filler, valueBytes);
    }

    private static byte[] BuildStrProperty(string name, string value, bool unicode = false)
    {
        var valueBytes = unicode ? Encoding.Unicode.GetBytes(value) : Encoding.Latin1.GetBytes(value);
        var declaredSize = (byte)(valueBytes.Length + (unicode ? 6 : 5));
        var flag = unicode ? byte.MaxValue : (byte)0;

        return Concat(
            Encoding.ASCII.GetBytes(name),
            Encoding.ASCII.GetBytes("StrProperty"),
            new byte[1],                  // offset +11: unused filler
            new[] { declaredSize },        // offset +12: declared size
            new byte[10],                  // offset +13..+22: unused filler
            new[] { flag },                 // offset +23: unicode flag
            valueBytes);                    // offset +24: string bytes
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
