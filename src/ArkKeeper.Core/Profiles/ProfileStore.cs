using System.Collections.Concurrent;
using System.Text.Json;

namespace ArkKeeper.Core.Profiles;

/// <summary>
/// Persists <see cref="ServerProfile"/> instances as JSON — one file per profile —
/// under a given directory. This is ArkKeeper's own storage format; exporting to the
/// actual game .ini files is a separate step via <see cref="ServerProfile.ToGameUserSettings"/>.
///
/// Serializes through <see cref="ServerProfileData"/> rather than <see cref="ServerProfile"/>
/// directly — see that type's doc comment for why (a System.Text.Json/CommunityToolkit.Mvvm
/// source-generator interop bug that silently drops data if ServerProfile is serialized directly).
/// </summary>
public sealed class ProfileStore
{
    // File.Create defaults to FileShare.None, so two overlapping SaveAsync calls for the same
    // profile (e.g. rapid-fire Add/Remove on the Mods page, which has no busy-guard unlike
    // DownloadAllAsync) would race to open the same path for exclusive write and one would throw
    // IOException. Keyed per profile rather than one global lock, so unrelated profiles still
    // save concurrently.
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _saveLocks = new();

    public ProfileStore(string directory)
    {
        Directory = directory;
    }

    public string Directory { get; }

    public async Task<IReadOnlyList<ServerProfile>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        if (!System.IO.Directory.Exists(Directory))
        {
            return Array.Empty<ServerProfile>();
        }

        var profiles = new List<ServerProfile>();
        foreach (var file in System.IO.Directory.EnumerateFiles(Directory, "*.json"))
        {
            await using var stream = File.OpenRead(file);
            var data = await JsonSerializer.DeserializeAsync(stream, ServerProfileDataJsonContext.Default.ServerProfileData, cancellationToken);
            if (data is not null)
            {
                profiles.Add(ServerProfile.FromData(data));
            }
        }

        return profiles;
    }

    public async Task SaveAsync(ServerProfile profile, CancellationToken cancellationToken = default)
    {
        var gate = _saveLocks.GetOrAdd(profile.ProfileId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            var path = PathFor(profile.ProfileId);
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, profile.ToData(), ServerProfileDataJsonContext.Default.ServerProfileData, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Delete(Guid profileId)
    {
        var path = PathFor(profileId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string PathFor(Guid profileId) => Path.Combine(Directory, $"{profileId}.json");
}
