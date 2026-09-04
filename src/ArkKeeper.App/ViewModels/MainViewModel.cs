using System.Collections.ObjectModel;
using System.Net.Http;
using ArkKeeper.App.Services;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Settings;
using ArkKeeper.Discord;
using ArkKeeper.Orchestration;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArkKeeper.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ProfileStore _profileStore;
    private readonly ServerFleet _fleet;
    private readonly DispatcherTimer _statusPollTimer;
    private bool _isRunningScheduledTasksCheck;

    /// <summary>Design-time/previewer-only constructor — the XAML compiler needs a parameterless
    /// constructor for `&lt;vm:MainViewModel /&gt;` in MainWindow.axaml's Design.DataContext.
    /// The real app always goes through the DI constructor below (see Program.cs).</summary>
    public MainViewModel()
        : this(
            new ProfileStore(Path.Combine(Path.GetTempPath(), "ArkKeeperDesign")),
            new ServerFleet(),
            new AppSettingsStore(Path.Combine(Path.GetTempPath(), "ArkKeeperDesign", "settings.json")))
    {
    }

    public MainViewModel(ProfileStore profileStore, ServerFleet fleet, AppSettingsStore appSettingsStore)
    {
        _profileStore = profileStore;
        _fleet = fleet;

        ServersPage = new ServersViewModel(Profiles, fleet, _profileStore, Activity, OpenEditor, OpenConsole, OpenPlayers, OpenBackups, OpenScheduler, OpenMods);
        DashboardPage = new DashboardViewModel(ServersPage.Servers, Activity);
        SettingsPage = new SettingsViewModel(appSettingsStore);

        SelectedPage = DashboardPage;

        _statusPollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _statusPollTimer.Tick += async (_, _) =>
        {
            ServersPage.RefreshAll();
            DashboardPage.RefreshSummary();

            // Guarded against overlap: a slow/unreachable RCON connection could otherwise let
            // ticks pile up faster than they resolve.
            if (_isRunningScheduledTasksCheck)
            {
                return;
            }

            _isRunningScheduledTasksCheck = true;
            try
            {
                await ServersPage.RunDueScheduledTasksForAllAsync();
            }
            finally
            {
                _isRunningScheduledTasksCheck = false;
            }
        };
        _statusPollTimer.Start();
    }

    public ObservableCollection<ServerProfile> Profiles { get; } = new();

    /// <summary>Shared across every page that can produce a real event (server start/stop,
    /// backups) — owned here rather than per-page so the Dashboard's Activity card reflects
    /// everything regardless of which page is actually open when it happens.</summary>
    public ActivityLog Activity { get; } = new();

    public DashboardViewModel DashboardPage { get; }

    public ServersViewModel ServersPage { get; }

    public SettingsViewModel SettingsPage { get; }

    [ObservableProperty]
    public partial ViewModelBase SelectedPage { get; set; }

    public async Task InitializeAsync()
    {
        // Settings first: applies the saved theme/accent before the window is really seen, and
        // makes sure a configured Discord webhook is wired into the fleet before any profile
        // below gets its ManagedServer created.
        await SettingsPage.InitializeAsync();
        if (!string.IsNullOrWhiteSpace(SettingsPage.DiscordWebhookUrl))
        {
            _fleet.Notifier = new DiscordWebhookNotifier(new HttpClient(), SettingsPage.DiscordWebhookUrl);
        }

        foreach (var profile in await _profileStore.LoadAllAsync())
        {
            Profiles.Add(profile);
        }
    }

    /// <summary>Opens the create/edit form, swapping it into the content area. Passed down to
    /// <see cref="ServersViewModel"/> as a delegate rather than having it own navigation itself —
    /// only MainViewModel knows about <see cref="SelectedPage"/>.</summary>
    private void OpenEditor(ServerProfile? existing) =>
        SelectedPage = new ProfileEditorViewModel(existing, Profiles, _profileStore, SettingsPage.SteamCmdDirectory, () => SelectedPage = ServersPage);

    /// <summary>Opens the RCON console for one server. Same navigation pattern as
    /// <see cref="OpenEditor"/>.</summary>
    private void OpenConsole(ServerRowViewModel row) =>
        SelectedPage = new RconConsoleViewModel(row, () => SelectedPage = ServersPage);

    /// <summary>Opens the players/tribes page for one server. Same navigation pattern as
    /// <see cref="OpenEditor"/>.</summary>
    private void OpenPlayers(ServerRowViewModel row) =>
        SelectedPage = new PlayersViewModel(row, () => SelectedPage = ServersPage);

    /// <summary>Opens the world save/restore page for one server. Same navigation pattern as
    /// <see cref="OpenEditor"/>.</summary>
    private void OpenBackups(ServerRowViewModel row) =>
        SelectedPage = new BackupsViewModel(row, Activity, () => SelectedPage = ServersPage);

    /// <summary>Opens the scheduled-tasks page for one server. Same navigation pattern as
    /// <see cref="OpenEditor"/>.</summary>
    private void OpenScheduler(ServerRowViewModel row) =>
        SelectedPage = new SchedulerViewModel(row, () => SelectedPage = ServersPage);

    /// <summary>Opens the mods page for one server. Same navigation pattern as
    /// <see cref="OpenEditor"/>.</summary>
    private void OpenMods(ServerRowViewModel row) =>
        SelectedPage = new ModsViewModel(row, _profileStore, SettingsPage.SteamCmdDirectory, () => SelectedPage = ServersPage);
}
