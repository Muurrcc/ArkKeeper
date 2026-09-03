using System.Buffers.Binary;
using System.Text;

namespace ArkKeeper.Core.Saves;

/// <summary>
/// Reads named properties out of ARK's Unreal-Engine tagged-property binary format
/// (used by .arktribe, .arkprofile, and map save files), by searching for the property's
/// name and type as raw ASCII text rather than doing a full structural parse.
///
/// Ported from the original ARK Server Manager's ArkData/Helpers.cs (GPL-3.0) — the exact
/// same byte-offset scheme that tool used in production for years. The "+9"/"+12"/"+13"
/// offsets below correspond to the property tag's null terminator, Size and ArrayIndex
/// fields (and, for strings, the value's own embedded length prefix and unicode flag).
/// </summary>
internal static class ArkPropertyReader
{
    public static int GetInt32(ReadOnlySpan<byte> data, string propertyName) =>
        TryLocateFixedValue(data, propertyName, "IntProperty", 4, out var valueOffset)
            ? BinaryPrimitives.ReadInt32LittleEndian(data[valueOffset..])
            : -1;

    public static uint GetUInt32(ReadOnlySpan<byte> data, string propertyName) =>
        TryLocateFixedValue(data, propertyName, "UInt32Property", 4, out var valueOffset)
            ? BinaryPrimitives.ReadUInt32LittleEndian(data[valueOffset..])
            : 0;

    public static ushort GetUInt16(ReadOnlySpan<byte> data, string propertyName) =>
        TryLocateFixedValue(data, propertyName, "UInt16Property", 2, out var valueOffset)
            ? BinaryPrimitives.ReadUInt16LittleEndian(data[valueOffset..])
            : (ushort)0;

    public static ulong GetUInt64(ReadOnlySpan<byte> data, string propertyName) =>
        TryLocateFixedValue(data, propertyName, "UInt64Property", 8, out var valueOffset)
            ? BinaryPrimitives.ReadUInt64LittleEndian(data[valueOffset..])
            : 0;

    /// <summary>Reads a fixed-length ASCII/Latin1 field found directly <paramref name="offsetAfterName"/>
    /// bytes after <paramref name="propertyName"/>'s own text — used for struct-typed properties
    /// (like the player's SteamID) that don't have a separate "XxxProperty" type marker to search
    /// for, unlike the scalar properties the other Get* methods handle.</summary>
    public static string GetFixedString(ReadOnlySpan<byte> data, string propertyName, int offsetAfterName, int length)
    {
        var nameOffset = data.LocateFirst(Encoding.ASCII.GetBytes(propertyName));
        if (nameOffset < 0)
        {
            return string.Empty;
        }

        var valueStart = nameOffset + propertyName.Length + offsetAfterName;
        if (valueStart < 0 || valueStart + length > data.Length)
        {
            return string.Empty;
        }

        return Encoding.Latin1.GetString(data.Slice(valueStart, length)).TrimEnd('\0');
    }

    public static string GetString(ReadOnlySpan<byte> data, string propertyName)
    {
        ReadOnlySpan<byte> typeBytes = "StrProperty"u8;
        var nameOffset = data.LocateFirst(Encoding.ASCII.GetBytes(propertyName));
        var typeOffset = data.LocateFirst(typeBytes, nameOffset);

        if (typeOffset < 0)
        {
            return string.Empty;
        }

        var flagOffset = typeOffset + typeBytes.Length + 12;
        var declaredSizeOffset = typeOffset + typeBytes.Length + 1;
        var valueStart = typeOffset + typeBytes.Length + 13;

        if (flagOffset >= data.Length || valueStart > data.Length)
        {
            return string.Empty;
        }

        var isUnicode = data[flagOffset] == byte.MaxValue;
        var declaredSize = data[declaredSizeOffset];
        var length = declaredSize - (isUnicode ? 6 : 5);

        if (length <= 0 || valueStart + length > data.Length)
        {
            return string.Empty;
        }

        var bytes = data.Slice(valueStart, length);
        return isUnicode ? Encoding.Unicode.GetString(bytes) : Encoding.Latin1.GetString(bytes);
    }

    private static bool TryLocateFixedValue(ReadOnlySpan<byte> data, string propertyName, string propertyType, int valueSize, out int valueOffset)
    {
        var typeBytes = Encoding.ASCII.GetBytes(propertyType);
        var nameOffset = data.LocateFirst(Encoding.ASCII.GetBytes(propertyName));
        var typeOffset = data.LocateFirst(typeBytes, nameOffset);

        if (typeOffset < 0)
        {
            valueOffset = -1;
            return false;
        }

        valueOffset = typeOffset + typeBytes.Length + 9;
        return valueOffset >= 0 && valueOffset + valueSize <= data.Length;
    }
}
