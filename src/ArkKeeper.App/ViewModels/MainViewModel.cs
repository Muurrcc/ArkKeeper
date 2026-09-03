using System.Collections.ObjectModel;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Orchestration;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArkKeeper.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ProfileStore _profileStore;
    private readonly DispatcherTimer _statusPollTimer;

    /// <summary>Design-time/previewer-only constructor — the XAML compiler needs a parameterless
    /// constructor for `&lt;vm:MainViewModel /&gt;` in MainWindow.axaml's Design.DataContext.
    /// The real app always goes through the DI constructor below (see Program.cs).</summary>
    public MainViewModel()
        : this(new ProfileStore(Path.Combine(Path.GetTempPath(), "ArkKeeperDesign")), new ServerFleet())
    {
    }

    public MainViewModel(ProfileStore profileStore, ServerFleet fleet)
    {
        _profileStore = profileStore;

        ServersPage = new ServersViewModel(Profiles, fleet);
        DashboardPage = new DashboardViewModel(ServersPage.Servers);
        SettingsPage = new SettingsViewModel();

        SelectedPage = DashboardPage;

        _statusPollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _statusPollTimer.Tick += (_, _) =>
        {
            ServersPage.RefreshAll();
            DashboardPage.RefreshSummary();
        };
        _statusPollTimer.Start();
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
