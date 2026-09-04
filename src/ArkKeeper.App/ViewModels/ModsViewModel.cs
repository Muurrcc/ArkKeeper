using System.Collections.ObjectModel;
using System.Net.Http;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Networking.SteamCmd;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

/// <summary>Steam Workshop mods for one server: editing <see cref="ServerProfile.ModIds"/> (the
/// list <c>LaunchArgumentsBuilder</c> uses for -mods=, and that <c>ToGameUserSettings</c> now
/// mirrors into ActiveMods) plus actually downloading each mod's content via SteamCMD.</summary>
public partial class ModsViewModel : ViewModelBase
{
    private readonly ServerRowViewModel _server;
    private readonly ProfileStore _profileStore;
    private readonly string _steamCmdDirectory;
    private readonly Action _onClose;

    public ModsViewModel(ServerRowViewModel server, ProfileStore profileStore, string steamCmdDirectory, Action onClose)
    {
        _server = server;
        _profileStore = profileStore;
        _steamCmdDirectory = steamCmdDirectory;
        _onClose = onClose;
    }

    public ServerProfile Profile => _server.Profile;

    /// <summary>The profile's own collection, bound directly — edits here are immediately visible
    /// to a Start() (via LaunchArgumentsBuilder) even before Save persists them to disk.</summary>
    public ObservableCollection<string> ModIds => Profile.ModIds;

    [ObservableProperty]
    public partial string NewModId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial string ProgressLog { get; set; } = string.Empty;

    /// <summary>The most recent line steamcmd reported — see the matching property on
    /// <see cref="ProfileEditorViewModel"/> for why this exists as its own binding.</summary>
    [ObservableProperty]
    public partial string? LatestProgressLine { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadAllCommand))]
    public partial bool IsBusy { get; set; }

    [RelayCommand]
    private async Task AddAsync()
    {
        ErrorMessage = null;

        var id = NewModId.Trim();
        if (id.Length == 0 || !id.All(char.IsAsciiDigit))
        {
            ErrorMessage = "Enter a numeric Steam Workshop item ID.";
            return;
        }

        if (ModIds.Contains(id))
        {
            ErrorMessage = "That mod is already in the list.";
            return;
        }

        ModIds.Add(id);
        await _profileStore.SaveAsync(Profile);
        NewModId = string.Empty;
    }

    [RelayCommand]
    private async Task RemoveAsync(string modId)
    {
        ModIds.Remove(modId);
        await _profileStore.SaveAsync(Profile);
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAllAsync()
    {
        ErrorMessage = null;
        StatusMessage = null;
        ProgressLog = string.Empty;
        LatestProgressLine = null;

        if (string.IsNullOrWhiteSpace(Profile.InstallDirectory))
        {
            ErrorMessage = "Set this server's install directory first (Edit).";
            return;
        }

        if (string.IsNullOrWhiteSpace(_steamCmdDirectory))
        {
            ErrorMessage = "Set the SteamCMD directory in Settings first.";
            return;
        }

        if (ModIds.Count == 0)
        {
            ErrorMessage = "Add at least one mod ID first.";
            return;
        }

        IsBusy = true;
        try
        {
            var installer = new SteamCmdInstaller(new HttpClient());
            var steamCmdExecutable = await installer.EnsureInstalledAsync(_steamCmdDirectory);
            var client = new SteamCmdClient(steamCmdExecutable);

            foreach (var modId in ModIds.ToArray())
            {
                AppendLog($"--- Downloading workshop item {modId} ---");
                var exitCode = await client.DownloadWorkshopItemAsync(Profile.InstallDirectory, modId, AppendLog);
                AppendLog($"--- Item {modId} finished (exit code {exitCode}) ---");
            }

            StatusMessage = "Finished downloading all mods.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't download mods: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanDownload() => !IsBusy;

    /// <summary>SteamCmdClient's output callback fires on the process's own background thread —
    /// marshal onto the UI thread before touching an observable property.</summary>
    private void AppendLog(string line) =>
        Dispatcher.UIThread.Post(() =>
        {
            ProgressLog += line + Environment.NewLine;
            LatestProgressLine = line;
        });

    [RelayCommand]
    private void Close() => _onClose();
}
