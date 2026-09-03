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

        ServersPage = new ServersViewModel(Profiles, fleet, _profileStore, OpenEditor);
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
        foreach (var profile in await _profileStore.LoadAllAsync())
        {
            Profiles.Add(profile);
        }
    }

    /// <summary>Opens the create/edit form, swapping it into the content area. Passed down to
    /// <see cref="ServersViewModel"/> as a delegate rather than having it own navigation itself —
    /// only MainViewModel knows about <see cref="SelectedPage"/>.</summary>
    private void OpenEditor(ServerProfile? existing) =>
        SelectedPage = new ProfileEditorViewModel(existing, Profiles, _profileStore, () => SelectedPage = ServersPage);
}
