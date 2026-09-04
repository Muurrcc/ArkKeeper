using System.Collections.Concurrent;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Discord;

namespace ArkKeeper.Orchestration;

/// <summary>Owns one <see cref="ManagedServer"/> per profile — the piece that lets ArkKeeper run
/// more than one server at a time, which nothing before this tracked.</summary>
public sealed class ServerFleet : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, ManagedServer> _servers = new();

    public ServerFleet(DiscordWebhookNotifier? notifier = null)
    {
        Notifier = notifier;
    }

    /// <summary>Mutable rather than a constructor-only value: the webhook URL it's built from
    /// lives in app settings, which load asynchronously after the fleet itself is constructed by
    /// DI — this lets the caller assign it once settings are ready, and any profile tracked from
    /// that point on picks it up. Servers already created before an assignment keep whatever
    /// notifier they were built with (not retroactively updated).</summary>
    public DiscordWebhookNotifier? Notifier { get; set; }

    public IReadOnlyCollection<ManagedServer> Servers => _servers.Values.ToArray();

    /// <summary>Gets the managed server for this profile, creating it (not starting it) if it
    /// doesn't exist yet.</summary>
    public ManagedServer GetOrAdd(ServerProfile profile) =>
        _servers.GetOrAdd(profile.ProfileId, _ => new ManagedServer(profile, Notifier));

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
