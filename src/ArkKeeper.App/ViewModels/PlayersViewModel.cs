using System.Collections.ObjectModel;
using ArkKeeper.Core.Players;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Core.Saves;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

/// <summary>Players and tribes for one server: currently-connected players over RCON (with
/// kick/ban), plus the known roster read straight from the profile's save directory (.arkprofile/
/// .arktribe files) — that part works even while the server is stopped, unlike the RCON side.</summary>
public partial class PlayersViewModel : ViewModelBase
{
    private readonly ServerRowViewModel _server;
    private readonly Action _onClose;

    public PlayersViewModel(ServerRowViewModel server, Action onClose)
    {
        _server = server;
        _onClose = onClose;
        LoadKnownPlayersAndTribes();
    }

    public ServerProfile Profile => _server.Profile;

    public ObservableCollection<ConnectedPlayer> ConnectedPlayers { get; } = new();

    public ObservableCollection<PlayerInfo> KnownPlayers { get; } = new();

    public ObservableCollection<TribeInfo> Tribes { get; } = new();

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool HasNoKnownPlayers { get; set; } = true;

    [ObservableProperty]
    public partial bool HasNoTribes { get; set; } = true;

    [ObservableProperty]
    public partial string UnbanSteamId { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshConnectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(KickCommand))]
    [NotifyCanExecuteChangedFor(nameof(BanCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnbanCommand))]
    public partial bool IsBusy { get; set; }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task RefreshConnectedAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var players = await _server.GetPlayersAsync();
            ConnectedPlayers.Clear();
            foreach (var player in players)
            {
                ConnectedPlayers.Add(player);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task KickAsync(ConnectedPlayer player)
    {
        await RunModerationCommandAsync(() => _server.KickPlayerAsync(player.SteamId));
        await RefreshConnectedAsync();
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task BanAsync(ConnectedPlayer player)
    {
        await RunModerationCommandAsync(() => _server.BanPlayerAsync(player.SteamId));
        await RefreshConnectedAsync();
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task UnbanAsync()
    {
        var steamId = UnbanSteamId.Trim();
        if (steamId.Length == 0)
        {
            return;
        }

        await RunModerationCommandAsync(() => _server.UnbanPlayerAsync(steamId));
        UnbanSteamId = string.Empty;
    }

    private bool CanAct() => !IsBusy;

    private async Task RunModerationCommandAsync(Func<Task<string>> action)
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ReloadKnown() => LoadKnownPlayersAndTribes();

    private void LoadKnownPlayersAndTribes()
    {
        KnownPlayers.Clear();
        foreach (var player in PlayerFileReader.ReadDirectory(Profile.GetSaveDirectory()))
        {
            KnownPlayers.Add(player);
        }

        Tribes.Clear();
        foreach (var tribe in TribeFileReader.ReadDirectory(Profile.GetSaveDirectory()))
        {
            Tribes.Add(tribe);
        }

        HasNoKnownPlayers = KnownPlayers.Count == 0;
        HasNoTribes = Tribes.Count == 0;
    }

    [RelayCommand]
    private void Close() => _onClose();
}
