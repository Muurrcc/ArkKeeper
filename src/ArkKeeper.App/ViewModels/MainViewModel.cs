using System.Collections.ObjectModel;
using ArkKeeper.Core.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArkKeeper.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ProfileStore _profileStore;

    public MainViewModel()
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ArkKeeper",
            "profiles");
        _profileStore = new ProfileStore(dataDirectory);

        DashboardPage = new DashboardViewModel(Profiles);
        ServersPage = new ServersViewModel(Profiles);
        SettingsPage = new SettingsViewModel();

        SelectedPage = DashboardPage;
    }

    public ObservableCollection<ServerProfile> Profiles { get; } = new();

    public DashboardViewModel DashboardPage { get; }

    public ServersViewModel ServersPage { get; }

    public SettingsViewModel SettingsPage { get; }

    [ObservableProperty]
    public partial ViewModelBase SelectedPage { get; set; }

    public async Task InitializeAsync()
    {
        var loaded = await _profileStore.LoadAllAsync();

        if (loaded.Count == 0)
        {
            foreach (var sample in SampleProfiles())
            {
                Profiles.Add(sample);
            }
            return;
        }

        foreach (var profile in loaded)
        {
            Profiles.Add(profile);
        }
    }

    private static IEnumerable<ServerProfile> SampleProfiles()
    {
        yield return new ServerProfile
        {
            ProfileName = "The Island (sample)",
            SessionName = "ArkKeeper - The Island",
            Port = 7777,
            MaxPlayers = 20,
            PveMode = true,
        };
        yield return new ServerProfile
        {
            ProfileName = "Ragnarok (sample)",
            SessionName = "ArkKeeper - Ragnarok",
            Port = 7787,
            MaxPlayers = 50,
            XpMultiplier = 2.0f,
        };
    }
}
