using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArkKeeper.App.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    public DashboardViewModel(ObservableCollection<ServerRowViewModel> servers)
    {
        Servers = servers;
        Servers.CollectionChanged += (_, _) => RefreshSummary();
        RefreshSummary();
    }

    public ObservableCollection<ServerRowViewModel> Servers { get; }

    [ObservableProperty]
    public partial int ServerCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyRunning))]
    public partial int RunningCount { get; set; }

    [ObservableProperty]
    public partial int TotalCapacity { get; set; }

    /// <summary>Drives the "Live" pulse animation on the RUNNING NOW icon — only worth animating
    /// when there's actually something running to draw attention to.</summary>
    public bool IsAnyRunning => RunningCount > 0;

    /// <summary>Re-derives the running count from each row's live status. Called alongside
    /// <see cref="ServersViewModel.RefreshAll"/> on the same poll timer.</summary>
    public void RefreshSummary()
    {
        ServerCount = Servers.Count;
        RunningCount = Servers.Count(s => s.IsRunning);
        TotalCapacity = Servers.Sum(s => s.Profile.MaxPlayers);
    }
}
