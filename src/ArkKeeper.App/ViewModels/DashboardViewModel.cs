using System.Collections.ObjectModel;
using System.Linq;
using ArkKeeper.Core.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArkKeeper.App.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    public DashboardViewModel(ObservableCollection<ServerProfile> profiles)
    {
        Profiles = profiles;
        Profiles.CollectionChanged += (_, _) => RefreshSummary();
        RefreshSummary();
    }

    public ObservableCollection<ServerProfile> Profiles { get; }

    [ObservableProperty]
    public partial int ServerCount { get; set; }

    [ObservableProperty]
    public partial int TotalCapacity { get; set; }

    private void RefreshSummary()
    {
        ServerCount = Profiles.Count;
        TotalCapacity = Profiles.Sum(p => p.MaxPlayers);
    }
}
