namespace ArkKeeper.Core.Saves;

/// <summary>Reads tribe metadata (id, name, owner) out of a server's .arktribe save files.</summary>
public static class TribeFileReader
{
    public static TribeInfo? Read(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            return null;
        }

        var data = File.ReadAllBytes(filePath);

        return new TribeInfo(
            Id: ArkPropertyReader.GetInt32(data, "TribeID"),
            Name: ArkPropertyReader.GetString(data, "TribeName"),
            OwnerId: GetOwnerId(data),
            FilePath: filePath,
            FileCreatedUtc: fileInfo.CreationTimeUtc,
            FileUpdatedUtc: fileInfo.LastWriteTimeUtc);
    }

    public static Task<TribeInfo?> ReadAsync(string filePath, CancellationToken cancellationToken = default) =>
        Task.Run(() => Read(filePath), cancellationToken);

    /// <summary>Scans <paramref name="directory"/> for *.arktribe files and reads each one.
    /// Files that fail to parse are skipped rather than throwing.</summary>
    public static IReadOnlyList<TribeInfo> ReadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return Array.Empty<TribeInfo>();
        }

        var tribes = new List<TribeInfo>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.arktribe"))
        {
            TribeInfo? tribe;
            try
            {
                tribe = Read(file);
            }
            catch (IOException)
            {
                // Same reasoning as PlayerFileReader.ReadDirectory: this reads a running server's
                // own save directory, and the server can have this exact file open for exclusive
                // write at the moment we try to read it — one locked file shouldn't hide every
                // other tribe too.
                continue;
            }

            if (tribe is not null)
            {
                tribes.Add(tribe);
            }
        }

        return tribes;
    }

    private static uint? GetOwnerId(ReadOnlySpan<byte> data)
    {
        var ownerId = ArkPropertyReader.GetUInt32(data, "OwnerPlayerDataID");
        return ownerId == 0 ? null : ownerId;
    }
}
