using ArkKeeper.App.Services;
using ArkKeeper.Core.Players;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Scheduling;
using ArkKeeper.Core.Servers;
using ArkKeeper.Orchestration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

/// <summary>Binds one profile's real <see cref="ManagedServer"/> to the UI: live status plus
/// start/stop/kill commands. <see cref="Refresh"/> is polled rather than event-driven because
/// <see cref="ManagedServer"/> exposes <c>Status</c> as a plain property, not a change event.</summary>
public partial class ServerRowViewModel : ViewModelBase
{
    /// <summary>1 minute of history at the app's 2s poll interval — long enough for a sparkline
    /// trend to read as a trend, short enough to stay a "right now" view, not a history log.</summary>
    private const int ResourceHistoryLength = 30;

    private readonly ManagedServer _server;
    private readonly SchedulerStore _schedulerStore;
    private readonly ActivityLog _activityLog;
    private bool _schedulerLoaded;

    public ServerRowViewModel(ManagedServer server, ActivityLog activityLog)
    {
        _server = server;
        _activityLog = activityLog;
        Status = _server.Status;

        var filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ArkKeeper", "Schedules", $"{server.Profile.ProfileId}.json");
        _schedulerStore = new SchedulerStore(filePath);
    }

    public ServerProfile Profile => _server.Profile;

    /// <summary>Owned by this row (not the scheduler page) so scheduled tasks keep running via
    /// <see cref="RunDueScheduledTasksAsync"/> in the background poll even while the user is
    /// looking at some other page — matches the whole point of a scheduler.</summary>
    public SchedulerRunner Scheduler { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyCanExecuteChangedFor(nameof(KillCommand))]
    public partial ServerStatus Status { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool IsRunning => Status == ServerStatus.Running;

    [ObservableProperty]
    public partial double CpuPercent { get; set; }

    [ObservableProperty]
    public partial double RamGigabytes { get; set; }

    /// <summary>Rolling window of recent CPU% samples — oldest first, so it plots left-to-right as
    /// a normal time-series chart. Cleared whenever the server isn't running, so a stopped
    /// server's card doesn't keep showing a frozen trend from before it stopped.</summary>
    public Queue<double> CpuHistory { get; } = new();

    public Queue<double> RamHistory { get; } = new();

    public void Refresh() => Status = _server.Status;

    /// <summary>Samples this server's real CPU/RAM usage and appends to its rolling history — a
    /// no-op (and clears history) when it isn't running. Called from the same poll tick as
    /// <see cref="Refresh"/>.</summary>
    public void SampleResourceUsage()
    {
        var sample = _server.SampleResourceUsage();
        if (sample is not { } value)
        {
            CpuPercent = 0;
            RamGigabytes = 0;
            CpuHistory.Clear();
            RamHistory.Clear();
            return;
        }

        CpuPercent = value.CpuPercent;
        RamGigabytes = value.WorkingSetGigabytes;

        CpuHistory.Enqueue(value.CpuPercent);
        RamHistory.Enqueue(value.WorkingSetGigabytes);
        while (CpuHistory.Count > ResourceHistoryLength)
        {
            CpuHistory.Dequeue();
        }
        while (RamHistory.Count > ResourceHistoryLength)
        {
            RamHistory.Dequeue();
        }
    }

    /// <summary>Exposes RCON without leaking <see cref="ManagedServer"/> itself to the UI layer
    /// — used by <see cref="RconConsoleViewModel"/>.</summary>
    public Task<string> SendRconCommandAsync(string command, CancellationToken cancellationToken = default) =>
        _server.SendRconCommandAsync(command, cancellationToken);

    /// <summary>Exposes the typed player-management RCON commands — used by
    /// <see cref="PlayersViewModel"/>.</summary>
    public Task<IReadOnlyList<ConnectedPlayer>> GetPlayersAsync(CancellationToken cancellationToken = default) =>
        _server.GetPlayersAsync(cancellationToken);

    public Task<string> KickPlayerAsync(string steamId, CancellationToken cancellationToken = default) =>
        _server.KickPlayerAsync(steamId, cancellationToken);

    public Task<string> BanPlayerAsync(string steamId, CancellationToken cancellationToken = default) =>
        _server.BanPlayerAsync(steamId, cancellationToken);

    public Task<string> UnbanPlayerAsync(string steamId, CancellationToken cancellationToken = default) =>
        _server.UnbanPlayerAsync(steamId, cancellationToken);

    /// <summary>Loads this server's saved schedule the first time it's needed — called eagerly
    /// once per row by <see cref="ServersViewModel"/> so background execution works even if the
    /// user never opens the Scheduler page, and safe to call again (a no-op after the first).</summary>
    public async Task EnsureSchedulerLoadedAsync()
    {
        if (_schedulerLoaded)
        {
            return;
        }

        _schedulerLoaded = true;
        Scheduler.AddRange(await _schedulerStore.LoadAsync());
    }

    public Task SaveScheduleAsync() => _schedulerStore.SaveAsync(Scheduler.Tasks);

    /// <summary>Runs whatever's due, swallowing failures — called on a background poll tick, not
    /// a user action, so there's no error UI to surface it to and a transient RCON hiccup
    /// shouldn't spam anything. <see cref="SchedulerRunner.RunDueTasksAsync"/> itself already logs
    /// failures.</summary>
    public async Task RunDueScheduledTasksAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            await _server.RunDueScheduledTasksAsync(Scheduler, DateTimeOffset.UtcNow);
        }
        catch
        {
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        ErrorMessage = null;
        try
        {
            _server.Start();
            _activityLog.Add($"{Profile.ProfileName} started", ActivityKind.Server);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        Refresh();
    }

    private bool CanStart() => Status != ServerStatus.Running;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        ErrorMessage = null;
        try
        {
            await _server.StopAsync(TimeSpan.FromSeconds(30));
            _activityLog.Add($"{Profile.ProfileName} stopped", ActivityKind.Server);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Kill()
    {
        ErrorMessage = null;
        try
        {
            _server.Kill();
            _activityLog.Add($"{Profile.ProfileName} killed", ActivityKind.Server);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        Refresh();
    }

    private bool CanStop() => Status == ServerStatus.Running;
}
