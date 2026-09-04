using System.Collections.ObjectModel;
using System.Net.Http;
using ArkKeeper.Core.Profiles;
using ArkKeeper.Networking.SteamCmd;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

/// <summary>Create/edit form for one <see cref="ServerProfile"/>. Edits the profile instance in
/// place rather than a working copy: for an existing profile, that instance is the exact one a
/// <c>ManagedServer</c> in the fleet already holds a reference to, so an in-place edit is visible
/// to it immediately (e.g. on the next Start) — copying then swapping the reference in the
/// profiles collection would leave that ManagedServer pointing at stale settings until the app
/// restarts. Trade-off: Cancel doesn't revert in-memory edits mid-session — nothing reaches disk
/// until Save runs, so a restart still discards anything not saved.</summary>
public partial class ProfileEditorViewModel : ViewModelBase
{
    private readonly ObservableCollection<ServerProfile> _profiles;
    private readonly ProfileStore _profileStore;
    private readonly string _steamCmdDirectory;
    private readonly Action _onClose;

    public ProfileEditorViewModel(
        ServerProfile? existing,
        ObservableCollection<ServerProfile> profiles,
        ProfileStore profileStore,
        string steamCmdDirectory,
        Action onClose)
    {
        _profiles = profiles;
        _profileStore = profileStore;
        _steamCmdDirectory = steamCmdDirectory;
        _onClose = onClose;
        IsNew = existing is null;
        Profile = existing ?? new ServerProfile();
    }

    public ServerProfile Profile { get; }

    public bool IsNew { get; }

    public IReadOnlyList<string> AvailableMaps { get; } =
    [
        "TheIsland", "TheCenter", "ScorchedEarth_P", "Ragnarok", "Aberration_P",
        "Extinction", "Valguero_P", "Genesis", "CrystalIsles", "Genesis2", "LostIsland", "Fjordur",
    ];

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? InstallErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? InstallStatusMessage { get; set; }

    [ObservableProperty]
    public partial string InstallProgressLog { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallServerCommand))]
    public partial bool IsInstalling { get; set; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Profile.ProfileName))
        {
            ErrorMessage = "Profile name is required.";
            return;
        }

        try
        {
            await _profileStore.SaveAsync(Profile);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't save: {ex.Message}";
            return;
        }

        if (IsNew)
        {
            _profiles.Add(Profile);
        }

        _onClose();
    }

    [RelayCommand]
    private void Cancel() => _onClose();

    /// <summary>Actually installs/updates the ARK dedicated server via SteamCMD into
    /// <see cref="ServerProfile.InstallDirectory"/> — without this, Start() has nothing to launch
    /// (it fails with "Server executable not found"). SteamCMD is known to sometimes exit
    /// non-zero on its first-ever run (it self-updates before doing anything else) even though
    /// the install actually completed, so success is judged by whether the server executable
    /// landed on disk afterward, not the exit code alone — see SteamCmdClient's own doc comment.</summary>
    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallServerAsync()
    {
        InstallErrorMessage = null;
        InstallStatusMessage = null;
        InstallProgressLog = string.Empty;

        if (string.IsNullOrWhiteSpace(Profile.InstallDirectory))
        {
            InstallErrorMessage = "Set an install directory first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_steamCmdDirectory))
        {
            InstallErrorMessage = "Set the SteamCMD directory in Settings first.";
            return;
        }

        IsInstalling = true;
        try
        {
            var installer = new SteamCmdInstaller(new HttpClient());
            var steamCmdExecutable = await installer.EnsureInstalledAsync(_steamCmdDirectory);
            var client = new SteamCmdClient(steamCmdExecutable);

            await client.InstallOrUpdateAsync(Profile.InstallDirectory, AppendLog);

            if (File.Exists(Profile.GetServerExecutablePath()))
            {
                InstallStatusMessage = "Server installed/updated successfully.";
            }
            else
            {
                InstallErrorMessage = "SteamCMD finished, but the server executable wasn't found afterward — check the log above.";
            }
        }
        catch (Exception ex)
        {
            InstallErrorMessage = $"Couldn't install the server: {ex.Message}";
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private bool CanInstall() => !IsInstalling;

    /// <summary>SteamCmdClient's output callback fires on the process's own background thread —
    /// marshal onto the UI thread before touching an observable property.</summary>
    private void AppendLog(string line) =>
        Dispatcher.UIThread.Post(() => InstallProgressLog += line + Environment.NewLine);
}
