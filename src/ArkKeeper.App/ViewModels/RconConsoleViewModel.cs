using System.Collections.ObjectModel;
using ArkKeeper.Core.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

/// <summary>A live RCON console for one server: free-form command entry plus a couple of one-click
/// shortcuts for the two most common admin actions. Each entry connects/reuses the same RCON
/// connection <see cref="ManagedServer"/> already manages (see its own locking/retry doc comment)
/// — this view model just forwards text, it owns no connection state of its own.</summary>
public partial class RconConsoleViewModel : ViewModelBase
{
    private readonly ServerRowViewModel _server;
    private readonly Action _onClose;

    public RconConsoleViewModel(ServerRowViewModel server, Action onClose)
    {
        _server = server;
        _onClose = onClose;
    }

    public ServerProfile Profile => _server.Profile;

    public ObservableCollection<string> Log { get; } = new();

    [ObservableProperty]
    public partial string CommandText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveWorldCommand))]
    [NotifyCanExecuteChangedFor(nameof(ListPlayersCommand))]
    public partial bool IsBusy { get; set; }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var command = CommandText.Trim();
        if (command.Length == 0)
        {
            return;
        }

        CommandText = string.Empty;
        await RunCommandAsync(command);
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task SaveWorldAsync() => RunCommandAsync("SaveWorld");

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task ListPlayersAsync() => RunCommandAsync("ListPlayers");

    private bool CanSend() => !IsBusy;

    private async Task RunCommandAsync(string command)
    {
        Log.Add($"> {command}");
        IsBusy = true;
        try
        {
            var response = await _server.SendRconCommandAsync(command);
            Log.Add(string.IsNullOrWhiteSpace(response) ? "(empty response)" : response);
        }
        catch (Exception ex)
        {
            Log.Add($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Close() => _onClose();
}
