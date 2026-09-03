using ArkKeeper.Core.Profiles;
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
    private readonly ManagedServer _server;

    public ServerRowViewModel(ManagedServer server)
    {
        _server = server;
        Status = _server.Status;
    }

    public ServerProfile Profile => _server.Profile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyCanExecuteChangedFor(nameof(KillCommand))]
    public partial ServerStatus Status { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool IsRunning => Status == ServerStatus.Running;

    public void Refresh() => Status = _server.Status;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        ErrorMessage = null;
        try
        {
            _server.Start();
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
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        Refresh();
    }

    private bool CanStop() => Status == ServerStatus.Running;
}
