using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Orchestration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

public sealed partial class ServersViewModel : ViewModelBase
{
    private readonly ServerFleet _fleet;
    private readonly ProfileStore _profileStore;
    private readonly ObservableCollection<ServerProfile> _profiles;
    private readonly Action<ServerProfile?> _openEditor;
    private readonly Action<ServerRowViewModel> _openConsole;
    private readonly Action<ServerRowViewModel> _openPlayers;
    private readonly Action<ServerRowViewModel> _openBackups;
    private readonly Action<ServerRowViewModel> _openScheduler;

    public ServersViewModel(
        ObservableCollection<ServerProfile> profiles,
        ServerFleet fleet,
        ProfileStore profileStore,
        Action<ServerProfile?> openEditor,
        Action<ServerRowViewModel> openConsole,
        Action<ServerRowViewModel> openPlayers,
        Action<ServerRowViewModel> openBackups,
        Action<ServerRowViewModel> openScheduler)
    {
        _profiles = profiles;
        _fleet = fleet;
        _profileStore = profileStore;
        _openEditor = openEditor;
        _openConsole = openConsole;
        _openPlayers = openPlayers;
        _openBackups = openBackups;
        _openScheduler = openScheduler;

        foreach (var profile in profiles)
        {
            AddRow(profile);
        }

        profiles.CollectionChanged += OnProfilesChanged;
        IsEmpty = Servers.Count == 0;
    }

    public ObservableCollection<ServerRowViewModel> Servers { get; } = new();

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    /// <summary>Re-reads each server's live process status. Called on a poll timer since
    /// <see cref="ManagedServer"/> has no status-changed event to subscribe to.</summary>
    public void RefreshAll()
    {
        foreach (var server in Servers)
        {
            server.Refresh();
        }
    }

    /// <summary>Runs each server's due scheduled tasks (a no-op per row while it isn't running).
    /// Called on the same poll timer as <see cref="RefreshAll"/>.</summary>
    public async Task RunDueScheduledTasksForAllAsync()
    {
        foreach (var server in Servers)
        {
            await server.RunDueScheduledTasksAsync();
        }
    }

    [RelayCommand]
    private void AddServer() => _openEditor(null);

    [RelayCommand]
    private void Edit(ServerRowViewModel row) => _openEditor(row.Profile);

    [RelayCommand]
    private void Console(ServerRowViewModel row) => _openConsole(row);

    [RelayCommand]
    private void Players(ServerRowViewModel row) => _openPlayers(row);

    [RelayCommand]
    private void Backups(ServerRowViewModel row) => _openBackups(row);

    [RelayCommand]
    private void Scheduler(ServerRowViewModel row) => _openScheduler(row);

    [RelayCommand]
    private async Task DeleteAsync(ServerRowViewModel row)
    {
        if (row.IsRunning)
        {
            row.ErrorMessage = "Stop the server before deleting its profile.";
            return;
        }

        await _fleet.RemoveAsync(row.Profile.ProfileId);
        _profileStore.Delete(row.Profile.ProfileId);
        _profiles.Remove(row.Profile);
    }

    private void OnProfilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (ServerProfile profile in e.NewItems)
            {
                AddRow(profile);
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

        IsEmpty = Servers.Count == 0;
    }

    private void AddRow(ServerProfile profile)
    {
        var row = new ServerRowViewModel(_fleet.GetOrAdd(profile));
        Servers.Add(row);
        _ = row.EnsureSchedulerLoadedAsync();
    }
}
