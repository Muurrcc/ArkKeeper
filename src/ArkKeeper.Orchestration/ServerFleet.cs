using System.Collections.Concurrent;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Discord;

namespace ArkKeeper.Orchestration;

/// <summary>Owns one <see cref="ManagedServer"/> per profile — the piece that lets ArkKeeper run
/// more than one server at a time, which nothing before this tracked.</summary>
public sealed class ServerFleet : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, ManagedServer> _servers = new();
    private readonly DiscordWebhookNotifier? _notifier;

    public ServerFleet(DiscordWebhookNotifier? notifier = null)
    {
        _notifier = notifier;
    }

    public IReadOnlyCollection<ManagedServer> Servers => _servers.Values.ToArray();

    /// <summary>Gets the managed server for this profile, creating it (not starting it) if it
    /// doesn't exist yet.</summary>
    public ManagedServer GetOrAdd(ServerProfile profile) =>
        _servers.GetOrAdd(profile.ProfileId, _ => new ManagedServer(profile, _notifier));

    public ManagedServer? Find(Guid profileId) => _servers.GetValueOrDefault(profileId);

    public async Task RemoveAsync(Guid profileId)
    {
        if (_servers.TryRemove(profileId, out var server))
        {
            await server.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var server in _servers.Values)
        {
            await server.DisposeAsync();
        }
        _servers.Clear();
    }
}
