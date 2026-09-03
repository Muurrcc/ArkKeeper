using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Orchestration;

namespace ArkKeeper.App.ViewModels;

public sealed class ServersViewModel : ViewModelBase
{
    private readonly ServerFleet _fleet;

    public ServersViewModel(ObservableCollection<ServerProfile> profiles, ServerFleet fleet)
    {
        _fleet = fleet;

        foreach (var profile in profiles)
        {
            Servers.Add(new ServerRowViewModel(_fleet.GetOrAdd(profile)));
        }

        profiles.CollectionChanged += OnProfilesChanged;
    }

    public ObservableCollection<ServerRowViewModel> Servers { get; } = new();

    /// <summary>Re-reads each server's live process status. Called on a poll timer since
    /// <see cref="ManagedServer"/> has no status-changed event to subscribe to.</summary>
    public void RefreshAll()
    {
        foreach (var server in Servers)
        {
            server.Refresh();
        }
    }

    private void OnProfilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (ServerProfile profile in e.NewItems)
            {
                Servers.Add(new ServerRowViewModel(_fleet.GetOrAdd(profile)));
            }
        }

        if (e.OldItems is not null)
        {
            foreach (ServerProfile profile in e.OldItems)
            {
                var row = Servers.FirstOrDefault(s => s.Profile.ProfileId == profile.ProfileId);
                if (row is not null)
                {
                    Servers.Remove(row);
                }
            }
        }
    }
}
