using System.Text.Json;

namespace ArkKeeper.Core.Profiles;

/// <summary>
/// Persists <see cref="ServerProfile"/> instances as JSON — one file per profile —
/// under a given directory. This is ArkKeeper's own storage format; exporting to the
/// actual game .ini files is a separate step via <see cref="ServerProfile.ToGameUserSettings"/>.
/// </summary>
public sealed class ProfileStore
{
    // ServerProfile's settable properties come from CommunityToolkit.Mvvm's [ObservableProperty]
    // source generator. A System.Text.Json source-generated JsonSerializerContext runs against
    // the pre-expansion syntax and silently misses those generated properties (confirmed: it
    // only serialized ProfileId/ModIds, dropping everything else) — so this stays reflection-based.
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

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
            var profile = await JsonSerializer.DeserializeAsync<ServerProfile>(stream, SerializerOptions, cancellationToken);
            if (profile is not null)
            {
                profiles.Add(profile);
            }
        }

        return profiles;
    }

    public async Task SaveAsync(ServerProfile profile, CancellationToken cancellationToken = default)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var path = PathFor(profile.ProfileId);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, profile, SerializerOptions, cancellationToken);
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
