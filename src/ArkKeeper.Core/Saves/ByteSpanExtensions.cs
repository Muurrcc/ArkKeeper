namespace ArkKeeper.Core.Saves;

internal static class ByteSpanExtensions
{
    /// <summary>Finds the first occurrence of <paramref name="pattern"/> in <paramref name="data"/>
    /// at or after <paramref name="offset"/>. Returns -1 if not found, including when
    /// <paramref name="offset"/> itself is negative (a prior failed search feeding this one).</summary>
    public static int LocateFirst(this ReadOnlySpan<byte> data, ReadOnlySpan<byte> pattern, int offset = 0)
    {
        if (offset < 0 || offset > data.Length || pattern.IsEmpty || data.IsEmpty)
        {
            return -1;
        }

        var found = data[offset..].IndexOf(pattern);
        return found < 0 ? -1 : found + offset;
    }
}
