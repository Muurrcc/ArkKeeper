namespace ArkKeeper.Core.Saves;

/// <summary>Reads player metadata (Steam id/name, character name, tribe, level) out of a
/// server's .arkprofile save files. Ported from the original tool's ArkData/PlayerParser.cs.</summary>
public static class PlayerFileReader
{
    public static PlayerInfo? Read(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            return null;
        }

        var data = File.ReadAllBytes(filePath);

        return new PlayerInfo(
            PlayerDataId: ArkPropertyReader.GetUInt64(data, "PlayerDataID"),
            SteamId: ArkPropertyReader.GetFixedString(data, "UniqueNetIdRepl", offsetAfterName: 9, length: 17),
            SteamName: ArkPropertyReader.GetString(data, "PlayerName"),
            CharacterName: ArkPropertyReader.GetString(data, "PlayerCharacterName"),
            TribeId: GetTribeId(data),
            Level: GetLevel(data),
            FilePath: filePath,
            FileCreatedUtc: fileInfo.CreationTimeUtc,
            FileUpdatedUtc: fileInfo.LastWriteTimeUtc);
    }

    public static Task<PlayerInfo?> ReadAsync(string filePath, CancellationToken cancellationToken = default) =>
        Task.Run(() => Read(filePath), cancellationToken);

    /// <summary>Scans <paramref name="directory"/> for *.arkprofile files and reads each one.
    /// Files that fail to parse are skipped rather than throwing.</summary>
    public static IReadOnlyList<PlayerInfo> ReadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return Array.Empty<PlayerInfo>();
        }

        var players = new List<PlayerInfo>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.arkprofile"))
        {
            var player = Read(file);
            if (player is not null)
            {
                players.Add(player);
            }
        }

        return players;
    }

    private static int? GetTribeId(ReadOnlySpan<byte> data)
    {
        var tribeId = ArkPropertyReader.GetInt32(data, "TribeID");
        return tribeId == -1 ? null : tribeId;
    }

    private static short GetLevel(ReadOnlySpan<byte> data)
    {
        var extraLevels = ArkPropertyReader.GetUInt16(data, "CharacterStatusComponent_ExtraCharacterLevel");
        return (short)(1 + extraLevels);
    }
}
